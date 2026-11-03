// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class TargetMovement : MonoBehaviour
    {
        public enum ETargetDirectionType
        {
            Horizontal,
            Vertical,
            None
        }
        public ETargetDirectionType directionType = ETargetDirectionType.Horizontal;
        public float movementDistance = 5.0f;
        public float distancePerTick = 0.025f;

        private float goalHalfDistanceToMove;
        private float currentDistance;
        private float movementTick = 0.01f;
        private bool bIncreaseDistance = true;
        private Vector3 initalTargetLocation;


        // Start is called before the first frame update
        void Start()
        {
            if (directionType != ETargetDirectionType.None)
            {
                goalHalfDistanceToMove = movementDistance / 2.0f;
                currentDistance = 0.0f;
                initalTargetLocation = transform.position;
                InvokeRepeating("UpdateTargetPosition", 0.0f, movementTick);
            }
        }

        void UpdateTargetPosition()
        {
            currentDistance += distancePerTick * (bIncreaseDistance ? 1 : -1);

            Vector3 newTargetLocation = initalTargetLocation;
            Vector3 vectorToRotate = currentDistance * (directionType == ETargetDirectionType.Horizontal ? Vector3.right : Vector3.up);
            newTargetLocation += Quaternion.Euler(transform.rotation.eulerAngles) * vectorToRotate;

            transform.position = newTargetLocation;

            if (bIncreaseDistance)
            {
                bIncreaseDistance = currentDistance < goalHalfDistanceToMove;
            }
            else
            {
                bIncreaseDistance = currentDistance <= -goalHalfDistanceToMove;
            }
        }
    }
}