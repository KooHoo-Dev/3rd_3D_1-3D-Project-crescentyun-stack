// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class GunFireAnimation : MonoBehaviour
    {
        [Tooltip("How the gun will be change local position")]
        public AnimationCurve lerpCurve;
        [Tooltip("Curve length")]
        public float lerpTime = 0.1f;
        private float _timer = 0.0f;
        private Vector3 StartPosition;
        private bool bCanUpdateAnimation = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        public void RunFireAnimation()
        {
            _timer = 0.0f;
            bCanUpdateAnimation = true;
            StartPosition = transform.localPosition;
        }

        // Update is called once per frame
        void Update()
        {
            if (bCanUpdateAnimation)
            {
                _timer += Time.deltaTime;

                if (_timer > lerpTime)
                {
                    bCanUpdateAnimation = false;
                }
                float lerpRatio = _timer / lerpTime;

                transform.localPosition = StartPosition + new Vector3(0.0f, 0.0f, lerpCurve.Evaluate(lerpRatio));
            }
        }
    }
}