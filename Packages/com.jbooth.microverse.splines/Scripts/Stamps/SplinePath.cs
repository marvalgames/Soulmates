
using UnityEngine;
using System.Collections.Generic;


using UnityEngine.Splines;

namespace JBooth.MicroVerseCore
{
    public class SplinePath : Stamp, IHeightModifier, ITextureModifier
    {
        public enum CombineMode
        {
            Override = 0,
            Max = 1,
            Min = 2,
            Blend = 9,
        }
        public CombineMode heightBlendMode = CombineMode.Override;
        public enum SDFRes
        {
            k256 = 256,
            k512 = 512,
            k1024 = 1024,
            k2048 = 2048
        }

        public enum SearchQuality
        {
            VeryLow = 64,
            Low = 128,
            Medium = 256,
            High = 512,
            VeryHigh = 1024,
            ExtremelyHigh = 2048
        }

        [HideInInspector] public SplineRenderer.RenderDesc[] multiSpline;

        public SplineContainer spline;
        [Tooltip("When true, a closed spline is treated as an area for the effect instead of following the path")]

        public Noise positionNoise = new Noise();
        public Noise widthNoise = new Noise();
        
        [Tooltip("Blend between existing height map and new one")]
        [Range(0, 1)] public float blend = 1; 

        public bool treatAsSplineArea;
        [Tooltip("Resolution of the internal SDF used for the spline. Higher makes edits take longer")]
        public SDFRes sdfRes = SDFRes.k512;
        [Tooltip("Higher values will spend more time finding the closest point on the spline, improving quality but increasing update times")]
        public SearchQuality searchQuality = SearchQuality.Medium;
        [Tooltip("Should the heightmap be adjusted to match the spline")]
        public bool modifyHeightMap = true;
        [Tooltip("Width of the area")]
        public float width = 1;
        [Tooltip("How many units should it be before the effect is gone")]
        public float smoothness = 2;
        [Tooltip("Positive values push the terrain down, negative up")]
        public float trench = 0;
        public AnimationCurve trenchCurve = AnimationCurve.Constant(0, 1, 0);
        public bool useTrenchCurve;
        public Noise heightNoise = new Noise();
        public Easing embankmentEasing = new Easing();
        public Noise embankmentNoise = new Noise();

        public bool useTextureCurve;
        public bool useDetailCurve;
        public bool useTreeCurve;
        public AnimationCurve textureCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public AnimationCurve treeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public AnimationCurve detailCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Allows you to texture the area of the spline with a terrain layer")]
        public bool modifySplatMap = true;
        public TerrainLayer layer;
        [Tooltip("Width of texturing effect")]
        [Range(0,1)]public float splatWeight = 1;
        public float splatWidth = 1;
        [Tooltip("How many units should it be before the effect is gone")]
        public float splatSmoothness = 2;
        public Noise splatNoise = new Noise();
        [Tooltip("Texture the area of the spline's falloff with a separate texture")]
        public TerrainLayer embankmentLayer;

        [Tooltip("When true, tree's will not appear on the path")]
        public bool clearTrees = true;
        [Tooltip("Width of tree clearing effect")]
        public float treeWidth = 1;
        [Tooltip("Falloff of tree clearing effect")]
        public float treeSmoothness = 3;

        [Tooltip("When true, detail objects will not appear on the path")]
        public bool clearDetails = true;
        [Tooltip("Width of detail clearing effect")]
        public float detailWidth = 1;
        [Tooltip("falloff of detail clearing effect")]
        public float detailSmoothness = 3;

        [Tooltip("When true, objects will not appear on the path")]
        public bool clearObjects = false;
        [Tooltip("Width of detail clearing effect")]
        public float objectWidth = 1;
        [Tooltip("falloff of detail clearing effect")]
        public float objectSmoothness = 3;

        [Tooltip("Will prevent future things from modifying heights")]
        public bool occludeHeightMod = false;
        [Tooltip("Width of detail clearing effect")]
        public float occludeHeightWidth = 1;
        [Tooltip("falloff of detail clearing effect")]
        public float occludeHeightSmoothness = 3;
        [Tooltip("Will prevent future things from modifying splats")]
        public bool occludeTextureMod = false;
        [Tooltip("Width of detail clearing effect")]
        public float occludeTextureWidth = 1;
        [Tooltip("falloff of detail clearing effect")]
        public float occludeTextureSmoothness = 3;

        [Tooltip("Curve to use when interpolating the width of the spline")]
        public Easing splineWidthEasing = new Easing();

        static Material heightMat;
        static Material splatMat;

        float ComputeMaxSDF()
        {
            float val = 0;
            if (modifyHeightMap)
                val = width + smoothness;
            if (modifySplatMap)
                val = Mathf.Max(val, splatWidth + splatSmoothness);
            if (clearTrees)
                val = Mathf.Max(val, treeWidth + treeSmoothness);
            if (clearDetails)
                val = Mathf.Max(val, detailWidth + detailSmoothness);
            if (occludeHeightMod)
                val = Mathf.Max(val, occludeHeightSmoothness + occludeHeightWidth);
            if (occludeTextureMod)
                val = Mathf.Max(val, occludeTextureWidth + occludeTextureSmoothness);
            if (splineWidths != null)
            {
                float mw = 0;
                foreach (var sw in splineWidths)
                {
                    foreach (var w in sw.widthData)
                    {
                        if (w.Value > mw)
                            mw = w.Value;
                    }
                }
                val += mw;
            }
            return val + 3; // lets make sure it's at least something..
            
        }

        [System.Serializable]
        public class SplineWidthData
        {
            public SplineData<float> widthData = new SplineData<float>();
        }

        public List<SplineWidthData> splineWidths = new List<SplineWidthData>();

        RenderBuffer[] multipleRenderBuffers;

        public bool NeedCurvatureMap() { return false; }
        public bool NeedFlowMap() { return false; }

        Dictionary<Terrain, SplineRenderer> splineRenderers = new Dictionary<Terrain, SplineRenderer>();

        public override void OnEnable()
        {
            if (spline == null)
            {
                spline = GetComponent<SplineContainer>();
            }
            base.OnEnable();
           
        }

        public void ClearSplineRenders(Bounds? bounds = null)
        {
            if (bounds == null)
            {
                foreach (var sr in splineRenderers.Values)
                {
                    sr.Dispose();
                }
                splineRenderers.Clear();
            }
            else
            {
                var b = bounds.Value;
                b.max = new Vector3(b.max.x, 100000, b.max.z);
                b.min = new Vector3(b.min.x, -100000, b.min.z);
                b.Expand(this.ComputeMaxSDF());
                List<Terrain> toClear = new List<Terrain>();
                foreach (var t in splineRenderers.Keys)
                {
                    if (TerrainUtil.ComputeTerrainBounds(t).Intersects(b))
                    {
                        toClear.Add(t);
                    }
                }
                foreach (var t in toClear)
                {
                    splineRenderers[t].Dispose();
                    splineRenderers.Remove(t);
                }
            }
            ClearCachedBounds();
        }

        SplineRenderer GetSplineRenderer(Terrain terrain)
        {
            if (splineRenderers.ContainsKey(terrain))
            {
                var sr = splineRenderers[terrain];
                var mx = ComputeMaxSDF();
                if (sr.lastMaxSDF < mx)
                {
                    if (multiSpline != null)
                    {
                        sr.Render(multiSpline, terrain, (int)sdfRes, mx, (int)searchQuality);
                    }
                    else if (spline != null)
                    {
                        sr.Render(spline, terrain, positionNoise, widthNoise, splineWidths, splineWidthEasing, (int)sdfRes, mx, (int)searchQuality);
                    }
                }
                return sr;
            }
            else
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                if (terrainBounds.Intersects(GetBounds()))
                {
                    SplineRenderer sr = new SplineRenderer();
                    bounds = new Bounds(Vector3.zero, Vector3.zero);
                    if (multiSpline != null)
                    {
                        sr.Render(multiSpline, terrain, (int)sdfRes, ComputeMaxSDF(), (int)searchQuality);
                    }
                    else if (spline != null)
                    {
                        sr.Render(spline, terrain, positionNoise, widthNoise, splineWidths, splineWidthEasing, (int)sdfRes, ComputeMaxSDF(), (int)searchQuality);
                    }
                    
                    splineRenderers.Add(terrain, sr);
                    return sr;
                }
            }
            return null;
        }

        public void UpdateSplineSDFs()
        {
            ClearSplineRenders();
            if (MicroVerse.instance == null)
                return;
            MicroVerse.instance.SyncTerrainList();
            foreach (var terrain in MicroVerse.instance.terrains)
            {
                GetSplineRenderer(terrain);
            }
        }

        public void Initialize()
        {
            if (heightMat == null)
            {
                heightMat = new Material(Shader.Find("Hidden/MicroVerse/SplinePathHeight"));
            }
            if (splatMat == null)
            {
                splatMat = new Material(Shader.Find("Hidden/MicroVerse/SplinePathTexture"));
            }
            if (multipleRenderBuffers == null)
            {
                multipleRenderBuffers = new RenderBuffer[2];
            }
        }
        int mainChannelIndex = -1;
        int embankmentChannelIndex;

        public override void OnDisable()
        {
            base.OnDisable();
            ClearSplineRenders();
        }
        protected override void OnDestroy()
        {
            if (heightMat != null) DestroyImmediate(heightMat);
            if (splatMat != null) DestroyImmediate(splatMat);
            if (sdfToMaskMat != null) DestroyImmediate(sdfToMaskMat);
            ClearSplineRenders();
            base.OnDestroy();
        }


        static int _SplineSDF = Shader.PropertyToID("_SplineSDF");
        static int _TerrainHeight = Shader.PropertyToID("_TerrainHeight");
        static int _TreeWidth = Shader.PropertyToID("_TreeWidth");
        static int _Channel = Shader.PropertyToID("_Channel");
        static int _TreeSmoothness = Shader.PropertyToID("_TreeSmoothness");
        static int _DetailWidth = Shader.PropertyToID("_DetailWidth");
        static int _DetailSmoothness = Shader.PropertyToID("_DetailSmoothness");
        static int _SplatWidth = Shader.PropertyToID("_SplatWidth");
        static int _SplatSmoothness = Shader.PropertyToID("_SplatSmoothness");
        static int _WeightMap = Shader.PropertyToID("_WeightMap");
        static int _IndexMap = Shader.PropertyToID("_IndexMap");
        static int _AlphaMapSize = Shader.PropertyToID("_AlphaMapSize");
        static int _SplatWeight = Shader.PropertyToID("_SplatWeight");
        static int _HeightMapSize = Shader.PropertyToID("_HeightMapSize");
        static int _Blend = Shader.PropertyToID("_Blend");
        static Shader sdfToMaskShader = null;
        static Material sdfToMaskMat = null;
        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest,
            HeightmapData heightmapData, OcclusionData od)
        {
            bool ret = false;

            keywordBuilder.Clear();
            SplineRenderer sr = GetSplineRenderer(od.terrain);
            if (sr != null)
            {
                if (modifyHeightMap)
                {
                    PrepareMaterial(heightMat, heightmapData, keywordBuilder.keywords);

                    heightMat.SetTexture(_SplineSDF, sr.splineSDF);
                    heightMat.SetFloat(_TerrainHeight, od.terrain.transform.position.y);
                    heightMat.SetFloat(_HeightMapSize, source.width);
                    keywordBuilder.Assign(heightMat);
                    Graphics.Blit(source, dest, heightMat);
                    heightMat.SetFloat(_Blend, blend);
                    ret = true;
                }

                if (clearTrees || clearDetails || occludeHeightMod || occludeTextureMod)
                {
                    if (sdfToMaskShader == null)
                    {
                        sdfToMaskShader = Shader.Find("Hidden/MicroVerse/SDFToMask");
                    }
                    if (sdfToMaskMat == null)
                    {
                        sdfToMaskMat = new Material(sdfToMaskShader);
                    }
                    sdfToMaskMat.DisableKeyword("_TREATASAREA");
                    if (treatAsSplineArea)
                    {
                        sdfToMaskMat.EnableKeyword("_TREATASAREA");
                    }
                    sdfToMaskMat.SetFloat(_HeightWidth, occludeHeightMod ? occludeHeightWidth : -1);
                    sdfToMaskMat.SetFloat(_HeightSmoothness, occludeHeightSmoothness);
                    sdfToMaskMat.SetFloat(_SplatWidth, occludeTextureMod ? occludeTextureWidth : -1);
                    sdfToMaskMat.SetFloat(_SplatSmoothness, occludeTextureSmoothness);
                    sdfToMaskMat.SetFloat(_TreeWidth, clearTrees ? treeWidth : -1);
                    sdfToMaskMat.SetFloat(_TreeSmoothness, treeSmoothness);
                    sdfToMaskMat.SetFloat(_DetailWidth, clearDetails ? detailWidth : -1);
                    sdfToMaskMat.SetFloat(_DetailSmoothness, detailSmoothness);

                    sdfToMaskMat.SetTexture(_SplineSDF, sr.splineSDF);

                    sdfToMaskMat.DisableKeyword("_SPLINECURVETREEWEIGHT");
                    sdfToMaskMat.DisableKeyword("_SPLINECURVEDETAILWEIGHT");
                    if (useTreeCurve)
                    {
                        sdfToMaskMat.EnableKeyword("_SPLINECURVETREEWEIGHT");
                        UpdateCachedTreeWeight();
                        sdfToMaskMat.SetTexture("_SplineTreeWeight", cachedSplineTreeWeight);
                    }

                    if (useDetailCurve)
                    {
                        sdfToMaskMat.EnableKeyword("_SPLINECURVEDETAILWEIGHT");
                        UpdateCachedDetailWeight();
                        sdfToMaskMat.SetTexture("_SplineDetailWeight", cachedSplineDetailWeight);
                    }

                    var rt = RenderTexture.GetTemporary(od.terrainMask.descriptor);
                    rt.name = "SplinePath::OcclusionRender";
                    rt.wrapMode = TextureWrapMode.Clamp;

                    Graphics.Blit(od.terrainMask, rt, sdfToMaskMat);
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(od.terrainMask);
                    od.terrainMask = rt;
                    RenderTexture.active = dest;
                }
                if (clearObjects)
                {
                    if (sdfToMaskShader == null)
                    {
                        sdfToMaskShader = Shader.Find("Hidden/MicroVerse/SDFToMask");
                    }
                    if (sdfToMaskMat == null)
                    {
                        sdfToMaskMat = new Material(sdfToMaskShader);
                    }
                    sdfToMaskMat.DisableKeyword("_TREATASAREA");
                    sdfToMaskMat.SetFloat(_HeightWidth, objectWidth);
                    sdfToMaskMat.SetFloat(_HeightSmoothness, objectSmoothness);

                    sdfToMaskMat.SetTexture(_SplineSDF, sr.splineSDF);

                    var rt = RenderTexture.GetTemporary(od.objectMask.descriptor);
                    rt.name = "SplinePath::OcclusionRender";
                    rt.wrapMode = TextureWrapMode.Clamp;

                    Graphics.Blit(od.objectMask, rt, sdfToMaskMat);
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(od.objectMask);
                    od.objectMask = rt;
                    RenderTexture.active = dest;
                    
                }

            }

            return ret;
        }

        Texture2D cachedSplineTextureWeight;
        Texture2D cachedSplineTreeWeight;
        Texture2D cachedSplineDetailWeight;
        Texture2D cachedSplineTrenchWeight;


        public void ClearCachedSplineTextureCurve()
        {
            if (cachedSplineTextureWeight != null)
            {
                DestroyImmediate(cachedSplineTextureWeight);
            }
        }

        public void ClearCachedSplineTreeCurve()
        {
            if (cachedSplineTreeWeight != null)
            {
                DestroyImmediate(cachedSplineTreeWeight);
            }
        }

        public void ClearCachedSplineDetailCurve()
        {
            if (cachedSplineDetailWeight != null)
            {
                DestroyImmediate(cachedSplineDetailWeight);
            }
        }

        public void ClearCachedSplineTrenchCurve()
        {
            if (cachedSplineTrenchWeight != null)
            {
                DestroyImmediate(cachedSplineTrenchWeight);
            }
        }

        public void UpdateCachedTextureWeight()
        {
            if (cachedSplineTextureWeight == null)
            {
                cachedSplineTextureWeight = new Texture2D(128, 1, TextureFormat.R8, false);
                cachedSplineTextureWeight.filterMode = FilterMode.Bilinear;
                cachedSplineTextureWeight.wrapMode = TextureWrapMode.Clamp;
                cachedSplineTextureWeight.hideFlags = HideFlags.HideAndDontSave;
                for (int i = 0; i < 128; ++i)
                {
                    cachedSplineTextureWeight.SetPixel(i, 0, new Color(textureCurve.Evaluate((float)i/128.0f), 0, 0, 1));
                }
                cachedSplineTextureWeight.Apply();
            }
        }

        public void UpdateCachedTreeWeight()
        {
            if (cachedSplineTreeWeight == null)
            {
                cachedSplineTreeWeight = new Texture2D(128, 1, TextureFormat.R8, false);
                cachedSplineTreeWeight.filterMode = FilterMode.Bilinear;
                cachedSplineTreeWeight.wrapMode = TextureWrapMode.Clamp;
                cachedSplineTreeWeight.hideFlags = HideFlags.HideAndDontSave;
                for (int i = 0; i < 128; ++i)
                {
                    cachedSplineTreeWeight.SetPixel(i, 0, new Color(treeCurve.Evaluate((float)i / 128.0f), 0, 0, 1));
                }
                cachedSplineTreeWeight.Apply();
            }
        }

        public void UpdateCachedTrenchCurve()
        {
            if (cachedSplineTrenchWeight == null)
            {
                cachedSplineTrenchWeight = new Texture2D(128, 1, TextureFormat.RFloat, false);
                cachedSplineTrenchWeight.filterMode = FilterMode.Bilinear;
                cachedSplineTrenchWeight.wrapMode = TextureWrapMode.Clamp;
                cachedSplineTrenchWeight.hideFlags = HideFlags.HideAndDontSave;
                for (int i = 0; i < 128; ++i)
                {
                    cachedSplineTrenchWeight.SetPixel(i, 0, new Color(trenchCurve.Evaluate((float)i / 128.0f), 0, 0, 1));
                }
                cachedSplineTrenchWeight.Apply();
            }
        }

        public void UpdateCachedDetailWeight()
        {
            if (cachedSplineDetailWeight == null)
            {
                cachedSplineDetailWeight = new Texture2D(128, 1, TextureFormat.R8, false);
                cachedSplineDetailWeight.filterMode = FilterMode.Bilinear;
                cachedSplineDetailWeight.wrapMode = TextureWrapMode.Clamp;
                cachedSplineDetailWeight.hideFlags = HideFlags.HideAndDontSave;
                for (int i = 0; i < 128; ++i)
                {
                    cachedSplineDetailWeight.SetPixel(i, 0, new Color(detailCurve.Evaluate((float)i / 128.0f), 0, 0, 1));
                }
                cachedSplineDetailWeight.Apply();
            }
        }

        public bool ApplyTextureStamp(RenderTexture indexSrc, RenderTexture indexDest,
            RenderTexture weightSrc, RenderTexture weightDest,
            TextureData splatmapData, OcclusionData od)
        {
            if (layer == null)
                return false;
            if (!modifySplatMap)
                return false;

            SplineRenderer sr = GetSplineRenderer(od.terrain);
            if (sr != null)
            {
                mainChannelIndex = TerrainUtil.FindTextureChannelIndex(od.terrain, layer);
                embankmentChannelIndex = TerrainUtil.FindTextureChannelIndex(od.terrain, embankmentLayer);


                if (mainChannelIndex == -1)
                {
                    //Debug.LogError("Layer is not on terrain ", layer);
                    return false;
                }
                keywordBuilder.Clear();

                PrepareMaterial(splatMat, splatmapData, keywordBuilder.keywords);
                splatMat.SetTexture(_SplineSDF, sr.splineSDF);
                splatMat.SetFloat(_Channel, mainChannelIndex);
                splatMat.SetTexture(_WeightMap, weightSrc);
                splatMat.SetTexture(_IndexMap, indexSrc);
                splatMat.SetFloat(_AlphaMapSize, indexSrc.width);
                splatMat.SetFloat(_SplatWeight, splatWeight);
                if (useTextureCurve)
                {
                    keywordBuilder.Add("_SPLINECURVETEXTUREWEIGHT");
                    UpdateCachedTextureWeight();
                    splatMat.SetTexture("_SplineTextureWeight", cachedSplineTextureWeight);
                }
                keywordBuilder.Assign(splatMat);

                multipleRenderBuffers[0] = indexDest.colorBuffer;
                multipleRenderBuffers[1] = weightDest.colorBuffer;

                Graphics.SetRenderTarget(multipleRenderBuffers, indexDest.depthBuffer);

                Graphics.Blit(null, splatMat, 0);
                return true;
            }
            return false;

        }


        public void Dispose()
        {
            
        }

        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");
        static int _Width = Shader.PropertyToID("_Width");
        static int _Smoothness = Shader.PropertyToID("_Smoothness");
        static int _RealHeight = Shader.PropertyToID("_RealHeight");
        static int _Trench = Shader.PropertyToID("_Trench");
        static int _TrenchCurve = Shader.PropertyToID("_TrenchCurve");
        static int _CombineMode = Shader.PropertyToID("_CombineMode");
        static int _CombineBlend = Shader.PropertyToID("_CombineBlend");

        void PrepareMaterial(Material material, HeightmapData heightmapData, List<string> keywords)
        {
            if (treatAsSplineArea)
            {
                keywordBuilder.Add("_TREATASAREA");
            }
            var noisePos = heightmapData.terrain.transform.position;
            noisePos.x /= heightmapData.terrain.terrainData.size.x;
            noisePos.z /= heightmapData.terrain.terrainData.size.z;

            material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, GetTerrainScalingFactor(heightmapData.terrain)));


            material.SetFloat(_Width, width);
            material.SetFloat(_Smoothness, smoothness);
            material.SetFloat(_Trench, trench);
            if (useTrenchCurve)
            {
                keywords.Add("_SPLINECURVETRENCHWEIGHT");
                UpdateCachedTrenchCurve();
                material.SetTexture(_TrenchCurve, cachedSplineTrenchWeight);
            }

            heightNoise.PrepareMaterial(material, "_HEIGHT", "_Height", keywords);
            material.SetFloat(_RealHeight, heightmapData.RealHeight);
            material.SetFloat(_Blend, blend);
            material.SetFloat(_CombineBlend, blend);
            embankmentEasing.PrepareMaterial(material, "_FALLOFF", keywords);
            embankmentNoise.PrepareMaterial(material, "_FALLOFF", "_Falloff", keywords);

            material.SetInt(_CombineMode, (int)heightBlendMode); 
        }


        static int _EmbankmentChannel = Shader.PropertyToID("_EmbankmentChannel");
        static int _HeightWidth = Shader.PropertyToID("_HeightWidth");
        static int _HeightSmoothness = Shader.PropertyToID("_HeightSmoothness");
        static int _NoiseParams = Shader.PropertyToID("_NoiseParams");
        static int _NoiseParams2 = Shader.PropertyToID("_NoiseParams2");
        static int _SplatNoiseChannel = Shader.PropertyToID("_SplatNoiseChannel");
        static int _SplatNoiseTexture = Shader.PropertyToID("_SplatNoiseTexture");


        void PrepareMaterial(Material material, TextureData splatmapData, List<string> keywords)
        {
            if (treatAsSplineArea)
            {
                keywordBuilder.Add("_TREATASAREA");
            }
            material.SetFloat(_Width, splatWidth);
            material.SetFloat(_Smoothness, splatSmoothness);
            material.SetFloat(_EmbankmentChannel, embankmentChannelIndex);
            material.SetFloat(_HeightWidth, width);
            material.SetFloat(_HeightSmoothness, smoothness);
            material.SetVector(_NoiseParams, splatNoise.GetParamVector());
            material.SetVector(_NoiseParams2, splatNoise.GetParam2Vector());
            material.SetFloat(_SplatNoiseChannel, (int)splatNoise.channel);
            material.SetTexture(_SplatNoiseTexture, splatNoise.texture);
            material.SetTextureScale(_SplatNoiseTexture, splatNoise.GetTextureScale());
            material.SetTextureOffset(_SplatNoiseTexture, splatNoise.GetTextureOffset());
            material.SetFloat(_CombineBlend, blend);

            var noisePos = splatmapData.terrain.transform.position;
            noisePos.x /= splatmapData.terrain.terrainData.size.x;
            noisePos.z /= splatmapData.terrain.terrainData.size.z;

            material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, GetTerrainScalingFactor(splatmapData.terrain)));
            splatNoise.EnableKeyword(material, "_SPLAT", keywords);

            
            if (embankmentChannelIndex != -1)
            {
                keywordBuilder.Add("_EMBANKMENT");
            }
        }

        // Spline Bounds computation in Unity is stupidly slow. Rather than rewrite it all,
        // I just cache it. 
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        public static Bounds ComputeBounds(SplineContainer spline, float expand)
        {
            if (spline == null || spline.Spline == null)
                return new Bounds(new Vector3(-999999, -999999, -99999), Vector3.one);
            Bounds b = SplineUtility.GetBounds(spline.Spline, spline.transform.localToWorldMatrix);
            b.Expand(expand);
            b.max = new Vector3(b.max.x, 100000, b.max.z);
            b.min = new Vector3(b.min.x, -100000, b.min.z);

            for (int i = 1; i < spline.Splines.Count; ++i)
            {
                Spline s = spline.Splines[i];
                Bounds sb = SplineUtility.GetBounds(s, spline.transform.localToWorldMatrix);
                
                sb.center = spline.transform.localToWorldMatrix.MultiplyPoint(sb.center);
                sb.size = spline.transform.localToWorldMatrix.MultiplyPoint(sb.size);
                sb.Expand(expand);
                sb.max = new Vector3(sb.max.x, 100000, sb.max.z);
                sb.min = new Vector3(sb.min.x, -100000, sb.min.z);
                b.Encapsulate(sb);
            }

            return b;
        }
        
        public override Bounds GetBounds()
        {
            if (bounds.size == Vector3.zero)
            {
                float expand = (Mathf.Max(width, splatWidth));
                expand = (Mathf.Max(expand, smoothness));
                expand = (Mathf.Max(expand, splatSmoothness));
                if (multiSpline != null)
                {
                    int count = 0;
                    foreach (var m in multiSpline)
                    {
                        if (m.splineContainer != null)
                        {
                            if (count == 0)
                            {
                                bounds = ComputeBounds(m.splineContainer, expand + m.widthBoost + positionNoise.amplitude * 0.5f);
                            }
                            else
                            {
                                bounds.Encapsulate(ComputeBounds(m.splineContainer, expand + m.widthBoost + positionNoise.amplitude * 0.5f));
                            }
                            count++;
                        }
                    }
                }
                else if (spline != null)
                {
                    bounds = ComputeBounds(spline, expand);
                }
                else
                    bounds = new Bounds(Vector3.zero, Vector3.zero);
            }
            return bounds;
        }

#if UNITY_EDITOR
        public override void OnMoved()
        {
            ClearSplineRenders();
            base.OnMoved();
        }
#endif


        public void InqTerrainLayers(Terrain terrain, List<TerrainLayer> layers)
        {
            if (layer != null)
                layers.Add(layer);
            if (embankmentLayer != null)
                layers.Add(embankmentLayer);
           
        }
    }

}

