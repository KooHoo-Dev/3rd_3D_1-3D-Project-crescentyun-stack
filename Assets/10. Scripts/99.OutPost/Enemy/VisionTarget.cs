using UnityEngine;

namespace Study3D.OutPost
{
    // 플레이어 시야 밖이면 모델을 숨김 (적, 나중엔 아이템에도 사용 가능)
    public class VisionTarget : MonoBehaviour
    {
        [SerializeField] private float checkHeight = 1f;   // 판정 기준 높이 (몸 중앙)

        private Renderer[] renderers;
        private bool isVisible = true;

        private void Awake()
        {
            // 자식 모델, 무기까지 전부 (그림자도 같이 꺼져서 그림자로 위치가 들키지 않음)
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            PlayerVision vision = PlayerVision.Instance;
            bool canSee = vision == null || vision.CanSee(transform.position + Vector3.up * checkHeight);

            if (canSee == isVisible) return;   // 상태가 바뀔 때만 처리
            SetVisible(canSee);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            foreach (Renderer r in renderers)
                r.enabled = visible;
        }
    }
}