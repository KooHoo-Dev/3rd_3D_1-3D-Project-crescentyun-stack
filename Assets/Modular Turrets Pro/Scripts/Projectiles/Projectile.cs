// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Sound when projectile start impact the target")]
        public AudioClip impactSound;
        [Tooltip("Applying damage for target")]
        public float damage = 1.0f;

        [Tooltip("Effect when projectile start impact the target")]
        public GameObject impactVFX;
        public float impact_duration = 2.0f;

        // Start is called before the first frame update
        void Start()
        {
            DamageManager damageManager = GetComponent<DamageManager>();
            if (damageManager != null)
            {
                damageManager.SetDamage(damage);
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            SoundManager.instance.PlaySoundFXClip(impactSound, gameObject.transform, 1.0f);

            if (impactVFX)
            {
                GameObject impactFVXObject = Instantiate(impactVFX, transform.position, Quaternion.Euler(transform.forward * -1.0f));
                ParticleSystem particleSystem = impactFVXObject.GetComponent<ParticleSystem>();
                particleSystem.Play();
                Destroy(impactFVXObject, impact_duration);
            }
        }
    }
}