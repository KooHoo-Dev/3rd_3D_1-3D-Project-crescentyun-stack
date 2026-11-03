// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class DamageManager : MonoBehaviour
    {
        private float damage = 1.0f;

        public void SetDamage(float damage)
        {
            this.damage = damage;
        }

        public float GetDamage()
        {
            return damage;
        }
    }
}
