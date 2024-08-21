using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FIMSpace.Generating
{
    public class PGG_Demo_Sidescroll_CollectPainters : MonoBehaviour
    {
        public BuildPlannerExecutor executor;
        public PGG_PixelMapGenerator_SidescrollXY pixelMapper;

        public void AfterGenerating()
        {
            for (int e = 0; e < executor.GeneratedGenerators.Count; e++)
            {
                pixelMapper.GenerateOutOf.Add(executor.GeneratedGenerators[e]);
            }

            pixelMapper.AddToMinimap = PGG_MinimapHandler.Instance;
            pixelMapper.enabled = true;
        }
    }
}