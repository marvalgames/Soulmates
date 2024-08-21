using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VolumetricLightsDemo {

    public class CubeGen : MonoBehaviour {

        public int count;
        public float delay = 0.1f;

        float last;

        void Update() {
            if (Time.time - last < delay) return;
            last = Time.time;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = transform.position;
            cube.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
            cube.transform.forward = Random.onUnitSphere;
            cube.AddComponent<Rigidbody>();
            if (--count < 0) Destroy(this);



        }
    }

}