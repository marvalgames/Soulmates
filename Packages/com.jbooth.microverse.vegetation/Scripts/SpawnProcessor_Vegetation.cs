using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;

namespace JBooth.MicroVerseCore
{
    public partial class SpawnProcessor
    {
        Dictionary<Terrain, List<TreeJobHolder>> treeJobHolders = new Dictionary<Terrain, List<TreeJobHolder>>();
        Dictionary<Terrain, List<DetailJobHolder>> detailJobHolders = new Dictionary<Terrain, List<DetailJobHolder>>();

        void FinishedRendereringVegetation(MicroVerse.DataCache dataCache, Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers)
        {
            RenderTexture.active = null;
            foreach (var td in dataCache.treeDatas.Values)
            {
                if (td.treeClearMap != null)
                {
                    RenderTexture.ReleaseTemporary(td.treeClearMap);
                    td.treeClearMap = null;
                }
            }

            foreach (var dd in dataCache.detailDatas.Values)
            {
                if (dd.clearMap != null)
                {
                    RenderTexture.ReleaseTemporary(dd.clearMap);
                    dd.clearMap = null;
                }
            }

            // merge detail layers together if they are for the same index
            Material mergeMat = new Material(Shader.Find("Hidden/MicroVerse/CombineDetailBuffers"));
            foreach (var tk in resultBuffers.Keys)
            {
                var dbuffer = resultBuffers[tk];
                foreach (var k in dbuffer.Keys)
                {
                    var resultList = dbuffer[k];
                    if (resultList.Count > 1)
                    {
                        RenderTexture targetA = RenderTexture.GetTemporary(resultList[0].descriptor);
                        RenderTexture targetB = RenderTexture.GetTemporary(resultList[0].descriptor);

                        targetA.name = "MicroVerse::GenerateDetails";
                        targetB.name = "MicroVerse::GenerateDetails";

                        Graphics.Blit(resultList[0], targetA);
                        RenderTexture.ReleaseTemporary(resultList[0]);
                        for (int i = 1; i < resultList.Count; ++i)
                        {
                            var buffer = resultList[i];
                            mergeMat.SetTexture("_Merge", buffer);

                            Graphics.Blit(targetA, targetB, mergeMat);
                            RenderTexture.active = null;
                            RenderTexture.ReleaseTemporary(buffer);
                            (targetA, targetB) = (targetB, targetA);
                        }
                        resultList.Clear();
                        resultList.Add(targetA);
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(targetB);
                    }
                }
                foreach (var k in dbuffer.Keys)
                {
                    var resultList = dbuffer[k];
                    if (resultList.Count != 1)
                    {
                        Debug.LogError("Detail channels have not been merged, memory will be leaked");
                    }
                    var holder = new DetailJobHolder();
                    holder.terrain = tk;
                    holder.AddJob(resultList[0], k);
                    if (detailJobHolders.ContainsKey(tk))
                    {
                        detailJobHolders[tk].Add(holder);
                    }
                    else
                    {
                        detailJobHolders.Add(tk, new List<DetailJobHolder>() { holder });
                    }
                }
            }
            Object.DestroyImmediate(mergeMat);
        }

        void RenderVegetationClearLayers(Terrain[] terrains, MicroVerse.DataCache dataCache)
        {
            bool needsTreeClearMap = false;
            bool needsDetailClearmap = false;

            foreach (var m in spawners)
            {
                ITreeModifier tm = m as ITreeModifier;
                IDetailModifier dm = m as IDetailModifier;
                if (tm != null)
                {
                    needsTreeClearMap |= tm.NeedTreeClear();
                }
                if (dm != null)
                {
                    needsDetailClearmap |= dm.NeedDetailClear();
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
                RenderTexture detailClearMap = null;
                if (needsTreeClearMap)
                {
                    int size = terrain.terrainData.alphamapResolution;
                    clearMap = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);
                    RenderTexture.active = clearMap;
                    GL.Clear(false, true, Color.clear);
                    RenderTexture.active = null;
                }
                if (needsDetailClearmap)
                {
                    int size = terrain.terrainData.detailResolution;
                    detailClearMap = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);
                    RenderTexture.active = detailClearMap;
                    GL.Clear(false, true, Color.clear);
                    RenderTexture.active = null;
                }

                TreeData td = new TreeData(terrain, heightmapGen, normalGen, curvatureGen, flowGen, clearMap, dataCache);
                dataCache.treeDatas[terrain] = td;

                DetailData dd = new DetailData(terrain, heightmapGen, normalGen, curvatureGen, flowGen, detailClearMap, dataCache);
                dataCache.detailDatas[terrain] = dd;
                

                foreach (var s in spawners)
                {
                    ITreeModifier treeModifier = s as ITreeModifier;
                    IDetailModifier detailModifier = s as IDetailModifier;
                    if (treeModifier != null)
                    {
                        if (terrainBounds.Intersects(treeModifier.GetBounds()))
                        {
                            treeModifier.ApplyTreeClear(td);
                        }
                    }
                    if (detailModifier != null)
                    {
                        if (terrainBounds.Intersects(detailModifier.GetBounds()))
                        {
                            detailModifier.ApplyDetailClear(dd);
                        }
                    }
                }
            }

            // reset layers
            foreach (var terrain in terrains)
            {
                if (dataCache.treeDatas.ContainsKey(terrain))
                {
                    TreeData td = dataCache.treeDatas[terrain];
                    td.layerIndex = -1;
                }
                if (dataCache.detailDatas.ContainsKey(terrain))
                {
                    DetailData dd = dataCache.detailDatas[terrain];
                    dd.layerIndex = -1;
                }
            }
        }

        void RenderDetailStamp(Terrain[] terrains, IDetailModifier detailModifier, MicroVerse.DataCache dataCache,
            Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers)
        {
            Profiler.BeginSample("Generate Details");
            foreach (var terrain in terrains)
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                if (terrainBounds.Intersects(detailModifier.GetBounds()))
                {
                    var dd = dataCache.detailDatas[terrain];
                    var od = dataCache.occlusionDatas[terrain];
                    detailModifier.ApplyDetailStamp(dd, resultBuffers, od);
                }
            }
            Profiler.EndSample();
        }

        void RenderTreeStamp(Terrain[] terrains, ITreeModifier treeModifier, MicroVerse.DataCache dataCache, bool allSDF, bool enableTreeSDF)
        {
            foreach (var terrain in terrains)
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                if (terrainBounds.Intersects(treeModifier.GetBounds()))
                {
                    var occlusionData = dataCache.occlusionDatas[terrain];
                    TreeData td = dataCache.treeDatas[terrain];
                    treeModifier.ApplyTreeStamp(td, treeJobHolders, occlusionData);
                }
            }

            bool needToGenerateForChildren = treeModifier.NeedToGenerateSDFForChilden();
            if (allSDF || treeModifier.NeedSDF() || needToGenerateForChildren || (treeModifier.OccludesOthers() && enableTreeSDF))
            {
                bool sdfOthers = treeModifier.OccludesOthers() && enableTreeSDF;
                foreach (var terrain in terrains)
                {
                    var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                    if (terrainBounds.Intersects(treeModifier.GetBounds()))
                    {
                        var od = dataCache.occlusionDatas[terrain];
                        od?.RenderTreeSDF(terrain, dataCache.occlusionDatas, sdfOthers || allSDF);
                        if (needToGenerateForChildren && od != null && od.currentTreeSDF != null)
                        {
                            RenderTexture rt = RenderTexture.GetTemporary(od.currentTreeSDF.descriptor);
                            Graphics.Blit(od.currentTreeSDF, rt);
                            treeModifier.SetSDF(terrain, rt);
                        }
                    }
                }
                
            }
            foreach (var terrain in terrains)
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

                var occlusionData = dataCache.occlusionDatas[terrain];
                if (dataCache.treeDatas.ContainsKey(terrain))
                {
                    if (terrainBounds.Intersects(treeModifier.GetBounds()))
                    {
                        var td = dataCache.treeDatas[terrain];
                        treeModifier.ProcessTreeStamp(td, treeJobHolders, occlusionData);
                    }
                }
            }
        }

        void InitTerrainVegetation(Terrain terrain, List<TreePrototypeSerializable> treePrototypes, List<DetailPrototypeSerializable> detailPrototypes)
        {
            TreePrototype[] trees = new TreePrototype[treePrototypes.Count];
            DetailPrototype[] details = new DetailPrototype[detailPrototypes.Count];

            for (int i = 0; i < treePrototypes.Count; ++i) { trees[i] = treePrototypes[i].GetPrototype(); }
            for (int i = 0; i < detailPrototypes.Count; ++i) { details[i] = detailPrototypes[i].GetPrototype(); }

            // try to avoid calling any unity terrain API functions if at all possible. This saves 7ms
            // on the demo scene, which is before we even start GPU processing, so big win.
            var existingTrees = terrain.terrainData.treePrototypes;
            var existingDetails = terrain.terrainData.detailPrototypes;
            bool setTrees = false;
            bool setDetails = false;
            if (existingTrees.Length != trees.Length)
                setTrees = true;
            if (existingDetails.Length != details.Length)
                setDetails = true;

            if (!setTrees)
            {
                for (int i = 0; i < trees.Length; ++i)
                {
                    if (!trees[i].Equals(existingTrees[i]))
                    {
                        setTrees = true;
                        break;
                    }
                }
            }
            if (!setDetails)
            {
                for (int i = 0; i < details.Length; ++i)
                {
                    if (!details[i].Equals(existingDetails[i]))
                    {
                        setDetails = true;
                        break;
                    }
#if UNITY_2022_2_OR_NEWER
                    // UNITY BUG: They updated the struct, but not the equality operator, nice..
                    if (details[i].positionJitter != existingDetails[i].positionJitter ||
                        details[i].alignToGround != existingDetails[i].alignToGround)
                    {
                        setDetails = true;
                        break;
                    }
                    
#endif
                }
            }
            if (setTrees)
            {
                terrain.terrainData.SetTreeInstances(new TreeInstance[0], false);
                terrain.terrainData.treePrototypes = trees;
            }
            if (setDetails)
            {
                terrain.terrainData.detailPrototypes = details;
            }
        }

        void CancelVegetationJobs(MicroVerse.DataCache dataCache)
        {
            foreach (var lst in treeJobHolders.Values)
            {
                foreach (var h in lst)
                {
                    h.canceled = true;
                }
            }
            foreach (var lst in detailJobHolders.Values)
            {
                foreach (var h in lst)
                {
                    h.canceled = true;
                }
            }
            if (dataCache != null)
            {
                foreach (var td in dataCache.treeDatas.Values)
                {
                    if (td.treeClearMap != null)
                    {
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(td.treeClearMap);
                        td.treeClearMap = null;
                    }
                }
                foreach (var td in dataCache.detailDatas.Values)
                {
                    if (td.clearMap != null)
                    {
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(td.clearMap);
                        td.clearMap = null;
                    }
                }
            }
        }

        List<Terrain> finishedTrees = new List<Terrain>();
        public void ApplyTrees()
        {
            Profiler.BeginSample("Apply Trees");
            finishedTrees.Clear();

            foreach (var lst in treeJobHolders.Values)
            {
                for (int i = 0; i < lst.Count; ++i)
                {
                    if (lst[i].canceled)
                    {
                        if (lst[i].IsDone())
                        {
                            lst[i].Dispose();
                            lst.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            foreach (var terrain in treeJobHolders.Keys)
            {
                var lst = treeJobHolders[terrain];
                bool isDone = true;
                foreach (var h in lst)
                {
                    if (h.IsDone() == false)
                    {
                        isDone = false;
                        break;
                    }
                }
                if (lst.Count == 0)
                {
                    finishedTrees.Add(terrain);
                }
                if (isDone && lst.Count > 0)
                {

                    int completeCount = 0;
                    foreach (var h in lst)
                    {
                        h.handle.Complete();// TODO: they should all be done, but aren't!? wtf?
                        completeCount += h.job.count[0];
                    }
                    NativeArray<TreeInstance> totalInstances = new NativeArray<TreeInstance>(completeCount, Allocator.Temp);
                    int destIndex = 0;
                    foreach (var h in lst)
                    {
                        if (h.job.count[0] > 0)
                        {
                            var range = h.job.trees.GetSubArray(0, h.job.count[0]);
                            NativeArray<TreeInstance>.Copy(range, 0, totalInstances, destIndex, h.job.count[0]);
                            destIndex += h.job.count[0];
                        }
                        h.Dispose();
                    }
                    lst.Clear();
                    TreeInstance[] instances = new TreeInstance[completeCount];
                    totalInstances.CopyTo(instances);
                    terrain.terrainData.SetTreeInstances(instances, false);
                    totalInstances.Dispose();
                    finishedTrees.Add(terrain);
                }
            }

            foreach (var f in finishedTrees)
            {
                treeJobHolders.Remove(f);
            }
            finishedTrees.Clear();
            Profiler.EndSample(); // apply trees

        }

        List<Terrain> finishedDetails = new List<Terrain>();
        public void ApplyDetails()
        {
            Profiler.BeginSample("Apply Details");

            foreach (var lst in detailJobHolders.Values)
            {
                for (int i = 0; i < lst.Count; ++i)
                {
                    if (lst[i].canceled && lst[i].IsDone())
                    {
                        lst[i].Dispose();
                        lst.RemoveAt(i);
                        i--;
                    }
                }
            }


            finishedDetails.Clear();
            foreach (var terrain in detailJobHolders.Keys)
            {
                var lst = detailJobHolders[terrain];
                bool isDone = true;
                foreach (var h in lst)
                {
                    if (h.IsDone() == false)
                    {
                        isDone = false;
                        break;
                    }
                }
                if (isDone)
                {
                    finishedDetails.Add(terrain);
                }
            }


            foreach (var f in finishedDetails)
            {
                detailJobHolders.Remove(f);
            }

            finishedDetails.Clear();
            Profiler.EndSample(); // apply details
        }
    }
}
