using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JBooth.MicroVerseCore
{
    public partial class MicroVerse : MonoBehaviour
    {
#if __MICROSPLAT__
        public JBooth.MicroSplat.TextureArrayConfig msConfig = null;
#endif

        public bool IsUsingMicroSplat()
        {
#if __MICROSPLAT__
            return msConfig != null;
#else
            return false;
#endif
        }

#if UNITY_EDITOR

        /// <summary>
        /// Update in case the hierarchy under microverse changed.
        /// Example: Moving a tree stamp with occlusion stamp up in the hierarchy changes the overall scene
        /// </summary>
        private void OnHierarchyChanged()
        {
            if (UnityEditor.Selection.activeGameObject == null || UnityEditor.Selection.activeGameObject.GetComponentInParent<MicroVerse>() == null)
                return;
            if (MicroVerse.instance.IsModifyingTerrain)
                return;
            // TODO: This causes a loop when the object module is installed, cannot figure out why.
#if !__MICROVERSE_OBJECTS__
            //MicroVerse.instance.Invalidate();
#endif
        }

#if __MICROSPLAT__



        public static void AddTerrainLayerToMicroSplat(TerrainLayer l)
        {
            if (l != null && MicroVerse.instance != null && MicroVerse.instance.msConfig != null)
            {
                MicroVerse.instance.msConfig.AddTerrainLayer(l);
            }
        }

        
#endif  //microsplat


        public static void SaveRT(RenderTexture rt, string name)
        {
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            var tga = tex.EncodeToTGA();
            System.IO.File.WriteAllBytesAsync("Assets/" + name + ".tga", tga);

        }

        int updateFrame = 0;
        int saveBackFrame;
        void OnEditorUpdate()
        {
            // I really wish I could test for some kind of keypress here,
            // but Input.anykey or other options don't work here.
            if (!needUpdate && !IsModifyingTerrain && !IsHeightSyncd
                && modifiedTerrains.Count > 0 && !IsAddingHeightStamp)
            {
                saveBackFrame++;
                if (saveBackFrame > 1)
                {
                    RequestHeightSaveback();
                }
            }
            else
            {
                saveBackFrame = 0;
            }

#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
            
            if (IsUsingProxyRenderer &&  proxyRenderMode == ProxyRenderMode.ProxyWhileUpdating && IsHeightSyncd)
            {
                DisableProxyRenderer();
            }
#endif
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (options.settings.useSceneCulling)
                {
                    SyncTerrainList();
                    foreach (var t in terrains)
                    {
                        var cameras = SceneView.GetAllSceneCameras();
                        Bounds terrainBounds = TerrainUtil.ComputeTerrainBounds(t);
                        
                        bool inBounds = false;
                        bool inTerrainRange = false;
                        bool inVegRange = false;
                        foreach (var cam in cameras)
                        {
                            var dist = Vector3.Distance(cam.transform.position, t.transform.position);
                            if (dist < options.settings.sceneTerrainCullingDistance)
                            {
                                inTerrainRange = true;
                            }
                            if (dist < options.settings.sceneVegetationCullingDistance)
                            {
                                inVegRange = true;
                            }
                            var cameraFrustum = GeometryUtility.CalculateFrustumPlanes(cam);
                            if (GeometryUtility.TestPlanesAABB(cameraFrustum, terrainBounds))
                            {
                                inBounds = true;
                                break;
                            }
                        }
                        var cams = SceneView.GetAllSceneCameras();
                        foreach (var cam in cams)
                        {
                            cam.farClipPlane = options.settings.sceneCameraCullingDistance;
                        }
                        t.drawHeightmap = inTerrainRange && inBounds;
                        t.drawTreesAndFoliage = inVegRange && inBounds;
                    }
                }
            }

#endif

            if (!Application.isPlaying && updateFrame > 1)
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
            updateFrame++;
        }

#endif // unity editor
        }
    }
