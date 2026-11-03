using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 화면 좌측 상단 상태 UI. 실행 시 코드로 만들어짐 (씬에 UI를 따로 만들 필요 없음)
    //  1) 전초기지 체력바 — 처음엔 숨김 → 플레이어가 BaseArrivalTrigger를 밟으면 Show()
    //  2) 폭탄 칸 (전초기지 밑) — 하이브 폭탄이 설치되어 있는 동안만 폭발까지 남은 시간 + 폭탄 체력 표시
    // 씬의 아무 오브젝트에나 붙임 (예: GameManager)
    public class OutpostBaseHealthUI : MonoBehaviour
    {
        [SerializeField] private OutpostBase outpost;   // 비우면 씬에서 자동으로 찾음

        [Header("표시")]
        [Tooltip("기지 도착 전이라도 기지가 맞으면 바로 표시")]
        [SerializeField] private bool showOnFirstDamage = true;
        [SerializeField] private string baseTitle = "전초기지";
        [Tooltip("비워두면 제목 없이 타이머만 왼쪽에 표시")]
        [SerializeField] private string bombTitle = "";

        [Header("모양")]
        [Tooltip("좌측 상단 기준 위치. 기존 TopLeft(공격 타이머, 낮/밤) 아래에 오도록 y를 내림")]
        [SerializeField] private Vector2 position = new(24f, -110f);
        [SerializeField] private Vector2 barSize = new(380f, 26f);
        [Tooltip("전초기지 칸과 폭탄 칸 사이 간격")]
        [SerializeField] private float sectionSpacing = 14f;
        [SerializeField] private Color backColor = new(0.08f, 0.06f, 0.12f, 0.85f);
        [SerializeField] private Color baseFullColor = new(0.3f, 0.75f, 1f);
        [SerializeField] private Color bombFullColor = new(1f, 0.75f, 0.2f);
        [SerializeField] private Color lowColor = new(0.92f, 0.28f, 0.33f);
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashTime = 0.15f;
        [Tooltip("남은 시간이 이 초 이하면 타이머를 빨갛게")]
        [SerializeField] private float timerWarnSeconds = 10f;
        [SerializeField] private TMP_FontAsset font;   // 한글 폰트

        // 칸 하나 (제목 + 오른쪽 정보 + 체력바 + 숫자)
        private class Section
        {
            public GameObject Root;
            public TMP_Text Info;          // 제목 오른쪽 (폭탄 타이머)
            public RectTransform Fill;
            public Image FillImage;
            public TMP_Text Value;
            public Color FullColor;
            public float Ratio = 1f;
            public float FlashTimer;
        }

        private Section baseSection;
        private Section bombSection;
        private bool baseShown;

        private HiveBomb trackedBomb;
        private float lastBombHealth;

        private void Awake()
        {
            if (outpost == null) outpost = FindAnyObjectByType<OutpostBase>();
            if (outpost == null) Debug.LogWarning("[OutpostBaseHealthUI] 씬에 OutpostBase가 없음");

            BuildUI();
            baseSection.Root.SetActive(false);
            bombSection.Root.SetActive(false);
        }

        private void OnEnable()
        {
            if (outpost != null) outpost.OnHealthChanged += HandleBaseHealthChanged;
        }

        private void OnDisable()
        {
            if (outpost != null) outpost.OnHealthChanged -= HandleBaseHealthChanged;
        }

        // BaseArrivalTrigger가 호출
        public void Show()
        {
            if (baseShown) return;
            baseShown = true;
            baseSection.Root.SetActive(true);
            if (outpost != null) SetBar(baseSection, outpost.CurrentHealth, outpost.MaxHealth);
            UpdateBombPosition();
        }

        private void HandleBaseHealthChanged(float current, float max)
        {
            if (!baseShown)
            {
                if (!showOnFirstDamage) return;
                Show();
            }

            SetBar(baseSection, current, max);
            baseSection.FlashTimer = hitFlashTime;
        }

        private void Update()
        {
            UpdateBomb();
            UpdateFlash(baseSection);
            UpdateFlash(bombSection);
        }

        // 폭탄은 이벤트가 없으니 매 프레임 상태를 읽음
        private void UpdateBomb()
        {
            HiveBomb bomb = HiveBomb.Active;
            bool show = bomb != null && bomb.IsAlive;

            if (bombSection.Root.activeSelf != show)
            {
                bombSection.Root.SetActive(show);
                UpdateBombPosition();
            }
            if (!show) { trackedBomb = null; return; }

            // 새 폭탄이면 깜빡임 기준값 초기화
            if (bomb != trackedBomb)
            {
                trackedBomb = bomb;
                lastBombHealth = bomb.CurrentHealth;
            }
            if (bomb.CurrentHealth < lastBombHealth) bombSection.FlashTimer = hitFlashTime;
            lastBombHealth = bomb.CurrentHealth;

            SetBar(bombSection, bomb.CurrentHealth, bomb.MaxHealth);

            int sec = Mathf.CeilToInt(bomb.TimeLeft);
            bombSection.Info.text = $"{sec / 60:00}:{sec % 60:00}";
            bombSection.Info.color = bomb.TimeLeft <= timerWarnSeconds ? lowColor : Color.white;
        }

        // 전초기지 칸이 숨겨져 있으면 폭탄 칸을 그 자리로 올림
        private void UpdateBombPosition()
        {
            var rect = (RectTransform)bombSection.Root.transform;
            float y = baseShown ? position.y - SectionHeight - sectionSpacing : position.y;
            rect.anchoredPosition = new Vector2(position.x, y);
        }

        private void SetBar(Section s, float current, float max)
        {
            s.Ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            s.Fill.anchorMax = new Vector2(s.Ratio, 1f);
            if (s.FlashTimer <= 0f) s.FillImage.color = Color.Lerp(lowColor, s.FullColor, s.Ratio);
            s.Value.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // 맞은 직후 잠깐 흰색으로 깜빡였다가 원래 색으로 (게임 정지 중에도 끝나도록 unscaled)
        private void UpdateFlash(Section s)
        {
            if (s.FlashTimer <= 0f) return;
            s.FlashTimer -= Time.unscaledDeltaTime;
            Color normal = Color.Lerp(lowColor, s.FullColor, s.Ratio);
            s.FillImage.color = Color.Lerp(normal, hitFlashColor, Mathf.Clamp01(s.FlashTimer / hitFlashTime));
        }

        #region UI 만들기

        private const float TitleHeight = 30f;
        private const float TitleGap = 4f;
        private float SectionHeight => TitleHeight + TitleGap + barSize.y;

        private void BuildUI()
        {
            var root = new GameObject("TopLeft Status UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // 건설 메뉴(50), 결과 화면(100)보다 아래
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            baseSection = CreateSection(root.transform, "Base", baseTitle, baseFullColor);
            ((RectTransform)baseSection.Root.transform).anchoredPosition = position;

            bombSection = CreateSection(root.transform, "Bomb", bombTitle, bombFullColor);
            UpdateBombPosition();
        }

        private Section CreateSection(Transform parent, string objName, string title, Color fullColor)
        {
            var s = new Section { FullColor = fullColor };

            RectTransform panel = CreateRect(objName, parent);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
            panel.sizeDelta = new Vector2(barSize.x, SectionHeight);
            s.Root = panel.gameObject;

            // 제목 (왼쪽)
            RectTransform titleRect = CreateRect("Title", panel);
            titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0f, 1f);
            titleRect.sizeDelta = new Vector2(barSize.x, TitleHeight);
            AddText(titleRect, title, 24f, TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;

            // 정보 (오른쪽, 폭탄 타이머)
            RectTransform infoRect = CreateRect("Info", panel);
            infoRect.anchorMin = infoRect.anchorMax = infoRect.pivot = new Vector2(0f, 1f);
            infoRect.sizeDelta = new Vector2(barSize.x, TitleHeight);
            // 제목이 없으면 타이머를 왼쪽에
            var infoAlign = string.IsNullOrEmpty(title) ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            s.Info = AddText(infoRect, "", 24f, infoAlign);
            s.Info.fontStyle = FontStyles.Bold;

            // 체력바 (배경 + 채움)
            RectTransform back = CreateRect("Back", panel);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 1f);
            back.anchoredPosition = new Vector2(0f, -(TitleHeight + TitleGap));
            back.sizeDelta = barSize;
            AddImage(back, backColor);

            s.Fill = CreateRect("Fill", back);
            Stretch(s.Fill);
            s.FillImage = AddImage(s.Fill, fullColor);

            // 숫자 (바 위에 겹침)
            RectTransform valueRect = CreateRect("Value", back);
            Stretch(valueRect);
            s.Value = AddText(valueRect, "", 18f, TextAlignmentOptions.Center);

            return s;
        }

        private TMP_Text AddText(RectTransform rect, string text, float size, TextAlignmentOptions align)
        {
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRect(string objName, Transform parent)
        {
            var go = new GameObject(objName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
