using System.Collections.Generic;
using UnityEngine;
using MTP = ProjectZima.PolygonModularTurretsPack;

namespace Study3D.OutPost
{
    // 자동 포탑: 사거리에 먼저 들어온 좀비를 찾아 머리를 돌리고, 조준되면 발사 → 탄창이 비면 재장전
    // 같은 오브젝트에 TurretHealth 필요 (체력/파괴/수리)
    // 에셋 샘플 프리팹에 붙인 뒤 컴포넌트 ⋮ 메뉴 "에셋 포탑 설정 가져오기"를 실행하면 부품이 자동으로 채워짐
    public class AutoTurret : MonoBehaviour
    {
        [SerializeField] private TurretData data;

        [Header("부품")]
        [Tooltip("좌우로 도는 부분 (에셋의 Rotator)")]
        [SerializeField] private Transform yawPivot;
        [Tooltip("위아래로 도는 부분 (에셋의 Head). 비우면 yawPivot이 위아래까지 같이 돈다")]
        [SerializeField] private Transform pitchPivot;
        [Tooltip("총알이 나가는 위치. 총이 2개면 2개 넣기 (동시에 발사)")]
        [SerializeField] private Transform[] muzzles;
        [Tooltip("플레이어 Gun이 쓰는 Bullet 프리팹")]
        [SerializeField] private Bullet bulletPrefab;

        [Header("탐지")]
        [Tooltip("좀비 레이어 (Enemy)")]
        [SerializeField] private LayerMask enemyMask;
        [Tooltip("시야를 가리는 벽 레이어 (좀비 시야의 obstacleMask와 같게)")]
        [SerializeField] private LayerMask obstacleMask;
        [Tooltip("주변 좀비를 다시 찾는 간격(초)")]
        [SerializeField] private float scanInterval = 0.2f;
        [Tooltip("좀비의 어디를 노릴지 (발밑 기준 높이)")]
        [SerializeField] private float targetHeight = 1f;

        [Header("연출")]
        [Tooltip("쏠 때 뒤로 밀리는 총 부분 (에셋의 GunFireAnimation이 붙어 있던 오브젝트)")]
        [SerializeField] private Transform[] recoilParts;
        [SerializeField] private float recoilDistance = 0.08f;
        [SerializeField] private float recoilTime = 0.08f;
        [Tooltip("총열 회전 (에셋 스크립트 그대로 사용, 없으면 비워둠)")]
        [SerializeField] private MTP.RotateBarrel[] barrels;
        [SerializeField] private GameObject muzzleVFX;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;

        // 사거리 안의 좀비 (먼저 들어온 순서대로)
        private readonly List<EnemyHealth> inRange = new();
        private readonly HashSet<EnemyHealth> scanned = new();
        private readonly Collider[] hits = new Collider[32];

        private TurretHealth health;
        private EnemyHealth target;
        private Vector3[] recoilBasePositions;
        private float recoilTimer;
        private float nextScanTime;
        private float nextFireTime;
        private float reloadEndTime;
        private int currentAmmo;
        private bool isReloading;
        private bool barrelsSpinning;

        public TurretData Data => data;
        public EnemyHealth Target => target;
        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;

        // 건설할 때 TurretBuilder가 Instantiate 직후 호출 (Start보다 먼저)
        public void Init(TurretData newData)
        {
            data = newData;
        }

        // 컴포넌트를 처음 붙였을 때 레이어 기본값 채우기
        private void Reset()
        {
            enemyMask = LayerMask.GetMask("Enemy");
            obstacleMask = LayerMask.GetMask("Buildings", "Obstacle", "Pillars");
        }

        private void Awake()
        {
            health = GetComponent<TurretHealth>();

            recoilBasePositions = new Vector3[recoilParts.Length];
            for (int i = 0; i < recoilParts.Length; i++)
                if (recoilParts[i] != null) recoilBasePositions[i] = recoilParts[i].localPosition;
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.OnDestroyed += HandleDestroyed;
            health.OnRepaired += HandleRepaired;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.OnDestroyed -= HandleDestroyed;
            health.OnRepaired -= HandleRepaired;
        }

        private void Start()
        {
            if (!CheckSetup())
            {
                enabled = false;
                return;
            }

            currentAmmo = data.MagazineSize;
            foreach (var barrel in barrels)
                if (barrel != null) barrel.InitBarrelFireTick(data.FireInterval);
            SetBarrels(false);
        }

        private void Update()
        {
            UpdateRecoil();

            if (health != null && health.IsDestroyed) return;

            // 재장전 완료
            if (isReloading && Time.time >= reloadEndTime)
            {
                isReloading = false;
                currentAmmo = data.MagazineSize;
            }

            // 대상이 죽었으면 바로 다시 찾기
            if (target != null && target.IsDead)
            {
                target = null;
                nextScanTime = 0f;
            }

            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + scanInterval;
                Scan();
            }

            if (target == null)
            {
                SetBarrels(false);
                return;
            }

            Vector3 aimPoint = target.transform.position + Vector3.up * targetHeight;
            Aim(aimPoint);

            bool canShoot = !isReloading && IsAimed(aimPoint);
            SetBarrels(canShoot);

            if (canShoot && Time.time >= nextFireTime)
                Fire(aimPoint);
        }

        // 빠진 설정이 있으면 Console에 한 번에 알려주고 false
        private bool CheckSetup()
        {
            var problems = new List<string>();
            if (data == null) problems.Add("Data(TurretData)가 비어 있음");
            if (bulletPrefab == null) problems.Add("Bullet Prefab이 비어 있음");
            if (yawPivot == null) problems.Add("Yaw Pivot이 비어 있음 (에셋 포탑 설정 가져오기 실행)");
            if (muzzles == null || muzzles.Length == 0) problems.Add("Muzzles가 비어 있음");
            if (enemyMask.value == 0) problems.Add("Enemy Mask가 Nothing (Enemy 레이어 선택)");
            if (health == null) problems.Add("TurretHealth가 같은 오브젝트에 없음");

            if (problems.Count == 0) return true;
            Debug.LogError($"[AutoTurret] {name} 설정 문제 → 공격하지 않음\n- " + string.Join("\n- ", problems), this);
            return false;
        }

        #region 대상 찾기

        private void Scan()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, data.Range, hits, enemyMask,
                QueryTriggerInteraction.Ignore);

            // 이번에 잡힌 좀비. 새로 들어온 좀비는 목록 맨 뒤에 붙임
            scanned.Clear();
            for (int i = 0; i < count; i++)
            {
                EnemyHealth enemy = hits[i].GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy.IsDead) continue;
                if (!scanned.Add(enemy)) continue;   // 콜라이더가 여러 개인 좀비는 한 번만
                if (!inRange.Contains(enemy)) inRange.Add(enemy);
            }

            // 죽었거나 사거리를 벗어난 좀비는 목록에서 뺌
            inRange.RemoveAll(e => e == null || e.IsDead || !scanned.Contains(e));

            // 지금 대상이 아직 유효하면 계속 쏨
            if (target != null && inRange.Contains(target) && CanSee(target)) return;

            // 아니면 먼저 들어온 순서대로, 보이는 좀비를 새 대상으로
            target = null;
            foreach (EnemyHealth enemy in inRange)
            {
                if (!CanSee(enemy)) continue;
                target = enemy;
                break;
            }
        }

        // 벽에 가려지지 않았으면 true
        private bool CanSee(EnemyHealth enemy)
        {
            Vector3 eye = AimTransform.position;
            Vector3 point = enemy.transform.position + Vector3.up * targetHeight;
            return !Physics.Linecast(eye, point, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        #endregion

        #region 조준과 발사

        // 실제로 총구 방향을 나타내는 부분
        private Transform AimTransform => pitchPivot != null ? pitchPivot : yawPivot;

        private void Aim(Vector3 aimPoint)
        {
            float maxStep = data.RotateSpeed * Time.deltaTime;

            // 머리가 하나로 위아래·좌우 다 도는 구조
            if (pitchPivot == null)
            {
                Vector3 dir = aimPoint - yawPivot.position;
                if (dir.sqrMagnitude < 0.001f) return;
                yawPivot.rotation = Quaternion.RotateTowards(yawPivot.rotation, Quaternion.LookRotation(dir), maxStep);
                return;
            }

            // 좌우(yawPivot) + 위아래(pitchPivot) 따로 도는 구조
            Vector3 flat = aimPoint - yawPivot.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
                yawPivot.rotation = Quaternion.RotateTowards(yawPivot.rotation, Quaternion.LookRotation(flat), maxStep);

            Vector3 pitchDir = aimPoint - pitchPivot.position;
            if (pitchDir.sqrMagnitude > 0.001f)
                pitchPivot.rotation = Quaternion.RotateTowards(pitchPivot.rotation, Quaternion.LookRotation(pitchDir), maxStep);
        }

        private bool IsAimed(Vector3 aimPoint)
        {
            Transform aim = AimTransform;
            return Vector3.Angle(aim.forward, aimPoint - aim.position) <= data.AttackAngle;
        }

        private void Fire(Vector3 aimPoint)
        {
            nextFireTime = Time.time + data.FireInterval;

            // 모든 총구에서 동시에 발사
            foreach (Transform muzzle in muzzles)
            {
                if (muzzle == null) continue;

                Vector3 dir = aimPoint - muzzle.position;
                Bullet bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(dir));
                bullet.Init(data.Damage, transform);   // 쏜 쪽 = 이 포탑 (좀비가 반격할 대상)

                if (muzzleVFX != null)
                    Destroy(Instantiate(muzzleVFX, muzzle.position, muzzle.rotation), 1f);
            }

            recoilTimer = recoilTime;
            if (audioSource != null && fireSound != null) audioSource.PlayOneShot(fireSound);

            // 한 번 쏠 때 총구 수만큼 탄 소모
            currentAmmo -= muzzles.Length;
            if (currentAmmo <= 0)
            {
                currentAmmo = 0;
                isReloading = true;
                reloadEndTime = Time.time + data.ReloadTime;
            }
        }

        // 쏜 순간 뒤로 밀렸다가 원래 자리로 돌아옴
        private void UpdateRecoil()
        {
            if (recoilTimer <= 0f) return;

            recoilTimer = Mathf.Max(0f, recoilTimer - Time.deltaTime);
            float t = recoilTime > 0f ? recoilTimer / recoilTime : 0f;   // 1 → 0

            for (int i = 0; i < recoilParts.Length; i++)
            {
                if (recoilParts[i] == null) continue;
                recoilParts[i].localPosition = recoilBasePositions[i] + Vector3.back * (recoilDistance * t);
            }
        }

        private void SetBarrels(bool spin)
        {
            if (barrelsSpinning == spin) return;
            barrelsSpinning = spin;
            foreach (var barrel in barrels)
                if (barrel != null) barrel.UpdateRotationState(spin);
        }

        #endregion

        private void HandleDestroyed()
        {
            target = null;
            inRange.Clear();
            SetBarrels(false);
        }

        private void HandleRepaired()
        {
            currentAmmo = data.MagazineSize;
            isReloading = false;
        }

        private void OnDrawGizmosSelected()
        {
            float range = data != null ? data.Range : 10f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);

            if (target != null && yawPivot != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(AimTransform.position, target.transform.position + Vector3.up * targetHeight);
            }
        }

#if UNITY_EDITOR
        // 에셋 샘플 프리팹(SM_MachineGun_1, _4)의 설정을 읽어 이 컴포넌트에 채우고,
        // 에셋의 동작 스크립트(Turret, TargetFinder, FiringEvent, Gun, GunFireAnimation)는 제거함
        [ContextMenu("에셋 포탑 설정 가져오기")]
        private void ImportFromAsset()
        {
            var src = GetComponent<MTP.Turret>();
            if (src == null)
            {
                Debug.LogWarning("[AutoTurret] 같은 오브젝트에 에셋의 Turret 컴포넌트가 없음");
                return;
            }

            UnityEditor.Undo.RecordObject(this, "Import Turret");

            yawPivot = src.rotatorMesh != null ? src.rotatorMesh.transform : null;
            // 에셋에서 canRotateXRotator가 꺼져 있으면 머리(head)가 따로 위아래로 돈다
            pitchPivot = !src.canRotateXRotator && src.canRotateHeadSeparately && src.headMesh != null
                ? src.headMesh.transform
                : null;

            var muzzleList = new List<Transform>();
            var recoilList = new List<Transform>();
            foreach (var fire in GetComponents<MTP.FiringEvent>())
            {
                foreach (GameObject gunObj in fire.gunPrefabs)
                {
                    if (gunObj == null) continue;
                    var gun = gunObj.GetComponent<MTP.Gun>();
                    if (gun != null && gun.muzzleObject != null) muzzleList.Add(gun.muzzleObject.transform);

                    var anim = gunObj.GetComponentInChildren<MTP.GunFireAnimation>(true);
                    if (anim != null) recoilList.Add(anim.transform);
                }

                if (muzzleVFX == null) muzzleVFX = fire.muzzleVFX;
                if (fireSound == null) fireSound = fire.firingSound;
            }
            muzzles = muzzleList.ToArray();
            recoilParts = recoilList.ToArray();
            barrels = GetComponentsInChildren<MTP.RotateBarrel>(true);

            // 파괴되면 머리(Rotator)를 숨기고 하체만 남김
            var turretHealth = GetComponent<TurretHealth>();
            if (turretHealth == null) turretHealth = UnityEditor.Undo.AddComponent<TurretHealth>(gameObject);
            if (yawPivot != null) turretHealth.EditorSetHideParts(yawPivot.gameObject);

            // 에셋의 동작 스크립트 제거 (모델·RotateBarrel은 남김)
            foreach (var c in GetComponents<MTP.FiringEvent>()) UnityEditor.Undo.DestroyObjectImmediate(c);
            foreach (var c in GetComponentsInChildren<MTP.Gun>(true)) UnityEditor.Undo.DestroyObjectImmediate(c);
            foreach (var c in GetComponentsInChildren<MTP.GunFireAnimation>(true)) UnityEditor.Undo.DestroyObjectImmediate(c);
            var finder = GetComponent<MTP.TargetFinder>();
            if (finder != null) UnityEditor.Undo.DestroyObjectImmediate(finder);
            UnityEditor.Undo.DestroyObjectImmediate(src);

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[AutoTurret] 가져오기 완료 — 총구 {muzzles.Length}개, 반동 {recoilParts.Length}개, " +
                      $"총열 회전 {barrels.Length}개, 위아래 분리: {(pitchPivot != null)}");
        }
#endif
    }
}
