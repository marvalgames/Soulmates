Shader "Skatanic/MasterUnlit"
{
    Properties
    {
    
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1,0)
        _DetailMask("Detail Mask Texture", 2D) = "black" {}
        _ShadowTex("Shadow Texture", 2D) = "white" {}
         _ShadowColor("Shadow Color", Color) = (1, 1, 1, 0)
          _Brightness("Brightness", Range(-1, 1)) = 0
        _OutlineThickness("Outline Thickness", Float) = 1
        _OutlineType("Outline Type", Integer) = 0
        _OutlineOffset("Outline Offset", Vector) = (0, 0, 0,0)
        _Steps("Steps", Integer) = 2
        _Smoothness("Smoothness", Range(0,1))=1
        [HDR]_SpecularColor("Specular Color", Color) = (1,1,1,0)
        [Header(Noise)]
        _NoiseScale("Noise Scale", Float) = 1
        _NoiseBias("Noise Bias", Range(0,1)) = 0.5
        _NoiseColor("Noise Color", Color) = (1,1,1,0)
        
    }

    SubShader
    {
        
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        Pass
        {   Tags { "LightMode" = "UniversalForward" }
            Name "UniversalForward"
            // Render State
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma vertex Vert;
            #pragma fragment Frag;

            #include "Assets/Shaders/HLSL/Unlit.hlsl"
            ENDHLSL
        }

        
        //MY FIRST OUTLINE PASS
        Pass
        {
            Tags { "LightMode" = "Outline" }
            Name "Outline"
            Cull Front
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            
            #pragma vertex InverseVert;
            #pragma fragment Outline;

            #include "Assets/Shaders/HLSL/Unlit.hlsl"
            ENDHLSL
        }
        
        /*
        //MY SECOND OUTLINE PASS
        Pass
        {
           Tags { "LightMode" = "Outline Two" }
            Name "Outline Two"
            Cull Front
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            
            #pragma vertex InverseVertTwo;
            #pragma fragment OutlineTwo;

            #include "Assets/Shaders/HLSL/Unlit.hlsl"
            ENDHLSL
        }
        */

        ///ADDITIONAL PASSES FOR LIGHTING CALCS
        Pass
        {   Tags { "LightMode" = "ShadowCaster" }
            Name "ShadowCaster"
            // Render State
            Cull Back
            Blend One Zero
            ZTest LEqual
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert;
            #pragma fragment Frag;

            #include "Assets/Shaders/HLSL/Unlit.hlsl"
            ENDHLSL
        }
        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
        
    
    }
}
