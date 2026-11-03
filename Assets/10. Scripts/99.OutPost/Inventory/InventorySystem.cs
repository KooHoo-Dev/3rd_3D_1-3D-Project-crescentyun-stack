using System.Collections;
using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace Study3D.OutPost
{
    // 인벤토리 전체 관리
    // - F로 루팅 열기/닫기 (PlayerInteractor가 호출)
    // - 루팅 아이템을 위에서부터 하나씩 탐색(회색 → 확인)
    // - 확인된 루팅 아이템 클릭 시 내 인벤토리로 이동 (같은 아이템은 자동으로 합침)
    // - 탄약 개수 세기 / 소모 (Gun이 재장전할 때 호출)
    public class InventorySystem : SingletonBase<InventorySystem>
    {
        [Header("UI")]
        [SerializeField] private GameObject inventoryUI;     // 인벤토리 창 전체를 묶은 부모
        [SerializeField] private Inventory playerInventory;
        [SerializeField] private Inventory lootInventory;
        [SerializeField] private PlayerHotbar hotbar;        // 1 · 2 · V · 3~6 장착/바로가기 데이터

        private List<Inventory> Inventories { get; set; } = new List<Inventory>();
        private Coroutine searchRoutine;

        public bool IsOpen { get; private set; }
        public LootContainer CurrentContainer { get; private set; }

        public Inventory PlayerInventory => playerInventory;
        public Inventory LootInventory => lootInventory;
        public PlayerHotbar Hotbar
        {
            get
            {
                if (hotbar == null) hotbar = GetComponent<PlayerHotbar>();
                return hotbar;
            }
        }

        private void Start()
        {
            if (inventoryUI != null) inventoryUI.SetActive(false);
        }

        // ───────── 열기 / 닫기 ─────────
        public void Open(LootContainer container)
        {
            if (container == null) return;

            if (searchRoutine != null) StopCoroutine(searchRoutine);   // Tab으로 열려 있던 상태에서 바로 루팅할 때

            CurrentContainer = container;
            IsOpen = true;

            inventoryUI.SetActive(true);
            lootInventory.gameObject.SetActive(true);
            playerInventory.Init();
            lootInventory.Init();

            RefreshLoot();
            searchRoutine = StartCoroutine(SearchRoutine());
        }

        // Tab: 루팅 창 없이 내 인벤토리만 열기
        public void OpenPlayerOnly()
        {
            CurrentContainer = null;
            IsOpen = true;

            inventoryUI.SetActive(true);
            playerInventory.Init();
            lootInventory.gameObject.SetActive(false);
        }

        public void Close()
        {
            if (searchRoutine != null) StopCoroutine(searchRoutine);
            searchRoutine = null;

            foreach (var slot in lootInventory.Slots) slot.SetSearchProgress(0f);

            IsOpen = false;
            CurrentContainer = null;
            inventoryUI.SetActive(false);
        }

        private void RefreshLoot()
        {
            var slots = lootInventory.Slots;
            var entries = CurrentContainer.Entries;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i < entries.Count && !entries[i].IsEmpty) slots[i].SetStack(entries[i].Stack, entries[i].Searched);
                else slots[i].Clear();
            }
        }

        // ───────── 탐색 (하나씩 천천히) ─────────
        private IEnumerator SearchRoutine()
        {
            var slots = lootInventory.Slots;
            var entries = CurrentContainer.Entries;

            for (int i = 0; i < entries.Count && i < slots.Count; i++)
            {
                var entry = entries[i];
                if (entry.IsEmpty || entry.Searched) continue;

                float duration = Mathf.Max(0.01f, entry.Item.SearchTime);
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    slots[i].SetSearchProgress(t / duration);
                    yield return null;
                }

                entry.Searched = true;                 // 확인 상태는 컨테이너에 저장 → 다시 열어도 유지
                slots[i].SetStack(entry.Stack, true);
            }
            searchRoutine = null;
        }

        // ───────── 클릭 이동 (루팅 → 내 인벤토리) ─────────
        public void OnSlotClicked(InventorySlot slot)
        {
            if (!IsOpen || CurrentContainer == null) return;

            int index = lootInventory.IndexOf(slot);
            if (index < 0) return;                      // 내 인벤토리 클릭은 무시 (반대 방향 없음)

            var entries = CurrentContainer.Entries;
            if (index >= entries.Count) return;

            var entry = entries[index];
            if (entry.IsEmpty || !entry.Searched) return;   // 미확인은 못 가져감

            // 같은 아이템 칸에 먼저 합치고 남으면 빈 칸에. 다 못 넣으면 남은 개수는 루팅 칸에 그대로
            if (playerInventory.TryAdd(entry.Stack))
            {
                entry.Stack = null;
                slot.Clear();
            }
            else
            {
                slot.Refresh();
                Debug.Log("인벤토리가 가득 찼습니다.");
            }
        }

        // ───────── 탄약 (Gun에서 사용) ─────────
        public int CountAmmo(AmmoType type)
        {
            if (playerInventory == null || type == AmmoType.None) return 0;
            return playerInventory.CountWhere(item => item.Type == ItemType.Ammo && item.AmmoType == type);
        }

        /// <summary> 탄약을 amount발까지 빼낸다. 실제로 뺀 수를 돌려준다. </summary>
        public int ConsumeAmmo(AmmoType type, int amount)
        {
            if (playerInventory == null || type == AmmoType.None || amount <= 0) return 0;
            return playerInventory.RemoveWhere(item => item.Type == ItemType.Ammo && item.AmmoType == type, amount);
        }

        // ───────── 기존 기능 유지 ─────────
        public void Register(Inventory inventory)
        {
            if (!Inventories.Contains(inventory)) Inventories.Add(inventory);
        }

        public void Remove(Inventory inventory)
        {
            Inventories.Remove(inventory);
        }

        public Inventory GetInventoryOrNull(string targetName)
        {
            for (int i = 0; i < Inventories.Count; i++)
                if (Inventories[i].Name.Equals(targetName)) return Inventories[i];
            return null;
        }

        public Inventory GetInventoryOrNull(InventorySlot slot)
        {
            for (int i = 0; i < Inventories.Count; ++i)
                if (Inventories[i].IsIn(slot)) return Inventories[i];
            return null;
        }
    }
}
