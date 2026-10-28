
using System;
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
        
        
        [Header("스태미나")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float runStaminaCost = 50f;   // 달릴 때 초당 소모
        [SerializeField] private float staminaRegen = 50f;     // 초당 회복
        [SerializeField] private float regenDelay = 1f;        // 마지막 사용 후 회복 시작까지 (초)
        [SerializeField] private float minStaminaToRun = 10f;  // 달리기를 새로 시작할 때 필요한 최소량
        
        
        
        private float stamina;
        private float lastStaminaUseTime;

        public float Stamina => stamina;
        public float MaxStamina => maxStamina;
        public event Action<float, float> OnStaminaChanged;   // (현재, 최대)
        
        
        
        [Header("Roll")]
        // 가로: 진행도(0~1), 세로: 이동한 비율(0~1). Inspector에서 모양을 직접 조절할 수 있음
        [SerializeField] private AnimationCurve rollCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1.6f),    // 시작: 빠르게
            new Keyframe(1f, 1f, 0.4f, 0f));   // 끝: 느리게 (0이면 완전히 멈춤)
        [SerializeField] private float rollDistance = 4f;    // 구르는 거리 (m)
        [SerializeField] private float rollDuration = 0.7f;  // 구르는 시간 (초), 클립 길이에 맞춰 조절
        [SerializeField] private float rollStamina = 20f; // 구르기 스태미나
        
        private int upperBodyLayer;   // UpperBody 레이어 번호
        
        private static readonly int RollHash  = Animator.StringToHash("Roll");
        private static readonly int RollXHash = Animator.StringToHash("RollX");
        private static readonly int RollZHash = Animator.StringToHash("RollZ");
        private static readonly int RollStateHash = Animator.StringToHash("Roll");   // State 이름
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        private static readonly int EmptyStateHash = Animator.StringToHash("Empty");   // UpperBody 레이어의 빈 상태
        private static readonly int FireStateHash = Animator.StringToHash("Fire");   // UpperBody 레이어의 사격 상태
        private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType"); 
        
        private static readonly int[] FireStateHashes =
        {
            Animator.StringToHash("FireRifle"),
            Animator.StringToHash("FirePistol"),
        };
        private static readonly int[] DrawStateHashes =
        {
            Animator.StringToHash("DrawRifle"),
            Animator.StringToHash("DrawPistol"),
        };
        private static readonly int[] PutBackStateHashes =
        {
            Animator.StringToHash("PutBackRifle"),
            Animator.StringToHash("PutBackPistol"),
        };
        
        
        private bool isRolling;
        private float rollTimer;
        private Vector3 rollDirection;   // 구르기 시작할 때 정한 월드 방향, 도중에는 바뀌지 않음

        public bool IsRolling => isRolling;   // 나중에 피격 스크립트에서 무적 판정용
        
        [Header("Quarter View")]
        [SerializeField] private float pitch = 45f;      // 내려다보는 각도 (클수록 위에서 수직으로 봄)
        [SerializeField] private float yaw = 45f;        // 비스듬한 방향 (45 = 대각선 쿼터뷰)
        [SerializeField] private float distance = 12f;   // 보는 크기
        [SerializeField] private float turnSpeed = 720f; // 초당 회전 각도
        
        
        [Header("무기 교체")]
        [SerializeField] private float putBackTime = 0.5f;   // 넣는 시간 (2-B에서 클립 길이에 맞춤)
        [SerializeField] private float drawTime = 0.5f;      // 꺼내는 시간

        private enum SwapPhase { None, PutBack, Draw }
        private SwapPhase swapPhase = SwapPhase.None;
        private float swapEndTime;    // 현재 단계가 끝나는 시각
        private int pendingSlot;      // 바꿀 예정인 슬롯

        public bool IsSwapping => swapPhase != SwapPhase.None;
        public event Action<int> OnSlotChanged;   // 새 슬롯 번호 (0 또는 1)
        
        
        
        
        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private Gun[] guns = new Gun[2];   // [0] 1번 슬롯, [1] 2번 슬롯

        private int currentSlot;
        public Gun CurrentGun => guns[currentSlot];
        public Gun[] Guns => guns;          // UI에서 슬롯별 탄약을 읽을 때 사용
        public int CurrentSlot => currentSlot;
        
        
        [Header("Animation")]
        [SerializeField] private float animDamp = 0.1f;   // 값이 바뀔 때 부드럽게 따라가는 시간

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

        private Vector3 moveVector;   // 이번 프레임의 월드 이동 방향 (길이 0 또는 1)
        private bool isRunning;
        
        private Camera cam;

        public static PlayerController Instance { get; private set; }
        
        private void Awake()
        {
            Instance = this;
            stamina = maxStamina;
        }

        private void OnEnable()
        {
            swapPhase = SwapPhase.None;   // 교체 도중에 죽었을 경우 정리
            SyncWeaponType();
        }

        private void Start()
        {
            upperBodyLayer = animator.GetLayerIndex("UpperBody");   // 레이어 이름으로 번호 찾기
            aimDirection = transform.forward;   // 커서 계산 전에도 0벡터가 되지 않도록
            cam = cameraTransform.GetComponent<Camera>();
            animator = GetComponentInChildren<Animator>();
            ConfineCursor();
            RefreshGunObjects();   // 두 총의 Awake가 끝난 뒤 1번 슬롯만 남김
            SyncWeaponType();   // 1번 슬롯에 권총을 넣어도 맞는 자세로 시작
        }

        private void Update()
        {
            ReadMoveInput();
            UpdateAimDirection();
            TryStartRoll();
            UpdateStamina();
            UpdateSwap();
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

            bool hasStamina = isRunning ? stamina > 0f : stamina >= minStaminaToRun;
            isRunning = moveVector.sqrMagnitude > 0.01f && SimpleInput.GetKey(Key.LeftShift) && hasStamina;
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
            if (IsSwapping) return false;       // 무기 교체 중
            if (CurrentGun.IsReloading) return false;  // 재장전 중
            // 몸이 아직 커서 쪽으로 다 돌지 않았으면 발사하지 않습니다.
            if (Vector3.Angle(transform.forward, aimDirection) > fireAngle) return false;
            return true;
        }

        private void UpdateGunInput()
        {
            // 달리기 시작하면 재장전 취소
            if (isRunning) CancelReload();
            if (SimpleInput.GetKeyDown(Key.Digit1)) EquipSlot(0);
            if (SimpleInput.GetKeyDown(Key.Digit2)) EquipSlot(1);
            
            // 재장전 (R) — 달리는 중에는 시작하지 않음
            
            if (SimpleInput.GetKeyDown(Key.R) && !isRunning && !IsSwapping && CurrentGun.RequestReload())
                animator.SetTrigger(ReloadHash);
            
            // 사격 (왼쪽 버튼을 누르고 있는 동안 연사)
            bool firePressed = CurrentGun.IsAutomatic
                ? SimpleInput.GetMouseButton(SimpleInput.MouseButton.Left)       // 연사: 누르고 있는 동안
                : SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left);  // 단발: 누른 순간만
            if (!firePressed) return;
            if (!CanFire()) return;

            // 발사 방향은 4단계에서 커서 방향으로 바꿉니다. 지금은 임시로 정면입니다.
            
            Debug.DrawRay(transform.position + Vector3.up, aimDirection * 5f, Color.red);
            
            // 실제로 발사됐을 때만 사격 모션을 처음(0)부터 재생
            if (CurrentGun.TryFire(aimDirection))
                animator.Play(FireStateHashes[CurrentGun.WeaponType], upperBodyLayer, 0f);
            
        }
        
        // 재장전 로직과 애니메이션을 함께 끊습니다.
        private void CancelReload()
        {
            if (!CurrentGun.IsReloading) return;

            CurrentGun.CancelReload();
            animator.ResetTrigger(ReloadHash);                         // 남아있는 트리거 제거
            animator.CrossFade(EmptyStateHash, 0.1f, upperBodyLayer);  // 상체를 0.1초 만에 Empty로
        }
        
        // 슬롯을 바꿉니다. 같은 슬롯이거나 비어 있으면 무시
        // 교체를 시작합니다. 실제 교체는 UpdateSwap에서 시간이 되면 일어남
        private void EquipSlot(int slot)
        {
            if (IsSwapping) return;
            if (slot == currentSlot) return;
            if (slot < 0 || slot >= guns.Length || guns[slot] == null) return;

            CancelReload();
            pendingSlot = slot;
            swapPhase = SwapPhase.PutBack;
            swapEndTime = Time.time + putBackTime;
            animator.CrossFade(PutBackStateHashes[CurrentGun.WeaponType], 0.1f, upperBodyLayer);
            // 2-B: 여기서 PutBack 애니메이션 재생
        }

        
        // 시간이 되면 다음 단계로 넘어갑니다. (재장전의 reloadEndTime과 같은 방식)
        private void UpdateSwap()
        {
            if (!IsSwapping) return;
            if (Time.time < swapEndTime) return;

            if (swapPhase == SwapPhase.PutBack)
            {
                // 넣기 끝 → 총 모델 교체 → 꺼내기 시작
                currentSlot = pendingSlot;
                RefreshGunObjects();
                OnSlotChanged?.Invoke(currentSlot);

                swapPhase = SwapPhase.Draw;
                swapEndTime = Time.time + drawTime;
                SyncWeaponType();
                animator.CrossFade(DrawStateHashes[CurrentGun.WeaponType], 0.1f, upperBodyLayer);
                // 2-B: 여기서 WeaponType 변경과 Draw 애니메이션 재생
            }
            else
            {
                swapPhase = SwapPhase.None;   // 꺼내기 끝 → 사격 가능
            }
        }

        private void CancelSwap()
        {
            if (!IsSwapping) return;
            swapPhase = SwapPhase.None;
            animator.CrossFade(EmptyStateHash, 0.1f, upperBodyLayer);   // 상체 자세 풀기
        }
        
        
        // 지금 슬롯의 총만 켜고 나머지는 끕니다.
        private void RefreshGunObjects()
        {
            for (int i = 0; i < guns.Length; i++)
                if (guns[i] != null) guns[i].gameObject.SetActive(i == currentSlot);
        }
        
        private void SyncWeaponType()
        {
            if (animator == null || CurrentGun == null) return;
            animator.SetInteger(WeaponTypeHash, CurrentGun.WeaponType);
        }
        
        
        #endregion
        
        #region 구르기
        
        private void TryStartRoll()
        {
           
            if (isRolling) return;
            if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == RollStateHash) return;   
            if (!SimpleInput.GetKeyDown(Key.Space)) return;
            if (stamina < rollStamina) return;   // 스태미나 부족
            
            
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

            CancelReload();
            CancelSwap();
            isRolling = true;
            rollTimer = 0f;
            ChangeStamina(-rollStamina);

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

        #region 스태미나

        // 스태미나를 바꾸는 곳은 반드시 이 함수 하나만 거치게 합니다.
        private void ChangeStamina(float amount)
        {
            float before = stamina;
            stamina = Mathf.Clamp(stamina + amount, 0f, maxStamina);
            if (amount < 0f) lastStaminaUseTime = Time.time;

            if (!Mathf.Approximately(before, stamina))
                OnStaminaChanged?.Invoke(stamina, maxStamina);
        }

        private void UpdateStamina()
        {
            if (isRolling) return;   // 구르는 중에는 소모도 회복도 없음

            if (isRunning)
            {
                ChangeStamina(-runStaminaCost * Time.deltaTime);
                return;
            }

            if (Time.time - lastStaminaUseTime < regenDelay) return;
            ChangeStamina(staminaRegen * Time.deltaTime);
        }

        #endregion
        
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        
        private void ConfineCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}