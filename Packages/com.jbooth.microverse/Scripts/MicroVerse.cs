
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;

/// Main MicroVerse Class
/// 
/// This does all the updating and launches all compute/jobs/etc.
/// The sync is pretty funky between all of these things, mainly because
/// we're trying to not hit the slow ass Unity API every frame. It can take
/// 70ms to update the height map on a single Unity terrain, even though MV
/// can calculate 64 terrains worth of heights, splats, vegetation, splines, etc
/// in 10ms. So essentially we do a bunch of crap to work around things:
///
/// - First frame when loading a scene forces everything to happen at once
/// - Saving forces everything to happen at once
/// - Invalidate is called by anything that might change a frame
/// - Invalidate calls get passed a bounds to clip against, only updating where needed
/// - Modify is the actual work function, but only called once per frame when invalidated
/// - Vegetaion is amortized to resolve when the GPU/Jobs are finished, usually 1-2 frames
/// - "Sneaky Height Saveback" syncronizes the height maps to the terrain only when needed
///   as these are just used for LOD calculations. But get too out of sync and the terrain
///   starts rendering holes, etc. So we do these on mouse up events. They take 70ms per
///   terrain, so we amortize them. But since they use the modifiedTerrains list, we don't
///   start until 2 frames after a mouse up to give vegetation time to finish.
/// - The modifiedTerrains list grows until a sneaky saveback can happen. This makes sure
///   that vegetation is rendered on previous areas (ie: move stamp across 9 terrains, leaves
///   a hole in the original terrain because it's jobs have been canceled). 
/// - A MicroSplat Proxy renderer can be used to avoid terrain updating until sneaky saveback
/// - Everything has to be canceled when moving/sliding a slider, but compute/jobs are not
///   cancelable so we have to throw away their results when canceling. (avoid terrain calls)
///
/// Fixing Unity Terrains and their API would be so much easier than all of this crap.
/// 

namespace JBooth.MicroVerseCore
{

    [ExecuteAlways]
    public partial class MicroVerse : MonoBehaviour
    {
        public Options options = new Options();
        public delegate void TerrainLayersChanged(TerrainLayer[] newLayers);
        public static event TerrainLayersChanged OnTerrainLayersChanged;

        public static UnityEngine.Events.UnityEvent OnFinishedUpdating = new UnityEngine.Events.UnityEvent();
        public static UnityEngine.Events.UnityEvent OnBeginUpdating = new UnityEngine.Events.UnityEvent();
        public static UnityEngine.Events.UnityEvent OnCancelUpdating = new UnityEngine.Events.UnityEvent();
        bool needHoleSync = false;
        int holeCount = 0;
        [Tooltip("You can use this list to explicitly set the terrains instead of having them parented under the MicroVerse object")]
        public Terrain[] explicitTerrains;
        Terrain[] _terrains;
        public Terrain[] terrains
        {
            get
            {
                if (explicitTerrains != null && explicitTerrains.Length > 0)
                {
                    foreach (var t in explicitTerrains)
                    {
                        if (t == null)
                            return _terrains;
                    }
                    return explicitTerrains;
                }
                else
                {
                    return _terrains;
                }
            }
            private set { _terrains = value; }
        }

#if __MICROVERSE_MASKS__
        [Tooltip("Used by the mask module to capture the various buffers that MicroSplat uses and use them elsewhere in your project")]
        public BufferCaptureTarget bufferCaptureTarget;
#endif

        static MicroVerse _instance = null;

#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
        SpawnProcessor spawnProcessor;
#endif


        public static MicroVerse instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                _instance = FindFirstObjectByType<MicroVerse>();
                return _instance;
            }
        }

        private void Awake()
        {
            _instance = this;
            SyncTerrainList();
        }

        Terrain[] GetAllTerrains()
        {
            if (explicitTerrains != null && explicitTerrains.Length > 0)
            {
                return explicitTerrains;
            }
            return GetComponentsInChildren<Terrain>();
        }
        /// <summary>
        /// This is called to make sure the list of MicroVerse
        /// terrains is up to date.
        /// </summary>
        public void SyncTerrainList()
        {
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            if (spawnProcessor == null)
                spawnProcessor = new SpawnProcessor();
#endif

            if (explicitTerrains != null && explicitTerrains.Length > 0)
            {
                bool valid = true;
                foreach (var t in explicitTerrains)
                {
                    if (t == null)
                    {
                        valid = false;
                        Debug.LogError("Explicit terrain list has Null terrain in it, please fix");
                        break;
                    }
                    if (t.drawInstanced == false)
                        t.drawInstanced = true;
                }
                if (valid)
                    return;
            }
            if (options.settings.terrainSearchMethod == Options.Settings.TerrainSearchMethod.Hierarchy)
            {
                terrains = GetComponentsInChildren<Terrain>();
            }
            else
            {
                terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            }
            if (terrains.Length > 0)
            {
                // make sure draw instance is on, we're stupidly slow
                // without it because unity forces CPU readbacks.
                for (int i = 0; i < terrains.Length; ++i)
                {
                    Terrain t = terrains[i];
                    if (t == null)
                    {
                        Debug.LogError("Terrain is null, removing from MicroVerse update");
                        var ts = new List<Terrain>(terrains);
                        ts.RemoveAt(i);
                        terrains = ts.ToArray();
                        i--;
                        continue;
                    }
                    if (t.terrainData == null)
                    {
                        Debug.LogError("Terrain " + t.name + " does not TerrainData and is not a valid Unity terrain, removing from MicroVerse update");
                        var ts = new List<Terrain>(terrains);
                        ts.RemoveAt(i);
                        terrains = ts.ToArray();
                        i--;
                        continue;
                    }
                    if (t.drawInstanced == false)
                        t.drawInstanced = true;
                }
            }

        }

        // don't update more than once per frame.. We can't delay updating,
        // because if we do spline motion isn't smooth

        public enum InvalidateType
        {
            All,
            Splats,
            Tree
        }

        private InvalidateType invalidateType = InvalidateType.All;

        bool needUpdate = false;

        /// <summary>
        /// This gets called to request MicroVerse to update the terrain
        /// but it will only execute once per frame.
        /// </summary>
        /// <param name="type"></param>

        Bounds invalidateBounds;
        Bounds lastInvalidBounds;
        bool boundsSet = false;
        public void Invalidate(Bounds? bounds = null, InvalidateType type = InvalidateType.All)
        {
            if (!boundsSet && bounds != null)
            {
                invalidateBounds = bounds.Value;
                boundsSet = true;
            }
            else if (bounds != null)
            {
                invalidateBounds.Encapsulate(bounds.Value);
            }
            else
            {
                invalidateBounds = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
            }
            
            if (!needUpdate)
                invalidateType = type;
            else
            {
                if (invalidateType != type)
                    invalidateType = InvalidateType.All;
            }
            needUpdate = true;
        }

#if UNITY_EDITOR
        JobHandle batchRaycast;
        List<Stamp> raycastStamps = new List<Stamp>(256);
        NativeArray<RaycastHit> raycastResults;
        NativeArray<RaycastCommand> raycastCommands;
        Vector3 raycastOrigin;
#endif

        
        void Update()
        {

            if (Application.isPlaying)
            {
                if (needUpdate)
                {
                    needUpdate = false;
                    Modify(false, false, true);
                }
#if __MICROVERSE_VEGETATION__
                spawnProcessor.ApplyTrees();
                spawnProcessor.ApplyDetails();
#endif
#if __MICROVERSE_OBJECTS__
                spawnProcessor.ApplyObjects();
                foreach (var td in dataCache.objectDatas.Values)
                {
                    if (td.clearMap != null)
                    {
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(td.clearMap);
                        td.clearMap = null;
                    }
                }
#endif
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
                spawnProcessor.CheckDone();
#endif
#if __MICROVERSE_ROADS__
                foreach (var rj in roadJobs)
                {
                    rj.Item1.ProcessJobs(rj.Item2);
                }
                roadJobs.Clear();
#endif


            }

#if UNITY_EDITOR
            if (UnityEditor.SceneView.lastActiveSceneView != null)
            {
                Profiler.BeginSample("Raycast Gizmo Occlusion");
                GetComponentsInChildren<Stamp>(raycastStamps);
                if (raycastStamps.Count > 0)
                {
                    raycastResults = new NativeArray<RaycastHit>(raycastStamps.Count, Allocator.TempJob);

                    raycastCommands = new NativeArray<RaycastCommand>(raycastStamps.Count, Allocator.TempJob);
                    if (UnityEditor.SceneView.lastActiveSceneView != null)
                    {
                        var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
                        if (sceneCam != null)
                        {
                            raycastOrigin = sceneCam.transform.position;
                            for (int i = 0; i < raycastStamps.Count; ++i)
                            {
                                var stamp = raycastStamps[i];
                                Vector3 worldPos = stamp.transform.position;
                                worldPos.y += stamp.transform.lossyScale.y;
#if UNITY_2022_2_OR_NEWER
                            raycastCommands[i] = new RaycastCommand(raycastOrigin, (worldPos - raycastOrigin).normalized, QueryParameters.Default);
#else
                                raycastCommands[i] = new RaycastCommand(raycastOrigin, (worldPos - raycastOrigin).normalized);
#endif
                            }

                            batchRaycast = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, 1);
                        }
                    }
                }
                Profiler.EndSample();
            }

#endif
        }


        public void LateUpdate()
        {
            if (IsModifyingTerrain)
            {
                bool mod = false;
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
                if (SpawnProcessor.IsModifyingTerrain)
                    mod = true;
#endif
                if (!mod)
                {
                    IsModifyingTerrain = false;
                    boundsSet = false;
                }
            }

#if UNITY_EDITOR
            Profiler.BeginSample("Resolve Gizmo Raycast Occlusions");
            if (raycastStamps.Count > 0)
            {
                batchRaycast.Complete();
                for (int i = 0; i < raycastStamps.Count; ++i)
                {
                    var stamp = raycastStamps[i];
                    var hit = raycastResults[i];
                    if (hit.collider != null)
                    {
                        Vector3 worldPos = stamp.transform.position;
                        worldPos.y += stamp.transform.lossyScale.y;
                        stamp.gizmoVisible = ((hit.point - raycastOrigin).magnitude > (worldPos - raycastOrigin).magnitude);
                    }
                }
                raycastResults.Dispose();
                raycastCommands.Dispose();
            }
            Profiler.EndSample();
#endif
        }



        bool firstUpdate = false;

        private void OnEnable()
        {
            firstUpdate = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += OnEditorUpdate;
            UnityEditor.EditorApplication.hierarchyChanged += OnHierarchyChanged;

            if (!Application.isPlaying)
            {

                SyncTerrainList();

#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                if (proxyRenderMode == ProxyRenderMode.AlwaysProxy)
                {
                    DisableProxyRenderer();
                    EnableProxyRenderer();
                }
#endif
                //Modify(true); // causes issues when entering play mode, since it gets fired them
            }
#endif
        }




        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= OnEditorUpdate;
            UnityEditor.EditorApplication.hierarchyChanged -= OnHierarchyChanged;
#endif
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            DisableProxyRenderer();
#endif

            _instance = null;
        }

        bool _isHeightSyncd;
        public bool IsHeightSyncd
        {
            get { return _isHeightSyncd; }
            private set
            {
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                if (value == true && _isHeightSyncd == false)
                {
                    if (proxyRenderMode == ProxyRenderMode.ProxyWhileUpdating)
                    {
                        IsUsingProxyRenderer = false;
                    }
                }
#endif
                _isHeightSyncd = value;
            }
        }
        private bool _isModifyingTerrain = false;
        public bool IsModifyingTerrain {
            get { return _isModifyingTerrain; }
            set
            {
                var old = _isModifyingTerrain;
                _isModifyingTerrain = value;
                if (value == false && old == true && OnFinishedUpdating != null)
                {
                    OnFinishedUpdating.Invoke();
                }
            }
        }

        /// <summary>
        /// Flag which prevents syncing back to the terrain. 
        /// It's being invoked from the content browser's drag handler on drag start
        /// and disabled again when dragging is finished.
        /// Used to prevent raycasts of the drag handler against the currently added height stamp
        /// </summary>
        private bool _isAddingHeightStamp = false;
        public bool IsAddingHeightStamp
        {
            get { return _isAddingHeightStamp; }
            set { _isAddingHeightStamp = value; }
        }

        /// <summary>
        /// This gets called when the height has been changed by something
        /// to sync the data back from the GPU to the CPU for physics and such
        /// </summary>
        ///

        void RequestHeightSaveback()
        {
            if (!IsHeightSyncd)
            {
                if (modifiedTerrains.Count > 0)
                {
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                    if (IsUsingProxyRenderer &&  proxyRenderMode == ProxyRenderMode.AlwaysProxy)
                    {
                        modifiedTerrains.Clear();
                        IsHeightSyncd = true;
                        return;
                    }
#endif
                    Profiler.BeginSample("Sync Height Map back to CPU");
                    int count = options.settings.maxHeightSaveBackPerFrame;
                    if (count < 1)
                        count = 1;
                    if (count > modifiedTerrains.Count)
                        count = modifiedTerrains.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        var t = modifiedTerrains[0];
                        modifiedTerrains.RemoveAt(0);
                        t.terrainData.SyncHeightmap();
                    }
                    Profiler.EndSample();


                    if (modifiedTerrains.Count == 0)
                    {
                        IsHeightSyncd = true;
                    }
                }
            }
        }
        /// <summary>
        /// Save everything back to the terrain, which is slow, because unity
        /// stores alpha maps as arrays instead of textures and uses a
        /// bloated 4 weights per texture format. Don't even talk to me about detail
        /// maps.
        /// </summary>
        public void SaveBackToTerrain(bool forceFinishSpawnProcssing = false)
        {
            Profiler.BeginSample("SyncBackTerrain");
            SyncTerrainList();

#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            if (forceFinishSpawnProcssing)
            {
#if __MICROVERSE_VEGETATION__
                spawnProcessor.ApplyTrees();
                spawnProcessor.ApplyDetails();
#endif
#if __MICROVERSE_OBJECTS__
                spawnProcessor.ApplyObjects();
#endif
            }
#endif

            foreach (var terrain in terrains)
            {
                terrain.terrainData.SyncTexture(TerrainData.AlphamapTextureName);
                terrain.terrainData.SyncHeightmap();
                
                if (needHoleSync)
                {
                    terrain.terrainData.SyncTexture(TerrainData.HolesTextureName);
                    
                }
                
            }
            needHoleSync = false;
            modifiedTerrains.Clear();
            
            IsHeightSyncd = true;

            Profiler.EndSample();
            
        }


        bool DoTerrainLayersMatch(TerrainLayer[] a, TerrainLayer[] b)
        {
            if (a.Length != b.Length) { return false; }
            for (int i = 0; i < a.Length; ++i)
            {
                if (!ReferenceEquals(a[i], b[i]))
                    return false;

            }
            return true;
        }

        /// <summary>
        /// Syncs terrain layers across all terrains and lets external
        /// programs know if they've been changed.
        /// </summary>
        /// <param name="splatmapModifiers"></param>
        void SanatizeTerrainLayers(List<ITextureModifier> splatmapModifiers, Terrain[] allTerrains)
        {
            List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
            foreach (var terrain in terrains)
            {
                foreach (var sm in splatmapModifiers)
                {
                    sm.InqTerrainLayers(terrain, terrainLayers);
                }
            }
            terrainLayers.RemoveAll(item => item == null);
            var allLayers = terrainLayers.Distinct().OrderBy(x=>x?.name).ToArray();
            
            bool needsUpdate = false;
            foreach (var terrain in allTerrains)
            {
                if (!DoTerrainLayersMatch(allLayers, terrain.terrainData.terrainLayers))
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.terrainLayers = allLayers;
                }
            }
            if (OnTerrainLayersChanged != null)
                OnTerrainLayersChanged.Invoke(allLayers);

        }


        public class DataCache
        {
            public Dictionary<Terrain, RenderTexture> heightMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> normalMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, OcclusionData> occlusionDatas = new Dictionary<Terrain, OcclusionData>();
            public Dictionary<Terrain, RenderTexture> indexMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> weightMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> curvatureMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> flowMaps = new Dictionary<Terrain, RenderTexture>();
            public Dictionary<Terrain, RenderTexture> holeMaps = new Dictionary<Terrain, RenderTexture>();
#if __MICROVERSE_VEGETATION__
            public Dictionary<Terrain, TreeData> treeDatas = new Dictionary<Terrain, TreeData>();
            public Dictionary<Terrain, DetailData> detailDatas = new Dictionary<Terrain, DetailData>();
#endif
#if __MICROVERSE_OBJECTS__
            public Dictionary<Terrain, ObjectData> objectDatas = new Dictionary<Terrain, ObjectData>();
#endif

        }
         
        void SeamHeightMaps(DataCache dataCache)
        {
            Profiler.BeginSample("MicroVerse::HeightSeamer");
            // Not a huge fan of this, but there are a lot of resolution dependent
            // issues that might cause tiny cracks in the terrain, so seem them up
            if (heightSeamShader == null)
            {
                heightSeamShader = (ComputeShader)Resources.Load("MicroVerseHeightSeamer");
            }
            foreach (var terrain in terrains)
            {
                if (terrain.leftNeighbor != null && terrains.Contains(terrain.leftNeighbor))
                {
                    int kernelHandle = heightSeamShader.FindKernel("CSLeft");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeamShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeamShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.leftNeighbor]);
                    heightSeamShader.SetInt("_Width", hm.width - 1);
                    heightSeamShader.SetInt("_Height", hm.height - 1);

                    heightSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.height / 512.0f), 1, 1);
                }
                if (terrain.rightNeighbor != null && terrains.Contains(terrain.rightNeighbor))
                {
                    int kernelHandle = heightSeamShader.FindKernel("CSRight");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeamShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeamShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.rightNeighbor]);
                    heightSeamShader.SetInt("_Width", hm.width - 1);
                    heightSeamShader.SetInt("_Height", hm.height - 1);

                    heightSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.height / 512.0f), 1, 1);
                }
                if (terrain.topNeighbor != null && terrains.Contains(terrain.topNeighbor))
                {
                    int kernelHandle = heightSeamShader.FindKernel("CSUp");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeamShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeamShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.topNeighbor]);
                    heightSeamShader.SetInt("_Width", hm.width - 1);
                    heightSeamShader.SetInt("_Height", hm.height - 1);

                    heightSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.width / 512.0f), 1, 1);
                }
                if (terrain.bottomNeighbor != null && terrains.Contains(terrain.bottomNeighbor))
                {
                    int kernelHandle = heightSeamShader.FindKernel("CSDown");
                    var hm = dataCache.heightMaps[terrain];
                    heightSeamShader.SetTexture(kernelHandle, "_Terrain", hm);
                    heightSeamShader.SetTexture(kernelHandle, "_Neighbor", dataCache.heightMaps[terrain.bottomNeighbor]);
                    heightSeamShader.SetInt("_Width", hm.width - 1);
                    heightSeamShader.SetInt("_Height", hm.height - 1);

                    heightSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(hm.width / 512.0f), 1, 1);
                }
            }
            Profiler.EndSample();
        }

        static ComputeShader heightSeamShader = null;
        
        float FindIndex(TerrainLayer[] protos, TerrainLayer layer)
        {
            for (int i = 0; i < protos.Length; ++i)
            {
                if (protos[i] == layer)
                { 
                    return (float)i;
                }
            }  
             
            return -1;   
        }

        static int _Mapping = Shader.PropertyToID("_Mapping");
        float[] indexRemap = new float[32];
        GraphicsBuffer indexRemapBuffer = null;
        void MapIndecies(int kernelIndex, Terrain terrain, Terrain neighbor)
        {
            var terrainProtos = terrain.terrainData.terrainLayers;
            var neighborProtos = neighbor.terrainData.terrainLayers;
            int count = neighborProtos.Length;
            for (int i = 0; i < count; ++i)
            {
                indexRemap[i] = FindIndex(terrainProtos, neighborProtos[i]);
            }
            indexRemapBuffer.SetData(indexRemap);
            alphaSeamShader.SetBuffer(kernelIndex, _Mapping, indexRemapBuffer);
        }

        static int _TerrainIndex = Shader.PropertyToID("_TerrainIndex");
        static int _TerrainWeight = Shader.PropertyToID("_TerrainWeight");
        static int _NeighborIndex = Shader.PropertyToID("_NeighborIndex");
        static int _NeighborWeight = Shader.PropertyToID("_NeighborWeight");
        static int _Width = Shader.PropertyToID("_Width");
        static int _Height = Shader.PropertyToID("_Height");

        void SeamAlphaMaps(DataCache dataCache)
        { 
            Profiler.BeginSample("MicroVerse::AlphaSeamer");
            // Not a huge fan of this, but there are a lot of resolution dependent
            // issues that might cause tiny cracks in the terrain, so seam them up.
            // Note that when using regular unity terrain (Not MicroSplat), we have to deal
            // with the texture order of each terrain being potentially different, so a simple
            // copy of indexes/weights between edges is not enough.
            //
            // Annoying- wasted several hours because SetFloats on a compute buffer doesn't work,
            // and you have to use a graphics buffer instead. WTF..
            
            if (alphaSeamShader == null)
            {
                alphaSeamShader = (ComputeShader)Resources.Load("MicroVerseAlphaSeamer");
            }
            if (indexRemapBuffer == null)
            {
                indexRemapBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 32, 4);
            }
            foreach (var terrain in terrains)
            {
                if (dataCache.indexMaps[terrain] == null)
                    continue;
                if (terrain.leftNeighbor != null && terrains.Contains(terrain.leftNeighbor) && dataCache.indexMaps[terrain.leftNeighbor] != null)
                {
                    
                    int kernelHandle = alphaSeamShader.FindKernel("CSLeft");
                    MapIndecies(kernelHandle, terrain, terrain.leftNeighbor);
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainIndex, dataCache.indexMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainWeight, dataCache.weightMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborIndex, dataCache.indexMaps[terrain.leftNeighbor]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborWeight, dataCache.weightMaps[terrain.leftNeighbor]);
                    alphaSeamShader.SetInt(_Width, dataCache.indexMaps[terrain].width - 1);
                    alphaSeamShader.SetInt(_Height, dataCache.indexMaps[terrain].height - 1);

                    alphaSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(dataCache.indexMaps[terrain].height / 512.0f), 1, 1);
                }
                if (terrain.rightNeighbor != null && terrains.Contains(terrain.rightNeighbor) && dataCache.indexMaps[terrain.rightNeighbor] != null)
                {
                    int kernelHandle = alphaSeamShader.FindKernel("CSRight");
                    MapIndecies(kernelHandle, terrain, terrain.rightNeighbor);
                    
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainIndex, dataCache.indexMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainWeight, dataCache.weightMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborIndex, dataCache.indexMaps[terrain.rightNeighbor]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborWeight, dataCache.weightMaps[terrain.rightNeighbor]);
                    alphaSeamShader.SetInt(_Width, dataCache.indexMaps[terrain].width - 1);
                    alphaSeamShader.SetInt(_Height, dataCache.indexMaps[terrain].height - 1);

                    alphaSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(dataCache.indexMaps[terrain].height / 512.0f), 1, 1);
                }
                if (terrain.topNeighbor != null && terrains.Contains(terrain.topNeighbor) && dataCache.indexMaps[terrain.topNeighbor] != null)
                {
                    int kernelHandle = alphaSeamShader.FindKernel("CSUp");
                    MapIndecies(kernelHandle, terrain, terrain.topNeighbor);
                    
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainIndex, dataCache.indexMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainWeight, dataCache.weightMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborIndex, dataCache.indexMaps[terrain.topNeighbor]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborWeight, dataCache.weightMaps[terrain.topNeighbor]);
                    alphaSeamShader.SetInt(_Width, dataCache.indexMaps[terrain].width - 1);
                    alphaSeamShader.SetInt(_Height, dataCache.indexMaps[terrain].height - 1);

                    alphaSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(dataCache.indexMaps[terrain].height / 512.0f), 1, 1);
                }
                if (terrain.bottomNeighbor != null && terrains.Contains(terrain.bottomNeighbor) && dataCache.indexMaps[terrain.bottomNeighbor] != null)
                {
                    int kernelHandle = alphaSeamShader.FindKernel("CSDown");
                    MapIndecies(kernelHandle, terrain, terrain.bottomNeighbor);
                    
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainIndex, dataCache.indexMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _TerrainWeight, dataCache.weightMaps[terrain]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborIndex, dataCache.indexMaps[terrain.bottomNeighbor]);
                    alphaSeamShader.SetTexture(kernelHandle, _NeighborWeight, dataCache.weightMaps[terrain.bottomNeighbor]);
                    alphaSeamShader.SetInt(_Width, dataCache.indexMaps[terrain].width - 1);
                    alphaSeamShader.SetInt(_Height, dataCache.indexMaps[terrain].height - 1);

                    alphaSeamShader.Dispatch(kernelHandle, Mathf.CeilToInt(dataCache.indexMaps[terrain].height / 512.0f), 1, 1);
                }
            }
            indexRemapBuffer.Release();
            indexRemapBuffer = null;
            Profiler.EndSample();
        }

        void CullTerrainList(bool boundsCull)
        {

            Profiler.BeginSample("Cull Terrains");
            
            // filter terrains by bounds
            if (boundsCull)
            {
                Profiler.BeginSample("Cull terrain list to edit bounds");
                List<Terrain> cullTerrains = new List<Terrain>(terrains.Length);
                Bounds testBounds = invalidateBounds;
                if (lastInvalidBounds.size.x < 99999)
                {
                    testBounds.Encapsulate(lastInvalidBounds);
                }
                lastInvalidBounds = invalidateBounds;

                for (int i = 0; i < terrains.Length; ++i)
                {
                    Bounds terBounds = TerrainUtil.ComputeTerrainBounds(terrains[i]);
                    if (terBounds.Intersects(testBounds))
                    {
                        cullTerrains.Add(terrains[i]);
                    }
                }
                terrains = cullTerrains.ToArray();

                Profiler.EndSample();
            }

            if (modifiedTerrains.Count == 0)
            {
                modifiedTerrains = new List<Terrain>(terrains);
            }
            else
            {
                if (invalidateType == InvalidateType.All)
                {
                    foreach (var t in terrains)
                    {
                        if (!modifiedTerrains.Contains(t))
                        {
                            modifiedTerrains.Add(t);
                        }
                    }
                }
            }
            Profiler.EndSample();

        }

#if __MICROVERSE_ROADS__
        List<(Road, RoadSystem)> roadJobs = new List<(Road, RoadSystem)>();
        List<RoadSystem> roadSystems = new List<RoadSystem>();
        Bounds? roadUpdateBounds = null;
        public void AddRoadJob(Road road, RoadSystem rs, Bounds b)
        {
            if (!roadJobs.Contains((road, rs)))
            {
                roadJobs.Add((road, rs));
            }
            if (!roadSystems.Contains(rs))
            {
                roadSystems.Add(rs);
            }
            if (roadUpdateBounds == null)
            {
                roadUpdateBounds = b;
            }
            else
            {
                roadUpdateBounds.Value.Encapsulate(b);
            }
        }
#endif

        static ComputeShader alphaSeamShader = null;


        // tree density and noises used to be based on terrain size. Now they are normalized to
        // 1000 meters, so we need to revision old data. Since old data might be in a prefab
        // that's getting dragged into the scene, we need to do this on every change. 
        void RevisionAllStamps()
        {
#if UNITY_EDITOR
            if (terrains.Length > 0 && terrains[0] != null && terrains[0].terrainData != null)
            {
                Profiler.BeginSample("Revision stamp data");
                var stamps = GetComponentsInChildren<Stamp>(true);
                foreach (var s in stamps)
                {
                    if (s.stampVersion == 0)
                    {
                        float scale = terrains[0].terrainData.size.x / Stamp.terrainReferenceSize;
                        var f = s.GetFilterSet();
                        if (f != null)
                            f.ScaleAllNoises(1.0f / scale);
#if __MICROVERSE_VEGETATION__
                        TreeStamp ts = s as TreeStamp;
                        if (ts != null)
                        {
                            ts.density *= 1.0f / scale;
                        }
#endif
                        
                        s.stampVersion = 1;
                        UnityEditor.EditorUtility.SetDirty(s.gameObject);
                        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(s))
                            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(s);
                    }
                }
                Profiler.EndSample();

            }
#endif
        }



        public static bool noAsyncReadback { get; private set; }
        /// <summary>
        /// This is the actual function that does updates to the terrain.
        /// If you call it directly it will update all the height/splat maps
        /// immediately, but tree's and details are deferred due to Unity's
        /// terrible terrain API and GPU readback. You can request it
        /// to write immediately back to the CPU in the case of height and
        /// splats, which will make it slower, or force no async readbacks
        /// which will make it complete immediately but be really slow
        /// </summary>
        /// <param name="writeToCPU"></param>
        ///
        List<IModifier> allModifiers = new List<IModifier>(256);
        List<IHeightModifier> heightmapModifiers = new List<IHeightModifier>(64);
        List<ITextureModifier> splatmapModifiers = new List<ITextureModifier>(64);
        List<IHoleModifier> holeModifiers = new List<IHoleModifier>(16);
        DataCache dataCache = null;
        List<Terrain> modifiedTerrains = new List<Terrain>();
        public void Modify(bool writeToCPU = false, bool noAsync = false, bool boundsCull = false)
        {
            noAsyncReadback = noAsync;
            if (!enabled)
            {
                return;
            }

            RevisionAllStamps();

            IsModifyingTerrain = true;
            CancelModify(false);
            

            if (OnBeginUpdating != null)
                OnBeginUpdating.Invoke();

#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            if (proxyRenderMode == ProxyRenderMode.ProxyWhileUpdating && !noAsync)
            {
                IsUsingProxyRenderer = true;
            }
#endif
            
            Profiler.BeginSample("MicroVerse::Modify Terrain");
            Profiler.BeginSample("Sync/Cull Terrain List");
            IsHeightSyncd = false;
            SyncTerrainList();
            var allTerrains = terrains;
            CullTerrainList(boundsSet);
            
            Profiler.EndSample();
            if (terrains.Length == 0)
            {
                CancelModify();
                Profiler.EndSample();
                return;
            }

            Profiler.BeginSample("Init");
            Profiler.BeginSample("Find Stamps");

            GetComponentsInChildren<IModifier>(allModifiers);
            heightmapModifiers.Clear();
            splatmapModifiers.Clear();
            holeModifiers.Clear();
            // filtering is faster than finding them. Note that when MS is enabled we
            // want textures to remain the same when objects are disabled, so we
            // have to scan in that case.
            if (IsUsingMicroSplat())
            {
                for (int i = 0; i < allModifiers.Count; ++i)
                {
                    var m = allModifiers[i];
                    if (m is IHeightModifier && m.IsEnabled())
                    {
                        heightmapModifiers.Add(m as IHeightModifier);
                    }
                }
                GetComponentsInChildren<ITextureModifier>(true, splatmapModifiers);
                GetComponentsInChildren<IHoleModifier>(true, holeModifiers);
            }
            else
            {
                for (int i = 0; i < allModifiers.Count; ++i)
                {
                    var m = allModifiers[i];
                    if (m is IHeightModifier && m.IsEnabled())
                    {
                        heightmapModifiers.Add(m as IHeightModifier);
                    }
                    if (m is ITextureModifier && m.IsEnabled())
                    {
                        splatmapModifiers.Add(m as ITextureModifier);
                    }
                    if (m is IHoleModifier && m.IsEnabled())
                    {
                        var hm = m as IHoleModifier;
                        if (hm.IsValidHoleStamp())
                        {
                            holeModifiers.Add(m as IHoleModifier);
                        }
                    }
                }
            }

            // remove all with disabled components, this lets us
            // have meta-modifiers which pipe through to disabled components.
            allModifiers.RemoveAll(p => p.IsEnabled() == false);
            allModifiers = allModifiers.Distinct().ToList();
           
            Profiler.EndSample();
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            spawnProcessor.InitSystem();
#endif

#if __MICROVERSE_ROADS__
            // clear any spline data which needs to be regenerated
            if (roadUpdateBounds != null)
            {
                foreach (var rs in roadSystems)
                {
                    var sp = rs.GetComponent<SplinePath>();
                    if (sp != null)
                    {
                        sp.ClearSplineRenders(roadUpdateBounds);
                    }
                }
            }
            roadUpdateBounds = null;
            roadSystems.Clear();

#endif

            Profiler.BeginSample("Modify::InitModifiers");
            foreach (var m in allModifiers) { m.Initialize(); }
            Profiler.EndSample();

            if (options.settings.keepLayersInSync || IsUsingMicroSplat())
            {
                Profiler.BeginSample("Modify::SanitizeTerrainLayers");
                SanatizeTerrainLayers(splatmapModifiers, allTerrains);
                Profiler.EndSample();
            }
            else
            {
                Profiler.BeginSample("Modify::GetLayers");
                List<TerrainLayer> terrainLayers = new List<TerrainLayer>(256);
                foreach (var terrain in terrains)
                {
                    var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                    foreach (var sm in splatmapModifiers)
                    {
                        if (terrainBounds.Intersects(sm.GetBounds()))
                        {
                            sm.InqTerrainLayers(terrain, terrainLayers);
                        }
                    }
                    terrain.terrainData.terrainLayers = terrainLayers.Distinct().ToArray();
                    terrainLayers.Clear();
                }
                Profiler.EndSample();
            }
            // we strip these after getting the terrain layers. This lets you "reserve"
            // textures in an array based shader, means the layers don't get removed when
            // you toggle a component on and off (requiring an array rebuild), but may
            // mean people leaving disabled components around end up with more textures
            // on their terrain.
            splatmapModifiers.RemoveAll(p => p.IsEnabled() == false);

            // if we need curvature at all, we need it for all due to boundaries
            bool needCurvatureMap = false;
            bool needFlowMap = false;

#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (bufferCaptureTarget != null && bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CurvatureMap))
            {
                needCurvatureMap = true;
            }

            if (bufferCaptureTarget != null && bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.FlowMap))
            {
                needFlowMap = true;
            }
#endif

            Profiler.BeginSample("Modify::Scan Modifiers for Flags");
            // Grab all the assets needed from the modifiers
            // and make sure the data is on the terrain
            foreach (var terrain in terrains)
            {
                foreach (var sm in splatmapModifiers)
                {
                    needCurvatureMap |= sm.NeedCurvatureMap();
                    needFlowMap |= sm.NeedFlowMap();
                }
                foreach (var hm in holeModifiers)
                {
                    needCurvatureMap |= hm.NeedCurvatureMap();
                    needFlowMap |= hm.NeedFlowMap();
                }

#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
                Profiler.BeginSample("Modify::SpawnProcessor::Init");
                spawnProcessor.InitTerrain(terrain, invalidateType, ref needCurvatureMap, ref needFlowMap);
                Profiler.EndSample();
#endif

            }
            Profiler.EndSample(); // scan
            Profiler.EndSample(); // init

            Profiler.BeginSample("Modify::HeightMaps");
            dataCache = new DataCache();
            int heightMapRes = terrains[0].terrainData.heightmapResolution;
            int splatMapRes = terrains[0].terrainData.alphamapResolution;
            var maskSize = heightMapRes - 1;
            if (splatMapRes > maskSize)
                maskSize = splatMapRes;

            int odSize = maskSize;
            
            if (odSize > 1024)
                odSize = 1024;
            if (odSize < 512)
                odSize = 512;

            
            // do height maps
            foreach (var terrain in terrains)
            {
                var hmd = new HeightmapData(terrain);
                Vector3 rs = new Vector3(hmd.RealSize.x, hmd.RealHeight, hmd.RealSize.y);
                var tbs = terrain.terrainData.bounds;
                tbs.center = terrain.transform.position;
                tbs.center += new Vector3(tbs.size.x * 0.5f, 0, tbs.size.z * 0.5f);
                var od = new OcclusionData(terrain, odSize);
                dataCache.occlusionDatas.Add(terrain, od);
                dataCache.heightMaps.Add(terrain, GenerateHeightmap(hmd, heightmapModifiers, tbs, od));
            }
            Profiler.EndSample();

            // we have to seam twice - once before we generate normals and curvature
            // Then again after tree's happen, such that the data is seamed before
            // either operations.
            SeamHeightMaps(dataCache);


            // generate normals
            Profiler.BeginSample("Modify::GenerateNormals");
            foreach (var terrain in terrains)
            {
                dataCache.normalMaps.Add(terrain, MapGen.GenerateNormalMap(terrain, dataCache.heightMaps, heightMapRes, heightMapRes));
            }
            Profiler.EndSample();

            foreach (var terrain in terrains)
            {
                Profiler.BeginSample("Generate::CurveMaps");
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                var occlusionData = dataCache.occlusionDatas[terrain];
                var heightmapData = new HeightmapData(terrain);
                Vector3 realSize = new Vector3(heightmapData.RealSize.x, heightmapData.RealHeight, heightmapData.RealSize.y);

                RenderTexture curvatureGen = needCurvatureMap ? MapGen.GenerateCurvatureMap(terrain, dataCache.normalMaps, splatMapRes, splatMapRes) : null;
                dataCache.curvatureMaps[terrain] = curvatureGen;
                Profiler.EndSample();

                Profiler.BeginSample("Generate::FlowMaps");
                RenderTexture flowGen = needFlowMap ? MapGen.GenerateFlowMap(terrain, dataCache.heightMaps) : null;
                dataCache.flowMaps[terrain] = flowGen;
                Profiler.EndSample();


                Profiler.BeginSample("Modify::SplatMaps");
                var heightmapGen = dataCache.heightMaps[terrain];
                var normalGen = dataCache.normalMaps[terrain];

                var splatmapData = new TextureData(terrain, 0, heightmapGen, normalGen, curvatureGen, flowGen);
                GenerateSplatmaps(splatmapData, splatmapModifiers, terrainBounds, occlusionData);
                dataCache.indexMaps[terrain] = splatmapData.indexMap;
                dataCache.weightMaps[terrain] = splatmapData.weightMap;
                Profiler.EndSample();
            }

            if (holeModifiers.Count > 0)
            {
                Profiler.BeginSample("Modify::HoleStamps");
                holeCount = holeModifiers.Count;
                foreach (var terrain in terrains)
                {
                    var od = dataCache.occlusionDatas[terrain];
                    var holeData = new HoleData(terrain, dataCache.heightMaps[terrain],
                        dataCache.normalMaps[terrain], dataCache.curvatureMaps[terrain],
                        dataCache.flowMaps[terrain],
                        dataCache.indexMaps[terrain], dataCache.weightMaps[terrain]);

                    var format = Terrain.holesRenderTextureFormat;
                    var res = terrain.terrainData.holesResolution;
                    RenderTexture holeA = RenderTexture.GetTemporary(res, res, 0, format, RenderTextureReadWrite.Linear);
                    RenderTexture holeB = RenderTexture.GetTemporary(res, res, 0, format, RenderTextureReadWrite.Linear);
                    RenderTexture.active = holeA;
                    GL.Clear(false, true, Color.white);
                    foreach (var hm in holeModifiers)
                    {
                        if (hm.IsValidHoleStamp() && hm.IsEnabled())
                        {
                            hm.ApplyHoleStamp(holeA, holeB, holeData, od);
                            (holeA, holeB) = (holeB, holeA);
                        }
                    }
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(holeB);
                    
                    dataCache.holeMaps.Add(terrain, holeA);
                }
                Profiler.EndSample();
            }
            else if (holeCount > 0)
            {
                holeCount = 0;
                needHoleSync = false;
                // last hole removed, repair..
                foreach (var terrain in terrains)
                {
                    var holeData = new HoleData(terrain, dataCache.heightMaps[terrain],
                        dataCache.normalMaps[terrain], dataCache.curvatureMaps[terrain],
                        dataCache.flowMaps[terrain],
                        dataCache.indexMaps[terrain], dataCache.weightMaps[terrain]);

                    var format = Terrain.holesRenderTextureFormat;
                    var res = terrain.terrainData.holesResolution;
                    RenderTexture hole = RenderTexture.GetTemporary(res, res, 0, format);
                    RenderTexture.active = hole;
                    GL.Clear(false, true, Color.white);
                    terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.HolesTextureName, 0, new RectInt(0, 0, hole.width, hole.height),
                        new Vector2Int(0, 0), true);
                }
                RenderTexture.active = null;
            }
#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (needCurvatureMap && bufferCaptureTarget != null &&
                bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CurvatureMap))
            {
                foreach (var terrain in terrains)
                {
                    var nm = dataCache.curvatureMaps[terrain];
                    bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.CurvatureMap, nm);
                }
            }

            if (needFlowMap && bufferCaptureTarget != null &&
                bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.FlowMap))
            {
                foreach (var terrain in terrains)
                {
                    var nm = dataCache.flowMaps[terrain];
                    bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.FlowMap, nm);
                }
            }
#endif

#if __MICROVERSE_ROADS__
            for (int i = 0; i < roadJobs.Count; ++i)
            {
                var rj = roadJobs[i];
                rj.Item1.LaunchJobs(rj.Item2);
            }
#endif


#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            spawnProcessor.GenerateSpawnables(terrains, dataCache);

#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (bufferCaptureTarget != null)
            {
                if (bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CombinedTreeSDF))
                {
                    foreach (var terrain in terrains)
                    {
                        var nm = dataCache.occlusionDatas[terrain]?.treeSDF;
                        if (nm != null)
                            bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.CombinedTreeSDF, nm);
                    }
                }
                if (bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.CombinedOcclusionMask))
                {
                    foreach (var terrain in terrains)
                    {
                        var nm = dataCache.occlusionDatas[terrain]?.terrainMask;
                        if (nm != null)
                            bufferCaptureTarget.SaveRenderData(terrain, BufferCaptureTarget.BufferCapture.CombinedOcclusionMask, nm);
                    }
                }
            }
#endif // UNITY_EDITOR && __MICROVERSE_MASKS__

#endif // vegetation

#if __MICROVERSE_MASKS__
            MaskData.ProcessMasks(terrains, dataCache);
#endif

            SeamHeightMaps(dataCache);
            SeamAlphaMaps(dataCache);

            foreach (var terrain in terrains)
            {
                if (holeModifiers.Count > 0)
                {
                    Profiler.BeginSample("Modify::CopyHolesToTerrain");
                    var holeMap = dataCache.holeMaps[terrain];
                    RenderTexture.active = holeMap;

                    // unity will crash a lot if we defer this.. so ugh..
                    //terrain.terrainData.enableHolesTextureCompression = false;
                    terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.HolesTextureName, 0, new RectInt(0, 0, holeMap.width, holeMap.height),
                        new Vector2Int(0, 0), !writeToCPU);
                    
                    
                    //
                    needHoleSync = !writeToCPU;

                    Profiler.EndSample();

#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                    if (IsUsingProxyRenderer)
                    {
                        UpdateHolemap(terrain, holeMap);
                    }
                    else
                    {
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(holeMap);
                    }
#else
                    RenderTexture.ReleaseTemporary(holeMap);
#endif
                }
                Profiler.BeginSample("Modify::Raster Splats");
                var indexMap = dataCache.indexMaps[terrain];
                var weightMap = dataCache.weightMaps[terrain];
                var heightmapGen = dataCache.heightMaps[terrain];
                var normalGen = dataCache.normalMaps[terrain];
                var curvatureGen = dataCache.curvatureMaps[terrain];
                var flowGen = dataCache.flowMaps[terrain];
                var occlusionData = dataCache.occlusionDatas[terrain];
                if (invalidateType != InvalidateType.Tree)
                {
                    RasterizeSplatMaps(terrain, indexMap, weightMap, writeToCPU);
                }
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(indexMap);
                RenderTexture.ReleaseTemporary(weightMap);
                if (invalidateType == InvalidateType.All)
                {
                    Profiler.BeginSample("Modify::CopyHeightMapToTerrain");
                    RenderTexture.active = heightmapGen;
                    terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, heightmapGen.width, heightmapGen.height),
                        new Vector2Int(0, 0), writeToCPU ? TerrainHeightmapSyncControl.HeightAndLod : TerrainHeightmapSyncControl.None);
                    Profiler.EndSample();
                }
                RenderTexture.active = null;
                if (flowGen != null) RenderTexture.ReleaseTemporary(flowGen);
                if (curvatureGen != null) RenderTexture.ReleaseTemporary(curvatureGen);
                
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                if (IsUsingProxyRenderer)
                {
                    UpdateProxyHeightmap(terrain, heightmapGen);
                    UpdateProxyNormalmap(terrain, normalGen);
                }
                else
                {
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(normalGen);
                    RenderTexture.ReleaseTemporary(heightmapGen);
                }
#else
                RenderTexture.ReleaseTemporary(normalGen);
                RenderTexture.ReleaseTemporary(heightmapGen);
#endif
                Profiler.EndSample();

                occlusionData.Dispose();
            }



            foreach (var h in allModifiers) { h.Dispose(); }

            if (firstUpdate)
            {
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.SyncHeightmap();
                }
                modifiedTerrains.Clear();
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                if (proxyRenderMode == ProxyRenderMode.ProxyWhileUpdating)
                {
                    IsUsingProxyRenderer = false;
                }
#endif
                firstUpdate = false;
            }

            Profiler.EndSample(); // all terrains
            

        }

        /// <summary>
        /// stops a modify operation in progress. Note that it cannot
        /// stop the vegetation CPU readback once it's started, because
        /// you cannot cancel async GPU readbacks or jobs, so if you
        /// start/stop a bunch of times in a frame those will build up.
        /// </summary>
        public void CancelModify(bool cancelRoads = true)
        {
            if (OnCancelUpdating != null)
                OnCancelUpdating.Invoke();
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            spawnProcessor.Cancel(dataCache);
#endif

#if __MICROVERSE_ROADS__
            if (cancelRoads)
            {
                foreach (var rj in roadJobs)
                {
                    rj.Item1.CancelJobs();
                }
                roadJobs.Clear();
            }
#endif

        }


        private static void GenerateSplatmaps(TextureData splatmapData,
            List<ITextureModifier> splatmapModifiers, Bounds terrainBounds, OcclusionData od, bool writeToCPU = false)
        {
            if (splatmapModifiers.Count == 0)
                return;
            if (od.terrain.terrainData.terrainLayers.Length == 0)
                return;
            // make sure we actually have layers on the terrain
            // we're going to generate instead of just, say, a
            // copy stamp.
            List<TerrainLayer> layers = new List<TerrainLayer>();
            foreach (var sm in splatmapModifiers)
            {
                sm.InqTerrainLayers(splatmapData.terrain, layers);
            }
            if (layers.Count == 0)
                return;

            TerrainData terrainData = splatmapData.terrain.terrainData;

            RenderTextureDescriptor desc = new RenderTextureDescriptor(terrainData.alphamapWidth,
                terrainData.alphamapHeight, RenderTextureFormat.ARGB32, 0);
            desc.sRGB = false;
            desc.enableRandomWrite = true;
            desc.autoGenerateMips = false;

            RenderTexture indexMap0 = RenderTexture.GetTemporary(desc);
            RenderTexture weightMap0 = RenderTexture.GetTemporary(desc);

            RenderTexture indexMap1 = RenderTexture.GetTemporary(desc);
            RenderTexture weightMap1 = RenderTexture.GetTemporary(desc);

            indexMap0.name = "MicroVerse::GenerateSplats::indexMap0";
            indexMap1.name = "MicroVerse::GenerateSplats::indexMap1";
            weightMap0.name = "MicroVerse::GenerateSplats::weightMap0";
            weightMap1.name = "MicroVerse::GenerateSplats::weightMap1";

            RenderTexture.active = indexMap0;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = weightMap0;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = indexMap1;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = weightMap1;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = null;

            indexMap0.filterMode = FilterMode.Point;
            indexMap1.filterMode = FilterMode.Point;
            weightMap0.filterMode = FilterMode.Point;
            weightMap1.filterMode = FilterMode.Point;
            indexMap0.wrapMode = TextureWrapMode.Clamp;
            indexMap1.wrapMode = TextureWrapMode.Clamp;
            weightMap0.wrapMode = TextureWrapMode.Clamp;
            weightMap1.wrapMode = TextureWrapMode.Clamp;

            for (int i = splatmapModifiers.Count - 1; i >= 0; --i)
            {
                var splatmapModifier = splatmapModifiers[i];
                var bounds = splatmapModifier.GetBounds();
                bool inBounds = (bounds.Intersects(terrainBounds));

                if (inBounds)
                {
                    if (splatmapModifier.ApplyTextureStamp(indexMap0, indexMap1, weightMap0, weightMap1, splatmapData, od))
                    {
                        (indexMap0, indexMap1) = (indexMap1, indexMap0);
                        (weightMap0, weightMap1) = (weightMap1, weightMap0);
                    }
                }
            }

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(weightMap1);
            RenderTexture.ReleaseTemporary(indexMap1);
            splatmapData.indexMap = indexMap0;
            splatmapData.weightMap = weightMap0;
        }

        static ComputeShader rasterToTerrain = null;
        void RasterizeSplatMaps(Terrain terrain, RenderTexture indexMap, RenderTexture weightMap, bool writeToCPU)
        {
            var count = terrain.terrainData.alphamapTextureCount;
            if (count == 0)
                return;

            Profiler.BeginSample("Convert to Unity Format");
            // now we have to rasterize to the terrain system.
            if (rasterToTerrain == null)
            {
                rasterToTerrain = (ComputeShader)Resources.Load("MicroVerseRasterToTerrain");
            }
            int kernelHandle = rasterToTerrain.FindKernel("CSMain");
            rasterToTerrain.SetTexture(kernelHandle, "_WeightMap", weightMap);
            rasterToTerrain.SetTexture(kernelHandle, "_IndexMap", indexMap);
            
            var rts = new RenderTexture[count];
            var t = terrain.terrainData.GetAlphamapTexture(0);
            RenderTextureDescriptor desc = new RenderTextureDescriptor(t.width, t.height);
            desc.graphicsFormat = t.graphicsFormat;
            desc.sRGB = false;
            desc.enableRandomWrite = true;

            // alloc and assign textures
            for (int i = 0; i < count; ++i)
            {
                var rt = RenderTexture.GetTemporary(desc);
                rt.name = "MicroVerse:BackToTerrain";
                rts[i] = rt;
                rasterToTerrain.SetTexture(kernelHandle, "_Result" + i.ToString(), rt);
            }
            if (count > 1)
            {
                rasterToTerrain.shaderKeywords = new string[1] { "_COUNT_" + count };
            }
            else
            {
                rasterToTerrain.shaderKeywords = new string[] { };
            }

            rasterToTerrain.Dispatch(kernelHandle, Mathf.CeilToInt((float)t.width / 8), Mathf.CeilToInt((float)t.height / 8), 1);
            Profiler.EndSample();
            Profiler.BeginSample("Copy Alphamap To Terrain");
            for (int i = 0; i < count; ++i)
            {
                RenderTexture.active = rts[i];
                terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.AlphamapTextureName, i,
                    new RectInt(0, 0, rts[i].width, rts[i].height), new Vector2Int(0, 0),
                    !writeToCPU);

                RenderTexture.active = null;
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                if (IsUsingProxyRenderer)
                {
                    UpdateControlmap(terrain, i, rts[i]);
                }
                else
                {
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rts[i]);
                }
#else
                RenderTexture.ReleaseTemporary(rts[i]);
#endif
            }
            
            
            RenderTexture.active = null;
            Profiler.EndSample();

        }


        private static RenderTexture GenerateHeightmap(HeightmapData heightmapData,
            List<IHeightModifier> heightmapModifiers, Bounds terrainBounds, OcclusionData od, bool writeToCPU = false)
        {
            var terrainData = heightmapData.terrain.terrainData;
            var heightmapTexture = terrainData.heightmapTexture;
            var desc = heightmapTexture.descriptor;
            // reset, because otherwise MaxLOD size will fuck us.
            desc.width = heightmapData.terrain.terrainData.heightmapResolution;
            desc.height = desc.width;
            desc.enableRandomWrite = true;
            var rt1 = RenderTexture.GetTemporary(desc);
            var rt2 = RenderTexture.GetTemporary(desc);
            rt1.wrapMode = TextureWrapMode.Clamp;
            rt2.wrapMode = TextureWrapMode.Clamp;

            rt1.name = "MicroVerse::GenerateHeights:rt1";
            rt2.name = "MicroVerse::GenerateHeights:rt2";

            RenderTexture.active = rt1;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = rt2;
            GL.Clear(false, true, Color.clear);

            foreach (var heightmapModifier in heightmapModifiers)
            {
                var hmbounds = heightmapModifier.GetBounds();
                if (hmbounds.Intersects(terrainBounds))
                {
                    if (heightmapModifier.ApplyHeightStamp(rt1, rt2, heightmapData, od))
                        (rt1, rt2) = (rt2, rt1);
                }
            }

            RenderTexture.active = null;
            // ref can change!
            RenderTexture.ReleaseTemporary(rt2);
            return rt1;
        }
    }
}