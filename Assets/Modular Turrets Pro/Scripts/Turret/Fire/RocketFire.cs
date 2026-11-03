// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class RocketFire : FiringEvent
    {
        public override void Fire(GameObject projectilePrefab, GameObject target)
        {
            GameObject fakeRocket = GetNewRocket();
            if (fakeRocket != null)
            {
                fakeRocket.SetActive(false);
                GameObject rocket = Instantiate(projectilePrefab, fakeRocket.transform.position, fakeRocket.transform.rotation);

                // Init target reference
                RocketProjectileMovement rocketProjectileMovement = rocket.GetComponent<RocketProjectileMovement>();
                if (rocketProjectileMovement != null)
                {
                    rocketProjectileMovement.InitTarget(target);
                }

                // Play sound and VFX
                SoundManager.instance.PlaySoundFXClip(firingSound, fakeRocket.transform, 0.5f);

                if (muzzleVFX)
                {
                    GameObject impactFVXObject = Instantiate(muzzleVFX, fakeRocket.transform.position, Quaternion.Euler(fakeRocket.transform.forward * -1.0f));
                    ParticleSystem particleSystem = impactFVXObject.GetComponent<ParticleSystem>();
                    particleSystem.Play();
                    Destroy(impactFVXObject, muzzleVFXDuration);
                }
            }
        }

        // Get the new rocket from heads. If all rockets are inactive, activate all rocket again
        GameObject GetNewRocket()
        {
            List<GameObject> rocketList = new List<GameObject>();
            HeadMissles[] HeadMisslesScripts = GetComponentsInChildren<HeadMissles>();

            GameObject resultRocket = null;

            // Get all active rockets
            foreach (HeadMissles headMisslesScript in HeadMisslesScripts)
            {
                foreach (GameObject rocket in headMisslesScript.fakeRockets)
                {
                    if (resultRocket == null)
                    {
                        resultRocket = rocket;
                    }
                    if (rocket.activeSelf)
                    {
                        rocketList.Add(rocket);
                    }
                }
            }

            // Get random rocket from list
            if (rocketList.Count > 0)
            {
                resultRocket = rocketList[Random.Range(0, rocketList.Count - 1)];
                resultRocket.SetActive(false);

                return resultRocket;
            }
            else
            {
                // Activate all head rockets
                foreach (HeadMissles headMisslesScript in HeadMisslesScripts)
                {
                    foreach (GameObject rocket in headMisslesScript.fakeRockets)
                    {
                        rocket.SetActive(true);
                    }
                }
                return resultRocket;
            }
        }
    }
}