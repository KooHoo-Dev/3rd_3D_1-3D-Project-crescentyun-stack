
using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


namespace Study3D.OutPost
{

    // 적의 행동패턴 관련 스크립트
    // 1. 적은 일정 거리를 순찰하듯이 돌아다니다가 플레이어 발견시 달려듬
    // 2. 적은 일정 거리를 순찰하듯이 돌아다니다가 때때로 제자리에 정지해서 Idle상태로 자리에 그대로 가만히 있음
    // 3. 순찰하거나 가만히 있을 때 플레이어를 발견하면 달려든다.
    // 4. 기본 행동(behavior)에 따라 평소 행동이 달라짐
    //    - Wander : 스폰 위치 주변 순찰 (군사기지)
    //    - Guard  : 제자리 대기, 보거나 맞으면 추격 (도로, 하이브)
    //    - Assault: 전초기지로 진격해서 공격, 가다가 플레이어를 보면 플레이어 공격 (전초기지)


    // 플레이어를 static으로 싱글톤 구조


    public class EnemyController : MonoBehaviour
    {
        private enum EnemyState
        {
            Walk,
            Stay,
            Run,
            Attack,
            March,   // 전초기지로 진격 중
            ChaseTurret,   // 포탑으로 달려가는 중
            ChaseBomb,     // 하이브 폭탄 쪽으로 몰려가는 중 (가다가 플레이어/포탑을 보면 그쪽 공격)
            Dead
        }

        public enum EnemyBehavior
        {
            Wander,
            Guard,
            Assault
        }

        [Header("적 세팅")]
        [SerializeField] private EnemyState state = EnemyState.Stay;
        [SerializeField] private EnemyBehavior behavior = EnemyBehavior.Wander;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent agent;


        [Header("감지")]
        [SerializeField] private float detectRange = 8f;
        [SerializeField] private float viewAngle = 90f;
        [SerializeField] private float eyeHeight = 1.5f;
        [SerializeField] private LayerMask obstacleMask;


        [Header("추적")]
        [SerializeField] private float loseSightTime = 5f;   // 시야에서 사라진 뒤 포기까지 걸리는 시간 (초)


        [Header("공격")]
        [SerializeField] private float attackRange = 1.5f;     // 공격 거리 (m)
        [SerializeField] private float attackDuration = 2.2f;   // 공격 모션 전체 시간 (초, Speed 반영한 실제 시간)
        [SerializeField] private float attackHitTime = 0.8f;    // 공격 시작 후 데미지가 들어가는 순간 (초)
        [SerializeField] private float attackCooldown = 0.5f;   // 공격이 끝난 뒤 쉬는 시간 (초)
        [SerializeField] private float attackAngle = 60f;   // 정면 기준 이 각도 안에 있어야 공격, 타격 (전체 각도)
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float turnSpeed = 360f;       // 플레이어를 향해 도는 속도 (초당 각도)

        [Header("순찰")]
        [SerializeField] private float patrolRadius = 6f;   // 스폰 위치 기준 순찰 반경 (m)
        [SerializeField] private float minStayTime = 2f;    // 대기 최소 시간 (초)
        [SerializeField] private float maxStayTime = 4f;    // 대기 최대 시간 (초)

        [Header("진격 (Assault)")]
        [SerializeField] private Transform assaultTarget;   // 진격 목표 (전초기지). 스포너가 넣어줌

        [Header("사망")]
        [SerializeField] private float corpseLifetime = 10f;   // 죽은 뒤 시체가 사라지기까지 시간 (초)


        // 스포너(지역)가 구독하는 이벤트
        public event Action<EnemyController> OnPlayerSpotted;   // 플레이어를 발견하거나 맞았을 때 (하이브 경보용)
        public event Action<EnemyController> OnDied;            // 죽었을 때 (지역 마릿수 카운트용)

        public bool IsDead => state == EnemyState.Dead;


        private Vector3 spawnPos;   // 처음 서 있던 위치
        private float stayTimer;    // 남은 대기 시간

        private float nextAttackTime;   // 다음 공격이 가능한 시각

        private bool isAttacking;
        private float attackTimer;
        private bool hasHit;   // 이번 공격에서 이미 데미지를 줬는지

        private Transform attackTarget;       // 지금 공격 중인 대상 (플레이어 or 기지 or 포탑)
        private TurretHealth turretTarget;    // 노리는 포탑
        private Collider turretCollider;      // 포탑 콜라이더 (가장 가까운 면까지 거리로 판정)
        private Collider assaultCollider;     // 기지 콜라이더 (기지는 크니까 가장 가까운 면까지 거리로 판정)
        private IDamageable assaultDamageable;
        private HiveBomb bombTarget;          // 노리는 폭탄
        private Collider bombCollider;

        private Vector3 lastSeenPos;   // 마지막으로 플레이어를 본 위치
        private float lostTimer;       // 시야에서 사라진 뒤 지난 시간

        private Transform player;
        private PlayerHealth playerHealth;

        private EnemyHealth enemyHealth;
        private Collider bodyCollider;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int AttackHash = Animator.StringToHash("Attack");


        // 스포너가 Instantiate 직후 호출 (Start보다 먼저 실행됨)
        public void Init(EnemyBehavior newBehavior, Transform newAssaultTarget)
        {
            behavior = newBehavior;
            assaultTarget = newAssaultTarget;
        }


        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            bodyCollider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath += HandleDeath;
                enemyHealth.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath -= HandleDeath;
                enemyHealth.OnDamaged -= HandleDamaged;
            }
        }


        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = attackRange * 0.8f;
            spawnPos = transform.position;
            stayTimer = Random.Range(minStayTime, maxStayTime);


            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance.transform;
                playerHealth = PlayerController.Instance.GetComponent<PlayerHealth>();
            }
            else
                Debug.LogWarning("PlayerController.Instance가 없음!");


            if (assaultTarget != null)
            {
                assaultCollider = assaultTarget.GetComponentInChildren<Collider>();
                assaultDamageable = assaultTarget.GetComponentInParent<IDamageable>();
            }

            // 시작 상태를 기본 행동에 맞춤
            ReturnToDefault();
        }

        private void Update()
        {
            if (state == EnemyState.Dead) return;

            TryTargetBomb();   // 폭탄이 설치되어 있으면 할 일 없는 좀비는 폭탄 쪽으로

            switch (state)
            {
                case EnemyState.Stay:   UpdateStay();   break;
                case EnemyState.Walk:   UpdateWalk();   break;
                case EnemyState.Run:    UpdateRun();    break;
                case EnemyState.Attack: UpdateAttack(); break;
                case EnemyState.March:  UpdateMarch();  break;
                case EnemyState.ChaseTurret: UpdateChaseTurret(); break;
                case EnemyState.ChaseBomb:   UpdateChaseBomb();   break;
            }

            UpdateAnimation();
        }


        #region 기본 행동

        // 추격을 포기했거나 할 일이 끝났을 때 평소 행동으로 돌아감
        private void ReturnToDefault()
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            attackTarget = null;
            turretTarget = null;
            turretCollider = null;
            bombTarget = null;
            bombCollider = null;

            if (behavior == EnemyBehavior.Assault && assaultTarget != null)
            {
                StartMarch();
                return;
            }

            stayTimer = Random.Range(minStayTime, maxStayTime);
            state = EnemyState.Stay;
        }

        // 외부(하이브 경보 등)에서 플레이어 위치로 달려가게 함
        public void Alert(Vector3 targetPos)
        {
            if (state == EnemyState.Dead) return;
            if (isAttacking) return;                                         // 공격 모션은 끝까지
            if (state == EnemyState.Attack && attackTarget == player) return;   // 이미 플레이어와 싸우는 중

            lastSeenPos = targetPos;
            lostTimer = 0f;
            attackTarget = null;
            state = EnemyState.Run;
        }

        #endregion


        #region 이동 및 추적

        // 적이 플레이어를 발견하지 못하면 그냥 걸어다니는 순찰 상태
        private void UpdateWalk()
        {
            if (TryDetectPlayer()) return;
            if (TryDetectTurret()) return;

            // 목적지에 도착하면 대기
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                agent.ResetPath();
                stayTimer = Random.Range(minStayTime, maxStayTime);
                state = EnemyState.Stay;
            }
        }

        // 적이 플레이어를 발견하지 못하고 그냥 가만히 대기하고 있는 상태
        private void UpdateStay()
        {
            if (TryDetectPlayer()) return;
            if (TryDetectTurret()) return;

            // Guard는 순찰하지 않고 계속 제자리
            if (behavior == EnemyBehavior.Guard) return;

            stayTimer -= Time.deltaTime;
            if (stayTimer <= 0f)
                StartPatrol();
        }


        // 적이 플레이어를 발견했을 때 달려오는 상태
        private void UpdateRun()
        {
            if (player == null) { ReturnToDefault(); return; }

            // 보이면 위치 갱신, 안 보이면 타이머 증가
            if (CanSeePlayer(false))
            {
                lastSeenPos = player.position;
                lostTimer = 0f;

                // 공격 거리 안이면 공격 상태로
                if (DistanceTo(player) <= attackRange)
                {
                    BeginAttack(player);
                    return;
                }
            }
            else
            {
                lostTimer += Time.deltaTime;
            }

            agent.speed = runSpeed;
            agent.SetDestination(lastSeenPos);

            if (lostTimer >= loseSightTime)
                ReturnToDefault();
        }

        // 플레이어가 보이면 추적 시작하고 true
        private bool TryDetectPlayer()
        {
            if (!CanSeePlayer()) return false;

            lastSeenPos = player.position;
            lostTimer = 0f;
            attackTarget = null;
            state = EnemyState.Run;

            OnPlayerSpotted?.Invoke(this);
            return true;
        }

        // 플레이어가 보이면 true
        private bool CanSeePlayer(bool checkAngle = true)
        {
            if (playerHealth != null && playerHealth.IsDead) return false;
            if (player == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;   // 적의 눈
            Vector3 targetPos = player.position + Vector3.up * 1f;          // 플레이어 가슴
            Vector3 toPlayer = targetPos - eyePos;
            float distance = toPlayer.magnitude;

            // 1) 거리: 감지 거리보다 멀면 못 봄
            if (distance > detectRange) return false;

            // 2) 시야각: 높이 차이는 무시하고 수평 각도만 비교
            if (checkAngle)
            {
                Vector3 flatDir = toPlayer;
                flatDir.y = 0f;
                if (Vector3.Angle(transform.forward, flatDir) > viewAngle * 0.5f) return false;
            }

            // 3) 벽: 플레이어까지 가는 길에 벽이 맞으면 가려진 것
            if (Physics.Raycast(eyePos, toPlayer.normalized, distance, obstacleMask)) return false;

            return true;
        }

        // 스폰 주변 랜덤 지점으로 걷기 시작
        private void StartPatrol()
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = spawnPos + new Vector3(r.x, 0f, r.y);

            // 그 지점 근처에서 실제로 걸을 수 있는 위치 찾기
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.speed = moveSpeed;
                agent.SetDestination(hit.position);
                state = EnemyState.Walk;
            }
            else
            {
                stayTimer = 0.5f;   // 못 찾으면 0.5초 뒤 다시 시도
            }
        }


        // 전초기지를 향해 걷기 시작
        private void StartMarch()
        {
            agent.speed = moveSpeed;
            agent.SetDestination(assaultTarget.position);
            state = EnemyState.March;
        }

        // 진격 중: 플레이어를 보면 플레이어로, 기지에 닿으면 기지 공격
        private void UpdateMarch()
        {
            if (TryDetectPlayer()) return;
            if (TryDetectTurret()) return;
            if (assaultTarget == null) { behavior = EnemyBehavior.Guard; ReturnToDefault(); return; }

            if (DistanceTo(assaultTarget) <= attackRange)
                BeginAttack(assaultTarget);
        }

        #endregion


        #region 포탑

        // 시야 안에서 가장 가까운 포탑을 찾으면 추격 시작하고 true (플레이어를 먼저 확인한 뒤 호출)
        private bool TryDetectTurret()
        {
            TurretHealth best = null;
            float bestDistance = detectRange;

            foreach (TurretHealth turret in TurretHealth.All)
            {
                if (turret.IsDestroyed) continue;
                float distance = Vector3.Distance(transform.position, turret.transform.position);
                if (distance > bestDistance) continue;
                if (!CanSeeTurret(turret)) continue;

                bestDistance = distance;
                best = turret;
            }

            if (best == null) return false;
            StartChaseTurret(best);
            return true;
        }

        // 플레이어 시야 판정과 같은 방식 (거리, 시야각, 벽)
        private bool CanSeeTurret(TurretHealth turret)
        {
            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPos = turret.transform.position + Vector3.up * 1f;
            Vector3 toTurret = targetPos - eyePos;

            Vector3 flatDir = toTurret;
            flatDir.y = 0f;
            if (Vector3.Angle(transform.forward, flatDir) > viewAngle * 0.5f) return false;

            return !Physics.Raycast(eyePos, toTurret.normalized, toTurret.magnitude, obstacleMask);
        }

        private void StartChaseTurret(TurretHealth turret)
        {
            turretTarget = turret;
            turretCollider = turret.GetComponentInChildren<Collider>();
            attackTarget = null;
            state = EnemyState.ChaseTurret;
        }

        // 포탑으로 달려가서 닿으면 공격. 가는 중에 플레이어가 보이면 플레이어 우선
        private void UpdateChaseTurret()
        {
            if (TryDetectPlayer()) return;
            if (turretTarget == null || turretTarget.IsDestroyed) { ReturnToDefault(); return; }

            agent.speed = runSpeed;
            agent.SetDestination(turretTarget.transform.position);

            if (DistanceTo(turretTarget.transform) <= attackRange)
                BeginAttack(turretTarget.transform);
        }

        private bool IsTurret(Transform target)
            => turretTarget != null && target == turretTarget.transform;

        #endregion


        #region 하이브 폭탄

        // 폭탄이 설치되어 있으면 맵의 모든 좀비가 폭탄 쪽으로 몰려감 (거리·시야 상관없음)
        // 평소 행동(대기/순찰/진격) 중인 좀비만 → 플레이어 추격, 포탑 공격 중이면 그걸 먼저 끝냄
        // 추격을 포기하거나 대상이 부서지면 ReturnToDefault → 다음 프레임에 다시 폭탄 쪽으로
        private void TryTargetBomb()
        {
            HiveBomb bomb = HiveBomb.Active;
            if (bomb == null || !bomb.IsAlive) return;
            if (state != EnemyState.Stay && state != EnemyState.Walk && state != EnemyState.March) return;

            bombTarget = bomb;
            bombCollider = bomb.GetComponentInChildren<Collider>();
            turretTarget = null;
            turretCollider = null;
            attackTarget = null;
            state = EnemyState.ChaseBomb;
            SetBombDestination();
        }

        // 폭탄은 움직이지 않으므로 경로를 한 번만 계산
        // (매 프레임 SetDestination을 다시 부르면 먼 거리 경로 계산이 끝나지 못해서 이전 경로(전초기지 진격)를 계속 따라감)
        private void SetBombDestination()
        {
            if (!agent.isOnNavMesh || bombTarget == null) return;

            Vector3 dest = bombTarget.transform.position;
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                dest = hit.position;   // 폭탄 바로 옆의 걸을 수 있는 지점

            agent.ResetPath();   // 진격/순찰 경로 버리기
            agent.speed = runSpeed;
            agent.SetDestination(dest);
        }

        // 폭탄으로 가는 중: 플레이어나 포탑이 보이면 그쪽부터 공격 (평소 우선순위와 같음)
        private void UpdateChaseBomb()
        {
            if (bombTarget == null || !bombTarget.IsAlive) { ReturnToDefault(); return; }
            if (TryDetectPlayer()) return;
            if (TryDetectTurret()) return;

            agent.speed = runSpeed;
            // 경로가 없을 때만 다시 계산 (공격하다 멀어진 경우 등)
            if (!agent.pathPending && !agent.hasPath) SetBombDestination();

            if (DistanceTo(bombTarget.transform) <= attackRange)
                BeginAttack(bombTarget.transform);
        }

        private bool IsBomb(Transform target)
            => bombTarget != null && target != null && target == bombTarget.transform;

        #endregion


        private void HandleDamaged()
        {
            if (state == EnemyState.Dead) return;

            // 포탑 총알에 맞음 → 그 포탑을 공격하러 감 (플레이어와 싸우는 중이면 플레이어 우선)
            Transform attacker = enemyHealth.LastAttacker;
            TurretHealth shooter = attacker != null ? attacker.GetComponent<TurretHealth>() : null;
            if (shooter != null)
            {
                if (shooter.IsDestroyed || isAttacking) return;
                if (state == EnemyState.Run) return;                                         // 플레이어 추격 중
                if (state == EnemyState.Attack && attackTarget == player) return;             // 플레이어 공격 중
                if (turretTarget != null && !turretTarget.IsDestroyed &&
                    (state == EnemyState.ChaseTurret || state == EnemyState.Attack)) return;  // 이미 다른 포탑 상대 중

                StartChaseTurret(shooter);
                return;
            }

            if (player == null) return;

            // 이미 추격 중이면 위치만 갱신, 아니면 플레이어 쪽으로 달려감
            if (state == EnemyState.Run)
            {
                lastSeenPos = player.position;
                lostTimer = 0f;
            }
            else
            {
                Alert(player.position);
            }

            OnPlayerSpotted?.Invoke(this);
        }

        private void HandleDeath()
        {
            state = EnemyState.Dead;

            // 공격 중이었다면 취소
            isAttacking = false;
            animator.ResetTrigger(AttackHash);

            // 이동 정지
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;

            // 서 있는 콜라이더 끄기 → 총알 통과
            if (bodyCollider != null) bodyCollider.enabled = false;

            OnDied?.Invoke(this);
            Destroy(gameObject, corpseLifetime);   // 시체 정리
        }


        #region 공격

        private void BeginAttack(Transform target)
        {
            attackTarget = target;
            agent.ResetPath();
            agent.velocity = Vector3.zero;   // 즉시 멈춤
            state = EnemyState.Attack;
        }

        // 공격 상태: 멈춰서 대상을 보고, 주기마다 공격
        private void UpdateAttack()
        {
            if (attackTarget == null) { ReturnToDefault(); return; }

            // 공격 모션 중: 이동, 회전, 상태 변경 금지
            if (isAttacking)
            {
                attackTimer += Time.deltaTime;

                if (!hasHit && attackTimer >= attackHitTime)
                {
                    TryHit();
                    hasHit = true;
                }

                if (attackTimer >= attackDuration)
                {
                    isAttacking = false;
                    nextAttackTime = Time.time + attackCooldown;
                }
                return;
            }

            if (attackTarget == player)
            {
                // 플레이어가 죽었으면 평소 행동으로
                if (playerHealth != null && playerHealth.IsDead) { ReturnToDefault(); return; }

                // 멀어지면 다시 추적
                if (DistanceTo(player) > attackRange + 0.5f)
                {
                    lastSeenPos = player.position;
                    lostTimer = 0f;
                    attackTarget = null;
                    state = EnemyState.Run;
                    return;
                }
            }
            else if (IsBomb(attackTarget))
            {
                // 폭탄을 치다가 플레이어가 보이면 플레이어로 전환 (포탑 총에 맞으면 HandleDamaged에서 포탑으로)
                if (TryDetectPlayer()) return;
                // 부서지거나 터지면 평소 행동으로
                if (!bombTarget.IsAlive) { ReturnToDefault(); return; }
                if (DistanceTo(attackTarget) > attackRange + 0.5f)
                {
                    attackTarget = null;
                    state = EnemyState.ChaseBomb;
                    return;
                }
            }
            else
            {
                // 기지/포탑을 치다가 플레이어가 보이면 플레이어로 전환
                if (TryDetectPlayer()) return;

                if (IsTurret(attackTarget))
                {
                    // 포탑이 부서지면 평소 행동으로, 멀어지면 다시 추격
                    if (turretTarget.IsDestroyed) { ReturnToDefault(); return; }
                    if (DistanceTo(attackTarget) > attackRange + 0.5f)
                    {
                        attackTarget = null;
                        state = EnemyState.ChaseTurret;
                        return;
                    }
                }
            }

            LookAtTarget(attackTarget);

            if (Time.time >= nextAttackTime && AngleTo(attackTarget) <= attackAngle * 0.5f)
                StartAttack();
        }

        // 공격 시작: 애니메이션만 재생
        private void StartAttack()
        {
            animator.SetTrigger(AttackHash);
            isAttacking = true;
            attackTimer = 0f;
            hasHit = false;
        }

        // 팔이 닿는 순간: 이때 사거리 안에 있어야 맞음
        private void TryHit()
        {
            if (attackTarget == null) return;
            if (DistanceTo(attackTarget) > attackRange + 0.5f) return;   // 뒤로 빠져서 피함
            if (AngleTo(attackTarget) > attackAngle * 0.5f) return;      // 옆이나 뒤로 피함

            Vector3 point = GetTargetPoint(attackTarget);
            Vector3 hitPoint = point + Vector3.up;
            Vector3 hitNormal = (transform.position - point).normalized;

            if (attackTarget == player)
            {
                if (playerHealth != null) playerHealth.TakeDamage(attackDamage, hitPoint, hitNormal);
            }
            else if (IsTurret(attackTarget))
            {
                turretTarget.TakeDamage(attackDamage, hitPoint, hitNormal);
            }
            else if (IsBomb(attackTarget))
            {
                bombTarget.TakeDamage(attackDamage);
            }
            else
            {
                assaultDamageable?.TakeDamage(attackDamage, hitPoint, hitNormal);
            }
        }

        // 기지는 크기 때문에 중심이 아니라 콜라이더의 가장 가까운 지점을 기준으로 함
        private Vector3 GetTargetPoint(Transform target)
        {
            if (target == assaultTarget && assaultCollider != null)
                return assaultCollider.ClosestPoint(transform.position);
            if (IsTurret(target) && turretCollider != null && turretCollider.enabled)
                return turretCollider.ClosestPoint(transform.position);
            if (IsBomb(target) && bombCollider != null && bombCollider.enabled)
                return bombCollider.ClosestPoint(transform.position);
            return target.position;
        }

        // 높이를 무시한 수평 거리
        private float DistanceTo(Transform target)
        {
            Vector3 d = GetTargetPoint(target) - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        // 적의 정면과 대상 방향 사이 각도 (0 = 정면, 180 = 등 뒤)
        private float AngleTo(Transform target)
        {
            Vector3 dir = GetTargetPoint(target) - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return 0f;   // 딱 붙어 있으면 정면으로 취급
            return Vector3.Angle(transform.forward, dir);
        }

        // 대상 쪽으로 부드럽게 회전
        private void LookAtTarget(Transform target)
        {
            Vector3 dir = GetTargetPoint(target) - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }

        #endregion


        // Agent의 실제 속도를 Blend Tree 값으로 바꿔서 전달
        private void UpdateAnimation()
        {
            // 월드 속도 → 적 기준 속도 (앞뒤 = z, 좌우 = x)
            Vector3 localVel = transform.InverseTransformDirection(agent.velocity);
            float speed = localVel.magnitude;

            // 속도 → Blend Tree 값: 걷기 속도 = 1, 달리기 속도 = 2
            float blend;
            if (speed <= moveSpeed)
                blend = speed / moveSpeed;
            else
                blend = 1f + (speed - moveSpeed) / (runSpeed - moveSpeed);

            Vector3 dir = speed > 0.01f ? localVel / speed : Vector3.zero;   // 방향만 (길이 1)
            animator.SetFloat(MoveXHash, dir.x * blend, 0.1f, Time.deltaTime);
            animator.SetFloat(MoveZHash, dir.z * blend, 0.1f, Time.deltaTime);
        }

        // Gizmo 감지 확인
        private void OnDrawGizmosSelected()
        {
            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

            // 1) 감지 거리 (노란 원)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            // 2) 시야각 양쪽 경계선 (파란 선)
            Vector3 leftDir  = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0f,  viewAngle * 0.5f, 0f) * transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(eyePos, eyePos + leftDir  * detectRange);
            Gizmos.DrawLine(eyePos, eyePos + rightDir * detectRange);

            // 3) 플레이 중일 때 플레이어 방향 선 (보이면 초록, 안 보이면 빨강)
            if (Application.isPlaying && player != null)
            {
                Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
                Gizmos.DrawLine(eyePos, player.position + Vector3.up * 1f);
            }


            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);


            // 순찰 범위는 Wander일 때만 의미 있음
            if (behavior == EnemyBehavior.Wander)
            {
                Gizmos.color = Color.green;
                Vector3 center = Application.isPlaying ? spawnPos : transform.position;
                Gizmos.DrawWireSphere(center, patrolRadius);
            }

            // 진격 목표 (자홍색 선)
            if (behavior == EnemyBehavior.Assault && assaultTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, assaultTarget.position);
            }
        }

    }
}
