// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class SequenceFiring : FiringEvent
    {
        private int lastFiringGunIndex = -1;

        public override void Fire(GameObject projectilePrefab, GameObject target)
        {
            if (gunPrefabs.Length > 0)
            {
                if (lastFiringGunIndex == -1 || lastFiringGunIndex == gunPrefabs.Length - 1)
                {
                    lastFiringGunIndex = 0;
                }
                else
                {
                    lastFiringGunIndex++;
                }

                Gun myGunScript = gunPrefabs[lastFiringGunIndex].GetComponent<Gun>();
                if (myGunScript != null)
                {
                    myGunScript.Fire(projectilePrefab, firingSound, muzzleVFX, muzzleVFXDuration);
                }
            }
        }
    }
}