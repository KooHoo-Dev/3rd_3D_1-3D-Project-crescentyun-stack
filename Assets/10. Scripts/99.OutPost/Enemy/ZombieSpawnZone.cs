using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Study3D.OutPost
{
    // 지역 하나에 좀비를 무작위로 생성하고 관리하는 스크립트
    // - 지역(박스) 안 랜덤 위치, NavMesh 위에만 생성
    // - 플레이어 시야(PlayerVision) 안이거나 너무 가까우면 생성하지 않음
    // - 지역별 최대 마릿수 유지, 재생성 주기마다 부족한 만큼 채움
    // - 하이브: 한 마리가 플레이어를 발견하면 지역 전체 좀비가 추격
    public class ZombieSpawnZone : MonoBehaviour
    {
        [Header("지역")]
        [SerializeField] private Vector3 areaSize = new Vector3(20f, 4f, 20f);   // 생성 영역 크기 (오브젝트 회전 반영)

        [Header("좀비")]
        [SerializeField] private EnemyController zombiePrefab;   // zombie01
        [SerializeField] private EnemyController.EnemyBehavior behavior = EnemyController.EnemyBehavior.Wander;
        [SerializeField] private Transform assaultTarget;          // Assault일 때 진격 목표 (전초기지)

        [Header("마릿수 / 주기")]
        [SerializeField] private int maxAlive = 10;            // 지역에 동시에 살아 있을 수 있는 최대 수
        [SerializeField] private int initialCount = 10;        // 시작할 때 생성할 수
        [SerializeField] private float respawnInterval = 20f;  // 재생성 주기 (초)
        [SerializeField] private int spawnPerTick = 2;         // 한 주기에 최대 몇 마리 채울지

        [Header("생성 위치 조건")]
        [SerializeField] private float minDistanceFromPlayer = 5f;   // 시야 밖이라도 이 거리 안에는 생성 X
        [SerializeField] private float activationRange = 0f;         // 플레이어가 이 거리 안일 때만 생성 (0 = 항상)
        [SerializeField] private int maxTryCount = 15;               // 위치 찾기 재시도 횟수 (1마리당)
        [SerializeField] private float spawnCheckHeight = 1f;        // 시야 판정 높이 (VisionTarget과 같게)

        [Header("시작 조건")]
        [SerializeField] private bool waitForActivation = false;   // 체크하면 Activate()가 호출될 때부터 생성 시작 (ZoneActivationTrigger 사용)

        [Header("하이브 경보")]
        [SerializeField] private bool alertAllOnSpot = false;   // 한 마리가 발견하면 지역 전체가 추격
        [SerializeField] private float alertCooldown = 1f;      // 경보 연속 발생 방지 (초)

        private readonly List<EnemyController> alive = new List<EnemyController>();
        private float respawnTimer;
        private int pendingInitial;     // 시작 시 아직 못 채운 수 (시야 안이라 실패한 경우 다음 주기에 채움)
        private float lastAlertTime = -999f;
        private bool isActive;          // 생성이 시작됐는지

        public int AliveCount => alive.Count;
        public bool IsActive => isActive;


        private void Start()
        {
            if (zombiePrefab == null)
            {
                Debug.LogWarning($"{name}: zombiePrefab이 비어 있음!");
                enabled = false;
                return;
            }

            if (behavior == EnemyController.EnemyBehavior.Assault && assaultTarget == null)
                Debug.LogWarning($"{name}: Assault인데 assaultTarget(전초기지)이 비어 있음!");

            // 대기 옵션이 꺼져 있으면 바로 시작
            if (!waitForActivation) Activate();
        }

        // 생성 시작 (한 번 시작하면 계속 유지). 트리거 등 외부에서 호출
        public void Activate()
        {
            if (isActive || !enabled) return;
            isActive = true;

            pendingInitial = Mathf.Min(initialCount, maxAlive);
            pendingInitial -= SpawnMany(pendingInitial);
            respawnTimer = respawnInterval;
        }

        private void Update()
        {
            if (!isActive) return;

            // 시작할 때 못 채운 수는 1초마다 다시 시도
            if (pendingInitial > 0)
            {
                respawnTimer = Mathf.Min(respawnTimer, 1f);
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer > 0f) return;
            respawnTimer = respawnInterval;

            if (!IsPlayerInActivationRange()) return;

            int count = pendingInitial > 0 ? pendingInitial : spawnPerTick;
            int spawned = SpawnMany(count);
            pendingInitial = Mathf.Max(0, pendingInitial - spawned);
        }


        #region 생성

        // count마리까지 생성 시도, 실제로 생성한 수를 반환
        private int SpawnMany(int count)
        {
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                if (alive.Count >= maxAlive) break;
                if (!TrySpawnOne()) break;   // 자리가 없으면 이번 주기는 포기
                spawned++;
            }
            return spawned;
        }

        private bool TrySpawnOne()
        {
            if (!TryFindSpawnPoint(out Vector3 pos)) return false;

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            // 부모로 넣지 않음: 지역 오브젝트 스케일이 좀비 크기에 영향을 주지 않도록
            EnemyController zombie = Instantiate(zombiePrefab, pos, rot);
            zombie.Init(behavior, assaultTarget);   // Start보다 먼저 실행됨

            zombie.OnDied += HandleZombieDied;
            if (alertAllOnSpot) zombie.OnPlayerSpotted += HandlePlayerSpotted;

            alive.Add(zombie);
            return true;
        }

        // 지역 안 랜덤 위치 → NavMesh 보정 → 시야/거리 검사
        private bool TryFindSpawnPoint(out Vector3 result)
        {
            for (int i = 0; i < maxTryCount; i++)
            {
                Vector3 local = new Vector3(
                    Random.Range(-0.5f, 0.5f) * areaSize.x,
                    0f,
                    Random.Range(-0.5f, 0.5f) * areaSize.z);
                Vector3 candidate = transform.TransformPoint(local);

                // 걸을 수 있는 바닥으로 보정 (박스 높이만큼 위아래 탐색)
                float sampleRange = Mathf.Max(1f, areaSize.y);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRange, NavMesh.AllAreas))
                    continue;

                if (!IsHiddenFromPlayer(hit.position)) continue;

                result = hit.position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        // Duckov 방식: 플레이어 시야(부채꼴 + 주변 원, 벽 가림) 밖 + 최소 거리 밖이면 true
        private bool IsHiddenFromPlayer(Vector3 pos)
        {
            PlayerController pc = PlayerController.Instance;
            if (pc != null)
            {
                Vector3 d = pos - pc.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer) return false;
            }

            PlayerVision vision = PlayerVision.Instance;
            if (vision != null && vision.CanSee(pos + Vector3.up * spawnCheckHeight)) return false;

            return true;
        }

        private bool IsPlayerInActivationRange()
        {
            if (activationRange <= 0f) return true;
            PlayerController pc = PlayerController.Instance;
            if (pc == null) return true;
            return Vector3.Distance(pc.transform.position, transform.position) <= activationRange;
        }

        #endregion


        #region 이벤트

        private void HandleZombieDied(EnemyController zombie)
        {
            zombie.OnDied -= HandleZombieDied;
            zombie.OnPlayerSpotted -= HandlePlayerSpotted;
            alive.Remove(zombie);
        }

        // 하이브: 한 마리가 발견하면 살아 있는 전원에게 플레이어 위치 전달
        private void HandlePlayerSpotted(EnemyController spotter)
        {
            if (Time.time - lastAlertTime < alertCooldown) return;
            lastAlertTime = Time.time;

            PlayerController pc = PlayerController.Instance;
            if (pc == null) return;
            Vector3 playerPos = pc.transform.position;

            foreach (EnemyController z in alive)
            {
                if (z == null || z == spotter || z.IsDead) continue;
                z.Alert(playerPos);
            }
        }

        #endregion


        // 씬에서 지역 확인 (행동별 색)
        private void OnDrawGizmos()
        {
            Color c;
            switch (behavior)
            {
                case EnemyController.EnemyBehavior.Wander:  c = new Color(0.2f, 0.8f, 0.2f); break;   // 초록: 군사기지
                case EnemyController.EnemyBehavior.Guard:   c = alertAllOnSpot ? new Color(0.7f, 0.2f, 0.9f)   // 보라: 하이브
                                                                               : new Color(0.9f, 0.8f, 0.2f); break;   // 노랑: 도로
                default:                                    c = new Color(0.9f, 0.2f, 0.2f); break;   // 빨강: 전초기지
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(c.r, c.g, c.b, 0.15f);
            Gizmos.DrawCube(Vector3.zero, areaSize);
            Gizmos.color = c;
            Gizmos.DrawWireCube(Vector3.zero, areaSize);
            Gizmos.matrix = Matrix4x4.identity;

            if (behavior == EnemyController.EnemyBehavior.Assault && assaultTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, assaultTarget.position);
            }

            if (activationRange > 0f)
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, activationRange);
            }
        }
    }
}