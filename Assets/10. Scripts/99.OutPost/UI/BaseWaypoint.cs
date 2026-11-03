using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Study3D.OutPost
{
    // 네비게이션 마커 (Screen Space - Overlay Canvas 용). 기지, 하이브 등 대상마다 하나씩
    // 대상이 화면 안이면 대상 위에 아이콘, 화면 밖이면 화면 가장자리에 붙어서 화살표로 방향 표시
    // 거리는 플레이어 → 대상 기준
    // Start Hidden 체크 시 처음엔 숨김 → 다른 스크립트에서 Show() / Hide() 호출
    //
    // 구조:
    //   Canvas (Overlay)
    //    └ WaypointLayer   ← 화면 전체 Stretch, Pivot (0.5, 0.5)
    //       └ BaseMarker   ← 이 스크립트 + CanvasGroup, Anchor/Pivot 가운데
    //          ├ Icon
    //          ├ Arrow     ← 빈 RectTransform (가운데). 자식 화살표 이미지를 오른쪽(+X)에 배치, 오른쪽을 가리키게
    //          └ DistanceText (TMP)
    public class BaseWaypoint : MonoBehaviour
    {
        [Header("대상")]
        [Tooltip("비우면 씬에서 OutpostBase를 자동으로 찾음")]
        [FormerlySerializedAs("baseTarget")]   // 이름 바꾸기 전에 연결해 둔 값 유지
        [SerializeField] private Transform target;
        [Tooltip("비우면 Camera.main 사용")]
        [SerializeField] private Camera cam;

        [Header("UI")]
        [SerializeField] private RectTransform layer;          // 화면 전체를 덮는 RectTransform
        [SerializeField] private RectTransform arrow;          // 화면 밖일 때만 표시
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private CanvasGroup group;

        [Header("설정")]
        [SerializeField] private Vector3 worldOffset = new(0f, 3f, 0f);   // 대상 위 몇 m에 띄울지
        [Tooltip("화면 가장자리 여백 (캔버스 단위). 아래쪽은 핫바/체력바를 피하도록 크게")]
        [SerializeField] private float marginLeft = 80f;
        [SerializeField] private float marginRight = 80f;
        [SerializeField] private float marginTop = 80f;
        [SerializeField] private float marginBottom = 200f;
        [Tooltip("이 거리 안에서는 흐려짐 (m). 0이면 끔")]
        [SerializeField] private float fadeNear = 10f;
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.3f;

        [Header("표시")]
        [Tooltip("체크하면 처음엔 숨김. Show()가 호출되면 표시 (예: 하이브는 기지 도착 후)")]
        [SerializeField] private bool startHidden = false;

        private RectTransform rect;
        private bool shown;

        public bool IsShown => shown;

        public void Show() => shown = true;
        public void Hide() => shown = false;

        private void Awake()
        {
            rect = (RectTransform)transform;
            shown = !startHidden;
            group.alpha = 0f;   // 첫 계산 전 한 프레임 깜빡임 방지
        }

        private void Start()
        {
            if (target == null)
            {
                OutpostBase outpost = FindAnyObjectByType<OutpostBase>();
                if (outpost != null) target = outpost.transform;
                else Debug.LogWarning($"{name}: OutpostBase를 찾지 못함");
            }
        }

        private void LateUpdate()   // 카메라가 움직인 뒤에 계산 (PlayerController도 LateUpdate에서 카메라 갱신)
        {
            if (cam == null) cam = Camera.main;   // 플레이어가 런타임에 생성되므로 매번 확인
            PlayerController player = PlayerController.Instance;

            // 숨김 상태이거나 필요한 게 없으면 숨김
            bool ready = shown && target != null && cam != null && player != null;
            if (!ready)
            {
                group.alpha = 0f;
                return;
            }

            // ── 위치 ──
            Vector3 screen = cam.WorldToScreenPoint(target.position + worldOffset);
            bool behind = screen.z < 0f;   // 카메라 뒤면 좌표가 반전되어 나옴

            // 스크린 픽셀 → layer 로컬 좌표 (가운데가 0,0). Overlay Canvas는 카메라 인자 null
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
            if (behind) local = -local;

            // 여백을 뺀 사각형 (가운데 0,0 기준. 위아래 여백이 다르면 비대칭)
            Vector2 half = layer.rect.size * 0.5f;
            Vector2 min = new(-half.x + marginLeft, -half.y + marginBottom);
            Vector2 max = new(half.x - marginRight, half.y - marginTop);
            bool inside = !behind && local.x >= min.x && local.x <= max.x && local.y >= min.y && local.y <= max.y;

            if (inside)
            {
                rect.anchoredPosition = local;
            }
            else
            {
                Vector2 dir = local.sqrMagnitude > 0.01f ? local.normalized : Vector2.down;
                rect.anchoredPosition = ClampToEdge(dir, min, max);
                arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
            arrow.gameObject.SetActive(!inside);

            // ── 거리 (플레이어 → 대상, 높이 무시) ──
            Vector3 diff = target.position - player.transform.position;
            diff.y = 0f;
            float distance = diff.magnitude;
            distanceText.text = $"{distance:0}m";

            // ── 투명도: 화면 안 + 가까우면 흐리게 (항상 표시는 유지) ──
            group.alpha = inside && fadeNear > 0f
                ? Mathf.Max(minAlpha, Mathf.InverseLerp(fadeNear * 0.5f, fadeNear, distance))
                : 1f;
        }

        // 가운데(0,0)에서 dir 방향으로 뻗은 선이 여백 사각형과 만나는 점
        private static Vector2 ClampToEdge(Vector2 dir, Vector2 min, Vector2 max)
        {
            float tx = (dir.x >= 0f ? max.x : -min.x) / Mathf.Max(1e-4f, Mathf.Abs(dir.x));
            float ty = (dir.y >= 0f ? max.y : -min.y) / Mathf.Max(1e-4f, Mathf.Abs(dir.y));
            return dir * Mathf.Min(tx, ty);
        }
    }
}
