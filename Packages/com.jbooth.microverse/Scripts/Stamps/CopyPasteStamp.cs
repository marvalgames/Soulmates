using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace JBooth.MicroVerseCore
{
    public class CopyPasteStamp : Stamp, IHeightModifier, ITextureModifier, IHoleModifier
#if __MICROVERSE_VEGETATION__
            , ITreeModifier, IDetailModifier
#endif
    {
        public CopyStamp stamp;
        public bool copyHeights = true;
        public bool copyTexturing = true;
        public bool copyTrees = true;
        public bool copyDetails = true;
        public bool copyHoles = true;

        public bool applyHeights = true;
        public bool applyTexturing = true;
        public bool applyTrees = false;
        public bool applyDetails = false;
        public bool applyHoles = false;
        [Tooltip("When true, the stamp scale is aligned to pixel sizes, providing a precise copy instead of interpolating the data between pixels. You may want to turn this off if you are scaling/rotating a copy paste stamp")]
        public bool pixelQuantization = true;

        public HeightStamp heightStamp { get; private set; }
        Material splatPaste;
        RenderBuffer[] _mrt;
        float[] channels = new float[32];


        static float FindClosestDivisible(float inputValue, float increment)
        {
            float floorDivisible = Mathf.Floor(inputValue / increment) * increment;
            float ceilDivisible = Mathf.Ceil(inputValue / increment) * increment;

            float floorDifference = Mathf.Abs(inputValue - floorDivisible);
            float ceilDifference = Mathf.Abs(inputValue - ceilDivisible);

            if (floorDifference < ceilDifference)
            {
                return floorDivisible;
            }
            else
            {
                return ceilDivisible;
            }
        }

        public static void SetTerrainScale(Transform stamp, Terrain t, int textureSize)
        {
            var scale = stamp.transform.localScale;
            scale.x = FindClosestDivisible(scale.x, t.terrainData.size.x / textureSize);
            scale.z = FindClosestDivisible(scale.z, t.terrainData.size.z / textureSize);
            if (stamp.transform.localScale != scale)
                stamp.transform.localScale = scale;
        }

        [SerializeField] int version = 0;
        public override void OnEnable()
        {
            if (heightStamp == null)
            {
                heightStamp = GetComponent<HeightStamp>();
            }
            if (heightStamp == null)
            {
                heightStamp = this.gameObject.AddComponent<HeightStamp>();
                heightStamp.falloff.filterType = FalloffFilter.FilterType.Box;
                heightStamp.mode = HeightStamp.CombineMode.Max;
                heightStamp.enabled = false;
            }

            heightStamp.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            if (stamp != null)
            {
                if (version == 0 && heightStamp.mode == HeightStamp.CombineMode.Max)
                {
                    var pos = transform.position;
                    pos.y = 0;
                    transform.position = pos;
    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(gameObject);
    #endif
                }
                else if (version == 1 && heightStamp.mode != HeightStamp.CombineMode.Override && heightStamp.mode != HeightStamp.CombineMode.Max)
                {
                    var pos = transform.position;
                    pos.y = 0;
                    transform.position = pos;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
                }

                
            }
            version = 2;
            base.OnEnable();
        }

        public override void OnDisable()
        {
            if (stamp != null)
                base.OnDisable();
        }

#if __MICROVERSE_VEGETATION__
        public bool NeedTreeClear() { return false; }
        public void ApplyTreeClear(TreeData td) { }
        public bool NeedDetailClear() { return false; }
        public void ApplyDetailClear(DetailData td) { }
        public bool UsesOtherTreeSDF() { return false; }
        public bool UsesOtherObjectSDF() { return false; }
#endif

        static Shader pasteStampShader = null;
        public void Initialize()
        {
            if (stamp != null)
            {
                if (pasteStampShader == null)
                {
                    pasteStampShader = Shader.Find("Hidden/MicroVerse/PasteSplat");
                }
                stamp.Unpack();
                if (heightStamp != null)
                {
                    heightStamp.stamp = stamp.heightMap;
                }
                splatPaste = new Material(pasteStampShader);
                _mrt = new RenderBuffer[2];
            }
            if (heightStamp != null)
                heightStamp.Initialize();

        }

        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od)
        {
            if (stamp != null && applyHeights)
            {
                var origScale = this.transform.localScale;
                if (pixelQuantization)
                    SetTerrainScale(this.transform, heightmapData.terrain, dest.width);
                stamp.heightMap.filterMode = pixelQuantization ? FilterMode.Point : FilterMode.Bilinear;
                stamp.heightMap.wrapMode = TextureWrapMode.Clamp;
                var ret = heightStamp.ApplyHeightStampAbsolute(source, dest, heightmapData, od, stamp.heightRenorm);
                if (origScale != this.transform.localScale)
                    this.transform.localScale = origScale;
                return ret;
            }
            return false;
        }

        public bool IsValidHoleStamp() {  return stamp != null && applyHoles && stamp.holeData != null && stamp.holeMap != null; }

        public void ApplyHoleStamp(RenderTexture src, RenderTexture dest, HoleData holeData, OcclusionData od)
        {
            if (IsValidHoleStamp())
            {
                var origScale = this.transform.localScale;
                if (pixelQuantization)
                    SetTerrainScale(this.transform, holeData.terrain, dest.width);
                UnityEngine.Profiling.Profiler.BeginSample("Hole Modifier");
                var holeShader = Shader.Find("Hidden/MicroVerse/HoleStamp");
                var material = new Material(holeShader);
                keywordBuilder.Clear();
                keywordBuilder.Add("_COPYPASTE");
                heightStamp.falloff.PrepareMaterial(splatPaste, transform, keywordBuilder.keywords);
                keywordBuilder.Assign(material);
                material.SetMatrix("_Transform", TerrainUtil.ComputeStampMatrix(holeData.terrain, transform));
                material.SetTexture("_PasteTex", stamp.holeMap);
                Graphics.Blit(src, dest, material);
                UnityEngine.Profiling.Profiler.EndSample();
                DestroyImmediate(material);
                this.transform.localScale = origScale;
            }
            else
            {
                RenderTexture.active = dest;
                GL.Clear(false, true, Color.white);
                RenderTexture.active = null;
            }
        }

        public bool ApplyTextureStamp(RenderTexture indexSrc, RenderTexture indexDest,
            RenderTexture weightSrc, RenderTexture weightDest, TextureData splatmapData,
            OcclusionData od)
        {
            if (applyTexturing && stamp != null && stamp.layers != null && stamp.layers.Length > 0
                && stamp.indexMap != null && stamp.weightMap != null)
            {
                var origScale = this.transform.localScale;
                if (pixelQuantization)
                    SetTerrainScale(this.transform, splatmapData.terrain, indexSrc.width);
                var terrain = splatmapData.terrain;
                int count = stamp.layers.Length;
                if (count > 32)
                {
                    Debug.LogError("Greater than 32 textures on the terrain! Will not be able to preserve area");
                    count = 32;
                }
                for (int i = 0; i < count; ++i)
                {
                    channels[i] = TerrainUtil.FindTextureChannelIndex(terrain, stamp.layers[i]);
                }
                splatPaste.SetFloatArray("_Channels", channels);
                splatPaste.SetTexture("_OrigWeightMap", weightSrc);
                splatPaste.SetTexture("_OrigIndexMap", indexSrc);
                splatPaste.SetTexture("_WeightMap", stamp.weightMap);
                splatPaste.SetTexture("_IndexMap", stamp.indexMap);
                splatPaste.SetTexture("_PlacementMask", od.terrainMask);
                keywordBuilder.Clear();
                heightStamp.falloff.PrepareMaterial(splatPaste, transform, keywordBuilder.keywords);
                keywordBuilder.Assign(splatPaste);

                splatPaste.SetMatrix("_Transform", TerrainUtil.ComputeStampMatrix(terrain, transform));


                _mrt[0] = indexDest.colorBuffer;
                _mrt[1] = weightDest.colorBuffer;

                Graphics.SetRenderTarget(_mrt, indexDest.depthBuffer);

                Graphics.Blit(null, splatPaste, 0);

                this.transform.localScale = origScale;
                return true;
                
            }
            return false;
        }
#if __MICROVERSE_VEGETATION__

        public bool NeedSDF()
        {
            return false;
        }

        public bool NeedParentSDF() { return false; }
        public bool NeedToGenerateSDFForChilden() { return false; }
        public void SetSDF(Terrain t, RenderTexture rt) { }
        public RenderTexture GetSDF(Terrain t) { return null; }


        static int _ClearLayer = Shader.PropertyToID("_ClearLayer");
        static int _ClearMask = Shader.PropertyToID("_ClearMask");
        static Shader treePasteShader = null;
        public void ApplyTreeStamp(TreeData vd, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
            if (applyTrees && stamp != null && stamp.treeData != null &&
                stamp.treeData.prototypes != null && stamp.treeData.prototypes.Length > 0 &&
                stamp.treeData.randomsTex != null && stamp.treeData.positonsTex != null)
            {
                float[] indexMap = new float[stamp.treeData.prototypes.Length];
                for (int i = 0; i < indexMap.Length; ++i)
                {
                    indexMap[i] = TerrainUtil.FindTreeIndex(vd.terrain, stamp.treeData.prototypes[i].prefab);
                }

                keywordBuilder.Clear();
                // first we have to fix the data to have new height values
                if (treePasteShader == null)
                {
                    treePasteShader = Shader.Find("Hidden/MicroVerse/TreePasteStamp");
                }
                Material mat = new Material(treePasteShader);
                mat.SetTexture("_TreePos", stamp.treeData.positonsTex);
                mat.SetTexture("_TreeRand", stamp.treeData.randomsTex);
                mat.SetFloatArray("_Indexes", indexMap);
                mat.SetTexture("_Heightmap", vd.heightMap);
                mat.SetTexture("_PlacementMask", od.terrainMask);
                heightStamp.falloff.PrepareMaterial(mat, transform, keywordBuilder.keywords);
                var terrainData = vd.terrain.terrainData;
                mat.SetMatrix("_Transform", TerrainUtil.ComputeStampMatrix(vd.terrain, transform)); ;
                mat.SetVector("_RealSize", TerrainUtil.ComputeTerrainSize(vd.terrain));
                mat.SetFloat(_ClearLayer, vd.layerIndex);
                mat.SetTexture(_ClearMask, vd.treeClearMap);
                mat.SetMatrix("_StampTransform", transform.localToWorldMatrix);
                mat.SetMatrix("_TerrainTransform", vd.terrain.transform.worldToLocalMatrix);
                // TODO: This makes no sense why this is needed, but it is?
                float r = 1;
                if (terrainData.size.y > terrainData.size.x)
                {
                    r = terrainData.size.y / terrainData.size.x;
                }
                mat.SetFloat("_ScaleR", r);
                keywordBuilder.Assign(mat);
                RenderTexture posRT = new RenderTexture(stamp.treeData.dataSize.x, stamp.treeData.dataSize.y, 0, RenderTextureFormat.ARGBHalf);
                RenderTexture randRT = new RenderTexture(stamp.treeData.dataSize.x, stamp.treeData.dataSize.y, 0, RenderTextureFormat.ARGBHalf);
                _mrt[0] = posRT.colorBuffer;
                _mrt[1] = randRT.colorBuffer;
                Graphics.SetRenderTarget(_mrt, posRT.depthBuffer);

                Graphics.Blit(null, mat, 0);

                TreeUtil.ApplyOcclusion(posRT, od, true, false);


                // setup job to unpack
                var jholder = new TreeJobHolder();

                NativeArray<int> indexes = new NativeArray<int>(indexMap.Length, Allocator.Persistent);
                for (int i = 0; i < indexMap.Length; ++i)
                {
                    indexes[i] = (int)indexMap[i];
                }
                // find prototype index on actual terrain
                jholder.AddJob(posRT, randRT, indexes);
                if (jobs.ContainsKey(vd.terrain))
                {
                    jobs[vd.terrain].Add(jholder);
                }
                else
                {
                    jobs.Add(vd.terrain, new List<TreeJobHolder>() { jholder });
                }

            }
        }

        public void ProcessTreeStamp(TreeData vd, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
        }

        static Shader detailPasteShader = null;
        public void ApplyDetailStamp(DetailData dd, Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers, OcclusionData od)
        {
            if (applyDetails && stamp != null && stamp.detailData != null &&
                stamp.detailData.layers != null)
            {
                var data = stamp.detailData;
                foreach (var l in data.layers)
                {
                    if (!l.prototype.IsValid())
                        continue;

                    int detailIndex = VegetationUtilities.FindDetailIndex(od.terrain, l.prototype);
                    keywordBuilder.Clear();
                    if (detailPasteShader == null)
                    {
                        detailPasteShader = Shader.Find("Hidden/MicroVerse/DetailPasteStamp");
                    }
                    Material mat = new Material(detailPasteShader);
                    mat.SetFloat("_Weight", 1);
                    mat.SetTexture(_ClearMask, dd.clearMap);
                    mat.SetFloat(_ClearLayer, dd.layerIndex);
                    mat.SetTexture("_PlacementMask", od.terrainMask);
                    heightStamp.falloff.PrepareMaterial(mat, transform, keywordBuilder.keywords);
                    var terrainData = dd.terrain.terrainData;
                    mat.SetMatrix("_Transform", TerrainUtil.ComputeStampMatrix(dd.terrain, transform)); ;
                    
                    RenderTexture rt = RenderTexture.GetTemporary(dd.terrain.terrainData.detailWidth, dd.terrain.terrainData.detailHeight, 0,
                    UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
                    rt.name = "DetailStamp::rt";
                    

                    keywordBuilder.Assign(mat);

                    Graphics.Blit(l.texture, rt, mat);
                    if (!resultBuffers.ContainsKey(dd.terrain))
                        resultBuffers.Add(dd.terrain, new Dictionary<int, List<RenderTexture>>());
                    var dbuffer = resultBuffers[dd.terrain];

                    if (dbuffer.ContainsKey(detailIndex))
                    {
                        dbuffer[detailIndex].Add(rt);
                    }
                    else
                    {
                        dbuffer.Add(detailIndex, new List<RenderTexture>(1) { rt });
                    }

                }
            }
        }

        public void InqDetailPrototypes(List<DetailPrototypeSerializable> prototypes)
        {
            if (applyDetails && stamp != null && stamp.detailData != null &&
                stamp.detailData.layers != null)
            {
                foreach (var layer in stamp.detailData.layers)
                {
                    if (layer.prototype != null && (layer.prototype.prototype != null || layer.prototype.prototypeTexture != null))
                    {
                        if (!prototypes.Contains(layer.prototype))
                            prototypes.Add(layer.prototype);
                    }    
                }
            }
        }

        
        public void InqTreePrototypes(List<TreePrototypeSerializable> prototypes)
        {
            if (applyTrees && stamp != null && stamp.treeData != null && stamp.treeData.prototypes != null &&
                     stamp.treeData.prototypes.Length > 0)
            {
                prototypes.AddRange(stamp.treeData.prototypes);
            }
        }

        public bool OccludesOthers()
        {
            return true;
        }
#endif

        public void InqTerrainLayers(Terrain terrain, List<TerrainLayer> prototypes)
        {
            if (applyTexturing && stamp != null && stamp.layers != null && stamp.indexMap != null && stamp.weightMap != null)
            {
                if (TerrainUtil.ComputeTerrainBounds(terrain).Intersects(GetBounds()))
                {
                    foreach (var l in stamp.layers)
                    {
                        if (l != null)
                        {
                            prototypes.AddRange(stamp.layers);
                        }
                    }
                }
            }
        }

        public bool NeedCurvatureMap()
        {
            return false;
        }

        public bool NeedFlowMap() { return false; }




        public void Dispose()
        {
            if (heightStamp != null)
            {
                heightStamp.Dispose();
            }
            DestroyImmediate(splatPaste);
            _mrt = null;
        }

        public override Bounds GetBounds()
        {
#if __MICROVERSE_SPLINES__
            if (heightStamp != null && heightStamp.falloff.filterType == FalloffFilter.FilterType.SplineArea && heightStamp.falloff.splineArea != null)
            {
                return heightStamp.falloff.splineArea.GetBounds();
            }
#endif
            if (heightStamp != null && heightStamp.falloff.paintArea != null && heightStamp.falloff.paintArea.clampOutsideOfBounds)
            {
                return heightStamp.falloff.paintArea.GetBounds();
            }
            return TerrainUtil.GetBounds(transform);
        }

        void OnDrawGizmosSelected()
        {
            if (MicroVerse.instance != null)
            {
               
                if (heightStamp.falloff.filterType != FalloffFilter.FilterType.Global &&
                heightStamp.falloff.filterType != FalloffFilter.FilterType.SplineArea)
                {
                    Gizmos.color = MicroVerse.instance.options.colors.copyStampColor;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
                }
            }
        }

#if UNITY_EDITOR
        public override void OnMoved()
        {
            if (stamp == null)
                return;
            base.OnMoved();
        }

#endif
    }
}
