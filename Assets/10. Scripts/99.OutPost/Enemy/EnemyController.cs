using System;
using UnityEngine;

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
        [SerializeField] private float runSpeed = 9f;
        [SerializeField] private Animator animator;

        [Header("감지")] 
        [SerializeField] private float detectRange = 8f;
        [SerializeField] private float viewAngle = 90f;
        [SerializeField] private float eyeHeight = 1.5f;
        [SerializeField] private LayerMask obstacleMask;
        
        private Transform player;
        
        
        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
           
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
                state = EnemyState.Run;
                Debug.Log("플레이어 발견!");
            }
        }
        
        
        // 적이 플레이어를 발견했을 때 달려오는 상태
        // Ray를 사용해서 플레이어를 감지?
        private void UpdateRun()
        {
            Debug.Log("달리는중");
        }
        
        
        // 플레이어가 보이면 true
        private bool CanSeePlayer()
        {
            if (player == null) return false;

            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;   // 적의 눈
            Vector3 targetPos = player.position + Vector3.up * 1f;          // 플레이어 가슴
            Vector3 toPlayer = targetPos - eyePos;
            float distance = toPlayer.magnitude;

            // 1) 거리: 감지 거리보다 멀면 못 봄
            if (distance > detectRange) return false;

            // 2) 시야각: 높이 차이는 무시하고 수평 각도만 비교
            Vector3 flatDir = toPlayer;
            flatDir.y = 0f;
            if (Vector3.Angle(transform.forward, flatDir) > viewAngle * 0.5f) return false;

            // 3) 벽: 플레이어까지 가는 길에 벽이 맞으면 가려진 것
            if (Physics.Raycast(eyePos, toPlayer.normalized, distance, obstacleMask)) return false;
            
            return true;
        }
        
        
        
        
    }
}