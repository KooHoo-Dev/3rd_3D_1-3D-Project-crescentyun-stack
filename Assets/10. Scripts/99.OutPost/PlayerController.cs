
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.OutPost
{
    // PlayerController
    // WASD 이동
    // 마우스 움직임 => 카메라 회전
    // 마우스 왼쪽 => 사격
    // Left Shift => 달리기
    // Space => 점프 (아직 미구현 추후에 추가)
    // R => 재장전
    
    
    
    public class PlayerController : MonoBehaviour
    {
        [Header("이동")]   
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float runSpeed = 7f;
        
        [Header("Quarter View")]
        [SerializeField] private float pitch = 45f;      // 내려다보는 각도 (클수록 위에서 수직으로 봄)
        [SerializeField] private float yaw = 45f;        // 비스듬한 방향 (45 = 대각선 쿼터뷰)
        [SerializeField] private float distance = 12f;   // 보는 크기
        [SerializeField] private float turnSpeed = 720f; // 초당 회전 각도
        
        
        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        
      
        private Camera cam;
        

        private void Start()
        {
            cam = cameraTransform.GetComponent<Camera>();
            ConfineCursor();
        }

        private void Update()
        {
            UpdatePosition();
            UpdateLookAtCursor();

        }

        private void LateUpdate()
        {
            UpdateCameraRotation();
        }

       
        private void UpdateCameraRotation()
        {
            cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
            cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
        }
        
        
        private void UpdatePosition()
        {
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            if (inputAxis.sqrMagnitude < 0.01f) return;
            
            // inputAxis를 3차원 입력 벡터로 바꿔줍니다.
            Vector3 inputVector = new Vector3(inputAxis.x, 0f, inputAxis.y);
            
            Quaternion cameraYaw = Quaternion.Euler(0f, yaw, 0f);
            Vector3 moveVector = (cameraYaw * inputVector).normalized;
            
            // 크기(속도)를 결정해 줍니다.
            float speed = SimpleInput.GetKey(Key.LeftShift) ? runSpeed : moveSpeed;
            // 내가 이번 프레임에 이동해야할 월드 벡터가 나옵니다.
            transform.position += moveVector * (speed * Time.deltaTime);

       
        }

       
        private void UpdateLookAtCursor()
        {
            Ray ray = cam.ScreenPointToRay(SimpleInput.GetMousePosition());
            Plane ground = new Plane(Vector3.up, transform.position);

            if (!ground.Raycast(ray, out float enter)) return;

            Vector3 lookDir = ray.GetPoint(enter) - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.01f) return; // 커서가 캐릭터 바로 위면 무시

            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
        
        private void ConfineCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}