Shader "Hidden/MicroVerse/VegetationFilter"
{
    Properties
    {
        [HideInInspector] _MainTex ("Poisson Disc", 2D) = "black" {}
        [HideInInspector] _Heightmap("Heightmap", 2D) = "black" {}
        [HideInInspector] _Normalmap("Normalmap", 2D) = "black" {}
        [HideInInspector] _PlacementMask("Placement Mask", 2D) = "black" {}
        [HideInInspector] _PlacementSDF("Placement SDF", 2D) = "white" {}
        [HideInInspector] _PlacementSDF2("Placement SDF2", 2D) = "white" {}
        [HideInInspector] _PlacementSDF3("Placement SDF3", 2D) = "white" {}
        [HideInInspector] _Curvemap ("Curvemap", 2D) = "black" {}
        [HideInInspector] _FalloffTexture("Falloff", 2D) = "white" {}
        [HideInInspector] _WeightNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _SlopeNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _AngleNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _CurvatureNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _ClearMask("ClearMask", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _TEXTUREFILTER

            #define _REQUIRELODSAMPLER 1

            #include "UnityCG.cginc"

            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Filtering.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/SDFFilter.cginc"

            sampler2D _MainTex;
            sampler2D _PlacementMask;
            
            float4 _PlacementSDF_TexelSize;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            sampler2D _ClearMask;
            float _ClearLayer;

            float _InstanceCount;
            float4 _MainTex_TexelSize;
            int _Seed;
            float _DiscStrength;
            float _MinHeight;
            float3 _TextureLayerWeights[32];
            int _NumTreeIndexes;
            float _TotalWeights;
            int _YCount;
            float _HeightOffset;
            int _TerrainPixelCount;
            float _ModWidth;


            sampler2D _RandomTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };



            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct FragmentOutput
            {
                half4 posWeight : SV_Target0;
                half4 randoms : SV_Target1;
            };

            struct Randomization
            {
                float weight;
                float2 scaleHeightRange;
                float2 scaleWidthRange;
                float sink;
                float scaleMultiplierAtBoundaries;
                float2 weightRange;
                int flags;
            };

            StructuredBuffer<Randomization> _Randomizations;

            bool GetFlagLockScaleWidthHeight(Randomization r) { return (r.flags & (1 << 1)) != 0; }
            bool GetFlagRandomRotation(Randomization r) { return (r.flags & (1 << 2)) == 0; }
            bool GetFlagDensityByWeight(Randomization r) { return (r.flags & (1 << 3)) == 0; }
            bool GetFlagDisabled(Randomization r) { return (r.flags & (1 << 4)) == 0; }
            bool GetFlagMapHeightToScale(Randomization r) { return (r.flags & (1 << 5)) != 0; }
            bool GetFlagMapWeightToScale(Randomization r) { return (r.flags & (1 << 6)) != 0; }
            bool GetFlagRandomScale(Randomization r) { return (r.flags & (1 << 7)) == 0; }
            float4 NextRandom(float cellIdx)
            {
                float2 uv = (cellIdx + _Seed) * 719.71892;
                uv /= 64;
                uv.y /= 64;
                return tex2Dlod(_RandomTex, float4(uv, 0, 0));
            }


            FragmentOutput frag(v2f i)
            {
                FragmentOutput o = (FragmentOutput)0;
                float cellCount = sqrt(_InstanceCount);
            
                //float cellIdx = floor(i.uv.y * _YCount + i.uv.x * _InstanceCount);
                float cellIdx = floor(floor(i.uv.y * _YCount) * 512 + i.uv.x * 512);
                float x = floor(cellIdx % cellCount);
                float y = floor(cellIdx / cellCount);
                float2 uv = float2(x, y);

                float discU = i.uv.x;
                discU += i.uv.y;
                discU *= _InstanceCount * 3.1927;
                discU += _Seed;
                discU %= _MainTex_TexelSize.z;
                discU *= _MainTex_TexelSize.x;

                float2 disk = tex2D(_MainTex, float2(discU, 0.5)).xy * _DiscStrength;
                uv += disk;
                uv /= floor(cellCount);


                // transform the stamp since our UV is artificial.

                float2 stampUV = mul(_Transform, float4(uv, 0, 1)).xy;

                float2 noiseUV = uv * _NoiseUV.z + _NoiseUV.xy;

                float mask = 1.0 - tex2D(_PlacementMask, uv).b;
                float sdf = SDFFilter(uv);
                
                // do not saturate! will break tree scaling by weight
                float heightWeight = 1;
                float result = (DoFilters(uv, stampUV, noiseUV, heightWeight));

                float height = 0;
                float texMask = 1;
                #if _TEXTUREFILTER
                    half4 indexes = tex2D(_IndexMap, uv) * 32;
                    half4 weights = tex2D(_WeightMap, uv);
                    for (int itr = 0; itr < 4; ++itr)
                    {
                        int index = round(indexes[itr]);
                        float weight = weights[itr];
                        float3 tlw = _TextureLayerWeights[index];
                        texMask -= ((tlw.x * weight) + (tlw.z * weight) * tlw.y); 
                    }
                    texMask = saturate(texMask);
                #endif

                float w = result * sdf * texMask * mask;
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1 || cellIdx > _InstanceCount)
                {
                    o.posWeight = -1;
                    o.randoms = -1;
                    return o;
                }
                float r = NextRandom(cellIdx + 76).x;
                if (w < r)
                {
                    o.posWeight = -1;
                    o.randoms = -1;
                    return o;
                }

                float2 clearMask = tex2D(_ClearMask, uv).xy;
                if (round(clearMask.r * 256) > _ClearLayer+0.5)
                    w *= 1.0 - clearMask.y;
                
                float4 randomRet = float4(0,1,1,0);

                if (w > 0)
                {
                    // fetch random values

                    float4 randomValues = NextRandom(cellIdx + 243);

                    // choose tree index

                    float treeWeight = randomValues.x * _TotalWeights;
                    float curWeight = 0;
                    int treeIdx = 0;
                    for (int i = 0; i < _NumTreeIndexes; ++i)
                    {
                        if (GetFlagDisabled(_Randomizations[i]))
                            continue;
                        
                        curWeight += 1 + _Randomizations[i].weight;
                        treeIdx = i;
                        if (curWeight >= treeWeight)
                        {
                            i = _NumTreeIndexes;
                        }
                    }

                    randomRet.x = treeIdx;

                    // get data for that tree index
                    Randomization random = _Randomizations[treeIdx];
                    // apply sink
                    height = UnpackHeightmap(_Heightmap.Sample(shared_linear_clamp, uv));
                    height = max(_MinHeight / _RealSize.y, height);

                    height -= random.sink / _RealSize.y;

                    if (GetFlagDisabled(random))
                    {
                        w = -1;
                    }
                    else if (GetFlagDensityByWeight(random))
                    {
                        w = w > 0.5 ? (w-0.5) * 2 : -1;
                    }

                    if (random.weightRange.y > 0.001 && (w < random.weightRange.x || w > random.weightRange.y))
                        w = -1;

                    float scaleByWeight = lerp(random.scaleMultiplierAtBoundaries, 1, saturate(w/3));
                    float scaleLerp = 1;

                    if (GetFlagMapWeightToScale(random))
                    {
                        scaleLerp = saturate(w);
                    }
                    if (GetFlagMapHeightToScale(random))
                    {
                        scaleLerp *= saturate(heightWeight);
                    }
                    float2 scale = 1;
                    if (GetFlagRandomScale(random))
                    {
                        scale = randomValues.yz;
                    }
                    if (GetFlagLockScaleWidthHeight(random))
                    {
                        scale.y = scale.x;
                    }
                    scale *= scaleLerp;
                  
                    randomRet.y = lerp(random.scaleHeightRange.x, random.scaleHeightRange.y, scale.x) * scaleByWeight;
                    randomRet.z = lerp(random.scaleWidthRange.x, random.scaleWidthRange.y, scale.y) * scaleByWeight;

                    if (GetFlagRandomRotation(random))
                    {
                        randomRet.w = randomValues.w * 6.28318530718;
                    }
                }

                // pos.xyz, weight
                // index, scale.xy, rot
                o.posWeight = float4(uv.x, height, uv.y, w);
                o.randoms = randomRet;
                return o;
            }
            ENDCG
        }
    }
}
