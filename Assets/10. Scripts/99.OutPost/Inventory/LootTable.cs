using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Study3D.OutPost
{
    // 상자/시체에서 나올 아이템 목록과 확률(가중치)
    // - guaranteed : 항상 나오는 아이템 (수량만 랜덤)
    // - entries    : 가중치로 뽑는 랜덤 아이템 (minCount~maxCount 번 뽑음)
    // 확정 아이템만 나오게 하려면 entries를 비우거나 minCount/maxCount를 0으로 둔다.
    [CreateAssetMenu(menuName = "OutPost/Loot Table", fileName = "LootTable_")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public InventoryItem Item;
            [Tooltip("랜덤 목록에서만 사용 (확정 목록에서는 무시)")]
            [Min(0f)] public float Weight = 1f;   // 클수록 잘 나옴

            [Tooltip("나오는 개수 범위 (무기는 항상 1개로 고정됨)")]
            [Min(1)] public int MinAmount = 1;
            [Min(1)] public int MaxAmount = 1;

            public int RollAmount()
            {
                return Random.Range(MinAmount, Mathf.Max(MinAmount, MaxAmount) + 1);
            }
        }

        [Header("확정 아이템 (항상 나옴, 수량만 랜덤)")]
        [SerializeField] private List<Entry> guaranteed = new List<Entry>();

        [Header("랜덤 아이템 (가중치로 뽑기)")]
        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField, Min(0)] private int minCount = 1;
        [SerializeField, Min(0)] private int maxCount = 4;

        /// <summary> 뽑힌 아이템 묶음 목록. 개수는 각 아이템의 MaxStack을 넘지 않는다. </summary>
        public List<ItemStack> Roll(int capacity)
        {
            var result = new List<ItemStack>();

            // 1) 확정 아이템 — 목록에 있는 아이템은 모두 넣는다
            foreach (var e in guaranteed)
            {
                if (e.Item == null) continue;
                AddSplit(result, e.Item, e.RollAmount(), capacity);
            }

            // 2) 랜덤 아이템 — 남은 칸 수만큼만
            float total = 0f;
            foreach (var e in entries)
                if (e.Item != null) total += e.Weight;
            if (total <= 0f) return result;

            int count = Mathf.Min(Random.Range(minCount, maxCount + 1), capacity - result.Count);
            for (int i = 0; i < count; i++)
            {
                float r = Random.value * total;
                Entry pick = null;
                foreach (var e in entries)
                {
                    if (e.Item == null) continue;
                    pick = e;
                    r -= e.Weight;
                    if (r <= 0f) break;
                }
                if (pick == null) continue;

                AddSplit(result, pick.Item, pick.RollAmount(), capacity);
            }
            return result;
        }

        // 수량이 MaxStack을 넘으면 여러 묶음으로 나눠 담는다 (무기는 MaxStack 1 → 항상 1개)
        private static void AddSplit(List<ItemStack> result, InventoryItem item, int amount, int capacity)
        {
            if (!item.IsStackable) amount = 1;

            while (amount > 0 && result.Count < capacity)
            {
                int take = Mathf.Min(amount, item.MaxStackSize);
                result.Add(new ItemStack(item, take));
                amount -= take;
            }
        }
    }
}
