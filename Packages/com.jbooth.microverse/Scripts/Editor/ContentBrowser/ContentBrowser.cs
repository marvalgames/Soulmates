using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using static JBooth.MicroVerseCore.Browser.Package;
using Unity.Mathematics;
using JBooth.MicroVerseCore.Browser.CollectionWizard;

#if __MICROVERSE_SPLINES__
using UnityEngine.Splines;
#endif

namespace JBooth.MicroVerseCore.Browser
{
    public class ContentBrowser : EditorWindow
    {
        static GUIContent COptionalVisibility = new GUIContent("O", "Optional content visibility");
        static GUIContent CDescriptionVisibility = new GUIContent("D", "Description visibility");
        static GUIContent CHelpVisibility = new GUIContent("?", "Help information visibility");

        const string FilterOptionAllText = "All";

        [MenuItem("Window/MicroVerse/Content Browser")]
        public static void CreateWindow()
        {
            var w = EditorWindow.GetWindow<ContentBrowser>();
            w.Show();
            w.wantsMouseEnterLeaveWindow = true;
            w.wantsMouseMove = true;
            w.titleContent = new GUIContent("MicroVerse Browser");
        }

        public static List<T> LoadAllInstances<T>() where T : ScriptableObject
        {
            UnityEngine.Profiling.Profiler.BeginSample("Loading All browser content");
            var ret = AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<T>)
                        .ToList();
            UnityEngine.Profiling.Profiler.EndSample();

            return ret;


        }

        public enum Grouping
        {
            Package,
            ContentType
        }

        /// <summary>
        /// Subset of the Falloff filter type
        /// </summary>
        public enum FalloffDefault
        {
            Box = FalloffFilter.FilterType.Box,
            Range = FalloffFilter.FilterType.Range,
        }

        public enum Tab 
        {
            Height = ContentType.Height,
            Texture = ContentType.Texture,
            Vegetation = ContentType.Vegetation,
            Objects = ContentType.Objects,
            Audio = ContentType.Audio,
            Biomes = ContentType.Biomes,
            Roads = ContentType.Roads,
            Caves = ContentType.Caves,
            Global = ContentType.Global
        }

        public ContentTab[] contentTabs = new ContentTab[9]
        {
            new HeightTab(),
            new TextureTab(),
            new VegetationTab(),
            new ObjectTab(),
            new AmbienceTab(),
            new BiomeTab(),
            new RoadTab(),
            new CaveTab(),
            new GlobalTab()
        };

        GUIContent[] tabNames = new GUIContent[9] { new GUIContent("Height"), new GUIContent("Texture"), new GUIContent("Vegetation"), new GUIContent("Objects"), new GUIContent("Audio"), new GUIContent("Biomes"), new GUIContent("Roads"), new GUIContent("Caves"), new GUIContent("Global") };
        
        public static Tab tab = Tab.Height;

        List<Package> filteredCollectionPackages = null;
        List<Package> filteredAdPackages = null;
        Package selectedPackage;

        List<BrowserContent> filteredCollections = null;
        List<BrowserContent> filteredAds = null;
        List<PresetItem> filteredPresets = null;

        List<ContentCollection> allCollections = null;
        List<BrowserContent> allContent = null;


        private static int headerWidth = 180;
        private static int listWidth = headerWidth + 10;

        private Vector2 listScrollPosition = Vector2.zero;

        private Color selectionColor = Color.green;

        public FalloffDefault filterTypeDefault = FalloffDefault.Box;
        public Vector3 heightStampDefaultScale = new Vector3(300, 120, 300);
        public Vector3 textureStampDefaultScale = new Vector3(300, 120, 300);
        public Vector3 vegetationStampDefaultScale = new Vector3(300, 120, 300);
        public float roadHeightOffset = 0.0f;

        /// <summary>
        /// Selected item per tab, hash of browser content, since the object can change
        /// </summary>
        private Dictionary<Tab, Package> selectedTabItems = new Dictionary<Tab, Package>();

        private bool optionalVisible = true;
        private bool descriptionVisible = true;
        private bool helpBoxVisible = true;

        private ContentSelectionGrid contentSelectionGrid;
        private ContentDragHandler contentDragHandler;

        /// <summary>
        /// Index of the Author popup
        /// </summary>
        private int selectedAuthorFilterIndex = 0;

        /// <summary>
        /// Search text of the selected author filter.
        /// Don't use this string directly, rather use dedicated methods like <see cref="IsInFilter(BrowserContent)"/>.
        /// </summary>
        private string selectedAuthorFilterText = FilterOptionAllText;

        private Grouping grouping = Grouping.Package;

        /// <summary>
        /// Get all presets that are currently visible in the browser
        /// </summary>
        /// <returns></returns>
        public List<PresetItem> GetPresets()
        {
            if (selectedPackage == null)
                return new();

            List<PresetItem> presets = filteredPresets.Where(x => x.collection.packName == selectedPackage.packName).ToList();

            return presets;
        }

        public Package GetSelectedPackage()
        {
            return selectedPackage;
        }
        public Tab GetSelectedTab()
        {
            return tab;
        }

        /// <summary>
        /// Get the selected browser content if it can be uniquely identified.
        /// Might not be the case if the same author creates multiple of the same content id and content type.
        /// In case it can't be uniquely identified an error popup will show up and null will be returned.
        /// </summary>
        /// <returns>The selected content asset or null if no content is available or multiple assets were found</returns>
        public BrowserContent GetSelectedBrowserContentAsset()
        {

            // at least package is required for detecting the current collection
            if (grouping == Grouping.ContentType)
            {
                EditorUtility.DisplayDialog("Error", $"Not supported for grouping by {grouping}.\nAborting operation.", "Ok");
                return null;
            }

            BrowserContent contentAsset = null;

            int count = 0;

            foreach (ContentCollection contentCollection in filteredCollections)
            {
                if (!IsInFilter(contentCollection))
                    continue;

                if (contentCollection.id == selectedPackage.id && contentCollection.contentType == selectedPackage.contentType)
                {
                    contentAsset = contentCollection;
                    count++;
                }
            }

            if( count > 1)
            {
                EditorUtility.DisplayDialog("Error", $"Multiple content collections found: {count}\nConsider using a filter, eg Author filter.\nAborting operation.", "Ok");
                return null;
            }

            return contentAsset;
        }

        /// <summary>
        /// This returns the first ad for the currently selected package that's found or null if none was found.
        /// </summary>
        /// <returns></returns>
        public ContentAd GetFirstSelectedAd()
        {
            if (selectedPackage == null || selectedPackage.packageType != PackageType.Ad)
                return null;

            foreach( BrowserContent content in allContent)
            {
                if (!(content is ContentAd))
                    continue;

                if (content.id == selectedPackage.id && content.contentType == selectedPackage.contentType)
                    return content as ContentAd;
            }

            return null;
        }

        public List<BrowserContent> GetAllContent()
        {
            return allContent;
        }

        private void OnEnable()
        {
            // initialize settings; can't be initialized as global variable because this class is a scriptable object
            optionalVisible = MicroVerseSettingsProvider.OptionalVisible;
            descriptionVisible = MicroVerseSettingsProvider.DescriptionVisible;
            helpBoxVisible = MicroVerseSettingsProvider.HelpVisible;

            // content selection grid
            if(contentSelectionGrid == null)
            {
                contentSelectionGrid = new ContentSelectionGrid(this);
            }

            contentSelectionGrid.OnEnable();

            // content drag handler
            if (contentDragHandler == null)
            {
                contentDragHandler = new ContentDragHandler(this);
            }

            contentDragHandler.OnEnable();

#if __MICROVERSE_SPLINES__
            SceneView.beforeSceneGui -= OnSceneGUIEvent;
            SceneView.beforeSceneGui += OnSceneGUIEvent;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
#endif

        }

#if __MICROVERSE_SPLINES__
        private void OnSceneGUI(SceneView view)
        {
            if (Event.current.type == EventType.Repaint && paintStroke != null && paintStroke.Count > 1)
            {
                Handles.color = Color.blue;
                Handles.DrawAAPolyLine(20, paintStroke.ToArray());
                if (paintStroke.Count > 2 && placementMode == PlacementMode.PaintArea)
                {
                    Handles.DrawAAPolyLine(20, new Vector3[] { paintStroke[0], paintStroke[paintStroke.Count - 1] });
                }
                HandleUtility.Repaint();
            }
        }
#endif


#if __MICROVERSE_SPLINES__
        List<float3> m_Stroke = new List<float3>(1024);
        List<float3> m_Reduced = new List<float3>(512);
        List<Vector3> paintStroke = new List<Vector3>(1024);
        const int LeftMouseButton = 0;
        static float splineStrokeDeltaThreshold = .1f;
        static float splinePointReductionEpsilon = 5f;
        static SplineContainer splineContainer;
        // Tension affects how "curvy" splines are at knots. 0 is a sharp corner, 1 is maximum curvitude.
        static float splineTension = 0.25f;

   
        void RebuildSpline()
        {
            List<float3> samples = m_Stroke.Select(x => (float3)splineContainer.transform.InverseTransformPoint(x) + new float3(0, 0, 0)).ToList();

            // Before setting spline knots, reduce the number of sample points.
            SplineUtility.ReducePoints(samples, m_Reduced, splinePointReductionEpsilon);

            var spline = splineContainer.Spline;

            // Assign the reduced sample positions to the Spline knots collection. Here we are constructing new
            // BezierKnots from a single position, disregarding tangent and rotation. The tangent and rotation will be
            // calculated automatically in the next step wherein the tangent mode is set to "Auto Smooth."
            spline.Knots = m_Reduced.Select(x => new BezierKnot(x));


            var all = new SplineRange(0, spline.Count);

            // Sets the tangent mode for all knots in the spline to "Auto Smooth."
            spline.SetTangentMode(all, TangentMode.AutoSmooth);

            // Sets the tension parameter for all knots. Note that the "Tension" parameter is only applicable to
            // "Auto Smooth" mode knots.
            spline.SetAutoSmoothTension(all, splineTension);
           
            spline.Closed = placementMode == PlacementMode.PaintArea;
            EditorUtility.SetDirty(splineContainer);

            //m_Stats.text = $"Input Sample Count: {m_Stroke.Count}\nSpline Knot Count: {m_Reduced.Count}";
            //Debug.Log($"Input Sample Count: {m_Stroke.Count}\nSpline Knot Count: {m_Reduced.Count}");

        }


        private void OnSceneGUIEvent(SceneView sceneView)
        {
            // perform method only when the mouse is really in the sceneview; the scene view would register other events as well
            var isMouseInSceneView = new Rect(0, 0, sceneView.position.width, sceneView.position.height).Contains(Event.current.mousePosition);
            if (!isMouseInSceneView)
            {
                return;
            }
            if (placementMode == PlacementMode.Place)
                return;
            if (MicroVerse.instance == null)
                return;
            
            if (Event.current.isMouse)
            {
                // left button = 0; right = 1; middle = 2
                if (Event.current.button == LeftMouseButton)
                {
                    // drag case
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    {
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        // prefer terrain collisions..
                        var hits = Physics.RaycastAll(ray.origin, ray.direction, Mathf.Infinity);
                        if (hits.Length > 0)
                        {
                            RaycastHit hit = hits[0];
                            foreach (var h in hits)
                            {
                                if (h.collider.GetComponent<Terrain>())
                                {
                                    hit = h;
                                    break;
                                }
                            }
                            if (Event.current.type == EventType.MouseDown)
                            {
                                m_Stroke.Clear();
                                paintStroke.Clear();
         
                                string name = "Spline";
                                if (placementMode == PlacementMode.PaintArea)
                                {
                                    name = "SplineArea";
                                }
                                else if (placementMode == PlacementMode.PaintSplinePath)
                                {
                                    name = "SplinePath";
                                }
                                if (placementMode != PlacementMode.PaintSplinePath)
                                {
                                    if (contentSelectionGrid.selectedPresetItem != null && contentSelectionGrid.selectedPresetItem.content != null && contentSelectionGrid.selectedPresetItem.content.prefab != null)
                                    {
                                        name += " - " + contentSelectionGrid.selectedPresetItem.content.prefab.name;
                                    }
                                    else if (contentSelectionGrid.selectedPresetItem != null && contentSelectionGrid.selectedPresetItem.content != null && contentSelectionGrid.selectedPresetItem.content.stamp != null)
                                    {
                                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(new GUID(contentSelectionGrid.selectedPresetItem.content.stamp)));
                                        if (tex != null)
                                        {
                                            name += " - " + tex.name;
                                        }
                                    }
                                }

                                var go = new GameObject(name);
                                Undo.RegisterCreatedObjectUndo(go, "Content Browser Create Object");
                                Undo.SetTransformParent(go.transform, MicroVerse.instance.transform, "Content Browser Create Object");
                                splineContainer = Undo.AddComponent<SplineContainer>(go);
                                m_Stroke.Add(hit.point);
                                paintStroke.Add(hit.point);

                            }
                            else
                            {
                                Vector3 lastKnotWorldPosition = m_Stroke[m_Stroke.Count - 1];
                                float distance = Vector3.Distance(lastKnotWorldPosition, hit.point);

                                if (distance > splineStrokeDeltaThreshold)
                                {
                                    m_Stroke.Add(hit.point);
                                    paintStroke.Add(hit.point);
                                }
                            }
                        }

                        Event.current.Use();
                    }
                    if (Event.current.type == EventType.MouseUp && splineContainer != null)
                    {
                        RebuildSpline();
                        m_Stroke.Clear();
                        paintStroke.Clear();
                        if (placementMode == PlacementMode.PaintSplinePath)
                        {
                            var sp = Undo.AddComponent<SplinePath>(splineContainer.gameObject);
                            sp.width = 6;
                            sp.smoothness = 4;
                        }
                        else if (placementMode == PlacementMode.PaintArea ||
                            placementMode == PlacementMode.PaintSpline)
                        {
                            var area = Undo.AddComponent<SplineArea>(splineContainer.gameObject);
                            area.spline = splineContainer;
                            
                            if (splineContainer.Spline != null && splineContainer.Spline.Count > 1)
                            {
                                bool canSpawn = true;
#if __MICROVERSE_SPLINES__
                                var collection = contentSelectionGrid.selectedPresetItem?.collection;
                                canSpawn = contentTabs[(int)tab].BeforeSplineSpawn(placementMode, splineContainer, collection);
#endif
                                if (canSpawn && contentSelectionGrid.selectedPresetItem != null)
                                {
                                    var spawned = contentTabs[(int)tab].Spawn(this, contentSelectionGrid.selectedPresetItem, false);
 
                                    if (spawned != null)
                                    {
                                        Undo.RegisterCreatedObjectUndo(spawned, "Content Browser Create Object");
                                        Undo.SetTransformParent(spawned.transform, splineContainer.transform, "Content Browser Create Object");

                                        int areaUses = 0;
                                        // move spawned object to center of spline area
                                        Vector3 min = new Vector3(99999, 99999, 99999);
                                        Vector3 max = new Vector3(-99999, -99999, -99999);
                                        float3 avg = splineContainer.Spline[0].Position;
                                        for (int i = 1; i < splineContainer.Spline.Count; ++i)
                                        {
                                            var p = splineContainer.Spline[i].Position;
                                            avg += p;
                                            min = Vector3.Min(min, p);
                                            max = Vector3.Max(max, p);
                                            
                                        }

                                        avg /= splineContainer.Spline.Count;
                                        spawned.transform.position = splineContainer.transform.localToWorldMatrix.MultiplyPoint(avg);
                                        Vector3 range = max - min;
                                        range.y = 120;
                                        spawned.transform.localScale = range;
                                        // now setup stamps
                                        Stamp[] stamps = spawned.GetComponentsInChildren<Stamp>();
                                        foreach (var s in stamps)
                                        {
                                            if (s.stampVersion == 0)
                                                s.stampVersion = 1;
                                        }
                                        var over = spawned.GetComponent<FalloffOverride>();
                                        if (over != null)
                                        {
                                            var falloff = over.filter;
                                            if (falloff != null)
                                            {
                                                falloff.filterType = FalloffFilter.FilterType.SplineArea;
                                                falloff.splineArea = area;
                                                falloff.falloffRange = Vector2.one;
                                                if (placementMode == PlacementMode.PaintSpline)
                                                {
                                                    falloff.splineAreaFalloffBoost = 10;
                                                    falloff.splineAreaFalloff = 5;
                                                }
                                                areaUses++;
                                            }
                                        }
                                        else
                                        {
                                            foreach (var s in stamps)
                                            {
                                                var falloffFilter = s.GetFilterSet();
                                                FalloffFilter falloff = null;
                                                if (falloffFilter != null)
                                                {
                                                    falloff = falloffFilter.falloffFilter;
                                                }
                                                else
                                                {
                                                    // height stamps don't have falloff sets
                                                    var hm = s as HeightStamp;
                                                    if (hm != null)
                                                    {
                                                        falloff = hm.falloff;
                                                    }
                                                }
                                                if (falloff != null)
                                                {
                                                    falloff.filterType = FalloffFilter.FilterType.SplineArea;
                                                    falloff.splineArea = area;
                                                    falloff.falloffRange = Vector2.one;
                                                    if (placementMode == PlacementMode.PaintSpline)
                                                    {
                                                        falloff.splineAreaFalloffBoost = 10;
                                                        falloff.splineAreaFalloff = 5;
                                                    }
                                                    areaUses++;
                                                }
                                            }
                                        }
                                        contentTabs[(int)tab].AfterSplineSpawn(placementMode, spawned, splineContainer, area, areaUses);
                                    }
                                }
                            }
                        }
                        // clear spline container too
                        splineContainer = null;
                        if (placementMode != PlacementMode.Place && Event.current.shift == false)
                        {
                            placementMode = PlacementMode.Place;
                        }
                        Repaint();
                    }
                }
            }
            
        }

        

#endif

        private void OnDisable()
        {
            // content selection grid
            contentSelectionGrid.OnDisable();
            contentSelectionGrid = null;

            // content drag handler
            contentDragHandler.OnDisable();
            contentDragHandler = null;
#if __MICROVERSE_SPLINES__
            SceneView.beforeSceneGui -= OnSceneGUIEvent;
            SceneView.duringSceneGui -= OnSceneGUI;
#endif
        }

        bool HasContentForAd(ContentAd ad, List<ContentCollection> content)
        {
            for (int i = 0; i < content.Count; ++i)
            {
                if (content[i].id == ad.id && !string.IsNullOrEmpty(ad.id))
                    return true;
                if (content[i].packName == ad.packName && !string.IsNullOrEmpty(ad.packName))
                    return true;
                
            }
            return false;
        }

        private void OnFocus()
        {
            allContent = LoadAllInstances<BrowserContent>();
        }


        public enum PlacementMode
        {
            Place = 0,
            PaintSplinePath = 1,
            PaintSpline = 2,
            PaintArea = 3
        }


        Texture2D splineIcon;
        Texture2D splinePathIcon;
        Texture2D splineAreaIcon;
        Texture2D placementIcon;
        
        static int placementModeHeight = 24;
        public static PlacementMode placementMode = PlacementMode.Place;

        void DrawPlacementTools()
        {
            if (splineIcon == null)
            {
                string filename = EditorGUIUtility.isProSkin ? "d_microverse_spline_icon" : "microverse_spline_icon";
                splineIcon = Resources.Load<Texture2D>(filename);
            }
            if (splinePathIcon == null)
            {
                string filename = EditorGUIUtility.isProSkin ? "d_microverse_splinepath_icon" : "microverse_splinepath_icon";
                splinePathIcon = Resources.Load<Texture2D>(filename);
            }
            if (splineAreaIcon == null)
            {
                string filename = EditorGUIUtility.isProSkin ? "d_microverse_splinearea_icon" : "microverse_splinearea_icon";
                splineAreaIcon = Resources.Load<Texture2D>(filename);
            }
            if (placementIcon == null)
            {
                string filename = EditorGUIUtility.isProSkin ? "d_microverse_placement_icon" : "microverse_placement_icon";
                placementIcon = Resources.Load<Texture2D>(filename);
            }
            bool supportsPlacement = true;
            bool supportsSpline = contentTabs[(int)tab].SupportsPlacementMode(PlacementMode.PaintSpline);
            bool supportsSplineArea = contentTabs[(int)tab].SupportsPlacementMode(PlacementMode.PaintArea);
            bool supportsSplinePath = contentTabs[(int)tab].SupportsPlacementMode(PlacementMode.PaintSplinePath);

            GUIContent[] toolContent;
            PlacementMode[] placementModes;

            int count = (supportsPlacement ? 1 : 0) + (supportsSpline ? 1 : 0) + (supportsSplineArea ? 1 : 0) + (supportsSplinePath ? 1 : 0);
            toolContent = new GUIContent[count];
            placementModes = new PlacementMode[count];
            toolContent[0] = new GUIContent("", placementIcon, "Drag and Drop into the scene");
            placementModes[0] = PlacementMode.Place;
            int index = 1;
            if (supportsSpline)
            {
                toolContent[index] = new GUIContent("", splineIcon, "Use the selected content constrained to an open spline");
                placementModes[index] = PlacementMode.PaintSpline;
                index++;
            }
            if (supportsSplineArea)
            {
                toolContent[index] = new GUIContent("", splineAreaIcon, "Use the selected content constrained to a spline area");
                placementModes[index] = PlacementMode.PaintArea;
                index++;
            }

            if (supportsSplinePath)
            {
                toolContent[index] = new GUIContent("", splinePathIcon, "Create a spline path by painting");
                placementModes[index] = PlacementMode.PaintSplinePath;
                index++;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(196));
            if (count > 1)
            {
                EditorGUILayout.LabelField("Placement:", GUILayout.Width(64), GUILayout.Height(placementModeHeight));
                int placeIdx = 0;
                for (int i = 0; i < placementModes.Length; ++i)
                {
                    if (placementModes[i] == placementMode)
                    {
                        placeIdx = i;
                    }
                }
                placeIdx = GUILayout.Toolbar(placeIdx, toolContent, GUILayout.Width(count * 48), GUILayout.Height(placementModeHeight));
                placementMode = placementModes[placeIdx];
            }

#if __MICROVERSE_SPLINES__
            if (placementMode != PlacementMode.Place)
            {
                splineStrokeDeltaThreshold = EditorGUILayout.Slider("Delta", splineStrokeDeltaThreshold, 0.1f, 0.4f);
                splinePointReductionEpsilon = EditorGUILayout.Slider("Epsilon", splinePointReductionEpsilon, 1, 20f);
                splineTension = EditorGUILayout.Slider("Tension", splineTension, 0.1f, 0.5f);
            }
#endif
            EditorGUILayout.EndHorizontal();
        }

        void DrawTabToolbar()
        {
            
            EditorGUILayout.BeginVertical("box");
            {
                float prev = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f;
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        DrawPlacementTools();

                        contentTabs[(int)tab].DrawToolbar(this);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUIUtility.labelWidth = prev;

            }
            EditorGUILayout.EndVertical();
            
        }

        /// <summary>
        /// Get popup options by getting a distinct list of all authors.
        /// Only uses content collections, no Ads considers
        /// </summary>
        /// <returns></returns>
        private string[] GetFilterOptions()
        {
            List<string> options = allContent
                .Where( x => x is ContentCollection) // installed
                .Select(x => x.author) // author name
                .Distinct() // unique
                .ToList();

            options.Sort();
            
            // add "All" on top of the list
            options.Insert(0, FilterOptionAllText);

            return options.ToArray();
        }

        /// <summary>
        /// Determine whether the collection is in the filter or not.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        private bool IsInFilter( BrowserContent collection)
        {
            if (collection != null && collection.renderPipeline != 0)
            {
#if USING_HDRP
                if (!collection.renderPipeline.HasFlag(BrowserContent.RenderPipeline.HDRP))
                    return false;
#elif USING_URP
                if (!collection.renderPipeline.HasFlag(BrowserContent.RenderPipeline.URP))
                    return false;
#else
                if (!collection.renderPipeline.HasFlag(BrowserContent.RenderPipeline.Standard))
                    return false;
#endif
            }
            // All
            if (selectedAuthorFilterIndex == 0)
                return true;

            if (collection == null || collection.author == null)
                return true;

            return selectedAuthorFilterText.Equals( collection.author);
        }

        /// <summary>
        /// Update the content to be displayed
        /// </summary>
        private void UpdateFilteredContent( Tab selectedTab)
        {
            if (allContent == null)
                allContent = LoadAllInstances<BrowserContent>();

            List<ContentAd> allAds = allContent
                .Where(x => (int)x.contentType == (int)selectedTab)
                .Where(x => x is ContentAd)
                .Cast<ContentAd>()
                .Where(x => IsInFilter(x))
                .ToList();

            allCollections = allContent
                .Where(x => (int)x.contentType == (int)selectedTab)
                .Where(x => x is ContentCollection)
                .Cast<ContentCollection>()
                .Where(x => IsInFilter(x))
                .ToList();


            // get all Ad ids which are invalid, ie require an object, but the object isn't set
            List<string> invalidAdIds = allAds
                .Where(x => x.requireInstalledObject && x.installedObject == null && x.id != null)
                .Select(x => x.id)
                .ToList();

            // remove collections which have invalid Ad ids
            allCollections = allCollections.Where(x => !invalidAdIds.Contains(x.id)).ToList();

            // create filtered content
            filteredCollections = new List<BrowserContent>();
            filteredCollections.AddRange(allCollections);

            // add ads
            filteredAds = new List<BrowserContent>();
            filteredAds.AddRange( allAds.Where(x => x.requireInstalledObject && x.installedObject == null || !HasContentForAd(x, allCollections)).ToList());

            filteredCollections = filteredCollections.OrderByDescending(x => x.GetType().Name).ThenBy(x => x.packName).ToList();

            // create draggable presets
            // TODO: adjust filter; allow all in one tab
            // TODO: group list by pack

            filteredPresets = new List<PresetItem>();
            foreach (ContentCollection contentCollection in filteredCollections)
            {

                // author filter
                if (!IsInFilter(contentCollection))
                    continue;

                ContentData[] contentData = contentCollection.contents;

                if (contentData != null)
                {
                    for (int i = 0; i < contentData.Length; i++)
                    {
                        filteredPresets.Add(new PresetItem(contentCollection, contentData[i], i));
                    }
                }

            }

            // packages
            filteredCollectionPackages = new List<Package>();
            foreach (ContentCollection c in filteredCollections)
            {
                Package package = new Package(c.id, c.packName, c.contentType, PackageType.Collection);

                if (!filteredCollectionPackages.Contains(package))
                {
                    filteredCollectionPackages.Add(package);
                }

            }

            filteredAdPackages = new List<Package>();
            foreach (ContentAd c in filteredAds)
            {
                Package package = new Package(c.id, c.packName, c.contentType, PackageType.Ad);

                if (!filteredAdPackages.Contains(package))
                {
                    filteredAdPackages.Add(package);
                }
            }
        }

        void DrawMissingModule(string name, string url)
        {
            EditorGUILayout.HelpBox("You do not have the " + name + " module installed, which is required for content on this tab", MessageType.Error);
            if (GUILayout.Button("Get " + name, GUILayout.Width(200)))
            {
                Application.OpenURL(url);
            }
        }

        void DrawMissingModule()
        {
            switch (tab)
            {
                case Tab.Audio:
                    {
                        
#if !__MICROVERSE_AMBIANCE__
                        DrawMissingModule("Ambience", MicroVerseEditor.ambientUrl);
#endif
                        break;
                    }
                case Tab.Vegetation:
                    {
#if !__MICROVERSE_VEGETATION__
                        DrawMissingModule("Vegetation", MicroVerseEditor.vegUrl);
#endif
                        break;
                    }
                case Tab.Roads:
                    {
#if !__MICROVERSE_ROADS__
                        DrawMissingModule("Roads", MicroVerseEditor.roadUrl);
#endif

#if !__MICROVERSE_SPLINES__
                        DrawMissingModule("Spline", MicroVerseEditor.splineUrl);
#endif
                        break;
                    }
                case Tab.Caves:
                    {
#if !__MICROVERSE_ROADS__
                        DrawMissingModule("Roads", MicroVerseEditor.roadUrl);
#endif

#if !__MICROVERSE_SPLINES__
                        DrawMissingModule("Spline", MicroVerseEditor.splineUrl);
#endif
                        break;
                    }
            }
        }

        private void OnGUI()
        {
            Package oldPackage = null;
            var oldTab = tab;

            EditorGUILayout.BeginHorizontal();
            {
                // tab bar
                tab = (Tab)GUILayout.Toolbar((int)tab, tabNames);

                // optional button
                if (GUILayout.Button(COptionalVisibility, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    optionalVisible = !optionalVisible;
                    MicroVerseSettingsProvider.OptionalVisible = optionalVisible;
                }

                // description button
                if (GUILayout.Button(CDescriptionVisibility, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    descriptionVisible = !descriptionVisible;
                    MicroVerseSettingsProvider.DescriptionVisible = descriptionVisible;
                }

                // help button
                if (GUILayout.Button(CHelpVisibility, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    helpBoxVisible = !helpBoxVisible;
                    MicroVerseSettingsProvider.HelpVisible = helpBoxVisible;
                }
            }
            EditorGUILayout.EndHorizontal();

            // set filtered content list; this can be optimized by not calling it all the time in OnGUI()
            // must happen after the tab got selected
            // could be optimized to happen only on tab switch and on package change. but then we wouldn't auto-detect changes, so leaving it as it is for now
            UpdateFilteredContent( tab);

            if (tab != oldTab)
            {
                oldPackage = selectedTabItems.GetValueOrDefault(oldTab);

                // try to keep the same package group selected during tab switch
                foreach (Package package in filteredCollectionPackages)
                {
                    if (package.IsInGroup(oldPackage))
                    {
                        selectedPackage = package;
                        selectedTabItems[tab] = selectedPackage;
                        break;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical("box", GUILayout.Width(listWidth));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Group By", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                grouping = (Grouping) EditorGUILayout.EnumPopup( grouping, GUILayout.Width(listWidth-80));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Author", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                string[] options = GetFilterOptions();
                selectedAuthorFilterIndex = EditorGUILayout.Popup(selectedAuthorFilterIndex, options, GUILayout.Width(listWidth-80));
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Create New Collection"))
                {
                    BrowserToolsEditorWindow.CreateWindow();
                }
                selectedAuthorFilterText = selectedAuthorFilterIndex < options.Length ? options[selectedAuthorFilterIndex] : "<Undefined>";

                // nothing selected => pick first one if available
                if (selectedPackage == null && filteredCollectionPackages.Count > 0)
                {
                    selectedPackage = filteredCollectionPackages[0];
                    selectedTabItems[tab] = selectedPackage;

                }

                if (grouping == Grouping.Package)
                {
                    DrawPackageList();
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        // falloff toolbar
                        DrawTabToolbar();

                        // content drag/drop
                        contentDragHandler.OnGUI();
                        // package content
                        if (grouping == Grouping.ContentType)
                        {
                            selectedPackage = null;
                            contentSelectionGrid.Draw(filteredPresets, grouping);
                        }
                        else if (grouping == Grouping.Package)
                        {
                            if (selectedPackage != null)
                            {
                                if (selectedPackage.packageType == PackageType.Collection)
                                {
                                    List<PresetItem> draggablePresets = filteredPresets.Where(x => x.collection.packName == selectedPackage.packName).ToList();
                                    contentSelectionGrid.Draw(draggablePresets, grouping);
                                }
                                else if (selectedPackage.packageType == PackageType.Ad)
                                {
                                    ContentAd selectedAd = GetFirstSelectedAd();
                                    
                                    if (selectedAd != null && !HasContentForAd(selectedAd, allCollections))
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.BeginVertical();
                                        if (GUILayout.Button("Download", GUILayout.Width(420)))
                                        {
                                            var path = selectedAd.downloadPath;
                                            if (path.Contains("assetstore.unity.com"))
                                            {
                                                path += "?aid=25047";
                                            }
                                            Application.OpenURL(path);
                                        }
                                        Rect r = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(420), GUILayout.MaxHeight(280));
                                        if (GUI.Button(r, ""))
                                        {
                                            var path = selectedAd.downloadPath;
                                            if (path.Contains("assetstore.unity.com"))
                                            {
                                                path += "?aid=25047";
                                            }
                                            Application.OpenURL(path);
                                        }
                                        if (selectedAd.image == null)
                                        {
                                            GUI.DrawTexture(r, Texture2D.whiteTexture);
                                        }
                                        else
                                        {
                                            GUI.DrawTexture(r, selectedAd.image);
                                        }
                                        EditorGUILayout.EndVertical();
                                        
                                        if (!string.IsNullOrEmpty(selectedAd.description))
                                        {
                                            GUIStyle style = new GUIStyle(EditorStyles.label);
                                            style.wordWrap = true;
                                            EditorGUILayout.LabelField(selectedAd.description, style);
                                        }
                                        EditorGUILayout.EndHorizontal();

                                    }
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw all packages. First Installed, then Optional
        /// </summary>
        private void DrawPackageList()
        {
            listScrollPosition = GUILayout.BeginScrollView(listScrollPosition, GUILayout.Width(listWidth+20));
            {
                bool hasCollections = filteredCollectionPackages.Count > 0;
                bool hasAds = filteredAdPackages.Count > 0;

                if(hasCollections)
                {
                    EditorGUILayout.LabelField("Installed", EditorStyles.miniBoldLabel, GUILayout.Width(listWidth));
                    DrawPackageListItems(filteredCollectionPackages);
                }

                if (optionalVisible && hasAds)
                {
                    if (hasCollections)
                    {
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.LabelField("Optional", EditorStyles.miniBoldLabel, GUILayout.Width(listWidth));
                    DrawPackageListItems(filteredAdPackages);
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Draw the individual collections, eg Installed and Optional
        /// </summary>
        /// <param name="packages"></param>
        private void DrawPackageListItems( List<Package> packages)
        {
            foreach (Package package in packages)
            {
                Color prevColor = GUI.backgroundColor;
                {
                    selectedPackage = selectedTabItems.GetValueOrDefault(tab);

                    if (package.IsInGroup( selectedPackage))
                    {
                        GUI.backgroundColor = selectionColor;
                    }

                    if (GUILayout.Button(package.packName, GUILayout.Width(headerWidth+10)))
                    {
                        selectedPackage = package;
                        selectedTabItems[tab] = selectedPackage;
                    }
                }
                GUI.backgroundColor = prevColor;
            }
        }

        public bool IsDescriptionVisible()
        {
            return descriptionVisible;
        }

        public bool IsHelpBoxVisible()
        {
            return helpBoxVisible;
        }

        public int GetListWidth()
        {
            return listWidth;
        }
    }
}
