// Copyright Project Zima. All Rights Reserved.
// Author: Sergey Leushkin

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectZima.PolygonModularTurretsPack
{
    public class HeadMissles : MonoBehaviour
    {
        [Tooltip("Positions for the fake rockets")]
        public GameObject[] rocketPositions;

        public GameObject fakeRocketPrefab;

        public List<GameObject> fakeRockets;

        // Start is called before the first frame update
        void Start()
        {
            foreach (GameObject position in rocketPositions)
            {
                GameObject fakeRocket = Instantiate(fakeRocketPrefab, position.transform.position, position.transform.rotation);
                fakeRocket.transform.parent = transform;
                fakeRockets.Add(fakeRocket);
            }
        }
    }
}