
Shader "Hidden/MicroVerse/SplineSDFFill"
{
    Properties
    {
        _NumSegments("Number of Segments", Int) = 120
        _MainTex("Main", 2D) = "white" {}
        _Prev("Prev", 2D) = "white" {}
        _MaxSDF("Max SDF Field Distance", Float) = 256
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _EDGES
            #pragma shader_feature_local_fragment _ _WIDTHSMOOTHSTEP _WIDTHEASEIN _WIDTHEASEOUT _WIDTHEASEINOUT

            #pragma shader_feature_local_fragment _ _POSITIONNOISE _POSITIONFBM _POSITIONWORLEY _POSITIONNOISETEXTURE _POSITIONWORM _POSITIONWORMFBM
            #pragma shader_feature_local_fragment _ _WIDTHNOISE _WIDTHFBM _WIDTHWORLEY _WIDTHNOISETEXTURE _WIDTHWORM _WIDTHWORMFBM
            

            #pragma shader_feature_local_fragment _ _INTERSECTION _AREA _ROAD

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/com.unity.splines/Shader/Spline.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"

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

            SamplerState shared_linear_repeat;

            Texture2D _PositionNoiseTexture;
            float4 _PositionNoiseTexture_ST;
            float4 _PositionNoise;
            float4 _PositionNoise2;
            int _PositionNoiseChannel;
            Texture2D _WidthNoiseTexture;
            float4 _WidthNoiseTexture_ST;
            float4 _WidthNoise;
            float4 _WidthNoise2;
            int _WidthNoiseChannel;
            float _SDFMult;
            float3 _RealSize;
            float4x4 _Transform;
            sampler2D _Prev;
            sampler2D _MainTex;
            float _MaxSDF;
            float4 _SplineBounds;

            SplineInfo _Info = float4(0, 0, 0, 0);
            float4 _WidthInfo;
            float _WidthBoost;
            StructuredBuffer<BezierCurve> _Curves;
            StructuredBuffer<float> _CurveLengths;
            StructuredBuffer<float2> _Widths;
            uint _NumSegments;

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            int solve_cubic(float3 coeffs, inout float3 r)
            {

	            float a = coeffs.z;
	            float b = coeffs.y;
	            float c = coeffs.x;

	            float p = b - a*a / 3.0;
	            float q = a * (2.0*a*a - 9.0*b) / 27.0 + c;
	            float p3 = p*p*p;
	            float d = q*q + 4.0*p3 / 27.0;
	            float offset = -a / 3.0;
	            if(d >= 0.0) { // Single solution
		            float z = sqrt(d);
		            float u = (-q + z) / 2.0;
		            float v = (-q - z) / 2.0;
		            u = sign(u)*pow(abs(u),1.0/3.0);
		            v = sign(v)*pow(abs(v),1.0/3.0);
		            r.x = offset + u + v;	

		            //Single newton iteration to account for cancellation
		            float f = ((r.x + a) * r.x + b) * r.x + c;
		            float f1 = (3. * r.x + 2. * a) * r.x + b;

		            r.x -= f / f1;

		            return 1;
	            }
	            float u = sqrt(-p / 3.0);
	            float v = acos(-sqrt( -27.0 / p3) * q / 2.0) / 3.0;
	            float m = cos(v), n = sin(v)*1.732050808;

	            //Single newton iteration to account for cancellation
	            //(once for every root)
	            r.x = offset + u * (m + m);
                r.y = offset - u * (n + m);
                r.z = offset + u * (n - m);

	            float3 f = ((r + a) * r + b) * r + c;
	            float3 f1 = (3. * r + 2. * a) * r + b;

	            r -= f / f1;

	            return 3;
            }

            int cubic_bezier_sign(float2 uv, float2 p0, float2 p1, float2 p2, float2 p3){

	            float cu = (-p0.y + 3. * p1.y - 3. * p2.y + p3.y);
	            float qu = (3. * p0.y - 6. * p1.y + 3. * p2.y);
	            float li = (-3. * p0.y + 3. * p1.y);
	            float co = p0.y - uv.y;

	            float3 roots = 1e38;
	            int n_roots = solve_cubic(float3(co/cu,li/cu,qu/cu),roots);

	            int n_ints = 0;

	            for(int i=0;i<3;i++){
		            if(i < n_roots){
			            if(roots[i] >= 0. && roots[i] <= 1.){
				            float x_pos = -p0.x + 3. * p1.x - 3. * p2.x + p3.x;
				            x_pos = x_pos * roots[i] + 3. * p0.x - 6. * p1.x + 3. * p2.x;
				            x_pos = x_pos * roots[i] + -3. * p0.x + 3. * p1.x;
				            x_pos = x_pos * roots[i] + p0.x;

				            if(x_pos < uv.x){
					            n_ints++;
				            }
			            }
		            }
	            }
                return n_ints;
            }

            float length2( float2 v ) { return dot(v,v); }

            float segment_dis_sq( float2 p, float2 a, float2 b ){
	            float2 pa = p-a, ba = b-a;
	            float h = saturate( dot(pa,ba)/dot(ba,ba));
	            return length2( pa - ba*h );
            }

            float4 cubic_bezier_segments_dis_sq(float2 uv, float3 p0, float3 p1, float3 p2, float3 p3, out float _t)
            {
                int numSeg = max(_NumSegments, 2);
                float d0 = 1e38;
                float3 a = p0;
                float3 minPos = 99999;
                _t = 0;
                for( int i=1; i<numSeg; i++ )
                {
                    float t = float(i)/float(numSeg-1);
                    float s = 1.0-t;
                    float3 b = p0*s*s*s + p1*3.0*s*s*t + p2*3.0*s*t*t + p3*t*t*t;
                    float nd = segment_dis_sq(uv, a.xz, b.xz);
                    if (nd < d0)
                    {
                        d0 = nd;
                        minPos = b;
                        _t = t;
                    }
                    a = b;
                }
    
                return float4(d0, minPos);
            }


            

            bool InBounds(float4 bounds, float2 p, float e)
            {
                bounds += float4(-e, e, -e, e);
                return (p.x >= bounds.x &&
                        p.x <= bounds.y &&
                        p.y >= bounds.z &&
                        p.y <= bounds.w);
            }

            bool InBounds(float2 p0, float2 p1, float2 p2, float2 p3, float2 p, float e)
            {
                float xmin = min(min(min(p0.x, p1.x), p2.x), p3.x);
                float xmax = max(max(max(p0.x, p1.x), p2.x), p3.x);
                float ymin = min(min(min(p0.y, p1.y), p2.y), p3.y);
                float ymax = max(max(max(p0.y, p1.y), p2.y), p3.y);

                return InBounds(float4(xmin, xmax, ymin, ymax), p, e);
            }


            float4 frag(v2f i) : SV_Target
            {
                float4 last = tex2D(_MainTex, i.uv);
                #if _EDGES
                    UNITY_BRANCH
                    if (i.uv.x > 0.01 && i.uv.y > 0.01 && i.uv.y < 0.99 && i.uv.y < 0.99)
                    {
                        return tex2D(_Prev, i.uv);
                    }
                #endif

                float2 position = i.uv * _RealSize.xz;
                position = mul(_Transform, float4(position.x, 0, position.y, 1)).xz;

                float2 origPositon = position;

                #if _POSITIONNOISE
                    position.x += Noise(position.xy * 0.01, _PositionNoise);
                    position.y += Noise(position.xy * 0.01 + 0.371, _PositionNoise);
                #elif _POSITIONFBM
                    position.x += NoiseFBM(position.xy * 0.01, _PositionNoise);
                    position.y += NoiseFBM(position.xy * 0.01 + 0.371, _PositionNoise);
                #elif _POSITIONWORLEY
                    position.x += NoiseWorley(position.xy * 0.01, _PositionNoise);
                    position.y += NoiseWorley(position.xy * 0.01 + 0.371, _PositionNoise);
                #elif _POSITIONWORM
                    position.x += NoiseWorm(position.xy * 0.01, _PositionNoise);
                    position.y += NoiseWorm(position.xy * 0.01 + 0.371, _PositionNoise);
                #elif _POSITIONWORMFBM
                    position.x += NoiseWormFBM(position.xy * 0.01, _PositionNoise);
                    position.y += NoiseWormFBM(position.xy * 0.01 + 0.371, _PositionNoise);
                #elif _POSITIONNOISETEXTURE
                    position.x += ((_PositionNoiseTexture.Sample(shared_linear_repeat, position.xy * _PositionNoiseTexture_ST.xy + _PositionNoiseTexture_ST.zw)[_PositionNoiseChannel]) * _PositionNoise.y + _PositionNoise.w);
                    position.y += ((_PositionNoiseTexture.Sample(shared_linear_repeat, (position.xy + 0.371) * _PositionNoiseTexture_ST.xy + _PositionNoiseTexture_ST.zw)[_PositionNoiseChannel]) * _PositionNoise.y + _PositionNoise.w);
                #endif


                float4 d = 99999;

                float maxSDF = _MaxSDF * 2.0;

                if (!InBounds(_SplineBounds, position, maxSDF))
                {
                    return last;
                }

                #if _INTERSECTION
                    maxSDF *= _SDFMult;
                #endif

                uint numIntersections = 0;
                float finalT = 0;
                int xcount = _Info.x;

                // if we're not an area, we lop the last point off so it doesn't draw the tangent handle
                #if _ROAD
                    if (_Info.y < 0.5)
                        xcount -= 1;
                #endif
                
                for (int x = 0; x < xcount; ++x)
                {
                    BezierCurve bc = _Curves[x];
                    #if !_INTERSECTION
                    if (InBounds(bc.P0.xz, bc.P1.xz, bc.P2.xz, bc.P3.xz, position, maxSDF))
                    #endif
                    {
                        float t = 0;
                        float4 nd = cubic_bezier_segments_dis_sq(position, bc.P0, bc.P1, bc.P2, bc.P3, t);
                        if (nd.x < d.x)
                        {
                            d = nd;
                            finalT = x + t;
                            maxSDF = min(maxSDF, nd.x);
                        }
                    }
                    numIntersections += cubic_bezier_sign(position, bc.P0.xz, bc.P1.xz, bc.P2.xz, bc.P3.xz);
                }

                
                float width = 0;
                if (_WidthInfo.x == 1)
                {
                    width = _Widths[0].y;
                }
                else if (_WidthInfo.x >= 1)
                {
                    width = _Widths[0].y;
                    if (finalT >= _Widths[_WidthInfo.x-1].x)
                    {
                        width = _Widths[_WidthInfo.x-1].y;
                    }
                    else
                    {
                        for (int x = 1; x < _WidthInfo.x; ++x)
                        {
                            float2 pw = _Widths[x-1];
                            float2 cw = _Widths[x];
                            if (finalT >= pw.x && finalT < cw.x)
                            {
                                float fr = max(0.001,(cw.x - pw.x));
                                float r = frac((finalT-cw.x)/fr);
                            
                                width = lerp(pw.y, cw.y, r);
                                float maxW = max(0.001, max(pw.y, cw.y));
                                width /= maxW;
                                #if _WIDTHSMOOTHSTEP
                                    width = smoothstep(0,1,width);
                                #elif _WIDTHEASEIN
                                    width *= width;
                                #elif _WIDTHEASEOUT
                                    width = 1 - (1 - width) * (1 - width);
                                #elif _WIDTHEASEINOUT
                                    width = width < 0.5 ? 2 * width * width : 1 - pow(-2 * width + 2, 2) / 2;
                                #endif

                                width *= maxW;
                            }
                        }
                    }
                }

                #if _WIDTHNOISE
                    width += abs(Noise(origPositon.xy * 0.01, _WidthNoise));
                #elif _WIDTHFBM
                    width += abs(NoiseFBM(origPositon.xy * 0.01, _WidthNoise));
                #elif _WIDTHWORLEY
                    width += abs(NoiseWorley(origPositon.xy * 0.01, _WidthNoise));
                #elif _WIDTHWORM
                    width += abs(NoiseWorm(origPositon.xy * 0.01, _WidthNoise));
                #elif _WIDTHWORMFBM
                    width += abs(NoiseWormFBM(origPositon.xy * 0.01, _WidthNoise));
                #elif _WIDTHNOISETEXTURE
                    width += ((_WidthNoiseTexture.Sample(shared_linear_repeat, origPositon.xy * _WidthNoiseTexture_ST.xy + _WidthNoiseTexture_ST.zw)[_WidthNoiseChannel]) * _WidthNoise.y + _WidthNoise.w);
                #endif
   


                d.x = sqrt(d.x);
                d.x -= _WidthBoost;
                
                

                float sn =  (frac(numIntersections/2.0) > 0 ) ? -1 : 1;
                float dw = max(0, d.x - width);
                float sdf = sn * dw;


                // well, ain't this getting complex
                // We're combining sdf's as we're computing them. When the SDF is an area
                // we want to return the minimum if we're in the negative space of the SDF
                // but if not, we return the regular sdf
                // return the sdf as negative only in area cases

                #if _AREA
                    if (last.x < dw)
                    {
                        last.xy = min(last.xy, float2(sdf, dw));
                        return last;
                    }
                    return float4(sdf, dw, d.z, width);
                #elif _INTERSECTION
                    if (last.x < 0)
                    {
                        return last;
                    }
                    return float4(sdf, dw, d.z, width);
                #else
                   if (last.x < dw)
                   {
                      return last;
                   }
                   return float4(dw, dw, d.z, width);
                #endif
                
            }
            ENDCG
        }
    }
}