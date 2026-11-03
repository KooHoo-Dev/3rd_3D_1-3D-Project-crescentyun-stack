using System;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    /// <summary>
    /// 하단 캐릭터 아이콘 한 칸.
    /// 데이터의 초상화를 표시하고, 클릭되면 자기 번호를 알린다.
    /// (참조는 CharacterSelectUIBuilder 가 자동 연결)
    /// </summary>
    public class CharacterIconSlot : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject highlight;   // 선택 테두리
        [SerializeField] private Image portrait;

        private int index;
        private Action<int> onClick;

        public CharacterData Data { get; private set; }
        public bool IsSelectable => Data != null;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary> 데이터가 없으면(null) 빈 칸으로 두고 클릭을 막는다. </summary>
        public void Setup(int slotIndex, CharacterData data, Action<int> clickCallback)
        {
            index   = slotIndex;
            Data    = data;
            onClick = clickCallback;

            portrait.sprite = data != null ? data.Portrait : null;
            button.interactable = IsSelectable;
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (highlight != null) highlight.SetActive(selected);
        }

        private void HandleClick()
        {
            if (IsSelectable) onClick?.Invoke(index);
        }
    }
}
