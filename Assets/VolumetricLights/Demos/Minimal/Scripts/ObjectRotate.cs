using UnityEngine;

namespace VolumetricLightsDemo {

    public class ObjectRotate : MonoBehaviour {

        public Vector3 endAngles;
        public float speed = 0.5f;

        Vector3 startAngles;

        void Start() {
            startAngles = transform.eulerAngles;
        }

        void Update() {
            float t = Mathf.PingPong(Time.time * speed, 1f);
            t = Mathf.SmoothStep(0, 1, t);
            Vector3 angles = Vector3.Slerp(startAngles, endAngles, t);
            transform.eulerAngles = angles;
        }
    }

}