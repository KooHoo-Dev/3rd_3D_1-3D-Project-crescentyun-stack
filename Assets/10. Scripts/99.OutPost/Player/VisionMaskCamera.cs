using UnityEngine;

namespace Study3D.OutPost
{
    // 시야 메시만 찍어서 흑백 마스크 텍스처(_VisionMask)를 만드는 카메라
    // 메인 카메라의 자식으로 두고 Position/Rotation은 (0,0,0)
    [RequireComponent(typeof(Camera))]
    public class VisionMaskCamera : MonoBehaviour
    {
        [SerializeField] private Camera mainCam;
        [SerializeField, Range(1, 4)] private int downSample = 2;   // 2 = 화면 절반 해상도 (가볍고 경계도 살짝 부드러워짐)

        // 셰이더에서 이 이름으로 텍스처를 읽음
        private static readonly int VisionMaskId = Shader.PropertyToID("_VisionMask");

        private Camera maskCam;
        private RenderTexture maskTexture;

        private void Awake()
        {
            maskCam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            // 메인 카메라와 화각이 같아야 마스크가 화면에 정확히 겹침
            maskCam.orthographic     = mainCam.orthographic;
            maskCam.fieldOfView      = mainCam.fieldOfView;
            maskCam.orthographicSize = mainCam.orthographicSize;
            maskCam.nearClipPlane    = mainCam.nearClipPlane;
            maskCam.farClipPlane     = mainCam.farClipPlane;

            EnsureTexture();
        }

        // 화면 크기가 바뀌면 텍스처를 다시 만듦
        private void EnsureTexture()
        {
            int w = Mathf.Max(1, Screen.width  / downSample);
            int h = Mathf.Max(1, Screen.height / downSample);
            if (maskTexture != null && maskTexture.width == w && maskTexture.height == h) return;

            if (maskTexture != null) maskTexture.Release();
            maskTexture = new RenderTexture(w, h, 16, RenderTextureFormat.R8) { name = "VisionMask" };   // R8 = 흑백 한 채널

            maskCam.targetTexture = maskTexture;
            Shader.SetGlobalTexture(VisionMaskId, maskTexture);   // 모든 셰이더에서 _VisionMask로 접근 가능
        }

        private void OnDestroy()
        {
            if (maskTexture != null) maskTexture.Release();
        }
    }
}