using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace JBooth.MicroVerseCore
{

    public static class NativeArrayExtensions
    {
        public static unsafe void CopyToFast<T>(
                   this NativeArray<T> nativeArray,
                   T[,] array)
                   where T : struct
        {
            int byteLength = nativeArray.Length * Marshal.SizeOf(default(T));
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0, 0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
        }

        public static unsafe void CopyToFastByteToInt(
                   this NativeArray<byte> nativeArray,
                   int[,] array)
        {
            int byteLength = nativeArray.Length * Marshal.SizeOf(default(byte));
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0, 0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpyStride(managedBuffer, 4, nativeBuffer, 1,1, byteLength);
        }
    }

    [BurstCompile]
    struct UnityAPISucksJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte> source;
        [WriteOnly] public NativeArray<int> target;
        public void Execute(int i)
        {
            target[i] = (int)source[i];
        }
    }

    public class DetailJobHolder
    {
        private AsyncGPUReadbackRequest gpuRequest;
        RenderTexture detailLayer;
        public int detailIndex { get; private set; }
        public Terrain terrain;
        NativeArray<byte> rawData;
        int width, height;
        static int[,] resultValues = null;

        public bool canceled { get; set; }
        public bool IsDone()
        {
            return gpuRequest.done;
        }

        public void Dispose()
        {
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(detailLayer);
        }

        private void OnAsynComplete(AsyncGPUReadbackRequest obj)
        {
            // Unity could we get a way to do this that doesn't suck? Details are int[i,i]
            // A) you only use a byte worth of each int anyway (255 max value)
            // B) none of this makes sense with GPU stuff
            // C) Or native array stuff
            // D) Forced into 16mb allocation for 4 2k terrains
            if (canceled)
            {
                if (rawData.IsCreated)
                    rawData.Dispose();
                return;
            }
            UnityEngine.Profiling.Profiler.BeginSample("Apply Details: Shit Unity API == mem!");
            UnityEngine.Profiling.Profiler.BeginSample("Alloc buffer");
            if (resultValues == null || width * height != resultValues.Length)
            {
                resultValues = new int[width, height];
            }
            UnityEngine.Profiling.Profiler.EndSample();
            NativeArray<int> temp = new NativeArray<int>(rawData.Length, Allocator.TempJob);
            UnityAPISucksJob job = new UnityAPISucksJob()
            {
                source = rawData,
                target = temp
            };

            // turns out, forcing the job to complete is faster than doing this asyncronously.
            // With this method, in the test scene, we peak at 90ms per update frame. Where as
            // in the version that lets this run then finishes later, it peaks at 125ms with
            // a less consistent frame rate. Whats odd is that the call to set the data on the
            // terrain takes way more time than in the amortized version. I think this is because
            // the async readback callback is hapenning earlier in the frame, and whatever that API
            // does is async, so it's able to get done faster with less waiting by being earlier in
            // the frame. Fucking hell. 
            job.Schedule(temp.Length, 4096).Complete();
            temp.CopyToFast(resultValues); 
            temp.Dispose();
            rawData.Dispose();

            UnityEngine.Profiling.Profiler.BeginSample("Set data on terrain");
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(detailLayer);
            if (terrain != null && terrain.terrainData != null)
            {
                terrain.terrainData.SetDetailLayer(0, 0, detailIndex, resultValues);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();

            // this was more direct, but ultimately slower.
            /*
            UnityEngine.Profiling.Profiler.BeginSample("Apply Details: Shit Unity API == mem!");
            UnityEngine.Profiling.Profiler.BeginSample("Alloc buffer");
            if (resultValues == null || width * height != resultValues.Length)
            {
                resultValues = new int[width, height];
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Copy to buffer");
            rawData.CopyToFastByteToInt(resultValues);
            rawData.Dispose();
            RenderTexture.active = null;
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Set data on terrain");
            RenderTexture.ReleaseTemporary(detailLayer);
            terrain.terrainData.SetDetailLayer(0, 0, detailIndex, resultValues);
            UnityEngine.Profiling.Profiler.EndSample();


            UnityEngine.Profiling.Profiler.EndSample();
            Dispose();
            */
        }

        public void AddJob(RenderTexture detailLayer, int detailIndex)
        {
            this.width = detailLayer.width;
            this.height = detailLayer.height;
            this.detailIndex = detailIndex;
            this.detailLayer = detailLayer;
            if (MicroVerse.noAsyncReadback)
            {
                Texture2D tex = new Texture2D(detailLayer.width, detailLayer.height, TextureFormat.R8, false, true);
                RenderTexture.active = detailLayer;
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                RenderTexture.active = null;
                tex.Apply();
                if (resultValues == null || width * height != resultValues.Length)
                {
                    resultValues = new int[width, height];
                }
                NativeArray<byte> rawData = tex.GetRawTextureData<byte>();
                rawData.CopyToFastByteToInt(resultValues);
                rawData.Dispose();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(detailLayer);
                GameObject.DestroyImmediate(tex);
                terrain.terrainData.SetDetailLayer(0, 0, detailIndex, resultValues);

            }
            else
            {
                rawData = new NativeArray<byte>(width * height, Allocator.Persistent);
                gpuRequest = AsyncGPUReadback.RequestIntoNativeArray(ref rawData, detailLayer, 0, OnAsynComplete);
            }
        }
    }

    [ExecuteAlways]
    public class DetailStamp : Stamp, IDetailModifier
    {
        public override FilterSet GetFilterSet()
        {
            return filterSet;
        }
        public DetailPrototypeSerializable prototype = new DetailPrototypeSerializable();
        public DetailPrototypeSettings settings = null;
        public FilterSet filterSet = new FilterSet();

        Material material;
        public bool occludedByOthers = true;
        public float minDistanceFromTree = 0;
        public float maxDistanceFromTree = 0;
        public float minDistanceFromObject = 0;
        public float maxDistanceFromObject = 0;
        public float minDistanceFromParent = 0;
        public float maxDistanceFromParent = 0;
        public bool sdfClamp;

        [Tooltip("Weight Range in which details will spawn")]
        public Vector2 weightRange = new Vector2(0, 999999);

        public bool NeedCurvatureMap() { return filterSet.NeedCurvatureMap(); }
        public bool NeedFlowMap() { return filterSet.NeedFlowMap(); }

        public bool UsesOtherTreeSDF() { return minDistanceFromTree > 0 || maxDistanceFromTree > 0; }
        public bool UsesOtherObjectSDF() { return minDistanceFromObject > 0 || maxDistanceFromObject > 0; }

        public bool NeedSDF() { return false; }
        // do I need my parent to generate an SDF is parented
        public bool NeedParentSDF()
        {
            return minDistanceFromParent > 0 || maxDistanceFromParent > 0;
        }

        // Do I need to generate an SDF for subspawners
        public bool NeedToGenerateSDFForChilden() { return false; }
        public void SetSDF(Terrain t, RenderTexture rt) { }
        public RenderTexture GetSDF(Terrain t) { return null; }



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

        static Shader detailShader = null;
        public void Initialize()
        {
            if (detailShader == null)
            {
                detailShader = Shader.Find("Hidden/MicroVerse/DetailFilter");
            }
            material = new Material(detailShader);
            keywordBuilder.ClearInitial();
            filterSet.PrepareMaterial(this.transform, material, keywordBuilder.initialKeywords);
        }

        static int _Heightmap = Shader.PropertyToID("_Heightmap");
        static int _Normalmap = Shader.PropertyToID("_Normalmap");
        static int _Curvemap = Shader.PropertyToID("_Curvemap");
        static int _Flowmap = Shader.PropertyToID("_Flowmap");
        static int _WeightRange = Shader.PropertyToID("_WeightRange");
        static int _Density = Shader.PropertyToID("_Density");
        static int _PlacementMask = Shader.PropertyToID("_PlacementMask");
        static int _IndexMap = Shader.PropertyToID("_IndexMap");
        static int _WeightMap = Shader.PropertyToID("_WeightMap");
        static int _TextureLayerWeights = Shader.PropertyToID("_TextureLayerWeights");
        static int _ClearLayer = Shader.PropertyToID("_ClearLayer");
        static int _ClearMask = Shader.PropertyToID("_ClearMask");
        static int _DensityNoise = Shader.PropertyToID("_DensityNoise");

        public void ApplyDetailStamp(DetailData dd, Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers, OcclusionData od)
        {
            var proto = prototype;
            if (settings != null && settings.prototype != null)
            {
                proto = settings.prototype;
            }
            if (!proto.IsValid())
                return;

            

            int detailIndex = VegetationUtilities.FindDetailIndex(od.terrain, proto);
            var textureLayerWeights = filterSet.GetTextureWeights(od.terrain.terrainData.terrainLayers);
            keywordBuilder.Clear();
            keywordBuilder.Add("_RECONSTRUCTNORMAL");
            UnityEngine.Profiling.Profiler.BeginSample("Detail Modifier");
            material.SetTexture(_ClearMask, dd.clearMap);
            material.SetFloat(_ClearLayer, dd.layerIndex);
            material.SetTexture(_Heightmap, dd.heightMap);
            material.SetTexture(_Normalmap, dd.normalMap);
            material.SetTexture(_Curvemap, dd.curveMap);
            material.SetTexture(_Flowmap, dd.flowMap);
            material.SetVector(_WeightRange, weightRange);


#if UNITY_2022_2_OR_NEWER
            if (od.terrain.terrainData.detailScatterMode == DetailScatterMode.CoverageMode)
            {
                material.SetVector(_DensityNoise, Vector2.zero);
                material.SetFloat(_Density, 1);
            }
            else
            {
                if (proto.density < 1)
                {
                    material.SetFloat(_Density, 1.0f / 128.0f);
                    material.SetVector(_DensityNoise, new Vector2(1.0f - Mathf.Pow(proto.density, 4), 0.25f));
                }
                else
                {
                    material.SetFloat(_Density, proto.density / 128.0f);
                    material.SetVector(_DensityNoise, Vector2.zero);
                }
            }
#else
            if (proto.density < 1)
            {
                material.SetFloat(_Density, 1.0f / 128.0f);
                material.SetVector(_DensityNoise, new Vector2(1.0f - Mathf.Pow(proto.density, 4), 0.25f));
            }
            else
            {
                material.SetFloat(_Density, proto.density / 128.0f);
                material.SetVector(_DensityNoise, Vector2.zero);
            }
#endif
            if (material.GetFloat(_Density) < 1)
            {
               keywordBuilder.Add("_DENSITYNOISENEEDED");
            }

            if (occludedByOthers)
                material.SetTexture(_PlacementMask, od.terrainMask);
            else
                material.SetTexture(_PlacementMask, null);

            float ratio = dd.heightMap.width / dd.terrain.terrainData.size.x;
            FilterSet.PrepareSDFFilter(keywordBuilder, material, transform, od, ratio, sdfClamp,
               minDistanceFromTree, maxDistanceFromTree,
               minDistanceFromObject, maxDistanceFromObject,
               minDistanceFromParent, maxDistanceFromParent);
            

            material.SetTexture(_IndexMap, dd.dataCache.indexMaps[dd.terrain]);
            material.SetTexture(_WeightMap, dd.dataCache.weightMaps[dd.terrain]);
            material.SetVectorArray(_TextureLayerWeights, textureLayerWeights);
            filterSet.PrepareTransform(this.transform, dd.terrain, material, keywordBuilder.keywords, GetTerrainScalingFactor(dd.terrain));
            
            keywordBuilder.Assign(material);
            RenderTexture rt = RenderTexture.GetTemporary(dd.terrain.terrainData.detailWidth, dd.terrain.terrainData.detailHeight, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
            rt.name = "DetailStamp::rt";
            
            Graphics.Blit(null, rt, material);
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

            
            UnityEngine.Profiling.Profiler.EndSample();

        }

        protected override void OnDestroy()
        {
            if (material != null) DestroyImmediate(material);
            base.OnDestroy();
        }

        public void Dispose()
        {
            
        }


        void OnDrawGizmosSelected()
        {
            if (filterSet.falloffFilter.filterType != FalloffFilter.FilterType.Global &&
                filterSet.falloffFilter.filterType != FalloffFilter.FilterType.SplineArea)
            {
                if (MicroVerse.instance != null)
                {
                    Gizmos.color = MicroVerse.instance.options.colors.detailStampColor;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
                }
            }
        }


        public void InqDetailPrototypes(List<DetailPrototypeSerializable> prototypes)
        {
            if (settings != null && settings.prototype != null)
            {
                prototypes.Add(settings.prototype);
            }
            else
            {
                prototypes.Add(prototype);
            }
        }

        public bool NeedDetailClear()
        {
            return false;
        }

        public void ApplyDetailClear(DetailData td)
        {
         
        }
    }
}
