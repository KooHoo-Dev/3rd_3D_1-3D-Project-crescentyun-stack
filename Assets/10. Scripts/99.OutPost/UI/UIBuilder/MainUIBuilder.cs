using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;  
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#endif

namespace Study3D.OutPost
{
    /// <summary>
    /// 메인 HUD를 코드로 조립하는 에디터 전용 빌더.
    /// 빈 GameObject에 붙이고 컴포넌트 우클릭 메뉴로 실행합니다.
    /// </summary>
    public class MainUIBuilder : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string RootName = "Main UI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        [SerializeField] private TMP_FontAsset font; // 한글 폰트 교체용
        
        // Build 마지막에 숨길 오브젝트 목록
        private readonly List<GameObject> hideOnStart = new List<GameObject>();
        
        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            // 이미 루트가 있으면 겹쳐 만들지 않음
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[MainUIBuilder] '{RootName}' 이(가) 이미 있습니다. Clear 후 다시 실행하세요.");
                return;
            }

            hideOnStart.Clear();

            // Canvas 생성
            Transform canvas = CreateCanvas();

            // 상단 좌측
            CreateTopLeft(canvas);

            // 하단 중앙
            CreateBottomCenter(canvas);

            // 처음에 숨겨야 할 요소는 마지막에 비활성화
            foreach (GameObject go in hideOnStart)
                go.SetActive(false);

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[MainUIBuilder] Build 완료");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            // 우리가 만든 루트 이름만 삭제
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log($"[MainUIBuilder] 삭제할 '{RootName}' 이(가) 없습니다.");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[MainUIBuilder] '{RootName}' 삭제 완료");
        }
        
        #region Canvas

        /// <summary>루트 Canvas 생성 (Overlay + ScaleWithScreenSize)</summary>
        private Transform CreateCanvas()
        {
            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            Undo.RegisterCreatedObjectUndo(root, "Build Main UI");

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // EventSystem 존재 여부 확인
#if UNITY_2023_1_OR_NEWER
            bool hasEventSystem = FindFirstObjectByType<EventSystem>() != null;
#else
            bool hasEventSystem = FindObjectOfType<EventSystem>() != null;
#endif
            if (!hasEventSystem)
                Debug.LogWarning("[MainUIBuilder] 씬에 EventSystem이 없습니다. (GameObject > UI > Event System 으로 추가)");

            return root.transform;
        }

        #endregion
        
        #region TopLeft

        // 낮/밤 색상 (로직에서 바꿔 쓸 수 있도록 참고값)
        private static readonly Color DayColor   = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color NightColor = new Color(0.65f, 0.5f, 1f, 1f);

        /// <summary>상단 좌측: 기지 공격 타이머 + 낮/밤 표시</summary>
        private void CreateTopLeft(Transform canvas)
        {
            // 컨테이너 (좌상단 고정)
            GameObject topLeft = NewUI("TopLeft", canvas);
            SetRect((RectTransform)topLeft.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -16f), new Vector2(400f, 80f));

            // 기지 공격까지 남은 시간
            TMP_Text timer = NewText("RaidTimerText", topLeft.transform, "01:29", 64f, TextAlignmentOptions.Left);
            timer.fontStyle = FontStyles.Bold;
            SetRect(timer.rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(200f, 80f));

            // 낮/밤 그룹 (아이콘 + 텍스트)
            GameObject dayNight = NewUI("DayNightGroup", topLeft.transform);
            SetRect((RectTransform)dayNight.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(210f, -8f), new Vector2(150f, 32f));

            HorizontalLayoutGroup layout = dayNight.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 낮/밤 아이콘 자리 (스프라이트 대신 색 블록)
            GameObject icon = NewPanel("DayNightIcon", dayNight.transform, new Vector2(24f, 24f));
            icon.GetComponent<Image>().color = NightColor;
            icon.GetComponent<Image>().raycastTarget = false;
            SetRect((RectTransform)icon.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(24f, 24f));

            // 낮/밤 텍스트
            TMP_Text dayNightText = NewText("DayNightText", dayNight.transform, "Night", 26f, TextAlignmentOptions.Left);
            dayNightText.color = NightColor;
            SetRect(dayNightText.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(110f, 32f));
        }

        #endregion
        
        #region BottomCenter

        // 게이지 색상
        private static readonly Color BarBgColor   = new Color(0.08f, 0.06f, 0.12f, 0.85f);
        private static readonly Color HealthColor  = new Color(0.92f, 0.28f, 0.33f, 1f);
        private static readonly Color HungerColor  = new Color(0.95f, 0.62f, 0.2f, 1f);
        private static readonly Color StaminaColor = new Color(0.55f, 0.88f, 0.3f, 1f);

        /// <summary>하단 중앙: 상태 게이지 + 장비 슬롯 + 퀵슬롯</summary>
        private void CreateBottomCenter(Transform canvas)
        {
            // 컨테이너 (하단 중앙 고정)
            GameObject bottom = NewUI("BottomCenter", canvas);
            SetRect((RectTransform)bottom.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(1200f, 110f));

            HorizontalLayoutGroup layout = bottom.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 자식 너비 합에 맞춰 가로 크기 자동 조절 → 항상 화면 중앙 정렬
            ContentSizeFitter fitter = bottom.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            
            CreateStatusGroup(bottom.transform);
            CreateEquipSlots(bottom.transform);
            CreateQuickSlots(bottom.transform);      // ← 교체
        }

        /// <summary>체력바 + (배고픔/스태미나) 2줄 게이지</summary>
        private void CreateStatusGroup(Transform parent)
        {
            GameObject group = NewUI("StatusGroup", parent);
            SetRect((RectTransform)group.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(420f, 40f));

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 하트 아이콘 자리
            GameObject heart = NewPanel("HealthIcon", group.transform, new Vector2(28f, 28f));
            heart.GetComponent<Image>().color = HealthColor;
            heart.GetComponent<Image>().raycastTarget = false;
            SetRect((RectTransform)heart.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(28f, 28f));

            // 체력바 + 수치 텍스트
            GameObject healthBar = NewBar("HealthBar", group.transform, new Vector2(220f, 30f), HealthColor);
            TMP_Text healthText = NewText("HealthText", healthBar.transform, "50 / 50", 20f, TextAlignmentOptions.Center);
            healthText.fontStyle = FontStyles.Bold;
            Stretch(healthText.rectTransform, 0f);

            // 배고픔(위) / 스태미나(아래) 세로 묶음
            GameObject sub = NewUI("SubGauges", group.transform);
            SetRect((RectTransform)sub.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(140f, 30f));

            VerticalLayoutGroup vLayout = sub.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 6f;
            vLayout.childAlignment = TextAnchor.MiddleLeft;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;

            NewBar("HungerBar",  sub.transform, new Vector2(140f, 12f), HungerColor);
            NewBar("StaminaBar", sub.transform, new Vector2(140f, 12f), StaminaColor);
        }

        /// <summary>
        /// 가로 게이지 (Background + Fill).
        /// 채움 비율은 Fill의 anchorMax.x (0~1)로 조절한다.
        /// </summary>
        private GameObject NewBar(string name, Transform parent, Vector2 size, Color fillColor)
        {
            GameObject bar = NewUI(name, parent);
            SetRect((RectTransform)bar.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size);

            // 배경 (테두리 역할)
            GameObject bg = NewPanel("Background", bar.transform, size);
            bg.GetComponent<Image>().color = BarBgColor;
            bg.GetComponent<Image>().raycastTarget = false;
            Stretch((RectTransform)bg.transform, 0f);

            // 채움 (안쪽 2px 여백, 오른쪽 끝 = anchorMax.x)
            GameObject fill = NewPanel("Fill", bar.transform, size);
            fill.GetComponent<Image>().color = fillColor;
            fill.GetComponent<Image>().raycastTarget = false;
            Stretch((RectTransform)fill.transform, 2f);

            return bar;
        }

        
                // 슬롯 색상
        private static readonly Color SlotBgColor     = new Color(0.12f, 0.1f, 0.18f, 0.85f);
        private static readonly Color HighlightColor  = new Color(0.95f, 0.9f, 0.75f, 1f);
        private static readonly Color GunIconColor    = new Color(0.55f, 0.56f, 0.62f, 1f);
        private static readonly Color MeleeIconColor  = new Color(0.72f, 0.56f, 0.35f, 1f);
        private static readonly Color KeyLabelBgColor = new Color(0.2f, 0.18f, 0.26f, 0.9f);

        private const float SlotSize = 72f;

        /// <summary>장착 장비 슬롯 3개 (1 · 2 · V)</summary>
        private void CreateEquipSlots(Transform parent)
        {
            GameObject group = NewUI("EquipSlots", parent);
            SetRect((RectTransform)group.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(SlotSize * 3f + 8f * 2f, SlotSize));

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            //            이름               키    아이콘 색       탄약     선택됨
            NewEquipSlot(group.transform, "EquipSlot_1",     "1", GunIconColor,   "30/90", false, "Rifle");
            NewEquipSlot(group.transform, "EquipSlot_2",     "2", GunIconColor,   "30/54", true,  "Rusty Bullet (AR)");
            NewEquipSlot(group.transform, "EquipSlot_Melee", "V", MeleeIconColor, null,    false, "Bat");
        }

        /// <summary>장비 슬롯 하나 생성 (ammo가 null이면 탄약 텍스트 없음)</summary>
        private void NewEquipSlot(Transform parent, string name, string key, Color iconColor,
                                  string ammo, bool selected, string weaponName)
        {
            // 슬롯 루트 (이미지 없음, 크기 기준용)
            GameObject slot = NewUI(name, parent);
            SetRect((RectTransform)slot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(SlotSize, SlotSize));

            // 선택 테두리 (배경보다 먼저 = 뒤에 그려짐)
            GameObject highlight = NewPanel("Highlight", slot.transform, Vector2.zero);
            highlight.GetComponent<Image>().color = HighlightColor;
            highlight.GetComponent<Image>().raycastTarget = false;
            Stretch((RectTransform)highlight.transform, -3f);   // 바깥으로 3px
            if (!selected) hideOnStart.Add(highlight);

            // 배경
            GameObject bg = NewPanel("Background", slot.transform, Vector2.zero);
            bg.GetComponent<Image>().color = SlotBgColor;
            Stretch((RectTransform)bg.transform, 0f);

            // 아이콘 자리
            GameObject icon = NewPanel("Icon", slot.transform, Vector2.zero);
            icon.GetComponent<Image>().color = iconColor;
            icon.GetComponent<Image>().raycastTarget = false;
            Stretch((RectTransform)icon.transform, 14f);

            // 탄약 (슬롯 안 위쪽)
            if (ammo != null)
            {
                TMP_Text ammoText = NewText("AmmoText", slot.transform, ammo, 16f, TextAlignmentOptions.Center);
                SetRect(ammoText.rectTransform,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -2f), new Vector2(SlotSize, 18f));
            }

            // 키 라벨 (슬롯 아래)
            NewKeyLabel(slot.transform, key);

            // 무기 이름 툴팁 (슬롯 위)
            GameObject tooltip = NewPanel("WeaponNameTooltip", slot.transform, new Vector2(200f, 30f));
            tooltip.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            tooltip.GetComponent<Image>().raycastTarget = false;
            SetRect((RectTransform)tooltip.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                new Vector2(0f, 10f), new Vector2(200f, 30f));

            TMP_Text tipText = NewText("TooltipText", tooltip.transform, weaponName, 18f, TextAlignmentOptions.Center);
            Stretch(tipText.rectTransform, 4f);

            if (!selected) hideOnStart.Add(tooltip);
        }

        /// <summary>슬롯 아래 키 라벨 (6단계 퀵슬롯에서도 사용)</summary>
        private void NewKeyLabel(Transform slot, string key)
        {
            GameObject label = NewPanel("KeyLabel", slot, new Vector2(22f, 22f));
            label.GetComponent<Image>().color = KeyLabelBgColor;
            label.GetComponent<Image>().raycastTarget = false;
            SetRect((RectTransform)label.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -4f), new Vector2(22f, 22f));

            TMP_Text keyText = NewText("KeyText", label.transform, key, 15f, TextAlignmentOptions.Center);
            Stretch(keyText.rectTransform, 0f);
        }
        
        
                private static readonly Color ItemIconColor = new Color(0.85f, 0.3f, 0.32f, 1f);

        /// <summary>퀵슬롯 4칸 (3 · 4 · 5 · 6)</summary>
        private void CreateQuickSlots(Transform parent)
        {
            const int count = 4;
            const float spacing = 8f;

            GameObject group = NewUI("QuickSlots", parent);
            SetRect((RectTransform)group.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(SlotSize * count + spacing * (count - 1), SlotSize));

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < count; i++)
            {
                int key = 3 + i;   // 3 ~ 6번 키
                bool hasItem = key == 5;   // 예시: 5번에 구급상자
                NewQuickSlot(group.transform, key, hasItem ? "First Aid Kit" : null, hasItem ? "x2" : null);
            }
        }

        /// <summary>퀵슬롯 하나 생성 (itemName이 null이면 빈 슬롯)</summary>
        private void NewQuickSlot(Transform parent, int key, string itemName, string countText)
        {
            bool isEmpty = itemName == null;

            GameObject slot = NewUI($"QuickSlot_{key}", parent);
            SetRect((RectTransform)slot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(SlotSize, SlotSize));

            // 배경
            GameObject bg = NewPanel("Background", slot.transform, Vector2.zero);
            bg.GetComponent<Image>().color = SlotBgColor;
            Stretch((RectTransform)bg.transform, 0f);

            // 아이콘 자리
            GameObject icon = NewPanel("Icon", slot.transform, Vector2.zero);
            icon.GetComponent<Image>().color = ItemIconColor;
            icon.GetComponent<Image>().raycastTarget = false;
            SetRect((RectTransform)icon.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 6f), new Vector2(36f, 36f));

            // 아이템 이름 (슬롯 안 아래쪽)
            TMP_Text nameText = NewText("ItemNameText", slot.transform, itemName ?? "", 12f, TextAlignmentOptions.Center);
            SetRect(nameText.rectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 3f), new Vector2(SlotSize - 4f, 16f));

            // 수량 (슬롯 안 우상단)
            TMP_Text amount = NewText("CountText", slot.transform, countText ?? "", 14f, TextAlignmentOptions.Right);
            SetRect(amount.rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-4f, -2f), new Vector2(40f, 18f));

            // 키 라벨 (슬롯 아래)
            NewKeyLabel(slot.transform, key.ToString());

            // 빈 슬롯은 내용물 숨김
            if (isEmpty)
            {
                hideOnStart.Add(icon);
                hideOnStart.Add(nameText.gameObject);
                hideOnStart.Add(amount.gameObject);
            }
        }
        
        
        #endregion
        
        #region Helpers

        /// <summary>RectTransform만 가진 빈 UI 오브젝트</summary>
        private GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Image가 붙은 패널 (기본: 반투명 어두운 색)</summary>
        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            RectTransform rect = (RectTransform)go.transform;
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f);
            return go;
        }

        /// <summary>TextMeshProUGUI 텍스트 (font 필드 적용)</summary>
        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;       // 텍스트는 클릭을 막지 않도록
            tmp.enableWordWrapping = false;  // TMP 버전에 따라 경고가 나오면 textWrappingMode로 교체
            return tmp;
        }

        /// <summary>라벨이 있는 버튼</summary>
        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            GameObject go = NewPanel(name, parent, new Vector2(160f, 50f));
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            labelText = NewText("Label", go.transform, label, 24f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);
            return button;
        }

        /// <summary>부모를 꽉 채우기 (padding만큼 안쪽 여백)</summary>
        private void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>앵커·피벗·위치·크기를 한 번에 명시적으로 설정 (추가 도우미)</summary>
        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                             Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        #endregion
        
#endif
    }
}