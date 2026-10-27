using UnityEngine;
using Study.Utilities;
using UnityEngine.InputSystem;

namespace Study3D.OutPost
{

    public class Gun : MonoBehaviour
    {
        [Header("라이플")] 
        [SerializeField] private string weaponName = "AssaultRifle";
        [SerializeField] private Sprite icon;
        [SerializeField] private bool isAutomatic = true;   // true: 누르고 있으면 연사, false: 클릭마다 한 발
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int reserveAmmo = 90;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField] private float damage = 10f;
        
        
        
        // 내부변수
        private int currentAmmo;
        private bool isReloading;
        private float nextFireTime;
        private float reloadEndTime;   // 이 시각이 되면 재장전 완료

        public string WeaponName => weaponName;
        public Sprite Icon => icon;
        public int MagazineSize => magazineSize;

        public bool IsAutomatic => isAutomatic;
        
        public event System.Action<int, int> OnAmmoChanged;   // (탄창, 예비)
        
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;

        private void Awake()
        {
            currentAmmo = magazineSize;
        }
        
        private void Update()
        {
            if (isReloading && Time.time >= reloadEndTime) FinishReload();
        }

        // 탄창에 필요한 만큼만 예비 탄약에서 옮겨 담습니다.
        private void FinishReload()
        {
            int need = magazineSize - currentAmmo;          // 채워야 할 양 (예: 30 - 12 = 18)
            int amount = Mathf.Min(need, reserveAmmo);      // 예비 탄약이 부족하면 있는 만큼만
            currentAmmo += amount;
            reserveAmmo -= amount;
            isReloading = false;

            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            
            Debug.Log($"재장전 완료: {currentAmmo} / {reserveAmmo}");
        }
        
        
        // 발사를 시도합니다. 실제로 발사했으면 true를 돌려줍니다.
        public bool TryFire(Vector3 direction)
        {
            if (isReloading) return false;              // 재장전 중
            if (Time.time < nextFireTime) return false; // 아직 다음 발사 시간이 안 됨
            if (currentAmmo <= 0) return false;         // 탄창이 비었음 (빈 탄창 처리는 9단계)

            nextFireTime = Time.time + fireInterval;    // 다음 발사 가능 시각 예약
            currentAmmo--;

            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            
            Debug.Log($"발사! {currentAmmo} / {reserveAmmo}");

            // 총구 위치에서, 커서 방향을 바라보게 생성
            Bullet bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));
            bullet.Init(damage);   // 이 총의 데미지를 총알에 넘겨줌
            return true;
        }

        // 재장전을 요청합니다. (8단계에서 구현)
        // 재장전을 시작합니다. 실제로 시작했으면 true (애니메이션 재생 판단용)
        public bool RequestReload()
        {
            if (isReloading) return false;                 // 이미 재장전 중
            if (currentAmmo >= magazineSize) return false; // 탄창이 가득 참
            if (reserveAmmo <= 0) return false;            // 예비 탄약이 없음

            isReloading = true;
            reloadEndTime = Time.time + reloadTime;        // nextFireTime과 같은 예약 방식
            return true;
        }
        
        // 재장전을 중단합니다. 탄은 채워지지 않습니다.
        public void CancelReload()
        {
            if (!isReloading) return;
            isReloading = false;
            Debug.Log("재장전 취소");
        }

    }
}