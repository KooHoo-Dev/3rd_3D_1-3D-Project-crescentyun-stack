using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 게이지 하나(Fill 이미지 + 숫자 텍스트). 값을 받아서 보여주기만 함
    public class GaugeBar : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text valueText;   // 비워두면 숫자 표시 안 함 (스태미나바)

        public void SetValue(float current, float max)
        {
            fill.fillAmount = max > 0f ? current / max : 0f;

            if (valueText != null)
                valueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // 에디터 테스트용: 컴포넌트 ⋮ 메뉴에서 실행
        [ContextMenu("Test 30%")]
        private void Test30() => SetValue(30f, 100f);
    }
}