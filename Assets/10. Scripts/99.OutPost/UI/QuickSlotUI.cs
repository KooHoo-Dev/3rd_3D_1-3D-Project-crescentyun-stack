using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 메인 화면 퀵슬롯 하나 (3~6). 값을 받아서 보여주기만 함
    // 인스펙터 칸이 비어 있으면 MainUIBuilder가 만든 자식 이름(Icon / ItemNameText / CountText)으로 찾는다.
    public class QuickSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;

        private void Awake()
        {
            if (icon == null) icon = FindChild<Image>("Icon");
            if (nameText == null) nameText = FindChild<TMP_Text>("ItemNameText");
            if (countText == null) countText = FindChild<TMP_Text>("CountText");
        }

        /// <summary> item이 null이거나 count가 0이면 빈 칸 </summary>
        public void SetItem(InventoryItem item, int count)
        {
            bool has = item != null && count > 0;

            if (icon != null)
            {
                icon.gameObject.SetActive(has);
                if (has && item.Icon != null)
                {
                    icon.sprite = item.Icon;
                    icon.color = Color.white;
                }
            }

            if (nameText != null)
            {
                nameText.gameObject.SetActive(has);
                nameText.text = has ? item.Name : "";
            }

            if (countText != null)
            {
                countText.gameObject.SetActive(has);
                countText.text = has ? $"x{count}" : "";
            }
        }

        private T FindChild<T>(string path) where T : Component
        {
            Transform t = transform.Find(path);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
