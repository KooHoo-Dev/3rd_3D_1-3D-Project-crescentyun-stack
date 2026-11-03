using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.OutPost
{
    // 테스트용 치트 키. 에디터와 빌드 모두에서 동작 (출시할 때는 이 컴포넌트를 빼거나 끌 것)
    //  F1 : 무한 스태미나 켜기/끄기
    // 씬의 아무 오브젝트에나 붙임 (예: GameManager)
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField] private Key infiniteStaminaKey = Key.F1;
        [Tooltip("켜져 있는 치트를 화면 우측 상단에 표시")]
        [SerializeField] private bool showLabel = true;

        private bool infiniteStamina;

        private void Update()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null) return;

            // 플레이어가 다시 생성돼도 상태 유지
            if (player.InfiniteStamina != infiniteStamina) player.SetInfiniteStamina(infiniteStamina);

            if (SimpleInput.GetKeyDown(infiniteStaminaKey))
            {
                infiniteStamina = !infiniteStamina;
                player.SetInfiniteStamina(infiniteStamina);
                Debug.Log($"[Cheat] Infinite Stamina {(infiniteStamina ? "ON" : "OFF")}");
            }
        }

        private void OnGUI()
        {
            if (!showLabel || !infiniteStamina) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperRight };
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(Screen.width - 320f, 10f, 300f, 30f), "INFINITE STAMINA (F1)", style);
        }
    }
}
