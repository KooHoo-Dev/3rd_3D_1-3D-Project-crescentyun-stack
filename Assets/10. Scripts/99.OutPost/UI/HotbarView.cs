using System;
using UnityEngine;

namespace Study3D.OutPost
{
    // 인벤토리 창 안의 Hotbar(7칸)를 PlayerHotbar 데이터에 맞춰 그린다.
    // 창이 켜져 있을 때만 구독하므로, 창이 꺼져 있어도 데이터는 PlayerHotbar에 그대로 남는다.
    public class HotbarView : MonoBehaviour
    {
        [Tooltip("1 · 2 · V · 3 · 4 · 5 · 6 순서")]
        [SerializeField] private InventorySlot[] slots = new InventorySlot[PlayerHotbar.SlotCount];

        private PlayerHotbar hotbar;
        private Inventory inventory;

        public int IndexOf(InventorySlot slot) => Array.IndexOf(slots, slot);

        private void OnEnable()
        {
            if (!InventorySystem.IsInitialized) return;

            hotbar = InventorySystem.Instance.Hotbar;
            inventory = InventorySystem.Instance.PlayerInventory;

            if (hotbar != null) hotbar.OnSlotChanged += HandleSlotChanged;
            if (inventory != null) inventory.OnChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (hotbar != null) hotbar.OnSlotChanged -= HandleSlotChanged;
            if (inventory != null) inventory.OnChanged -= RefreshAll;
        }

        private void HandleSlotChanged(int index) => RefreshAll();

        private void RefreshAll()
        {
            if (hotbar == null) return;

            for (int i = 0; i < slots.Length && i < PlayerHotbar.SlotCount; i++)
            {
                if (slots[i] == null) continue;

                ItemStack stack = hotbar.Get(i);
                if (stack == null)
                {
                    slots[i].Clear(false);
                }
                else if (PlayerHotbar.IsConsumableSlot(i))
                {
                    // 바로가기: 화면용 묶음을 새로 만들어 인벤토리 안 총 개수를 표시
                    slots[i].SetStack(new ItemStack(stack.Data) { Count = hotbar.CountOf(i) }, true, false);
                }
                else
                {
                    slots[i].SetStack(stack, true, false);
                }
            }
        }
    }
}
