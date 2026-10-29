using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 재장전 중 슬롯 아이콘 위에 원형 덮개를 그림.
    // 재장전 상태는 Gun이 가지고 있고, 여기서는 매 프레임 읽어서 그리기만 함
    // (그래서 달리기/구르기/교체로 재장전이 취소돼도 따로 처리할 필요 없음)
    public class ReloadCooldown : MonoBehaviour
    {
        [SerializeField] private Image cooldownMask;   // Image Type: Filled · Radial 360 · Origin Top

        private Gun gun;

        // WeaponSlotUI.SetWeapon에서 호출
        public void SetGun(Gun target)
        {
            gun = target;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            bool reloading = gun != null && gun.IsReloading && gun.ReloadTime > 0f;

            cooldownMask.enabled = reloading;
            if (reloading)
                cooldownMask.fillAmount = gun.ReloadRemain / gun.ReloadTime;   // 1 → 0으로 줄어들며 회전
        }
    }
}