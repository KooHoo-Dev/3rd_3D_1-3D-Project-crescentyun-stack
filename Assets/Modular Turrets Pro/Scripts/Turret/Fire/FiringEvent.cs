// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class FiringEvent : MonoBehaviour
    {
        public GameObject[] gunPrefabs;
        public AudioClip firingSound;
        public GameObject muzzleVFX;
        public float muzzleVFXDuration = 2.0f;

        public virtual void Fire(GameObject projectilePrefab, GameObject target)
        {
            // Overridden in children
        }

    }
}