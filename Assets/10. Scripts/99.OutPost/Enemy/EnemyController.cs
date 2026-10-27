
using UnityEngine;
using UnityEngine.AI;


namespace Study3D.OutPost
{
    
    // 적의 행동패턴 관련 스크립트
    // 1. 적은 일정 거리를 순찰하듯이 돌아다니다가 플레이어 발견시 달려듬
    // 2. 적은 일정 거리를 순찰하듯이 돌아다니다가 때때재로 제자리에 정지해서 Idle상태로 자리에 그대로 가만히 있음
    // 3. 순찰하거나 가만히 있을 때 플레이어를 발견하면 달려든다. 
    
    
    // 플레이어를 static으로 싱글톤 구조
    
    
    public class EnemyController : MonoBehaviour
    {
        private enum EnemyState
        {
            Walk,
            Stay,
            Run,
            Attack,
            Dead
        }
        
        [Header("적 세팅")] 
        [SerializeField] private EnemyState state = EnemyState.Stay;
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

        private Vector3 spawnPos;   // 처음 서 있던 위치
        private float stayTimer;    // 남은 대기 시간
        
        private float nextAttackTime;   // 다음 공격이 가능한 시각
        
        private bool isAttacking;
        private float attackTimer;
        private bool hasHit;   // 이번 공격에서 이미 데미지를 줬는지
        
        private Vector3 lastSeenPos;   // 마지막으로 플레이어를 본 위치
        private float lostTimer;       // 시야에서 사라진 뒤 지난 시간
        
        private Transform player;
        private PlayerHealth playerHealth;
        
        private EnemyHealth enemyHealth;
        private Collider bodyCollider;
        
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        
        
        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            bodyCollider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            if (enemyHealth != null) enemyHealth.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (enemyHealth != null) enemyHealth.OnDeath -= HandleDeath;
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
        }

        private void Update()
        {
            if (state == EnemyState.Dead) return; 
            
            switch (state)
            {
                case EnemyState.Stay: UpdateStay(); break;
                case EnemyState.Walk: UpdateWalk(); break;
                case EnemyState.Run:  UpdateRun();  break;
                case EnemyState.Attack: UpdateAttack(); break;
                
            }

            UpdateAnimation();
        }


        #region 이동 및 추적
        
        // 적이 플레이어를 발견하지 못하면 그냥 걸어다니는 순찰 상태
        private void UpdateWalk()
        {
            if (TryDetectPlayer()) return;

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

            stayTimer -= Time.deltaTime;
            if (stayTimer <= 0f)
                StartPatrol();
        }
        
        
        // 적이 플레이어를 발견했을 때 달려오는 상태  
        // Ray를 사용해서 플레이어를 감지?
        private void UpdateRun()
        {
            if (player == null) { state = EnemyState.Stay; return; }

            // 보이면 위치 갱신, 안 보이면 타이머 증가
            if (CanSeePlayer(false))
            {
                lastSeenPos = player.position;
                lostTimer = 0f;

                // ↓ 추가: 공격 거리 안이면 공격 상태로
                if (DistanceToPlayer() <= attackRange)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;   // ← 추가: 즉시 멈춤
                    state = EnemyState.Attack;
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
            {
                agent.ResetPath();
                stayTimer = Random.Range(minStayTime, maxStayTime);   // ← 추가
                state = EnemyState.Stay;
            }
        }
        
        // 플레이어가 보이면 추적 시작하고 true
        private bool TryDetectPlayer()
        {
            if (!CanSeePlayer()) return false;

            lastSeenPos = player.position;
            lostTimer = 0f;
            state = EnemyState.Run;
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
            }// 2) 시야각: 높이 차이는 무시하고 수평 각도만 비교
            

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
        
        
        #endregion
        
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
        }
        
        
        #region 공격
        
        // 공격 상태: 멈춰서 플레이어를 보고, 주기마다 공격
        private void UpdateAttack()
        {
            if (player == null) { state = EnemyState.Stay; return; }

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

            // 플레이어가 죽었으면 멈춤
            if (playerHealth != null && playerHealth.IsDead)
            {
                state = EnemyState.Stay;
                return;
            }

            // 멀어지면 다시 추적
            if (DistanceToPlayer() > attackRange + 0.5f)
            {
                lastSeenPos = player.position;
                lostTimer = 0f;
                state = EnemyState.Run;
                return;
            }

            LookAtPlayer();

            if (Time.time >= nextAttackTime && AngleToPlayer() <= attackAngle * 0.5f)
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
            if (playerHealth == null) return;
            if (DistanceToPlayer() > attackRange + 0.5f) return;   // 뒤로 빠져서 피함
            if (AngleToPlayer() > attackAngle * 0.5f) return;   // 옆이나 뒤로 피함
            
            Vector3 hitPoint = player.position + Vector3.up;
            Vector3 hitNormal = (transform.position - player.position).normalized;
            playerHealth.TakeDamage(attackDamage, hitPoint, hitNormal);
        }

        // 높이를 무시한 수평 거리
        private float DistanceToPlayer()
        {
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        // 적의 정면과 플레이어 방향 사이 각도 (0 = 정면, 180 = 등 뒤)
        private float AngleToPlayer()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            return Vector3.Angle(transform.forward, dir);
        }
        
        
        // 플레이어 쪽으로 부드럽게 회전
        private void LookAtPlayer()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
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
            
            
            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? spawnPos : transform.position;
            Gizmos.DrawWireSphere(center, patrolRadius);
            
        }
        
    }
}