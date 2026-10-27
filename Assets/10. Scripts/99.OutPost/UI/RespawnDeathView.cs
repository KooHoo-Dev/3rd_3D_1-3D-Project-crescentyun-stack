using UnityEngine;
using UnityEngine.UI;

namespace Study3D.OutPost
{
    /// <summary>
    /// 사망 시 죽은 캐릭터를 위에서 비추는 전용 카메라를 만들고,
    /// 그 화면(RenderTexture)을 Respawn UI 의 DeathViewImage(RawImage)에 띄운다.
    /// RenderTexture·카메라는 모두 런타임에 코드로 생성하므로 에셋이 필요 없다.
    /// </summary>
    public class RespawnDeathView : MonoBehaviour
    {
        #region 인스펙터

        [Header("UI")]
        [Tooltip("Respawn UI > CenterColumn > DeathViewImage 를 연결")]
        [SerializeField] private RawImage targetImage;

        [Header("탑다운 카메라 구도")]
        [Tooltip("캐릭터 위 몇 m 높이에서 볼지")]
        [SerializeField] private float height = 6f;
        [Tooltip("살짝 비스듬히 보이게 뒤로 빼는 거리 (0이면 완전 수직)")]
        [SerializeField] private float backOffset = 2f;
        [Tooltip("카메라가 바라볼 지점의 높이 보정 (캐릭터 발밑 기준)")]
        [SerializeField] private float lookAtHeight = 0.3f;

        [Header("카메라 렌즈")]
        [SerializeField] private bool useOrthographic = true;
        [Tooltip("Orthographic 일 때 화면 세로 절반 크기 (작을수록 확대)")]
        [SerializeField] private float orthographicSize = 1.6f;
        [Tooltip("Perspective 일 때 시야각")]
        [SerializeField] private float fieldOfView = 30f;

        [Header("렌더링")]
        [Tooltip("이 카메라가 그릴 레이어 (캐릭터만 보이게 하려면 해당 레이어만 체크)")]
        [SerializeField] private LayerMask cullingMask = ~0;
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        [Tooltip("RenderTexture 세로 해상도 (가로는 DeathViewImage 비율로 자동 계산)")]
        [SerializeField] private int textureHeight = 540;

        #endregion

        private Camera deathCamera;
        private RenderTexture renderTexture;

        private void Start()
        {
            if (targetImage == null) return;
            EnsureTexture();
            EnsureCamera();   // 만들어지기만 하고 enabled = false 상태로 대기
        }
        
        #region 공개 메서드

        /// <summary>사망 화면 표시. 죽은 캐릭터의 Transform 을 넘긴다.</summary>
        public void Show(Transform deadCharacter)
        {
            if (targetImage == null)
            {
                Debug.LogWarning("[RespawnDeathView] targetImage 가 연결되지 않았습니다.");
                return;
            }
            if (deadCharacter == null)
            {
                Debug.LogWarning("[RespawnDeathView] 죽은 캐릭터 Transform 이 null 입니다.");
                return;
            }

            EnsureTexture();
            EnsureCamera();

            // 탑다운 위치 잡기 (월드 기준: 위 + 살짝 뒤에서 내려다봄)
            Vector3 focus = deadCharacter.position + Vector3.up * lookAtHeight;
            deathCamera.transform.position = focus + Vector3.up * height - Vector3.forward * backOffset;
            deathCamera.transform.LookAt(focus);

            deathCamera.enabled = true;
            targetImage.texture = renderTexture;
            targetImage.color = Color.white;
        }

        /// <summary>사망 화면 끄기 (리스폰/계속하기 시 호출)</summary>
        public void Hide()
        {
            if (deathCamera != null) deathCamera.enabled = false;
            if (targetImage != null) targetImage.texture = null;
        }

        #endregion

        #region 내부 생성

        /// <summary>DeathViewImage 비율에 맞는 RenderTexture 를 한 번만 생성</summary>
        private void EnsureTexture()
        {
            if (renderTexture != null) return;

            Rect rect = targetImage.rectTransform.rect;
            float aspect = rect.height > 0f ? rect.width / rect.height : 710f / 267f;
            int width = Mathf.Max(16, Mathf.RoundToInt(textureHeight * aspect));

            renderTexture = new RenderTexture(width, textureHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "DeathView_RT",
                antiAliasing = 2
            };
            renderTexture.Create();
        }

        /// <summary>사망 연출 전용 카메라를 한 번만 생성 (플레이어 자식이 아니라 독립 오브젝트)</summary>
        private void EnsureCamera()
        {
            if (deathCamera != null) return;

            var camGo = new GameObject("DeathCamera");
           
            deathCamera = camGo.AddComponent<Camera>();

            deathCamera.clearFlags = CameraClearFlags.SolidColor;
            deathCamera.backgroundColor = backgroundColor;
            deathCamera.cullingMask = cullingMask;
            deathCamera.orthographic = useOrthographic;
            deathCamera.orthographicSize = orthographicSize;
            deathCamera.fieldOfView = fieldOfView;
            deathCamera.nearClipPlane = 0.1f;
            deathCamera.farClipPlane = height + backOffset + 20f;
            deathCamera.targetTexture = renderTexture;
            deathCamera.depth = -10f;   // 메인 카메라보다 먼저 그림
            deathCamera.enabled = false;
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (deathCamera != null) Destroy(deathCamera.gameObject);   // ← 추가
        }

        #endregion

        #region 테스트

        [ContextMenu("Test Show (Player 태그)")]
        private void TestShow()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[RespawnDeathView] 'Player' 태그 오브젝트가 없습니다.");
                return;
            }
            Show(player.transform);
        }

        [ContextMenu("Test Hide")]
        private void TestHide() => Hide();

        #endregion
    }
}
