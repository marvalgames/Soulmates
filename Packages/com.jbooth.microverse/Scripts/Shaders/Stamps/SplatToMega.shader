Shader "Hidden/MicroVerse/SplatToMega"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _MAX4TEXTURES _MAX8TEXTURES _MAX12TEXTURES _MAX16TEXTURES _MAX20TEXTURES _MAX24TEXTURES _MAX28TEXTURES _MAX32TEXTURES

            #include_with_pragmas "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _Control0;
            sampler2D _Control1;
            sampler2D _Control2;
            sampler2D _Control3;
            sampler2D _Control4;
            sampler2D _Control5;
            sampler2D _Control6;
            sampler2D _Control7;

            struct FragmentOutput
            {
                half4 indexMap : SV_Target0;
                half4 weightMap : SV_Target1;
            };


            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            #if _MAX4TEXTURES
                #define TEXCOUNT 4
            #elif _MAX8TEXTURES
                #define TEXCOUNT 8
            #elif _MAX12TEXTURES
                #define TEXCOUNT 12
            #elif _MAX16TEXTURES
                #define TEXCOUNT 16
            #elif _MAX20TEXTURES
                #define TEXCOUNT 20
            #elif _MAX24TEXTURES
                #define TEXCOUNT 24
            #elif _MAX28TEXTURES
                #define TEXCOUNT 28
            #elif _MAX32TEXTURES
                #define TEXCOUNT 32
            #else
                #define TEXCOUNT 4
            #endif

            FragmentOutput frag (v2f i)
            {
                half4 w0 = tex2D(_Control0, i.uv);
                #if _MAX8TEXTURES || _MAX12TEXTURES || _MAX16TEXTURES || _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                    half4 w1 = tex2D(_Control1, i.uv);
                #endif
                #if _MAX12TEXTURES || _MAX16TEXTURES || _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                    half4 w2 = tex2D(_Control2, i.uv);
                #endif
                #if _MAX16TEXTURES || _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                    half4 w3 = tex2D(_Control3, i.uv);
                #endif
                #if  _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                    half4 w4 = tex2D(_Control4, i.uv);
                #endif
                #if _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                    half4 w5 = tex2D(_Control5, i.uv);
                #endif
                #if  _MAX28TEXTURES || _MAX32TEXTURES
                    half4 w6 = tex2D(_Control6, i.uv);
                #endif
                #if  _MAX32TEXTURES
                    half4 w7 = tex2D(_Control7, i.uv);
                #endif
            

                fixed splats[TEXCOUNT];

                splats[0] = w0.x;
                splats[1] = w0.y;
                splats[2] = w0.z;
                splats[3] = w0.w;
                #if _MAX8TEXTURES || _MAX12TEXTURES || _MAX16TEXTURES || _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                   splats[4] = w1.x;
                   splats[5] = w1.y;
                   splats[6] = w1.z;
                   splats[7] = w1.w;
                #endif
                #if _MAX12TEXTURES || _MAX16TEXTURES || _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                   splats[8] = w2.x;
                   splats[9] = w2.y;
                   splats[10] = w2.z;
                   splats[11] = w2.w;
                #endif
                #if _MAX16TEXTURES || _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                   splats[12] = w3.x;
                   splats[13] = w3.y;
                   splats[14] = w3.z;
                   splats[15] = w3.w;
                #endif
                #if _MAX20TEXTURES || _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                   splats[16] = w4.x;
                   splats[17] = w4.y;
                   splats[18] = w4.z;
                   splats[19] = w4.w;
                #endif
                #if _MAX24TEXTURES || _MAX28TEXTURES || _MAX32TEXTURES
                   splats[20] = w5.x;
                   splats[21] = w5.y;
                   splats[22] = w5.z;
                   splats[23] = w5.w;
                #endif
                #if _MAX28TEXTURES || _MAX32TEXTURES
                   splats[24] = w6.x;
                   splats[25] = w6.y;
                   splats[26] = w6.z;
                   splats[27] = w6.w;
                #endif
                #if _MAX32TEXTURES
                   splats[28] = w7.x;
                   splats[29] = w7.y;
                   splats[30] = w7.z;
                   splats[31] = w7.w;
                #endif

                float4 weights = 0;
                float4 indexes = 0;

                for (int x = 0; x < TEXCOUNT; ++x)
                {
                   fixed w = splats[x];
                   if (w >= weights[0])
                   {
                      weights[3] = weights[2];
                      indexes[3] = indexes[2];
                      weights[2] = weights[1];
                      indexes[2] = indexes[1];
                      weights[1] = weights[0];
                      indexes[1] = indexes[0];
                      weights[0] = w;
                      indexes[0] = x;
                   }
                   else if (w >= weights[1])
                   {
                      weights[3] = weights[2];
                      indexes[3] = indexes[2];
                      weights[2] = weights[1];
                      indexes[2] = indexes[1];
                      weights[1] = w;
                      indexes[1] = x;
                   }
                   else if (w >= weights[2])
                   {
                      weights[3] = weights[2];
                      indexes[3] = indexes[2];
                      weights[2] = w;
                      indexes[2] = x;
                   }
                   else if (w >= weights[3])
                   {
                      weights[3] = w;
                      indexes[3] = x;
                   }
                }

                FragmentOutput o;
                o.indexMap = indexes / 32;
                o.weightMap = weights / (weights.x + weights.y + weights.z + weights.w);
                return o;

            }
            ENDCG
        }
    }
}
