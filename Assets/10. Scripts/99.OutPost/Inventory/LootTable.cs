using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Study3D.OutPost
{
    // 상자/시체에서 나올 아이템 목록과 확률(가중치)
    [CreateAssetMenu(menuName = "OutPost/Loot Table", fileName = "LootTable_")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public InventoryItem Item;
            [Min(0f)] public float Weight = 1f;   // 클수록 잘 나옴

            [Tooltip("한 번 뽑힐 때 나오는 개수 (탄약은 크게, 예: 10~30)")]
            [Min(1)] public int MinAmount = 1;
            [Min(1)] public int MaxAmount = 1;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField, Min(0)] private int minCount = 1;
        [SerializeField, Min(0)] private int maxCount = 4;

        /// <summary> 뽑힌 아이템 묶음 목록. 개수는 각 아이템의 MaxStack을 넘지 않는다. </summary>
        public List<ItemStack> Roll(int capacity)
        {
            var result = new List<ItemStack>();

            float total = 0f;
            foreach (var e in entries)
                if (e.Item != null) total += e.Weight;
            if (total <= 0f) return result;

            int count = Mathf.Min(Random.Range(minCount, maxCount + 1), capacity);
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

                int amount = Random.Range(pick.MinAmount, Mathf.Max(pick.MinAmount, pick.MaxAmount) + 1);
                result.Add(new ItemStack(pick.Item, amount));
            }
            return result;
        }
    }
}
