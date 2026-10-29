using System;
using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어 이벤트를 구독해서 HUD 조각들에 값을 전달
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private HealthBarTrail healthBar;
        [SerializeField] private GaugeBar staminaBar;
        [SerializeField] private WeaponSlotUI[] weaponSlots = new WeaponSlotUI[2];   // [0] 1번, [1] 2번

        private PlayerController player;
        private PlayerHealth health;
        private Gun[] guns;
        private Action<int, int>[] ammoHandlers;   // 총마다 구독한 함수 (해제할 때 필요)

        private void Start()
        {
            player = PlayerController.Instance;
            if (player == null)
            {
                Debug.LogWarning("[PlayerHUD] PlayerController를 찾지 못했습니다.");
                return;
            }
            health = player.GetComponent<PlayerHealth>();
            guns = player.Guns;

            // HP
            healthBar.Snap(health.Hp, health.MaxHp);
            health.OnHpChanged += HandleHpChanged;

            // 스태미나
            staminaBar.SetValue(player.Stamina, player.MaxStamina);
            player.OnStaminaChanged += HandleStaminaChanged;

            // 무기 슬롯
            ammoHandlers = new Action<int, int>[guns.Length];
            int count = Mathf.Min(guns.Length, weaponSlots.Length);
            for (int i = 0; i < count; i++)
            {
                Gun gun = guns[i];
                WeaponSlotUI slot = weaponSlots[i];

                slot.SetWeapon(gun);
                if (gun == null) continue;   // 빈 슬롯

                slot.SetAmmo(gun.CurrentAmmo, gun.ReserveAmmo);
                ammoHandlers[i] = (current, reserve) => slot.SetAmmo(current, reserve);
                gun.OnAmmoChanged += ammoHandlers[i];
            }

            HandleSlotChanged(player.CurrentSlot);
            player.OnSlotChanged += HandleSlotChanged;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnHpChanged -= HandleHpChanged;

            if (player != null)
            {
                player.OnStaminaChanged -= HandleStaminaChanged;
                player.OnSlotChanged -= HandleSlotChanged;
            }

            if (guns == null || ammoHandlers == null) return;
            for (int i = 0; i < guns.Length; i++)
                if (guns[i] != null && ammoHandlers[i] != null)
                    guns[i].OnAmmoChanged -= ammoHandlers[i];
        }

        private void HandleHpChanged(float current, float max) => healthBar.SetValue(current, max);

        private void HandleStaminaChanged(float current, float max) => staminaBar.SetValue(current, max);

        private void HandleSlotChanged(int slot)
        {
            for (int i = 0; i < weaponSlots.Length; i++)
                weaponSlots[i].SetSelected(i == slot);
        }
    }
}