using UnityEngine;
using UnityEngine.Serialization;

namespace VolumetricLights {

    public enum ShadowResolution {
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    public enum ShadowBakeInterval {
        OnStart,
        EveryFrame
    }

    public enum AttenuationMode {
        Simple,
        Quadratic
    }

    public enum RaymarchPresets {
        UserDefined = 0,
        Default = 10,
        Faster = 20,
        EvenFaster = 30,
        LightSpeed = 40
    }

    public enum BlendMode {
        Additive = 0,
        Blend = 1,
        PreMultiply = 2,
        Substractive = 3
    }

    public enum ShadowOrientation {
        ToCamera,
        FixedDirection
    }

    public enum ShadowBakeMode {
        HalfSphere,
        CubemapOneFacePerFrame,
        CubemapAll6FacesPerFrame
    }

    public enum DirectLightBlendMode { 
        Additive = 0,
        Blend = 1
    }


    [CreateAssetMenu(menuName = "Volumetric Light Profile", fileName = "VolumetricLightProfile", order = 335)]
    public class VolumetricLightProfile : ScriptableObject {

        [Header("Rendering")]
        public BlendMode blendMode = BlendMode.Additive;

        public RaymarchPresets raymarchPreset = RaymarchPresets.UserDefined;

        [Tooltip("Determines the general accuracy of the effect. The greater this value, the more accurate effect (shadow occlusion as well). Try to keep this value as low as possible while maintainig a good visual result. If you need better performance increase the 'Raymarch Min Step' and then 'Jittering' amount to improve quality.")]
        [Range(1, 256)] public int raymarchQuality = 8;

        [Tooltip("Determines the minimum step size. Increase to improve performance / decrease to improve accuracy. When increasing this value, you can also increase 'Jittering' amount to improve quality.")]
        public float raymarchMinStep = 0.1f;

        [Tooltip("Maximum number of raymarch steps. This value represents a hard maximum, usually each ray uses less samples but it can be topped by lowering this value.")]
        public int raymarchMaxSteps = 200;

        [Tooltip("Increase to reduce inaccuracy due to low number of samples (due to a high raymarch step size).")]
        public float jittering = 0.5f;

        [Tooltip("Increase to reduce banding artifacts. Usually jittering has a bigger impact in reducing artifacts.")]
        [Range(0, 2)] public float dithering = 1f;

        [Tooltip("Uses blue noise for jittering computation reducing moiré pattern of normal jitter. Usually not needed unless you use shadow occlusion. It adds an additional texture fetch so use only if it provides you a clear visual improvement.")]
        public bool useBlueNoise;

        public bool animatedBlueNoise = true;

        [Tooltip("The render queue for this renderer. By default, all transparent objects use a render queue of 3000. Use a lower value to render before all transparent objects.")]
        public int renderQueue = 3101;

        [Tooltip("Optional sorting layer Id (number) for this renderer. By default 0. Usually used to control the order with other transparent renderers, like Sprite Renderer.")]
        public int sortingLayerID;

        [Tooltip("Optional sorting order for this renderer. Used to control the order with other transparent renderers, like Sprite Renderer.")]
        public int sortingOrder;

        [Tooltip("Use only if depth texture is inverted. Alternatively you can enable MSAA, HDR or change the render scale in the pipeline asset.")]
        public bool flipDepthTexture;

        [Tooltip("Ignores light enable state. Always show volumetric fog. This option is useful to produce fake volumetric lights.")]
        public bool alwaysOn;

        [Tooltip("If the volumetric light effect may illuminate the objects. Only available in deferred rendering path.")]
        public bool castDirectLight;

        [Tooltip("Multiplier for the direct light")]
        public float directLightMultiplier = 1f;

        [Tooltip("Number of samples to creates smooth shadows")]
        [Range(1, 32)]
        public int directLightSmoothSamples = 8;

        [Tooltip("Radius for the smooth shadow")]
        [Range(0, 8)]
        public float directLightSmoothRadius = 4;

        [Tooltip("Blending mode for the direct light effect. Blend mode requires deferred rendering mode and is slower.")]
        public DirectLightBlendMode directLightBlendMode = DirectLightBlendMode.Additive;

        [Tooltip("Fully enable/disable volumetric effect when far from main camera in order to optimize performance")]
        public bool autoToggle;
        [Tooltip("Distance to the light source at which the volumetric effect starts to dim. Note that the distance is to the light position regardless of its light range or volume so you should consider the light area or range into this distance as well.")]
        public float distanceStartDimming = 70f;
        [Tooltip("Distance to the light source at which the volumetric effect is fully deactivated. Note that the distance is to the light position regardless of its light range or volume so you should consider the light area or range into this distance as well.")]
        public float distanceDeactivation = 100f;
        [Tooltip("Minimum time between distance checks")]
        public float autoToggleCheckInterval = 1f;

        [Header("Appearance")]
        public bool useNoise = true;
        public Texture3D noiseTexture;
        [Range(0, 3)] public float noiseStrength = 1f;
        public float noiseScale = 5f;
        public float noiseFinalMultiplier = 1f;

        public float density = 0.2f;

        public Color mediumAlbedo = Color.white;

        [Tooltip("Overall brightness multiplier.")]
        public float brightness = 1f;

        [Tooltip("Attenuation Mode")]
        public AttenuationMode attenuationMode = AttenuationMode.Simple;

        [Tooltip("Constant coefficient (a) of the attenuation formula. By modulating these coefficients, you can tweak the attenuation quadratic curve 1/(a + b*x + c*x*x).")]
        public float attenCoefConstant = 1f;

        [Tooltip("Linear coefficient (b) of the attenuation formula. By modulating these coefficients, you can tweak the attenuation quadratic curve 1/(a + b*x + c*x*x).")]
        public float attenCoefLinear = 2f;

        [Tooltip("Quadratic coefficient (c) of the attenuation formula. By modulating these coefficients, you can tweak the attenuation quadratic curve 1/(a + b*x + c*x*x).")]
        public float attenCoefQuadratic = 1f;

        [Tooltip("Attenuation of light intensity based on square of distance. Plays with brightness to achieve a more linear or realistic (quadratic attenuation effect).")]
        [FormerlySerializedAs("distanceFallOff")]
        public float rangeFallOff = 1f;

        [Tooltip("Brightiness increase when looking against light source.")]
        public float diffusionIntensity;

        [Range(0, 1), Tooltip("Smooth edges")]
        [FormerlySerializedAs("border")]
        public float penumbra = 0.5f;

        [Header("Spot Light")]
        [Tooltip("Radius of the tip of the cone. Only applies to spot lights.")] public float tipRadius;
        public float nearClipDistance;
        [Tooltip("Custom cookie texture (RGB).")] public Texture2D cookieTexture;

        public Vector2 cookieScale = Vector2.one;
        public Vector2 cookieOffset;
        public Vector2 cookieSpeed;

        [Header("Area Light")]
        [Range(0f, 80f)] public float frustumAngle;

        [Header("Animation")]
        [Tooltip("Noise animation direction and speed.")]
        public Vector3 windDirection = new Vector3(0.03f, 0.02f, 0);

        [Header("Dust Particles")]
        public bool enableDustParticles;
        public float dustBrightness = 1f;
        public float dustMinSize = 0.01f;
        public float dustMaxSize = 0.02f;
        public float dustWindSpeed = 1f;
        [Tooltip("Dims particle intensity beyond this distance to camera")]
        public float dustDistanceAttenuation = 10f;
        [Tooltip("Fully enable/disable particle system renderer when far from main camera in order to optimize performance")]
        public bool dustAutoToggle;
        [Tooltip("Distance to the light source at which the particule system is fully deactivated. Note that the distance is to the light position regardless of its light range or volume so you should consider the light area or range into this distance as well.")]
        public float dustDistanceDeactivation = 70f;
        [Tooltip("Prewarms/populates dust when the volumetric light is enabled to ensure there're enough visible particles from start. Disabling this option can improve performance when many lights are activated at the same time.")]
        public bool dustPrewarm = true;

        [Header("Shadow Occlusion")]
        public bool enableShadows;
        public float shadowIntensity = 0.7f;
        public Color shadowColor = new Color(0, 0, 0, 1);
        [Tooltip("Enable translucent shadow map")]
        public bool shadowTranslucency;
        [Tooltip("Customizable intensity for the translucent map sampling")]
        public float shadowTranslucencyIntensity = 1f;
        [Tooltip("Amount of colorization")]
        [Range(0, 1)] public float shadowTranslucencyBlend = 0.5f;
        public ShadowResolution shadowResolution = ShadowResolution._256;
        public LayerMask shadowCullingMask = ~2;
        public ShadowBakeInterval shadowBakeInterval = ShadowBakeInterval.OnStart;
        public float shadowNearDistance = 0.1f;
        [Tooltip("Fully enable/disable shadows when far from main camera in order to optimize performance")]
        public bool shadowAutoToggle;
        [Tooltip("Max distance to main camera to render shadows for this volumetric light.")]
        public float shadowDistanceDeactivation = 250f;
        [Tooltip("Compute shadows in a half-sphere oriented to camera (faster) or in a cubemap but render one face per frame (slower) or all 6 faces per frame (slowest).")]
        public ShadowBakeMode shadowBakeMode = ShadowBakeMode.HalfSphere;
        public ShadowOrientation shadowOrientation = ShadowOrientation.ToCamera;
        public Vector3 shadowDirection = Vector3.down;

        private void OnEnable() {
            if (noiseTexture == null) {
                noiseTexture = Resources.Load<Texture3D>("Textures/NoiseTex3D1");
            }
        }

        private void OnValidate() {

            switch (raymarchPreset) {
                case RaymarchPresets.Default:
                    raymarchQuality = 8;
                    raymarchMinStep = 0.1f;
                    jittering = 0.5f;
                    break;
                case RaymarchPresets.Faster:
                    raymarchQuality = 4;
                    raymarchMinStep = 0.2f;
                    jittering = 1f;
                    break;
                case RaymarchPresets.EvenFaster:
                    raymarchQuality = 2;
                    raymarchMinStep = 1f;
                    jittering = 4f;
                    break;
                case RaymarchPresets.LightSpeed:
                    raymarchQuality = 1;
                    raymarchMinStep = 8f;
                    jittering = 4f;
                    break;
            }

            tipRadius = Mathf.Max(0, tipRadius);
            nearClipDistance = Mathf.Max(0, nearClipDistance);
            density = Mathf.Max(0, density);
            noiseScale = Mathf.Max(0.1f, noiseScale);
            diffusionIntensity = Mathf.Max(0, diffusionIntensity);
            dustMaxSize = Mathf.Max(dustMaxSize, dustMinSize);
            rangeFallOff = Mathf.Max(rangeFallOff, 0);
            brightness = Mathf.Max(brightness, 0);
            penumbra = Mathf.Max(0.002f, penumbra);
            attenCoefConstant = Mathf.Max(0.0001f, attenCoefConstant);
            attenCoefLinear = Mathf.Max(0, attenCoefLinear);
            attenCoefQuadratic = Mathf.Max(0, attenCoefQuadratic);
            dustBrightness = Mathf.Max(0, dustBrightness);
            dustMinSize = Mathf.Max(0, dustMinSize);
            dustMaxSize = Mathf.Max(0, dustMaxSize);
            shadowNearDistance = Mathf.Max(0, shadowNearDistance);
            dustDistanceAttenuation = Mathf.Max(0, dustDistanceAttenuation);
            raymarchMinStep = Mathf.Max(0.1f, raymarchMinStep);
            jittering = Mathf.Max(0, jittering);
            dithering = Mathf.Max(0, dithering);
            distanceStartDimming = Mathf.Max(0, distanceStartDimming);
            distanceDeactivation = Mathf.Max(0, distanceDeactivation);
            distanceStartDimming = Mathf.Min(distanceStartDimming, distanceDeactivation);
            shadowIntensity = Mathf.Max(0, shadowIntensity);
            raymarchMaxSteps = Mathf.Max(1, raymarchMaxSteps);
            shadowTranslucencyIntensity = Mathf.Max(0, shadowTranslucencyIntensity);

            // Update all lights using this profile
            VolumetricLight[] vls = FindObjectsOfType<VolumetricLight>();
            for (int k = 0; k < vls.Length; k++) {
                VolumetricLight vl = vls[k];
                if (vl != null && vl.profileSync && vl.profile == this) {
                    ApplyTo(vl);
                }
            }
        }

        public void ApplyTo(VolumetricLight vl) {
            vl.blendMode = blendMode;
            vl.flipDepthTexture = flipDepthTexture;
            vl.raymarchPreset = raymarchPreset;
            vl.raymarchMinStep = raymarchMinStep;
            vl.raymarchQuality = raymarchQuality;
            vl.raymarchMaxSteps = raymarchMaxSteps;
            vl.jittering = jittering;
            vl.dithering = dithering;
            vl.useBlueNoise = useBlueNoise;
            vl.animatedBlueNoise = animatedBlueNoise;
            vl.renderQueue = renderQueue;
            vl.sortingLayerID = sortingLayerID;
            vl.sortingOrder = sortingOrder;
            vl.alwaysOn = alwaysOn;
            vl.castDirectLight = castDirectLight;
            vl.directLightMultiplier = directLightMultiplier;
            vl.directLightSmoothSamples = directLightSmoothSamples;
            vl.directLightSmoothRadius = directLightSmoothRadius;
            vl.directLightBlendMode = directLightBlendMode;
            vl.autoToggle = autoToggle;
            vl.distanceStartDimming = distanceStartDimming;
            vl.distanceDeactivation = distanceDeactivation;
            vl.autoToggleCheckInterval = autoToggleCheckInterval;
            vl.useNoise = useNoise;
            vl.noiseTexture = noiseTexture;
            vl.noiseStrength = noiseStrength;
            vl.noiseScale = noiseScale;
            vl.noiseFinalMultiplier = noiseFinalMultiplier;
            vl.density = density;
            vl.mediumAlbedo = mediumAlbedo;
            vl.brightness = brightness;
            vl.attenuationMode = attenuationMode;
            vl.attenCoefConstant = attenCoefConstant;
            vl.attenCoefLinear = attenCoefLinear;
            vl.attenCoefQuadratic = attenCoefQuadratic;
            vl.rangeFallOff = rangeFallOff;
            vl.diffusionIntensity = diffusionIntensity;
            vl.penumbra = penumbra;
            vl.tipRadius = tipRadius;
            vl.nearClipDistance = nearClipDistance;
            vl.cookieTexture = cookieTexture;
            vl.cookieScale = cookieScale;
            vl.cookieSpeed = cookieSpeed;
            vl.cookieOffset = cookieOffset;
            vl.frustumAngle = frustumAngle;
            vl.windDirection = windDirection;
            vl.enableDustParticles = enableDustParticles;
            vl.dustBrightness = dustBrightness;
            vl.dustMinSize = dustMinSize;
            vl.dustMaxSize = dustMaxSize;
            vl.dustWindSpeed = dustWindSpeed;
            vl.dustDistanceAttenuation = dustDistanceAttenuation;
            vl.dustAutoToggle = dustAutoToggle;
            vl.dustDistanceDeactivation = dustDistanceDeactivation;
            vl.dustPrewarm = dustPrewarm;
            vl.enableShadows = enableShadows;
            vl.shadowIntensity = shadowIntensity;
            vl.shadowColor = shadowColor;
            vl.shadowTranslucency = shadowTranslucency;
            vl.shadowTranslucencyIntensity = shadowTranslucencyIntensity;
            vl.shadowTranslucencyBlend = shadowTranslucencyBlend;
            vl.shadowResolution = shadowResolution;
            vl.shadowCullingMask = shadowCullingMask;
            vl.shadowBakeInterval = shadowBakeInterval;
            vl.shadowNearDistance = shadowNearDistance;
            vl.shadowAutoToggle = shadowAutoToggle;
            vl.shadowDistanceDeactivation = shadowDistanceDeactivation;
            vl.shadowBakeMode = shadowBakeMode;
            vl.shadowOrientation = shadowOrientation;
            vl.shadowDirection = shadowDirection;
            vl.UpdateMaterialProperties();
        }


        public void LoadFrom(VolumetricLight vl) {
            blendMode = vl.blendMode;
            flipDepthTexture = vl.flipDepthTexture;
            raymarchPreset = vl.raymarchPreset;
            raymarchMinStep = vl.raymarchMinStep;
            raymarchQuality = vl.raymarchQuality;
            raymarchMaxSteps = vl.raymarchMaxSteps;
            jittering = vl.jittering;
            dithering = vl.dithering;
            useBlueNoise = vl.useBlueNoise;
            animatedBlueNoise = vl.animatedBlueNoise;
            renderQueue = vl.renderQueue;
            sortingLayerID = vl.sortingLayerID;
            sortingOrder = vl.sortingOrder;
            alwaysOn = vl.alwaysOn;
            castDirectLight = vl.castDirectLight;
            directLightMultiplier = vl.directLightMultiplier;
            directLightSmoothSamples = vl.directLightSmoothSamples;
            directLightSmoothRadius = vl.directLightSmoothRadius;
            directLightBlendMode = vl.directLightBlendMode;
            autoToggle = vl.autoToggle;
            distanceStartDimming = vl.distanceStartDimming;
            distanceDeactivation = vl.distanceDeactivation;
            autoToggleCheckInterval = vl.autoToggleCheckInterval;
            useNoise = vl.useNoise;
            noiseTexture = vl.noiseTexture;
            noiseStrength = vl.noiseStrength;
            noiseScale = vl.noiseScale;
            noiseFinalMultiplier = vl.noiseFinalMultiplier;
            density = vl.density;
            mediumAlbedo = vl.mediumAlbedo;
            brightness = vl.brightness;
            attenuationMode = vl.attenuationMode;
            attenCoefConstant = vl.attenCoefConstant;
            attenCoefLinear = vl.attenCoefLinear;
            attenCoefQuadratic = vl.attenCoefQuadratic;
            rangeFallOff = vl.rangeFallOff;
            diffusionIntensity = vl.diffusionIntensity;
            penumbra = vl.penumbra;
            tipRadius = vl.tipRadius;
            nearClipDistance = vl.nearClipDistance;
            cookieTexture = vl.cookieTexture;
            cookieScale = vl.cookieScale;
            cookieOffset = vl.cookieOffset;
            cookieSpeed = vl.cookieSpeed;
            frustumAngle = vl.frustumAngle;
            windDirection = vl.windDirection;
            enableDustParticles = vl.enableDustParticles;
            dustBrightness = vl.dustBrightness;
            dustMinSize = vl.dustMinSize;
            dustMaxSize = vl.dustMaxSize;
            dustWindSpeed = vl.dustWindSpeed;
            dustDistanceAttenuation = vl.dustDistanceAttenuation;
            dustPrewarm = vl.dustPrewarm;
            dustAutoToggle = vl.dustAutoToggle;
            dustDistanceDeactivation = vl.dustDistanceDeactivation;
            enableShadows = vl.enableShadows;
            shadowIntensity = vl.shadowIntensity;
            shadowColor = vl.shadowColor;
            shadowTranslucency = vl.shadowTranslucency;
            shadowTranslucencyIntensity = vl.shadowTranslucencyIntensity;
            shadowTranslucencyBlend = vl.shadowTranslucencyBlend;
            shadowResolution = vl.shadowResolution;
            shadowCullingMask = vl.shadowCullingMask;
            shadowBakeInterval = vl.shadowBakeInterval;
            shadowNearDistance = vl.shadowNearDistance;
            shadowAutoToggle = vl.shadowAutoToggle;
            shadowDistanceDeactivation = vl.shadowDistanceDeactivation;
            shadowBakeMode = vl.shadowBakeMode;
            shadowOrientation = vl.shadowOrientation;
            shadowDirection = vl.shadowDirection;
        }

    }
}