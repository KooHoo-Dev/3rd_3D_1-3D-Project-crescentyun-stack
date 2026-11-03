using System;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.OutPost
{
    // 상자, 적 시체에 붙이는 루팅 컨테이너
    // - 적에 붙이면 EnemyHealth가 죽은 뒤에만 루팅 가능
    // - 처음 열 때 LootTable로 아이템 생성, 확인 상태는 계속 유지
    public class LootContainer : MonoBehaviour
    {
        [Serializable]
        public class LootEntry
        {
            public ItemStack Stack;
            public bool Searched;

            public InventoryItem Item => Stack != null ? Stack.Data : null;
            public bool IsEmpty => Stack == null || Stack.Data == null || Stack.Count <= 0;
        }

        // 씬에 있는 모든 컨테이너 (콜라이더/레이어 없이 거리로 찾기 위함)
        public static readonly List<LootContainer> All = new List<LootContainer>();

        [SerializeField] private LootTable lootTable;
        [SerializeField, Min(1)] private int capacity = 8;

        private EnemyHealth enemyHealth;
        private List<LootEntry> entries;

        public bool CanLoot => enemyHealth == null || enemyHealth.IsDead;

        public IReadOnlyList<LootEntry> Entries
        {
            get
            {
                if (entries == null) Generate();
                return entries;
            }
        }

        private void Awake()
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        private void OnEnable()  => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Generate()
        {
            entries = new List<LootEntry>();
            if (lootTable == null) return;

            foreach (var stack in lootTable.Roll(capacity))
            {
                // 같은 아이템이 또 나오면 기존 칸에 먼저 합친다
                foreach (var entry in entries)
                {
                    if (entry.Stack.CanStackWith(stack)) entry.Stack.MergeFrom(stack);
                    if (stack.Count <= 0) break;
                }

                if (stack.Count > 0 && entries.Count < capacity)
                    entries.Add(new LootEntry { Stack = stack, Searched = false });
            }
        }
    }
}
