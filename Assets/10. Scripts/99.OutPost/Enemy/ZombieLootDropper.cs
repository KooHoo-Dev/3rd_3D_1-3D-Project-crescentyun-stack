using UnityEngine;

namespace Study3D.OutPost
{
    // 좀비1 프리팹에 붙임: 죽으면 그 자리에 루팅 박스를 100% 생성
    // - 기지 공격 좀비(좀비2)에는 붙이지 않는다 → 박스 드랍 없음
    // - 박스는 좀비 자식이 아니라 따로 생성 (시체는 corpseLifetime 뒤 사라지지만 박스는 60초 유지)
    // - 바닥으로 Raycast해서 높이·기울기를 맞춘다
    [RequireComponent(typeof(EnemyHealth))]
    public class ZombieLootDropper : MonoBehaviour
    {
        [Header("박스")]
        [Tooltip("LootContainer(좀비 루팅 테이블) + ZombieLootBox가 붙은 Synty 상자 프리팹")]
        [SerializeField] private ZombieLootBox boxPrefab;

        [Header("바닥 맞추기")]
        [SerializeField] private LayerMask groundMask = ~0;   // 바닥 레이어만 켜 두면 가장 정확
        [SerializeField] private float rayStartHeight = 2f;   // 좀비 위치에서 이만큼 위에서 아래로 쏨
        [SerializeField] private float rayDistance = 6f;
        [SerializeField] private bool alignToGround = true;   // 경사면이면 바닥 기울기에 맞춤

        private static readonly RaycastHit[] hits = new RaycastHit[8];

        private EnemyHealth health;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()  => health.OnDeath += DropBox;
        private void OnDisable() => health.OnDeath -= DropBox;

        private void DropBox()
        {
            if (boxPrefab == null)
            {
                Debug.LogWarning($"{name}: ZombieLootDropper에 boxPrefab이 비어 있습니다.");
                return;
            }

            Vector3 pos = transform.position;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);   // 매번 다른 방향

            if (TryFindGround(pos, out RaycastHit ground))
            {
                pos = ground.point;
                if (alignToGround) rot = Quaternion.FromToRotation(Vector3.up, ground.normal) * rot;
            }

            Instantiate(boxPrefab, pos, rot);
        }

        // 좀비 자기 콜라이더는 건너뛰고 가장 가까운 바닥을 찾는다
        // (OnDeath 순서에 따라 몸 콜라이더가 아직 켜져 있을 수 있음)
        private bool TryFindGround(Vector3 pos, out RaycastHit ground)
        {
            ground = default;
            Vector3 origin = pos + Vector3.up * rayStartHeight;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits,
                rayStartHeight + rayDistance, groundMask, QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance < best)
                {
                    best = hits[i].distance;
                    ground = hits[i];
                    found = true;
                }
            }
            return found;
        }
    }
}
