Shader "Hidden/MicroVerse/ObjectFilter"
{
    Properties
    {
        [HideInInspector] _MainTex ("Poisson Disc", 2D) = "black" {}
        [HideInInspector] _Heightmap("Heightmap", 2D) = "black" {}
        [HideInInspector] _Normalmap("Normalmap", 2D) = "black" {}
        [HideInInspector] _PlacementMask("Placement Mask", 2D) = "black" {}
        [HideInInspector] _ObjectMask("Object Mask", 2D) = "black" {}
        [HideInInspector] _PlacementSDF("Placement SDF", 2D) = "white" {}
        [HideInInspector] _PlacementSDF2("Placement SDF", 2D) = "white" {}
        [HideInInspector] _PlacementSDF3("Placement SDF", 2D) = "white" {}
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
            #pragma target 5.0

            #include_with_pragmas "UnityCG.cginc"

            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Filtering.cginc"

            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/SDFFilter.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Quaternion.cginc"

            sampler2D _MainTex;
            sampler2D _PlacementMask;
            sampler2D _ObjectMask;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            sampler2D _ClearMask;
            float _ClearLayer;

            float _Density;
            float _InstanceCount;
            float4 _MainTex_TexelSize;
            int _Seed;
            float _DiscStrength;
            float _MinHeight;
            float3 _TextureLayerWeights[32];
            int _NumObjectIndexes;
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

            

            
            struct Randomization
            {
                float weight;
                float2 weightRange;
                float2 rotationRangeX;
                float2 rotationRangeY;
                float2 rotationRangeZ;
                float2 scaleRangeX;
                float2 scaleRangeY;
                float2 scaleRangeZ;
                int scaleLock; // none, xy, xz, yz, xyz
                int rotationLock;
                float slopeAlignment;
                float2 sink;
                float scaleMultiplierAtBoundaries;
            
                int flags;
            };


            StructuredBuffer<Randomization> _Randomizations;

            bool GetFlagDensityByWeight(Randomization r) { return (r.flags & (1 << 3)) == 0; }
            bool GetFlagDisabled(Randomization r) { return (!(r.flags & (1 << 4)) == 0); }
            bool GetFlagAlignDownhill(Randomization r) { return (!(r.flags & (1 << 5)) == 0); }

            float4 NextRandom(float cellIdx)
            {
                float2 uv = cellIdx;
                uv /= 64;
                uv.y /= 64;
                return tex2Dlod(_RandomTex, float4(uv, 0, 0));
            }

            struct FragmentOutput
            {
                float4 positionWeight : SV_Target0;
                float4 rotation : SV_Target1;
                float4 scaleIndex : SV_Target2;
            };


            FragmentOutput frag(v2f i) : SV_Target
            {
                float cellCount = sqrt(_InstanceCount);
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

                float2 noiseUV = (uv * _NoiseUV.z) + _NoiseUV.xy;

                float mask = 1.0 - tex2D(_PlacementMask, uv).b;
                float mask2 = 1.0 - tex2D(_ObjectMask, uv).r;

                float sdf = SDFFilter(uv);

                float result = (DoFilters(uv, stampUV, noiseUV));

                float height = UnpackHeightmap(_Heightmap.Sample(shared_linear_clamp, uv));
                height = max(_MinHeight / _RealSize.y, height);
                height += (_HeightOffset / _RealSize.y) * 0.5;

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
                float r = NextRandom(cellIdx * 3).xy;
                float w = result * sdf * texMask * mask * mask2;
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1 || w < r || cellIdx > _InstanceCount)
                    w = -1;

                float2 clearMask = tex2D(_ClearMask, uv).xy;
                if (round(clearMask.r * 256) > _ClearLayer+0.5)
                    w *= 1.0 - clearMask.y;


                //float3 normal = _Normalmap.Sample(shared_linear_clamp, uv) * 2 - 1;
                float height0 = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv));
                float height1 = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv + float2(_Heightmap_TexelSize.x, 0)));
                float height2 = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv + float2(0, _Heightmap_TexelSize.y)));
                float2 dxy = height0 - float2(height1, height2);

                dxy = dxy * _Heightmap_TexelSize.zw;
                float3 normal = normalize(float4( dxy.x, 1.0, dxy.y, height0));

                FragmentOutput o = (FragmentOutput)0;   

                if (w > 0)
                {
                    // fetch random values

                    float4 randomValues = NextRandom(cellIdx * 5);
                    
                    // choose tree index

                    float objectWeight = randomValues.x * _TotalWeights;
                    float curWeight = 0;
                    int objectIndex = 0;
                    for (int i = 0; i < _NumObjectIndexes; ++i)
                    {
                        if (GetFlagDisabled(_Randomizations[i]))
                            continue;

                        curWeight += 1 + _Randomizations[i].weight;
                        objectIndex = i;
                        if (curWeight >= objectWeight)
                        {
                            i = _NumObjectIndexes;
                        }
                    }

                    // get data for that tree index
                    Randomization random = _Randomizations[objectIndex];
                    
                    if (GetFlagDisabled(random))
                    {
                        w = -1;
                    }
                    else if (GetFlagDensityByWeight(random))
                    {
                        w = w > 0.5 ? (w-0.5) * 2 : -1;
                    }

                    if (random.weightRange.y > 0 && (w < random.weightRange.x || w > random.weightRange.y))
                        w = -1;

                    if (w > 0)
                    {
                        float scaleByWeight = lerp(random.scaleMultiplierAtBoundaries, 1, saturate(w/3));
                        float rotX = lerp(random.rotationRangeX.x, random.rotationRangeX.y, randomValues.y);
                        float rotY = lerp(random.rotationRangeY.x, random.rotationRangeY.y, randomValues.z);
                        float rotZ = lerp(random.rotationRangeZ.x, random.rotationRangeZ.y, randomValues.w);


                        randomValues = NextRandom(cellIdx * 3 + 1927);
                        float scaleX = lerp(random.scaleRangeX.x, random.scaleRangeX.y, randomValues.y);
                        float scaleY = lerp(random.scaleRangeY.x, random.scaleRangeY.y, randomValues.z);
                        float scaleZ = lerp(random.scaleRangeZ.x, random.scaleRangeZ.y, randomValues.w);

                        // apply sink
                        height -= lerp(random.sink.x, random.sink.y, randomValues.x) /_RealSize.y;


                        // none, xy, xz, yz, xyz
                        if (random.rotationLock == 1)
                        {
                            rotY = rotX;
                        }
                        else if (random.rotationLock == 2)
                        {
                            rotZ = rotX;
                        }
                        else if (random.rotationLock == 3)
                        {
                            rotZ = rotY;
                        }
                        else if (random.rotationLock == 4)
                        {
                            rotY = rotX;
                            rotZ = rotX;
                        }

                        if (random.scaleLock == 1)
                        {
                            scaleY = scaleX;
                        }
                        else if (random.scaleLock == 2)
                        {
                            scaleZ = scaleX;
                        }
                        else if (random.scaleLock == 3)
                        {
                            scaleZ = scaleY;
                        }
                        else if (random.scaleLock == 4)
                        {
                            scaleY = scaleX;
                            scaleZ = scaleX;
                        }
                       

                        float4 qslopeAlign = 0;
                        
                        if (GetFlagAlignDownhill(random))
                        {
                            float3 right = float3(1,0,0);
                            if (normal.y < 1)
                            {
                                right = cross(normal, float3(0,1,0));
                            }
                            float4 qAlign = q_look_at(right, -normal);
                            float4 qPlane = q_look_at(right, float3(0, -1, 0)); 
                            qslopeAlign = q_slerp(qPlane, qAlign, random.slopeAlignment);
                        }
                        else
                        {
                            float3 slopeAlign = lerp(float3(0,0,0), float3(normal.z * 90, 0, normal.x * -90), random.slopeAlignment);
                            slopeAlign = radians(slopeAlign);
                            qslopeAlign = euler_to_quaternion(slopeAlign);
                        }
                        float3 rot = float3(rotX, rotY, rotZ);
                        rot = radians(rot);
                        float4 qrot = euler_to_quaternion(rot);
                        float4 fq = qmul(qslopeAlign, qrot);
                        o.rotation = fq;

                        o.positionWeight.xyz = float3(uv.x, height * 2, uv.y);
                        o.positionWeight.w = w;
                        o.scaleIndex.xyz = float3(scaleX, scaleY, scaleZ) * scaleByWeight;
                        o.scaleIndex.w = objectIndex;

                    }
                }

                return o;
            }
            ENDCG
        }
    }
}
