Shader "Hidden/MicroVerse/RasterToTerrain"
{
    Properties
    {
        [HideInInspector]
        _Weights ("Weights", 2D) = "black" {}
        _Indexes ("indexes", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "UnityCG.cginc"

            struct vertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _Weights;
            sampler2D _Indexes;
            float _Target;

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                int4 indexes = round(tex2D(_Indexes, i.uv) * 32);
                half4 weights = tex2D(_Weights, i.uv);
                float total = weights.x + weights.y + weights.z + weights.w;
                if (total <= 0)
                {
                    if (_Target == 0)
                        weights = float4(0.25,0,0,0);
                }
                else
                {
                    weights /= total;
                }
                half o[4];
                o[0] = 0; o[1] = 0; o[2] = 0; o[3] = 0;

                indexes -= _Target * 4;

                UNITY_UNROLL
                for (int i = 3; i >= 0; --i)
                {
                    if (weights[i] > 0 && indexes[i] >= 0 && indexes[i] <= 4)
                        o[indexes[i]] += weights[i];
                }

                return float4(o[0], o[1], o[2], o[3]);

            }
            ENDCG
        }
    }
}