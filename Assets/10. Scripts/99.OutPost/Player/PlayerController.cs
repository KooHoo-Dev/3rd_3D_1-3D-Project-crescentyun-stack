
using System;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Study3D.OutPost
{
    // PlayerController
    // WASD 이동
    // 마우스 움직임 => 캐릭터가 바라보는 방향
    // 마우스 왼쪽 => 사격 / 근접 공격 (V 상태)
    // Left Shift => 달리기
    // Space => 구르기
    // R => 재장전
    // 1 / 2 => 총 교체, V => 근접무기(총이 없을 때 기본 상태)
    // 3 ~ 6 => 소모품 사용
    // 총은 Hotbar(PlayerHotbar)에 장착된 아이템의 프리팹을 손에 생성해서 씀
    // 근접무기는 Hotbar V 칸 아이템의 MeleePrefab을 손(meleeSocket)에 생성해서 씀
    
    
    
    public class PlayerController : MonoBehaviour
    {
        [Header("사격")]
        [SerializeField] private float fireAngle = 10f;   // 몸과 커서 방향이 이 각도 이내일 때만 발사

        private Vector3 aimDirection;   // 캐릭터에서 커서를 향하는 수평 방향 (길이 1)
        public Vector3 AimDirection => aimDirection;   // 시야(PlayerVision)에서 읽음
        
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

        // 테스트용: true면 스태미나가 줄지 않음 (DebugCheats의 F1)
        public bool InfiniteStamina { get; private set; }
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

        // 근접무기 (UpperBody 레이어 State 이름 — Player.controller의 이름과 정확히 같아야 함)
        private static readonly int DrawMeleeStateHash = Animator.StringToHash("Draw2HandMelee");
        private static readonly int PutBackMeleeStateHash = Animator.StringToHash("PutBack2HandMelee");
        private static readonly int MeleeAttackStateHash = Animator.StringToHash("AttackMelee");
        
        
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
        public event Action<int> OnSlotChanged;   // 새 슬롯 번호 (0, 1 = 총 / 2 = 근접무기)
        public event Action OnGunsChanged;        // Hotbar 변경으로 총이 새로 생성/제거/교환됨 (HUD 갱신용)
        
        
        
        
        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [Tooltip("Hotbar에 넣은 총 프리팹이 생성될 손 위치 (비우면 아래 '미리 배치한 총'의 부모를 사용)")]
        [SerializeField] private Transform weaponSocket;
        [Tooltip("(이전 방식) 씬에 미리 배치한 총. 손 위치를 찾는 데만 쓰고 시작할 때 제거됩니다.")]
        [FormerlySerializedAs("guns")]
        [SerializeField] private Gun[] placedGuns = new Gun[0];
        [Tooltip("근접무기 프리팹이 생성될 손 위치 (비우면 Weapon Socket 사용)")]
        [SerializeField] private Transform meleeSocket;

        public const int MeleeSlot = 2;   // V 칸 (기본 근접무기, 총이 없을 때 들고 있는 상태)

        private readonly Gun[] guns = new Gun[2];                    // [0] 1번 칸, [1] 2번 칸 — Hotbar에서 생성
        private readonly ItemStack[] gunStacks = new ItemStack[2];   // 각 총을 만든 아이템 묶음 (탄창 수 저장용)
        private MeleeWeapon melee;   // V 칸 근접무기 — Hotbar에서 생성 (없으면 맨손)
        private PlayerHotbar hotbar;
        private PlayerHealth health;

        private static readonly Key[] ConsumableKeys = { Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };

        private int currentSlot = MeleeSlot;
        public Gun CurrentGun => currentSlot < guns.Length ? guns[currentSlot] : null;   // 근접무기 상태면 null
        public Gun[] Guns => guns;          // UI에서 슬롯별 탄약을 읽을 때 사용
        public int CurrentSlot => currentSlot;
        public MeleeWeapon Melee => melee;
        public bool IsMeleeAttacking => melee != null && melee.IsAttacking;
        private bool IsMeleeSlot => currentSlot == MeleeSlot;
        
        
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
            health = GetComponent<PlayerHealth>();
            RemovePlacedGuns();
        }

        private void OnEnable()
        {
            swapPhase = SwapPhase.None;   // 교체 도중에 죽었을 경우 정리
            if (melee != null) melee.CancelAttack();
            SyncWeaponType();
        }

        private void Start()
        {
            upperBodyLayer = animator.GetLayerIndex("UpperBody");   // 레이어 이름으로 번호 찾기
            aimDirection = transform.forward;   // 커서 계산 전에도 0벡터가 되지 않도록
            cam = cameraTransform.GetComponent<Camera>();
            animator = GetComponentInChildren<Animator>();
            ConfineCursor();
            ConnectHotbar();       // Hotbar에 장착된 총 / 근접무기 생성 (시작은 근접무기 상태)
            RefreshWeaponObjects();
            SyncWeaponType();
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
            if (CurrentGun == null) return false;      // 근접무기 상태
            if (CurrentGun.IsReloading) return false;  // 재장전 중
            // 몸이 아직 커서 쪽으로 다 돌지 않았으면 발사하지 않습니다.
            if (Vector3.Angle(transform.forward, aimDirection) > fireAngle) return false;
            return true;
        }

        private void UpdateGunInput()
        {
            // 인벤토리가 열려 있으면 클릭이 사격으로 나가지 않게 막음
            if (InventorySystem.IsInitialized && InventorySystem.Instance.IsOpen) return;
            // 포탑 건설 메뉴/배치 중이면 클릭이 사격으로 나가지 않게 막음
            if (TurretBuilder.IsBusy) return;
            // 게임이 끝났으면(승리 화면) 입력 무시
            if (GameManager.IsGameOver) return;

            // 달리기 시작하면 재장전 / 근접 공격 취소
            if (isRunning)
            {
                CancelReload();
                CancelMeleeAttack();
            }
            if (SimpleInput.GetKeyDown(Key.Digit1)) EquipSlot(0);
            if (SimpleInput.GetKeyDown(Key.Digit2)) EquipSlot(1);
            if (SimpleInput.GetKeyDown(Key.V)) EquipSlot(MeleeSlot);
            UpdateConsumableInput();

            // 근접무기 상태: 좌클릭으로 공격
            if (CurrentGun == null)
            {
                UpdateMeleeInput();
                return;
            }
            
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
            if (CurrentGun.TryFire(aimDirection) && CurrentGun.WeaponType < FireStateHashes.Length)
                animator.Play(FireStateHashes[CurrentGun.WeaponType], upperBodyLayer, 0f);
            
        }
        
        // 재장전 로직과 애니메이션을 함께 끊습니다.
        private void CancelReload()
        {
            if (CurrentGun == null || !CurrentGun.IsReloading) return;

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
            if (slot < 0 || slot > MeleeSlot) return;
            if (slot < guns.Length && guns[slot] == null) return;   // 비어 있는 총 칸

            CancelReload();
            CancelMeleeAttack();
            pendingSlot = slot;
            swapPhase = SwapPhase.PutBack;
            // 들고 있는 무기를 넣는 동작 (맨손이면 바로 꺼내기 단계로)
            swapEndTime = Time.time + CurrentPutBackTime();
            CrossFadeUpper(PutBackStateHashes, PutBackMeleeStateHash);
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
                if (currentSlot < guns.Length && guns[currentSlot] == null) currentSlot = MeleeSlot;   // 교체 중 총이 빠졌으면
                RefreshWeaponObjects();
                OnSlotChanged?.Invoke(currentSlot);

                swapPhase = SwapPhase.Draw;
                swapEndTime = Time.time + CurrentDrawTime();
                SyncWeaponType();
                CrossFadeUpper(DrawStateHashes, DrawMeleeStateHash);
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
        
        
        // 지금 슬롯의 무기만 켜고 나머지는 끕니다. (근접무기 포함)
        private void RefreshWeaponObjects()
        {
            for (int i = 0; i < guns.Length; i++)
                if (guns[i] != null) guns[i].gameObject.SetActive(i == currentSlot);

            if (melee != null) melee.gameObject.SetActive(IsMeleeSlot);
        }
        
        // Animator WeaponType: 총이면 총 값, V 상태면 근접무기 값
        // (예전에는 총이 없으면 return해서 V로 바꿔도 이전 총 자세가 남았음)
        private void SyncWeaponType()
        {
            if (animator == null) return;

            if (CurrentGun != null) animator.SetInteger(WeaponTypeHash, CurrentGun.WeaponType);
            else if (IsMeleeSlot && melee != null) animator.SetInteger(WeaponTypeHash, melee.WeaponType);
        }

        // 지금 들고 있는 무기에 맞는 상체 동작
        // 총: 종류별 State / 근접무기: meleeStateHash / 둘 다 아니면 Empty
        private void CrossFadeUpper(int[] gunStateHashes, int meleeStateHash)
        {
            if (animator == null) return;

            int target = EmptyStateHash;
            Gun gun = CurrentGun;
            if (gun != null)
            {
                if (gun.WeaponType >= 0 && gun.WeaponType < gunStateHashes.Length)
                    target = gunStateHashes[gun.WeaponType];
            }
            else if (IsMeleeSlot && melee != null)
            {
                target = meleeStateHash;
            }

            animator.CrossFade(target, 0.1f, upperBodyLayer);
        }

        private float CurrentPutBackTime()
        {
            if (CurrentGun != null) return putBackTime;
            if (IsMeleeSlot && melee != null) return melee.PutBackTime;
            return 0f;
        }

        private float CurrentDrawTime()
        {
            if (CurrentGun != null) return drawTime;
            if (IsMeleeSlot && melee != null) return melee.DrawTime;
            return 0f;
        }
        
        #endregion

        #region 근접 공격

        private bool CanMeleeAttack()
        {
            if (melee == null) return false;     // 근접무기가 없음 (맨손)
            if (!IsMeleeSlot) return false;
            if (isRolling) return false;         // 구르는 중
            if (isRunning) return false;         // 달리는 중
            if (IsSwapping) return false;        // 무기 교체 중
            return true;
        }

        private void UpdateMeleeInput()
        {
            if (!SimpleInput.GetMouseButtonDown(SimpleInput.MouseButton.Left)) return;
            if (!CanMeleeAttack()) return;

            // 공격 방향은 지금 커서 방향으로 바로 맞춤 (판정은 transform.forward 기준)
            transform.rotation = Quaternion.LookRotation(aimDirection);

            // 실제로 공격이 시작됐을 때만 공격 모션을 처음(0)부터 재생
            if (melee.TryAttack())
                animator.Play(MeleeAttackStateHash, upperBodyLayer, 0f);
        }

        // 근접 공격 로직과 애니메이션을 함께 끊습니다.
        private void CancelMeleeAttack()
        {
            if (melee == null || !melee.IsAttacking) return;

            melee.CancelAttack();
            animator.CrossFade(EmptyStateHash, 0.1f, upperBodyLayer);
        }
        
        #endregion

        #region Hotbar 연동 (총 생성 / 소모품)

        // 씬에 미리 배치했던 총은 손 위치만 알려주고 제거 (이제 Hotbar에서 생성)
        private void RemovePlacedGuns()
        {
            if (placedGuns != null)
            {
                foreach (Gun placed in placedGuns)
                {
                    if (placed == null) continue;
                    if (weaponSocket == null) weaponSocket = placed.transform.parent;
                    Destroy(placed.gameObject);
                }
            }

            if (weaponSocket == null)
                Debug.LogWarning("[PlayerController] Weapon Socket이 비어 있어 총을 손에 생성할 수 없습니다.", this);
        }

        private void ConnectHotbar()
        {
            if (!InventorySystem.IsInitialized || InventorySystem.Instance.Hotbar == null)
            {
                Debug.LogWarning("[PlayerController] PlayerHotbar를 찾지 못해 근접무기 상태로만 동작합니다.", this);
                return;
            }

            hotbar = InventorySystem.Instance.Hotbar;
            hotbar.OnSlotChanged += HandleHotbarSlotChanged;
            hotbar.OnWeaponsSwapped += HandleWeaponsSwapped;

            SpawnMelee();
            for (int i = 0; i < guns.Length; i++) SyncGun(i);
        }

        // V 칸(고정)의 근접무기 프리팹을 손에 생성합니다. V 칸은 바뀌지 않으므로 시작할 때 한 번만
        private void SpawnMelee()
        {
            ItemStack stack = hotbar.Get(MeleeSlot);
            MeleeWeapon prefab = stack != null && stack.Data != null ? stack.Data.MeleePrefab : null;
            Transform socket = meleeSocket != null ? meleeSocket : weaponSocket;

            if (prefab == null)
            {
                Debug.LogWarning("[PlayerController] V 칸 아이템에 Melee Prefab이 없어 맨손 상태로 동작합니다.", this);
                return;
            }
            if (socket == null)
            {
                Debug.LogWarning("[PlayerController] Melee Socket / Weapon Socket이 비어 있어 근접무기를 생성할 수 없습니다.", this);
                return;
            }

            melee = Instantiate(prefab, socket, false);
            melee.Init(transform);
        }

        private void HandleHotbarSlotChanged(int index)
        {
            if (PlayerHotbar.IsWeaponSlot(index)) SyncGun(index);
        }

        // Hotbar 칸의 아이템과 손의 총을 맞춥니다. (같은 묶음이면 그대로 둠)
        private void SyncGun(int i)
        {
            ItemStack stack = hotbar != null ? hotbar.Get(i) : null;
            if (gunStacks[i] == stack) return;

            // 기존 총 정리: 탄창 수를 아이템 묶음에 저장하고 제거
            if (guns[i] != null)
            {
                if (i == currentSlot)
                {
                    CancelReload();
                    CancelSwap();
                }
                if (gunStacks[i] != null) gunStacks[i].Ammo = guns[i].CurrentAmmo;
                Destroy(guns[i].gameObject);
                guns[i] = null;
            }

            gunStacks[i] = stack;

            // 새 총 생성: 손 위치에 프리팹을 만들고 저장돼 있던 탄창 수로 맞춤
            if (stack != null && stack.Data != null && stack.Data.GunPrefab != null && weaponSocket != null)
            {
                Gun gun = Instantiate(stack.Data.GunPrefab, weaponSocket, false);
                gun.SetMagazine(stack.Ammo);
                guns[i] = gun;
            }

            // 들고 있던 총이 빠졌으면 근접무기 상태로
            if (i == currentSlot && guns[i] == null)
            {
                currentSlot = MeleeSlot;
                OnSlotChanged?.Invoke(currentSlot);
                CrossFadeUpper(DrawStateHashes, DrawMeleeStateHash);
            }

            RefreshWeaponObjects();
            SyncWeaponType();
            OnGunsChanged?.Invoke();
        }

        // 1번 ↔ 2번 칸 교환: 총 오브젝트는 그대로, 순서만 바꿈 (들고 있는 총은 계속 들고 있음)
        private void HandleWeaponsSwapped()
        {
            (guns[0], guns[1]) = (guns[1], guns[0]);
            (gunStacks[0], gunStacks[1]) = (gunStacks[1], gunStacks[0]);

            if (currentSlot < guns.Length)
            {
                currentSlot = 1 - currentSlot;
                OnSlotChanged?.Invoke(currentSlot);
            }
            if (IsSwapping && pendingSlot < guns.Length) pendingSlot = 1 - pendingSlot;

            OnGunsChanged?.Invoke();
        }

        // 3 ~ 6키: 소모품 사용 (다 쓰면 PlayerHotbar가 칸을 비움)
        private void UpdateConsumableInput()
        {
            if (hotbar == null) return;

            for (int k = 0; k < ConsumableKeys.Length; k++)
            {
                if (!SimpleInput.GetKeyDown(ConsumableKeys[k])) continue;
                if (!hotbar.TryConsume(PlayerHotbar.FirstConsumableSlot + k, out InventoryItem used)) continue;

                if (health != null) health.Heal(used.HealAmount);
                Debug.Log($"{used.Name} 사용");
            }
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
            CancelMeleeAttack();
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
            if (InfiniteStamina && amount < 0f) return;   // 무한 스태미나: 소모 무시
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
        
        /// <summary> 캐릭터 선택 스탯 적용 (PlayerSpawner 가 생성 직후 호출). 스태미나는 가득 채운다. </summary>
        // 테스트용 무한 스태미나 켜기/끄기. 켜면 바로 가득 채움
        public void SetInfiniteStamina(bool on)
        {
            InfiniteStamina = on;
            if (on) ChangeStamina(maxStamina);
        }

        public void ApplyStats(float newMoveSpeed, float newRunSpeed, float newMaxStamina)
        {
            moveSpeed  = newMoveSpeed;
            runSpeed   = newRunSpeed;
            maxStamina = newMaxStamina;
            stamina    = maxStamina;
            OnStaminaChanged?.Invoke(stamina, maxStamina);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (hotbar != null)
            {
                hotbar.OnSlotChanged -= HandleHotbarSlotChanged;
                hotbar.OnWeaponsSwapped -= HandleWeaponsSwapped;
            }
        }
        
        private void ConfineCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}