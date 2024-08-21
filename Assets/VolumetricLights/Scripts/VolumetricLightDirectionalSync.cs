using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VolumetricLights {
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    [RequireComponent(typeof(VolumetricLight))]
    public class VolumetricLightDirectionalSync : MonoBehaviour {

        [Tooltip("The directional light that is synced with this volumetric area light.")]
        public Light directionalLight;
        [Tooltip("Makes this area light position follow the desired target. Usually this is the main camera.")]
        public Transform follow;
        [Tooltip("Move volumetric light to 'follow' gameobject position if distance is greater than this value. Updating the position of this volumetric light area every frame is not recommended.")]
        public float distanceUpdate = 1f;

        VolumetricLight vl;
        Light fakeLight;
        Vector3 lastFollowPos;

        private void Start() {
            vl = GetComponent<VolumetricLight>();
            fakeLight = GetComponent<Light>();
            if (follow == null && Camera.main != null) follow = Camera.main.transform;
        }


        void LateUpdate() {
            if (directionalLight != null) {
                if (follow != null) {
                    Vector3 followPos = follow.position;
                    if (Vector3.Distance(lastFollowPos, followPos) > distanceUpdate) {
                        lastFollowPos = followPos;
                        transform.position = follow.position;
                        transform.position -= directionalLight.transform.forward * vl.generatedRange * 0.5f;
                    }
                }
                transform.forward = directionalLight.transform.forward;
                if (fakeLight != null) {
                    fakeLight.enabled = false;
                    fakeLight.color = directionalLight.color;
                    fakeLight.intensity = directionalLight.intensity;
                }
            }

        }
    }

}