// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class RigidForce : MonoBehaviour
    {
        public float magnitude = 5.0f;
        public float deviation = 2.0f;
        // Start is called before the first frame update
        void Start()
        {
            Rigidbody myRigidComp = GetComponent<Rigidbody>();
            if (myRigidComp != null)
            {
                myRigidComp.AddForce(Vector3.up * Random.Range(magnitude - deviation, magnitude + deviation), ForceMode.Impulse);
            }
        }
    }
}