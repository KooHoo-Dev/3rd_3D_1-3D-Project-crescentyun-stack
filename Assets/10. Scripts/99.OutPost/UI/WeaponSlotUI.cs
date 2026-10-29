using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 무기 슬롯 하나. 값을 받아서 보여주기만 함
    public class WeaponSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private GameObject highlight;
        [SerializeField] private GameObject tooltip;     // WeaponNameTooltip
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private ReloadCooldown reloadCooldown;   // 비워두면 원형 표시 안 함
        // 어떤 총이 들어 있는지 (이름, 아이콘)
        public void SetWeapon(Gun gun)
        {
            if (reloadCooldown != null) reloadCooldown.SetGun(gun);
            
            if (gun == null)   // 빈 슬롯
            {
                icon.enabled = false;
                tooltipText.text = "";
                ammoText.text = "";
                return;
            }

            icon.enabled = true;
            if (gun.Icon != null)
            {
                icon.sprite = gun.Icon;
                icon.color = Color.white;   // 빌더가 칠한 색이 스프라이트에 섞이지 않게
            }
            tooltipText.text = gun.WeaponName;
        }

        public void SetAmmo(int current, int reserve)
        {
            ammoText.text = $"{current}/{reserve}";
        }

        public void SetSelected(bool selected)
        {
            highlight.SetActive(selected);
            tooltip.SetActive(selected);
        }
    }
}