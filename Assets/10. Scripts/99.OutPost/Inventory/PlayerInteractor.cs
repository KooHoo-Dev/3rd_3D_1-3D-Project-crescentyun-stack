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
