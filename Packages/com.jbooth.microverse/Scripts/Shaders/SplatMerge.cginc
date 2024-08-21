
            struct FragmentOutput
            {
                half4 indexMap : SV_Target0;
                half4 weightMap : SV_Target1;
            };

           
            FragmentOutput FilterSplatWeights(float result, half4 weightMap, half4 indexMap, float channel)
            {
                float totalWeight = weightMap.x + weightMap.y + weightMap.z + weightMap.w;
                result = min(result, 1.0 - saturate(totalWeight));
                if (result > weightMap.x)
                {
                    weightMap.w = weightMap.z;
                    weightMap.z = weightMap.y;
                    weightMap.y = weightMap.x;
                    weightMap.x = result;
                    indexMap.w = indexMap.z;
                    indexMap.z = indexMap.y;
                    indexMap.y = indexMap.x;
                    indexMap.x = channel;
                }
                else if (result > weightMap.y)
                {
                    weightMap.w = weightMap.z;
                    weightMap.z = weightMap.y;
                    weightMap.y = result;
                    indexMap.w = indexMap.z;
                    indexMap.z = indexMap.y;
                    indexMap.y = channel;
                }
                else if (result > weightMap.z)
                {
                    weightMap.w = weightMap.z;
                    weightMap.z = result;
                    indexMap.w = indexMap.z;
                    indexMap.z = channel;
                }
                else if (result > weightMap.w)
                {
                    weightMap.w = result;
                    indexMap.w = channel;
                }

                FragmentOutput o;
                o.indexMap = indexMap;
                o.weightMap = weightMap;
                return o;
            }