using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Study3D.OutPost
{
    /// <summary>
    /// 메인 인게임 UI(인벤토리 / 무게 / 퀵슬롯 / 루팅 창)를 코드로 조립하는 에디터 전용 빌더.
    /// 빈 GameObject에 붙인 뒤 컴포넌트 우클릭 → "1. Build UI" / "2. Clear UI".
    /// 화면만 만들며, 게임 로직은 연결하지 않는다.
    /// </summary>
    public class InventoryUIBuilder : MonoBehaviour
    {
        // 한글 폰트 교체용. 비워 두면 TMP 기본 폰트를 사용한다.
        // (빌드/에디터 직렬화 구조가 같도록 필드는 #if 바깥에 둔다)
        [SerializeField] private TMP_FontAsset font;

#if UNITY_EDITOR

        #region 상수 / 색상

        private const string RootName = "Main UI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // 인벤토리 (6열 x 12행 = 72칸, 스크롤)
        private const int InventoryCapacity = 72;
        private const int InventoryUsed = 51;
        private const int InventoryColumns = 6;
        private const float InventoryCell = 66f;
        private const float InventorySpacing = 6f;

        // 루팅 창 (4열 x 2행 = 8칸)
        private const int LootCapacity = 8;
        private const int LootUsed = 5;
        private const int LootColumns = 4;
        private const float LootCell = 92f;
        private const float LootSpacing = 8f;

        // 퀵슬롯 키 순서
        private static readonly string[] HotbarKeys = { "1", "2", "V", "3", "4", "5", "6" };
        private const float HotbarCell = 84f;
        private const float HotbarSpacing = 10f;

        // 무게 (시안 값)
        private const float CurrentWeight = 59.3f;
        private const float MaxWeight = 118.9f;

        // 색상 (외부 에셋 없이 코드로만 지정)
        private static readonly Color PanelColor      = new Color(0.10f, 0.13f, 0.16f, 0.82f);
        private static readonly Color SubPanelColor   = new Color(0.14f, 0.18f, 0.22f, 0.92f);
        private static readonly Color SlotColor       = new Color(0.20f, 0.25f, 0.30f, 0.80f);
        private static readonly Color SlotFilledColor = new Color(0.27f, 0.33f, 0.39f, 0.95f);
        private static readonly Color ButtonColor     = new Color(0.86f, 0.93f, 0.96f, 1f);
        private static readonly Color TextColor       = new Color(0.95f, 0.96f, 0.97f, 1f);
        private static readonly Color SubTextColor    = new Color(0.78f, 0.82f, 0.86f, 1f);
        private static readonly Color DarkTextColor   = new Color(0.10f, 0.13f, 0.16f, 1f);
        private static readonly Color GaugeBackColor  = new Color(0.04f, 0.05f, 0.07f, 0.90f);
        private static readonly Color GaugeFillColor  = new Color(0.92f, 0.94f, 0.96f, 1f);
        private static readonly Color CoinColor       = new Color(0.95f, 0.78f, 0.25f, 1f);

        // 아이템 아이콘 자리 표시용 색 (스프라이트 대신 색 블록)
        private static readonly Color[] ItemPlaceholderColors =
        {
            new Color(0.55f, 0.62f, 0.40f, 1f), // 탄약/총기류
            new Color(0.70f, 0.55f, 0.35f, 1f), // 재료/잡화
            new Color(0.80f, 0.30f, 0.30f, 1f), // 의료품
            new Color(0.35f, 0.55f, 0.75f, 1f), // 음료/소모품
            new Color(0.85f, 0.75f, 0.35f, 1f), // 귀중품
            new Color(0.55f, 0.45f, 0.70f, 1f), // 특수
        };

        private static int UILayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("UI");
                return layer < 0 ? 0 : layer;
            }
        }

        #endregion

        #region 진입점 (ContextMenu)

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            // 이미 만들어져 있으면 겹쳐 만들지 않는다.
            if (FindRoot() != null)
            {
                Debug.LogWarning($"[MainUIBuilder] '{RootName}' 이(가) 이미 있습니다. 먼저 '2. Clear UI' 를 실행하세요.", this);
                return;
            }

            Transform canvas = CreateCanvas();

            CreateInventory(canvas);
            CreateWeight(canvas);
            CreateHotbar(canvas);
            GameObject loot = CreateLoot(canvas);

            // 루팅 창은 상자를 열 때만 보이므로 처음엔 숨김
            loot.SetActive(false);

            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Build Main UI");
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Selection.activeGameObject = canvas.gameObject;

            Debug.Log($"[MainUIBuilder] '{RootName}' 생성 완료.", canvas.gameObject);
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            int removed = 0;

            // 우리가 만든 루트 이름의 오브젝트만 지운다.
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go.name != RootName) continue;

                Debug.Log($"[MainUIBuilder] 삭제: {go.name}");
                Undo.DestroyObjectImmediate(go);
                removed++;
            }

            if (removed == 0)
            {
                Debug.Log($"[MainUIBuilder] 지울 '{RootName}' 이(가) 없습니다.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        private GameObject FindRoot()
        {
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
            {
                if (go.name == RootName) return go;
            }
            return null;
        }

        #endregion

        #region Canvas

        private Transform CreateCanvas()
        {
            var root = new GameObject(RootName, typeof(RectTransform));
            root.layer = UILayer;
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

#if UNITY_2022_2_OR_NEWER
            bool hasEventSystem = FindFirstObjectByType<EventSystem>() != null;
#else
            bool hasEventSystem = FindObjectOfType<EventSystem>() != null;
#endif
            if (!hasEventSystem)
            {
                Debug.LogWarning("[MainUIBuilder] 씬에 EventSystem 이 없습니다. 버튼/드래그 입력을 받으려면 추가하세요.");
            }

            return root.transform;
        }

        #endregion

        #region 인벤토리 (좌측 상단)

        private void CreateInventory(Transform canvas)
        {
            const float panelWidth = 460f;
            const float panelHeight = 940f;

            // 패널: 좌측 상단 고정 (기존 Equipment 자리부터 시작)
            GameObject panel = NewPanel("InventoryPanel", canvas, new Vector2(panelWidth, panelHeight));
            Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                  new Vector2(40f, -40f), new Vector2(panelWidth, panelHeight));
            panel.GetComponent<Image>().color = PanelColor;

            // ── 재화 표시 (패널 우측 상단)
            GameObject currency = NewPanel("CurrencyBar", panel.transform, new Vector2(170f, 36f));
            Place(currency, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                  new Vector2(-12f, -12f), new Vector2(170f, 36f));
            currency.GetComponent<Image>().color = SubPanelColor;

            GameObject coinIcon = NewPanel("CurrencyIcon", currency.transform, new Vector2(24f, 24f));
            Place(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                  new Vector2(8f, 0f), new Vector2(24f, 24f));
            Image coinImage = coinIcon.GetComponent<Image>();
            coinImage.color = CoinColor;
            coinImage.raycastTarget = false;

            TMP_Text currencyText = NewText("CurrencyText", currency.transform, "99,351", 22f, TextAlignmentOptions.Right);
            StretchWithOffset(currencyText.rectTransform, 40f, 10f, 0f, 0f);

            // ── 헤더: 제목 + 정렬 버튼
            GameObject header = NewUI("InventoryHeader", panel.transform);
            Place(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -56f), new Vector2(-24f, 40f));

            TMP_Text title = NewText("InventoryTitle", header.transform,
                                     $"Inventory ({InventoryUsed}/{InventoryCapacity})", 26f, TextAlignmentOptions.Left);
            StretchWithOffset(title.rectTransform, 0f, 110f, 0f, 0f);

            Button sortButton = NewButton("SortButton", header.transform, "Sort", out TMP_Text sortLabel);
            Place(sortButton.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                  Vector2.zero, new Vector2(100f, 36f));
            sortLabel.color = DarkTextColor;

            // ── 스크롤 영역
            GameObject scroll = NewUI("InventoryScroll", panel.transform);
            StretchWithOffset((RectTransform)scroll.transform, 12f, 12f, 104f, 12f);
            Image scrollHit = scroll.AddComponent<Image>();
            scrollHit.color = new Color(0f, 0f, 0f, 0f); // 투명이지만 드래그 입력을 받기 위해 필요

            GameObject viewport = NewUI("Viewport", scroll.transform);
            Stretch((RectTransform)viewport.transform, 0f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = NewUI("InventoryGrid", viewport.transform);
            Place(content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  Vector2.zero, Vector2.zero);

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.cellSize = new Vector2(InventoryCell, InventoryCell);
            grid.spacing = new Vector2(InventorySpacing, InventorySpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = InventoryColumns;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.content = (RectTransform)content.transform;
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // ── 슬롯 72칸 (앞 51칸은 아이템 있음 표시)
            for (int i = 0; i < InventoryCapacity; i++)
            {
                CreateItemSlot($"InventorySlot_{i:00}", content.transform,
                               new Vector2(InventoryCell, InventoryCell), i < InventoryUsed, i);
            }
        }

        #endregion

        #region 무게 (좌측 하단)

        private void CreateWeight(Transform canvas)
        {
            const float width = 460f;
            const float height = 36f;

            GameObject panel = NewPanel("WeightPanel", canvas, new Vector2(width, height));
            Place(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                  new Vector2(40f, 40f), new Vector2(width, height));
            panel.GetComponent<Image>().color = PanelColor;

            TMP_Text label = NewText("WeightLabel", panel.transform, "Weight", 20f, TextAlignmentOptions.Left);
            Place(label.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                  new Vector2(12f, 0f), new Vector2(80f, 30f));

            // 게이지 배경 (라벨과 수치 사이를 채움)
            GameObject gaugeBack = NewPanel("WeightGaugeBack", panel.transform, Vector2.zero);
            StretchWithOffset((RectTransform)gaugeBack.transform, 100f, 150f, 12f, 12f);
            Image backImage = gaugeBack.GetComponent<Image>();
            backImage.color = GaugeBackColor;
            backImage.raycastTarget = false;

            // 게이지 채움: anchorMax.x 를 비율로 사용 (로직에서 이 값만 바꾸면 됨)
            float ratio = Mathf.Clamp01(CurrentWeight / MaxWeight);
            GameObject gaugeFill = NewPanel("WeightGaugeFill", gaugeBack.transform, Vector2.zero);
            Place(gaugeFill, new Vector2(0f, 0f), new Vector2(ratio, 1f), new Vector2(0f, 0.5f),
                  Vector2.zero, Vector2.zero);
            Image fillImage = gaugeFill.GetComponent<Image>();
            fillImage.color = GaugeFillColor;
            fillImage.raycastTarget = false;

            TMP_Text value = NewText("WeightValueText", panel.transform,
                                     $"{CurrentWeight:0.0}/{MaxWeight:0.0}kg", 18f, TextAlignmentOptions.Right);
            Place(value.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                  new Vector2(-12f, 0f), new Vector2(130f, 30f));
        }

        #endregion

        #region 퀵슬롯 (하단 중앙)

        private void CreateHotbar(Transform canvas)
        {
            int count = HotbarKeys.Length;
            float rowInnerWidth = count * HotbarCell + (count - 1) * HotbarSpacing; // 648
            float rowWidth = rowInnerWidth + 32f;          // 좌우 패딩 16
            float rowHeight = 10f + HotbarCell + 30f;      // 위 패딩 + 슬롯 + 키 표시 공간
            const float hintHeight = 30f;
            float rootHeight = hintHeight + 8f + rowHeight;

            GameObject root = NewUI("Hotbar", canvas);
            Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                  new Vector2(0f, 24f), new Vector2(rowWidth, rootHeight));

            TMP_Text hint = NewText("HotbarHint", root.transform,
                                    "Drag items into slots to set up your hotbar.", 20f, TextAlignmentOptions.Center);
            Place(hint.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  Vector2.zero, new Vector2(0f, hintHeight));
            hint.color = SubTextColor;

            GameObject row = NewPanel("HotbarSlots", root.transform, new Vector2(rowWidth, rowHeight));
            Place(row, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                  Vector2.zero, new Vector2(rowWidth, rowHeight));
            row.GetComponent<Image>().color = PanelColor;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 30);
            layout.spacing = HotbarSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (string key in HotbarKeys)
            {
                GameObject slot = CreateItemSlot($"HotbarSlot_{key}", row.transform,
                                                 new Vector2(HotbarCell, HotbarCell), false, 0);

                // 슬롯 아래 키 표시
                GameObject badge = NewPanel("KeyBadge", slot.transform, new Vector2(28f, 22f));
                Place(badge, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                      new Vector2(0f, -4f), new Vector2(28f, 22f));
                Image badgeImage = badge.GetComponent<Image>();
                badgeImage.color = SubPanelColor;
                badgeImage.raycastTarget = false;

                TMP_Text keyText = NewText("KeyText", badge.transform, key, 16f, TextAlignmentOptions.Center);
                Stretch(keyText.rectTransform, 0f);
            }
        }

        #endregion

        #region 루팅 창 (우측 상단, 상자 열 때만 표시)

        private GameObject CreateLoot(Transform canvas)
        {
            int rows = Mathf.CeilToInt(LootCapacity / (float)LootColumns);
            float gridWidth = LootColumns * LootCell + (LootColumns - 1) * LootSpacing; // 392
            float gridHeight = rows * LootCell + (rows - 1) * LootSpacing;              // 192
            float boxWidth = gridWidth + 48f;                                             // 440
            float boxHeight = 12f + 36f + 8f + gridHeight + 20f;                          // 268
            const float titleHeight = 60f;

            GameObject root = NewUI("LootPanel", canvas);
            Place(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                  new Vector2(-40f, -200f), new Vector2(boxWidth, titleHeight + 8f + boxHeight));

            // 큰 제목
            TMP_Text title = NewText("LootTitle", root.transform,
                                     $"Loot ({LootUsed}/{LootCapacity})", 44f, TextAlignmentOptions.Left);
            Place(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  Vector2.zero, new Vector2(0f, titleHeight));
            title.fontStyle = FontStyles.Bold;

            // 슬롯 박스
            GameObject box = NewPanel("LootBox", root.transform, new Vector2(boxWidth, boxHeight));
            Place(box, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -(titleHeight + 8f)), new Vector2(0f, boxHeight));
            box.GetComponent<Image>().color = PanelColor;

            TMP_Text header = NewText("LootHeader", box.transform,
                                      $"Loot ({LootUsed}/{LootCapacity})", 22f, TextAlignmentOptions.Left);
            Place(header.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -12f), new Vector2(-48f, 36f));

            GameObject gridObj = NewUI("LootGrid", box.transform);
            Place(gridObj, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                  new Vector2(0f, -56f), new Vector2(gridWidth, gridHeight));

            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(LootCell, LootCell);
            grid.spacing = new Vector2(LootSpacing, LootSpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = LootColumns;

            for (int i = 0; i < LootCapacity; i++)
            {
                CreateItemSlot($"LootSlot_{i}", gridObj.transform,
                               new Vector2(LootCell, LootCell), i < LootUsed, i + 2);
            }

            return root;
        }

        #endregion

        #region 공용 슬롯

        /// <summary>
        /// 아이템 슬롯 1칸: 배경 + 아이콘 자리(색 블록) + 수량 텍스트.
        /// 레이아웃 그룹 아래에 놓이면 위치/크기는 그룹이 덮어쓴다.
        /// </summary>
        private GameObject CreateItemSlot(string name, Transform parent, Vector2 size, bool filled, int colorIndex)
        {
            GameObject slot = NewPanel(name, parent, size);
            Place(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                  Vector2.zero, size);
            slot.GetComponent<Image>().color = filled ? SlotFilledColor : SlotColor;

            GameObject icon = NewPanel("ItemIcon", slot.transform, size);
            float inset = Mathf.Round(size.x * 0.12f);
            StretchWithOffset((RectTransform)icon.transform, inset, inset, inset, inset);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.color = ItemPlaceholderColors[colorIndex % ItemPlaceholderColors.Length];
            iconImage.raycastTarget = false;
            iconImage.enabled = filled; // 빈 칸은 아이콘을 끈다

            TMP_Text countText = NewText("CountText", slot.transform, "", 14f, TextAlignmentOptions.BottomRight);
            Stretch(countText.rectTransform, 4f);

            return slot;
        }

        #endregion

        #region 도우미 메서드

        private GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            go.transform.SetParent(parent, false);
            return go;
        }

        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            ((RectTransform)go.transform).sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = PanelColor;
            return go;
        }

        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;

            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = TextColor;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            GameObject go = NewPanel(name, parent, new Vector2(160f, 40f));
            Image image = go.GetComponent<Image>();
            image.color = ButtonColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = NewText("Label", go.transform, label, 20f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);
            return button;
        }

        private void Stretch(RectTransform rect, float padding)
        {
            StretchWithOffset(rect, padding, padding, padding, padding);
        }

        /// <summary>부모를 꽉 채우되, 변마다 여백을 다르게 줄 때 사용.</summary>
        private void StretchWithOffset(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>anchorMin · anchorMax · pivot · anchoredPosition · sizeDelta 를 한 번에 명시.</summary>
        private RectTransform Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                    Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        #endregion

#endif
    }
}
