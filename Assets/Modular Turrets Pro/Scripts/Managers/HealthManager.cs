// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class HealthManager : MonoBehaviour
    {
        public float health = 20.0f;
        private float maxHealth = 20.0f;

        // Start is called before the first frame update
        void Start()
        {
            maxHealth = health;
            RestoreHealth();

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (health <= 0.0f)
            {
                return;
            }

            DamageManager damageManager = other.GetComponent<DamageManager>();
            if (damageManager != null)
            {
                health -= damageManager.GetDamage();
                if (health <= 0.0f)
                {
                    Target myOwner = gameObject.GetComponent<Target>();
                    if (myOwner != null)
                    {
                        myOwner.OnTargetLooseHealth();
                    }
                }

                other.gameObject.SetActive(false);
                Destroy(other.gameObject, 0.5f);
            }
        }

        public void RestoreHealth()
        {
            health = maxHealth;
        }
    }

}