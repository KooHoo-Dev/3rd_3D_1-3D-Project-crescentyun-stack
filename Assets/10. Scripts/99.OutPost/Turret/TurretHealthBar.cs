using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 포탑 위에 뜨는 체력바. 실행하면 코드로 월드 캔버스를 만들어 붙임 (UI 따로 만들 필요 없음)
    // 파괴되면 체력바 대신 "[N] 수리" 안내와 필요 재료를 보여줌
    public class TurretHealthBar : MonoBehaviour
    {
        [SerializeField] private TurretHealth health;   // 비우면 부모에서 찾음

        [Header("모양")]
        [SerializeField] private float height = 2.2f;   // 포탑 바닥에서 체력바까지 높이 (m)
        [SerializeField] private Vector2 size = new(1.2f, 0.12f);
        [SerializeField] private Color backColor = new(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color fullColor = new(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color lowColor = new(0.9f, 0.25f, 0.2f);
        [SerializeField] private TMP_FontAsset font;     // 한글 폰트 (비우면 기본 폰트 → 한글이 깨질 수 있음)

        private Transform root;
        private GameObject barObject;
        private RectTransform fillRect;
        private Image fillImage;
        private TMP_Text label;
        private Transform cam;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<TurretHealth>();
            Build();
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.OnHealthChanged += Refresh;
            health.OnDestroyed += RefreshState;
            health.OnRepaired += RefreshState;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.OnHealthChanged -= Refresh;
            health.OnDestroyed -= RefreshState;
            health.OnRepaired -= RefreshState;
        }

        private void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            RefreshState();
        }

        private void LateUpdate()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam != null) root.rotation = cam.rotation;   // 항상 카메라를 향하게

            // 파괴 상태에서는 재료가 바뀔 수 있으니 매 프레임 안내 갱신
            if (health != null && health.IsDestroyed) UpdateRepairLabel();
        }

        private void Refresh(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillImage.color = Color.Lerp(lowColor, fullColor, ratio);
        }

        private void RefreshState()
        {
            bool destroyed = health != null && health.IsDestroyed;
            barObject.SetActive(!destroyed);
            label.gameObject.SetActive(destroyed);
            if (destroyed) UpdateRepairLabel();
            else if (health != null) Refresh(health.CurrentHealth, health.MaxHealth);
        }

        private void UpdateRepairLabel()
        {
            TurretData data = health.Data;
            bool canAfford = TurretCost.CanAfford(data, true);
            label.text = $"[N] Repair\n{TurretCost.Describe(data, true)}";
            label.color = canAfford ? Color.white : lowColor;
        }

        #region UI 만들기

        private void Build()
        {
            // 월드 공간 캔버스 (1 단위 = 1m)
            var canvasObj = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
            root = canvasObj.transform;
            root.SetParent(transform, false);
            root.localPosition = Vector3.up * height;

            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)root;
            canvasRect.sizeDelta = size;

            // 배경 + 채움 (스프라이트 없이 앵커 크기로 비율 표시)
            barObject = CreateImage("Back", root, backColor).gameObject;
            Stretch((RectTransform)barObject.transform);

            fillImage = CreateImage("Fill", barObject.transform, fullColor);
            fillRect = (RectTransform)fillImage.transform;
            Stretch(fillRect);

            // 파괴 시 수리 안내
            var labelObj = new GameObject("RepairLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(root, false);
            label = labelObj.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 0.25f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var labelRect = (RectTransform)labelObj.transform;
            labelRect.sizeDelta = new Vector2(3f, 0.8f);
            labelObj.SetActive(false);
        }

        private static Image CreateImage(string objName, Transform parent, Color color)
        {
            var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
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
