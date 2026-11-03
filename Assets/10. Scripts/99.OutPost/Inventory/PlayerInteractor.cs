using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.OutPost
{
    // 플레이어에 붙임: 주변 루팅 컨테이너 감지 + F키로 열기/닫기
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float range = 2f;
        [SerializeField] private GameObject promptUI;   // "F 루팅" 안내 (선택)

        // 플레이어가 PlayerSpawner로 프리팹에서 생성되면 씬 UI(InteractPrompt) 참조가 비어 있다.
        // (프리팹은 씬 오브젝트를 참조할 수 없음) → 비어 있으면 이름으로 찾아 연결한다.
        private const string PromptName = "InteractPrompt";

        private void Start()
        {
            if (promptUI == null) promptUI = FindSceneObjectIncludingInactive(PromptName);
            if (promptUI == null)
                Debug.LogWarning($"[{nameof(PlayerInteractor)}] '{PromptName}' 를 찾지 못해 [F] 루팅 안내가 표시되지 않습니다.", this);
        }

        // InteractPrompt는 평소 꺼져 있어서 GameObject.Find로는 못 찾는다 → 꺼진 오브젝트까지 검색
        private static GameObject FindSceneObjectIncludingInactive(string objectName)
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<RectTransform>(true);
#endif
            foreach (var rt in all)
                if (rt.name == objectName) return rt.gameObject;
            return null;
        }

        private void Update()
        {
            var sys = InventorySystem.Instance;

            bool fDown   = SimpleInput.GetKeyDown(Key.F);
            bool tabDown = SimpleInput.GetKeyDown(Key.Tab);
            bool escDown = SimpleInput.GetKeyDown(Key.Escape);

            // ── 루팅 중: F / Tab / ESC 또는 멀어지면 닫기
            if (sys.IsOpen && sys.CurrentContainer != null)
            {
                SetPrompt(false);
                if (fDown || tabDown || escDown || !InRange(sys.CurrentContainer)) sys.Close();
                return;
            }

            LootContainer nearest = FindNearest();

            // ── Tab으로 내 인벤토리만 열린 상태
            if (sys.IsOpen)
            {
                SetPrompt(false);
                if (tabDown || escDown) sys.Close();
                else if (fDown && nearest != null) sys.Open(nearest);   // 열린 채로 F 누르면 바로 루팅
                return;
            }

            // ── 닫힌 상태
            SetPrompt(nearest != null);

            if (fDown && nearest != null) sys.Open(nearest);
            else if (tabDown) sys.OpenPlayerOnly();
        }

        private LootContainer FindNearest()
        {
            LootContainer best = null;
            float bestSqr = range * range;

            foreach (var c in LootContainer.All)
            {
                if (!c.CanLoot) continue;
                float sqr = (c.transform.position - transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = c;
                }
            }
            return best;
        }

        private bool InRange(LootContainer c)
        {
            if (c == null) return false;
            // 여는 거리보다 살짝 여유를 줌
            float r = range + 0.5f;
            return (c.transform.position - transform.position).sqrMagnitude <= r * r;
        }

        private void SetPrompt(bool on)
        {
            if (promptUI != null && promptUI.activeSelf != on) promptUI.SetActive(on);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}