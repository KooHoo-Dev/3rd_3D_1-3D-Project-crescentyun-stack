// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class TargetFinder : MonoBehaviour
    {
        public float radius = 10f;
        public string targetTag = "Enemy";

        [Tooltip("If null - takes from the parent pivot")]
        public GameObject pivotObject;
        [Tooltip("Represent the check zone and linecast traces")]
        public bool showDebug = false;

        private Transform pivotTransform;

        // Start is called before the first frame update
        void Start()
        {
            if (pivotObject == null)
            {
                pivotTransform = transform.transform;
            }
            else
            {
                pivotTransform = pivotObject.transform;
            }
        }

        // Spawn ovelap sphere and find the possible targets with targetTag
        private List<Transform> GetTargetsFromRadius()
        {
            Collider[] colliders = Physics.OverlapSphere(pivotTransform.position, radius);
            List<Transform> possibleTargets = new List<Transform>();

            foreach (Collider collider in colliders)
            {
                if (collider.gameObject.CompareTag(targetTag))
                {
                    RaycastHit hit;
                    bool bResult = Physics.Linecast(pivotTransform.position, collider.transform.position, out hit);

                    if (bResult && hit.collider == collider)
                    {
                        possibleTargets.Add(collider.transform);

                        if (showDebug)
                        {
                            Debug.DrawLine(pivotTransform.position, collider.transform.position, Color.green);
                        }
                    }
                    else
                    {
                        if (showDebug)
                        {
                            Debug.DrawLine(pivotTransform.position, collider.transform.position, Color.red);
                        }
                    }

                }
            }

            return possibleTargets;
        }

        // Search in the possible targets array and find the nearest target
        public GameObject FindNearestTarget()
        {
            List<Transform> possibleTargets = GetTargetsFromRadius();

            Transform nearestTarget = null;
            float nearestDistance = Mathf.Infinity;
            Vector3 currentPosition = pivotTransform.position;

            foreach (Transform target in possibleTargets)
            {
                float distance = Vector3.Distance(currentPosition, target.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = target;
                }
            }

            if (nearestTarget != null)
            {
                return nearestTarget.gameObject;
            }
            return null;

        }

        // Visualize the debug overlap sphere
        void OnDrawGizmosSelected()
        {
            if (showDebug)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
}