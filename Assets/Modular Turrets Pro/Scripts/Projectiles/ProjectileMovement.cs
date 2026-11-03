// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class ProjectileMovement : MonoBehaviour
    {
        public float speed = 20.0f;
        public float lifeSpan = 2.0f;

        // Start is called before the first frame update
        void Start()
        {
            Invoke("OnLifeSpanTimeEnd", lifeSpan);
        }

        void OnLifeSpanTimeEnd()
        {
            Destroy(gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

    }
}