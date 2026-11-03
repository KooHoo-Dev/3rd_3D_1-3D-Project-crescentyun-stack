// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class Target : MonoBehaviour
    {
        [Tooltip("Time to activate before destroy")]
        public float restoreDelay = 8.0f;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnTargetLooseHealth()
        {
            gameObject.SetActive(false);
            Invoke("ActivateTarget", restoreDelay);
        }

        void ActivateTarget()
        {
            gameObject.SetActive(true);
            HealthManager myHealth = GetComponentInChildren<HealthManager>();
            if (myHealth != null)
            {
                myHealth.RestoreHealth();
            }
        }
    }
}