using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [ExecuteInEditMode]
    public class OcclusionStamp : Stamp, IHeightModifier, ITextureModifier
#if __MICROVERSE_VEGETATION__
        , ITreeModifier, IDetailModifier
#endif
#if __MICROVERSE_OBJECTS__
        , IObjectModifier
#endif
    {
        [Tooltip("How much to prevent future height stamps in the hierarchy from affecting this area")]
        [Range(0, 1)] public float occludeHeightWeight;
        [Tooltip("How much to prevent future texture stamps in the hierarchy from affecting this area")]
        [Range(0, 1)] public float occludeTextureWeight;
        [Tooltip("How much to prevent future tree stamps in the hierarchy from affecting this area")]
        [Range(0, 1)] public float occludeTreeWeight;
        [Tooltip("How much to prevent future detail stamps in the hierarchy from affecting this area")]
        [Range(0, 1)] public float occludeDetailWeight;
        [Tooltip("How much to prevent future objects from affecting this area")]
        [Range(0, 1)] public float occludeObjectWeight;

        public FilterSet filterSet = new FilterSet();

        Material material;
        static Shader occlusionShader = null;
        public void Initialize()
        {
            if (occlusionShader == null)
            {
                occlusionShader = Shader.Find("Hidden/MicroVerse/OccludeLayer");
            }
            if (material == null)
            {
                material = new Material(occlusionShader);
            }
            keywordBuilder.ClearInitial();
            filterSet.PrepareMaterial(this.transform, material, keywordBuilder.initialKeywords);
        }

#if __MICROVERSE_VEGETATION__
        public bool NeedTreeClear() { return false; }
        public void ApplyTreeClear(TreeData td) { }
        public bool NeedDetailClear() { return false; }
        public void ApplyDetailClear(DetailData td) { }
        public bool UsesOtherTreeSDF() { return false; }
        public bool UsesOtherObjectSDF() { return false; }
#endif

        public override FilterSet GetFilterSet()
        {
            return filterSet;
        }

        void PrepareMaterial(Material material, OcclusionData od, List<string> keywords)
        {
            
            material.SetMatrix("_Transform", TerrainUtil.ComputeStampMatrix(od.terrain, transform)); ;
            material.SetVector("_RealSize", TerrainUtil.ComputeTerrainSize(od.terrain));

            keywordBuilder.Add("_RECONSTRUCTNORMAL");
            filterSet.PrepareTransform(this.transform, od.terrain, material, keywords, GetTerrainScalingFactor(od.terrain));
        }

        void Render(OcclusionData od)
        {
            RenderTexture temp = RenderTexture.GetTemporary(od.terrainMask.descriptor);
            temp.name = "Occlusion::Render::Temp";
            material.SetTexture("_MainTex", od.terrainMask);
            Graphics.Blit(od.terrainMask, temp, material);
            RenderTexture.ReleaseTemporary(od.terrainMask);
            od.terrainMask = temp;
        }
        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od)
        {
            // we don't modify the heightmaps, rather the occlusion maps, so always return
            // false so buffers aren't swapped.

            if (occludeHeightWeight <= 0)
            {
                return false;
            }
            keywordBuilder.Clear();
            PrepareMaterial(material, od, keywordBuilder.keywords);
            filterSet.PrepareMaterial(transform, material, keywordBuilder.keywords);
            keywordBuilder.Assign(material);
            material.SetVector("_Mask", new Vector4(occludeHeightWeight, 0, 0, 0));
            Render(od);
            return false;
        }

        public bool ApplyTextureStamp(RenderTexture indexSrc, RenderTexture indexDest, RenderTexture weightSrc, RenderTexture weightDest,
            TextureData splatmapData, OcclusionData od)
        {
            if (occludeTextureWeight <= 0)
            {
                return false;
            }
            keywordBuilder.Clear();
            keywordBuilder.Add("_ISSPLAT");
            filterSet.PrepareMaterial(transform, material, keywordBuilder.keywords);
            PrepareMaterial(material, od, keywordBuilder.keywords);
            keywordBuilder.Assign(material);
            // we don't render into the occlusion mask, because splat layers
            // render in reverse order, so instead of lower the weights of the
            // layers before us. 
            material.SetVector("_Mask", new Vector4(0, occludeTextureWeight, 0, 0));
            material.SetTexture("_MainTex", weightSrc);
            Graphics.Blit(weightSrc, weightDest, material);
            Graphics.Blit(indexSrc, indexDest);
            return true;

        }

#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
        public bool OccludesOthers() { return true; }
        public bool NeedSDF() { return false; }


        static int _Heightmap = Shader.PropertyToID("_Heightmap");
        static int _Normalmap = Shader.PropertyToID("_Normalmap");
        static int _Curvemap = Shader.PropertyToID("_Curvemap");
        static int _Flowmap = Shader.PropertyToID("_Flowmap");
        static int _IndexMap = Shader.PropertyToID("_IndexMap");
        static int _WeightMap = Shader.PropertyToID("_WeightMap");
#endif

#if __MICROVERSE_VEGETATION__
        public void ApplyTreeStamp(TreeData vd, Dictionary<Terrain, List<TreeJobHolder>> jobs,
            OcclusionData od)
        {
            if (occludeTreeWeight <= 0)
            {
                return;
            }

            keywordBuilder.Clear();
            PrepareMaterial(material, od, keywordBuilder.keywords);
            filterSet.PrepareMaterial(transform, material, keywordBuilder.keywords);

            var textureLayerWeights = filterSet.GetTextureWeights(vd.terrain.terrainData.terrainLayers);
            material.SetVectorArray("_TextureLayerWeights", textureLayerWeights);
            material.SetTexture(_Heightmap, vd.heightMap);
            material.SetTexture(_Normalmap, vd.normalMap);
            material.SetTexture(_Curvemap, vd.curveMap);
            material.SetTexture(_Flowmap, vd.flowMap);
            material.SetTexture(_IndexMap, vd.dataCache.indexMaps[vd.terrain]);
            material.SetTexture(_WeightMap, vd.dataCache.weightMaps[vd.terrain]);

            keywordBuilder.Assign(material);
            


            keywordBuilder.Assign(material);

            material.SetVector("_Mask", new Vector4(0, 0, occludeTreeWeight, 0));
            Render(od);
        }

        public void ProcessTreeStamp(TreeData vd, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
            
        }

        
        public bool NeedParentSDF() { return false; }
        public bool NeedToGenerateSDFForChilden() { return false;  }
        public void SetSDF(Terrain t, RenderTexture rt) { }
        public RenderTexture GetSDF(Terrain t) { return null; }




        public void ApplyDetailStamp(DetailData dd, Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers,
            OcclusionData od)
        {
            if (occludeDetailWeight <= 0)
            {
                return;
            }
            keywordBuilder.Clear();
            PrepareMaterial(material, od, keywordBuilder.keywords);
            filterSet.PrepareMaterial(transform, material, keywordBuilder.keywords);
            keywordBuilder.Assign(material);
            var textureLayerWeights = filterSet.GetTextureWeights(dd.terrain.terrainData.terrainLayers);
            material.SetVectorArray("_TextureLayerWeights", textureLayerWeights);
            material.SetTexture(_Heightmap, dd.heightMap);
            material.SetTexture(_Normalmap, dd.normalMap);
            material.SetTexture(_Curvemap, dd.curveMap);
            material.SetTexture(_Flowmap, dd.flowMap);
            material.SetTexture(_IndexMap, dd.dataCache.indexMaps[dd.terrain]);
            material.SetTexture(_WeightMap, dd.dataCache.weightMaps[dd.terrain]);
            material.SetVector("_Mask", new Vector4(0, 0, 0, occludeDetailWeight));
            Render(od);
        }

        public void InqTreePrototypes(List<TreePrototypeSerializable> prototypes) { }
        public void InqDetailPrototypes(List<DetailPrototypeSerializable> prototypes) { }
#endif

        public void InqTerrainLayers(Terrain terrain, List<TerrainLayer> prototypes) { }
        public bool NeedCurvatureMap() { return filterSet.NeedCurvatureMap(); }
        public bool NeedFlowMap() { return filterSet.NeedFlowMap(); }

        public void Dispose()
        {
            
        }

        protected override void OnDestroy()
        {
            if (material != null) DestroyImmediate(material);
            base.OnDestroy();
        }

        public override Bounds GetBounds()
        {
            FalloffOverride fo = GetComponentInParent<FalloffOverride>();
            var foType = filterSet.falloffFilter.filterType;
            var foFilter = filterSet.falloffFilter;
            if (fo != null && fo.enabled)
            {
                foType = fo.filter.filterType;
                foFilter = fo.filter;
            }
#if __MICROVERSE_SPLINES__
            if (foType == FalloffFilter.FilterType.SplineArea && foFilter.splineArea != null)
            {
                return foFilter.splineArea.GetBounds();
            }
#endif

            if (foType == FalloffFilter.FilterType.Global && foFilter.paintArea != null && foFilter.paintArea.clampOutsideOfBounds)
            {
                return foFilter.paintArea.GetBounds();
            }

            if (foType == FalloffFilter.FilterType.Global)
                return new Bounds(Vector3.zero, new Vector3(99999, 999999, 99999));
            else
            {
                return TerrainUtil.GetBounds(transform);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (MicroVerse.instance != null)
            {
                if (filterSet.falloffFilter.filterType != FalloffFilter.FilterType.Global &&
                filterSet.falloffFilter.filterType != FalloffFilter.FilterType.SplineArea)
                {
                    Gizmos.color = MicroVerse.instance.options.colors.occluderStampColor;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
                }
            }
        }
#if __MICROVERSE_OBJECTS__
        public void ApplyObjectStamp(ObjectData td, Dictionary<Terrain, List<ObjectJobHolder>> jobs, OcclusionData od)
        {
            if (occludeObjectWeight <= 0)
            {
                return;
            }
            keywordBuilder.Clear();
            PrepareMaterial(material, od, keywordBuilder.keywords);
            filterSet.PrepareMaterial(transform, material, keywordBuilder.keywords);
            keywordBuilder.Assign(material);

            var textureLayerWeights = filterSet.GetTextureWeights(td.terrain.terrainData.terrainLayers);
            material.SetVectorArray("_TextureLayerWeights", textureLayerWeights);
            material.SetTexture(_Heightmap, td.heightMap);
            material.SetTexture(_Normalmap, td.normalMap);
            material.SetTexture(_Curvemap, td.curveMap);
            material.SetTexture(_Flowmap, td.flowMap);
            material.SetTexture(_IndexMap, td.indexMap);
            material.SetTexture(_WeightMap, td.weightMap);

            material.SetVector("_Mask", new Vector4(occludeObjectWeight, 0, 0, 0));
            RenderTexture temp = RenderTexture.GetTemporary(od.objectMask.descriptor);
            temp.name = "Occlusion::Render::ObjectMaskTemp";
            material.SetTexture("_MainTex", od.objectMask);
            Graphics.Blit(od.objectMask, temp, material);
            RenderTexture.ReleaseTemporary(od.objectMask);
            od.objectMask = temp;
        }

        public void ProcessObjectStamp(ObjectData td, Dictionary<Terrain, List<ObjectJobHolder>> jobs, OcclusionData od)
        {
            
        }

        public void ApplyObjectClear(ObjectData td)
        {
            
        }

        public bool NeedObjectClear()
        {
            return false;
        }
#endif
    }
}