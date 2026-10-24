using System;
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
            Run
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

        private Vector3 lastSeenPos;   // 마지막으로 플레이어를 본 위치
        private float lostTimer;       // 시야에서 사라진 뒤 지난 시간
        
        private Transform player;
        
        
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        
        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            
            if (PlayerController.Instance != null)
                player = PlayerController.Instance.transform;
            else
                Debug.LogWarning("PlayerController.Instance가 없음!");
        }

        private void Update()
        {
            switch (state)
            {
                case EnemyState.Stay: UpdateStay(); break;
                case EnemyState.Walk: UpdateWalk(); break;
                case EnemyState.Run:  UpdateRun();  break;
            }

            UpdateAnimation();
        }

        // 적이 플레이어를 발견하지 못하면 그냥 걸어다니는 순찰 상태
        private void UpdateWalk()
        {
            
        }
        
        // 적이 플레이어를 발견하지 못하고 그냥 가만히 대기하고 있는 상태
        private void UpdateStay()
        {
            if (CanSeePlayer())
            {
                lastSeenPos = player.position;
                lostTimer = 0f;
                state = EnemyState.Run;
            }
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
            }
            else
            {
                lostTimer += Time.deltaTime;
            }

            agent.speed = runSpeed;
            agent.SetDestination(lastSeenPos);

// 너무 오래 못 찾으면 포기
            if (lostTimer >= loseSightTime)
            {
                agent.ResetPath();   // 이동 멈춤
                state = EnemyState.Stay;
            }
        }
        
        
        // 플레이어가 보이면 true
        private bool CanSeePlayer(bool checkAngle = true)
        {
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
        }
        
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
        
        
        
    }
}