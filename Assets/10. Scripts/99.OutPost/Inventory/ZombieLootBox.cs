using UnityEngine;

namespace Study3D.OutPost
{
    // 좀비 루팅 박스 프리팹(Synty 상자)에 붙임
    // - 아이템은 같은 오브젝트의 LootContainer가 좀비 루팅 테이블로 처음 열 때 한 번 생성 → 다시 열어도 같은 결과
    // - 아무것도 안 나와도 박스는 그대로 생성 (빈 박스)
    // - 생성 후 lifetime초 뒤 사라짐. 그때 이 박스를 열고 있으면 창을 먼저 닫는다
    [RequireComponent(typeof(LootContainer))]
    public class ZombieLootBox : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float lifetime = 60f;

        private LootContainer container;

        private void Awake()
        {
            container = GetComponent<LootContainer>();
        }

        private void Start()
        {
            Invoke(nameof(Despawn), lifetime);
        }

        private void Despawn()
        {
            var sys = InventorySystem.Instance;
            if (sys != null && sys.IsOpen && sys.CurrentContainer == container)
                sys.Close();   // 열린 루팅 창이 사라진 박스를 계속 보여주지 않도록

            Destroy(gameObject);
        }
    }
}
