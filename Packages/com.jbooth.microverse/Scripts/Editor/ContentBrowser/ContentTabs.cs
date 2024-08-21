
using UnityEditor;
using UnityEngine;
#if __MICROVERSE_SPLINES__
using UnityEngine.Splines;
#endif
using static JBooth.MicroVerseCore.Browser.ContentBrowser;

namespace JBooth.MicroVerseCore.Browser
{
    // default implimentation
    public class ContentTab
    {
        public virtual bool SupportsPlacementMode(PlacementMode pm)
        {
            return pm != PlacementMode.PaintSpline && pm != PlacementMode.PaintArea;
        }

        public virtual void DrawToolbar(ContentBrowser browser)
        {

        }

        public virtual GameObject Spawn(ContentBrowser browser, PresetItem preset, bool wasShiftPressed)
        {
            if (preset == null)
                return null;
            var cd = preset.content;
            if (cd == null)
                return null;
            GameObject instance = null;
            if (cd.prefab != null)
            {
                // TODO: If instantiated as a prefab, using the painter causes a hang
                // PrefabUtility.InstantiatePrefab(selected.prefab) as GameObject;
                // Update: Unpacking the prefab solves the problem. Keeping this comment here in case unforeseen issues arise
                // Original code was: instance = GameObject.Instantiate(selected.prefab);
                instance = PrefabUtility.InstantiatePrefab(cd.prefab) as GameObject;
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = cd.prefab.name;
                instance.transform.SetParent(MicroVerse.instance?.transform);

                // overrides
                // shift at start of drag operation: force falloff type global
                bool forceFalloffTypeGlobal = wasShiftPressed;
                if (forceFalloffTypeGlobal)
                {
                    FalloffOverride falloffOverride = instance.GetComponent<FalloffOverride>();
                    if (falloffOverride && falloffOverride.enabled)
                    {
                        falloffOverride.filter.filterType = FalloffFilter.FilterType.Global;
                    }
                    else
                    {
                        Stamp[] stamps = instance.GetComponentsInChildren<Stamp>();
                        foreach (Stamp stamp in stamps)
                        {
                            if (stamp.GetFilterSet() == null)
                                continue;

                            stamp.GetFilterSet().falloffFilter.filterType = FalloffFilter.FilterType.Global;
                        }
                    }
                }

                MicroVerse.instance?.Invalidate(null); // TODO: Do better?
            }
            return instance;
        }

#if __MICROVERSE_SPLINES__

        /// <summary>
        /// Return if you want to continue spawning the object beyond the spline.
        /// </summary>
        /// <param name="pm"></param>
        /// <param name="splineContainer"></param>
        /// <returns></returns>
        public virtual bool BeforeSplineSpawn(PlacementMode pm, SplineContainer splineContainer, ContentCollection collection)
        {
            return true;
        }
        public virtual void AfterSplineSpawn(PlacementMode pm, GameObject spawned, SplineContainer splineContainer, SplineArea area, int areaUses)
        {
            if (area != null)
            {
                // add clear stamp if control is pressed and data needs it..
#if __MICROVERSE_VEGETATION__
                if (Event.current.control)
                {
                    // don't add if content is already setup to clear
                    var clearStampExisting = spawned.GetComponentInChildren<ClearStamp>();
                    if (clearStampExisting == null)
                    {
                        bool clearObjects = false;
                        bool clearDetails = spawned.GetComponentInChildren<DetailStamp>() != null;
                        bool clearTrees = spawned.GetComponentInChildren<TreeStamp>() != null;
#if __MICROVERSE_OBJECTS__
                        clearObjects = spawned.GetComponentInChildren<ObjectStamp>() != null;
#endif
                        if (clearObjects || clearDetails || clearTrees)
                        {
                            GameObject clearObj = new GameObject("ClearStamp");
                            Undo.RegisterCreatedObjectUndo(clearObj, "Content Browser Create Object");
                            Undo.SetTransformParent(clearObj.transform, spawned.transform.parent, "Content Browser Create Object");
                            Undo.SetTransformParent(spawned.transform, clearObj.transform, "Content Browser Create Object");
                            var clear = Undo.AddComponent<ClearStamp>(clearObj);

                            clear.filterSet.falloffFilter.filterType = FalloffFilter.FilterType.SplineArea;
                            clear.filterSet.falloffFilter.splineArea = area;
                            clear.filterSet.falloffFilter.splineAreaFalloff = 10;
                            clear.clearTrees = clearTrees;
                            clear.clearDetails = clearDetails;
#if __MICROVERSE_OBJECTS__
                            clear.clearObjects = clearObjects;
#endif
                        }
                    }
                    else
                    {
                        clearStampExisting.filterSet.falloffFilter.splineArea = area;
                        clearStampExisting.filterSet.falloffFilter.filterType = FalloffFilter.FilterType.SplineArea;
                    }
                }
#endif
                MicroVerse.instance.Invalidate(area.GetBounds());
            }
        }
#endif
    }

    public class TextureTab : ContentTab
    {
        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return true;
        }

        public override void DrawToolbar(ContentBrowser browser)
        {
            EditorGUILayout.LabelField("Size", GUILayout.Width(80));
            browser.textureStampDefaultScale = EditorGUILayout.Vector3Field("", browser.textureStampDefaultScale, GUILayout.Width(200));
        }

        public override GameObject Spawn(ContentBrowser browser, PresetItem preset, bool wasShiftPressed)
        {
            var instance = base.Spawn(browser, preset, wasShiftPressed);
            if (instance != null)
                instance.transform.localScale = browser.textureStampDefaultScale;
            return instance;
        }

    }

    public class ObjectTab : ContentTab
    {
    }

    public class AmbienceTab : ContentTab
    {
        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return true;
        }
#if __MICROVERSE_AMBIANCE__ && __MICROVERSE_SPLINES__
        public override void AfterSplineSpawn(PlacementMode pm, GameObject go, SplineContainer splineContainer, SplineArea area, int areaUses)
        {
            base.AfterSplineSpawn(pm, go, splineContainer, area, areaUses);
            var aas = go.GetComponentsInChildren<AmbientArea>();
            foreach (var aa in aas)
            {
                if (pm == PlacementMode.PaintSpline)
                {
                    aa.falloff = AmbientArea.AmbianceFalloff.Spline;
                    aa.spline = splineContainer;
                }
                else
                {
                    aa.falloff = AmbientArea.AmbianceFalloff.SplineArea;
                    aa.spline = splineContainer;
                }
                if (areaUses == 0 && area != null)
                {
                    Undo.DestroyObjectImmediate(area);
                }

            }
        }
#endif
    }

    public class VegetationTab : ContentTab
    {
        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return true;
        }

        public override GameObject Spawn(ContentBrowser browser, PresetItem preset, bool wasShiftPressed)
        {
            GameObject instance = base.Spawn(browser, preset, wasShiftPressed);
            if (instance != null)
            {
                instance.transform.localScale = browser.vegetationStampDefaultScale;
#if __MICROVERSE_VEGETATION__
                var trees = instance.GetComponentsInChildren<TreeStamp>();
                var details = instance.GetComponentsInChildren<DetailStamp>();
                foreach (var t in trees)
                {
                    t.seed = (uint)UnityEngine.Random.Range(0, 99);
                }
#endif
            }
            return instance;
        }

        public override void DrawToolbar(ContentBrowser browser)
        {
            EditorGUILayout.LabelField("Size", GUILayout.Width(80));
            browser.vegetationStampDefaultScale = EditorGUILayout.Vector3Field("", browser.vegetationStampDefaultScale, GUILayout.Width(200));

        }
    }

    public class RoadTab : ContentTab
    {
        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return pm != PlacementMode.PaintArea;
        }
#if __MICROVERSE_ROADS__ && __MICROVERSE_SPLINES__
        RoadSystem FindOrCreateRoadSystem(ContentCollection collection)
        {
            RoadSystem[] roadSystems = null;
            if (MicroVerse.instance == null)
            {
                roadSystems = GameObject.FindObjectsOfType<RoadSystem>();
            }
            else
            {
                roadSystems = MicroVerse.instance.GetComponentsInChildren<RoadSystem>();
            }
            RoadSystem rs = null;
            foreach (var crs in roadSystems)
            {
                if (!string.IsNullOrEmpty(crs.contentID) && crs.contentID == collection.id)
                {
                    rs = crs;
                    break;
                }
            }
            if (rs == null)
            {
                string prefixName = "Road System";
                RoadSystemConfig rscfg = null;
                if (collection.systemConfig != null)
                {
                    rscfg = (RoadSystemConfig)collection.systemConfig;
                    if (rscfg != null)
                    {
                        if (!string.IsNullOrEmpty(rscfg.namePrefix))
                            prefixName = rscfg.namePrefix + " System";

                    }
                }

                GameObject go = new GameObject(prefixName);
                rs = go.AddComponent<RoadSystem>();
                rs.contentID = collection.id;
                rs.systemConfig = rscfg;

                if (MicroVerse.instance != null)
                {
                    rs.transform.SetParent(MicroVerse.instance.transform);
                }
                rs.transform.localPosition = Vector3.zero;
                rs.transform.localScale = Vector3.one;
                rs.transform.localRotation = Quaternion.identity;
            }

            return rs;
        }

        public override GameObject Spawn(ContentBrowser browser, PresetItem preset, bool wasShiftPressed)
        {
            var instance = base.Spawn(browser, preset, wasShiftPressed);

            var rs = FindOrCreateRoadSystem(preset.collection);
            
            instance.transform.SetParent(rs.transform);
            rs.UpdateAll();
            return instance;
        }

        public override bool BeforeSplineSpawn(PlacementMode pm, SplineContainer splineContainer, ContentCollection collection)
        {
            if (pm != PlacementMode.PaintSpline)
                return true;
            if (collection == null || collection.systemConfig == null)
            {
                GameObject.DestroyImmediate(splineContainer.gameObject);
                return false;
            }
      
            var rsc = (RoadSystemConfig)collection.systemConfig;
            if (rsc != null && rsc.splinePaintDefault != null)
            {
                var rs = FindOrCreateRoadSystem(collection);
                var road = Undo.AddComponent<Road>(splineContainer.gameObject);
                road.config = rsc.splinePaintDefault;
                road.splineContainer = splineContainer;
                road.modifiesTerrain = rsc.modifyTerrainByDefault;
                road.gameObject.transform.SetParent(rs.transform);
                road.Generate();
                rs.UpdateAll();
            }

            return false;
        }

#endif

        public override void DrawToolbar(ContentBrowser browser)
        {
            EditorGUILayout.LabelField("Height Offset", GUILayout.Width(120));
            browser.roadHeightOffset = EditorGUILayout.FloatField("", browser.roadHeightOffset, GUILayout.Width(60));
        }
    
    }

    public class CaveTab : RoadTab
    {

    }

    public class GlobalTab : ContentTab
    {
        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return false;
        }
    }

    public class BiomeTab : ContentTab
    {
        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return true;
        }
    }


    public class HeightTab : ContentTab
    {

        public override bool SupportsPlacementMode(PlacementMode pm)
        {
            return true;
        }

        public override void DrawToolbar(ContentBrowser browser)
        {
            EditorGUILayout.PrefixLabel("Falloff Type");
            browser.filterTypeDefault = (FalloffDefault)EditorGUILayout.EnumPopup(browser.filterTypeDefault, GUILayout.Width(120));

            EditorGUILayout.LabelField("Size", GUILayout.Width(80));
            browser.heightStampDefaultScale = EditorGUILayout.Vector3Field("", browser.heightStampDefaultScale, GUILayout.Width(200));
        }
        

        public override GameObject Spawn(ContentBrowser browser, PresetItem preset, bool wasShiftPressed)
        {
            GameObject instance = null;
            var cd = preset.content;
            if (cd == null)
                return null;
            if (cd.prefab != null)
            {
                // TODO: If instantiated as a prefab, using the painter causes a hang
                // PrefabUtility.InstantiatePrefab(selected.prefab) as GameObject;
                // Update: Unpacking the prefab solves the problem. Keeping this comment here in case unforeseen issues arise
                // Original code was: instance = GameObject.Instantiate(selected.prefab);
                instance = PrefabUtility.InstantiatePrefab(cd.prefab) as GameObject;
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = cd.prefab.name;
                instance.transform.localScale = browser.heightStampDefaultScale;
                
            }
            else if (cd.stamp != null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(new GUID(cd.stamp)));
                if (tex != null)
                {
                    instance = new GameObject(tex.name + " (Height Stamp)");
                    HeightStamp heightStamp = instance.AddComponent<HeightStamp>();

                    heightStamp.stamp = tex;
                    heightStamp.mode = HeightStamp.CombineMode.Add;

                    heightStamp.falloff.filterType = (FalloffFilter.FilterType)browser.filterTypeDefault;
                    heightStamp.falloff.falloffRange = new Vector2(0.8f, 1f);

                    // overrides
                    bool autoScaleTerrain = wasShiftPressed;
                    if (autoScaleTerrain)
                    {
                        Terrain[] terrains = MicroVerse.instance.GetComponentsInChildren<Terrain>();

                        Bounds worldBounds = TerrainUtil.ComputeTerrainBounds(terrains);

                        // scale
                        float x = worldBounds.size.x;
                        float y = worldBounds.size.y;
                        float z = worldBounds.size.z;

                        // if y is dynamic and depends on the current bounds
                        // however if it's very low or 0 in case it's the first terrain we use
                        // a heuristic to calculate a resonable height. the values are just arbitrary
                        float threshold = 10f;
                        if (y < threshold)
                        {
                            if (Terrain.activeTerrain)
                            {
                                y = TerrainUtil.ComputeTerrainSize(Terrain.activeTerrain).y * 0.1f;
                            }
                            else
                            {
                                y = threshold;
                            }
                        }

                        instance.transform.localScale = new Vector3(x, y, z);
                    }
                    else
                    {
                        instance.transform.localScale = browser.heightStampDefaultScale;
                    }
                }
            }
            if (MicroVerse.instance != null)
            {
                instance.transform.SetParent(MicroVerse.instance.transform);
            }
            return instance;
        }

        
    }
}
