Shader "VolumetricLights/DustParticles"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _BumpMap("Normal Map", 2D) = "bump" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        // -------------------------------------
        // Particle specific
        _SoftParticlesNearFadeDistance("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance("Soft Particles Far Fade", Float) = 1.0
        _CameraNearFadeDistance("Camera Near Fade", Float) = 1.0
        _CameraFarFadeDistance("Camera Far Fade", Float) = 2.0
        _DistortionBlend("Distortion Blend", Float) = 0.5
        _DistortionStrength("Distortion Strength", Float) = 1.0

        // -------------------------------------
        // Hidden properties - Generic
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__mode", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        // Particle specific
        [HideInInspector] _ColorMode("_ColorMode", Float) = 0.0
        [HideInInspector] _BaseColorAddSubDiff("_ColorMode", Vector) = (0,0,0,0)
        [ToggleOff] _FlipbookBlending("__flipbookblending", Float) = 0.0
        [HideInInspector] _SoftParticlesEnabled("__softparticlesenabled", Float) = 0.0
        [HideInInspector] _CameraFadingEnabled("__camerafadingenabled", Float) = 0.0
        [HideInInspector] _SoftParticleFadeParams("__softparticlefadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _CameraFadeParams("__camerafadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _DistortionEnabled("__distortionenabled", Float) = 0.0
        [HideInInspector] _DistortionStrengthScaled("Distortion Strength Scaled", Float) = 0.1
	    [HideInInspector] _ParticleDistanceAtten ("Distance Atten", Float) = 10

        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _FlipbookMode("flipbook", Float) = 0
        [HideInInspector] _Mode("mode", Float) = 0
        [HideInInspector] _Color("color", Color) = (1,1,1,1)

		[HideInInspector] _BoundsCenter("Bounds Center", Vector) = (0,0,0)
		[HideInInspector] _BoundsExtents("Bounds Size", Vector) = (0,0,0)
		[HideInInspector] _ConeTipData("Cone Tip Data", Vector) = (0,0,0,0.1)
		[HideInInspector] _ExtraGeoData("Extra Geo Data", Vector) = (1.0, 0, 0)
        [HideInInspector] _Border("Border", Float) = 0.1
        [HideInInspector] _DistanceFallOff("Length Falloff", Float) = 0
        [HideInInspector] _NearClipDistance("Near Clip Distance", Float) = 0
        [HideInInspector] _FallOff("FallOff Physical", Vector) = (1.0, 2.0, 1.0)
        [HideInInspector] _ConeAxis("Cone Axis", Vector) = (0,0,0,0.5)
        [HideInInspector] _AreaExtents("Area Extents", Vector) = (0,0,0,1)
        [HideInInspector] _ParticleLightColor("Particle Light Color", Color) = (1,1,1,1)
        [HideInInspector] _ShadowIntensity("Shadow Intensity", Vector) = (0,1,0,0)
		[HideInInspector] _Cookie2D("Cookie (2D)", 2D) = "black" {}
		[HideInInspector] _Cookie2D_SS("Cookie (Scale and Speed)", Vector) = (1,1,0,0)
        [HideInInspector] _Cookie2D_Offset("Cookie (Offset)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags{"RenderType" = "Transparent" "Queue"="Transparent+1" "IgnoreProjector" = "True" "PreviewType" = "Plane" "PerformanceChecks" = "False" "RenderPipeline" = "UniversalPipeline"}

        // ------------------------------------------------------------------
        //  Forward pass.
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"

            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            ColorMask RGB

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Particle Keywords
//            #pragma shader_feature _SOFTPARTICLES_ON
//            #pragma shader_feature _FADING_ON
//            #pragma shader_feature _DISTORTION_ON

            #pragma multi_compile_local VL_SPOT VL_SPOT_COOKIE VL_POINT VL_AREA_RECT VL_AREA_DISC 
            #pragma multi_compile_local _ VL_SHADOWS VL_SHADOWS_CUBEMAP VL_SHADOWS_TRANSLUCENCY
    		#pragma multi_compile_local _ VL_PHYSICAL_ATTEN
            #pragma shader_feature_local _ VL_CUSTOM_BOUNDS

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog

            #pragma vertex vertParticleUnlit
            #pragma fragment fragParticleUnlit

            #undef unity_WorldToObject
            #define unity_WorldToObject _WorldToLocal

            #include "DustParticlesInput.hlsl"
            #include "DustParticlesForwardPass.hlsl"

            ENDHLSL
        }
    }
}
