Shader "VolumetricLights/VolumetricLightURP"
{
	Properties
	{
		[HideInInspector] _MainTex("Main Texture", 2D) = "white" {}
		[HideInInspector] _NoiseTex("Noise Texture", Any) = "white" {}
		[HideInInspector] _NoiseScale("Noise Scale", Range(0.001, 0.04)) = 0.025
		[HideInInspector] _Color("Color", Color) = (1,1,1)
		[HideInInspector] _Density("Density", Float) = 1.0
		[HideInInspector] _NoiseStrength("Noise Strength", Float) = 1.0
		[HideInInspector] _NoiseScale("Noise Scale", Float) = 1.0
        [HideInInspector] _NoiseFinalMultiplier("Noise Final Multiplier", Float) = 1.0
		[HideInInspector] _RayMarchSettings("Raymarch Settings", Vector) = (2, 0.01, 1.0, 0.1)
		[HideInInspector] _WindDirection("Wind Direction", Vector) = (1, 0, 0)
		[HideInInspector] _BoundsCenter("Bounds Center", Vector) = (0,0,0)
		[HideInInspector] _BoundsExtents("Bounds Size", Vector) = (0,0,0)
		[HideInInspector] _MeshBoundsCenter("Non transformed Bounds Center", Vector) = (0,0,0)
		[HideInInspector] _MeshBoundsExtents("Non transformed Bounds Size", Vector) = (0,0,0)
		[HideInInspector] _ConeBoundsCenter("Cone Bounds Center", Vector) = (0,0,0)
		[HideInInspector] _ConeBoundsExtents("Cone Bounds Extents", Vector) = (1,1,1)
		[HideInInspector] _ConeTipData("Cone Tip Data", Vector) = (0,0,0,0.1)
		[HideInInspector] _ExtraGeoData("Extra Geometry Data", Vector) = (1.0, 0, 0)
        [HideInInspector] _Border("Border", Float) = 0.1
        [HideInInspector] _DistanceFallOff("Length Falloff", Float) = 0
        [HideInInspector] _NearClipDistance("Near Clip Distance", Float) = 0
        [HideInInspector] _FallOff("FallOff Physical", Vector) = (1.0, 2.0, 1.0)
        [HideInInspector] _ConeAxis("Cone Axis", Vector) = (0,0,0,0.5)
        [HideInInspector] _AreaExtents("Area Extents", Vector) = (0,0,0,1)
        [HideInInspector] _LightColor("Light Color", Color) = (1,1,1)
        [HideInInspector] _ToLightDir("To Light Dir", Vector) = (1,1,1,0)
        [HideInInspector] _BlendSrc("Blend Src", Int) = 1
        [HideInInspector] _BlendDest("Blend Dest", Int) = 1
		[HideInInspector] _BlendOp("Blend Op", Int) = 0
        [HideInInspector] _BlueNoise("Blue Noise", 2D) = "black" {}
		[HideInInspector] _Cookie2D("Cookie (2D)", 2D) = "black" {}
		[HideInInspector] _Cookie2D_SS("Cookie (Scale and Speed)", Vector) = (1,1,0,0)
		[HideInInspector] _Cookie2D_Offset("Cookie (Offset)", Vector) = (0,0,0,0)
        [HideInInspector] _ShadowIntensity("Shadow Intensity", Vector) = (0,1,0,0)
        [HideInInspector] _ShadowColor("Shadow Color", Color) = (0,0,0,1)
		[HideInInspector] _ShadowCubemap("Shadow Texture (Cubemap)", Any) = "" {}
		[HideInInspector] _RayMarchMaxSteps("Raymarch Max Steps", Int) = 16
		[HideInInspector] _FlipDepthTexture("Flip Depth Texture", Int) = 0
		[HideInInspector] _DirectLightData("Direct Light Data", Vector) = (1, 8, 4)
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "DisableBatching" = "True" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
			BlendOp [_BlendOp]
			Blend [_BlendSrc] [_BlendDest]
			ZTest Always
			Cull Front
			ZWrite Off

			Pass
			{
				Name "Volumetric Light Forward Pass"
				Tags { "LightMode" = "UniversalForward" }
				HLSLPROGRAM
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile _ VF2_DEPTH_PREPASS  // reuses same keyword than in Volumetric Fog & Mist 2 so only one render feature is needed
                #pragma multi_compile_local_fragment _ VL_NOISE
				#pragma multi_compile_local_fragment _ VL_BLUENOISE
                #pragma multi_compile_local_fragment VL_SPOT VL_SPOT_COOKIE VL_POINT VL_AREA_RECT VL_AREA_DISC
                #pragma multi_compile_local_fragment _ VL_SHADOWS VL_SHADOWS_TRANSLUCENCY VL_SHADOWS_CUBEMAP
                #pragma multi_compile_local_fragment _ VL_CAST_DIRECT_LIGHT_ADDITIVE VL_CAST_DIRECT_LIGHT_BLEND
				#pragma multi_compile_local_fragment _ VL_PHYSICAL_ATTEN
				#pragma multi_compile _ _GBUFFER_NORMALS_OCT
                #pragma shader_feature_local_fragment VL_CUSTOM_BOUNDS

				#include "CommonsURP.hlsl"
				#include "Primitives.hlsl"
                #include "ShadowOcclusion.hlsl"
				#include "Raymarch.hlsl"
				#include "Lighting.hlsl"

				struct appdata
				{
					float4 vertex : POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 pos     : SV_POSITION;
					float4 scrPos  : TEXCOORD0;
			        float3 wpos    : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				int _ForcedLightInvisible;

				v2f vert(appdata v)
				{
					v2f o;

					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.pos = TransformObjectToHClip(v.vertex.xyz);
					o.wpos = TransformObjectToWorld(v.vertex.xyz);
					o.scrPos = ComputeScreenPos(o.pos);
					#if defined(UNITY_REVERSED_Z)
						o.pos.z = o.pos.w * UNITY_NEAR_CLIP_VALUE * 0.99999; //  0.99999 avoids precision issues on some Android devices causing unexpected clipping of light mesh
					#else
						o.pos.z = o.pos.w - 1.0e-6f;
					#endif

					if (_ForcedLightInvisible == 1) {
						o.pos.xy = -10000;
                    }

					return o;
				}


				half4 frag(v2f i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

					float3 rayStart = GetRayStart(i.wpos);
					float3 ray = i.wpos - rayStart;
                    float  t1 = length(ray);
					float3 rayDir = ray / t1;
					float  t0 = ComputeIntersection(rayStart, rayDir);

                    #if VL_CUSTOM_BOUNDS
                        float b0, b1;
                        BoundsIntersection(rayStart, rayDir, b0, b1);
                        t0 = max(t0, b0);
                        t1 = min(t1, b1);
                    #endif

					float2 uv = i.scrPos.xy / i.scrPos.w;

					float tfar = t1;
					CLAMP_RAY_DEPTH(rayStart, uv, t1);
                    if (t0>=t1) return 0;

					SetJitter(uv);
                    //t0 += jitter * JITTERING;
					t0 = clamp(t0 + jitter * JITTERING, t0,  t1 * 0.99);

                    #if VL_SHADOWS || VL_SPOT_COOKIE || VL_SHADOWS_TRANSLUCENCY
                        ComputeShadowTextureCoords(rayStart, rayDir, t0, t1);
                    #endif

					half4 color = Raymarch(rayStart, rayDir, t0, t1);

					#if VL_CAST_DIRECT_LIGHT
						if (t1 < tfar) {
							AddDirectLighting(rayStart + t1 * rayDir, uv, color);
						}
					#endif

				    // Apply dither
					color = max(0, color - (half)(jitter * DITHERING));

					return color;
				}
				ENDHLSL
			}

		}
}
