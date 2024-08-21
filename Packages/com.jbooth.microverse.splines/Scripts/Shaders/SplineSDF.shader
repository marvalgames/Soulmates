// Convert a spline into an SDF image for optimized stampin

Shader "Hidden/MicroVerse/SplineSDF"
{
    Properties
    {
        _MainTex("Main", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _FINE

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/com.unity.splines/Shader/Spline.cginc"

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

            float3 _RealSize;
            float4x4 _Transform;
            sampler2D _Prev;
            sampler2D _MainTex;

            SplineInfo _Info = float4(0, 0, 0, 0);
            StructuredBuffer<BezierCurve> _Curves;
            StructuredBuffer<float> _CurveLengths;

            float3 GetClosestPointFine(float2 position, out float resultDistance)
            {
                float closestDistance = 99999;
                float3 closestPosition;

                for (float ct = 0; ct <= 1; ct += 0.00025)
                {
                    float curve = SplineToCurveT(_Info, _CurveLengths, ct);
                    float3 curvePosition = EvaluatePosition(_Curves[floor(curve) % GetKnotCount(_Info)], frac(curve));
                    float d = distance(curvePosition.xz, position);
                    if (d < closestDistance)
                    {
                        closestDistance = d;
                        closestPosition = curvePosition;
                    }
                }

                resultDistance = closestDistance;
                return (closestPosition);
            }

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 position = i.uv * _RealSize.xz;
                position = mul(_Transform, float4(position.x, 0, position.y, 1)).xz;
                float d = 0;

                float3 pos = GetClosestPointFine(position, d);
                float4 last = tex2D(_MainTex, i.uv);
                if (last.y < d)
                {
                    return last;
                }
                return float4(pos.y / _RealSize.y, d, 0, 0);


                
            }
            ENDCG
        }
    }
}