using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어 체력. 구르는 중에는 데미지를 무시함
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private Animator animator;
        
        
        [Header("리스폰관련")]
        [SerializeField] private Transform respawnPoint;   // 비워두면 게임 시작 위치에서 부활
        private Vector3 startPos;
        private Quaternion startRot;
        
        
        private float hp;
        private PlayerController controller;



        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public bool IsDead => hp <= 0f;

        private void Awake()
        {
            hp = maxHp;
            controller = GetComponent<PlayerController>();
            animator = GetComponentInChildren<Animator>();
            startPos = transform.position;
            startRot = transform.rotation;
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (IsDead) return;

            if (controller != null && controller.IsRolling)
            {
                Debug.Log("회피!");
                return;
            }

            hp -= damage;
            Debug.Log($"플레이어 피격! -{damage} (남은 HP: {hp})");

            if (hp <= 0f)
            {
                hp = 0f;
                Die();
            }
        }

        private void Die()
        {
            animator.SetTrigger(DeadHash);
            animator.SetLayerWeight(animator.GetLayerIndex("UpperBody"), 0f);   // 상체 레이어 끄기
            if (controller != null) controller.enabled = false;                 // 조작 막기

            Debug.Log("플레이어 사망");
        }
        
        [ContextMenu("Respawn")]
        public void Respawn()
        {
            if (!IsDead) return;

            hp = maxHp;

            // 위치: 리스폰 지점이 있으면 거기로, 없으면 시작 위치로
            if (respawnPoint != null)
                transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            else
                transform.SetPositionAndRotation(startPos, startRot);

            // Animator를 처음 상태로 (Dead 상태, 레이어 가중치, 파라미터 모두 초기화)
            animator.Rebind();
            animator.Update(0f);

            if (controller != null) controller.enabled = true;

            Debug.Log("리스폰");
        }
        
        
    }
}