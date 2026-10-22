
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study3D.OutPost
{
    // PlayerController
    // WASD 이동
    // 마우스 움직임 => 캐릭터가 바라보는 방향
    // 마우스 왼쪽 => 사격
    // Left Shift => 달리기
    // Space => 구르기
    // R => 재장전
    
    
    
    public class PlayerController : MonoBehaviour
    {
        [Header("사격")]
        [SerializeField] private float fireAngle = 10f;   // 몸과 커서 방향이 이 각도 이내일 때만 발사

        private Vector3 aimDirection;   // 캐릭터에서 커서를 향하는 수평 방향 (길이 1)
        
        [Header("이동")]   
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float runSpeed = 7f;
        
        [Header("Roll")]
        // 가로: 진행도(0~1), 세로: 이동한 비율(0~1). Inspector에서 모양을 직접 조절할 수 있음
        [SerializeField] private AnimationCurve rollCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1.6f),    // 시작: 빠르게
            new Keyframe(1f, 1f, 0.4f, 0f));   // 끝: 느리게 (0이면 완전히 멈춤)
        [SerializeField] private float rollDistance = 4f;    // 구르는 거리 (m)
        [SerializeField] private float rollDuration = 0.7f;  // 구르는 시간 (초), 클립 길이에 맞춰 조절

        private static readonly int RollHash  = Animator.StringToHash("Roll");
        private static readonly int RollXHash = Animator.StringToHash("RollX");
        private static readonly int RollZHash = Animator.StringToHash("RollZ");
        private static readonly int RollStateHash = Animator.StringToHash("Roll");   // State 이름
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        
        private bool isRolling;
        private float rollTimer;
        private Vector3 rollDirection;   // 구르기 시작할 때 정한 월드 방향, 도중에는 바뀌지 않음

        public bool IsRolling => isRolling;   // 나중에 피격 스크립트에서 무적 판정용
        
        [Header("Quarter View")]
        [SerializeField] private float pitch = 45f;      // 내려다보는 각도 (클수록 위에서 수직으로 봄)
        [SerializeField] private float yaw = 45f;        // 비스듬한 방향 (45 = 대각선 쿼터뷰)
        [SerializeField] private float distance = 12f;   // 보는 크기
        [SerializeField] private float turnSpeed = 720f; // 초당 회전 각도
        
        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private Gun gun;
        
        
        [Header("Animation")]
        [SerializeField] private float animDamp = 0.1f;   // 값이 바뀔 때 부드럽게 따라가는 시간

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

        private Vector3 moveVector;   // 이번 프레임의 월드 이동 방향 (길이 0 또는 1)
        private bool isRunning;
        
        private Camera cam;
         

        private void Start()
        {
            aimDirection = transform.forward;   // 커서 계산 전에도 0벡터가 되지 않도록
            cam = cameraTransform.GetComponent<Camera>();
            animator = GetComponentInChildren<Animator>();
            ConfineCursor();
        }

        private void Update()
        {
            ReadMoveInput();
            UpdateAimDirection();
            TryStartRoll();

            if (isRolling)
            {
                UpdateRoll();
                return;
            }

            UpdatePosition();
            UpdateRotation();
            UpdateAnimation();
            UpdateGunInput();
            
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
        
        private void ReadMoveInput()
        {
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            Vector3 inputVector = new Vector3(inputAxis.x, 0f, inputAxis.y);

            Quaternion cameraYaw = Quaternion.Euler(0f, yaw, 0f);
            moveVector = (cameraYaw * inputVector).normalized;   // 입력이 없으면 (0,0,0)

            isRunning = moveVector.sqrMagnitude > 0.01f && SimpleInput.GetKey(Key.LeftShift);
        }

        #region 이동 관련
        
        private void UpdatePosition()
        {
            if (moveVector.sqrMagnitude < 0.01f) return;

            float speed = isRunning ? runSpeed : moveSpeed;
            transform.position += moveVector * (speed * Time.deltaTime);
        }

        private void UpdateRotation()
        {
            if (isRunning)
                RotateTowards(moveVector);   // 달리는 중: 이동 방향을 바라봄
            else
                UpdateLookAtCursor();        // 평소: 마우스를 바라봄
        }
        
        
        private void UpdateAnimation()
        {
            Vector3 animDir;

            if (isRunning)
                animDir = new Vector3(0f, 0f, 2f);   // 항상 앞으로 달리기
            else
                animDir = transform.InverseTransformDirection(moveVector);   // 캐릭터 기준 방향

            animator.SetFloat(MoveXHash, animDir.x, animDamp, Time.deltaTime);
            animator.SetFloat(MoveZHash, animDir.z, animDamp, Time.deltaTime);
        }
        
        private void RotateTowards(Vector3 dir)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
       
        // 커서가 가리키는 바닥 지점을 향한 방향을 계산해 저장합니다.
        private void UpdateAimDirection()
        {
            Ray ray = cam.ScreenPointToRay(SimpleInput.GetMousePosition());
            Plane ground = new Plane(Vector3.up, transform.position);

            if (!ground.Raycast(ray, out float enter)) return;

            Vector3 dir = ray.GetPoint(enter) - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;   // 커서가 캐릭터 바로 위면 이전 방향 유지

            aimDirection = dir.normalized;
        }

        private void UpdateLookAtCursor()
        {
            RotateTowards(aimDirection);
        }

        #endregion
        
        #region 사격
        
        private bool CanFire()
        {
            if (isRolling) return false;        // 구르는 중
            if (isRunning) return false;        // 달리는 중
            if (gun.IsReloading) return false;  // 재장전 중
            // 몸이 아직 커서 쪽으로 다 돌지 않았으면 발사하지 않습니다.
            if (Vector3.Angle(transform.forward, aimDirection) > fireAngle) return false;
            return true;
        }

        private void UpdateGunInput()
        {
            // 재장전 (R)
            if (SimpleInput.GetKeyDown(Key.R) && gun.RequestReload())
                animator.SetTrigger(ReloadHash);

            // 사격 (왼쪽 버튼을 누르고 있는 동안 연사)
            if (!SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left)) return;
            if (!CanFire()) return;

            // 발사 방향은 4단계에서 커서 방향으로 바꿉니다. 지금은 임시로 정면입니다.
            
            Debug.DrawRay(transform.position + Vector3.up, aimDirection * 5f, Color.red);
            
            gun.TryFire(aimDirection);
            
        }
        
        #endregion
        
        #region 구르기
        
        private void TryStartRoll()
        {
            if (isRolling) return;
            if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == RollStateHash) return;   
            if (!SimpleInput.GetKeyDown(Key.Space)) return;

            // 입력이 없으면 바라보는 방향(앞)으로
            Vector3 worldDir = moveVector.sqrMagnitude > 0.01f ? moveVector : transform.forward;

            // 캐릭터 기준 방향으로 바꾼 뒤 4방향 중 가까운 쪽으로 맞춤
            Vector3 local = transform.InverseTransformDirection(worldDir);
            Vector3 snapped = Mathf.Abs(local.x) > Mathf.Abs(local.z)
                ? new Vector3(Mathf.Sign(local.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(local.z));

            rollDirection = transform.TransformDirection(snapped);   // 다시 월드 방향으로

            animator.SetFloat(RollXHash, snapped.x);   // 구르기 방향은 Damp 없이 즉시 넣기
            animator.SetFloat(RollZHash, snapped.z);
            animator.SetTrigger(RollHash);

            isRolling = true;
            rollTimer = 0f;
        }
        
        private void UpdateRoll()
        {
            float prevT = rollTimer / rollDuration;
            rollTimer += Time.deltaTime;
            float t = Mathf.Clamp01(rollTimer / rollDuration);

            // 이번 프레임에 곡선에서 진행한 만큼만 이동
            float delta = rollCurve.Evaluate(t) - rollCurve.Evaluate(prevT);
            transform.position += rollDirection * (rollDistance * delta);

            UpdateAnimation();   // ① 걷기 값 미리 갱신

            if (rollTimer >= rollDuration)
                isRolling = false;
        }
        
        #endregion
        
        
        private void ConfineCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}