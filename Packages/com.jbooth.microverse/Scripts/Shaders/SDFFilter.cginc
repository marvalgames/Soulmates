
            sampler2D _PlacementSDF;
            sampler2D _PlacementSDF2;
            sampler2D _PlacementSDF3;
            float2 _DistancesFromTrees;
            float2 _DistancesFromObject;
            float2 _DistancesFromParent;
            float _SDFClamp;

            #pragma shader_feature_local_fragment _ _SDFFILTERING
            
            float SDFFilter(float2 uv)
            {
                #if _SDFFILTERING

                    #if _REQUIRELODSAMPLER
                        float sdf = tex2Dlod(_PlacementSDF, float4(uv, 0, 0)).r * 256;
                        float sdf2 = tex2Dlod(_PlacementSDF2, float4(uv, 0, 0)).r * 256;
                        float sdf3 = tex2Dlod(_PlacementSDF3, float4(uv, 0, 0)).r * 256;
                    #else
                        float sdf = tex2D(_PlacementSDF, uv).r * 256;
                        float sdf2 = tex2D(_PlacementSDF2, uv).r * 256;
                        float sdf3 = tex2D(_PlacementSDF3, uv).r * 256;
                    #endif

                    float minsdf = saturate(sdf / _DistancesFromTrees.x);
                    float minsdf2 = saturate(sdf2 / _DistancesFromObject.x);
                    float minsdf3 = saturate(sdf3 / _DistancesFromParent.x);

                    if (_DistancesFromTrees.y > _DistancesFromTrees.x)
                    {
                        float maxsdf = 1.0 - saturate(sdf / _DistancesFromTrees.y);
                        sdf = min(minsdf, maxsdf);
                    }
                    else
                    {
                        sdf = minsdf;
                    }
                    if (_DistancesFromObject.y > _DistancesFromObject.x)
                    {
                        float maxsdf = 1.0 - saturate(sdf2 / _DistancesFromObject.y);
                        sdf2 = min(minsdf2, maxsdf);
                    }
                    else
                    {
                        sdf2 = minsdf2;
                    }

                    if (_DistancesFromParent.y > _DistancesFromParent.x)
                    {
                        float maxsdf = 1.0 - saturate(sdf3 / _DistancesFromParent.y);
                        sdf3 = min(minsdf3, maxsdf);
                    }
                    else
                    {
                        sdf3 = minsdf3;
                    }
                    sdf = smoothstep(0, 1, sdf * sdf2 * sdf3);
                    if (_SDFClamp > 0.5)
                        sdf = sdf > 0.15 ? 1.0 : 0.0;
                    return sdf;
                #endif
                return 1;
            }


