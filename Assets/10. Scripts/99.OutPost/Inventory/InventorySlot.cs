using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    public class InventorySlot : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        //  Slot은 내부에 Content라고 부르는 Image GameObject를 제어합니다.
        // - 외부에서 아이템 묶음(ItemStack) 설정 / 비우기, 개수 표시
        // - 루팅 슬롯은 "미확인(회색)" 상태와 탐색 진행도 표시
        // - 드래그 입력은 부모 캔버스의 InventoryInput에 넘김
        //   (슬롯이 직접 드래그를 받아야 ScrollRect가 드래그를 가로채지 않음)

        [field: SerializeField] public Image Content { get; private set; }
        [SerializeField] private TMP_Text countText;

        [Tooltip("탐색 진행도 표시용 Image (Image Type = Filled). 없어도 됨")]
        [SerializeField] private Image searchProgress;
        [SerializeField] private Color hiddenColor = new Color(0.25f, 0.25f, 0.25f, 1f);

        private ItemStack stack;
        private bool searched = true;
        private InventoryInput dragInput;

        /// <summary> 내용(아이템/개수)이 바뀌면 호출 </summary>
        public event Action<InventorySlot> OnChanged;

        public ItemStack Stack => stack;
        public InventoryItem Item => stack != null ? stack.Data : null;
        public bool IsEmpty => stack == null || stack.Data == null || stack.Count <= 0;

        // 꺼진 상태에서 먼저 넣어 둔 값도 처음 켜질 때 그대로 그려지도록, 여기서는 그리기만 한다.
        private void Awake()
        {
            Draw();
        }

        /// <summary>
        /// 묶음을 넣는다. null이면 비운다.
        /// searched == false 이면 회색(미확인)으로 표시.
        /// notify == false 이면 알림 없이 바꾼다 (두 칸을 교환할 때 중간 상태를 알리지 않기 위함).
        /// </summary>
        public void SetStack(ItemStack inputStack, bool isSearched = true, bool notify = true)
        {
            stack = inputStack;
            searched = isSearched;
            SetSearchProgress(0f);
            Refresh(notify);
        }

        public void Clear(bool notify = true) => SetStack(null, true, notify);

        /// <summary> 개수를 직접 바꾼 뒤 호출. 0개가 되면 칸을 비운다. </summary>
        public void Refresh(bool notify = true)
        {
            if (IsEmpty) stack = null;
            Draw();
            if (notify) OnChanged?.Invoke(this);
        }

        private void Draw()
        {
            if (Content == null) return;

            if (stack == null)
            {
                Content.sprite = null;
                Content.enabled = false;
                if (countText != null) countText.text = "";
                return;
            }

            Content.sprite = stack.Data.Icon;
            Content.color = searched ? Color.white : hiddenColor;
            Content.enabled = true;
            if (countText != null) countText.text = stack.Count > 1 ? stack.Count.ToString() : "";
        }

        public void SetSearchProgress(float t)
        {
            if (searchProgress == null) return;
            searchProgress.enabled = t > 0f;
            searchProgress.fillAmount = t;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            InventorySystem.Instance.OnSlotClicked(this);
        }

        // ───────── 드래그: InventoryInput에 전달 ─────────
        private InventoryInput DragInput
        {
            get
            {
                if (dragInput == null) dragInput = GetComponentInParent<InventoryInput>();
                return dragInput;
            }
        }

        public void OnBeginDrag(PointerEventData eventData) { if (DragInput != null) DragInput.BeginDrag(this, eventData); }
        public void OnDrag(PointerEventData eventData)      { if (DragInput != null) DragInput.Drag(eventData); }
        public void OnEndDrag(PointerEventData eventData)   { if (DragInput != null) DragInput.EndDrag(eventData); }
    }
}
