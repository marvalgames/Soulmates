using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace JBooth.MicroVerseCore
{
    class MenuItems
    {
        const int k_MenuPriority = 0;

        /// <summary>
        /// Stamp priority. Use 0 for same as MicroVerse. Use offset of 10+ for separator.
        /// </summary>
        const int k_MenuPriorityStamps = 100;

        /// <summary>
        /// Stamp menu. Optional. Use blank for root hierarchy, use "Stamps/" for sub-menu
        /// </summary>
        const string MenuStamps = "";

        public static GameObject CreateGO(string name)
        {
            GameObject go = new GameObject(name);
            if (Selection.activeObject != null)
            {
                if (Selection.activeObject as GameObject)
                {
                    go.transform.SetParent(((GameObject)Selection.activeObject).transform);
                }
            }
            
            if (Selection.activeObject is GameObject)
            {
                GameObject parent = Selection.activeObject as GameObject;
                go.transform.SetParent(parent.transform, false);
            }
            if (go.GetComponentInParent<MicroVerse>() == null && MicroVerse.instance != null)
            {
                go.transform.SetParent(MicroVerse.instance.gameObject.transform, true);
            }
            go.transform.localScale = new Vector3(100, 100, 100);
            return go;
        }

        [MenuItem("GameObject/MicroVerse/Create MicroVerse", false, k_MenuPriority + 0)]
        static void CreateMicroVerse()
        {
            if (MicroVerse.instance != null)
            {
                Debug.LogError("MicroVerse already exists");
                Selection.activeObject = MicroVerse.instance.gameObject;
                EditorGUIUtility.PingObject(MicroVerse.instance.gameObject);
            }
            else
            {
                Selection.activeObject = null;
                var mv = CreateGO("MicroVerse").AddComponent<MicroVerse>();
                mv.transform.localScale = Vector3.one;
            }
        }

        [MenuItem("GameObject/MicroVerse/Create MicroVerse With Terrain", false, k_MenuPriority + 2)]
        static void CreateMicroVerseWithTerrain()
        {
            if (MicroVerse.instance != null)
            {
                Debug.LogError("MicroVerse already exists");
                Selection.activeObject = MicroVerse.instance.gameObject;
                EditorGUIUtility.PingObject(MicroVerse.instance.gameObject);
            }
            else
            {
                Selection.activeObject = null;

                var mv = CreateGO("MicroVerse").AddComponent<MicroVerse>();
                mv.transform.localScale = Vector3.one;
                mv.enabled = false;

                #region Create Terrain

                TerrainData terrainData = new TerrainData();
                terrainData.heightmapResolution = 1025;
                terrainData.size = new Vector3(1000, 600, 1000);
                terrainData.baseMapResolution = 1024;
                terrainData.alphamapResolution = 1024;
                terrainData.SetDetailResolution(1024, terrainData.detailResolutionPerPatch);
                
                AssetDatabase.CreateAsset(terrainData, AssetDatabase.GenerateUniqueAssetPath("Assets/New Terrain.asset"));

                GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = "Terrain";

                Terrain terrain = terrainGO.GetComponent<Terrain>();
                terrain.heightmapPixelError = 1f;

                GameObjectUtility.SetParentAndAlign(terrainGO, mv.transform.gameObject);

                StageUtility.PlaceGameObjectInCurrentStage(terrainGO);
                GameObjectUtility.EnsureUniqueNameForSibling(terrainGO);

                Selection.activeObject = mv;

                // Undo.RegisterCreatedObjectUndo(mv, "Create MicroVerse");

                #endregion Create Terrain

                mv.SyncTerrainList();
                mv.enabled = true;
            }
        }

        [MenuItem("GameObject/MicroVerse/Create MicroVerse For Existing Terrains", false, k_MenuPriority + 3)]
        static void CreateMicroVerseForExisting()
        {
            if (MicroVerse.instance != null)
            {
                Debug.LogError("MicroVerse already exists");
                Selection.activeObject = MicroVerse.instance.gameObject;
                EditorGUIUtility.PingObject(MicroVerse.instance.gameObject);
            }
            else
            {
                Terrain[] terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
                if (terrains.Length == 0)
                {
                    Debug.LogError("No terrains found in scene");
                    return;
                }
                Selection.activeObject = null;
                if (EditorUtility.DisplayDialogComplex("Confirm", "Please make sure your terrain data is backed up before performing this operation.", "I have backed up", "Cancel", "") == 0)
                {
                    var mv = CreateGO("MicroVerse").AddComponent<MicroVerse>();
                    mv.transform.localScale = Vector3.one;
                    mv.enabled = false;
                    foreach (var terrain in terrains)
                    {
                        terrain.transform.SetParent(mv.transform, true);
                    }
                    mv.SyncTerrainList();
                    foreach (var terrain in terrains)
                    {
                        var oldPos = terrain.transform.position;
                        terrain.transform.position = new Vector3(terrain.transform.position.x, 0, terrain.transform.position.z);
                        var cp = CreateGO("CopyPaste Stamp " + terrain.name).AddComponent<CopyPasteStamp>();
                        cp.transform.parent = mv.transform;
                        cp.pixelQuantization = true;
                        cp.transform.localScale = terrain.terrainData.size;
                        cp.transform.position = terrain.transform.position;
                        cp.transform.localPosition += new Vector3(terrain.terrainData.size.x / 2, terrain.transform.localPosition.y, terrain.terrainData.size.z / 2); ;
                        var path = AssetDatabase.GetAssetPath(terrain.terrainData);
                        path = path.Replace("\\", "/");
                        if (string.IsNullOrEmpty(path))
                            path = "Assets/";
                        path = path.Substring(0, path.LastIndexOf("/")+1);
                        path = path + terrain.name + "_cpstamp";
                        cp.applyTrees = true;
                        cp.applyDetails = true;
                        cp.applyHoles = true;
                        cp.heightStamp.mode = HeightStamp.CombineMode.Override;
                        CopyPasteStampEditor.Capture(cp, path);
                        terrain.transform.position = oldPos;
                        cp.transform.position = terrain.transform.position;
                        cp.transform.localPosition += new Vector3(terrain.terrainData.size.x / 2, 0, terrain.terrainData.size.z / 2); ;
                    }
                    mv.enabled = true;
                }
                
            }
        }

#if __MICROVERSE_AMBIANCE__
        [MenuItem("GameObject/MicroVerse/Create Ambiant Area", false, k_MenuPriorityStamps + 0)]
        static void CreateAmbiantArea()
        {
            CreateGO("Ambiance Area").AddComponent<AmbientArea>();
        }
#endif

#if __MICROVERSE_VEGETATION__
        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Clear Stamp", false, k_MenuPriorityStamps + 1)]
        static void CreateClearStamp()
        {
            CreateGO("Clear Stamp").AddComponent<ClearStamp>();
        }
#endif

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create CopyPaste Stamp", false, k_MenuPriorityStamps + 2)]
        static void CreateCopyPasteStamp()
        {
            CreateGO("CopyPaste Stamp").AddComponent<CopyPasteStamp>();
        }

#if __MICROVERSE_VEGETATION__
        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Detail Stamp", false, k_MenuPriorityStamps + 3)]
        static void CreateDetailStamp()
        {
            CreateGO("Detail Stamp").AddComponent<DetailStamp>();
        }
#endif

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Height Stamp", false, k_MenuPriorityStamps + 4)]
        static void CreateHeightStamp()
        {
            CreateGO("Height Stamp").AddComponent<HeightStamp>();
        }

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Height Area Effect Stamp", false, k_MenuPriorityStamps + 5)]
        static void CreateHeightAreaEffectStamp()
        {
            CreateGO("Height Area Effect").AddComponent<HeightAreaEffectStamp>();
        }

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Hole Stamp", false, k_MenuPriorityStamps + 6)]
        static void CreateHoleStamp()
        {
            CreateGO("Hole Stamp").AddComponent<HoleStamp>();
        }

#if __MICROVERSE_MASKS__
        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Mask Stamp", false, k_MenuPriorityStamps + 7)]
        static void CreateMaskArea()
        {
            MenuItems.CreateGO("Mask Stamp").AddComponent<MaskStamp>();
        }
#endif

#if __MICROVERSE_OBJECTS__
        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Object Stamp", false, k_MenuPriorityStamps + 8)]
        static void CreateObjectStamp()
        {
            MenuItems.CreateGO("Object Stamp").AddComponent<ObjectStamp>();
        }
#endif

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Occlusion Stamp", false, k_MenuPriorityStamps + 9)]
        static void CreateOcclusionStamp()
        {
            CreateGO("Occlusion Stamp").AddComponent<OcclusionStamp>();
        }

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Paint Falloff Area", false, k_MenuPriorityStamps + 10)]
        static void CreatePaintFalloffArea()
        {
            CreateGO("Paint Falloff Area").AddComponent<PaintFalloffArea>();
        }

        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Texture Stamp", false, k_MenuPriorityStamps + 11)]
        static void CreateTextureStamp()
        {
            CreateGO("Texture Stamp").AddComponent<TextureStamp>();
        }

#if __MICROVERSE_VEGETATION__
        [MenuItem("GameObject/MicroVerse/" + MenuStamps + "Create Tree Stamp", false, k_MenuPriorityStamps + 12)]
        static void CreateTreeStamp()
        {
            CreateGO("Tree Stamp").AddComponent<TreeStamp>();
        }

#endif


    }
}
