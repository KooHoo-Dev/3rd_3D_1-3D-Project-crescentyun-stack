// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class Turret : MonoBehaviour
    {
        [Tooltip("The reference of object to rotate")]
        public GameObject rotatorMesh;
        [Tooltip("The reference of object to rotate when we can't rotate the rotator by Roll axis")]
        public GameObject headMesh;
        public GameObject projectilePrefab;

        [Space(15)]

        [Tooltip("True - only rotator will rotate; False - rotator will never rotate by Roll axis, the head will be rotate instead")]
        public bool canRotateXRotator = true;
        [Tooltip("False - in some cases we need to prevent head rotation - ex. Rocket turret")]
        public bool canRotateHeadSeparately = true;

        [Space(15)]

        [Tooltip("How fast turret will be rotate")]
        public float rotateSpeed = 10.0f;
        [Tooltip("Angle when the turret can attack the target")]
        public float turretAngleAttack = 5.0f;
        [Tooltip("How often turret will start to fire")]
        public float fireTick = 0.1f;

        public bool showDebug = false;

        private GameObject nearestTarget;
        private bool bCanAttack = false;
        private bool bIsActive = true;
        private Vector3 previousTargetFramePosition;
        private float projectileSpeed = 20.0f;

        // Start is called before the first frame update
        void Start()
        {
            UpdateTurretActiveState(bIsActive);

            if (projectilePrefab)
            {
                ProjectileMovement projectileComp = projectilePrefab.GetComponent<ProjectileMovement>();
                if (projectileComp != null)
                {
                    projectileSpeed = projectileComp.speed;
                }
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            UpdateNearestTarget();

            if (nearestTarget != null)
            {
                RotateTurretToTheTarget();
                UpdateTurretAttackState();
            }
        }

        // Run the target finder script and find the nearest target
        private void UpdateNearestTarget()
        {
            GameObject newNearestTarget = gameObject.GetComponent<TargetFinder>().FindNearestTarget();
            if (newNearestTarget != null)
            {
                if (nearestTarget == null || nearestTarget != newNearestTarget)
                {
                    // start rotating the head to target
                    nearestTarget = newNearestTarget;
                    previousTargetFramePosition = nearestTarget.transform.position;
                }
            }
            else
            {
                // stop rotating the head and drop states
                //Debug.Log("No one in radius. Deactivate turret");
                DeactivateTurret();
            }
        }

        void StartTurretActivation()
        {
            InvokeRepeating("Fire", 0.0f, fireTick);

            // Activate all barrels
            Gun[] guns = GetComponentsInChildren<Gun>();
            foreach (Gun gun in guns)
            {
                if (gun == null)
                {
                    continue;
                }

                RotateBarrel myRotateBarrel = gun.GetComponentInChildren<RotateBarrel>();
                if (myRotateBarrel != null)
                {
                    myRotateBarrel.InitBarrelFireTick(fireTick);
                    myRotateBarrel.UpdateRotationState(true);
                }
            }

        }

        void UpdateTurretActiveState(bool active)
        {
            bIsActive = active;
            if (bIsActive)
            {
                StartTurretActivation();
            }
            else
            {
                DeactivateTurret();
            }
        }
        void DeactivateTurret()
        {
            nearestTarget = null;
            bCanAttack = false;
        }

        // Check the turret is in range of attack angles and update boolean state
        void UpdateTurretAttackState()
        {
            Quaternion targetRotation = GetLookAtTarget();
            Quaternion headRotation = headMesh.transform.rotation; // was rotator mesh

            float yDiff = Quaternion.Angle(headRotation, targetRotation);
            float zDiff = Quaternion.Angle(headRotation, targetRotation);

            bCanAttack = yDiff <= turretAngleAttack && zDiff <= turretAngleAttack;
        }

        // Calculate and return look rotation to the target
        private Quaternion GetLookAtTarget()
        {
            Vector3 predictedPosition = PredictTargetPosition(nearestTarget.transform.position);
            //Vector3 direction = (predictedPosition - headMesh.transform.position).normalized;
            //Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion targetRotation = Quaternion.LookRotation(predictedPosition - headMesh.transform.position);

            return targetRotation;
        }

        private Vector3 PredictTargetPosition(Vector3 inPosition)
        {
            // Calculate target velocity
            Vector3 targetVeclocity = (inPosition - previousTargetFramePosition) / Time.deltaTime;

            // Calculate destination time
            float destinationTime = (headMesh.transform.position - inPosition).magnitude / projectileSpeed;

            Vector3 resultPosition = inPosition + (targetVeclocity * destinationTime);

            if (showDebug)
            {
                Debug.DrawLine(inPosition, resultPosition, Color.magenta);
                //Debug.DrawLine(headMesh.transform.position, resultPosition, Color.blue);
                Debug.DrawLine(headMesh.transform.position, headMesh.transform.forward * 23.0f + headMesh.transform.position, Color.white);
            }

            return resultPosition;
        }

        // Rotate turret's head to the target 
        void RotateTurretToTheTarget()
        {
            Quaternion targetRotation = GetLookAtTarget();

            if (canRotateXRotator)
            {
                // Just update the rotator mesh rotation
                rotatorMesh.transform.rotation = QInterp_ToConstant(rotatorMesh.transform.rotation, targetRotation, Time.deltaTime, rotateSpeed);
            }
            else
            {
                // Calculate rotation for rotator mesh but update rotation without x axis. Update head mesh rotation instead
                Quaternion calculatedRotation = QInterp_ToConstant(rotatorMesh.transform.rotation, targetRotation, Time.deltaTime, rotateSpeed);

                // set X axis to zero value to prevent xRotation
                Vector3 rotatorEulerAngles = calculatedRotation.eulerAngles;
                rotatorEulerAngles.x = 0.0f;
                rotatorMesh.transform.rotation = Quaternion.Euler(rotatorEulerAngles);

                if (canRotateHeadSeparately)
                {
                    headMesh.transform.rotation = QInterp_ToConstant(headMesh.transform.rotation, targetRotation, Time.deltaTime, rotateSpeed);
                }
            }

            previousTargetFramePosition = nearestTarget.transform.position;
        }

        void Fire()
        {
            if (!bCanAttack || projectilePrefab == null)
            {
                return;
            }

            // Get all firing events and run the fire events
            FiringEvent[] firingEvents = GetComponents<FiringEvent>();
            foreach (FiringEvent firingEvent in firingEvents)
            {
                firingEvent.Fire(projectilePrefab, nearestTarget);
            }
        }

        void OnDrawGizmosSelected()
        {
            Transform pivotTransform = transform;
            if (headMesh != null)
            {
                pivotTransform = headMesh.transform;
            }
            else
            {
                if (rotatorMesh == null)
                {
                    pivotTransform = rotatorMesh.transform;
                }
            }

            // Draw turret cone attack
            float halfFOV = turretAngleAttack / 2.0f;

            TargetFinder myFinderScript = GetComponent<TargetFinder>();
            float rayLength = 10.0f;
            if (myFinderScript != null)
            {
                rayLength = myFinderScript.radius;
            }

            Gizmos.color = Color.red;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, pivotTransform.transform.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, pivotTransform.transform.up);

            Vector3 upRayDirection = leftRayRotation * pivotTransform.transform.forward * rayLength;
            Vector3 downRayDirection = rightRayRotation * pivotTransform.transform.forward * rayLength;

            Gizmos.DrawRay(pivotTransform.transform.position, upRayDirection);
            Gizmos.DrawRay(pivotTransform.transform.position, downRayDirection);
            Gizmos.DrawLine(pivotTransform.transform.position + downRayDirection, pivotTransform.transform.position + upRayDirection);

            // Draw line between head and target
            if (nearestTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pivotTransform.transform.position, nearestTarget.transform.position);
            }


        }

        Quaternion QInterp_ToConstant(Quaternion current, Quaternion target, float deltaTime, float interpSpeed)
        {
            if (deltaTime == 0.0f || Quaternion.Equals(current, target))
            {
                return current;
            }

            float maxDeltaAngle = interpSpeed * deltaTime;
            float angle = Quaternion.Angle(current, target);
            if (angle <= maxDeltaAngle)
            {
                return target;
            }

            // Interpolate towards the target rotation with maxDeltaAngle
            float t = maxDeltaAngle / angle;
            Quaternion result = Quaternion.Slerp(current, target, t);

            return result;
        }
    }
}