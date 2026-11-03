using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 인벤토리 드래그 처리 (인벤토리 UI 루트 캔버스에 붙임)
    // 각 InventorySlot이 드래그 이벤트를 받아서 여기로 넘긴다.
    //
    //  시작 칸 → 끝 칸           처리
    //  인벤토리 → 인벤토리        빈 칸: 이동 / 같은 아이템: 합치기 / 다른 아이템: 교환
    //  인벤토리 → Hotbar 1·2     총 장착 (원래 있던 총은 그 인벤토리 칸으로)
    //  인벤토리 → Hotbar 3~6     소모품 바로가기 등록 (인벤토리는 그대로)
    //  Hotbar 1·2 → 인벤토리     장착 해제 (빈 칸) / 인벤토리의 다른 총과 교환
    //  Hotbar 3~6 → 인벤토리/바깥 바로가기 해제
    //  Hotbar 1 ↔ 2, 3~6 ↔ 3~6   서로 교환
    //  V 칸, 루팅 칸             드래그 안 함 (V는 고정, 루팅은 클릭으로 가져옴)
    public class InventoryInput : MonoBehaviour
    {
        [Tooltip("드래그 중 마우스를 따라다니는 아이콘 (Raycast Target 꺼짐)")]
        [SerializeField] private Image cursorIcon;
        [SerializeField] private HotbarView hotbarView;

        private InventorySlot startSlot;

        private enum Area { None, Player, Hotbar, Loot }

        private void Start()
        {
            HideCursor();
        }

        // ───────── 드래그 이벤트 (InventorySlot에서 호출) ─────────
        public void BeginDrag(InventorySlot slot, PointerEventData eventData)
        {
            startSlot = null;
            if (!InventorySystem.IsInitialized) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (slot == null || slot.IsEmpty) return;

            Area area = GetArea(slot, out int index);
            if (area == Area.None || area == Area.Loot) return;
            if (area == Area.Hotbar && index == PlayerHotbar.MeleeSlot) return;   // V 칸은 고정

            startSlot = slot;
            ShowCursor(slot.Item, eventData.position);
        }

        public void Drag(PointerEventData eventData)
        {
            if (startSlot == null || cursorIcon == null) return;
            cursorIcon.transform.position = eventData.position;   // Overlay 캔버스는 화면 좌표 = 월드 좌표
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (startSlot == null) return;

            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            InventorySlot endSlot = hit != null ? hit.GetComponentInParent<InventorySlot>() : null;

            if (endSlot != startSlot) HandleDrop(startSlot, endSlot);

            startSlot = null;
            HideCursor();
        }

        // ───────── 놓았을 때 처리 ─────────
        private void HandleDrop(InventorySlot from, InventorySlot to)
        {
            PlayerHotbar hotbar = InventorySystem.Instance.Hotbar;
            Area fromArea = GetArea(from, out int fromIndex);
            Area toArea = GetArea(to, out int toIndex);

            if (fromArea == Area.Player)
            {
                if (toArea == Area.Player) MoveInInventory(from, to);
                else if (toArea == Area.Hotbar && hotbar != null) InventoryToHotbar(from, toIndex, hotbar);
            }
            else if (fromArea == Area.Hotbar && hotbar != null)
            {
                if (toArea == Area.Hotbar) HotbarToHotbar(fromIndex, toIndex, hotbar);
                else if (toArea == Area.Player) HotbarToInventory(fromIndex, to, hotbar);
                else if (toArea == Area.None && PlayerHotbar.IsConsumableSlot(fromIndex))
                    hotbar.SetShortcut(fromIndex, null);   // 바깥에 놓으면 바로가기 해제
            }
        }

        private void MoveInInventory(InventorySlot from, InventorySlot to)
        {
            // 빈 칸으로 이동 (먼저 넣고 나중에 비워야 개수가 잠깐 0이 되지 않음)
            if (to.IsEmpty)
            {
                ItemStack moving = from.Stack;
                to.SetStack(moving, true, false);
                from.Clear();
                return;
            }

            // 같은 아이템이면 합치기 (넘치는 만큼은 원래 칸에 남음)
            if (to.Stack.CanStackWith(from.Stack) && !to.Stack.IsFull)
            {
                to.Stack.MergeFrom(from.Stack);
                to.Refresh(false);
                from.Refresh();
                return;
            }

            // 다른 아이템이면 교환
            ItemStack a = from.Stack;
            ItemStack b = to.Stack;
            from.SetStack(b, true, false);
            to.SetStack(a);
        }

        private void InventoryToHotbar(InventorySlot from, int toIndex, PlayerHotbar hotbar)
        {
            InventoryItem item = from.Item;
            if (!hotbar.CanPlace(toIndex, item)) return;

            // 소모품: 바로가기만 등록
            if (PlayerHotbar.IsConsumableSlot(toIndex))
            {
                hotbar.SetShortcut(toIndex, item);
                return;
            }

            // 총: 인벤토리 → 장착 칸, 원래 장착돼 있던 총은 그 인벤토리 칸으로
            ItemStack old = hotbar.SetWeapon(toIndex, from.Stack);
            from.SetStack(old);
        }

        private void HotbarToInventory(int fromIndex, InventorySlot to, PlayerHotbar hotbar)
        {
            if (PlayerHotbar.IsConsumableSlot(fromIndex))
            {
                hotbar.SetShortcut(fromIndex, null);
                return;
            }
            if (!PlayerHotbar.IsWeaponSlot(fromIndex)) return;

            // 빈 칸: 장착 해제
            if (to.IsEmpty)
            {
                ItemStack removed = hotbar.SetWeapon(fromIndex, null);
                to.SetStack(removed);
                return;
            }

            // 인벤토리 칸에 다른 총이 있으면 서로 교환
            if (hotbar.CanPlace(fromIndex, to.Item))
            {
                ItemStack removed = hotbar.SetWeapon(fromIndex, to.Stack);
                to.SetStack(removed);
            }
        }

        private void HotbarToHotbar(int fromIndex, int toIndex, PlayerHotbar hotbar)
        {
            if (PlayerHotbar.IsWeaponSlot(fromIndex) && PlayerHotbar.IsWeaponSlot(toIndex))
                hotbar.SwapWeapons();
            else if (PlayerHotbar.IsConsumableSlot(fromIndex) && PlayerHotbar.IsConsumableSlot(toIndex))
                hotbar.SwapShortcuts(fromIndex, toIndex);
        }

        // ───────── 슬롯 종류 판별 ─────────
        private Area GetArea(InventorySlot slot, out int index)
        {
            index = -1;
            if (slot == null) return Area.None;

            if (hotbarView != null && (index = hotbarView.IndexOf(slot)) >= 0) return Area.Hotbar;

            var system = InventorySystem.Instance;
            if (system.PlayerInventory != null && (index = system.PlayerInventory.IndexOf(slot)) >= 0) return Area.Player;
            if (system.LootInventory != null && (index = system.LootInventory.IndexOf(slot)) >= 0) return Area.Loot;

            index = -1;
            return Area.None;
        }

        // ───────── 커서 아이콘 ─────────
        private void ShowCursor(InventoryItem item, Vector2 position)
        {
            if (cursorIcon == null) return;

            cursorIcon.sprite = item != null ? item.Icon : null;
            cursorIcon.transform.position = position;
            cursorIcon.transform.SetAsLastSibling();   // 다른 UI보다 위에
            cursorIcon.gameObject.SetActive(true);
        }

        private void HideCursor()
        {
            if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
        }
    }
}
