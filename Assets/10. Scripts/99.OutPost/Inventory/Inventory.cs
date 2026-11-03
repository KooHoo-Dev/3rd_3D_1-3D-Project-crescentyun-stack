using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Study3D.OutPost
{
    // 인벤토리의 용도를 표시하는 enum
    public enum InventoryType
    {
        None,       // 설정되지 않은 상태 (시체, 박스 등 루팅 인벤토리 포함)
        Player,     // 유저 소유의 인벤토리
    }

    public class Inventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public InventoryType Type = InventoryType.None;
        [field: SerializeField] public int SizeRow { get; private set; } = 8;
        [field: SerializeField] public int SizeColumn { get; private set; } = 5;

        public InventorySlot[,] SlotGrid { get; private set; }

        /// <summary> 칸 하나라도 내용이 바뀌면 호출 (탄약 수, 퀵슬롯 개수 갱신용) </summary>
        public event Action OnChanged;

        // 자식 순서대로 정렬된 슬롯 목록 (루팅 인덱스와 1:1 대응)
        private List<InventorySlot> slots;
        public IReadOnlyList<InventorySlot> Slots
        {
            get { Init(); return slots; }
        }

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            InventorySystem.Instance.Register(this);
        }

        // 패널이 꺼진 상태에서도 호출될 수 있도록 지연 초기화
        public void Init()
        {
            if (slots != null) return;

            slots = new List<InventorySlot>(GetComponentsInChildren<InventorySlot>(true));
            SlotGrid = new InventorySlot[SizeRow, SizeColumn];

            int max = Mathf.Min(slots.Count, SizeRow * SizeColumn);
            for (int i = 0; i < max; ++i)
                SlotGrid[i / SizeColumn, i % SizeColumn] = slots[i];

            foreach (var slot in slots)
                slot.OnChanged += HandleSlotChanged;
        }

        private void HandleSlotChanged(InventorySlot slot) => OnChanged?.Invoke();

        public bool IsIn(InventorySlot slot)
        {
            return slot != null && Slots.Contains(slot);
        }

        public int IndexOf(InventorySlot slot)
        {
            return Slots is List<InventorySlot> list ? list.IndexOf(slot) : -1;
        }

        /// <summary>
        /// 같은 아이템 칸에 먼저 합치고, 남으면 첫 번째 빈 칸에 넣는다.
        /// 전부 들어가면 true. 일부만 들어가면 false이고 남은 개수는 stack.Count에 남는다.
        /// </summary>
        public bool TryAdd(ItemStack stack)
        {
            if (stack == null || stack.Data == null || stack.Count <= 0) return false;

            // 1. 같은 아이템 칸에 합치기
            if (stack.Data.IsStackable)
            {
                foreach (var slot in Slots)
                {
                    if (slot.IsEmpty || slot.Stack.IsFull || !slot.Stack.CanStackWith(stack)) continue;

                    slot.Stack.MergeFrom(stack);
                    slot.Refresh();
                    if (stack.Count <= 0) return true;
                }
            }

            // 2. 남은 것은 빈 칸에
            foreach (var slot in Slots)
            {
                if (!slot.IsEmpty) continue;
                slot.SetStack(stack);
                return true;
            }
            return false;
        }

        /// <summary> 조건에 맞는 아이템의 총 개수 </summary>
        public int CountWhere(Predicate<InventoryItem> match)
        {
            int total = 0;
            foreach (var slot in Slots)
                if (!slot.IsEmpty && match(slot.Item)) total += slot.Stack.Count;
            return total;
        }

        public int CountOf(InventoryItem item) => CountWhere(data => data == item);

        /// <summary> 조건에 맞는 아이템을 amount개까지 빼낸다 (뒤 칸부터). 실제로 뺀 개수를 돌려준다. </summary>
        public int RemoveWhere(Predicate<InventoryItem> match, int amount)
        {
            int removed = 0;
            for (int i = Slots.Count - 1; i >= 0 && removed < amount; i--)
            {
                var slot = Slots[i];
                if (slot.IsEmpty || !match(slot.Item)) continue;

                int take = Mathf.Min(amount - removed, slot.Stack.Count);
                slot.Stack.Count -= take;
                removed += take;
                slot.Refresh();   // 0개가 되면 칸이 비워짐
            }
            return removed;
        }

        public int Remove(InventoryItem item, int amount) => RemoveWhere(data => data == item, amount);
    }
}
