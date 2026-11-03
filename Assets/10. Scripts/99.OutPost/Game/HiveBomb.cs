using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    // 하이브 옆에 설치하는 폭탄. TurretBuilder(B 메뉴)로 건설 → Init(TurretData)로 체력/이름을 받음
    // 설치되어 있는 동안(Active) 맵의 모든 좀비가 이 폭탄만 노림 (EnemyController.TryTargetBomb)
    // 타이머가 끝나면 하이브 폭파 → 연출 → GameManager.Win()
    // 좀비에게 부서지면 사라짐 → 재료를 모아 다시 건설
    //
    // IDamageable을 일부러 구현하지 않음 → 플레이어/포탑 총알에 맞아도 대미지 없음 (좀비만 직접 TakeDamage 호출)
    public class HiveBomb : MonoBehaviour
    {
        public static HiveBomb Active { get; private set; }

        [Header("폭탄")]
        [Tooltip("설치 후 폭발까지 시간(초)")]
        [SerializeField] private float fuseTime = 60f;
        [Tooltip("TurretData가 없을 때 쓰는 체력 (보통은 TurretData.MaxHealth 사용)")]
        [SerializeField] private float fallbackMaxHealth = 300f;

        [Header("연출")]
        [Tooltip("하이브 위치에 생성할 폭발 이펙트 (선택)")]
        [SerializeField] private GameObject explosionVFX;
        [Tooltip("폭탄이 좀비에게 부서질 때 이펙트 (선택)")]
        [SerializeField] private GameObject breakVFX;
        [Tooltip("폭발 후 승리 화면이 뜨기까지 시간(초)")]
        [SerializeField] private float winDelay = 2.5f;

        [Header("UI (머리 위 타이머 + 체력바)")]
        [SerializeField] private float uiHeight = 2f;
        [SerializeField] private Vector2 barSize = new(1.4f, 0.14f);
        [SerializeField] private Color backColor = new(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color fullColor = new(1f, 0.75f, 0.2f);
        [SerializeField] private Color lowColor = new(0.9f, 0.25f, 0.2f);
        [SerializeField] private TMP_FontAsset font;

        private TurretData data;
        private float maxHealth;
        private float currentHealth;
        private float timeLeft;
        private bool exploded;

        private Transform uiRoot;
        private RectTransform fillRect;
        private Image fillImage;
        private TMP_Text timerText;
        private Transform cam;

        public bool IsAlive => !exploded && currentHealth > 0f;
        public float TimeLeft => timeLeft;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        // TurretBuilder.Place에서 Instantiate 직후 호출 (Start보다 먼저)
        public void Init(TurretData newData)
        {
            data = newData;
        }

        private void Awake()
        {
            Active = this;   // 생성되는 즉시 좀비들이 노리기 시작
            BuildUI();
        }

        private void Start()
        {
            maxHealth = data != null ? data.MaxHealth : fallbackMaxHealth;
            currentHealth = maxHealth;
            timeLeft = fuseTime;
            if (Camera.main != null) cam = Camera.main.transform;

            RefreshUI();
            Debug.Log($"폭탄 설치! {fuseTime}초 동안 지키세요");
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Update()
        {
            if (exploded) return;

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                Explode();
                return;
            }
            RefreshUI();
        }

        private void LateUpdate()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam != null && uiRoot != null) uiRoot.rotation = cam.rotation;   // 항상 카메라를 향하게
        }

        // 좀비(EnemyController.TryHit)가 직접 호출
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            RefreshUI();

            if (currentHealth <= 0f) Break();
        }

        // 좀비에게 부서짐 → 다시 건설해야 함
        private void Break()
        {
            Active = null;
            if (breakVFX != null)
                Destroy(Instantiate(breakVFX, transform.position + Vector3.up, Quaternion.identity), 3f);

            Debug.Log("폭탄이 부서짐! 재료를 모아 다시 설치하세요");
            Destroy(gameObject);
        }

        private void Explode()
        {
            exploded = true;
            Active = null;   // 좀비들은 원래 행동으로

            // 폭탄 모습 숨김 (코루틴이 돌아야 하니 오브젝트는 남겨둠)
            if (uiRoot != null) uiRoot.gameObject.SetActive(false);
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Hive hive = Hive.Instance;
            Vector3 fxPos = hive != null ? hive.transform.position : transform.position;
            if (explosionVFX != null) Instantiate(explosionVFX, fxPos, Quaternion.identity);
            if (hive != null) hive.DestroyHive();

            StartCoroutine(WinAfterDelay());
        }

        private IEnumerator WinAfterDelay()
        {
            yield return new WaitForSeconds(winDelay);

            if (GameManager.Instance != null) GameManager.Instance.Win();
            else Debug.LogWarning("[HiveBomb] 씬에 GameManager가 없어서 승리 처리를 못 함");
        }

        #region UI

        private void RefreshUI()
        {
            if (fillRect == null) return;

            float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillImage.color = Color.Lerp(lowColor, fullColor, ratio);

            int sec = Mathf.CeilToInt(timeLeft);
            timerText.text = $"{sec / 60:00}:{sec % 60:00}";
        }

        // TurretHealthBar와 같은 방식의 월드 캔버스 (1 단위 = 1m)
        private void BuildUI()
        {
            var canvasObj = new GameObject("BombUI", typeof(RectTransform), typeof(Canvas));
            uiRoot = canvasObj.transform;
            uiRoot.SetParent(transform, false);
            uiRoot.localPosition = Vector3.up * uiHeight;
            canvasObj.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)uiRoot).sizeDelta = barSize;

            // 체력바
            Image back = CreateImage("Back", uiRoot, backColor);
            Stretch((RectTransform)back.transform);
            fillImage = CreateImage("Fill", back.transform, fullColor);
            fillRect = (RectTransform)fillImage.transform;
            Stretch(fillRect);

            // 타이머 (체력바 위)
            var textObj = new GameObject("Timer", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(uiRoot, false);
            timerText = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) timerText.font = font;
            timerText.fontSize = 0.4f;
            timerText.color = Color.white;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.textWrappingMode = TextWrappingModes.NoWrap;
            var textRect = (RectTransform)textObj.transform;
            textRect.sizeDelta = new Vector2(2f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 0.35f);
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
