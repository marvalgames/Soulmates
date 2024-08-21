//------------------------------------------------------------------------------------------------------------------
// Volumetric Lights
// Created by Kronnect
//------------------------------------------------------------------------------------------------------------------

using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#endif

namespace VolumetricLights {

    public delegate void PropertiesChangedEvent(VolumetricLight volumetricLight);

    [ExecuteAlways, RequireComponent(typeof(Light)), AddComponentMenu("Effects/Volumetric Light", 1000)]
    [HelpURL("https://kronnect.com/guides/volumetric-lights-2-urp-volumetric-light-parameters/")]
    [DefaultExecutionOrder(100)]
    public partial class VolumetricLight : MonoBehaviour {

        // Events
        public event PropertiesChangedEvent OnPropertiesChanged;

        // Common
        public bool profileSync = true;
        public bool useCustomBounds;
        public Bounds bounds;
        [Tooltip("In enabled, bounds coordinates are relative to the light position")]
        public bool boundsInLocalSpace;
        public VolumetricLightProfile profile;
        public float customRange = 1f;
        [Tooltip("Used for point light occlusion orientation and checking camera distance when autoToggle options are enabled. If not assigned, it will try to use the main camera.")]
        public Transform targetCamera;

        // Area
        public bool useCustomSize;
        public float areaWidth = 1f, areaHeight = 1f;

        [NonSerialized]
        public Light lightComp;

        const float GOLDEN_RATIO = 0.618033989f;

        MeshFilter mf;
        [NonSerialized]
        public MeshRenderer meshRenderer;
        Material fogMat, fogMatLight, fogMatInvisible;
        Vector4 windDirectionAcum;
        bool requireUpdateMaterial;
        List<string> keywords;
        static Texture2D blueNoiseTex;
        float distanceToCameraSqr;

        [NonSerialized]
        public static Transform mainCamera;
        float lastDistanceCheckTime;
        bool wasInRange;


        public static List<VolumetricLight> volumetricLights = new List<VolumetricLight>();


        void OnEnable() {
            Init();
        }


#if UNITY_EDITOR
        private void OnWillRenderObject() {
            if (!Application.isPlaying) {
                requireUpdateMaterial = true;
            }
        }
#endif

        public void Init() {
            if (!volumetricLights.Contains(this)) {
                volumetricLights.Add(this);
            }
            lightComp = GetComponent<Light>();
            if (gameObject.layer == 0) { // if object is in default layer, move it to transparent fx layer
                gameObject.layer = 1;
            }
            SettingsInit();
            Refresh();
        }

        public void Refresh() {
            if (!enabled) return;
            CheckProfile();

            generatedRange = generatedTipRadius = generatedSpotAngle = generatedBaseRadius = -1;
            generatedAreaWidth = generatedAreaHeight = generatedAreaFrustumAngle = generatedAreaFrustumMultiplier = 0;
            CheckMesh();
            CheckShadows();
            UpdateMaterialPropertiesNow();
        }

        private void OnValidate() {
            SettingsValidate();
            requireUpdateMaterial = true;
        }

        public void OnDidApplyAnimationProperties() {
            requireUpdateMaterial = true;
        }

        private void OnDisable() {
            if (volumetricLights.Contains(this)) {
                volumetricLights.Remove(this);
            }
            TurnOff();
        }

        void TurnOff() {
            if (meshRenderer != null) {
                meshRenderer.enabled = false;
            }
            ShadowsDisable();
            ParticlesDisable();
        }

        private void OnDestroy() {
            if (fogMatInvisible != null) {
                DestroyImmediate(fogMatInvisible);
                fogMatInvisible = null;
            }
            if (fogMatLight != null) {
                DestroyImmediate(fogMatLight);
                fogMatLight = null;
            }
            if (meshRenderer != null) {
                meshRenderer.enabled = false;
            }
            ShadowsDispose();
        }

        void LateUpdate() {

            bool isActiveAndEnabled = lightComp.isActiveAndEnabled || alwaysOn;
            if (isActiveAndEnabled) {
                if (meshRenderer != null && !meshRenderer.enabled) {
                    requireUpdateMaterial = true;
                }
            } else {
                if (meshRenderer != null && meshRenderer.enabled) {
                    TurnOff();
                }
                return;
            }

            if (CheckMesh()) {
                if (!Application.isPlaying) {
                    ParticlesDisable();
                }
                ScheduleShadowCapture();
                requireUpdateMaterial = true;
            }

            if (requireUpdateMaterial) {
                requireUpdateMaterial = false;
                UpdateMaterialPropertiesNow();
            }

            if (fogMat == null || meshRenderer == null) return;

            UpdateVolumeGeometry();

            float now = Time.time;
            if ((dustAutoToggle || shadowAutoToggle || autoToggle) && (!Application.isPlaying || (now - lastDistanceCheckTime) >= autoToggleCheckInterval)) {
                lastDistanceCheckTime = now;
                ComputeDistanceToCamera();
            }

            float brightness = this.brightness;

            if (autoToggle) {
                float maxDistSqr = distanceDeactivation * distanceDeactivation;
                float minDistSqr = distanceStartDimming * distanceStartDimming;
                if (minDistSqr > maxDistSqr) minDistSqr = maxDistSqr;
                float dim = 1f - Mathf.Clamp01((distanceToCameraSqr - minDistSqr) / (maxDistSqr - minDistSqr));
                brightness *= dim;
                bool isInRange = dim > 0.0f;
                if (isInRange != wasInRange) {
                    wasInRange = isInRange;
                    meshRenderer.enabled = isInRange;
                }
            }

            UpdateDiffusionTerm();

            if (enableDustParticles) {
                if (!Application.isPlaying) {
                    ParticlesResetIfTransformChanged();
                }
                UpdateParticlesVisibility();
            }

            fogMat.SetColor(ShaderParams.LightColor, lightComp.color * mediumAlbedo * (lightComp.intensity * brightness));
            float deltaTime = Time.deltaTime;
            windDirectionAcum.x += windDirection.x * deltaTime;
            windDirectionAcum.y += windDirection.y * deltaTime;
            windDirectionAcum.z += windDirection.z * deltaTime;
            windDirectionAcum.w = animatedBlueNoise ? GOLDEN_RATIO * (Time.frameCount % 480) : 0;
            fogMat.SetVector(ShaderParams.WindDirection, windDirectionAcum);

            ShadowsUpdate();
        }


        void ComputeDistanceToCamera() {
            if (mainCamera == null) {
                mainCamera = targetCamera;
                if (mainCamera == null && Camera.main != null) {
                    mainCamera = Camera.main.transform;
                }
                if (mainCamera == null) return;
            }
            Vector3 camPos = mainCamera.position;
            Vector3 pos = bounds.center;
            distanceToCameraSqr = (camPos - pos).sqrMagnitude;
        }

        void UpdateDiffusionTerm() {
            Vector4 toLightDir = -transform.forward;
            toLightDir.w = diffusionIntensity;
            fogMat.SetVector(ShaderParams.ToLightDir, toLightDir);
        }


        public void UpdateVolumeGeometry() {
            NormalizeScale();
            UpdateVolumeGeometryMaterial(fogMat);
            if (enableDustParticles && particleMaterial != null) {
                UpdateVolumeGeometryMaterial(particleMaterial);
                particleMaterial.SetMatrix(ShaderParams.WorldToLocalMatrix, transform.worldToLocalMatrix);
            }
        }

        void UpdateVolumeGeometryMaterial(Material mat) {
            if (mat == null) return;

            Transform t = transform;

            Vector3 pos = t.position;
            Vector4 tipData;
            tipData.x = pos.x;
            tipData.y = pos.y;
            tipData.z = pos.z;
            tipData.w = tipRadius;
            mat.SetVector(ShaderParams.ConeTipData, tipData);

            Vector3 forward = t.forward;
            Vector4 coneAxis;
            coneAxis.x = forward.x * generatedRange;
            coneAxis.y = forward.y * generatedRange;
            coneAxis.z = forward.z * generatedRange;
            float maxDistSqr = generatedRange * generatedRange;
            coneAxis.w = maxDistSqr;
            mat.SetVector(ShaderParams.ConeAxis, coneAxis);

            float falloff = Mathf.Max(0.0001f, rangeFallOff);
            float pointAttenX = -1f / (maxDistSqr * falloff);
            float pointAttenY = maxDistSqr / (maxDistSqr * falloff);
            mat.SetVector(ShaderParams.ExtraGeoData, new Vector4(generatedBaseRadius, pointAttenX, pointAttenY, generatedRange));

            if (!useCustomBounds) {
                bounds = meshRenderer.bounds;
            }

            Bounds adjustedBounds = bounds;
            if (useCustomBounds && boundsInLocalSpace) {
                adjustedBounds.center += pos;
            }
            mat.SetVector(ShaderParams.BoundsCenter, adjustedBounds.center);
            mat.SetVector(ShaderParams.BoundsExtents, adjustedBounds.extents);
            if (generatedType == LightType.Rectangle || generatedType == LightType.Disc) {
                if (mf != null && mf.sharedMesh != null) {
                    Bounds meshBounds = mf.sharedMesh.bounds; // non transformed real bounds
                    mat.SetVector(ShaderParams.MeshBoundsCenter, meshBounds.center);
                    mat.SetVector(ShaderParams.MeshBoundsExtents, meshBounds.extents);
                }
                float baseMultiplierComputed = (generatedAreaFrustumMultiplier - 1f) / generatedRange;
                if (generatedType == LightType.Rectangle) {
                    mat.SetVector(ShaderParams.AreaExtents, new Vector4(areaWidth * 0.5f, areaHeight * 0.5f, generatedRange, baseMultiplierComputed));
                } else {
                    mat.SetVector(ShaderParams.AreaExtents, new Vector4(areaWidth * areaWidth, areaHeight, generatedRange, baseMultiplierComputed));
                }
            }
        }


        public void UpdateMaterialProperties() {
            requireUpdateMaterial = true;
        }

        void UpdateMaterialPropertiesNow() {

            wasInRange = false;
            lastDistanceCheckTime = -999;

            mainCamera = null;
            ComputeDistanceToCamera();

            if (this == null || !isActiveAndEnabled || lightComp == null || (!lightComp.isActiveAndEnabled && !alwaysOn)) {
                ShadowsDisable();
                return;
            }

            SettingsValidate();

            if (meshRenderer == null) {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (fogMatLight == null) {
                if (meshRenderer != null) {
                    fogMatLight = meshRenderer.sharedMaterial;
                    // ensure this material is not used by other lights (can happen if user duplicates another light gameobject in scene)
                    foreach(VolumetricLight light in volumetricLights) {
                        if (light != null && light != this && light.meshRenderer != null && light.meshRenderer.sharedMaterial == fogMatLight) {
                            fogMatLight = null;
                            if (mf != null) mf.sharedMesh = null;
                            break;
                        }
                    }
                }
                if (fogMatLight == null) {
                    fogMatLight = new Material(Shader.Find("VolumetricLights/VolumetricLightURP"));
                    fogMatLight.hideFlags = HideFlags.DontSave;
                }
            }
            fogMat = fogMatLight;


            if (fogMat == null) return;

            SetFogMaterial();

            if (customRange < 0.001f) customRange = 0.001f;

            if (meshRenderer != null) {
                meshRenderer.sortingLayerID = sortingLayerID;
                meshRenderer.sortingOrder = sortingOrder;
            }
            fogMat.renderQueue = renderQueue;

            switch (blendMode) {
                case BlendMode.Additive:
                    fogMat.SetInt(ShaderParams.BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                    fogMat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.One);
                    fogMat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.One);
                    break;
                case BlendMode.Blend:
                    fogMat.SetInt(ShaderParams.BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                    fogMat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.One);
                    fogMat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.PreMultiply:
                    fogMat.SetInt(ShaderParams.BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                    fogMat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    fogMat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.One);
                    break;
                case BlendMode.Substractive:
                    fogMat.SetInt(ShaderParams.BlendOp, (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
                    fogMat.SetInt(ShaderParams.BlendSrc, (int)UnityEngine.Rendering.BlendMode.One);
                    fogMat.SetInt(ShaderParams.BlendDest, (int)UnityEngine.Rendering.BlendMode.One);
                    break;
            }
            fogMat.SetTexture(ShaderParams.NoiseTex, noiseTexture);
            fogMat.SetFloat(ShaderParams.NoiseStrength, noiseStrength);
            fogMat.SetFloat(ShaderParams.NoiseScale, 0.1f / noiseScale);
            fogMat.SetFloat(ShaderParams.NoiseFinalMultiplier, noiseFinalMultiplier);
            fogMat.SetFloat(ShaderParams.Penumbra, penumbra);
            fogMat.SetFloat(ShaderParams.RangeFallOff, rangeFallOff);
            fogMat.SetFloat(ShaderParams.Density, density);
            fogMat.SetFloat(ShaderParams.NearClipDistance, nearClipDistance);
            fogMat.SetVector(ShaderParams.DirectLightData, new Vector4(directLightMultiplier, directLightSmoothSamples, directLightSmoothRadius, 0));
            fogMat.SetVector(ShaderParams.FallOff, new Vector4(attenCoefConstant, attenCoefLinear, attenCoefQuadratic, 0));
            fogMat.SetVector(ShaderParams.RayMarchSettings, new Vector4(raymarchQuality, dithering * 0.001f, jittering, raymarchMinStep));
            fogMat.SetInt(ShaderParams.RayMarchMaxSteps, raymarchMaxSteps);
            if (jittering > 0) {
                if (blueNoiseTex == null) blueNoiseTex = Resources.Load<Texture2D>("Textures/blueNoiseVL128");
                fogMat.SetTexture(ShaderParams.BlueNoiseTexture, blueNoiseTex);
            }
            fogMat.SetInt(ShaderParams.FlipDepthTexture, flipDepthTexture ? 1 : 0);

            if (keywords == null) {
                keywords = new List<string>();
            } else {
                keywords.Clear();
            }

            if (useBlueNoise) {
                keywords.Add(ShaderParams.SKW_BLUENOISE);
            }
            if (useNoise) {
                keywords.Add(ShaderParams.SKW_NOISE);
            }
            switch (lightComp.type) {
                case LightType.Spot:
                    if (cookieTexture != null) {
                        keywords.Add(ShaderParams.SKW_SPOT_COOKIE);
                        fogMat.SetTexture(ShaderParams.CookieTexture, cookieTexture);
                        fogMat.SetVector(ShaderParams.CookieTexture_ScaleAndSpeed, new Vector4(cookieScale.x, cookieScale.y, cookieSpeed.x, cookieSpeed.y));
                        fogMat.SetVector(ShaderParams.CookieTexture_Offset, new Vector4(cookieOffset.x, cookieOffset.y, 0, 0));
                    } else {
                        keywords.Add(ShaderParams.SKW_SPOT);
                    }
                    break;
                case LightType.Point: keywords.Add(ShaderParams.SKW_POINT); break;
                case LightType.Rectangle: keywords.Add(ShaderParams.SKW_AREA_RECT); break;
                case LightType.Disc: keywords.Add(ShaderParams.SKW_AREA_DISC); break;
            }
            if (attenuationMode == AttenuationMode.Quadratic) {
                keywords.Add(ShaderParams.SKW_PHYSICAL_ATTEN);
            }
            if (castDirectLight) {
                keywords.Add(directLightBlendMode == DirectLightBlendMode.Additive ? ShaderParams.SKW_CAST_DIRECT_LIGHT_ADDITIVE : ShaderParams.SKW_CAST_DIRECT_LIGHT_BLEND);
            }
            if (useCustomBounds) {
                keywords.Add(ShaderParams.SKW_CUSTOM_BOUNDS);
            }

            ShadowsSupportCheck();
            if (enableShadows) {
                if (usesCubemap) {
                    keywords.Add(ShaderParams.SKW_SHADOWS_CUBEMAP);
                } else if (usesTranslucency) {
                    keywords.Add(ShaderParams.SKW_SHADOWS_TRANSLUCENCY);
                } else {
                    keywords.Add(ShaderParams.SKW_SHADOWS);
                }
            }
#if UNITY_2021_3_OR_NEWER
            fogMat.enabledKeywords = null;
#endif
            fogMat.shaderKeywords = keywords.ToArray();

            ParticlesCheckSupport();

            if (OnPropertiesChanged != null) {
                OnPropertiesChanged(this);
            }
        }

        void SetFogMaterial() {
            if (meshRenderer != null) {
                if (density <= 0 || mediumAlbedo.a == 0) {
                    if (fogMatInvisible == null) {
                        fogMatInvisible = new Material(Shader.Find("VolumetricLights/Invisible"));
                        fogMatInvisible.hideFlags = HideFlags.DontSave;
                    }
                    meshRenderer.sharedMaterial = fogMatInvisible;
                } else {
                    meshRenderer.sharedMaterial = fogMat;
                }
            }
        }

        /// <summary>
        /// Creates an automatic profile if profile is not set
        public void CheckProfile() {
            if (profile != null) {
                if ("Auto".Equals(profile.name)) {
                    profile.ApplyTo(this);
                    profile = null;
                } else if (profileSync) {
                    profile.ApplyTo(this);
                }
            }
        }

        /// <summary>
        /// Gets bounds in world space
        /// </summary>
        public Bounds GetBounds() {
            Bounds bounds = this.bounds;
            if (useCustomBounds && boundsInLocalSpace) {
                bounds.center += transform.position;
            }
            return bounds;
        }


        /// <summary>
        /// Sets bounds in world space
        /// </summary>
        public void SetBounds(Bounds bounds) {
            if (useCustomBounds && boundsInLocalSpace) {
                bounds.center -= transform.position;
            }
            this.bounds = bounds;
            UpdateVolumeGeometry();
        }
    }
}
