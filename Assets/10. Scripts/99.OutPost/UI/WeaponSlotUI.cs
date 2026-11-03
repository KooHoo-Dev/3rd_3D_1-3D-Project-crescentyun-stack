using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 무기 슬롯 하나 (1 · 2 · V). 값을 받아서 보여주기만 함
    // 인스펙터 칸이 비어 있으면 MainUIBuilder가 만든 자식 이름으로 자동으로 찾는다.
    public class WeaponSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private GameObject highlight;
        [SerializeField] private GameObject tooltip;     // WeaponNameTooltip
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private TMP_Text ammoText;      // 근접무기 칸은 없어도 됨
        [SerializeField] private ReloadCooldown reloadCooldown;   // 비워두면 원형 표시 안 함

        private void Awake()
        {
            if (icon == null) icon = FindChild<Image>("Icon");
            if (highlight == null) highlight = FindChildObject("Highlight");
            if (tooltip == null) tooltip = FindChildObject("WeaponNameTooltip");
            if (tooltipText == null) tooltipText = FindChild<TMP_Text>("WeaponNameTooltip/TooltipText");
            if (ammoText == null) ammoText = FindChild<TMP_Text>("AmmoText");
        }

        // 어떤 총이 들어 있는지 (이름, 아이콘)
        public void SetWeapon(Gun gun)
        {
            if (reloadCooldown != null) reloadCooldown.SetGun(gun);

            if (gun == null)   // 빈 슬롯
            {
                SetEmpty();
                return;
            }

            ShowIcon(gun.Icon);
            SetText(tooltipText, gun.WeaponName);
        }

        // 근접무기처럼 Gun이 없는 아이템 (이름, 아이콘만)
        public void SetItem(InventoryItem item)
        {
            if (reloadCooldown != null) reloadCooldown.SetGun(null);

            if (item == null)
            {
                SetEmpty();
                return;
            }

            ShowIcon(item.Icon);
            SetText(tooltipText, item.Name);
            SetText(ammoText, "");
        }

        public void SetAmmo(int current, int reserve)
        {
            SetText(ammoText, $"{current}/{reserve}");
        }

        public void SetSelected(bool selected)
        {
            if (highlight != null) highlight.SetActive(selected);
            if (tooltip != null) tooltip.SetActive(selected);
        }

        private void SetEmpty()
        {
            if (icon != null) icon.enabled = false;
            SetText(tooltipText, "");
            SetText(ammoText, "");
        }

        private void ShowIcon(Sprite sprite)
        {
            if (icon == null) return;
            icon.enabled = true;
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;   // 빌더가 칠한 색이 스프라이트에 섞이지 않게
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        private T FindChild<T>(string path) where T : Component
        {
            Transform t = transform.Find(path);
            return t != null ? t.GetComponent<T>() : null;
        }

        private GameObject FindChildObject(string path)
        {
            Transform t = transform.Find(path);
            return t != null ? t.gameObject : null;
        }
    }
}
