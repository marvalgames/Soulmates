using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [System.Serializable]
    public class FilterSet : ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize(){}

        public void OnAfterDeserialize()
        {
            if (version == 0)
            {
                slopeFilter.range *= 1.57894736842f;
                slopeFilter.smoothness *= 1.57894736842f;
            }
            if (version == 1)
            {
                slopeFilter.range *= 1.57894736842f;
                slopeFilter.smoothness *= 1.57894736842f;
                slopeFilter.range.x = Mathf.Clamp(slopeFilter.range.x, 0, 90);
                slopeFilter.range.y = Mathf.Clamp(slopeFilter.range.y, 0, 90);
                slopeFilter.smoothness.x = Mathf.Clamp(slopeFilter.smoothness.x, 0, 90);
                slopeFilter.smoothness.y = Mathf.Clamp(slopeFilter.smoothness.y, 0, 90);
            }
            version = 2;
        }

        public void ScaleAllNoises(float factor)
        {
            weightNoise.frequency *= factor;
            weight2Noise.frequency *= factor;
            weight3Noise.frequency *= factor;
            angleFilter.noise.frequency *= factor;
            slopeFilter.noise.frequency *= factor;
            curvatureFilter.noise.frequency *= factor;
            flowFilter.noise.frequency *= factor;
            heightFilter.noise.frequency *= factor;
            falloffFilter.noise.frequency *= factor;
        }

        public FalloffFilter falloffFilter = new FalloffFilter();

        [Range(0, 1)] public float weight = 1;
        public Noise weightNoise = new Noise();
        public Noise weight2Noise = new Noise();
        public Noise weight3Noise = new Noise();

        public enum NoiseOp
        {
            Add,
            Subtract,
            Multiply,
            Overlay,
            Min,
            Max,
        }

        public NoiseOp weight2NoiseOp = NoiseOp.Add;
        public NoiseOp weight3NoiseOp = NoiseOp.Add;


        [System.Serializable]
        public class Filter
        {
            public enum FilterType
            {
                Simple,
                Curve
            }

            public Filter(Vector2 range, Vector2 smoothness)
            {
                this.range = range;
                this.smoothness = smoothness;
            }
            public bool enabled;
            public FilterType filterType = FilterType.Simple;
            [Range(0, 1)]
            public float weight = 1;
            public Vector2 range = new Vector2(0, 1);
            public Vector2 smoothness = new Vector2(1, 1);
            public Noise noise = new Noise();
            public float mipBias = 0;
            public AnimationCurve curve = new AnimationCurve(new Keyframe[4]
                {
                    new Keyframe(0, 0), new Keyframe(0.25f, 1),
                    new Keyframe(0.75f, 1), new Keyframe(1, 0)
                });
            public Texture2D _curveTexture = null;
            public Texture2D curveTexture
            {
                get
                {
                    if (_curveTexture == null)
                    {
                        _curveTexture = new Texture2D(128, 1, TextureFormat.R8, false, true);
                        _curveTexture.hideFlags = HideFlags.HideAndDontSave;
                        for (int i = 0; i < 128; ++i)
                        {
                            float r = curve.Evaluate((float)i / 128.0f);
                            _curveTexture.SetPixel(i, 0, new Color(r, r, r, r));
                        }
                        _curveTexture.Apply();
                    }
                    
                    return _curveTexture;
                }
            }
        }
        public int version = 0;

        public Filter heightFilter = new Filter(new Vector2(0, 500), new Vector2(20, 20));
        public Filter slopeFilter = new Filter(new Vector2(0, 18), new Vector2(4, 4));
        public Filter angleFilter = new Filter(new Vector2(0, 90), new Vector2(12, 12));
        public Filter curvatureFilter = new Filter(new Vector2(0.6f, 1), new Vector2(0.1f, 0.1f));
        public Filter flowFilter = new Filter(new Vector2(0.6f, 1), new Vector2(0.1f, 0.1f));

        public bool NeedCurvatureMap() { return (curvatureFilter.enabled); }
        public bool NeedFlowMap() { return (flowFilter.enabled); }

        /// <summary>
        /// Texture filter.
        /// Uses a custom property drawer for property selection.
        /// If you add a property here, you need to adjust the drawer as well.
        /// </summary>
        [System.Serializable]
        public class TextureFilter
        {
            public TerrainLayer layer;
            [Range(-1, 1)] public float weight;
            [Range(0, 10)] public float amplitude = 1;
            [Range(-1, 1)] public float balance;
        }
        public bool textureFilterEnabled;
        [Range(0,1)] public float otherTextureWeight = 1;
        public List<TextureFilter> textureFilters = new List<TextureFilter>();
        static Vector4[] terrainLayerWeights = new Vector4[32];
        public Vector4[] GetTextureWeights(TerrainLayer[] layers)
        {
            terrainLayerWeights = new Vector4[32];
            for (int i = 0; i < 32; ++i)
            {
                terrainLayerWeights[i] = new Vector4(1.0f - otherTextureWeight, 1, 0, 0);
            }

            for (int i = 0; i < layers.Length; ++i)
            {
                foreach (var tf in textureFilters)
                {
                    if (ReferenceEquals(tf.layer, layers[i]))
                    {
                        terrainLayerWeights[i] = new Vector4(1.0f - tf.weight, tf.amplitude, tf.balance, 0);
                    }
                }
            }
            return terrainLayerWeights;
        }

       

        static int _Transform = Shader.PropertyToID("_Transform");
        static int _RealSize = Shader.PropertyToID("_RealSize");
        static int _Weight = Shader.PropertyToID("_Weight");
        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");
        static int _WeightNoise = Shader.PropertyToID("_WeightNoise");
        static int _WeightNoise2 = Shader.PropertyToID("_WeightNoise2");

        static int _WeightNoiseChannel = Shader.PropertyToID("_WeightNoiseChannel");
        static int _WeightNoiseTexture = Shader.PropertyToID("_WeightNoiseTexture");
        static int _Weight2Noise = Shader.PropertyToID("_Weight2Noise");
        static int _Weight2Noise2 = Shader.PropertyToID("_Weight2Noise2");
        static int _Weight2NoiseTexture = Shader.PropertyToID("_Weight2NoiseTexture");
        static int _Weight2NoiseChannel = Shader.PropertyToID("_Weight2NoiseChannel");
        static int _Weight3Noise = Shader.PropertyToID("_Weight3Noise");
        static int _Weight3Noise2 = Shader.PropertyToID("_Weight3Noise2");
        static int _Weight3NoiseTexture = Shader.PropertyToID("_Weight3NoiseTexture");
        static int _Weight3NoiseChannel = Shader.PropertyToID("_Weight3NoiseChannel");
        static int _Weight2NoiseOp = Shader.PropertyToID("_Weight2NoiseOp");
        static int _Weight3NoiseOp = Shader.PropertyToID("_Weight3NoiseOp");

        static int _HeightWeight = Shader.PropertyToID("_HeightWeight");
        static int _HeightRange = Shader.PropertyToID("_HeightRange");
        static int _HeightSmoothness = Shader.PropertyToID("_HeightSmoothness");
        static int _HeightNoise1 = Shader.PropertyToID("_HeightNoise1");
        static int _HeightNoise2 = Shader.PropertyToID("_HeightNoise");
        static int _HeightNoiseTexture = Shader.PropertyToID("_HeightNoiseTexture");
        static int _HeightNoiseChannel = Shader.PropertyToID("_HeightNoiseChannel");
        static int _SlopeWeight = Shader.PropertyToID("_SlopeWeight");
        static int _SlopeRange = Shader.PropertyToID("_SlopeRange");
        static int _SlopeSmoothness = Shader.PropertyToID("_SlopeSmoothness");
        static int _SlopeNoise = Shader.PropertyToID("_SlopeNoise");
        static int _SlopeNoise2 = Shader.PropertyToID("_SlopeNoise2");
        static int _SlopeNoiseTexture = Shader.PropertyToID("_SlopeNoiseTexture");
        static int _SlopeNoiseChannel = Shader.PropertyToID("_SlopeNoiseChannel");
        static int _AngleWeight = Shader.PropertyToID("_AngleWeight");
        static int _AngleRange = Shader.PropertyToID("_AngleRange");
        static int _AngleSmoothness = Shader.PropertyToID("_AngleSmoothness");
        static int _AngleNoise = Shader.PropertyToID("_AngleNoise");
        static int _AngleNoise2 = Shader.PropertyToID("_AngleNoise2");
        static int _AngleNoiseTexture = Shader.PropertyToID("_AngleNoiseTexture");
        static int _AngleNoiseChannel = Shader.PropertyToID("_AngleNoiseChannel");

        static int _CurvatureWeight = Shader.PropertyToID("_CurvatureWeight");
        static int _CurvatureRange = Shader.PropertyToID("_CurvatureRange");
        static int _CurvatureSmoothness = Shader.PropertyToID("_CurvatureSmoothness");
        static int _CurvatureNoise = Shader.PropertyToID("_CurvatureNoise");
        static int _CurvatureNoise2 = Shader.PropertyToID("_CurvatureNoise2");
        static int _CurvatureNoiseTexture = Shader.PropertyToID("_CurvatureNoiseTexture");
        static int _CurvatureNoiseChannel = Shader.PropertyToID("_CurvatureNoiseChannel");
        static int _CurvatureMipBias = Shader.PropertyToID("_CurvatureMipBias");

        static int _FlowWeight = Shader.PropertyToID("_FlowWeight");
        static int _FlowRange = Shader.PropertyToID("_FlowRange");
        static int _FlowSmoothness = Shader.PropertyToID("_FlowSmoothness");
        static int _FlowNoise = Shader.PropertyToID("_FlowNoise");
        static int _FlowNoise2 = Shader.PropertyToID("_FlowNoise2");
        static int _FlowNoiseTexture = Shader.PropertyToID("_FlowNoiseTexture");
        static int _FlowNoiseChannel = Shader.PropertyToID("_FlowNoiseChannel");


        static int _HeightCurve = Shader.PropertyToID("_HeightCurve");
        static int _SlopeCurve = Shader.PropertyToID("_SlopeCurve");
        static int _AngleCurve = Shader.PropertyToID("_AngleCurve");
        static int _CurvatureCurve = Shader.PropertyToID("_CurvatureCurve");
        static int _FlowCurve = Shader.PropertyToID("_FlowCurve");
        static int _GlobalOriginMTX = Shader.PropertyToID("_GlobalOriginMTX");
        static int _TerrainSize = Shader.PropertyToID("_TerrainSize");

        public void PrepareTransform(Transform transform, Terrain terrain, Material material, List<string> keywords, float densityScale = 1)
        {
            var terrainData = terrain.terrainData;
            var realHeight = terrainData.heightmapScale.y * 2;
            material.SetMatrix(_Transform, TerrainUtil.ComputeStampMatrix(terrain, transform)); ;
            material.SetVector(_RealSize, TerrainUtil.ComputeTerrainSize(terrain));

            var noisePos = terrain.transform.position;
            var originShift = Shader.GetGlobalMatrix("_GlobalOriginMTX").GetPosition();
            noisePos += originShift;

            noisePos.x /= terrain.terrainData.size.x;
            noisePos.z /= terrain.terrainData.size.z;
            material.SetVector(_TerrainSize, terrain.terrainData.size);
            material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, densityScale));
           
            if (heightFilter.enabled)
            {
                material.SetVector(_HeightRange, heightFilter.range / realHeight);
                material.SetVector(_HeightSmoothness, heightFilter.smoothness / realHeight);
            }

            falloffFilter.PrepareTerrain(material, terrain, transform, keywords);
        }

        public void PrepareMaterial(Transform transform, Material material, List<string> keywords)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FilterSet::PrepareMaterial");
           
            falloffFilter.PrepareMaterial(material, transform, keywords);

            material.SetFloat(_Weight, weight);
            if (weightNoise.noiseType != Noise.NoiseType.None)
            {
                material.SetVector(_WeightNoise, weightNoise.GetParamVector());
                material.SetVector(_WeightNoise2, weightNoise.GetParam2Vector());
                material.SetTexture(_WeightNoiseTexture, weightNoise.texture);
                material.SetTextureScale(_WeightNoiseTexture, weightNoise.GetTextureScale());
                material.SetTextureOffset(_WeightNoiseTexture, weightNoise.GetTextureOffset());
                material.SetFloat(_WeightNoiseChannel, (int)weightNoise.channel);
            }
            if (weight2Noise.noiseType != Noise.NoiseType.None)
            {
                material.SetVector(_Weight2Noise, weight2Noise.GetParamVector());
                material.SetVector(_Weight2Noise2, weight2Noise.GetParam2Vector());
                material.SetTexture(_Weight2NoiseTexture, weight2Noise.texture);
                material.SetTextureScale(_Weight2NoiseTexture, weight2Noise.GetTextureScale());
                material.SetTextureOffset(_Weight2NoiseTexture, weight2Noise.GetTextureOffset());
                material.SetFloat(_Weight2NoiseChannel, (int)weight2Noise.channel);
                material.SetFloat(_Weight2NoiseOp, (int)weight2NoiseOp);
            }
            if (weight3Noise.noiseType != Noise.NoiseType.None)
            {
                material.SetVector(_Weight3Noise, weight3Noise.GetParamVector());
                material.SetVector(_Weight3Noise2, weight3Noise.GetParam2Vector());
                material.SetTexture(_Weight3NoiseTexture, weight3Noise.texture);
                material.SetTextureScale(_Weight3NoiseTexture, weight3Noise.GetTextureScale());
                material.SetTextureOffset(_Weight3NoiseTexture, weight3Noise.GetTextureOffset());
                material.SetFloat(_Weight3NoiseChannel, (int)weight3Noise.channel);
                material.SetFloat(_Weight3NoiseOp, (int)weight3NoiseOp);
            }


            if (heightFilter.enabled)
            {
                material.SetFloat(_HeightWeight, heightFilter.weight);
                material.SetVector(_HeightNoise1, heightFilter.noise.GetParamVector());
                material.SetVector(_HeightNoise2, heightFilter.noise.GetParam2Vector());
                material.SetTexture(_HeightNoiseTexture, heightFilter.noise.texture);
                material.SetTextureScale(_HeightNoiseTexture, heightFilter.noise.GetTextureScale());
                material.SetTextureOffset(_HeightNoiseTexture, heightFilter.noise.GetTextureOffset());
                material.SetFloat(_HeightNoiseChannel, (int)heightFilter.noise.channel);
            }
            if (slopeFilter.enabled)
            {
                material.SetFloat(_SlopeWeight, slopeFilter.weight);
                material.SetVector(_SlopeRange, slopeFilter.range * Mathf.Deg2Rad);
                material.SetVector(_SlopeSmoothness, slopeFilter.smoothness * Mathf.Deg2Rad);
                material.SetVector(_SlopeNoise, slopeFilter.noise.GetParamVector());
                material.SetVector(_SlopeNoise2, slopeFilter.noise.GetParam2Vector());
                material.SetTexture(_SlopeNoiseTexture, slopeFilter.noise.texture);
                material.SetTextureScale(_SlopeNoiseTexture, slopeFilter.noise.GetTextureScale());
                material.SetTextureOffset(_SlopeNoiseTexture, slopeFilter.noise.GetTextureOffset());
                material.SetFloat(_SlopeNoiseChannel, (int)slopeFilter.noise.channel);
            }
            if (angleFilter.enabled)
            {
                material.SetFloat(_AngleWeight, angleFilter.weight);
                material.SetVector(_AngleRange, angleFilter.range * Mathf.Deg2Rad);
                material.SetVector(_AngleSmoothness, angleFilter.smoothness * Mathf.Deg2Rad);
                material.SetVector(_AngleNoise, angleFilter.noise.GetParamVector());
                material.SetVector(_AngleNoise2, angleFilter.noise.GetParam2Vector());
                material.SetTexture(_AngleNoiseTexture, angleFilter.noise.texture);
                material.SetTextureScale(_AngleNoiseTexture, angleFilter.noise.GetTextureScale());
                material.SetTextureOffset(_AngleNoiseTexture, angleFilter.noise.GetTextureOffset());
                material.SetFloat(_AngleNoiseChannel, (int)angleFilter.noise.channel);
            }

            if (curvatureFilter.enabled)
            {
                material.SetFloat(_CurvatureWeight, curvatureFilter.weight);
                material.SetVector(_CurvatureRange, curvatureFilter.range);
                material.SetVector(_CurvatureSmoothness, curvatureFilter.smoothness);
                material.SetVector(_CurvatureNoise, curvatureFilter.noise.GetParamVector());
                material.SetVector(_CurvatureNoise2, curvatureFilter.noise.GetParam2Vector());
                material.SetTexture(_CurvatureNoiseTexture, curvatureFilter.noise.texture);
                material.SetTextureScale(_CurvatureNoiseTexture, curvatureFilter.noise.GetTextureScale());
                material.SetTextureOffset(_CurvatureNoiseTexture, curvatureFilter.noise.GetTextureOffset());
                material.SetFloat(_CurvatureNoiseChannel, (int)curvatureFilter.noise.channel);
                material.SetFloat(_CurvatureMipBias, curvatureFilter.mipBias);
            }

            if (flowFilter.enabled)
            {
                material.SetFloat(_FlowWeight, flowFilter.weight);
                material.SetVector(_FlowRange, flowFilter.range);
                material.SetVector(_FlowSmoothness, flowFilter.smoothness);
                material.SetVector(_FlowNoise, flowFilter.noise.GetParamVector());
                material.SetVector(_FlowNoise2, flowFilter.noise.GetParam2Vector());
                material.SetTexture(_FlowNoiseTexture, flowFilter.noise.texture);
                material.SetTextureScale(_FlowNoiseTexture, flowFilter.noise.GetTextureScale());
                material.SetTextureOffset(_FlowNoiseTexture, flowFilter.noise.GetTextureOffset());
                material.SetFloat(_FlowNoiseChannel, (int)flowFilter.noise.channel);
            }

            if (heightFilter.enabled)
            {
                keywords.Add("_HEIGHTFILTER");
                if (heightFilter.filterType == Filter.FilterType.Curve)
                {
                    keywords.Add("_HEIGHTCURVE");
                    material.SetTexture(_HeightCurve, heightFilter.curveTexture);
                }
                heightFilter.noise.EnableKeyword(material, "_HEIGHT", keywords);
            }
            if (slopeFilter.enabled)
            {
                keywords.Add("_SLOPEFILTER");
                if (slopeFilter.filterType == Filter.FilterType.Curve)
                {
                    keywords.Add("_SLOPECURVE");
                    material.SetTexture(_SlopeCurve, slopeFilter.curveTexture);
                }
                slopeFilter.noise.EnableKeyword(material, "_SLOPE", keywords);
            }
            if (angleFilter.enabled)
            {
                keywords.Add("_ANGLEFILTER");
                if (angleFilter.filterType == Filter.FilterType.Curve)
                {
                    keywords.Add("_ANGLECURVE");
                    material.SetTexture(_AngleCurve, angleFilter.curveTexture);
                }
                angleFilter.noise.EnableKeyword(material, "_ANGLE", keywords);
            }
            if (curvatureFilter.enabled)
            {
                keywords.Add("_CURVATUREFILTER");
                if (curvatureFilter.filterType == Filter.FilterType.Curve)
                {
                    keywords.Add("_CURVATURECURVE");
                    material.SetTexture(_CurvatureCurve, curvatureFilter.curveTexture);
                }
                curvatureFilter.noise.EnableKeyword(material, "_CURVATURE", keywords);
            }
            if (flowFilter.enabled)
            {
                keywords.Add("_FLOWFILTER");
                if (flowFilter.filterType == Filter.FilterType.Curve)
                {
                    keywords.Add("_FLOWCURVE");
                    material.SetTexture(_FlowCurve, curvatureFilter.curveTexture);
                }
                flowFilter.noise.EnableKeyword(material, "_FLOW", keywords);
            }

            if (textureFilterEnabled)
            {
                keywords.Add("_TEXTUREFILTER");
            }

            weightNoise.EnableKeyword(material, "_WEIGHT", keywords);
            weight2Noise.EnableKeyword(material, "_WEIGHT2", keywords);
            weight3Noise.EnableKeyword(material, "_WEIGHT3", keywords);
            UnityEngine.Profiling.Profiler.EndSample();

        }

        static int _PlacementSDF = Shader.PropertyToID("_PlacementSDF");
        static int _PlacementSDF2 = Shader.PropertyToID("_PlacementSDF2");
        static int _PlacementSDF3 = Shader.PropertyToID("_PlacementSDF3");
        static int _DistancesFromTrees = Shader.PropertyToID("_DistancesFromTrees");
        static int _DistancesFromObject = Shader.PropertyToID("_DistancesFromObject");
        static int _DistancesFromParent = Shader.PropertyToID("_DistancesFromParent");
        static int _SDFClamp = Shader.PropertyToID("_SDFClamp");

        public static void PrepareSDFFilter(Stamp.KeywordBuilder keywords, Material material, Transform transform,
                                     OcclusionData od, float ratio, bool sdfClamp,
                                     float minTree, float maxTree,
                                     float minObj, float maxObj,
                                     float minParent, float maxParent
                                     
            )
        {
            // because people think they have to specify a range, we unclamp the top of the range
            // so they get infinite if they move it to max.
            if (maxTree >= 255)
                maxTree = minTree;
            if (maxObj >= 255)
                maxObj = minObj;
            if (maxParent >= 255)
                maxParent = minParent;

            if (minObj + minTree + maxObj + maxTree <= 0 && !((minParent > 0 || maxParent > 0) && transform.parent != null))
            {
                return;
            }
            keywords.Add("_SDFFILTERING");
            if (minTree > 0 || maxTree > 0)
            {
                material.SetVector(_DistancesFromTrees, new Vector2(minTree * ratio, maxTree * ratio));
                material.SetTexture(_PlacementSDF, od.treeSDF);
            }
            if (minObj > 0 || maxObj > 0)
            {
                material.SetVector(_DistancesFromObject, new Vector2(minObj * ratio, maxObj * ratio));
                material.SetTexture(_PlacementSDF2, od.objectSDF);
            }

            if ((minParent > 0 || maxParent > 0) && transform.parent != null)
            {
                var parent = transform.parent.GetComponentInParent<ISpawner>(false);
                if (parent != null)
                {
                    var sdf3 = parent.GetSDF(od.terrain);
                    if (sdf3 != null)
                    {
                        
                        material.SetVector(_DistancesFromParent, new Vector2(minParent * ratio, maxParent * ratio));
                        material.SetTexture(_PlacementSDF3, sdf3);
                    }
                }
            }
            material.SetFloat(_SDFClamp, sdfClamp ? 1.0f : 0);
        }


    }
}