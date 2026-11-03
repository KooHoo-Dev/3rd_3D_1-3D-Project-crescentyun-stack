// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class Gun : MonoBehaviour
    {
        [Tooltip("Object for determine to instantiate the projectile prefab")]
        public GameObject muzzleObject;
        [Tooltip("Number of spawn projectile prefabs")]
        public int burstFireCount = 1;

        public void Fire(GameObject projectilePrefab, AudioClip firingSound, GameObject muzzleVFX, float muzzleVFXDuration)
        {
            if (muzzleObject != null)
            {
                for (int i = 0; i < burstFireCount; i++)
                {
                    Instantiate(projectilePrefab, muzzleObject.transform.position, muzzleObject.transform.rotation);

                    // play sound
                    SoundManager.instance.PlaySoundFXClip(firingSound, muzzleObject.transform, 0.5f);

                    // play effect
                    if (muzzleVFX)
                    {
                        GameObject impactFVXObject = Instantiate(muzzleVFX, muzzleObject.transform.position, Quaternion.Euler(muzzleObject.transform.forward));
                        ParticleSystem particleSystem = impactFVXObject.GetComponent<ParticleSystem>();
                        particleSystem.Play();
                        Destroy(impactFVXObject, muzzleVFXDuration);
                    }
                }

                GunFireAnimation myGunAnim = gameObject.GetComponentInChildren<GunFireAnimation>();
                if (myGunAnim != null)
                {
                    myGunAnim.RunFireAnimation();
                }
            }
        }
    }
}