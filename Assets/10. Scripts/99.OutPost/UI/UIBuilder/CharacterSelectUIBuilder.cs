using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Study3D.OutPost
{
    /// <summary>
    /// 캐릭터 선택창 UI를 코드로 조립하는 에디터 툴.
    /// 빈 GameObject에 붙인 뒤, 컴포넌트 우클릭 메뉴에서 실행한다.
    ///  - 1. Build UI : 캐릭터 선택창 생성
    ///  - 2. Clear UI : 이 툴이 만든 루트(CharacterSelectUI)만 삭제
    /// 화면을 만든 뒤 CharacterSelectUI / CharacterIconSlot 컴포넌트를 붙이고 참조까지 연결한다.
    /// (캐릭터 목록과 이동할 씬 이름은 생성된 CharacterSelectUI 인스펙터에서 넣는다)
    /// </summary>
    public class CharacterSelectUIBuilder : MonoBehaviour
    {
        // 한글 폰트 교체용. 생성되는 모든 텍스트에 적용된다. (비워두면 TMP 기본 폰트)
        // ※ 빌드/에디터 간 직렬화 구조가 같도록 #if 바깥에 둔다.
        [SerializeField] private TMP_FontAsset font;

#if UNITY_EDITOR

        #region 설정값

        private const string RootName = "CharacterSelectUI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // ── 색상 (스프라이트가 들어갈 자리는 색 블록으로 대체) ──
        private static readonly Color BackgroundColor    = new Color(0.45f, 0.16f, 0.08f, 1f);   // 배경 바탕
        private static readonly Color IllustrationColor  = Color.white;                          // 일러스트 자리
        private static readonly Color InfoPanelColor     = new Color(0.12f, 0.06f, 0.04f, 0.75f); // 설명 패널 바탕(반투명)
        private static readonly Color InfoImageColor     = new Color(1f, 1f, 1f, 0.08f);          // 설명 이미지 자리
        private static readonly Color IconFrameColor     = new Color(0.18f, 0.16f, 0.15f, 1f);   // 아이콘 테두리
        private static readonly Color IconPortraitColor  = Color.white;                          // 아이콘 초상화 자리
        private static readonly Color SelectedColor      = new Color(1f, 0.60f, 0.10f, 1f);      // 선택 하이라이트
        private static readonly Color StartButtonColor   = new Color(0.85f, 0.45f, 0.10f, 1f);   // 시작 버튼

        // ── 캐릭터 설명 패널 (중앙 좌측) ──
        private static readonly Vector2 InfoPanelSize   = new Vector2(760f, 440f);
        private static readonly Vector2 InfoPanelOffset = new Vector2(120f, 60f);   // 좌측 가장자리에서 120px, 중앙보다 60px 위

        // ── 캐릭터 선택 아이콘 (중앙 하단) ──
        private const int   CharacterCount       = 4;
        private const int   DefaultSelectedIndex = 0;      // 처음 선택되어 보일 칸
        private const float IconSpacing          = 20f;
        private const float IconBarBottom        = 40f;    // 화면 하단에서 띄울 거리
        private const float IconBorder           = 6f;     // 테두리 두께
        private static readonly Vector2 IconSize = new Vector2(110f, 135f);

        // ── 시작 버튼 (우측 하단) ──
        private static readonly Vector2 StartButtonSize   = new Vector2(280f, 90f);
        private static readonly Vector2 StartButtonOffset = new Vector2(-60f, 40f);   // 우측 하단 모서리 기준
        private const string StartButtonText = "Start";

        #endregion

        #region 진입점 (컨텍스트 메뉴)

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            // 이미 만들어져 있으면 겹쳐 만들지 않는다
            if (FindRoots().Count > 0)
            {
                Debug.LogWarning($"[{nameof(CharacterSelectUIBuilder)}] '{RootName}' 이(가) 이미 있습니다. 먼저 '2. Clear UI' 를 실행하세요.", this);
                return;
            }

            GameObject canvas = CreateCanvas();
            Undo.RegisterCreatedObjectUndo(canvas, "Build " + RootName);

            CreateBackground(canvas.transform);       // 1. 뒷배경 (일러스트)
            CreateInfoPanel(canvas.transform);        // 2. 중앙 좌측 (설명 칸)
            CreateCharacterIconBar(canvas.transform); // 3. 중앙 하단 (선택 아이콘)
            CreateStartButton(canvas.transform);      // 4. 우측 하단 (시작 버튼)

            ConnectLogic(canvas);                     // 5. 로직 컴포넌트 부착 + 참조 연결

            CheckEventSystem();

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Selection.activeGameObject = canvas;
            Debug.Log($"[{nameof(CharacterSelectUIBuilder)}] '{RootName}' 생성 완료.", canvas);
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            List<GameObject> roots = FindRoots();
            if (roots.Count == 0)
            {
                Debug.Log($"[{nameof(CharacterSelectUIBuilder)}] 삭제할 '{RootName}' 이(가) 없습니다.", this);
                return;
            }

            foreach (GameObject root in roots)
            {
                Debug.Log($"[{nameof(CharacterSelectUIBuilder)}] 삭제: {root.name}", this);
                Undo.DestroyObjectImmediate(root);
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        // 이 컴포넌트가 있는 씬의 최상위 오브젝트 중, 우리가 만든 루트 이름만 찾는다 (비활성 포함)
        private List<GameObject> FindRoots()
        {
            var result = new List<GameObject>();
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go.name == RootName) result.Add(go);
            }
            return result;
        }

        #endregion

        #region Canvas

        private GameObject CreateCanvas()
        {
            var go = new GameObject(RootName,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");

            // 활성 씬이 아니라 이 빌더가 있는 씬에 만든다
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            return go;
        }

        private void CheckEventSystem()
        {
#if UNITY_2022_2_OR_NEWER
            EventSystem es = FindFirstObjectByType<EventSystem>();
#else
            EventSystem es = FindObjectOfType<EventSystem>();
#endif
            if (es == null)
            {
                Debug.LogWarning($"[{nameof(CharacterSelectUIBuilder)}] 씬에 EventSystem 이 없습니다. " +
                                 "버튼 클릭을 받으려면 GameObject > UI > Event System 을 추가하세요.", this);
            }
        }

        #endregion

        #region 1. 뒷배경 (캐릭터 일러스트)

        private void CreateBackground(Transform canvas)
        {
            // 화면 전체를 덮는 바탕색
            GameObject background = NewPanel("Background", canvas, Vector2.zero);
            Stretch(background.GetComponent<RectTransform>(), 0f);
            Image bgImage = background.GetComponent<Image>();
            bgImage.color = BackgroundColor;
            bgImage.raycastTarget = false;

            // 캐릭터 일러스트가 들어갈 자리 (나중에 Sprite 교체)
            GameObject illustration = NewPanel("CharacterIllustration", background.transform, Vector2.zero);
            Stretch(illustration.GetComponent<RectTransform>(), 0f);
            Image illuImage = illustration.GetComponent<Image>();
            illuImage.color = IllustrationColor;
            illuImage.raycastTarget = false;
        }

        #endregion

        #region 2. 중앙 좌측 (캐릭터 설명 칸)

        private void CreateInfoPanel(Transform canvas)
        {
            // 좌측 가장자리 중앙에 고정
            GameObject panel = NewPanel("CharacterInfoPanel", canvas, InfoPanelSize);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, 0.5f);
            rect.anchorMax        = new Vector2(0f, 0.5f);
            rect.pivot            = new Vector2(0f, 0.5f);
            rect.anchoredPosition = InfoPanelOffset;
            rect.sizeDelta        = InfoPanelSize;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = InfoPanelColor;
            panelImage.raycastTarget = false;

            // 설명 이미지가 들어갈 자리 (패널 꽉 채움)
            GameObject infoImage = NewPanel("CharacterInfoImage", panel.transform, Vector2.zero);
            Stretch(infoImage.GetComponent<RectTransform>(), 0f);
            Image slot = infoImage.GetComponent<Image>();
            slot.color = InfoImageColor;
            slot.preserveAspect = true;   // 이미지 비율 유지
            slot.raycastTarget = false;
        }

        #endregion

        #region 3. 중앙 하단 (캐릭터 선택 아이콘)

        private void CreateCharacterIconBar(Transform canvas)
        {
            float barWidth = CharacterCount * IconSize.x + (CharacterCount - 1) * IconSpacing;

            // 하단 중앙에 고정
            GameObject bar = NewUI("CharacterIconBar", canvas);
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin        = new Vector2(0.5f, 0f);
            barRect.anchorMax        = new Vector2(0.5f, 0f);
            barRect.pivot            = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, IconBarBottom);
            barRect.sizeDelta        = new Vector2(barWidth, IconSize.y);

            // 아이콘 가로 나열
            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing                = IconSpacing;
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.childControlWidth      = false;
            layout.childControlHeight     = false;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            // 처음에 숨길 하이라이트들을 모아뒀다가 마지막에 끈다
            var hiddenHighlights = new List<GameObject>();

            for (int i = 0; i < CharacterCount; i++)
            {
                GameObject highlight = CreateCharacterIcon(bar.transform, i);
                if (i != DefaultSelectedIndex) hiddenHighlights.Add(highlight);
            }

            foreach (GameObject h in hiddenHighlights) h.SetActive(false);
        }

        /// <summary>아이콘 한 칸을 만들고, 선택 하이라이트 오브젝트를 돌려준다.</summary>
        private GameObject CreateCharacterIcon(Transform parent, int index)
        {
            Button button = NewButton($"CharacterIcon_{index + 1:00}", parent, string.Empty, out TMP_Text labelText);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = IconSize;

            button.GetComponent<Image>().color = IconFrameColor;

            // 선택 하이라이트 (테두리 역할) — 프레임보다 바깥으로 살짝 크게
            GameObject highlight = NewPanel("SelectedHighlight", button.transform, Vector2.zero);
            Stretch(highlight.GetComponent<RectTransform>(), -IconBorder);
            Image hlImage = highlight.GetComponent<Image>();
            hlImage.color = SelectedColor;
            hlImage.raycastTarget = false;

            // 초상화 자리 (나중에 Sprite 교체) — 테두리만큼 안쪽
            GameObject portrait = NewPanel("Portrait", button.transform, Vector2.zero);
            Stretch(portrait.GetComponent<RectTransform>(), IconBorder);
            Image portraitImage = portrait.GetComponent<Image>();
            portraitImage.color = IconPortraitColor;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            // 아이콘에는 글자가 필요 없으므로 라벨 제거
            Object.DestroyImmediate(labelText.gameObject);

            // 그리는 순서 : 하이라이트 → 초상화
            highlight.transform.SetSiblingIndex(0);
            portrait.transform.SetSiblingIndex(1);

            return highlight;
        }

        #endregion

        #region 4. 우측 하단 (시작 버튼)

        private void CreateStartButton(Transform canvas)
        {
            Button button = NewButton("StartButton", canvas, StartButtonText, out TMP_Text labelText);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(1f, 0f);
            rect.anchorMax        = new Vector2(1f, 0f);
            rect.pivot            = new Vector2(1f, 0f);
            rect.anchoredPosition = StartButtonOffset;
            rect.sizeDelta        = StartButtonSize;

            button.GetComponent<Image>().color = StartButtonColor;
            labelText.fontSize = 40f;
        }

        #endregion

        #region 5. 로직 연결

        // 만든 오브젝트에 런타임 컴포넌트를 붙이고, private [SerializeField] 를 SerializedObject 로 채운다
        private void ConnectLogic(GameObject canvas)
        {
            Transform root = canvas.transform;

            // ── 아이콘 칸마다 CharacterIconSlot ──
            Transform bar = root.Find("CharacterIconBar");
            var slots = new List<CharacterIconSlot>();
            for (int i = 0; i < bar.childCount; i++)
            {
                Transform icon = bar.GetChild(i);
                var slot = icon.gameObject.AddComponent<CharacterIconSlot>();

                var slotSO = new SerializedObject(slot);
                slotSO.FindProperty("button").objectReferenceValue    = icon.GetComponent<Button>();
                slotSO.FindProperty("highlight").objectReferenceValue = icon.Find("SelectedHighlight").gameObject;
                slotSO.FindProperty("portrait").objectReferenceValue  = icon.Find("Portrait").GetComponent<Image>();
                slotSO.ApplyModifiedPropertiesWithoutUndo();

                slots.Add(slot);
            }

            // ── 루트에 CharacterSelectUI ──
            var ui = canvas.AddComponent<CharacterSelectUI>();
            var uiSO = new SerializedObject(ui);

            SerializedProperty slotsProp = uiSO.FindProperty("slots");
            slotsProp.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

            uiSO.FindProperty("illustrationImage").objectReferenceValue =
                root.Find("Background/CharacterIllustration").GetComponent<Image>();
            uiSO.FindProperty("infoImage").objectReferenceValue =
                root.Find("CharacterInfoPanel/CharacterInfoImage").GetComponent<Image>();
            uiSO.FindProperty("startButton").objectReferenceValue =
                root.Find("StartButton").GetComponent<Button>();

            uiSO.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region 도우미 메서드

        private GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(100f, 100f);
            return go;
        }

        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = Color.white;
            return go;
        }

        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;   // 한글 폰트 적용
            tmp.text          = text;
            tmp.fontSize      = size;
            tmp.alignment     = align;
            tmp.color         = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            GameObject go = NewPanel(name, parent, new Vector2(160f, 60f));

            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            labelText = NewText("Label", go.transform, label, 28f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);
            return button;
        }

        private void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin        = Vector2.zero;
            rect.anchorMax        = Vector2.one;
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta        = new Vector2(-padding * 2f, -padding * 2f); // offsetMin = +padding, offsetMax = -padding
        }

        #endregion

#endif
    }
}
