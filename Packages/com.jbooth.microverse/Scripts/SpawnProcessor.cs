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
        List<ISpawner> spawners = null;

        public static bool IsModifyingTerrain { get; private set; }
        public void Cancel(MicroVerse.DataCache dataCache)
        {
#if __MICROVERSE_VEGETATION__
            CancelVegetationJobs(dataCache);
#endif
#if __MICROVERSE_OBJECTS__
            CancelObjectJobs(dataCache);
#endif
        }


        public void InitSystem()
        {
            SpawnProcessor.IsModifyingTerrain = true;
            spawners = new List<ISpawner>(MicroVerse.instance.GetComponentsInChildren<ISpawner>());
            spawners.RemoveAll(p => p.IsEnabled() == false);
        }

#if __MICROVERSE_VEGETATION__
        List<TreePrototypeSerializable> treePrototypes = new List<TreePrototypeSerializable>();
        List<DetailPrototypeSerializable> detailPrototypes = new List<DetailPrototypeSerializable>();
#endif

        public void InitTerrain(Terrain terrain, MicroVerse.InvalidateType invalidateType, ref bool needCurvatureMap, ref bool needFlowMap)
        {
            Profiler.BeginSample("Spawning::InitTerrain");
            var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);

            foreach (var m in spawners)
            {
#if __MICROVERSE_VEGETATION__
                ITreeModifier tm = m as ITreeModifier;
                IDetailModifier dm = m as IDetailModifier;
#endif
#if __MICROVERSE_OBJECTS__
                IObjectModifier om = m as IObjectModifier;
#endif
#if __MICROVERSE_VEGETATION__
                if (tm != null && tm.IsEnabled())
                {
                    if (terrainBounds.Intersects(tm.GetBounds()))
                    {
                        needCurvatureMap |= tm.NeedCurvatureMap();
                        needFlowMap |= tm.NeedFlowMap();
                    }
                    // This should really be in the check above, but it seems to have
                    // a very strange issue of occationally mixing up the stamp indexes
                    // between terrains. There's little harm in putting the prototypes on
                    // every terrain (except possibly if working on a very large streaming
                    // world) so for now at least skip the check. Ugh..
                    tm.InqTreePrototypes(treePrototypes);

                }
                if (dm != null)
                {

                    if (terrainBounds.Intersects(dm.GetBounds()))
                    {
                        needCurvatureMap |= dm.NeedCurvatureMap();
                        dm.InqDetailPrototypes(detailPrototypes);
                    }
                }
#endif
#if __MICROVERSE_OBJECTS__
                if (om != null)
                {
                    if (terrainBounds.Intersects(om.GetBounds()))
                    {
                        needCurvatureMap |= om.NeedCurvatureMap();
                    }
                }
#endif
            }

#if __MICROVERSE_VEGETATION__            
            treePrototypes = treePrototypes.Distinct().ToList();
            detailPrototypes = detailPrototypes.Distinct().ToList();
            InitTerrainVegetation(terrain, treePrototypes, detailPrototypes);
            treePrototypes.Clear();
            detailPrototypes.Clear();
#endif
            Profiler.EndSample();
        }


        public void GenerateSpawnables(Terrain[] terrains, MicroVerse.DataCache dataCache)
        {
            Profiler.BeginSample("Generate Spawns");
#if __MICROVERSE_VEGETATION__ || __MICROVERSE_OBJECTS__
            bool allSDF = false;
#endif
#if __MICROVERSE_VEGETATION__

            RenderVegetationClearLayers(terrains, dataCache);
            
#if UNITY_EDITOR && __MICROVERSE_MASKS__
            if (MicroVerse.instance.bufferCaptureTarget != null)
            {
                if (MicroVerse.instance.bufferCaptureTarget.IsOutputFlagSet(BufferCaptureTarget.BufferCapture.TreeStampSDF))
                {
                    allSDF = true;
                }
            }
#endif
#endif

#if __MICROVERSE_OBJECTS__
            RenderObjectClearLayers(terrains, dataCache);
#endif

            // render stamps
            Dictionary<Terrain, Dictionary<int, List<RenderTexture>>> resultBuffers = new Dictionary<Terrain, Dictionary<int, List<RenderTexture>>>();
            bool enableTreeSDF = false;
            bool enableObjectSDF = false;
            foreach (var s in spawners)
            {
                enableTreeSDF |= s.UsesOtherTreeSDF();
                enableObjectSDF |= s.UsesOtherObjectSDF();
            }

            foreach (var s in spawners)
            {
#if __MICROVERSE_VEGETATION__
                ITreeModifier treeModifier = s as ITreeModifier;
                IDetailModifier detailModifier = s as IDetailModifier;
#endif

#if __MICROVERSE_OBJECTS__
                IObjectModifier objectModifier = s as IObjectModifier;
#endif
#if __MICROVERSE_VEGETATION__
                if (treeModifier != null)
                {
                    RenderTreeStamp(terrains, treeModifier, dataCache, allSDF, enableTreeSDF);
                }
                if (detailModifier != null)
                {
                    RenderDetailStamp(terrains, detailModifier, dataCache, resultBuffers);
                }
#endif
#if __MICROVERSE_OBJECTS__
                if (objectModifier != null)
                {
                    RenderObjectStamp(terrains, objectModifier, dataCache, allSDF, enableObjectSDF);
                }
#endif
            }
#if __MICROVERSE_VEGETATION__
            FinishedRendereringVegetation(dataCache, resultBuffers);
#endif
            Profiler.EndSample();
            
        }


        public void CheckDone()
        {
            bool done = true;
#if __MICROVERSE_VEGETATION__
            if (treeJobHolders.Count > 0 || detailJobHolders.Count > 0) done = false;
#endif
#if __MICROVERSE_OBJECTS__
            if (objectJobHolders.Count > 0) done = false;
#endif
            if (done)
            {
                SpawnProcessor.IsModifyingTerrain = false;
            }
        }
    }
}