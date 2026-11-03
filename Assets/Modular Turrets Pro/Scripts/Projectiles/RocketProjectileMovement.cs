// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class RocketProjectileMovement : MonoBehaviour
    {
        [Tooltip("Strength to launch the rocket from head")]
        public float launchForceMagnitude = 7.0f;
        [Tooltip("Mesh to change during homming the projectile")]
        public Mesh rocketWithWings;
        [Tooltip("How fast projectile will reach the target")]
        public float force = 15.0f;
        [Tooltip("How strong the rotation of projectile will influence on the rotation")]
        public float rotattionForce = 5.0f;
        [Tooltip("Sound when projectile starts to launch")]
        public AudioClip launchSound;
        [Tooltip("Effect when projectile starts to launch")]
        public GameObject launchVFX;

        private GameObject target;
        private bool hommingState = false;

        // Start is called before the first frame update
        void Start()
        {
            Rigidbody myRigidComp = GetComponent<Rigidbody>();
            if (myRigidComp != null)
            {
                // Disable gravity and add force for launcing from head
                myRigidComp.useGravity = false;
                Invoke("EnableGravity", 0.15f);
                myRigidComp.AddForce(transform.forward * launchForceMagnitude, ForceMode.Impulse);
            }

            Invoke("StartHomming", 0.45f);
        }

        // Update target for homming event
        public void InitTarget(GameObject _target)
        {
            target = _target;
        }

        void EnableGravity()
        {
            Rigidbody myRigidComp = GetComponent<Rigidbody>();
            if (myRigidComp != null)
            {
                myRigidComp.useGravity = true;
            }

            // Play sound and VFX
            SoundManager.instance.PlaySoundFXClip(launchSound, transform, 0.5f);

            if (launchVFX)
            {
                GameObject impactFVXObject = Instantiate(launchVFX, transform.position, Quaternion.Euler(transform.forward * -1.0f));
                ParticleSystem particleSystem = impactFVXObject.GetComponent<ParticleSystem>();
                particleSystem.Play();
                Destroy(impactFVXObject, 2.0f);
            }
        }

        void StartHomming()
        {
            GetComponent<MeshFilter>().mesh = rocketWithWings;
            hommingState = true;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (hommingState)
            {
                Rigidbody myRigidComp = GetComponent<Rigidbody>();
                if (myRigidComp != null && target != null && target.activeSelf)
                {
                    Vector3 direction = target.transform.position - myRigidComp.position;
                    direction.Normalize();
                    Vector3 rotationAmount = Vector3.Cross(transform.forward, direction);
                    myRigidComp.angularVelocity = rotationAmount * rotattionForce;
                    myRigidComp.linearVelocity = transform.forward * force;
                }
            }
        }
    }
}