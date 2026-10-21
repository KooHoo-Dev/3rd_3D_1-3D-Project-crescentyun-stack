using UnityEngine;

namespace Study3D.OutPost
{

    public class Gun
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
        public int ReservAmmo => reserveAmmo;
        public bool IsREloading => isReloading;

        private void Awake()
        {
            currentAmmo = magazineSize;
        }


    }
}