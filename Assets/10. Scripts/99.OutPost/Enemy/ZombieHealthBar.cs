using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    /// <summary>
    /// 좀비 머리 위 체력바. 좀비 프리팹 루트(EnemyHealth가 있는 곳)에 붙이기만 하면 UI를 코드로 만든다.
    /// - 처음 맞은 순간부터 표시
    /// - 죽으면 숨김
    /// - 플레이어 시야 밖이면 숨김 (VisionTarget은 Renderer만 끄기 때문에 UI는 따로 처리)
    /// - 항상 카메라 쪽을 바라봄 (빌보드)
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class ZombieHealthBar : MonoBehaviour
    {
        private const float PixelsPerUnit = 100f;   // UI 1픽셀 = 0.01m

        [Header("위치 / 크기 (월드 단위 m)")]
        [SerializeField, Tooltip("좀비 발밑 기준 높이")] private float height = 2.2f;
        [SerializeField] private Vector2 size = new Vector2(1f, 0.12f);
        [SerializeField, Tooltip("검은 테두리 두께")] private float border = 0.015f;

        [Header("색")]
        [SerializeField] private Color backColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color trailColor = new Color(1f, 0.85f, 0.55f, 1f);
        [SerializeField] private Color fullColor = new Color(0.4f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.15f, 1f);

        [Header("연출")]
        [SerializeField, Tooltip("잔상이 줄어들기 시작할 때까지 대기 (초)")] private float trailDelay = 0.3f;
        [SerializeField, Tooltip("잔상 감소 속도 (비율/초)")] private float trailSpeed = 1f;
        [SerializeField, Tooltip("마지막 피격 후 이 시간이 지나면 숨김. 0이면 계속 표시")] private float hideAfter = 0f;
        [SerializeField, Tooltip("시야 판정 높이 (VisionTarget의 checkHeight와 맞추기)")] private float visionCheckHeight = 1f;

        private EnemyHealth health;
        private Camera cam;

        private Canvas canvas;
        private Transform barRoot;
        private RectTransform fillRect;
        private RectTransform trailRect;
        private Image fillImage;

        private bool activated;         // 한 번이라도 맞았는가
        private float ratio = 1f;       // 실제 체력 비율 (0~1)
        private float shownTrail = 1f;  // 화면의 잔상 비율
        private float holdTimer;
        private float lastHitTime;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            BuildUI();
            SetShown(false);
        }

        private void OnEnable()
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
        }

        private void OnDestroy()
        {
            // 바는 좀비 자식이 아니라 따로 있으므로 같이 지운다 (좀비 크기에 영향받지 않게 분리)
            if (barRoot != null) Destroy(barRoot.gameObject);
        }

        private void HandleHealthChanged(float current, float max)
        {
            float newRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (!activated)
            {
                activated = true;
                shownTrail = ratio;   // 맞기 전 체력에서 잔상 시작
            }

            if (newRatio < ratio) holdTimer = trailDelay;

            ratio = newRatio;
            lastHitTime = Time.time;

            SetWidth(fillRect, ratio);
            fillImage.color = Color.Lerp(lowColor, fullColor, ratio);
        }

        private void HandleDeath()
        {
            activated = false;
            SetShown(false);
        }

        private void LateUpdate()
        {
            if (!activated) return;

            // 일정 시간 맞지 않으면 숨김 (hideAfter > 0 일 때만)
            bool timedOut = hideAfter > 0f && Time.time - lastHitTime > hideAfter;

            // 플레이어 시야 밖이면 숨김
            PlayerVision vision = PlayerVision.Instance;
            bool canSee = vision == null || vision.CanSee(transform.position + Vector3.up * visionCheckHeight);

            SetShown(!timedOut && canSee);
            if (!canvas.enabled) return;

            // 잔상: 대기 후 실제 체력까지 줄어듦
            if (holdTimer > 0f) holdTimer -= Time.deltaTime;
            else shownTrail = Mathf.MoveTowards(shownTrail, ratio, trailSpeed * Time.deltaTime);
            SetWidth(trailRect, shownTrail);

            // 머리 위 위치 + 카메라 바라보기
            if (cam == null) cam = Camera.main;
            barRoot.position = transform.position + Vector3.up * height;
            if (cam != null) barRoot.rotation = cam.transform.rotation;
        }

        private void SetShown(bool shown)
        {
            if (canvas.enabled != shown) canvas.enabled = shown;
        }

        // 막대 길이 = 오른쪽 앵커 위치
        private static void SetWidth(RectTransform rect, float value)
        {
            Vector2 max = rect.anchorMax;
            max.x = value;
            rect.anchorMax = max;
        }

        // ───────── UI 생성 ─────────
        // 구조: ZombieHealthBar(Canvas) ─ Back ─ Inner ┬ Trail
        //                                              └ Fill
        private void BuildUI()
        {
            GameObject root = new GameObject($"{name}_HealthBar", typeof(RectTransform));
            barRoot = root.transform;

            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = size * PixelsPerUnit;
            rootRect.localScale = Vector3.one / PixelsPerUnit;

            RectTransform back = CreateImage("Back", rootRect, backColor, 0f);
            RectTransform inner = CreateImage("Inner", back, Color.clear, border * PixelsPerUnit);
            trailRect = CreateImage("Trail", inner, trailColor, 0f);
            fillRect = CreateImage("Fill", inner, fullColor, 0f);
            fillImage = fillRect.GetComponent<Image>();

            inner.GetComponent<Image>().enabled = false;   // 빈 영역만 잡는 용도
        }

        private static RectTransform CreateImage(string objName, Transform parent, Color color, float padding)
        {
            GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        // 테스트용 (플레이 모드에서 컴포넌트 ⋮ 메뉴로 실행)
        [ContextMenu("Test Hit 20")]
        private void TestHit() => health.TakeDamage(20f, transform.position, Vector3.up);
    }
}
