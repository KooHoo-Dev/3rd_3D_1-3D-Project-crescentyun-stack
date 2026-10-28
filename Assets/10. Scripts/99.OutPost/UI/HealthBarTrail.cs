using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    /// <summary>
    /// 체력 바 + 잔상(데미지 트레일) + 회복 미리보기 + 저체력 테두리 — UGUI 버전.
    /// 값은 직접 갖지 않고 PlayerHUD가 SetValue / Snap으로 넣어줌.
    /// Fill · HealPreview · Trail 세 Image 는 모두 Image Type = Filled, Fill Method = Horizontal.
    /// 그리는 순서(아래 → 위): Trail → HealPreview → Fill
    /// </summary>
    public class HealthBarTrail : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Image fill;
        [SerializeField] private Image trail;
        [SerializeField] private Image healPreview;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup vignette;   // 화면 전체 테두리 (비워두면 사용 안 함, Raycast Target 끔)

        [Header("연출")]
        [SerializeField, Tooltip("잔상이 줄어들기 시작할 때까지 대기 (초)")] private float trailDelay = 0.45f;
        [SerializeField, Tooltip("연타 중 잔상이 기다릴 수 있는 최대 시간 (초)")] private float maxHold = 1.2f;
        [SerializeField, Tooltip("잔상 감소 속도 (최대 체력 대비 비율/초)")] private float trailSpeed = 0.55f;
        [SerializeField, Tooltip("회복 채움 속도 (비율/초)")] private float healSpeed = 0.6f;
        [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.3f;
        

        private float maxHealth = 100f;   // SetValue / Snap으로 받음 (직접 수정 X)
        private float health = 1f;        // 실제 체력 비율 (0~1)
        private float shownFill = 1f;     // 화면의 메인 바
        private float shownTrail = 1f;    // 화면의 잔상
        private float holdTimer;
        private float heldTime;           // 이번 잔상이 지금까지 기다린 시간
        private float beatPhase;

        // PlayerHUD가 부르는 입구. 줄었는지 늘었는지 보고 연출을 고름
        public void SetValue(float current, float max)
        {
            if (health <= 0f) { Snap(current, max); return; }   // 죽어 있다가 늘어남 = 리스폰 → 즉시

            maxHealth = max;
            float target = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (target < health) ApplyDamage(target);
            else if (target > health) ApplyHeal(target);

            RefreshLabel();
        }

        // 연출 없이 즉시 맞춤 (시작할 때, 리스폰할 때)
        public void Snap(float current, float max)
        {
            maxHealth = max;
            health = shownFill = shownTrail = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            holdTimer = 0f;
            heldTime = 0f;

            Refresh();
            RefreshLabel();
        }

        private void ApplyDamage(float target)
        {
            float before = health;
            health = target;
            if (shownTrail <= shownFill + 0.0001f) heldTime = 0f;   // 잔상이 다 따라잡았으면 새로 시작
            shownFill = health;                                     // 메인 바는 즉시
            shownTrail = Mathf.Max(shownTrail, before);             // 잔상은 맞기 전 위치에 남긴다
            // 연타하면 대기 시간을 다시 채우되, 총 대기는 maxHold 를 넘지 않는다
            holdTimer = Mathf.Min(trailDelay, Mathf.Max(0f, maxHold - heldTime));
        }

        private void ApplyHeal(float target)
        {
            health = target;
            shownTrail = Mathf.Min(shownTrail, shownFill);
        }

        private void Update()
        {
            float dt = Time.deltaTime;   // 일시정지(timeScale 0) 중에도 움직이려면 Time.unscaledDeltaTime

            // 회복: 메인 바가 미리보기 구간을 천천히 채운다
            if (shownFill < health)
                shownFill = Mathf.MoveTowards(shownFill, health, healSpeed * dt);

            // 잔상: 대기 → 메인 바를 따라 감소
            if (holdTimer > 0f) { holdTimer -= dt; heldTime += dt; }
            else shownTrail = Mathf.MoveTowards(shownTrail, shownFill, trailSpeed * dt);

            // 저체력 맥박: 체력이 낮을수록 1Hz → 2Hz 로 빨라진다
            if (vignette != null)
            {
                float lowness = lowThreshold > 0f ? Mathf.Clamp01(1f - shownFill / lowThreshold) : 0f;
                beatPhase += dt * Mathf.Lerp(1f, 2f, lowness) * Mathf.PI * 2f;
                float pulse = 0.75f + 0.25f * Mathf.Sin(beatPhase);
                vignette.alpha = shownFill <= lowThreshold ? (0.35f + 0.65f * lowness) * pulse : 0f;
            }

            Refresh();
        }

        // 바 3개와 색 (매 프레임)
        private void Refresh()
        {
            fill.fillAmount = shownFill;
            trail.fillAmount = shownTrail;
            healPreview.fillAmount = health;   // Fill 아래에 깔려 있어 늘어날 구간만 보인다
           
        }

        // 숫자 텍스트 (값이 바뀔 때만)
        private void RefreshLabel()
        {
            if (label != null)
                label.text = $"{Mathf.CeilToInt(health * maxHealth)} / {maxHealth:0}";
        }

        // 테스트용 (플레이 모드에서 컴포넌트 ⋮ 메뉴로 실행)
        [ContextMenu("Test -20")] private void TestHit()  => SetValue(health * maxHealth - 20f, maxHealth);
        [ContextMenu("Test +30")] private void TestHeal() => SetValue(health * maxHealth + 30f, maxHealth);
    }
}