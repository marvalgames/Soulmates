using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;

namespace JBooth.MicroVerseCore
{
    public class ObjectUtil
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
            if (occlusionShader == null)
            {
                occlusionShader = (ComputeShader)Resources.Load("MicroVersePositionToOcclusionMask");
            }
            occlusionShader.EnableKeyword("_R8");
            int kernelHandle = occlusionShader.FindKernel("CSMain");
            occlusionShader.SetInt(_Result_Width, od.terrainMask.width);
            occlusionShader.SetInt(_Result_Height, od.terrainMask.height);
            occlusionShader.SetTexture(kernelHandle, _Positions, positions);

            if (others || selfSDF)
            {
                if (od.currentObjectMask == null)
                {
                    var desc = od.terrainMask.descriptor;
                    desc.colorFormat = RenderTextureFormat.R8;
                    desc.enableRandomWrite = true;
                    od.currentObjectMask = RenderTexture.GetTemporary(desc);
                    od.currentObjectMask.name = "Occlusion::CurrentObjectMask";
                }
                RenderTexture.active = od.currentObjectMask;
                GL.Clear(false, true, Color.clear);
                occlusionShader.SetTexture(kernelHandle, _Result, od.currentObjectMask);
                occlusionShader.Dispatch(kernelHandle, Mathf.CeilToInt(positions.width / 512), positions.height, 1);
            }

        }
    }

    public class ObjectJobHolder
    {
        public ObjectStamp stamp;
        public NativeArray<half4> positionWeightData;
        public NativeArray<half4> rotationData;
        public NativeArray<half4> scaleIndexData;

        private AsyncGPUReadbackRequest gpuRequestPlacement;
        private AsyncGPUReadbackRequest gpuRequestRotation;
        private AsyncGPUReadbackRequest gpuRequestScale;

        private NativeArray<uint> objectIndexes;
        private ObjectStamp.ReturnData buffer;

        public int unpackIndex = 0;
        Texture2D positionTex = null;
        Texture2D rotationTex = null;
        Texture2D scaleTex = null;


        public ObjectJobHolder(ObjectStamp stamp, NativeArray<uint> objIndexes, ObjectStamp.ReturnData buffer, int maxCount)
        { 
            this.stamp = stamp;
            this.buffer = buffer;
            this.objectIndexes = objIndexes;
            
            if (MicroVerse.noAsyncReadback)
            {
                int width = buffer.positionWeight.width;
                int height = buffer.positionWeight.height;
                positionTex = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
                rotationTex = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
                scaleTex = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
                RenderTexture.active = buffer.positionWeight;
                positionTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                positionTex.Apply();
                positionWeightData = positionTex.GetRawTextureData<half4>();
                RenderTexture.active = buffer.rotationIndex;
                rotationTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                rotationTex.Apply();
                rotationData = rotationTex.GetRawTextureData<half4>();
                RenderTexture.active = buffer.scale;
                scaleTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                scaleTex.Apply();
                scaleIndexData = scaleTex.GetRawTextureData<half4>(); 
            }
            else
            {
                int size = buffer.positionWeight.width * buffer.positionWeight.height;
                this.positionWeightData = new NativeArray<half4>(size, Allocator.Persistent);
                this.rotationData = new NativeArray<half4>(size, Allocator.Persistent);
                this.scaleIndexData = new NativeArray<half4>(size, Allocator.Persistent);
                this.gpuRequestPlacement = AsyncGPUReadback.RequestIntoNativeArray<half4>(ref positionWeightData, buffer.positionWeight);
                this.gpuRequestRotation = AsyncGPUReadback.RequestIntoNativeArray<half4>(ref rotationData, buffer.rotationIndex);
                this.gpuRequestScale = AsyncGPUReadback.RequestIntoNativeArray<half4>(ref scaleIndexData, buffer.scale);
            }
        }

        public bool IsDone()
        {
            if (MicroVerse.noAsyncReadback)
                return true;
        
            return (gpuRequestPlacement.done && gpuRequestRotation.done && gpuRequestScale.done);
        }
        
        public bool canceled { get; set; }

        public void Dispose()
        {
            if (buffer != null)
            {
                if (MicroVerse.noAsyncReadback == false)
                {
                    if (positionWeightData.IsCreated)
                    {
                        positionWeightData.Dispose();
                    }
                    if (rotationData.IsCreated)
                    {
                        rotationData.Dispose();
                    }
                    if (scaleIndexData.IsCreated)
                    {
                        scaleIndexData.Dispose();
                    }
                }
                else
                {
                    if (positionTex != null) Object.DestroyImmediate(positionTex);
                    if (rotationTex != null) Object.DestroyImmediate(rotationTex);
                    if (scaleTex != null) Object.DestroyImmediate(scaleTex);
                }
                if (objectIndexes.IsCreated)
                {
                    objectIndexes.Dispose();
                }

                buffer = null;
            }
        }
    }

    [ExecuteAlways]
    public class ObjectStamp : Stamp, IObjectModifier, ITextureModifier
    {
        public override FilterSet GetFilterSet()
        {
            return filterSet;
        }

        public string id;

        public List<GameObject> spawnedInstances = new List<GameObject>();

        public enum Lock
        {
            None,
            XY,
            XZ,
            YZ,
            XYZ
        }

        [Tooltip("Spawning as a prefab is slower than spawning as a game object")]
        public bool spawnAsPrefab;

        [System.Serializable]
        public struct Randomization
        {
            public float weight;
            public Vector2 weightRange;
            public Vector2 rotationRangeX;
            public Vector2 rotationRangeY;
            public Vector2 rotationRangeZ;
            public Vector2 scaleRangeX;
            public Vector2 scaleRangeY;
            public Vector2 scaleRangeZ;
            public Lock scaleLock;
            public Lock rotationLock;
            public float slopeAlignment;

            public Vector2 sink;
            public float scaleMultiplierAtBoundaries;
            
            public int flags;
            public bool densityByWeight { get { return (flags & (1 << 3)) == 0; } set { if (!value) flags |= 1 << 3; else flags &= ~(1 << 3); } }
            public bool disabled { get { return !((flags & (1 << 4)) == 0); } set { if (value) flags |= 1 << 4; else flags &= ~(1 << 4); } }
            public bool alignDownhill { get { return !((flags & (1 << 5)) == 0); } set { if (value) flags |= 1 << 5; else flags &= ~(1 << 5); } }

        }

        static Texture2D randomTexture;

        // these arrays must be kept in sync. Would like to contain them as one,
        // but one needs pointers to objects and the other needs to be a struct
        // for jobs.

        public List<Randomization> randomizations = new List<Randomization>();
        [Tooltip("Should the created game objects show up in the heriarchy or be hidden")]
        public bool hideInHierarchy;
        [Tooltip("spawn all objects under this transform")]
        public Transform parentObject;
        [Tooltip("Random seed, which can be changed")]
        public uint seed = 0;
        [Tooltip("You likely don't want to change this, but it controls how the spread of randomness is done")]
        public Texture2D poissonDisk;
        [Range(0,2)]
        public float poissonDiskStrength = 1;
        [Range(0.1f, 8)]
        public float density = 1;
        public List<GameObject> prototypes = new List<GameObject>();

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

        public FilterSet filterSet = new FilterSet();

        Material material;

        public bool NeedCurvatureMap() { return filterSet.NeedCurvatureMap(); }
        public bool NeedFlowMap() { return filterSet.NeedFlowMap(); }


        // because a dictionary won't serialize
        [System.Serializable]
        public class ParentObjectEntry
        {
            public Terrain terrain;
            public Transform transform;
        }

        List<ParentObjectEntry> parentObjects = new List<ParentObjectEntry>();

        public Transform FindParentObject(Terrain t)
        {
            if (parentObject != null)
                return parentObject;
            foreach (var poe in parentObjects)
            {
                if (poe.terrain == t)
                    return poe.transform;
            }
            return null;
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

        public bool OccludesOthers()
        {
            return occludeOthers;
        }

        public bool UsesOtherTreeSDF() { return minDistanceFromTree > 0 || maxDistanceFromTree > 0; }
        public bool UsesOtherObjectSDF() { return minDistanceFromObject > 0 || maxDistanceFromObject > 0; }


        public bool NeedSDF()
        {
            return (minDistanceFromTree > 0 || maxDistanceFromTree > 0)  || minDistanceFromObject > 0 || maxDistanceFromObject > 0|| heightModAmount > 0 || (layer != null && layerWeight > 0);
        }

        // do I need my parent to generate an SDF is parented
        public bool NeedParentSDF()
        {
            return minDistanceFromParent > 0 || maxDistanceFromParent > 0;
        }

        // Do I need to generate an SDF for subspawners
        public bool NeedToGenerateSDFForChilden()
        {
            var subs = GetComponentsInChildren<ISpawner>();
            foreach (var s in subs)
            {
                if (s.NeedParentSDF()) return true;
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

        public bool destroyOnNextClear { get; set; }
        public void ClearSpawnedInstances()
        {
            if (destroyOnNextClear)
            {
                foreach (var obj in spawnedInstances)
                {
                    if (obj != null && obj != parentObject && obj != gameObject)
                        DestroyImmediate(obj);
                }
            }
            else
            {
                foreach (var obj in spawnedInstances)
                {
                    if (obj != null)
                        SpawnProcessor.Despawn(obj);
                }
            }
            destroyOnNextClear = false;
            spawnedInstances.Clear();
            foreach (var t in parentObjects)
            {
                if (t.transform != null && t.transform != parentObject)
                {
                    DestroyImmediate(t.transform.gameObject);
                }
            }
            parentObjects.Clear();
        }

        static Shader objectShader = null;
        public void Initialize()
        {
            ClearSpawnedInstances();

            if (objectShader == null)
            {
                objectShader = Shader.Find("Hidden/MicroVerse/ObjectFilter");
            }
            material = new Material(objectShader);

            if (randomTexture == null)
            {
                randomTexture = new Texture2D(64, 64, TextureFormat.RGBAHalf, false, true);
                randomTexture.filterMode = FilterMode.Point;
                var data = randomTexture.GetRawTextureData<half4>();
                Unity.Mathematics.Random rand = new Unity.Mathematics.Random(31);
                rand.InitState(71);
                for (int i = 0; i < data.Length; ++i)
                {
                    data[i] = (half4)rand.NextFloat4(0, 1);
                }
                randomTexture.Apply(false, false);

            }

            keywordBuilder.ClearInitial();
            filterSet.PrepareMaterial(this.transform, material, keywordBuilder.initialKeywords);
        }


        int[] prototypeIndexes;

        public class ReturnData
        {
            public RenderTexture positionWeight;
            public RenderTexture rotationIndex;
            public RenderTexture scale;
        }

        Dictionary<Terrain, ReturnData> returnedRTs = new Dictionary<Terrain, ReturnData>();

        static int _RandomTex = Shader.PropertyToID("_RandomTex");
        static int _Disc = Shader.PropertyToID("_Disc");
        static int _DiscStrength = Shader.PropertyToID("_DiscStrength");
        static int _Density = Shader.PropertyToID("_Density");
        static int _InstanceCount = Shader.PropertyToID("_InstanceCount");
        static int _Heightmap = Shader.PropertyToID("_Heightmap");
        static int _Normalmap = Shader.PropertyToID("_Normalmap");
        static int _Curvemap = Shader.PropertyToID("_Curvemap");
        static int _Flowmap = Shader.PropertyToID("Flowmap");
        static int _ClearLayer = Shader.PropertyToID("_ClearLayer");
        static int _ClearMask = Shader.PropertyToID("_ClearMask");
        static int _MinHeight = Shader.PropertyToID("_MinHeight");
        static int _TotalWeights = Shader.PropertyToID("_TotalWeights");
        static int _HeightOffset = Shader.PropertyToID("_HeightOffset");
        static int _PlacementMask = Shader.PropertyToID("_PlacementMask");
        static int _ObjectMask = Shader.PropertyToID("_ObjectMask");

        static int _TerrainPixelCount = Shader.PropertyToID("_TerrainPixelCount");
        static int _ModWidth = Shader.PropertyToID("_ModWidth");
        static int _IndexMap = Shader.PropertyToID("_IndexMap");
        static int _WeightMap = Shader.PropertyToID("_WeightMap");
        static int _Seed = Shader.PropertyToID("_Seed");
        static int _TextureLayerWeights = Shader.PropertyToID("_TextureLayerWeights");
        static int _Randomizations = Shader.PropertyToID("_Randomizations");
        static int _YCount = Shader.PropertyToID("_YCount");


        
        public Transform FindParentInScene(Terrain t)
        {
            var scene = t.gameObject.scene;
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                var holders = r.GetComponentsInChildren<ObjectStampHolder>();
                foreach (var h in holders)
                {
                    if (h.guid == id)
                        return h.transform;
                }
            }
            return null;
        }

        public void ApplyObjectStamp(ObjectData td, Dictionary<Terrain, List<ObjectJobHolder>> jobs, OcclusionData od)
        {
            if (poissonDisk == null)
                return;

            if (prototypes.Count == 0)
                return;

            ClearSpawnedInstances();

            Transform rootParentTransform = parentObject;
            if (parentObject != null)
            {
                parentObjects.Add(new ParentObjectEntry() { terrain = td.terrain, transform = parentObject });
            }
            else
            {
                if (parentObjects.Count == 0)
                {
                    // when working with multiple scenes we cannot serialize references, so lets
                    // try to find a holder for our prefab first
                    Transform trans = FindParentInScene(td.terrain);
                    if (trans == null)
                    {
                        trans = new GameObject(this.name + " (Container)").transform;
#if UNITY_EDITOR
                        UnityEditor.GameObjectUtility.EnsureUniqueNameForSibling(trans.gameObject);
                        UnityEditor.EditorUtility.SetDirty(trans);
#endif
                        var holder = trans.gameObject.AddComponent<ObjectStampHolder>();
                        holder.guid = id;
                        trans.localPosition = Vector3.zero;
                        trans.localRotation = Quaternion.identity;
                        trans.localScale = Vector3.one;
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(trans.gameObject, td.terrain.gameObject.scene);
                    }
                    parentObjects.Add(new ParentObjectEntry() { terrain = td.terrain, transform = trans });
                    rootParentTransform = trans;
                }
                else if (td.terrain.gameObject.scene != rootParentTransform.gameObject.scene)
                {
                    Transform trans = FindParentInScene(td.terrain);
                    if (trans == null)
                    {
                        trans = new GameObject(this.name + " (Container)").transform;
#if UNITY_EDITOR
                        UnityEditor.GameObjectUtility.EnsureUniqueNameForSibling(trans.gameObject);
                        UnityEditor.EditorUtility.SetDirty(trans);
#endif
                        var holder = trans.gameObject.AddComponent<ObjectStampHolder>();
                        holder.guid = id;
                        trans.localPosition = Vector3.zero;
                        trans.localRotation = Quaternion.identity;
                        trans.localScale = Vector3.one;
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(trans.gameObject, td.terrain.gameObject.scene);
                    }
                    parentObjects.Add(new ParentObjectEntry() { terrain = td.terrain, transform = trans });
                }
                else
                {
                    parentObjects.Add(new ParentObjectEntry() { terrain = td.terrain, transform = rootParentTransform });
                }
            }
            
            UnityEngine.Profiling.Profiler.BeginSample("Object Modifier");
            var textureLayerWeights = filterSet.GetTextureWeights(od.terrain.terrainData.terrainLayers);
            prototypeIndexes = new int[prototypes.Count];
            keywordBuilder.Clear();

            float totalWeight = 0;
            for (int i = 0; i < prototypes.Count; ++i)
            {
                prototypeIndexes[i] = i;
                totalWeight += randomizations[i].weight + 1;
            }
            poissonDisk.wrapMode = TextureWrapMode.Repeat;
            poissonDisk.filterMode = FilterMode.Point;
            float densityScale = GetTerrainScalingFactor(td.terrain);
            int instanceCount = Mathf.RoundToInt(512 * density * density * densityScale * densityScale);

            material.SetTexture(_RandomTex, randomTexture);
            material.SetTexture(_Disc, poissonDisk);
            material.SetFloat(_DiscStrength, poissonDiskStrength);
            material.SetFloat(_Density, density);
            material.SetFloat(_InstanceCount, instanceCount);
            material.SetTexture(_Heightmap, td.heightMap);
            material.SetTexture(_Normalmap, td.normalMap);
            material.SetTexture(_Curvemap, td.curveMap);
            material.SetTexture(_Flowmap, td.flowMap);
            material.SetFloat(_MinHeight, minHeight);
            material.SetFloat("_NumObjectIndexes", prototypes.Count);
            material.SetFloat(_TotalWeights, totalWeight);
            material.SetFloat(_HeightOffset, heightModAmount);
            material.SetVector("_TerrainSize", td.terrain.terrainData.size);
            material.SetVector("_TerrainPosition", td.terrain.transform.position);
            material.SetFloat(_ClearLayer, td.layerIndex);
            material.SetTexture(_ClearMask, td.clearMap);

            if (occludedByOthers)
            {
                material.SetTexture(_PlacementMask, od.terrainMask);
                material.SetTexture(_ObjectMask, od.objectMask);
            }

            float ratio = td.heightMap.width / td.terrain.terrainData.size.x;
            FilterSet.PrepareSDFFilter(keywordBuilder, material, transform, od, ratio, sdfClamp,
               minDistanceFromTree, maxDistanceFromTree,
               minDistanceFromObject, maxDistanceFromObject,
               minDistanceFromParent, maxDistanceFromParent);

            material.SetInt(_TerrainPixelCount, td.heightMap.width);
            material.SetFloat(_ModWidth, Mathf.Max(layerWeight, heightModWidth));

            material.SetTexture(_IndexMap, td.indexMap);
            material.SetTexture(_WeightMap, td.weightMap);
            material.SetFloat(_Seed, seed);
            material.SetVectorArray(_TextureLayerWeights, textureLayerWeights);
            NativeArray<Randomization> randoms = new NativeArray<Randomization>(randomizations.Count, Allocator.Temp);
            randoms.CopyFrom(randomizations.ToArray());
            ComputeBuffer cb = new ComputeBuffer(randomizations.Count, UnsafeUtility.SizeOf<Randomization>());
            cb.SetData(randoms);
            material.SetBuffer(_Randomizations, cb);
            randoms.Dispose();
            filterSet.PrepareTransform(this.transform, td.terrain, material, keywordBuilder.keywords, GetTerrainScalingFactor(td.terrain));
            
            keywordBuilder.Add("_RECONSTRUCTNORMAL");
            float ny = (float)instanceCount / 512.0f;
            int yCount = Mathf.FloorToInt(instanceCount / 512);
            if (ny != Mathf.FloorToInt(ny))
                yCount += 1;

            material.SetFloat(_YCount, yCount);

            keywordBuilder.Assign(material);

            var posWeightRT = RenderTexture.GetTemporary(512, yCount, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            posWeightRT.name = "ObjectStamp::PositonWeightRT";

            var rotIndex = RenderTexture.GetTemporary(512, yCount, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            rotIndex.name = "ObjectStamp::RotationIndexRT";

            var scaleRT = RenderTexture.GetTemporary(512, yCount, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            scaleRT.name = "ObjectStamp::ScaleRT";


            var retData = new ReturnData();
            retData.positionWeight = posWeightRT;
            retData.rotationIndex = rotIndex;
            retData.scale = scaleRT;
            returnedRTs[td.terrain] = retData;

            RenderBuffer[] _mrt = new RenderBuffer[3];
            _mrt[0] = posWeightRT.colorBuffer;
            _mrt[1] = rotIndex.colorBuffer;
            _mrt[2] = scaleRT.colorBuffer;
            Graphics.SetRenderTarget(_mrt, posWeightRT.depthBuffer);
            Graphics.Blit(poissonDisk, material, 0);
            _mrt = null;
            cb.Dispose();

            // setup job to unpack
            
            NativeArray<uint> indexes = new NativeArray<uint>(prototypes.Count, Allocator.Persistent);
            for (int i = 0; i < prototypes.Count; ++i)
            {
                indexes[i] = (uint)i;
            }
            // find prototype index on actual terrain
            var jholder = new ObjectJobHolder(this, indexes, retData, instanceCount);
            if (jobs.ContainsKey(od.terrain))
            {
                jobs[od.terrain].Add(jholder);
            }
            else
            {
                jobs.Add(od.terrain, new List<ObjectJobHolder>() { jholder });
            }

            UnityEngine.Profiling.Profiler.EndSample();

            ObjectUtil.ApplyOcclusion(posWeightRT, od, occludeOthers, heightModAmount > 0 || layer != null && layerWeight > 0);

        }

        static Material heightModMat = null;
        static Material splatModMat = null;
        public void ProcessObjectStamp(ObjectData vd, Dictionary<Terrain, List<ObjectJobHolder>> jobs, OcclusionData od)
        {
            if (poissonDisk == null)
                return;

            if (prototypes.Count == 0)
                return;

#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (MicroVerse.instance.bufferCaptureTarget != null)
            {
                if (MicroVerse.instance.bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.ObjectStampOcclusionMask))
                {
                    var nm = od.currentObjectMask;
                    if (nm != null)
                        MicroVerse.instance.bufferCaptureTarget.SaveRenderData(od.terrain, BufferCaptureTarget.BufferCapture.ObjectStampOcclusionMask, nm, this.name);
                }
                if (MicroVerse.instance.bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.ObjectStampSDF))
                {
                    var nm = od.currentObjectSDF;
                    if (nm != null)
                        MicroVerse.instance.bufferCaptureTarget.SaveRenderData(od.terrain, BufferCaptureTarget.BufferCapture.ObjectStampSDF, nm, this.name);
                }
            }
#endif

            if (heightModAmount != 0)
            {
                if (heightModMat == null)
                {
                    heightModMat = new Material(Shader.Find("Hidden/MicroVerse/TreeHeightMod"));
                }
                var desc = vd.heightMap.descriptor;
                // Don't get fucked by maxLOD settings
                desc.width = vd.terrain.terrainData.heightmapResolution;
                desc.height = desc.width;
                var nhm = RenderTexture.GetTemporary(desc);
                heightModMat.SetFloat("_RealHeight", od.RealHeight);
                heightModMat.SetTexture("_TreeSDF", od.currentObjectSDF);
                heightModMat.SetFloat("_Amount", heightModAmount);
                heightModMat.SetTexture(_PlacementMask, od.terrainMask);
                float ratio = vd.heightMap.width / vd.terrain.terrainData.size.x;
                heightModMat.SetFloat("_Width", heightModWidth * ratio);
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
                var nim = RenderTexture.GetTemporary(vd.indexMap.descriptor);
                var nwm = RenderTexture.GetTemporary(vd.weightMap.descriptor);
                splatModMat.SetTexture("_TreeSDF", od.currentObjectSDF);
                splatModMat.SetTexture("_IndexMap", vd.indexMap);
                splatModMat.SetTexture("_WeightMap", vd.weightMap);
                splatModMat.SetFloat("_Amount", layerWeight);
                float ratio = vd.indexMap.width / vd.terrain.terrainData.size.x;
                splatModMat.SetFloat("_Width", layerWidth * ratio);
                int splatIndex = TerrainUtil.FindTextureChannelIndex(vd.terrain, layer);
                splatModMat.SetFloat("_Index", splatIndex);
                RenderBuffer[] _mrt = new RenderBuffer[2];
                _mrt[0] = nim.colorBuffer;
                _mrt[1] = nwm.colorBuffer;
                Graphics.SetRenderTarget(_mrt, nim.depthBuffer);

                Graphics.Blit(null, splatModMat);
                // copy back, this sucks!
                Graphics.Blit(nim, vd.indexMap);
                Graphics.Blit(nwm, vd.weightMap);

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(nim);
                RenderTexture.ReleaseTemporary(nwm);


            }
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
            foreach (var v in returnedRTs.Values)
            {
                if (v.positionWeight != null) RenderTexture.ReleaseTemporary(v.positionWeight);
                if (v.rotationIndex != null) RenderTexture.ReleaseTemporary(v.rotationIndex);
                if (v.scale != null) RenderTexture.ReleaseTemporary(v.scale);
            }
            
            RenderTexture.active = null;
            foreach (var k in sdfs.Values)
            {
                if (k != null)
                {
                    RenderTexture.ReleaseTemporary(k);
                }
            }
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

        public override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
            }
            // set active state of the container to the same as the stamp
            SyncContainerActiveState();
        }

        public override void OnDisable()
        {

            base.OnDisable();

            // set active state of the container to the same as the stamp
            SyncContainerActiveState();
        }

        /// <summary>
        /// Set the active state of the container to the same as the stamp. 
        /// But only inside the editor, ie basically when the user toggles the active checkbox of the stamp in the inspector.
        /// </summary>
        private void SyncContainerActiveState()
        {
            // sync only if microverse is active; this should implicitly consider any stripping
            if (!MicroVerse.instance.isActiveAndEnabled)
                return;

            // sync active state only in editor
            if (!Application.isEditor)
                return;

            // do nothing in play mode
            if (Application.isPlaying)
                return;

            // consider only if we have a container
            if (parentObject == null)  
                return;

#if UNITY_EDITOR
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
                return;

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

            // effectively sync the active state
            if (parentObject.gameObject.activeInHierarchy != this.isActiveAndEnabled)
            {
                parentObject.gameObject.SetActive(this.isActiveAndEnabled);
            }
        }

        public void ApplyObjectClear(ObjectData od)
        {
            
        }

        public bool NeedObjectClear()
        {
            return false;
        }
    }
}
