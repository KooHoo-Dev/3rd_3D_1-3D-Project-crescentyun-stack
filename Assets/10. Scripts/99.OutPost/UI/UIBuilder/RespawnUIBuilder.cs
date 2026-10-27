#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    /// <summary>
    /// 리스폰(사망) 화면 UI를 코드로 조립하는 에디터 전용 빌더.
    /// 빈 GameObject에 붙인 뒤 컴포넌트 우클릭 메뉴로 실행한다.
    /// </summary>
    public class RespawnUIBuilder : MonoBehaviour
    {
        #region 상수 / 색상

        private const string RootName = "Respawn UI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Color BackdropColor        = new Color32(0x12, 0x12, 0x12, 0xFF); // 바깥 배경
        private static readonly Color ColumnColor          = new Color32(0x1F, 0x1F, 0x1F, 0xFF); // 가운데 세로 영역
        private static readonly Color TitleColor           = new Color32(0xF0, 0x4E, 0x5E, 0xFF); // YOU DIED
        private static readonly Color TextColor            = new Color32(0xF2, 0xF2, 0xF2, 0xFF); // 일반 텍스트
        private static readonly Color HighlightBorderColor = new Color32(0xE8, 0x4A, 0x5A, 0xFF); // 사망 원인 박스 테두리
        private static readonly Color HighlightFillColor   = new Color32(0x1F, 0x1F, 0x1F, 0xFF); // 사망 원인 박스 안쪽
        private static readonly Color ButtonColor          = new Color32(0x5E, 0xCB, 0x7C, 0xFF); // 계속하기 버튼
        private static readonly Color ButtonLabelColor     = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        #endregion

        [Tooltip("생성되는 모든 텍스트에 적용할 폰트 (한글 폰트 지정 권장)")]
        [SerializeField] private TMP_FontAsset font;

        #region 메뉴

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            // 이미 루트가 있으면 중복 생성하지 않는다
            if (FindRoots().Length > 0)
            {
                Debug.LogWarning($"[RespawnUIBuilder] '{RootName}' 이(가) 이미 존재합니다. 먼저 '2. Clear UI'를 실행하세요.");
                return;
            }

            if (font == null)
                Debug.LogWarning("[RespawnUIBuilder] font 가 비어 있습니다. 기본 TMP 폰트는 한글이 깨질 수 있습니다.");

            if (FindFirstObjectByType<EventSystem>() == null)
                Debug.LogWarning("[RespawnUIBuilder] 씬에 EventSystem 이 없습니다. 버튼 입력을 받으려면 추가하세요.");

            GameObject root = CreateCanvas();
            Undo.RegisterCreatedObjectUndo(root, "Build Respawn UI");

            CreateBackdrop(root.transform);
            Transform column = CreateCenterColumn(root.transform);
            CreateTitle(column);
            CreateDeathCause(column);
            CreateContinueButton(column);

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[RespawnUIBuilder] '{RootName}' 생성 완료");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject[] roots = FindRoots();
            if (roots.Length == 0)
            {
                Debug.Log($"[RespawnUIBuilder] 삭제할 '{RootName}' 이(가) 없습니다.");
                return;
            }

            foreach (GameObject root in roots)
            {
                Debug.Log($"[RespawnUIBuilder] 삭제: {root.name}");
                Undo.DestroyObjectImmediate(root);
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        /// <summary>현재 씬 최상위에서 우리가 만든 루트 이름의 오브젝트만 찾는다.</summary>
        private GameObject[] FindRoots()
        {
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go.name == RootName) list.Add(go);
            }
            return list.ToArray();
        }

        #endregion

        #region Canvas

        private GameObject CreateCanvas()
        {
            var root = new GameObject(RootName, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        #endregion

        #region 배경

        /// <summary>화면 전체를 덮는 어두운 배경</summary>
        private void CreateBackdrop(Transform canvas)
        {
            GameObject backdrop = NewPanel("Backdrop", canvas, Vector2.zero);
            Image img = backdrop.GetComponent<Image>();
            img.color = BackdropColor;
            img.raycastTarget = true; // 뒤쪽 게임 화면 클릭 차단
            Stretch(backdrop.GetComponent<RectTransform>(), 0f);
        }

        /// <summary>시안의 가운데 세로 영역 (위아래 꽉 채움, 폭 750)</summary>
        private Transform CreateCenterColumn(Transform canvas)
        {
            GameObject column = NewPanel("CenterColumn", canvas, new Vector2(750f, 0f));
            column.GetComponent<Image>().color = ColumnColor;

            RectTransform rect = column.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(750f, 0f);
            return column.transform;
        }

        #endregion

        #region 중앙 콘텐츠

        /// <summary>"YOU DIED" 제목 (EXP 공간만큼 키우고 위로 올림)</summary>
        private void CreateTitle(Transform column)
        {
            TMP_Text title = NewText("TitleText", column, "YOU DIED", 140f, TextAlignmentOptions.Center);
            title.color = TitleColor;
            title.fontStyle = FontStyles.Bold;

            RectTransform rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 190f);
            rect.sizeDelta = new Vector2(720f, 170f);
        }

        /// <summary>"난 이것 때문에 죽었어 →" + 빨간 테두리 박스</summary>
        private void CreateDeathCause(Transform column)
        {
            // 한 줄 컨테이너 (내용 폭에 맞춰 가운데 정렬)
            GameObject row = NewUI("DeathCauseRow", column);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector2(0f, 40f);
            rowRect.sizeDelta = new Vector2(0f, 72f);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 왼쪽 안내 문구
            TMP_Text label = NewText("CauseLabelText", row.transform, "난 이것 때문에 죽었어 →", 34f, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;

            // 빨간 테두리 박스 (바깥 Image = 테두리 색)
            GameObject box = NewPanel("CauseHighlightBox", row.transform, Vector2.zero);
            box.GetComponent<Image>().color = HighlightBorderColor;

            HorizontalLayoutGroup boxLayout = box.AddComponent<HorizontalLayoutGroup>();
            boxLayout.padding = new RectOffset(22, 22, 10, 10);
            boxLayout.childAlignment = TextAnchor.MiddleCenter;
            boxLayout.childControlWidth = true;
            boxLayout.childControlHeight = true;
            boxLayout.childForceExpandWidth = false;
            boxLayout.childForceExpandHeight = false;

            // 안쪽 채움 (테두리 두께 3px 만큼 안으로)
            GameObject fill = NewPanel("CauseHighlightFill", box.transform, Vector2.zero);
            fill.GetComponent<Image>().color = HighlightFillColor;
            fill.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch(fill.GetComponent<RectTransform>(), 3f);

            // 사망 원인 텍스트 (Fill 다음에 생성해서 위에 그려지게)
            TMP_Text value = NewText("CauseValueText", box.transform, "꼬마덕 (AK-47) 머리, 눈", 34f, TextAlignmentOptions.Center);
            value.fontStyle = FontStyles.Bold;
        }

        /// <summary>하단 "계속하기" 버튼 (EXP 자리까지 내려와 크게 배치)</summary>
        private void CreateContinueButton(Transform column)
        {
            Button button = NewButton("ContinueButton", column, "계속하기", out TMP_Text labelText);
            labelText.fontSize = 46f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = ButtonLabelColor;

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -130f);
            rect.sizeDelta = new Vector2(690f, 120f);
        }

        #endregion

        #region 도우미 메서드

        /// <summary>RectTransform만 가진 빈 UI 오브젝트 생성</summary>
        private GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return go;
        }

        /// <summary>Image가 붙은 패널 생성 (색은 호출 측에서 지정)</summary>
        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            Image img = go.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            go.GetComponent<RectTransform>().sizeDelta = size;
            return go;
        }

        /// <summary>TextMeshProUGUI 텍스트 생성 (font 필드 자동 적용)</summary>
        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = TextColor;
            tmp.raycastTarget = false;

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(200f, 50f);
            return tmp;
        }

        /// <summary>Image + Button + 라벨 텍스트 생성</summary>
        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            GameObject go = NewPanel(name, parent, new Vector2(300f, 80f));
            Image img = go.GetComponent<Image>();
            img.color = ButtonColor;
            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor     = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor    = Color.white;
            colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;

            labelText = NewText("Label", go.transform, label, 40f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);
            return btn;
        }

        /// <summary>부모를 꽉 채우도록 늘린다 (padding 만큼 안쪽 여백)</summary>
        private void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        #endregion
    }
}
#endif
