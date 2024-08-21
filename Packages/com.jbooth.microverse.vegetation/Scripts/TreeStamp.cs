using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;

namespace JBooth.MicroVerseCore
{
    public class TreeUtil
    {
        static ComputeShader occlusionShader = null;
        static int _Result = Shader.PropertyToID("_Result");
        static int _Positions = Shader.PropertyToID("_Positions");
        static int _Result_Width = Shader.PropertyToID("_Result_Width");
        static int _Result_Height = Shader.PropertyToID("_Result_Height");

        public static void ApplyOcclusion(RenderTexture positions, OcclusionData od, bool others, bool selfSDF)
        {
            if (others == false && selfSDF == false)
                return;
            Profiler.BeginSample("Apply Tree Occlusion");
            if (occlusionShader == null)
            {
                occlusionShader = (ComputeShader)Resources.Load("MicroVersePositionToOcclusionMask");
            }
            occlusionShader.DisableKeyword("_R8");
            int kernelHandle = occlusionShader.FindKernel("CSMain");
            occlusionShader.SetTexture(kernelHandle, _Result, od.terrainMask);
            occlusionShader.SetTexture(kernelHandle, _Positions, positions);
            occlusionShader.SetInt(_Result_Width, od.terrainMask.width);
            occlusionShader.SetInt(_Result_Height, od.terrainMask.height);
            if (others)
            {
                occlusionShader.Dispatch(kernelHandle, Mathf.CeilToInt(positions.width/512), positions.height, 1);
            }

            if (others || selfSDF)
            {
                occlusionShader.EnableKeyword("_R8");
                if (od.currentTreeMask == null)
                {
                    var desc = od.terrainMask.descriptor;
                    desc.colorFormat = RenderTextureFormat.R8;
                    od.currentTreeMask = RenderTexture.GetTemporary(desc);
                    RenderTexture.active = od.currentTreeMask;
                    GL.Clear(false, true, Color.clear);
                    od.currentTreeMask.name = "Occlusion::CurrentTreeMask";
                }
                RenderTexture.active = od.currentTreeMask;
                GL.Clear(false, true, Color.clear);
                occlusionShader.SetTexture(kernelHandle, _Result, od.currentTreeMask);
                occlusionShader.Dispatch(kernelHandle, Mathf.CeilToInt(positions.width / 256.0f), positions.height, 1);
            }
            Profiler.EndSample();
        }
    }

    [BurstCompile]
    public struct UnpackTreeInstanceJob : IJob
    {
        public NativeArray<int> count;
        [WriteOnly] public NativeArray<TreeInstance> trees;
        [ReadOnly] public NativeArray<half4> placementData;
        [ReadOnly] public NativeArray<half4> randomData;
        [ReadOnly] public NativeArray<int> treeIndexes;


        public void Execute()
        {
            for (int i = 0; i < placementData.Length; ++i)
            {
                half4 pd = placementData[i];
                if (pd.w > 0)
                {
                    var tree = new TreeInstance();
                    half4 rd = randomData[i];
                    tree.position = new Vector3(pd.x, pd.y * 2, pd.z);
                    tree.color = Color.white;

                    tree.lightmapColor = Color.white;
                    
                    tree.prototypeIndex = treeIndexes[(int)rd.x % treeIndexes.Length];
                    tree.heightScale = rd.y;
                    tree.widthScale = rd.z;
                    tree.rotation = rd.w;
                    trees[count[0]] = tree;
                    count[0] = count[0] + 1;
                }
            }
        }
    }

    public class TreeJobHolder
    {
        public UnpackTreeInstanceJob job;
        public JobHandle handle;
        public NativeArray<half4> placementData;
        public NativeArray<half4> randomData;

        private RenderTexture filteredInstances;
        private RenderTexture randomResults;

        private AsyncGPUReadbackRequest gpuRequestPlacement;
        private AsyncGPUReadbackRequest gpuRequestRandoms;
        private NativeArray<int> treeIndexes;


        public bool IsDone()
        {
            if (MicroVerse.noAsyncReadback)
            {
                handle.Complete();
            }
            return (gpuRequestPlacement.done && gpuRequestRandoms.done && handle.IsCompleted);
        }

        public bool canceled { get; set; }

        public void Cleanup()
        {
            handle.Complete();
            if (placementData.IsCreated)
                placementData.Dispose();
            if (randomData.IsCreated)
                randomData.Dispose();
            if (treeIndexes.IsCreated)
                treeIndexes.Dispose();
            if (job.count.IsCreated)
                job.count.Dispose();
            if (job.trees.IsCreated)
                job.trees.Dispose();

        }

        public void Dispose()
        {
            Cleanup();
        }

        void LaunchJob()
        {
            job = new UnpackTreeInstanceJob()
            {
                placementData = placementData,
                randomData = randomData,
                count = new NativeArray<int>(1, Allocator.TempJob),
                trees = new NativeArray<TreeInstance>(placementData.Length, Allocator.TempJob),
                treeIndexes = treeIndexes,
            };

            handle = job.Schedule();
        }

        private void OnAsyncCompletePositions(AsyncGPUReadbackRequest obj)
        {
            RenderTexture.active = null;
            if (filteredInstances != null)
            {
                RenderTexture.active = null;
                Object.DestroyImmediate(filteredInstances);
            }
            filteredInstances = null;
            if (randomResults == null)
            {
                if (canceled)
                {
                    if (placementData.IsCreated)
                        placementData.Dispose();
                    if (randomData.IsCreated)
                        randomData.Dispose();
                    if (treeIndexes.IsCreated)
                        treeIndexes.Dispose();
                }
                else
                {
                    LaunchJob();
                }
            }
        }

        private void OnAsyncCompleteRandoms(AsyncGPUReadbackRequest obj)
        {
            RenderTexture.active = null;
            if (randomResults != null)
                Object.DestroyImmediate(randomResults);

            randomResults = null;

            if (filteredInstances == null)
            {
                if (canceled)
                {
                    if (placementData.IsCreated)
                        placementData.Dispose();
                    if (randomData.IsCreated)
                        randomData.Dispose();
                    if (treeIndexes.IsCreated)
                        treeIndexes.Dispose();
                }
                else
                {
                    LaunchJob();
                }
            }
        }

        public void AddJob(RenderTexture filteredInstances, RenderTexture randomResults, NativeArray<int> treeIndexes)
        {
            this.treeIndexes = treeIndexes;
            this.filteredInstances = filteredInstances;
            this.randomResults = randomResults;
            
            if (MicroVerse.noAsyncReadback)
            {
                Texture2D place = new Texture2D(filteredInstances.width, filteredInstances.height, TextureFormat.RGBAHalf, false, true);
                Texture2D random = new Texture2D(filteredInstances.width, filteredInstances.height, TextureFormat.RGBAHalf, false, true);
                RenderTexture.active = filteredInstances;
                place.ReadPixels(new Rect(0, 0, place.width, place.height), 0, 0);
                place.Apply();
                placementData = place.GetRawTextureData<half4>();
                RenderTexture.active = randomResults;
                random.ReadPixels(new Rect(0, 0, randomResults.width, randomResults.height), 0, 0);
                random.Apply();
                randomData = random.GetRawTextureData<half4>();
                LaunchJob();
                Object.DestroyImmediate(place);
                Object.DestroyImmediate(random);
            }
            else
            {
                this.placementData = new NativeArray<half4>(filteredInstances.width * filteredInstances.height, Allocator.Persistent);
                this.randomData = new NativeArray<half4>(filteredInstances.width * filteredInstances.height, Allocator.Persistent);

                this.gpuRequestPlacement = AsyncGPUReadback.RequestIntoNativeArray<half4>(ref placementData, filteredInstances, 0, OnAsyncCompletePositions);
                this.gpuRequestRandoms = AsyncGPUReadback.RequestIntoNativeArray<half4>(ref randomData, randomResults, 0, OnAsyncCompleteRandoms);
            }
        }


    }

    [ExecuteAlways]
    public class TreeStamp : Stamp, ITreeModifier, ITextureModifier
    {
        public override FilterSet GetFilterSet()
        {
            return filterSet;
        }

        // pos.xyz, weight
        // index, scale.xy, rot

        [System.Serializable]
        public struct Randomization
        {
            public float weight;
            public Vector2 scaleHeightRange;
            public Vector2 scaleWidthRange;
            public float sink;
            public float scaleMultiplierAtBoundaries;
            public Vector2 weightRange;
            public int flags;
            public bool lockScaleWidthHeight { get { return (flags & (1 << 1)) != 0; } set { if (value) flags |= 1 << 1; else flags &= ~(1 << 1); } }
            public bool randomRotation { get { return (flags & (1 << 2)) == 0; } set { if (!value) flags |= 1 << 2; else flags &= ~(1 << 2); } }
            public bool densityByWeight { get { return (flags & (1 << 3)) == 0; } set { if (!value) flags |= 1 << 3; else flags &= ~(1 << 3); } }
            public bool disabled { get { return (flags & (1 << 4)) == 0; } set { if (!value) flags |= 1 << 4; else flags &= ~(1 << 4); } }
            public bool mapHeightFilterToScale { get { return (flags & (1 << 5)) != 0; } set { if (value) flags |= 1 << 5; else flags &= ~(1 << 5); } }
            public bool mapWeightToScale { get { return (flags & (1 << 6)) != 0; } set { if (value) flags |= 1 << 6; else flags &= ~(1 << 6); } }
            public bool randomScale { get { return (flags & (1 << 7)) == 0; } set { if (!value) flags |= 1 << 7; else flags &= ~(1 << 7); } }
        }

        public int version = 0;
        static Texture2D randomTexture;

        // these arrays must be kept in sync. Would like to contain them as one,
        // but one needs pointers to objects and the other needs to be a struct
        // for jobs.

        public List<TreePrototypeSerializable> prototypes = new List<TreePrototypeSerializable>();
        public List<Randomization> randomizations = new List<Randomization>();

        public uint seed = 0;
        
        public Texture2D poissonDisk;
        [Range(0,2)]
        public float poissonDiskStrength = 1;
        [Range(0.1f, 20)]
        public float density = 1;

        [Tooltip("Write into occlusion system so other things won't spawn on top of us")]
        public bool occludeOthers = true;
        [Tooltip("Read occlusion system so we won't spawn where we're not supposed to")]
        public bool occludedByOthers = true;

        public float minDistanceFromTree = 0;
        public float maxDistanceFromTree = 0;
        public float minDistanceFromObject = 0;
        public float maxDistanceFromObject = 0;
        public float minDistanceFromParent = 0;
        public float maxDistanceFromParent = 0;
        public bool sdfClamp;


        [Tooltip("Minimum height to place tree - this lets you spawn objects on water, for instance")]
        public float minHeight = -99999;
        [Tooltip("Allows to to raise or lower the terrain around tree objects")]
        [Range(-3, 3)]
        public float heightModAmount = 0;
        [Tooltip("Controls the width of the height adjustment")]
        [Range(0.1f, 20)]
        public float heightModWidth = 5;
        [Tooltip("Texture to apply")]
        public TerrainLayer layer;
        [Tooltip("Weight of texture to apply")]
        [Range(0,1)]
        public float layerWeight = 0;
        [Tooltip("Controls the width of the texturing")]
        [Range(0.1f, 20)]
        public float layerWidth = 5;

        [Tooltip("Applies the slope filter from the stamp to the height/texture mods, so they don't go out over cliffs")]
        public bool applyFilteringToTextureMod = false;

        public FilterSet filterSet = new FilterSet();

        Vector4[] textureLayerWeights;

        Material material;
        RenderBuffer[] _mrt;

        public override void OnEnable()
        {
            base.OnEnable();
            Revision();
        }

        void Revision()
        {
            if (version == 0)
            {
                version = 1;
                for (int i = 0; i < randomizations.Count; ++i)
                {
                    var rand = randomizations[i];
                    rand.disabled = false;
                    randomizations[i] = rand;
                }
            }
        }

        public bool NeedCurvatureMap() { return filterSet.NeedCurvatureMap(); }
        public bool NeedFlowMap() { return filterSet.NeedFlowMap(); }

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

        public bool OccludesOthers()
        {
            return occludeOthers;
        }

        public bool UsesOtherTreeSDF() { return minDistanceFromTree > 0 || maxDistanceFromTree > 0; }
        public bool UsesOtherObjectSDF() { return minDistanceFromObject > 0 || maxDistanceFromObject > 0; }

        // Do I need an sdf for myself
        public bool NeedSDF()
        {
            return heightModAmount > 0 || (layer != null && layerWeight > 0);
        }

        // do I need my parent to generate an SDF is parented
        public bool NeedParentSDF()
        {
            return minDistanceFromParent > 0 || maxDistanceFromParent > 0;
        }

        // Do I need to generate an SDF for subspawners
        public bool NeedToGenerateSDFForChilden()
        {
            var me = GetComponent<ISpawner>();
            var subs = GetComponentsInChildren<ISpawner>(false);
            foreach (var s in subs)
            {
                if (s != me && s.NeedParentSDF()) return true;
            }
            return false;
        }

        Dictionary<Terrain, RenderTexture> sdfs = new Dictionary<Terrain, RenderTexture>();
        public void SetSDF(Terrain t, RenderTexture rt)
        {
            if (sdfs.ContainsKey(t))
            {
                Debug.LogError("Stamp " + this.name + " already generated sdf for " + t.name);
            }
            sdfs[t] = rt;
        }
        public RenderTexture GetSDF(Terrain t)
        {
            if (sdfs.ContainsKey(t))
                return sdfs[t];
            return null;
        }

        ComputeBuffer randomizationBuffer;

        static Shader treeStampShader = null;
        public void Initialize()
        {
            if (treeStampShader == null)
            {
                treeStampShader = Shader.Find("Hidden/MicroVerse/VegetationFilter");
            }
            prototypeMappings.Clear();
            if (material == null)
            {
                material = new Material(treeStampShader);
            }
            if (_mrt == null)
                _mrt = new RenderBuffer[2];

            if (randomTexture == null)
            {
                randomTexture = new Texture2D(64, 64, TextureFormat.RGBAHalf, false, true);
                randomTexture.filterMode = FilterMode.Point;
                var data = randomTexture.GetRawTextureData<half4>();
                Unity.Mathematics.Random rand = new Unity.Mathematics.Random(31);
                rand.InitState(31);
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] = (half4)rand.NextFloat4(0, 1);
                }
                randomTexture.Apply(false, false);

            }

            NativeArray<Randomization> randoms = new NativeArray<Randomization>(randomizations.Count, Allocator.Temp);
            randoms.CopyFrom(randomizations.ToArray());
            randomizationBuffer = new ComputeBuffer(randomizations.Count, UnsafeUtility.SizeOf<Randomization>());
            randomizationBuffer.SetData(randoms);
            randoms.Dispose();
            
            material.SetTexture(_RandomTex, randomTexture);
            material.SetTexture(_Disc, poissonDisk);
            material.SetFloat(_DiscStrength, poissonDiskStrength);
            material.SetFloat(_MinHeight, minHeight);
            material.SetFloat(_NumTreeIndexes, prototypes.Count);
            material.SetFloat(_HeightOffset, heightModAmount);
            material.SetFloat(_ModWidth, Mathf.Max(layerWeight, heightModWidth));
            material.SetFloat(_Seed, seed);
            material.SetBuffer(_Randomizations, randomizationBuffer);

            if (poissonDisk != null)
            {
                poissonDisk.wrapMode = TextureWrapMode.Repeat;
                poissonDisk.filterMode = FilterMode.Point;
            }
            if (prototypes != null && prototypes.Count != 0 && (prototypeIndexes == null || prototypeIndexes.Length != prototypes.Count))
                prototypeIndexes = new int[prototypes.Count];

            keywordBuilder.ClearInitial();
            filterSet.PrepareMaterial(this.transform, material, keywordBuilder.initialKeywords);
        }

        

        public void InqTreePrototypes(List<TreePrototypeSerializable> trees)
        {
            trees.AddRange(prototypes);
        }

        public bool NeedTreeClear() { return false; }
        public void ApplyTreeClear(TreeData td) { }
        public bool NeedDetailClear() { return false; }
        public void ApplyDetailClear(DetailData td) { }

        int[] prototypeIndexes;

        Dictionary<Terrain, RenderTexture> posWeightRTs = new Dictionary<Terrain, RenderTexture>();
        Dictionary<Terrain, RenderTexture> randomsRTs = new Dictionary<Terrain, RenderTexture>();

        static int _RandomTex = Shader.PropertyToID("_RandomTex");
        static int _Disc = Shader.PropertyToID("_Disc");
        static int _DiscStrength = Shader.PropertyToID("_DiscStrength");
        static int _Density = Shader.PropertyToID("_Density");
        static int _InstanceCount = Shader.PropertyToID("_InstanceCount");
        static int _Heightmap = Shader.PropertyToID("_Heightmap");
        static int _Normalmap = Shader.PropertyToID("_Normalmap");
        static int _Curvemap = Shader.PropertyToID("_Curvemap");
        static int _Flowmap = Shader.PropertyToID("_Flowmap");
        static int _ClearLayer = Shader.PropertyToID("_ClearLayer");
        static int _ClearMask = Shader.PropertyToID("_ClearMask");
        static int _MinHeight = Shader.PropertyToID("_MinHeight");
        static int _NumTreeIndexes = Shader.PropertyToID("_NumTreeIndexes");
        static int _TotalWeights = Shader.PropertyToID("_TotalWeights");
        static int _HeightOffset = Shader.PropertyToID("_HeightOffset");
        static int _PlacementMask = Shader.PropertyToID("_PlacementMask");

        static int _TerrainPixelCount = Shader.PropertyToID("_TerrainPixelCount");
        static int _ModWidth = Shader.PropertyToID("_ModWidth");
        static int _IndexMap = Shader.PropertyToID("_IndexMap");
        static int _WeightMap = Shader.PropertyToID("_WeightMap");
        static int _Seed = Shader.PropertyToID("_Seed");
        static int _TextureLayerWeights = Shader.PropertyToID("_TextureLayerWeights");
        static int _Randomizations = Shader.PropertyToID("_Randomizations");
        static int _YCount = Shader.PropertyToID("_YCount");

        Dictionary<Terrain, int[]> prototypeMappings = new Dictionary<Terrain, int[]>();
        public void ApplyTreeStamp(TreeData td, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
            if (poissonDisk == null)
                return;
            if (prototypes.Count == 0)
                return;

            Profiler.BeginSample("Apply Tree Stamp");
            textureLayerWeights = filterSet.GetTextureWeights(od.terrain.terrainData.terrainLayers);
            
            float totalWeight = 0;
            for (int i = 0; i < prototypes.Count; ++i)
            {
                prototypeIndexes[i] = VegetationUtilities.FindTreeIndex(od.terrain, prototypes[i]);
                totalWeight += randomizations[i].weight + 1;
            }
            prototypeMappings.Add(od.terrain, prototypeIndexes);
            
            keywordBuilder.Clear();
            
            

            material.SetFloat(_ClearLayer, td.layerIndex);
            material.SetTexture(_ClearMask, td.treeClearMap);
            material.SetTexture(_Heightmap, td.heightMap);
            material.SetTexture(_Normalmap, td.normalMap);
            material.SetTexture(_Curvemap, td.curveMap);
            material.SetTexture(_Flowmap, td.flowMap);
            material.SetVectorArray(_TextureLayerWeights, textureLayerWeights);
            material.SetFloat(_TotalWeights, totalWeight);

            if (occludedByOthers)
            {
                material.SetTexture(_PlacementMask, od.terrainMask);
            }
            else
            {
                material.SetTexture(_PlacementMask, null);
            }
            float ratio = td.heightMap.width / td.terrain.terrainData.size.x;
            FilterSet.PrepareSDFFilter(keywordBuilder, material, transform, od, ratio, sdfClamp,
                minDistanceFromTree, maxDistanceFromTree,
                minDistanceFromObject, maxDistanceFromObject,
                minDistanceFromParent, maxDistanceFromParent);

            material.SetInt(_TerrainPixelCount, td.heightMap.width);
            

            material.SetTexture(_IndexMap, td.dataCache.indexMaps[td.terrain]);
            material.SetTexture(_WeightMap, td.dataCache.weightMaps[td.terrain]);
            

            keywordBuilder.Add("_RECONSTRUCTNORMAL");
            float densityScale = GetTerrainScalingFactor(td.terrain);

            filterSet.PrepareTransform(this.transform, td.terrain, material, keywordBuilder.keywords, densityScale);
            
            keywordBuilder.Assign(material);

            
            int instanceCount = Mathf.RoundToInt(512 * density * density * densityScale * densityScale);
            material.SetFloat(_InstanceCount, instanceCount);

            float ny = (float)instanceCount / 512.0f;
            int yCount = Mathf.FloorToInt(instanceCount / 512);
            if (ny != Mathf.FloorToInt(ny))
                yCount += 1;

            material.SetFloat(_YCount, yCount);

            
            var posWeightRT = new RenderTexture(512, yCount, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            posWeightRT.name = "TreeStamp::PositonWeightRT";

            var randomsRT = new RenderTexture(512, yCount, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            posWeightRTs[td.terrain] = posWeightRT;
            randomsRTs[td.terrain] = randomsRT;

            _mrt[0] = posWeightRT.colorBuffer;
            _mrt[1] = randomsRT.colorBuffer;

            Graphics.SetRenderTarget(_mrt, posWeightRT.depthBuffer);
            Graphics.Blit(poissonDisk, material, 0);

            TreeUtil.ApplyOcclusion(posWeightRT, od, occludeOthers, heightModAmount > 0 || layer != null && layerWeight > 0);
            Profiler.EndSample();

        }

        static int _RealHeight = Shader.PropertyToID("_RealHeight");
        static int _TreeSDF = Shader.PropertyToID("_TreeSDF");
        static int _Amount = Shader.PropertyToID("_Amount");
        static int _Width = Shader.PropertyToID("_Width");
        static int _Index = Shader.PropertyToID("_Index");

        Material heightModMat = null;
        Material splatModMat = null;
        public void ProcessTreeStamp(TreeData vd, Dictionary<Terrain, List<TreeJobHolder>> jobs, OcclusionData od)
        {
            if (poissonDisk == null)
                return;
            if (prototypes.Count == 0)
                return;
            Profiler.BeginSample("Process Tree Stamp");
            Profiler.BeginSample("Height/Texture Mods");
            var posWeightRT = posWeightRTs[vd.terrain];
            var randomsRT = randomsRTs[vd.terrain];

            if (heightModAmount != 0)
            {
                if (heightModMat == null)
                {
                    heightModMat = new Material(Shader.Find("Hidden/MicroVerse/TreeHeightMod"));
                }
                var nhm = RenderTexture.GetTemporary(vd.heightMap.descriptor);
                heightModMat.SetFloat(_RealHeight, od.RealHeight);
                heightModMat.SetTexture(_TreeSDF, od.currentTreeSDF);
                heightModMat.SetFloat(_Amount, heightModAmount);
                heightModMat.SetTexture(_PlacementMask, od.terrainMask);
                float ratio = vd.heightMap.width / vd.terrain.terrainData.size.x;
                heightModMat.SetFloat(_Width, heightModWidth * ratio);
                Graphics.Blit(vd.heightMap, nhm, heightModMat);
                Graphics.Blit(nhm, vd.heightMap);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(nhm);
            }
            if ((layer != null && layerWeight > 0))
            {
                if (splatModMat == null)
                {
                    splatModMat = new Material(Shader.Find("Hidden/MicroVerse/TreeSplatMod"));
                }
                if (applyFilteringToTextureMod)
                {
                    var kb = new KeywordBuilder();
                    kb.Add("_RECONSTRUCTNORMAL");
                    kb.Add("_APPLYFILTER");
                    
                    filterSet.PrepareTransform(this.transform, od.terrain, splatModMat, kb.keywords);
                    filterSet.PrepareMaterial(this.transform, splatModMat, kb.initialKeywords);
                    splatModMat.SetTexture(_Heightmap, vd.heightMap);
                    splatModMat.SetTexture(_Normalmap, vd.normalMap);
                    splatModMat.SetTexture(_Curvemap, vd.curveMap);
                    splatModMat.SetTexture(_Flowmap, vd.flowMap);
                    splatModMat.SetVectorArray(_TextureLayerWeights, textureLayerWeights);
                    kb.Assign(splatModMat);
                }
                var indexMap = vd.dataCache.indexMaps[vd.terrain];
                var weightMap = vd.dataCache.weightMaps[vd.terrain];
                
                var nim = RenderTexture.GetTemporary(indexMap.descriptor);
                var nwm = RenderTexture.GetTemporary(weightMap.descriptor);

                splatModMat.SetTexture(_TreeSDF, od.currentTreeSDF);
                splatModMat.SetTexture(_IndexMap, indexMap);
                splatModMat.SetTexture(_WeightMap, weightMap);
                splatModMat.SetTexture(_PlacementMask, od.terrainMask);
                splatModMat.SetFloat(_Amount, layerWeight);
                float ratio = indexMap.width / vd.terrain.terrainData.size.x;
                splatModMat.SetFloat(_Width, layerWidth * ratio);
                int splatIndex = TerrainUtil.FindTextureChannelIndex(vd.terrain, layer);
                splatModMat.SetFloat(_Index, splatIndex);
                RenderBuffer[] _mrt = new RenderBuffer[2];
                _mrt[0] = nim.colorBuffer;
                _mrt[1] = nwm.colorBuffer;
                Graphics.SetRenderTarget(_mrt, nim.depthBuffer);
                
                Graphics.Blit(null, splatModMat);

                // copy back, this sucks!
                Graphics.Blit(nim, indexMap);
                Graphics.Blit(nwm, weightMap);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(nim);
                RenderTexture.ReleaseTemporary(nwm);
            }

            UnityEngine.Profiling.Profiler.EndSample();

#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (MicroVerse.instance.bufferCaptureTarget != null)
            {
                if (MicroVerse.instance.bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.TreeStampOcclusionMask))
                {
                    var nm = od.currentTreeMask;
                    if (nm != null)
                        MicroVerse.instance.bufferCaptureTarget.SaveRenderData(od.terrain, BufferCaptureTarget.BufferCapture.TreeStampOcclusionMask, nm, this.name);
                }
                if (MicroVerse.instance.bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.TreeStampSDF))
                {
                    var nm = od.currentTreeSDF;
                    if (nm != null)
                        MicroVerse.instance.bufferCaptureTarget.SaveRenderData(od.terrain, BufferCaptureTarget.BufferCapture.TreeStampSDF, nm, this.name);
                }
            }
#endif

            Profiler.BeginSample("Setup Jobs");
            // setup job to unpack
            var jholder = new TreeJobHolder();

            NativeArray<int> indexes = new NativeArray<int>(prototypeMappings[od.terrain].Length, Allocator.Persistent);
            indexes.CopyFrom(prototypeMappings[od.terrain]);

            jholder.AddJob(posWeightRT, randomsRT, indexes);
            if (jobs.ContainsKey(vd.terrain))
            {
                jobs[vd.terrain].Add(jholder);
            }
            else
            {
                jobs.Add(vd.terrain, new List<TreeJobHolder>() { jholder });
            }

            Profiler.EndSample();
            Profiler.EndSample();
        }

        protected override void OnDestroy()
        {
            if (material != null) DestroyImmediate(material);
            if (heightModMat != null) DestroyImmediate(heightModMat);
            if (splatModMat != null) DestroyImmediate(splatModMat);
            base.OnDestroy();
        }

        public void Dispose()
        {
            RenderTexture.active = null;
            foreach (var k in sdfs.Values)
            {
                if (k != null)
                {
                    RenderTexture.ReleaseTemporary(k);
                }
            }
            randomizationBuffer.Dispose();
            sdfs.Clear();
        }


        void OnDrawGizmosSelected()
        {
            if (filterSet.falloffFilter.filterType != FalloffFilter.FilterType.Global &&
                filterSet.falloffFilter.filterType != FalloffFilter.FilterType.SplineArea)
            { 
                if (MicroVerse.instance != null)
                {
                    Gizmos.color = MicroVerse.instance.options.colors.treeStampColor;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
                }
            }
        }

        public bool ApplyTextureStamp(RenderTexture indexSrc, RenderTexture indexDest, RenderTexture weightSrc, RenderTexture weightDest, TextureData splatmapData, OcclusionData od)
        {
            return false;
        }

        public void InqTerrainLayers(Terrain terrain, List<TerrainLayer> prototypes)
        {
            if (layer != null)
            {
                prototypes.Add(layer);
            }
        }
    }
}
