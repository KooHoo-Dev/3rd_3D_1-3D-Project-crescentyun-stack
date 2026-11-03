using System;
using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어 / Hotbar / 인벤토리 이벤트를 구독해서 HUD 조각들에 값을 전달
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private HealthBarTrail healthBar;
        [SerializeField] private GaugeBar staminaBar;

        [Tooltip("[0] 1번, [1] 2번, [2] V(근접). 비어 있으면 EquipSlot_1 / EquipSlot_2 / EquipSlot_Melee 를 찾아 자동 연결")]
        [SerializeField] private WeaponSlotUI[] weaponSlots = new WeaponSlotUI[3];

        [Tooltip("3 · 4 · 5 · 6번. 비어 있으면 QuickSlot_3 ~ QuickSlot_6 을 찾아 자동 연결")]
        [SerializeField] private QuickSlotUI[] quickSlots = new QuickSlotUI[4];

        private static readonly string[] WeaponSlotNames = { "EquipSlot_1", "EquipSlot_2", "EquipSlot_Melee" };

        private PlayerController player;
        private PlayerHealth health;
        private PlayerHotbar hotbar;
        private Inventory inventory;

        private readonly Gun[] boundGuns = new Gun[2];
        private readonly Action<int, int>[] ammoHandlers = new Action<int, int>[2];   // 총마다 구독한 함수 (해제할 때 필요)

        private void Start()
        {
            player = PlayerController.Instance;
            if (player == null)
            {
                Debug.LogWarning("[PlayerHUD] PlayerController를 찾지 못했습니다.");
                return;
            }
            health = player.GetComponent<PlayerHealth>();
            FindMissingSlots();

            // HP
            healthBar.Snap(health.Hp, health.MaxHp);
            health.OnHpChanged += HandleHpChanged;

            // 스태미나
            staminaBar.SetValue(player.Stamina, player.MaxStamina);
            player.OnStaminaChanged += HandleStaminaChanged;

            // Hotbar / 인벤토리
            if (InventorySystem.IsInitialized)
            {
                hotbar = InventorySystem.Instance.Hotbar;
                inventory = InventorySystem.Instance.PlayerInventory;
            }
            if (hotbar != null) hotbar.OnSlotChanged += HandleHotbarChanged;
            if (inventory != null) inventory.OnChanged += HandleInventoryChanged;

            // 무기 슬롯
            player.OnGunsChanged += RebindGuns;
            RebindGuns();
            RefreshMelee();
            RefreshQuickSlots();

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
                player.OnGunsChanged -= RebindGuns;
            }

            if (hotbar != null) hotbar.OnSlotChanged -= HandleHotbarChanged;
            if (inventory != null) inventory.OnChanged -= HandleInventoryChanged;

            UnbindGuns();
        }

        // ───────── 무기 (1, 2) ─────────
        private void RebindGuns()
        {
            UnbindGuns();

            for (int i = 0; i < boundGuns.Length; i++)
            {
                WeaponSlotUI slot = GetWeaponSlot(i);
                Gun gun = player.Guns[i];
                boundGuns[i] = gun;
                if (slot == null) continue;

                slot.SetWeapon(gun);
                if (gun == null) continue;   // 빈 슬롯

                slot.SetAmmo(gun.CurrentAmmo, gun.ReserveAmmo);
                ammoHandlers[i] = (current, reserve) => slot.SetAmmo(current, reserve);
                gun.OnAmmoChanged += ammoHandlers[i];
            }

            HandleSlotChanged(player.CurrentSlot);
        }

        private void UnbindGuns()
        {
            for (int i = 0; i < boundGuns.Length; i++)
            {
                if (boundGuns[i] != null && ammoHandlers[i] != null)
                    boundGuns[i].OnAmmoChanged -= ammoHandlers[i];
                boundGuns[i] = null;
                ammoHandlers[i] = null;
            }
        }

        // ───────── 근접무기 (V) / 소모품 (3~6) ─────────
        private void RefreshMelee()
        {
            WeaponSlotUI slot = GetWeaponSlot(PlayerHotbar.MeleeSlot);
            if (slot == null) return;

            ItemStack stack = hotbar != null ? hotbar.Get(PlayerHotbar.MeleeSlot) : null;
            slot.SetItem(stack != null ? stack.Data : null);
        }

        private void RefreshQuickSlots()
        {
            for (int k = 0; k < quickSlots.Length; k++)
            {
                if (quickSlots[k] == null) continue;

                int index = PlayerHotbar.FirstConsumableSlot + k;
                ItemStack stack = hotbar != null ? hotbar.Get(index) : null;
                quickSlots[k].SetItem(stack != null ? stack.Data : null, stack != null ? hotbar.CountOf(index) : 0);
            }
        }

        private void HandleHotbarChanged(int index)
        {
            if (index == PlayerHotbar.MeleeSlot) RefreshMelee();
            else if (PlayerHotbar.IsConsumableSlot(index)) RefreshQuickSlots();
            // 1, 2번 칸은 PlayerController가 총을 다시 만든 뒤 OnGunsChanged로 알려줌
        }

        // 탄약을 줍거나 쓰면 예비 탄약 수가 바뀜 + 소모품 개수 갱신
        private void HandleInventoryChanged()
        {
            for (int i = 0; i < boundGuns.Length; i++)
            {
                WeaponSlotUI slot = GetWeaponSlot(i);
                if (slot != null && boundGuns[i] != null)
                    slot.SetAmmo(boundGuns[i].CurrentAmmo, boundGuns[i].ReserveAmmo);
            }
            RefreshQuickSlots();
        }

        // ───────── 기타 ─────────
        private void HandleHpChanged(float current, float max) => healthBar.SetValue(current, max);

        private void HandleStaminaChanged(float current, float max) => staminaBar.SetValue(current, max);

        private void HandleSlotChanged(int slot)
        {
            for (int i = 0; i < weaponSlots.Length; i++)
                if (weaponSlots[i] != null) weaponSlots[i].SetSelected(i == slot);
        }

        private WeaponSlotUI GetWeaponSlot(int i) => i < weaponSlots.Length ? weaponSlots[i] : null;

        // 인스펙터에서 비어 있는 칸은 MainUIBuilder가 만든 이름으로 찾아서 컴포넌트를 붙인다
        private void FindMissingSlots()
        {
            if (weaponSlots == null || weaponSlots.Length < WeaponSlotNames.Length)
                Array.Resize(ref weaponSlots, WeaponSlotNames.Length);
            for (int i = 0; i < WeaponSlotNames.Length; i++)
                if (weaponSlots[i] == null) weaponSlots[i] = FindOrAdd<WeaponSlotUI>(WeaponSlotNames[i]);

            int quickCount = PlayerHotbar.SlotCount - PlayerHotbar.FirstConsumableSlot;   // 4
            if (quickSlots == null || quickSlots.Length < quickCount)
                Array.Resize(ref quickSlots, quickCount);
            for (int k = 0; k < quickCount; k++)
                if (quickSlots[k] == null) quickSlots[k] = FindOrAdd<QuickSlotUI>($"QuickSlot_{PlayerHotbar.FirstConsumableSlot + k}");
        }

        private static T FindOrAdd<T>(string objectName) where T : Component
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                Debug.LogWarning($"[PlayerHUD] '{objectName}' 을(를) 찾지 못했습니다.");
                return null;
            }
            return go.TryGetComponent(out T comp) ? comp : go.AddComponent<T>();
        }
    }
}
