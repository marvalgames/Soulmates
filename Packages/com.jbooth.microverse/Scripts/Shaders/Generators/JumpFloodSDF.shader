Shader "Hidden/MicroVerse/JumpFloodSDF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Zoom ("Zoom", Float) = 1
    }
    SubShader
    {
        Tags { "PreviewType" = "Plane" }
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE


        #define FLOOD_NULL_POS -1.0
        #define FLOOD_NULL_POS_FLOAT2 float2(FLOOD_NULL_POS, FLOOD_NULL_POS)
        

        ENDCG
        
        Pass // 0
        {
            Name "JUMPFLOODINIT"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "UnityCG.cginc"

            #pragma target 4.5

            int _Channel;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 frag (v2f i) : SV_Target {

                // sample silhouette texture for sobel
                half3x3 values;
                UNITY_UNROLL
                for(int u=0; u<3; u++)
                {
                    UNITY_UNROLL
                    for(int v=0; v<3; v++)
                    {
                        float2 sampleUV = clamp(i.uv + _MainTex_TexelSize.xy * float2(u-1, v-1), float2(0,0), float2(1,1));
                        values[u][v] = tex2D(_MainTex, sampleUV)[_Channel];
                    }
                }

                // calculate output position for this pixel
                float2 outPos = i.uv;

                // interior, return position
                if (values._m11 > 0.99)
                    return outPos;

                // exterior, return no position
                if (values._m11 < 0.01)
                    return FLOOD_NULL_POS_FLOAT2;

                // sobel to estimate edge direction
                float2 dir = -float2(
                    values[0][0] + values[0][1] * 2.0 + values[0][2] - values[2][0] - values[2][1] * 2.0 - values[2][2],
                    values[0][0] + values[1][0] * 2.0 + values[2][0] - values[0][2] - values[1][2] * 2.0 - values[2][2]
                    );

                // if dir length is small, this is either a sub pixel dot or line
                // no way to estimate sub pixel edge, so output position
                if (abs(dir.x) <= 0.005 && abs(dir.y) <= 0.005)
                    return outPos;

                // normalize direction
                dir = normalize(dir);

                // sub pixel offset
                float2 offset = dir * (1.0 - values._m11);

                // output encoded offset position
                return (i.uv.xy + offset * _MainTex_TexelSize.xy);
            }
            ENDCG
        }

        Pass // 1
        {
            Name "JUMPFLOOD"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler _MainTex;
            float4 _MainTex_TexelSize;
            int _StepWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                // initialize best distance at infinity
                float bestDist = 1.#INF;
                float2 bestCoord;

                // jump samples
                UNITY_UNROLL
                for(int u=-1; u<=1; u++)
                {
                    UNITY_UNROLL
                    for(int v=-1; v<=1; v++)
                    {
                        // calculate offset sample position

                        float2 offsetUV = i.uv + int2(u, v) * _MainTex_TexelSize.xy * _StepWidth;

                        // .Load() acts funny when sampling outside of bounds, so don't
                        offsetUV = clamp(offsetUV, 0, 1);

                        // decode position from buffer
                        float2 offsetPos = tex2D(_MainTex, offsetUV).rg * _MainTex_TexelSize.zw; 

                        // the offset from current position
                        float2 disp = i.uv * _MainTex_TexelSize.zw - offsetPos;

                        // square distance
                        float dist = dot(disp, disp);

                        // if offset position isn't a null position or is closer than the best
                        // set as the new best and store the position
                        if (offsetPos.y != FLOOD_NULL_POS && dist < bestDist)
                        {
                            bestDist = dist;
                            bestCoord = offsetPos;
                        }
                    }
                }

                // if not valid best distance output null position, otherwise output encoded position

                return isinf(bestDist) ? FLOOD_NULL_POS_FLOAT2 : bestCoord * _MainTex_TexelSize.xy;
            }
            ENDCG
        }

        // technically optional, as you can just decode distance in the shader and not do this pass.
        Pass // 2
        {
            Name "JUMPFLOODSDF"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Zoom;


            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                float fac = (1 + (_Zoom-1)*2);
                i.uv /= fac;
                i.uv += (1.0 / fac) * (_Zoom-1);

                // integer pixel position
                int2 uvInt = i.uv * _MainTex_TexelSize.zw;

                // load encoded position
                float2 encodedPos = _MainTex.Load(int3(uvInt, 0)).rg;
                
                // early out if null position
                if (encodedPos.y == -1)
                    return half4(99999,99999,99999,99999);

                // decode closest position
                float2 nearestPos = encodedPos * _MainTex_TexelSize.zw;

                // current pixel position
                float2 currentPos = i.uv.xy * _MainTex_TexelSize.zw;

                // distance in pixels to closest position
                half dist = distance(nearestPos, currentPos);
                return dist / 256;
            }
            ENDCG
        }
    }
}