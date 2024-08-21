//------------------------------------------------------------------------------------------------------------------
// Volumetric Lights
// Created by Kronnect
//------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;

namespace VolumetricLights {

    public partial class VolumetricLight : MonoBehaviour {

        #region Particle support

        const string PARTICLE_SYSTEM_NAME = "DustParticles";

        Material particleMaterial;

        [NonSerialized]
        public ParticleSystem ps;

        ParticleSystemRenderer psRenderer;
        Vector3 psLastPos;
        Quaternion psLastRot;

        void ParticlesDisable() {
            if (Application.isPlaying) {
                if (psRenderer != null) {
                    psRenderer.enabled = false;
                }
            } else {
                if (ps != null) {
                    ps.gameObject.SetActive(false);
                }
            }
        }

        void ParticlesResetIfTransformChanged() {
            if (ps != null && (ps.transform.position != psLastPos || ps.transform.rotation != psLastRot)) {
                ParticlesPopulate();
            }
        }

        void ParticlesPopulate() {
            if (dustPrewarm) {
                ps.Clear();
                ps.Simulate(100);
            }
            psLastPos = ps.transform.position;
            psLastRot = ps.transform.rotation;
        }

        void ParticlesCheckSupport() {
            if (!enableDustParticles) {
                ParticlesDisable();
                return;
            }

            bool psNew = false;
            if (ps == null) {
                Transform childPS = transform.Find(PARTICLE_SYSTEM_NAME);
                if (childPS != null) {
                    ps = childPS.GetComponent<ParticleSystem>();
                    if (ps == null) {
                        DestroyImmediate(childPS.gameObject);
                    }
                }
                if (ps == null) {
                    GameObject psObj = Resources.Load<GameObject>("Prefabs/DustParticles") as GameObject;
                    if (psObj == null) return;
                    psObj = Instantiate(psObj);
                    psObj.name = PARTICLE_SYSTEM_NAME;
                    psObj.transform.SetParent(transform, false);
                    ps = psObj.GetComponent<ParticleSystem>();
                }
                ps.gameObject.layer = 1;
                psNew = true;
            }

            if (particleMaterial == null) {
                particleMaterial = Instantiate(Resources.Load<Material>("Materials/DustParticle")) as Material;
            }

            if (keywords == null) {
                keywords = new List<string>();
            } else {
                keywords.Clear();
            }

            // Configure particle material
            if (useCustomBounds) {
                keywords.Add(ShaderParams.SKW_CUSTOM_BOUNDS);
            }

            switch (generatedType) {
                case LightType.Spot:
                    if (cookieTexture != null) {
                        keywords.Add(ShaderParams.SKW_SPOT_COOKIE);
                        particleMaterial.SetTexture(ShaderParams.CookieTexture, cookieTexture);
                        particleMaterial.SetVector(ShaderParams.CookieTexture_ScaleAndSpeed, new Vector4(cookieScale.x, cookieScale.y, cookieSpeed.x, cookieSpeed.y));
                        particleMaterial.SetVector(ShaderParams.CookieTexture_Offset, new Vector4(cookieOffset.x, cookieOffset.y, 0, 0));
                        particleMaterial.SetFloat(ShaderParams.NearClipDistance, nearClipDistance);
                    } else {
                        keywords.Add(ShaderParams.SKW_SPOT);
                    }
                    break;
                case LightType.Point:
                    keywords.Add(ShaderParams.SKW_POINT);
                    break;
                case LightType.Rectangle:
                    keywords.Add(ShaderParams.SKW_AREA_RECT);
                    break;
                case LightType.Disc:
                    keywords.Add(ShaderParams.SKW_AREA_DISC);
                    break;
            }
            if (attenuationMode == AttenuationMode.Quadratic) {
                keywords.Add(ShaderParams.SKW_PHYSICAL_ATTEN);
            }
            if (enableShadows) {
                if (usesCubemap) {
                    keywords.Add(ShaderParams.SKW_SHADOWS_CUBEMAP);
                } else if (usesTranslucency) {
                    keywords.Add(ShaderParams.SKW_SHADOWS_TRANSLUCENCY);
                } else {
                    keywords.Add(ShaderParams.SKW_SHADOWS);
                }
            }
            particleMaterial.shaderKeywords = keywords.ToArray();

            particleMaterial.renderQueue = renderQueue + 1;
            particleMaterial.SetFloat(ShaderParams.Penumbra, penumbra);
            particleMaterial.SetFloat(ShaderParams.RangeFallOff, rangeFallOff);
            particleMaterial.SetVector(ShaderParams.FallOff, new Vector3(attenCoefConstant, attenCoefLinear, attenCoefQuadratic));
            UpdateParticleColor();
            particleMaterial.SetFloat(ShaderParams.ParticleDistanceAtten, dustDistanceAttenuation * dustDistanceAttenuation);
            if (psRenderer == null) {
                psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            }
            psRenderer.material = particleMaterial;

            // Main properties
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.MinMaxCurve startSize = main.startSize;
            startSize.mode = ParticleSystemCurveMode.TwoConstants;
            startSize.constantMin = dustMinSize;
            startSize.constantMax = dustMaxSize;
            main.startSize = startSize;

            // Set emission bounds
            ParticleSystem.ShapeModule shape = ps.shape;
            switch (generatedType) {
                case LightType.Spot:
                    shape.shapeType = ParticleSystemShapeType.ConeVolume;
                    shape.angle = generatedSpotAngle * 0.5f;
                    shape.position = Vector3.zero;
                    shape.radius = tipRadius;
                    shape.length = generatedRange;
                    shape.scale = Vector3.one;
                    break;
                case LightType.Point:
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.position = Vector3.zero;
                    shape.scale = Vector3.one;
                    shape.radius = generatedRange;
                    break;
                case LightType.Rectangle:
                case LightType.Disc:
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.position = new Vector3(0, 0, generatedRange * 0.5f);
                    shape.scale = GetComponent<MeshFilter>().sharedMesh.bounds.size;
                    break;
            }

            // Set wind speed
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            Vector3 windDirection = transform.InverseTransformDirection(this.windDirection);
            ParticleSystem.MinMaxCurve velx = velocity.x;
            velx.constantMin = (-0.1f + windDirection.x) * dustWindSpeed;
            velx.constantMax = (0.1f + windDirection.x) * dustWindSpeed;
            velocity.x = velx;
            ParticleSystem.MinMaxCurve vely = velocity.y;
            vely.constantMin = (-0.1f + windDirection.y) * dustWindSpeed;
            vely.constantMax = (0.1f + windDirection.y) * dustWindSpeed;
            velocity.y = vely;
            ParticleSystem.MinMaxCurve velz = velocity.z;
            velz.constantMin = (-0.1f + windDirection.z) * dustWindSpeed;
            velz.constantMax = (0.1f + windDirection.z) * dustWindSpeed;
            velocity.z = velz;

            if (!ps.gameObject.activeSelf) {
                ps.gameObject.SetActive(true);
            }

            UpdateParticlesVisibility();

            if (psNew || ps.particleCount == 0) {
                ParticlesPopulate();
            }

            if (!ps.isPlaying) {
                ps.Play();
            }
        }


        void UpdateParticlesVisibility() {
            UpdateParticleColor();

            if (!Application.isPlaying || psRenderer == null) return;

            bool visible = meshRenderer.isVisible;
            if (visible && dustAutoToggle) {
                float maxDistSqr = dustDistanceDeactivation * dustDistanceDeactivation;
                visible = distanceToCameraSqr <= maxDistSqr;
            }
            if (visible) {
                if (!psRenderer.enabled) {
                    psRenderer.enabled = true;
                }
            } else { 
                if (psRenderer.enabled) {
                    psRenderer.enabled = false;
                }
            }
        }

        void UpdateParticleColor() {
            if (particleMaterial != null) {
                particleMaterial.SetColor(ShaderParams.ParticleLightColor, lightComp.color * mediumAlbedo * (lightComp.intensity * dustBrightness));
            }
        }


        #endregion

    }


}
