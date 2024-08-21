using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Mathematics;

namespace JBooth.MicroVerseCore
{
    public partial class SpawnProcessor
    {

        void RenderObjectClearLayers(Terrain[] terrains, MicroVerse.DataCache dataCache)
        {
            bool needsObjectClearMap = false;

            foreach (var m in spawners)
            {
                IObjectModifier tm = m as IObjectModifier;
                if (tm != null)
                {
                    needsObjectClearMap |= tm.NeedObjectClear();
                }
            }

            // generate clear maps for everything
            
            foreach (var terrain in terrains)
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                var indexMap = dataCache.indexMaps[terrain];
                var weightMap = dataCache.weightMaps[terrain];
                var heightmapGen = dataCache.heightMaps[terrain];
                var normalGen = dataCache.normalMaps[terrain];
                var curvatureGen = dataCache.curvatureMaps[terrain];
                var flowGen = dataCache.flowMaps[terrain];
                RenderTexture clearMap = null;

                if (needsObjectClearMap)
                {
                    int size = terrain.terrainData.alphamapResolution;
                    clearMap = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);
                    RenderTexture.active = clearMap;
                    GL.Clear(false, true, Color.clear);
                    RenderTexture.active = null;
                }

                ObjectData td = new ObjectData(terrain, heightmapGen, normalGen, curvatureGen, flowGen, indexMap, weightMap, clearMap);
                dataCache.objectDatas[terrain] = td;

                if (needsObjectClearMap)
                {
                    foreach (var s in spawners)
                    {
                        IObjectModifier objectModifier = s as IObjectModifier;
                        if (objectModifier != null)
                        {
                            if (terrainBounds.Intersects(objectModifier.GetBounds()))
                            {
                                objectModifier.ApplyObjectClear(td);
                            }
                        }
                    }
                }
            }

            // reset layers
            foreach (var terrain in terrains)
            {
                if (dataCache.objectDatas.ContainsKey(terrain))
                {
                    var td = dataCache.objectDatas[terrain];
                    td.layerIndex = -1;
                }
            }
        }

        void RenderObjectStamp(Terrain[] terrains, IObjectModifier objectModifier, MicroVerse.DataCache dataCache, bool allSDF, bool enableSDF)
        {
            foreach (var terrain in terrains)
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                
                if (terrainBounds.Intersects(objectModifier.GetBounds()))
                {
                    var occlusionData = dataCache.occlusionDatas[terrain];
                    ObjectData td = dataCache.objectDatas[terrain];
                    objectModifier.ApplyObjectStamp(td, objectJobHolders, occlusionData);
                }
            }
            bool needToGenerateForChildren = objectModifier.NeedToGenerateSDFForChilden();
          
            if (allSDF || objectModifier.NeedSDF() || needToGenerateForChildren || (objectModifier.OccludesOthers() && enableSDF))
            {
                bool sdfOthers = objectModifier.OccludesOthers() && enableSDF;
                foreach (var terrain in terrains)
                {
                    var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                    if (terrainBounds.Intersects(objectModifier.GetBounds()))
                    {
                        var od = dataCache.occlusionDatas[terrain];
                        od?.RenderObjectSDF(terrain, dataCache.occlusionDatas, sdfOthers || allSDF);
                        if (needToGenerateForChildren && od != null && od.currentObjectSDF != null)
                        {
                            RenderTexture rt = RenderTexture.GetTemporary(od.currentObjectSDF.descriptor);
                            Graphics.Blit(od.currentObjectSDF, rt);
                            objectModifier.SetSDF(terrain, rt);
                        }
                    }
                }
            }

            foreach (var terrain in terrains)
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                var occlusionData = dataCache.occlusionDatas[terrain];
                if (dataCache.objectDatas.ContainsKey(terrain))
                {
                    var td = dataCache.objectDatas[terrain];
                    if (terrainBounds.Intersects(objectModifier.GetBounds()))
                    {
                        objectModifier.ProcessObjectStamp(td, objectJobHolders, occlusionData);
                    }
                }
            }
        }


        void CancelObjectJobs(MicroVerse.DataCache dataCache)
        {
            foreach (var lst in objectJobHolders.Values)
            {
                foreach (var h in lst)
                {
                    h.canceled = true;
                }
            }
            if (dataCache != null)
            {
                foreach (var td in dataCache.objectDatas.Values)
                {
                    if (td.clearMap != null)
                    {
                        RenderTexture.ReleaseTemporary(td.clearMap);
                        td.clearMap = null;
                    }
                }
            }

        }
        Dictionary<Terrain, List<ObjectJobHolder>> objectJobHolders = new Dictionary<Terrain, List<ObjectJobHolder>>();


        // object pooling is only used during regeneration, and then the pools are
        // cleared so that any extras are removed
        [System.Serializable]
        public class Pool
        {
            public GameObject prefab;
            public Stack<GameObject> instances = new Stack<GameObject>();
        }

        public static List<Pool> pools = new List<Pool>();

        public static GameObject Spawn(Terrain t, GameObject go, Transform parent, bool asPrefab = false)
        {
#if UNITY_EDITOR
            foreach (var pool in pools)
            {
                if (pool.prefab == go)
                {
                    if (pool.instances.Count > 0)
                    {
                        var i = pool.instances.Pop();
                        if (i.transform.parent != parent)
                        {
                            i.transform.parent = parent;
                        }
                        return i;
                    }
                }
            }
            GameObject ret = null;
            if (asPrefab)
                ret = UnityEditor.PrefabUtility.InstantiatePrefab(go, parent) as GameObject;
            else
                ret = Object.Instantiate(go, parent);
#else
            GameObject ret = GameObject.Instantiate(go, parent);
#endif
            return ret;
        }

        public static void Despawn(GameObject instance)
        {
#if UNITY_EDITOR
            instance.hideFlags = HideFlags.DontSaveInEditor;
            instance.transform.parent = null;
            GameObject go = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            foreach (var pool in pools)
            {
                if (pool.prefab == go)
                {
                    pool.instances.Push(instance);
                    return;
                }
            }
            Pool p = new Pool();
            p.prefab = go;
            p.instances.Push(instance);
            pools.Add(p);

#else
            GameObject.DestroyImmediate(instance);
#endif

        }

        void ClearPools()
        {
            foreach (var p in pools)
            {
                foreach (var i in p.instances)
                {
                    GameObject.DestroyImmediate(i);
                }
                p.instances.Clear();
            }
            pools.Clear();
        }

        List<Terrain> finishedObjects = new List<Terrain>();
        public void ApplyObjects()
        {
            var noAsync = MicroVerse.noAsyncReadback;
            Profiler.BeginSample("Apply Objects");
            finishedObjects.Clear();

            int count = 0;
            foreach (var terrain in objectJobHolders.Keys)
            {
                var lst = objectJobHolders[terrain];
                for (int i = 0; i < lst.Count; ++i)
                {
                    var h = lst[i];
                    if (h.IsDone())
                    {
                        if (h.canceled)
                        {
                            h.Dispose();
                            lst.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            Vector3 terrainSize = terrain.terrainData.size;
                            Vector3 terrainPos = terrain.transform.position;
                            
                            for (int j = h.unpackIndex; j < h.positionWeightData.Length; ++j)
                            {
                                half4 positionWeight = h.positionWeightData[j];
                                if (positionWeight.w <= 0)
                                {
                                    continue;
                                }
                                half4 rotationData = h.rotationData[j];
                                half4 scaleData = h.scaleIndexData[j];

                                if (scaleData.w < 0 || scaleData.w >= h.stamp.prototypes.Count)
                                {
                                    Debug.LogError("Index out of range " + rotationData.w + " of " + h.stamp.prototypes.Count);
                                    continue;
                                }
                                var prefab = h.stamp.prototypes[(int)scaleData.w];
                                var position = new Vector3(positionWeight.x * terrainSize.x, positionWeight.y * terrainSize.y, positionWeight.z * terrainSize.z) + terrain.transform.position;
                                var rotation = new Quaternion(rotationData.x, rotationData.y, rotationData.z, rotationData.w);
                                var scale = new Vector3(scaleData.x, scaleData.y, scaleData.z);

                                GameObject go = Spawn(terrain, prefab, h.stamp.FindParentObject(terrain), h.stamp.spawnAsPrefab);
                                go.transform.SetPositionAndRotation(position, rotation);
                                go.transform.localScale = scale;
                                if (h.stamp.hideInHierarchy)
                                    go.hideFlags = HideFlags.HideInHierarchy;
                                else
                                    go.hideFlags = HideFlags.None;
                                h.stamp.spawnedInstances.Add(go);

                                count++;

                                if (count > 1500 && !noAsync)
                                {
                                    h.unpackIndex = j;
                                    Profiler.EndSample();
                                    return;
                                }
                            }

                            h.Dispose();
                            lst.RemoveAt(i);
                            i--;

                        }
                    }
                }

                if (lst.Count == 0)
                {
                    finishedObjects.Add(terrain);
                }
            }
            foreach (var f in finishedObjects)
            {
                objectJobHolders.Remove(f);
            }
            finishedObjects.Clear();
            if (objectJobHolders.Count == 0)
            {
                ClearPools();
            }

            Profiler.EndSample();

        }
    }
}
