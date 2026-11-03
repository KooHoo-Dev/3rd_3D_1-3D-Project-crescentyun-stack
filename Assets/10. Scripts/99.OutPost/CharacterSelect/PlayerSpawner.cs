using UnityEngine;

namespace Study3D.OutPost
{
    /// <summary>
    /// 게임 씬에 두는 스포너. 선택창에서 고른 캐릭터를 적용한다.
    ///  - Awake : 캐릭터 프리팹 생성 + 스탯 적용 (다른 스크립트보다 먼저 실행)
    ///  - Start : 시작 아이템 지급 (총 → Hotbar 1·2번 칸, 소모품 → 인벤토리 + 3~6번 바로가기)
    /// 선택창을 거치지 않고 이 씬을 바로 실행하면 Fallback Character 를 쓴다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerSpawner : MonoBehaviour
    {
        [Tooltip("플레이어가 생성될 위치 (비워두면 이 오브젝트 위치)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("선택창 없이 이 씬을 바로 실행했을 때 쓸 캐릭터 (테스트용)")]
        [SerializeField] private CharacterData fallbackCharacter;

        [Tooltip("시야 메시를 그릴 씬 오브젝트 (비워두면 이름이 'VisionMesh' 인 오브젝트를 찾음)")]
        [SerializeField] private MeshFilter visionMesh;
        private const string VisionMeshName = "VisionMesh";

        private CharacterData data;
        private PlayerController player;

        private void Awake()
        {
            data = SelectedCharacter.HasValue ? SelectedCharacter.Current : fallbackCharacter;
            if (data == null)
            {
                Debug.LogWarning($"[{nameof(PlayerSpawner)}] 선택된 캐릭터도, Fallback Character 도 없습니다. 씬에 있는 플레이어를 그대로 씁니다.", this);
                return;
            }

            player = data.PlayerPrefab != null ? SpawnPlayer(data.PlayerPrefab) : FindScenePlayer();
            if (player == null)
            {
                Debug.LogError($"[{nameof(PlayerSpawner)}] '{data.name}' 에 Player Prefab 이 없고, 씬에도 플레이어가 없습니다.", this);
                return;
            }

            ApplyStats();
            ConnectVision();
        }

        private void Start()
        {
            if (data != null && player != null) GiveStartItems();
        }

        // ───────── 생성 ─────────

        private PlayerController SpawnPlayer(PlayerController prefab)
        {
            // 씬에 미리 배치된 플레이어가 있으면 치운다 (Awake 전에 꺼서 Instance 가 덮어쓰이지 않게)
            PlayerController existing = FindScenePlayer();
            if (existing != null)
            {
                Debug.LogWarning($"[{nameof(PlayerSpawner)}] 씬에 있던 '{existing.name}' 을(를) 제거하고 선택한 캐릭터로 교체합니다.", existing);
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            Transform point = spawnPoint != null ? spawnPoint : transform;
            PlayerController spawned = Instantiate(prefab, point.position, point.rotation);
            spawned.name = prefab.name;   // "(Clone)" 제거
            return spawned;
        }

        private static PlayerController FindScenePlayer()
        {
#if UNITY_2022_2_OR_NEWER
            return FindFirstObjectByType<PlayerController>();
#else
            return FindObjectOfType<PlayerController>();
#endif
        }

        private void ApplyStats()
        {
            if (player.TryGetComponent(out PlayerHealth health))
                health.SetMaxHp(data.MaxHp);

            player.ApplyStats(data.MoveSpeed, data.RunSpeed, data.MaxStamina);
        }

        // 프리팹은 씬 오브젝트를 참조할 수 없어서, 시야 메시는 생성 후에 연결해준다
        private void ConnectVision()
        {
            if (!player.TryGetComponent(out PlayerVision vision)) return;

            if (visionMesh == null)
            {
                GameObject found = GameObject.Find(VisionMeshName);
                if (found != null) visionMesh = found.GetComponent<MeshFilter>();
            }

            if (visionMesh == null)
            {
                Debug.LogWarning($"[{nameof(PlayerSpawner)}] 시야 메시({VisionMeshName})를 찾지 못해 PlayerVision 이 시야를 그리지 않습니다.", this);
                return;
            }

            vision.SetViewMeshFilter(visionMesh);
        }

        // ───────── 시작 아이템 ─────────

        private void GiveStartItems()
        {
            if (data.StartItems.Count == 0) return;

            if (!InventorySystem.IsInitialized)
            {
                Debug.LogWarning($"[{nameof(PlayerSpawner)}] InventorySystem 이 없어 시작 아이템을 지급하지 못했습니다.", this);
                return;
            }

            Inventory inventory = InventorySystem.Instance.PlayerInventory;
            PlayerHotbar hotbar = InventorySystem.Instance.Hotbar;

            foreach (CharacterData.StartItem entry in data.StartItems)
            {
                if (entry == null || entry.Item == null) continue;
                InventoryItem item = entry.Item;

                switch (item.Type)
                {
                    case ItemType.Melee:
                        // V 칸은 PlayerHotbar 의 Default Melee 로 고정이라 바꾸지 않는다
                        Debug.LogWarning($"[{nameof(PlayerSpawner)}] 근접무기 '{item.Name}' 은(는) 시작 아이템으로 지급하지 않습니다. (V 칸은 PlayerHotbar 에서 고정)", this);
                        break;

                    case ItemType.Weapon:
                        for (int i = 0; i < entry.Count; i++)
                        {
                            // 빈 무기 칸이 있으면 장착, 없으면 인벤토리로
                            if (!TryEquipWeapon(hotbar, item)) AddToInventory(inventory, item, 1);
                        }
                        break;

                    case ItemType.Consumable:
                        AddToInventory(inventory, item, entry.Count);
                        RegisterShortcut(hotbar, item);
                        break;

                    default:   // Ammo, Misc
                        AddToInventory(inventory, item, entry.Count);
                        break;
                }
            }
        }

        private static bool TryEquipWeapon(PlayerHotbar hotbar, InventoryItem item)
        {
            if (hotbar == null) return false;

            for (int slot = PlayerHotbar.WeaponSlotA; slot <= PlayerHotbar.WeaponSlotB; slot++)
            {
                if (hotbar.Get(slot) != null || !hotbar.CanPlace(slot, item)) continue;

                hotbar.SetWeapon(slot, new ItemStack(item));   // 탄창은 장착 시 가득 채워짐 (Ammo = -1)
                return true;
            }
            return false;
        }

        // MaxStack 을 넘는 개수는 여러 칸으로 나눠 넣는다
        private void AddToInventory(Inventory inventory, InventoryItem item, int count)
        {
            if (inventory == null) return;

            int remaining = count;
            while (remaining > 0)
            {
                int amount = Mathf.Min(remaining, item.MaxStackSize);
                var stack = new ItemStack(item, amount);

                if (!inventory.TryAdd(stack))
                {
                    Debug.LogWarning($"[{nameof(PlayerSpawner)}] 인벤토리가 가득 차 '{item.Name}' {remaining - amount + stack.Count}개를 넣지 못했습니다.", this);
                    return;
                }
                remaining -= amount;
            }
        }

        // 3~6번 칸 중 비어 있는 첫 칸에 바로가기 등록 (이미 등록돼 있으면 건너뜀)
        private static void RegisterShortcut(PlayerHotbar hotbar, InventoryItem item)
        {
            if (hotbar == null) return;

            int emptySlot = -1;
            for (int slot = PlayerHotbar.FirstConsumableSlot; slot < PlayerHotbar.SlotCount; slot++)
            {
                ItemStack current = hotbar.Get(slot);
                if (current != null && current.Data == item) return;
                if (current == null && emptySlot < 0) emptySlot = slot;
            }

            if (emptySlot >= 0) hotbar.SetShortcut(emptySlot, item);
        }
    }
}
