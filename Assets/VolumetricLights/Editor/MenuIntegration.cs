using UnityEditor;
using UnityEngine;

namespace VolumetricLights {

    public static class MenuIntegration {

        [MenuItem("GameObject/Light/Volumetric Point Light", false, 100)]
        public static void AddVolumetricPointLight(MenuCommand menuCommand) {
            GameObject go = new GameObject("Volumetric Point Light", typeof(Light));
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            PlaceInFrontOfCamera(go);
            Light light = go.GetComponent<Light>();
            light.type = LightType.Point;
            VolumetricLight vl = go.AddComponent<VolumetricLight>();
            vl.useNoise = false;
            vl.density = 0.1f;
            vl.brightness = 1f;
            vl.attenuationMode = AttenuationMode.Quadratic;
            vl.rangeFallOff = 10f;
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }


        [MenuItem("GameObject/Light/Volumetric Spot Light", false, 100)]
        public static void AddVolumetricSpotLight(MenuCommand menuCommand) {
            GameObject go = new GameObject("Volumetric Spot Light", typeof(Light));
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            PlaceInFrontOfCamera(go);
            Light light = go.GetComponent<Light>();
            light.type = LightType.Spot;
            go.AddComponent<VolumetricLight>();
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Light/Volumetric Rect Area Light", false, 100)]
        public static void AddVolumetricRectAreaLight(MenuCommand menuCommand) {
            GameObject go = new GameObject("Volumetric Rect Area Light", typeof(Light));
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            PlaceInFrontOfCamera(go);
            Light light = go.GetComponent<Light>();
            light.type = LightType.Rectangle;
            light.areaSize = new Vector2(5f, 2f);
            go.AddComponent<VolumetricLight>();
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Light/Volumetric Disc Area Light", false, 100)]
        public static void AddVolumetricDiscAreaLight(MenuCommand menuCommand) {
            GameObject go = new GameObject("Volumetric Disc Area Light", typeof(Light));
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            PlaceInFrontOfCamera(go);
            Light light = go.GetComponent<Light>();
            light.type = LightType.Disc;
            light.areaSize = new Vector2(2f, 2f);
            go.AddComponent<VolumetricLight>();
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        static void PlaceInFrontOfCamera(GameObject go) {
            Transform t = go.transform.parent;
            if (t != null) return;
            Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
            if (sceneCamera != null) {
                go.transform.position = sceneCamera.transform.TransformPoint(Vector3.forward * 50f);
            }

        }

    }

}