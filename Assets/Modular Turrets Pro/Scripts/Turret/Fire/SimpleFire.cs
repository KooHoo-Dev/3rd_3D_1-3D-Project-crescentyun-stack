// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class SimpleFire : FiringEvent
    {
        // Simple fire. Get all guns and run fire events
        public override void Fire(GameObject projectilePrefab, GameObject target)
        {
            foreach (GameObject gun in gunPrefabs)
            {
                Gun myGunScript = gun.GetComponent<Gun>();
                if (myGunScript != null)
                {
                    myGunScript.Fire(projectilePrefab, firingSound, muzzleVFX, muzzleVFXDuration);
                }
            }

        }
    }
}