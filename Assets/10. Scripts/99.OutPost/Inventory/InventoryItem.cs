using UnityEngine;

namespace Study3D.OutPost
{
    // 아이템 분류 — 어느 칸에 들어갈 수 있는지, 겹쳐지는지를 정한다
    public enum ItemType
    {
        Misc,         // 기타 (재료 등)
        Weapon,       // 총 — Hotbar 1, 2번 칸
        Melee,        // 근접무기 — Hotbar V 칸
        Consumable,   // 소모품 — Hotbar 3~6번 칸 (바로가기)
        Ammo,         // 탄약 — 재장전 시 인벤토리에서 소모
    }

    // 탄약 종류 — 총(Gun)과 탄약 아이템이 같은 값을 가지면 서로 맞는다
    public enum AmmoType
    {
        None,
        Rifle,
        Pistol,
    }

    [CreateAssetMenu(menuName = "OutPost/Inventory Item", fileName = "Item_")]
    public class InventoryItem : ScriptableObject
    {
        public int Key;
        public string Name;
        public string Description;
        public Sprite Icon;

        [Tooltip("루팅 시 이 아이템이 확인되기까지 걸리는 시간(초)")]
        [Min(0f)] public float SearchTime = 1f;

        [Header("분류")]
        public ItemType Type = ItemType.Misc;

        [Tooltip("한 칸에 겹칠 수 있는 최대 개수 (무기/근접무기는 항상 1)")]
        [Min(1)] public int MaxStack = 5;

        [Header("무기 전용")]
        [Tooltip("장착하면 손에 생성될 총 프리팹")]
        public Gun GunPrefab;

        [Header("근접무기 전용")]
        [Tooltip("V 칸에 고정되면 손에 생성될 근접무기 프리팹 (MeleeWeapon이 붙은 모델)")]
        public MeleeWeapon MeleePrefab;

        [Header("탄약 전용")]
        public AmmoType AmmoType = AmmoType.None;
        
        
        
        [Header("소모품 전용")]
        [Tooltip("사용하면 회복되는 HP")]
        [Min(0f)] public float HealAmount = 0f;

        // 무기는 한 칸에 하나만
        public int MaxStackSize => (Type == ItemType.Weapon || Type == ItemType.Melee) ? 1 : MaxStack;
        public bool IsStackable => MaxStackSize > 1;
    }
}
