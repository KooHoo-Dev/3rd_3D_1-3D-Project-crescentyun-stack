using UnityEngine;

namespace Study3D.OutPost
{

    public class Gun : MonoBehaviour
    {
        [Header("라이플")] 
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private int reserveAmmo = 90;
        [SerializeField] private float reloadTime = 2f;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private Transform muzzle;

        // 내부변수

        private int currentAmmo;
        private bool isReloading;
        private float nextFireTime;

        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;

        private void Awake()
        {
            currentAmmo = magazineSize;
        }
        
        // 발사를 시도합니다. 실제로 발사했으면 true를 돌려줍니다. (5단계에서 구현)
        public bool TryFire(Vector3 direction)
        {
            Debug.Log("Fire 요청");
            return false;
        }

        // 재장전을 요청합니다. (8단계에서 구현)
        public void RequestReload()
        {
            
            
        }
        
        

    }
}