using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace JBooth.MicroVerseCore
{
    [InitializeOnLoadAttribute]
    public static class MicroVersePlayModeStateChanged
    {
        // register an event handler when the class is initialized
        static MicroVersePlayModeStateChanged()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (MicroVerse.instance != null)
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    if (MicroVerse.instance.options.settings.useSceneCulling)
                    {
                        MicroVerse.instance.SyncTerrainList();
                        foreach (var t in MicroVerse.instance.terrains)
                        {
                            t.drawHeightmap = true;
                            t.drawTreesAndFoliage = true;
                        }
                    }
                    MicroVerse.instance.enabled = false;
                }
            }
        }
    }

    class TerrainAssetProcessor : AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            MicroVerse instance = GameObject.FindFirstObjectByType<MicroVerse>();
            if (instance != null)
            {
                if (instance.options.settings.useSceneCulling)
                {
                    instance.SyncTerrainList();
                    foreach (var t in MicroVerse.instance.terrains)
                    {
                        t.drawHeightmap = true;
                        t.drawTreesAndFoliage = true;
                    }
                }
                //instance.Modify(true, true, false);
                instance.SaveBackToTerrain(); // your save terrain function
            }
#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            if (instance != null && instance.terrains != null && instance.proxyRenderMode != MicroVerse.ProxyRenderMode.AlwaysUnity)
            {
                bool draw = true;
                if (instance.terrains.Length > 0 && instance.terrains[0] != null)
                {
                    draw = instance.terrains[0].drawHeightmap;
                }
                foreach (var t in instance.terrains)
                {
                    if (t != null)
                        t.drawHeightmap = true;
                }
            }
#endif

            return paths;
        }
    }

    public enum HeightMapResolution
    {
        [InspectorName("33 x 33")]
        k33 = 33,
        [InspectorName("65 x 65")]
        k65 = 65,
        [InspectorName("129 x 129")]
        k129 = 129,
        [InspectorName("257 x 257")]
        k257 = 257,
        [InspectorName("513 x 513")]
        k513 = 513,
        [InspectorName("1025 x 1025")]
        k1025 = 1025,
        [InspectorName("2049 x 2049")]
        k2049 = 2049,
        [InspectorName("4097 x 4097")]
        k4097 = 4097
    }

    public enum SplatResolution
    {
        [InspectorName("32 x 32")]
        k32 = 32,
        [InspectorName("64 x 64")]
        k64 = 64,
        [InspectorName("128 x 128")]
        k128 = 128,
        [InspectorName("256 x 256")]
        k256 = 258,
        [InspectorName("512 x 512")]
        k512 = 512,
        [InspectorName("1024 x 1024")]
        k1024 = 1024,
        [InspectorName("2048 x 2048")]
        k2048 = 2048,
        [InspectorName("4096 x 4096")]
        k4096 = 4096
    }

    
    [CustomEditor(typeof(MicroVerse))]
    public class MicroVerseEditor : Editor
    {

        void CheckAreTerrainsConnected()
        {
            if (MicroVerse.instance == null)
                return;
            foreach (var t in MicroVerse.instance.terrains)
            {
                if (t != null && t.allowAutoConnect == false)
                {
                    EditorGUILayout.HelpBox("Terrain's are not set to autoconnect - this can result in seams between terrains", MessageType.Error);
                    return;
                }
            }
        }

        void DoTerrainSyncGUI()
        {
            if (MicroVerse.instance == null)
                return;
            MicroVerse.instance.SyncTerrainList();
            if (MicroVerse.instance.terrains == null || MicroVerse.instance.terrains.Length == 0 || MicroVerse.instance.terrains[0] == null)
                return;

            bool regenAll = false;
            var src = MicroVerse.instance.terrains[0];

            EditorGUILayout.HelpBox("Changing any property here will update the properties of all terrains", MessageType.Info);

#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            var proxyRender = MicroVerse.instance.proxyRenderMode;
#endif
            var drawTreesAndFoliage = src.drawTreesAndFoliage;
            var draw = src.drawHeightmap;
            var alphaMapResolution = (SplatResolution) src.terrainData.alphamapResolution;
            var heightmapResolution = (HeightMapResolution)src.terrainData.heightmapResolution;
            var basemapDistance = src.basemapDistance;
            var baseMapResolution = (SplatResolution)src.terrainData.baseMapResolution;
            var detailObjectDensity = src.detailObjectDensity;
            var detailObjectDistance = src.detailObjectDistance;
            var treeDistance = src.treeDistance;
            var pixelError = src.heightmapPixelError;
            var detailRes = src.terrainData.detailResolution;
            var detailResPerPatch = src.terrainData.detailResolutionPerPatch;
            var materialTemplate = src.materialTemplate;
            var heightmapScale = src.terrainData.size.y;
#if UNITY_2022_2_OR_NEWER
            var detailScatterMode = src.terrainData.detailScatterMode;
#endif
            EditorGUI.BeginChangeCheck();
#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            if (MicroVerse.instance.IsUsingMicroSplat() && MicroVerse.instance.terrains.Length > 0)
            {
                if (MicroVerse.instance.terrains[0] != null)
                {
                    var mst = MicroVerse.instance.terrains[0].GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                    if (mst.keywordSO != null && mst.keywordSO.IsKeywordEnabled("_OUTPUTMICROVERSEPREVIEW"))
                    {
                        var old = proxyRender;
                        proxyRender = (MicroVerse.ProxyRenderMode)EditorGUILayout.EnumPopup("Proxy Renderer Mode", proxyRender);
                        if (old != proxyRender)
                        {
                            regenAll = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("If you would like to use the fast proxy rendering you must enable it in the MicroSplat material", MessageType.Info);
                        GUI.enabled = false;
                        proxyRender = (MicroVerse.ProxyRenderMode)EditorGUILayout.EnumPopup("Proxy Renderer Mode", proxyRender);
                        GUI.enabled = true;
                    }
                }
                
            }
            else
            {
                proxyRender = MicroVerse.ProxyRenderMode.AlwaysUnity;
            }
#endif
            materialTemplate = (Material)EditorGUILayout.ObjectField("Material Template", materialTemplate, typeof(Material), false);
            #if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            GUI.enabled = proxyRender == MicroVerse.ProxyRenderMode.AlwaysUnity;
#endif
            draw = EditorGUILayout.Toggle("Draw Heightmap", draw);
            GUI.enabled = true;
            drawTreesAndFoliage = EditorGUILayout.Toggle("Draw Trees and Foliage", drawTreesAndFoliage);

            EditorGUI.BeginChangeCheck();
            heightmapResolution = (HeightMapResolution)EditorGUILayout.EnumPopup(new GUIContent("HeightMap Resolution"), heightmapResolution);
            if (heightmapResolution == HeightMapResolution.k4097)
            {
                EditorGUILayout.HelpBox("Unity Terrains are incredibly slow at 4k resolution, multiple lower res terrains are recommended", MessageType.Warning);
            }
            alphaMapResolution = (SplatResolution) EditorGUILayout.EnumPopup(new GUIContent("AlphaMap Resolution"), alphaMapResolution);
            if (alphaMapResolution == SplatResolution.k4096)
            {
                EditorGUILayout.HelpBox("Unity Terrains are incredibly slow at 4k resolution, multiple lower res terrains are recommended", MessageType.Warning);
            }
            EditorGUI.EndChangeCheck();

            baseMapResolution = (SplatResolution)EditorGUILayout.EnumPopup(new GUIContent("BaseMap Resolution"), baseMapResolution);
            if (!MicroVerse.instance.IsUsingMicroSplat())
                basemapDistance = EditorGUILayout.Slider("Base Map Distance", basemapDistance, 0, 20000);

            EditorGUI.BeginChangeCheck();
#if UNITY_2022_2_OR_NEWER
            detailScatterMode = (DetailScatterMode)EditorGUILayout.EnumPopup("Detail Scatter Mode", detailScatterMode);
#endif
            heightmapScale = EditorGUILayout.DelayedFloatField("Heightmap Scale", heightmapScale);
            detailRes = EditorGUILayout.DelayedIntField("Detail Resolution", detailRes);
            detailResPerPatch = EditorGUILayout.DelayedIntField("Detail Resolution Per Patch", detailResPerPatch);
            detailObjectDensity = EditorGUILayout.Slider("Detail Density", detailObjectDensity, 0, 1);
            EditorGUI.EndChangeCheck();

            detailObjectDistance = EditorGUILayout.Slider("Detail Distance", detailObjectDistance, 0, 400);
            treeDistance = EditorGUILayout.Slider("Tree Distance", treeDistance, 0, 5000);
            pixelError = EditorGUILayout.Slider("Pixel Error", pixelError, 1, 200);

            if (EditorGUI.EndChangeCheck())
            {
#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                
                if (proxyRender == MicroVerse.ProxyRenderMode.AlwaysUnity &&
                    proxyRender != MicroVerse.instance.proxyRenderMode)
                {
                    draw = true;
                }
                MicroVerse.instance.proxyRenderMode = proxyRender;
#endif

                for (int i = 0; i < MicroVerse.instance.terrains.Length; ++i)
                {
                    var t = MicroVerse.instance.terrains[i];
                    var size = t.terrainData.size;
                    t.drawTreesAndFoliage = drawTreesAndFoliage;
#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
                    if (proxyRender == MicroVerse.ProxyRenderMode.AlwaysUnity)
                        t.drawHeightmap = draw;
#else
                    t.drawHeightmap = draw;
#endif
                    if (t.terrainData.alphamapResolution != (int)alphaMapResolution)
                        t.terrainData.alphamapResolution = (int)alphaMapResolution;
                    if (t.terrainData.heightmapResolution != (int)heightmapResolution)
                        t.terrainData.heightmapResolution = (int)heightmapResolution;
                    if (t.terrainData.baseMapResolution != (int)baseMapResolution)
                        t.terrainData.baseMapResolution = (int)baseMapResolution;
                    var hms = t.terrainData.size;
                    hms.y = heightmapScale;
                    t.terrainData.size = hms;
                    t.materialTemplate = materialTemplate;
                    t.basemapDistance = basemapDistance;
                    t.detailObjectDensity = detailObjectDensity;
                    t.detailObjectDistance = detailObjectDistance;
                    t.treeDistance = treeDistance;
                    t.heightmapPixelError = pixelError;
                    t.terrainData.size = size;
#if UNITY_2022_2_OR_NEWER
                    t.terrainData.SetDetailScatterMode(detailScatterMode);
#endif
                    t.terrainData.SetDetailResolution(detailRes, detailResPerPatch);
                    EditorUtility.SetDirty(t);
                    EditorUtility.SetDirty(t.terrainData);
                    t.drawTreesAndFoliage = drawTreesAndFoliage;
                    if (t.terrainData.baseMapResolution != (int)baseMapResolution)
                        t.terrainData.baseMapResolution = (int)baseMapResolution;
                    t.materialTemplate = materialTemplate;
                    t.basemapDistance = basemapDistance;
                    t.detailObjectDensity = detailObjectDensity;
                    t.detailObjectDistance = detailObjectDistance;
                    t.treeDistance = treeDistance;
                    t.heightmapPixelError = pixelError;
                }
            }
            if (regenAll)
            {
                MicroVerse.instance?.Modify(true, true, false);
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

#if __MICROSPLAT__

        // TODO: Once MicroSplat is updated with this code and time has passed,
        // remove it and call into MicroSplat, because this shouldn't live in MV..
        public class LayerSort
        {
            public TerrainLayer terrainLayer;
            public Color[] propDataValues = null;
            public MicroSplat.TextureArrayConfig.TextureEntry source = null;
            public MicroSplat.TextureArrayConfig.TextureEntry source2 = null;
            public MicroSplat.TextureArrayConfig.TextureEntry source3 = null;
        }



        static bool IsInConfig(MicroSplat.TextureArrayConfig config, TerrainLayer l)
        {
            foreach (var c in config.sourceTextures)
            {
                if (c.terrainLayer == l)
                    return true;
            }
            return false;
        }


        static Color[] GetPropDataValues(MicroSplat.MicroSplatPropData pd, int textureIndex)
        {
            pd.RevisionData();
            Color[] c = new Color[MicroSplat.MicroSplatPropData.sMaxAttributes];
            for (int i = 0; i < MicroSplat.MicroSplatPropData.sMaxAttributes; ++i)
            {
                c[i] = pd.GetValue(textureIndex, i);
            }
            return c;
        }

        static void SetPropDataValues(MicroSplat.MicroSplatPropData pd, int textureIndex, Color[] c)
        {
            pd.RevisionData();
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(pd, "Changed Value");
#endif
            for (int i = 0; i < c.Length; ++i)
            {
                pd.SetValue(textureIndex, i, c[i]);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(pd);
#endif
        }

        public static void SyncMicroSplat()
        {
            var mv = MicroVerse.instance;
            if (mv == null)
                return;
            if (mv.terrains.Length == 0)
                return;
            var mst = mv.terrains[0].GetComponent<MicroSplat.MicroSplatTerrain>();

            MicroSplat.MicroSplatPropData propData = mst.propData;
            if (propData == null && mst.templateMaterial != null)
            {
                propData = MicroSplatShaderGUI.FindOrCreatePropTex(mst.templateMaterial);
            }
            if (propData == null)
            {
                Debug.LogError("Could not find or create propdata");
            }

            ITextureModifier[] texMods = mv.GetComponentsInChildren<ITextureModifier>(true);
            List<TerrainLayer> layers = new List<TerrainLayer>();
            foreach (var terrain in mv.terrains)
            {
                foreach (var texMod in texMods)
                {
                    texMod.InqTerrainLayers(terrain, layers);
                }
            }
            layers.RemoveAll(item => item == null);
            layers = layers.Distinct().OrderBy(x => x.name).ToList();

            var terrainLayers = mv.terrains[0].terrainData.terrainLayers;

            MatchAndSortTerrainLayers(mv.msConfig, propData, layers, terrainLayers);

            mv.Modify(true);
        }

        static void MatchAndSortTerrainLayers(MicroSplat.TextureArrayConfig config, MicroSplat.MicroSplatPropData propData,
            List<TerrainLayer> mvLayers, TerrainLayer[] terrainLayers)
        {
            // Go through the mvlayers and add any new ones
            for (int i = 0; i < mvLayers.Count; ++i)
            {
                if (!IsInConfig(config, mvLayers[i]))
                {
                    config.AddTerrainLayer(mvLayers[i]);
                }
            }

            // build sortable list of layers so we can sync them in alphabetical order
            List<LayerSort> layers = new List<LayerSort>();
            for (int i = 0; i < config.sourceTextures.Count; ++i)
            {
                if (mvLayers.Contains(config.sourceTextures[i].terrainLayer))
                {
                    LayerSort ls = new LayerSort();
                    ls.terrainLayer = config.sourceTextures[i].terrainLayer;
                    if (propData != null)
                        ls.propDataValues = GetPropDataValues(propData, i);
                    ls.source = config.sourceTextures[i];
                    if (config.sourceTextures2 != null && i < config.sourceTextures2.Count) ls.source2 = config.sourceTextures2[i];
                    if (config.sourceTextures3 != null && i < config.sourceTextures3.Count) ls.source3 = config.sourceTextures3[i];
                    layers.Add(ls);
                }
            }

            layers.Sort((x, y) => x.terrainLayer.name.CompareTo(y.terrainLayer.name));

            config.sourceTextures.Clear();
            config.sourceTextures2?.Clear();
            config.sourceTextures3?.Clear();
            // move propdata around and setup the textures
            for (int i = 0; i < layers.Count; ++i)
            {
                var l = layers[i];
                if (propData != null)
                    SetPropDataValues(propData, i, l.propDataValues);
                config.sourceTextures.Add(l.source);
                if (l.source2 != null) config.sourceTextures2?.Add(l.source2);
                if (l.source3 != null) config.sourceTextures3?.Add(l.source3);
            }
            MicroSplat.TextureArrayConfigEditor.CompileConfig(config);
        }

#endif // microsplat

        public static List<TerrainLayer> GetLayersIfSyncToMS()
        {
#if __MICROSPLAT__
            var mv = MicroVerse.instance;
            if (mv != null && mv.msConfig != null)
            {
                mv.SyncTerrainList();
                // find any terrains without an mst component and add them
                JBooth.MicroSplat.MicroSplatTerrain mst = null;
                // first find one so we can grab the material from it
                foreach (var terrain in mv.terrains)
                {
                    mst = terrain.GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                    if (mst != null) break;
                }

                foreach (var terrain in mv.terrains)
                {
                    var tc = terrain.GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                    if (tc == null)
                    {
                        tc = terrain.gameObject.AddComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                        tc.templateMaterial = mst.templateMaterial;
                        tc.Sync();
                    }
                }
                ITextureModifier[] texMods = mv.GetComponentsInChildren<ITextureModifier>(true);
                List<TerrainLayer> layers = new List<TerrainLayer>();
                foreach (var terrain in mv.terrains)
                {
                    foreach (var texMod in texMods)
                    {
                        texMod.InqTerrainLayers(terrain, layers);
                    }
                }
                layers.RemoveAll(item => item == null);
                layers = layers.Distinct().OrderBy(x => x.name).ToList();
                for (int i = 0; i < layers.Count; ++i)
                {
                    var layer = layers[i];
                    if (mv.msConfig.sourceTextures.Count != layers.Count || Object.ReferenceEquals(mv.msConfig.sourceTextures[i].terrainLayer, layer) == false)
                    {
                        return null;
                    }
                }
                return layers;
            }
#endif

            return null;
        }

        void DrawUninstalled(string name, string link)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name);
            if (GUILayout.Button("Get"))
            {
                Application.OpenURL(link);
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawInstalled(string name, string label = "Installed")
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name);
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        public static string ambientUrl = "https://assetstore.unity.com/packages/tools/terrain/microverse-ambiance-233582?aid=25047";
        public static string roadUrl = "https://assetstore.unity.com/packages/tools/terrain/microverse-roads-208590?aid=25047";
        public static string vegUrl = "https://assetstore.unity.com/packages/tools/terrain/microverse-vegetation-232973?aid=25047";
        public static string maskUrl = "https://assetstore.unity.com/packages/tools/terrain/microverse-masks-238841?aid=25047";
        public static string objectUrl = "https://assetstore.unity.com/packages/tools/terrain/microverse-objects-239407?aid=25047";
        public static string splineUrl = "https://assetstore.unity.com/packages/tools/terrain/microverse-splines-232974?aid=25047";

        void DrawModuleAds()
        {
            EditorGUILayout.BeginVertical(GUIUtil.boxStyle);
            EditorGUILayout.LabelField("Modules");
            EditorGUI.indentLevel++;
#if __MICROVERSE_AMBIANCE__
            DrawInstalled("Ambience");
#else
            DrawUninstalled("Ambience", ambientUrl);
#endif

#if __MICROVERSE_MASKS__
            DrawInstalled("Masks");
#else
            DrawUninstalled("Masks", maskUrl);
#endif

#if __MICROVERSE_OBJECTS__
            DrawInstalled("Objects");
#else
            DrawUninstalled("Objects", objectUrl);
#endif

#if __MICROVERSE_ROADS__
            DrawInstalled("Roads");
#else
            DrawInstalled("Roads", roadUrl); 
#endif

#if __MICROVERSE_SPLINES__
            DrawInstalled("Splines");
#else
            DrawUninstalled("Splines", splineUrl);
#endif

#if __MICROVERSE_VEGETATION__
            DrawInstalled("Vegetation");
#else
            DrawUninstalled("Vegetation", vegUrl);
#endif
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

        }

        public override void OnInspectorGUI()
        {
            var mv = (MicroVerse)target;
            serializedObject.Update();

            GUIUtil.DrawHeaderLogo();
            DrawModuleAds();
            if (mv != null && mv.terrains != null && mv.terrains.Length > 0)
            {
                CheckAreTerrainsConnected();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("explicitTerrains"));
#if __MICROVERSE_MASKS__
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bufferCaptureTarget"));
#endif

            bool useSceneCulling = mv.options.settings.useSceneCulling;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("options"));
            serializedObject.ApplyModifiedProperties();
            if (useSceneCulling != mv.options.settings.useSceneCulling)
            {
                if (mv.options.settings.useSceneCulling == false)
                {
                    mv.SyncTerrainList();
                    foreach (var t in mv.terrains)
                    {
                        t.drawHeightmap = true;
                        t.drawTreesAndFoliage = true;
                    }
                }
            }

#if __MICROSPLAT__
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("msConfig"));
                serializedObject.ApplyModifiedProperties();
                if (mv.msConfig != null)
                {
                    var layers = GetLayersIfSyncToMS();
                    if (layers == null)
                    {
                        EditorGUILayout.HelpBox("Terrain Layers are not in sync with the MicroSplat texture array config, please update them", MessageType.Error);
                        if (GUILayout.Button("Update Texture Arrays"))
                        {
                            SyncMicroSplat();
                        }
                    }
                    if (layers != null && layers.Count != mv.msConfig.sourceTextures.Count && layers.Count != 0)
                    {
                        if (GUILayout.Button("Remove unused layers from texture arrays"))
                        {
                            SyncMicroSplat();
                        }

                    }
                }
                else
                {
                    if (GUILayout.Button("Convert to MicroSplat"))
                    {
                        mv.options.settings.keepLayersInSync = true;
                        mv.Modify(true);
                        EditorUtility.SetDirty(mv);
                        mv.msConfig = JBooth.MicroSplat.MicroSplatTerrainEditor.ConvertTerrains(mv.terrains, mv.terrains[0].terrainData.terrainLayers);

                    }
                }
            }

            
#else

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Install MicroSplat"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/terrain/microsplat-96478");
            }


#if USING_URP
            if (GUILayout.Button("URP Module"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/terrain/microsplat-urp-2021-support-205510");
            }
#elif USING_HDRP
            if (GUILayout.Button("HDRP Module"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/terrain/microsplat-hdrp-2021-support-206311");
            }
#endif
            EditorGUILayout.EndHorizontal();

#endif // microsplat

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Sync to Terrain (Save)", GUILayout.Height(64)))
            {
                mv.Modify(true, true, false);
                mv.SaveBackToTerrain();
            }

            DoTerrainSyncGUI();
        }
    }
}
        