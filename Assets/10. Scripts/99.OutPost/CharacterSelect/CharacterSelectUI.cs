using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    /// <summary>
    /// 캐릭터 선택창 동작.
    ///  - 아이콘 클릭 → 하이라이트 이동 + 일러스트/설명 이미지 교체
    ///  - 시작 버튼 → 고른 캐릭터를 SelectedCharacter 에 저장하고 게임 씬으로 이동
    /// (UI 참조는 CharacterSelectUIBuilder 가 자동 연결, 캐릭터 목록만 직접 넣는다)
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("캐릭터 목록 (아이콘 왼쪽부터 순서대로)")]
        [SerializeField] private List<CharacterData> characters = new List<CharacterData>();

        [Header("UI 연결 (빌더가 자동 연결)")]
        [SerializeField] private CharacterIconSlot[] slots;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Image infoImage;
        [SerializeField] private Button startButton;

        [Header("씬 이동")]
        [Tooltip("시작 버튼을 누르면 이동할 씬 (Build Settings 에 등록돼 있어야 함)")]
        [SerializeField] private string gameSceneName = "02. OutPost";

        [Header("시작 잠금")]
        [Tooltip("시작이 잠긴 캐릭터를 골랐을 때 Start 버튼에 표시할 글자")]
        [SerializeField] private string lockedText = "Locked";

        private int selectedIndex = -1;

        // 스프라이트가 없는 캐릭터를 고르면 원래 색 블록으로 되돌리기 위해 저장
        private Color illustrationDefaultColor;
        private Color infoDefaultColor;

        private TMP_Text startLabel;   // Start 버튼 글자
        private string startText;      // 원래 글자 ("Start")

        private void Awake()
        {
            startLabel = startButton.GetComponentInChildren<TMP_Text>(true);
            if (startLabel != null) startText = startLabel.text;

            if (illustrationImage != null) illustrationDefaultColor = illustrationImage.color;
            if (infoImage != null) infoDefaultColor = infoImage.color;
        }

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                CharacterData data = i < characters.Count ? characters[i] : null;
                slots[i].Setup(i, data, Select);
            }

            if (characters.Count < slots.Length)
                Debug.LogWarning($"[{nameof(CharacterSelectUI)}] 캐릭터({characters.Count})가 아이콘 칸({slots.Length})보다 적어 빈 칸이 생깁니다.", this);
            if (characters.Count > slots.Length)
                Debug.LogWarning($"[{nameof(CharacterSelectUI)}] 캐릭터({characters.Count})가 아이콘 칸({slots.Length})보다 많아 일부가 표시되지 않습니다.", this);

            startButton.onClick.AddListener(OnClickStart);
            startButton.interactable = false;

            Select(FindFirstSelectable());
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnClickStart);
        }

        // ───────── 선택 ─────────

        private void Select(int index)
        {
            if (index < 0 || index >= slots.Length || !slots[index].IsSelectable) return;

            selectedIndex = index;
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetSelected(i == index);

            CharacterData data = slots[index].Data;
            SetImage(illustrationImage, data.Illustration, illustrationDefaultColor);
            SetImage(infoImage, data.InfoImage, infoDefaultColor);

            RefreshStartButton(data);
        }

        // 시작이 잠긴 캐릭터면 Start 버튼을 끄고 글자를 바꾼다
        private void RefreshStartButton(CharacterData data)
        {
            bool locked = data.IsStartLocked;
            startButton.interactable = !locked;
            if (startLabel != null) startLabel.text = locked ? lockedText : startText;
        }

        private int FindFirstSelectable()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsSelectable) return i;

            Debug.LogWarning($"[{nameof(CharacterSelectUI)}] 고를 수 있는 캐릭터가 없습니다. 캐릭터 목록을 확인하세요.", this);
            return -1;
        }

        private static void SetImage(Image target, Sprite sprite, Color defaultColor)
        {
            if (target == null) return;

            target.sprite = sprite;
            target.color  = sprite != null ? Color.white : defaultColor;
        }

        // ───────── 시작 ─────────

        private void OnClickStart()
        {
            if (selectedIndex < 0) return;
            if (slots[selectedIndex].Data.IsStartLocked) return;   // 잠긴 캐릭터는 시작 불가

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[{nameof(CharacterSelectUI)}] '{gameSceneName}' 씬을 불러올 수 없습니다. " +
                               "File > Build Settings 에 씬이 등록돼 있는지, 이름이 맞는지 확인하세요.", this);
                return;
            }

            SelectedCharacter.Set(slots[selectedIndex].Data);
            startButton.interactable = false;   // 연타 방지
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
