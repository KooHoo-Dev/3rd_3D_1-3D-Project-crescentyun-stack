using UnityEngine;

namespace Study3D.OutPost
{
    // Bullet
    // 매 프레임 이동할 거리만큼 앞으로 Raycast를 먼저 쏘고,
    // 맞은 게 없으면 그만큼 이동합니다. (빨라도 벽을 뚫지 않음)
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float speed = 40f;      // 초당 이동 거리 (m)
        [SerializeField] private float lifeTime = 2f;    // 아무것도 안 맞으면 이 시간 뒤에 사라짐
        [SerializeField] private LayerMask hitMask = ~0; // 맞을 수 있는 레이어 (~0 = 전부)

        private float timer;
        private float damage;
        
        

        private void Update()
        {
            float step = speed * Time.deltaTime;   // 이번 프레임에 이동할 거리

            // 이동하기 전에, 이동할 구간에 뭐가 있는지 먼저 검사
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                    step, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit);
                return;
            }

            transform.position += transform.forward * step;

            timer += Time.deltaTime;
            if (timer >= lifeTime) Destroy(gameObject);
        }

        private void OnHit(RaycastHit hit)
        {
            Debug.Log($"명중: {hit.collider.name}");
            // 콜라이더가 자식(팔, 머리 등)에 있어도 찾을 수 있게 부모까지 검색
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage, hit.point, hit.normal);
            Destroy(gameObject);
        }
        
        // 생성 직후 Gun이 호출합니다.
        public void Init(float damage)
        {
            this.damage = damage;
        }
        
    }
}