// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class RotateBarrel : MonoBehaviour
    {
        [Tooltip("Count of barrels determine angle to rotate the barrel to attack")]
        public int barrelsCount = 3;
        public bool isClockwiseRotation = true;

        private float fireTick = 0.0f;
        private bool bCanRotate = false;
        private float rotateSpeedPerTick = 0.0f;

        public void InitBarrelFireTick(float newTick)
        {
            fireTick = newTick;
            rotateSpeedPerTick = 360.0f / barrelsCount / fireTick;
        }

        public void UpdateRotationState(bool newState)
        {
            bCanRotate = newState;
        }

        // Update is called once per frame
        void Update()
        {
            if (bCanRotate)
            {
                transform.Rotate(0.0f, 0.0f, rotateSpeedPerTick * Time.deltaTime * (isClockwiseRotation ? 1.0f : -1.0f));
            }
        }
    }
}