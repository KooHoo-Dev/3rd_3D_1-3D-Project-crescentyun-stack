using System;
using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어의 장착/바로가기 데이터 (7칸)
    //  [0] 1번  총       — 아이템 묶음 자체가 들어옴 (인벤토리에서 빠짐)
    //  [1] 2번  총       — 〃
    //  [2] V    근접무기 — 기본 근접무기로 고정, 뺄 수 없음
    //  [3~6] 3~6번 소모품 — 바로가기 (아이템은 인벤토리에 남고, 개수는 인벤토리에서 센다)
    //
    // UI가 꺼져 있어도 값이 유지되도록 인벤토리 UI 루트(항상 켜짐)에 붙인다.
    // 화면(Hotbar 창, 메인 퀵슬롯)과 PlayerController는 이 데이터의 이벤트를 구독한다.
    public class PlayerHotbar : MonoBehaviour
    {
        public const int SlotCount = 7;
        public const int WeaponSlotA = 0;
        public const int WeaponSlotB = 1;
        public const int MeleeSlot = 2;
        public const int FirstConsumableSlot = 3;

        [Tooltip("V 칸에 고정되는 기본 근접무기 (Type = Melee)")]
        [SerializeField] private InventoryItem defaultMelee;

        private readonly ItemStack[] slots = new ItemStack[SlotCount];
        private Inventory subscribedInventory;

        /// <summary> 칸 하나의 내용이 바뀜 (칸 번호) </summary>
        public event Action<int> OnSlotChanged;

        /// <summary> 1번 ↔ 2번 칸 교환. 총 오브젝트는 다시 만들지 않고 순서만 바꾸면 된다. </summary>
        public event Action OnWeaponsSwapped;

        private Inventory PlayerInventory =>
            InventorySystem.IsInitialized ? InventorySystem.Instance.PlayerInventory : null;

        private void Awake()
        {
            if (defaultMelee != null) slots[MeleeSlot] = new ItemStack(defaultMelee);
            else Debug.LogWarning("[PlayerHotbar] Default Melee가 비어 있습니다. 기본 근접무기 아이템을 넣어 주세요.", this);
        }

        private void Start()
        {
            subscribedInventory = PlayerInventory;
            if (subscribedInventory != null) subscribedInventory.OnChanged += HandleInventoryChanged;
        }

        private void OnDestroy()
        {
            if (subscribedInventory != null) subscribedInventory.OnChanged -= HandleInventoryChanged;
        }

        // ───────── 조회 ─────────
        public ItemStack Get(int index) => IsValid(index) ? slots[index] : null;

        public static bool IsValid(int index) => index >= 0 && index < SlotCount;
        public static bool IsWeaponSlot(int index) => index == WeaponSlotA || index == WeaponSlotB;
        public static bool IsConsumableSlot(int index) => index >= FirstConsumableSlot && index < SlotCount;

        /// <summary> 이 칸에 넣을 수 있는 아이템인지 (V 칸은 고정이라 항상 false) </summary>
        public bool CanPlace(int index, InventoryItem item)
        {
            if (item == null) return false;
            if (IsWeaponSlot(index)) return item.Type == ItemType.Weapon && item.GunPrefab != null;
            if (IsConsumableSlot(index)) return item.Type == ItemType.Consumable;
            return false;
        }

        /// <summary> 소모품 칸이면 인벤토리 안 총 개수, 그 외 칸은 들어 있으면 1 </summary>
        public int CountOf(int index)
        {
            ItemStack stack = Get(index);
            if (stack == null) return 0;
            if (!IsConsumableSlot(index)) return 1;

            var inventory = PlayerInventory;
            return inventory != null ? inventory.CountOf(stack.Data) : 0;
        }

        // ───────── 무기 칸 (1, 2) ─────────
        /// <summary> 무기 칸에 묶음을 넣고, 원래 있던 묶음을 돌려준다 (없으면 null). </summary>
        public ItemStack SetWeapon(int index, ItemStack stack)
        {
            if (!IsWeaponSlot(index)) return stack;

            ItemStack old = slots[index];
            slots[index] = stack;
            OnSlotChanged?.Invoke(index);
            return old;
        }

        public void SwapWeapons()
        {
            (slots[WeaponSlotA], slots[WeaponSlotB]) = (slots[WeaponSlotB], slots[WeaponSlotA]);
            OnWeaponsSwapped?.Invoke();
            OnSlotChanged?.Invoke(WeaponSlotA);
            OnSlotChanged?.Invoke(WeaponSlotB);
        }

        // ───────── 소모품 칸 (3~6) ─────────
        /// <summary> 바로가기 등록/해제 (item == null 이면 해제). 인벤토리는 건드리지 않는다. </summary>
        public void SetShortcut(int index, InventoryItem item)
        {
            if (!IsConsumableSlot(index)) return;

            slots[index] = item != null ? new ItemStack(item) : null;   // 개수는 쓰지 않음
            OnSlotChanged?.Invoke(index);
        }

        public void SwapShortcuts(int a, int b)
        {
            if (!IsConsumableSlot(a) || !IsConsumableSlot(b) || a == b) return;

            (slots[a], slots[b]) = (slots[b], slots[a]);
            OnSlotChanged?.Invoke(a);
            OnSlotChanged?.Invoke(b);
        }

        /// <summary> 소모품을 하나 사용한다. 성공하면 사용한 아이템을 돌려준다 (효과 적용은 호출한 쪽에서). </summary>
        public bool TryConsume(int index, out InventoryItem used)
        {
            used = null;
            if (!IsConsumableSlot(index) || slots[index] == null) return false;

            var inventory = PlayerInventory;
            InventoryItem item = slots[index].Data;
            if (inventory == null || inventory.Remove(item, 1) <= 0) return false;

            used = item;
            return true;   // 0개가 되면 HandleInventoryChanged에서 칸이 비워짐
        }

        // 인벤토리에서 다 쓰거나 옮겨서 0개가 된 소모품 바로가기는 비운다
        private void HandleInventoryChanged()
        {
            for (int i = FirstConsumableSlot; i < SlotCount; i++)
            {
                if (slots[i] == null || CountOf(i) > 0) continue;
                slots[i] = null;
                OnSlotChanged?.Invoke(i);
            }
        }
    }
}
