using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VolumetricLightsDemo {

    public class FenceGen : MonoBehaviour {

        public int count;
        public float delay = 0.1f;
        public Vector3 step = new Vector3(0, 0, -2);

        float last;
        Vector3 pos;

        void Start() {
            pos = transform.position;
        }

        void Update() {
            if (Time.time - last < delay) return;
            last = Time.time;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
            pos += step;
            cube.transform.localScale = new Vector3(1, 4, 1);
            if (--count < 0) Destroy(this);
        }
    }

}