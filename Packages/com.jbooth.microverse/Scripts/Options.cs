
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [System.Serializable]
    public class Options
    {
        [System.Serializable]
        public class Settings
        {
            public enum TerrainSearchMethod
            {
                Hierarchy,
                AllInScene
            }
            [Tooltip("When working with multiple scenes, it can be useful to have MicroVerse find all terrains in all scenes instead of only working with the ones below it in the hierarchy")]
            public TerrainSearchMethod terrainSearchMethod = TerrainSearchMethod.Hierarchy;
            [Tooltip("Some terrain shaders need the terrain layers to stay in sync between terrains - this can increase the number of splat maps needed by increasing the texture count when some textures are only used on some terrains")]
            public bool keepLayersInSync = false;
            [Tooltip("Unity's API for updating terrains is really slow, MicroVerse tries to sneak these in on things like mouse up events. This will control how many terrains it attempts to sync back on such events.")]
            public int maxHeightSaveBackPerFrame = 1;
            [Tooltip("Unity terrain rendering is really slow, so when working with a large number of terrains, MicroVerse can automatically cull them at a certain distance to improve performance")]
            public bool useSceneCulling = false;
            [Range(100, 10000)] public float sceneTerrainCullingDistance = 2500;
            [Range(100, 10000)] public float sceneVegetationCullingDistance = 1500;
            [Range(100, 24000)] public float sceneCameraCullingDistance = 12000;
        }

        [System.Serializable]
        public class Colors
        {
            public bool drawStampPreviews = true;
            public Color heightStampColor = Color.gray;
            public Color textureStampColor = Color.clear;
            public Color treeStampColor = Color.green;
            public Color detailStampColor = Color.yellow;
            public Color occluderStampColor = Color.magenta;
            public Color copyStampColor = Color.cyan;
            public Color pasteStampColor = Color.cyan * 0.8f;
            public Color maskStampColor = Color.red;
            public Color objectStampColor = Color.blue;
            public Color ambientAreaColor = new Color(0, 0, 1, 0.5f);
            public Color noisePreviewColor = new Color(1, 0, 0, 0.8f);
            public Color filterPreviewColor = new Color(0, 0, 1, 0.8f);
        }

        public Settings settings = new Settings();
        public Colors colors = new Colors();

    }


}
