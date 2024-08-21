using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
namespace VolumetricLights {

    [CustomEditor(typeof(VolumetricLight))]
    public partial class VolumetricLightEditor : Editor {

        SerializedProperty profile, profileSync;

        SerializedProperty blendMode, raymarchPreset, raymarchQuality, raymarchMinStep, raymarchMaxSteps, dithering, jittering, useBlueNoise, animatedBlueNoise, renderQueue, sortingLayerID, sortingOrder, flipDepthTexture, alwaysOn;
        SerializedProperty castDirectLight, directLightMultiplier, directLightSmoothSamples, directLightSmoothRadius, directLightBlendMode;
        SerializedProperty autoToggle, distanceStartDimming, distanceDeactivation, autoToggleCheckInterval;
        SerializedProperty useNoise, noiseTexture, noiseStrength, noiseScale, noiseFinalMultiplier, density, mediumAlbedo, brightness;
        SerializedProperty attenuationMode, attenCoefConstant, attenCoefLinear, attenCoefQuadratic, rangeFallOff, diffusionIntensity, penumbra;
        SerializedProperty tipRadius, nearClipDistance, cookieTexture, cookieScale, cookieOffset, cookieSpeed, frustumAngle, windDirection;
        SerializedProperty enableDustParticles, dustBrightness, dustMinSize, dustMaxSize, dustDistanceAttenuation, dustWindSpeed, dustAutoToggle, dustDistanceDeactivation, dustPrewarm;
        SerializedProperty enableShadows, shadowIntensity, shadowColor, shadowTranslucency, shadowTranslucencyIntensity, shadowTranslucencyBlend, shadowResolution, shadowCullingMask, shadowBakeInterval, shadowNearDistance, shadowAutoToggle, shadowDistanceDeactivation;
        SerializedProperty shadowBakeMode, shadowOrientation, shadowDirection;

        SerializedProperty useCustomBounds, bounds, boundsInLocalSpace;
        SerializedProperty areaWidth, areaHeight;
        SerializedProperty customRange, useCustomSize;
        SerializedProperty targetCamera;

        static GUIStyle boxStyle;
        bool profileChanged, enableProfileApply;
        VolumetricLight vl;
        public static VolumetricLight lastEditingLight;

        void OnEnable() {

            vl = (VolumetricLight)target;
            if (vl.lightComp == null) {
                vl.Init();
            }

            lastEditingLight = vl;
            profile = serializedObject.FindProperty("profile");
            profileSync = serializedObject.FindProperty("profileSync");

            blendMode = serializedObject.FindProperty("blendMode");
            raymarchPreset = serializedObject.FindProperty("raymarchPreset");
            raymarchQuality = serializedObject.FindProperty("raymarchQuality");
            raymarchMinStep = serializedObject.FindProperty("raymarchMinStep");
            raymarchMaxSteps = serializedObject.FindProperty("raymarchMaxSteps");
            dithering = serializedObject.FindProperty("dithering");
            jittering = serializedObject.FindProperty("jittering");
            useBlueNoise = serializedObject.FindProperty("useBlueNoise");
            animatedBlueNoise = serializedObject.FindProperty("animatedBlueNoise");
            renderQueue = serializedObject.FindProperty("renderQueue");
            sortingLayerID = serializedObject.FindProperty("sortingLayerID");
            sortingOrder = serializedObject.FindProperty("sortingOrder");
            flipDepthTexture = serializedObject.FindProperty("flipDepthTexture");
            alwaysOn = serializedObject.FindProperty("alwaysOn");
            castDirectLight = serializedObject.FindProperty("castDirectLight");
            directLightMultiplier = serializedObject.FindProperty("directLightMultiplier");
            directLightSmoothSamples = serializedObject.FindProperty("directLightSmoothSamples");
            directLightSmoothRadius = serializedObject.FindProperty("directLightSmoothRadius");
            directLightBlendMode = serializedObject.FindProperty("directLightBlendMode");
            useNoise = serializedObject.FindProperty("useNoise");
            noiseTexture = serializedObject.FindProperty("noiseTexture");
            noiseStrength = serializedObject.FindProperty("noiseStrength");
            noiseScale = serializedObject.FindProperty("noiseScale");
            noiseFinalMultiplier = serializedObject.FindProperty("noiseFinalMultiplier");
            density = serializedObject.FindProperty("density");
            mediumAlbedo = serializedObject.FindProperty("mediumAlbedo");
            brightness = serializedObject.FindProperty("brightness");
            attenuationMode = serializedObject.FindProperty("attenuationMode");
            attenCoefConstant = serializedObject.FindProperty("attenCoefConstant");
            attenCoefLinear = serializedObject.FindProperty("attenCoefLinear");
            attenCoefQuadratic = serializedObject.FindProperty("attenCoefQuadratic");
            rangeFallOff = serializedObject.FindProperty("rangeFallOff");
            diffusionIntensity = serializedObject.FindProperty("diffusionIntensity");
            penumbra = serializedObject.FindProperty("penumbra");
            tipRadius = serializedObject.FindProperty("tipRadius");
            nearClipDistance = serializedObject.FindProperty("nearClipDistance");
            cookieTexture = serializedObject.FindProperty("cookieTexture");
            cookieScale = serializedObject.FindProperty("cookieScale");
            cookieOffset = serializedObject.FindProperty("cookieOffset");
            cookieSpeed = serializedObject.FindProperty("cookieSpeed");
            frustumAngle = serializedObject.FindProperty("frustumAngle");
            windDirection = serializedObject.FindProperty("windDirection");
            enableDustParticles = serializedObject.FindProperty("enableDustParticles");
            dustBrightness = serializedObject.FindProperty("dustBrightness");
            dustMinSize = serializedObject.FindProperty("dustMinSize");
            dustMaxSize = serializedObject.FindProperty("dustMaxSize");
            dustWindSpeed = serializedObject.FindProperty("dustWindSpeed");
            dustDistanceAttenuation = serializedObject.FindProperty("dustDistanceAttenuation");
            dustAutoToggle = serializedObject.FindProperty("dustAutoToggle");
            dustDistanceDeactivation = serializedObject.FindProperty("dustDistanceDeactivation");
            dustPrewarm = serializedObject.FindProperty("dustPrewarm");
            enableShadows = serializedObject.FindProperty("enableShadows");
            shadowIntensity = serializedObject.FindProperty("shadowIntensity");
            shadowColor = serializedObject.FindProperty("shadowColor");
            shadowTranslucency = serializedObject.FindProperty("shadowTranslucency");
            shadowTranslucencyIntensity = serializedObject.FindProperty("shadowTranslucencyIntensity");
            shadowTranslucencyBlend = serializedObject.FindProperty("shadowTranslucencyBlend");
            shadowResolution = serializedObject.FindProperty("shadowResolution");
            shadowCullingMask = serializedObject.FindProperty("shadowCullingMask");
            shadowBakeInterval = serializedObject.FindProperty("shadowBakeInterval");
            shadowNearDistance = serializedObject.FindProperty("shadowNearDistance");
            shadowAutoToggle = serializedObject.FindProperty("shadowAutoToggle");
            shadowDistanceDeactivation = serializedObject.FindProperty("shadowDistanceDeactivation");
            shadowBakeMode = serializedObject.FindProperty("shadowBakeMode");
            shadowOrientation = serializedObject.FindProperty("shadowOrientation");
            shadowDirection = serializedObject.FindProperty("shadowDirection");
            autoToggle = serializedObject.FindProperty("autoToggle");
            distanceDeactivation = serializedObject.FindProperty("distanceDeactivation");
            distanceStartDimming = serializedObject.FindProperty("distanceStartDimming");
            autoToggleCheckInterval = serializedObject.FindProperty("autoToggleCheckInterval");

            useCustomBounds = serializedObject.FindProperty("useCustomBounds");
            bounds = serializedObject.FindProperty("bounds");
            boundsInLocalSpace = serializedObject.FindProperty("boundsInLocalSpace");
            useCustomSize = serializedObject.FindProperty("useCustomSize");
            areaWidth = serializedObject.FindProperty("areaWidth");
            areaHeight = serializedObject.FindProperty("areaHeight");
            customRange = serializedObject.FindProperty("customRange");
            targetCamera = serializedObject.FindProperty("targetCamera");
        }


        public override void OnInspectorGUI() {

            if (vl.lightComp != null && vl.lightComp.type == LightType.Directional) {
                EditorGUILayout.HelpBox("Volumetric Lights works with Point, Spot and Area lights only.\nPress the button below to place a volumetric area light in the scene synced with the directional light orientation and color.\nAlternatively you can use Volumetric Fog & Mist which has native support for directional light.", MessageType.Info);
                if (GUILayout.Button("Create a Volumetric Area Light")) {
                    CreateVolumetricAreaLight(vl.lightComp);
                }
                if (GUILayout.Button("Volumetric Fog & Mist information")) {
                    Application.OpenURL("https://assetstore.unity.com/packages/slug/162694?aid=1101lGsd");
                }
                return;
            }

            var pipe = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (pipe == null) {
                EditorGUILayout.HelpBox("Universal Rendering Pipeline asset is not set in Project Settings / Graphics !", MessageType.Error);
                return;
            }

            if (!pipe.supportsCameraDepthTexture) {
                EditorGUILayout.HelpBox("Depth Texture option is required in Universal Rendering Pipeline asset!", MessageType.Error);
                if (GUILayout.Button("Go to Universal Rendering Pipeline Asset")) {
                    Selection.activeObject = pipe;
                }
                EditorGUILayout.Separator();
                GUI.enabled = false;
            } else if (!pipe.supportsHDR && pipe.msaaSampleCount == 1 && pipe.renderScale == 1f && vl.profile != null && !vl.profile.flipDepthTexture) {
                EditorGUILayout.HelpBox("Depth Texture might be inverted due to current pipeline setup. To fix depth texture orientation, enable Flip Depth Texture option in the Volumetric Light profile or enable HDR or MSAA in Universal Rendering Pipeline asset.", MessageType.Error);
                if (GUILayout.Button("Go to Universal Rendering Pipeline Asset")) {
                    Selection.activeObject = pipe;
                }
                EditorGUILayout.Separator();
            }

            if (pipe != null) {
                if (GUILayout.Button("Show Global Settings")) {
                    if (VolumetricLightsRenderFeature.installed) {
                        var so = new SerializedObject(pipe);
                        var prop = so.FindProperty("m_RendererDataList");
                        if (prop != null && prop.arraySize > 0) {
                            var o = prop.GetArrayElementAtIndex(0);
                            if (o != null) {
                                Selection.SetActiveObjectWithContext(o.objectReferenceValue, null);
                                GUIUtility.ExitGUI();
                            }
                        }

                    } else {
                        if (EditorUtility.DisplayDialog("Show Global Settings", "The global settings can be found in the Volumetric Lights Render Feature which can't be found in the URP default renderer asset. Click Ok to select the current URP asset so you can add the render feature now.", "Ok", "Cancel")) {
                            Selection.activeObject = pipe;
                        }
                    }
                }
            }


            if (boxStyle == null) {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(15, 10, 5, 5);
            }

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            VolumetricLightProfile prevProfile = (VolumetricLightProfile)profile.objectReferenceValue;
            EditorGUILayout.PropertyField(profile, new GUIContent("Profile", "Create or load stored presets."));

            if (profile.objectReferenceValue != null) {

                if (prevProfile != profile.objectReferenceValue) {
                    profileChanged = true;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
                if (GUILayout.Button(new GUIContent("Create", "Creates a new profile which is a copy of the current values."), GUILayout.Width(60))) {
                    CreateProfile();
                    profileChanged = false;
                    enableProfileApply = false;
                    GUIUtility.ExitGUI();
                    return;
                }
                if (GUILayout.Button(new GUIContent("Load", "Updates volumetric light settings with the profile values."), GUILayout.Width(60))) {
                    vl.profile.ApplyTo(vl);
                    EditorUtility.SetDirty(vl);
                    serializedObject.Update();
                    profileChanged = true;
                }
                if (!enableProfileApply)
                    GUI.enabled = false;
                if (GUILayout.Button(new GUIContent("Save", "Updates profile values with changes in this inspector."), GUILayout.Width(60))) {
                    enableProfileApply = false;
                    profileChanged = false;
                    vl.profile.LoadFrom(vl);
                    EditorUtility.SetDirty(vl.profile);
                    GUIUtility.ExitGUI();
                    return;
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(profileSync, new GUIContent("Sync With Profile", "If disabled, profile settings will only be loaded when clicking 'Load' which allows you to customize settings after loading a profile and keep those changes."));
                if (profileSync.boolValue) GUI.enabled = false;
                EditorGUILayout.BeginHorizontal();
            } else {
                if (GUILayout.Button(new GUIContent("Create", "Creates a new profile which is a copy of the current settings."), GUILayout.Width(60))) {
                    CreateProfile();
                    GUIUtility.ExitGUI();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(blendMode);
            EditorGUILayout.PropertyField(raymarchPreset);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(raymarchQuality);
            EditorGUILayout.PropertyField(raymarchMinStep, new GUIContent("Min Step Size"));
            EditorGUILayout.PropertyField(raymarchMaxSteps, new GUIContent("Max Steps"));
            EditorGUILayout.PropertyField(jittering);
            if (EditorGUI.EndChangeCheck()) {
                raymarchPreset.intValue = (int)RaymarchPresets.UserDefined;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(dithering);
            EditorGUILayout.PropertyField(useBlueNoise);
            if (useBlueNoise.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(animatedBlueNoise, new GUIContent("Animated"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(renderQueue);
            EditorGUILayout.PropertyField(sortingLayerID);
            EditorGUILayout.PropertyField(sortingOrder);
            EditorGUILayout.PropertyField(flipDepthTexture);
            EditorGUILayout.PropertyField(alwaysOn);
            EditorGUILayout.PropertyField(castDirectLight);
            if (castDirectLight.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(directLightMultiplier, new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(directLightSmoothSamples, new GUIContent("Softness Samples"));
                EditorGUILayout.PropertyField(directLightSmoothRadius, new GUIContent("Softness Radius"));
                EditorGUILayout.PropertyField(directLightBlendMode, new GUIContent("Blend Mode"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(autoToggle, new GUIContent("Auto Toggle"));
            if (autoToggle.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(distanceStartDimming, new GUIContent("Distance Start Dimming"));
                EditorGUILayout.PropertyField(distanceDeactivation, new GUIContent("Distance Deactivation"));
                EditorGUILayout.PropertyField(autoToggleCheckInterval, new GUIContent("Check Time Interval"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(useNoise);
            if (useNoise.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(noiseTexture);
                EditorGUILayout.PropertyField(noiseStrength, new GUIContent("Strength"));
                EditorGUILayout.PropertyField(noiseScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(noiseFinalMultiplier, new GUIContent("Final Multiplier"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(density);
            EditorGUILayout.PropertyField(mediumAlbedo);
            EditorGUILayout.PropertyField(brightness);

            EditorGUILayout.PropertyField(attenuationMode);
            if (attenuationMode.intValue == (int)AttenuationMode.Quadratic) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(attenCoefConstant, new GUIContent("Constant Coef"));
                EditorGUILayout.PropertyField(attenCoefLinear, new GUIContent("Linear Coef"));
                EditorGUILayout.PropertyField(attenCoefQuadratic, new GUIContent("Quadratic Coef"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(rangeFallOff);
            EditorGUILayout.PropertyField(diffusionIntensity);
            EditorGUILayout.PropertyField(penumbra);

            switch (vl.lightComp.type) {
                case LightType.Spot:
                    EditorGUILayout.PropertyField(tipRadius);
                    EditorGUILayout.PropertyField(nearClipDistance);
                    EditorGUILayout.PropertyField(cookieTexture, new GUIContent("Cookie Texture (RGB)", "Assign any colored or grayscale texture. RGB values drive the color tint."));
                    if (cookieTexture.objectReferenceValue != null) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(cookieScale, new GUIContent("Scale"));
                        EditorGUILayout.PropertyField(cookieOffset, new GUIContent("Offset"));
                        EditorGUILayout.PropertyField(cookieSpeed, new GUIContent("Scroll Speed"));
                        EditorGUI.indentLevel--;
                    }
                    break;
                case LightType.Rectangle:
                case LightType.Disc:
                    EditorGUILayout.PropertyField(frustumAngle);
                    break;
            }

            if (useNoise.boolValue) {
                EditorGUILayout.PropertyField(windDirection);
            }

            EditorGUILayout.PropertyField(enableShadows);
            if (enableShadows.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(shadowIntensity, new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(shadowColor, new GUIContent("Color"));
#if UNITY_2021_3_OR_NEWER
                EditorGUILayout.PropertyField(shadowTranslucency, new GUIContent("Translucency"));
                if (shadowTranslucency.boolValue) {
                    if (vl.lightComp.type != LightType.Spot && vl.lightComp.type != LightType.Rectangle && vl.lightComp.type != LightType.Disc) {
                        EditorGUILayout.HelpBox("Translucent shadow maps are not supported for this light type.", MessageType.Info);
                    } else {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(shadowTranslucencyIntensity, new GUIContent("Intensity"));
                        EditorGUILayout.PropertyField(shadowTranslucencyBlend, new GUIContent("Blending"));
                        EditorGUI.indentLevel--;
                    }
                }
#endif
                EditorGUILayout.PropertyField(shadowResolution, new GUIContent("Resolution"));
                EditorGUILayout.PropertyField(shadowCullingMask, new GUIContent("Culling Mask"));
                EditorGUILayout.PropertyField(shadowBakeInterval, new GUIContent("Bake Interval"));
                if (vl.lightComp != null && vl.lightComp.type == LightType.Point) {
                    EditorGUILayout.PropertyField(shadowBakeMode, new GUIContent("Bake Mode"));
                    if (!shadowBakeMode.boolValue) {
                        EditorGUILayout.PropertyField(shadowOrientation, new GUIContent("Orientation", "Only for point lights: specify the direction for the baked shadows (shadows are captured in a half sphere or 180º). You can choose a fixed direction or make the shadows be aligned with the direction to the player camera."));
                        if (shadowOrientation.intValue == (int)ShadowOrientation.FixedDirection) {
                            EditorGUILayout.PropertyField(shadowDirection, new GUIContent("Direction"));
                        }
                    }
                }
                EditorGUILayout.PropertyField(shadowNearDistance, new GUIContent("Near Clip Distance"));
                EditorGUILayout.PropertyField(shadowAutoToggle, new GUIContent("Auto Toggle"));
                if (shadowAutoToggle.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(shadowDistanceDeactivation, new GUIContent("Distance"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(enableDustParticles);
            if (enableDustParticles.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(dustBrightness, new GUIContent("Brightness"));
                EditorGUILayout.PropertyField(dustMinSize, new GUIContent("Min Size"));
                EditorGUILayout.PropertyField(dustMaxSize, new GUIContent("Max Size"));
                EditorGUILayout.PropertyField(dustWindSpeed, new GUIContent("Wind Speed"));
                EditorGUILayout.PropertyField(dustDistanceAttenuation, new GUIContent("Distance Attenuation"));
                EditorGUILayout.PropertyField(dustAutoToggle, new GUIContent("Auto Toggle"));
                if (dustAutoToggle.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(dustDistanceDeactivation, new GUIContent("Distance"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(dustPrewarm, new GUIContent("Prewarm"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();

            GUI.enabled = true;

            // Additional options
            EditorGUILayout.PropertyField(targetCamera);

            if (vl.lightComp != null) {
                EditorGUILayout.PropertyField(useCustomSize);
                if (useCustomSize.boolValue) {
                    EditorGUI.indentLevel++;
                    switch (vl.lightComp.type) {
                        case LightType.Rectangle:
                            EditorGUILayout.PropertyField(areaWidth, new GUIContent("Width"));
                            EditorGUILayout.PropertyField(areaHeight, new GUIContent("Height"));
                            break;
                        case LightType.Disc:
                            EditorGUILayout.PropertyField(areaWidth, new GUIContent("Radius"));
                            break;
                        case LightType.Spot:
                        case LightType.Point:
                            break;
                    }
                    EditorGUILayout.PropertyField(customRange, new GUIContent("Range"));
                    EditorGUI.indentLevel--;
                }
            }

            if (vl.ps != null) {
                if (GUILayout.Button("Select Particle System")) {
                    Selection.activeGameObject = vl.ps.gameObject;
                }
            }

            EditorGUILayout.PropertyField(useCustomBounds);
            if (useCustomBounds.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(bounds);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(boundsInLocalSpace, new GUIContent("Local Space"));
                if (EditorGUI.EndChangeCheck()) {
                    Bounds b = bounds.boundsValue;
                    if (boundsInLocalSpace.boolValue) {
                        b.center = vl.meshRenderer.bounds.center - vl.transform.position;
                    } else {
                        b.center = vl.meshRenderer.bounds.center;

                    }
                    bounds.boundsValue = b;
                }
                EditorGUI.indentLevel--;
            }

            if (serializedObject.ApplyModifiedProperties()) {
                if (vl.profile != null) {
                    if (profileChanged) {
                        vl.profile.ApplyTo(vl);
                        profileChanged = false;
                        enableProfileApply = false;
                    } else {
                        enableProfileApply = true;
                    }
                }
            }
        }

        void CreateProfile() {
            string path = "Assets";
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) {
#if UNITY_2020_3_OR_NEWER
                var prefabPath = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
#else
                var prefabPath = PrefabStageUtility.GetCurrentPrefabStage().prefabAssetPath;
#endif
                if (!string.IsNullOrEmpty(prefabPath)) {
                    path = Path.GetDirectoryName(prefabPath);
                }
            } else {
                foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
                    path = AssetDatabase.GetAssetPath(obj);
                    if (File.Exists(path)) {
                        path = Path.GetDirectoryName(path);
                    }
                    break;
                }
            }
            VolumetricLightProfile fp = CreateInstance<VolumetricLightProfile>();
            fp.LoadFrom(vl);
            fp.name = "New Volumetric Light Profile";
            string fullPath;
            int counter = 0;
            do {
                fullPath = path + "/" + fp.name;
                if (counter > 0) fullPath += " " + counter;
                fullPath += ".asset";
                counter++;
            } while (File.Exists(fullPath));
            AssetDatabase.CreateAsset(fp, fullPath);
            AssetDatabase.SaveAssets();

            serializedObject.Update();
            profile.objectReferenceValue = fp;
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(fp);
        }

        private readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        protected virtual void OnSceneGUI() {
            VolumetricLight vl = (VolumetricLight)target;
            if (!vl.useCustomBounds) return;

            Bounds bounds = vl.GetBounds();
            m_BoundsHandle.center = bounds.center;
            m_BoundsHandle.size = bounds.size;

            // draw the handle
            EditorGUI.BeginChangeCheck();
            m_BoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck()) {
                // record the target object before setting new values so changes can be undone/redone
                Undo.RecordObject(vl, "Change Bounds");

                // copy the handle's updated data back to the target object
                Bounds newBounds = new Bounds();
                newBounds.center = m_BoundsHandle.center;
                newBounds.size = m_BoundsHandle.size;
                vl.SetBounds(newBounds);
            }
        }

        void CreateVolumetricAreaLight(Light directionalLight) {
            GameObject go = new GameObject("Volumetric Area Light (Directional Light)", typeof(Light));
            Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
            if (sceneCamera != null) {
                go.transform.position = sceneCamera.transform.TransformPoint(Vector3.forward * 50f);
            }
            float range = 1000;
            Camera cam = Camera.main;
            if (cam != null) {
                range = cam.farClipPlane;
            }
            Light light = go.GetComponent<Light>();
            light.type = LightType.Disc;
            light.areaSize = new Vector2(50, 20);
            light.range = range;
            light.enabled = false;
            VolumetricLight vl = go.AddComponent<VolumetricLight>();
            vl.raymarchMinStep = 1f;
            vl.jittering = 1.5f;
            vl.brightness = 0.5f;
            vl.density = 0.1f;
            vl.useNoise = false;
            vl.useBlueNoise = true;
            vl.animatedBlueNoise = false;
            vl.alwaysOn = true;
            vl.enableShadows = true;
            vl.shadowResolution = ShadowResolution._1024;
            vl.shadowIntensity = 1f;
            vl.Refresh();

            VolumetricLightDirectionalSync dirSync = go.AddComponent<VolumetricLightDirectionalSync>();
            dirSync.directionalLight = directionalLight;
            VolumetricLight current = (VolumetricLight)target;
            DestroyImmediate(current);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
            GUIUtility.ExitGUI();
        }
    }

}