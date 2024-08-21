using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static JBooth.MicroVerseCore.SelectionGrid;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(TreeStamp))]
    public class TreeStampEditor : Editor
    {
        static GUIContent CWeightRange = new GUIContent("Weight Range", "Range of weights in which details will appear");
        static GUIContent CScaleHeightRange = new GUIContent("Scale Height Range", "Smallest to largest height of the object");
        static GUIContent CScaleWidthRange = new GUIContent("Scale Width Range", "Smallest to largest width of the object");
        static GUIContent CLockScaleWidth = new GUIContent("Lock Scale & Width", "Height and width will scale proportionally");
        static GUIContent CScaleMultiplierAtBoundaries = new GUIContent("Size Multiplier on Boundaries", "As weight of an area is reduced, you can increase or decrease the scale of objects");
        static GUIContent CDensityByWeight = new GUIContent("Density by weight", "Areas with less weight will spawn less objects");
        static GUIContent CRandomRotation = new GUIContent("Random Rotation", "Randomize rotations of spawned objects");
        static GUIContent CSink = new GUIContent("Sink", "Pushes the object down into the terrain");
        static GUIContent CWeight = new GUIContent("Weight", "Increases the chance that this tree gets picked over others based on their weighting. Note this is not a percent where 0 is no chance; if 2 tree's are in the system, one with a weight of 0 and the other 100, then there is a 1 in 101 chance that the tree with 0 weights is placed");
        static GUIContent CApplyToAll = new GUIContent("Apply to All", "Apply the settings of the current tree instance to all the others in the tree stamp");
        static GUIContent CMapHeightFilterToScale = new GUIContent("Map Height Filter to Scale", "When true, the result of the height filter will scale tree's between min and max size. This can be used to make tree's fade at certain height ranges");
        static GUIContent CMapWeightToScale = new GUIContent("Map Weight to Scale", "Scale the tree between min and max based on the 0-1 weight of the filters");
        static GUIContent CRandomizeScale = new GUIContent("Randomize Scale", "Randomize Scale of tree's between scale min/max");
        Selectable[] selectableTrees;
        List<int> selectedTreeIndexes = new List<int>();
        int selectedTreeInstance = -1;

        void LoadTreeIcons(List<TreePrototypeSerializable> trees)
        {
            if (trees == null || trees.Count == 0)
            {
                selectableTrees = new Selectable[0];
                //treeIcons[0] = new GUIContent("No Trees");
            }
            else
            {
                // Locate the proto types asset preview textures
                selectableTrees = new Selectable[trees.Count];
                for (int i = 0; i < selectableTrees.Length; i++)
                {
                    selectableTrees[i] = new Selectable();

                    Texture tex = AssetPreview.GetAssetPreview(trees[i].prefab);
                    selectableTrees[i].image = tex != null ? tex : null;
                    selectableTrees[i].text = selectableTrees[i].tooltip = trees[i].prefab != null ? trees[i].prefab.name : "Missing";
                    selectableTrees[i].active = true;
                }

                // select focused instance for multi-selection in selection grid
                if (selectedTreeInstance >= 0 && selectedTreeInstance < selectableTrees.Length)
                    selectableTrees[selectedTreeInstance].focused = true;
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (TreeStamp)target;
            if (stamp.filterSet.falloffFilter.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.filterSet.falloffFilter.paintMask, stamp.GetBounds(), stamp.transform);
            }
        }

        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            serializedObject.Update();
            using var changeScope = new EditorGUI.ChangeCheckScope();

            TreeStamp tp = (TreeStamp)target;

            if (tp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the heriarchy, will have no effect", MessageType.Warning);
            }

            if (tp.poissonDisk == null)
            {
                tp.poissonDisk = GUIUtil.FindDefaultTexture("microverse_default_poissondisk");
                EditorUtility.SetDirty(tp);
            }
            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                EditorGUILayout.LabelField("Placement Settings:");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("poissonDisk"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("poissonDiskStrength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("density"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeOthers"));
                serializedObject.ApplyModifiedProperties();
                if (tp.occludeOthers == false)
                {
                    EditorGUILayout.HelpBox("Tree will not generate distance fields with occlude others disabled", MessageType.Info);
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("occludedByOthers"));
                GUIUtil.DoSDFFilter(serializedObject);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("heightModAmount"));
                serializedObject.ApplyModifiedProperties();
                if (tp.heightModAmount != 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("heightModWidth"));
                }
                GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("layer"), tp.GetBounds(), "Texture Mod Layer");
                serializedObject.ApplyModifiedProperties();
                if (tp.layer != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layerWeight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layerWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("applyFilteringToTextureMod"));
                }

            }


            var otherTextureWeight = serializedObject.FindProperty("filterSet").FindPropertyRelative("otherTextureWeight");
            var filters = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilters");
            var filtersEnabled = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilterEnabled");
            GUIUtil.DrawFilterSet(tp, tp.filterSet, otherTextureWeight, filters, filtersEnabled, tp.transform, true);

            LoadTreeIcons(tp.prototypes);

            EditorGUILayout.LabelField("Tree variations to place");

            // drop area
            Rect prefabDropArea = GUILayoutUtility.GetRect(0.0f, 34.0f, GUIUtil.DropAreaStyle, GUILayout.ExpandWidth(true));

            Color prevColor = GUI.backgroundColor;
            GUI.color = GUIUtil.DropAreaBackgroundColor;
            GUI.Box(prefabDropArea, "Drop Trees Here", GUIUtil.DropAreaStyle);
            GUI.color = prevColor;

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (prefabDropArea.Contains(Event.current.mousePosition))
                    {

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (Event.current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            foreach (Object droppedObject in DragAndDrop.objectReferences)
                            {
                                // Debug.Log($"Dropped {droppedObject}");

                                if (!(droppedObject is GameObject))
                                {
                                    Debug.LogError("Not a gameobject: " + droppedObject);
                                    continue;
                                }

                                TreeManager treeManager = new TreeManager(tp, -1, tp.prototypes, tp.randomizations);
                                treeManager.SetTree(droppedObject as GameObject);
                                treeManager.DoApply();
                            }

                            EditorUtility.SetDirty(tp);

                        }
                    }
                    break;
            }

            // get current active state
            for (int i = 0; i < selectableTrees.Length; i++)
            {
                selectableTrees[i].active = !tp.randomizations[i].disabled;
            }

            // grid selection
            bool changed = SelectionGrid.ShowSelectionGrid(selectedTreeIndexes, selectableTrees, 128);

            // set the attributes of the objects in case anything changed on the selection grid
            if( changed)
            {
                for (int i = 0; i < selectableTrees.Length; i++)
                {
                    var randoms = tp.randomizations[i];

                    randoms.disabled = !selectableTrees[i].active;

                    tp.randomizations[i] = randoms;
                }

                EditorUtility.SetDirty(tp);
            }

            bool multiObjectEditMode = selectedTreeIndexes.Count > 1;

            GUILayout.BeginHorizontal();

            selectedTreeInstance = selectedTreeIndexes.Count > 0 ? selectedTreeIndexes[0] : -1;

            if (GUILayout.Button("Add"))
            {
                TreeManager treeManager = new TreeManager(tp, -1, tp.prototypes, tp.randomizations);
                TreeWizard.CreateWindow(treeManager, "Add");
                EditorUtility.SetDirty(tp);
            }

            GUI.enabled = selectedTreeIndexes.Count == 1;
            {
                if (tp.prototypes.Count == 0)
                {
                    GUI.enabled = false;
                }

                if (GUILayout.Button("Edit"))
                {
                    TreeManager treeManager = new TreeManager(tp, selectedTreeInstance, tp.prototypes, tp.randomizations);
                    TreeWizard.CreateWindow(treeManager, "Edit");
                    EditorUtility.SetDirty(tp);
                }
                if (tp.prototypes.Count == 0 || tp.prototypes.Count < selectedTreeInstance)
                {
                    GUI.enabled = false;
                }
            }

            GUI.enabled = true;

            if (GUILayout.Button("Remove"))
            {
                // get top index, we need to select any cell after all selected were removed
                int topIndex = selectedTreeIndexes.Count > 0 ? selectedTreeIndexes[0] : -1;

                // iterate backwards for multi-delete
                selectedTreeIndexes.Reverse();

                // remove cells
                foreach (int index in selectedTreeIndexes)
                {
                    if (index >= 0 && tp.prototypes.Count > index)
                    {
                        tp.prototypes.RemoveAt(index);
                        tp.randomizations.RemoveAt(index);
                    }
                }

                EditorUtility.SetDirty(tp);

                // pre-select cell: either previous one of the selected cell or the first one
                int newSelectedIndex = topIndex >= 0 ? topIndex - 1 : -1;
                if(newSelectedIndex < 0 && selectedTreeIndexes.Count > 0)
                {
                    newSelectedIndex = 0;
                }

                selectedTreeIndexes.Clear();
                selectedTreeIndexes.Add(newSelectedIndex);

            }


            if (GUILayout.Button("Clear"))
            {
                tp.prototypes.Clear();
                tp.randomizations.Clear();
                EditorUtility.SetDirty(tp);
            }
            
            
            GUILayout.EndHorizontal();

            if (tp.prototypes.Count == 0)
            {
                EditorGUILayout.HelpBox("Please add one or more tree's to begin", MessageType.Info);
            }
            else
            {
                using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
                {
                    EditorGUILayout.LabelField("Randomization:");

                    if (tp.prototypes.Count > 0 && tp.randomizations.Count == tp.prototypes.Count && selectedTreeInstance >= 0 && selectedTreeInstance < tp.randomizations.Count)
                    {
                        // prototype
                        EditorGUI.BeginChangeCheck();

                        if (multiObjectEditMode)
                        {
                            EditorGUILayout.LabelField("Prefab", "<Multiple Objects>");
                        }
                        else
                        {
                            Object prefab = EditorGUILayout.ObjectField("Prefab", tp.prototypes[selectedTreeInstance].prefab, typeof(GameObject), true);

                            if (EditorGUI.EndChangeCheck())
                            {
                                TreeManager treeManager = new TreeManager(tp, selectedTreeInstance, tp.prototypes, tp.randomizations);
                                treeManager.SetTree(prefab as GameObject);
                                treeManager.DoApply();

                            }

                        }

                        // details

                        EditorGUI.BeginChangeCheck();
                        var randoms = tp.randomizations[selectedTreeInstance];
                        float weight = EditorGUILayout.Slider(CWeight, randoms.weight, 0, 100);
                        Vector2 weightRange = EditorGUILayout.Vector2Field(CWeightRange, randoms.weightRange);
                        Vector2 scaleHeightRange = EditorGUILayout.Vector2Field(CScaleHeightRange, randoms.scaleHeightRange);
                        Vector2 scaleWidthRange = EditorGUILayout.Vector2Field(CScaleWidthRange, randoms.scaleWidthRange);
                        bool lockScaleWidthHeight = EditorGUILayout.Toggle(CLockScaleWidth, randoms.lockScaleWidthHeight);
                        bool randomizeScale = EditorGUILayout.Toggle(CRandomizeScale, randoms.randomScale);
                        bool mapHeightFilterToScale = EditorGUILayout.Toggle(CMapHeightFilterToScale, randoms.mapHeightFilterToScale);
                        bool mapWeightToScale = EditorGUILayout.Toggle(CMapWeightToScale, randoms.mapWeightToScale);
                        float scaleAtBoundaries = EditorGUILayout.Slider(CScaleMultiplierAtBoundaries, randoms.scaleMultiplierAtBoundaries, 0.2f, 4.0f);
                        bool densityByWeight = EditorGUILayout.Toggle(CDensityByWeight, randoms.densityByWeight);
                        bool randomRotation = EditorGUILayout.Toggle(CRandomRotation, randoms.randomRotation);

                        float sink = 0;// EditorGUILayout.FloatField(CSink, randoms.sink);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(tp, "Tree Parameter Change");
                            foreach (var targetIndex in selectedTreeIndexes)
                            {
                                var target = tp.randomizations[targetIndex];

                                if (weight != randoms.weight) { target.weight = weight; }
                                if (weightRange != randoms.weightRange) { target.weightRange = weightRange; }
                                if (target.scaleHeightRange != scaleHeightRange) target.scaleHeightRange = scaleHeightRange;
                                if (target.scaleWidthRange != scaleWidthRange) target.scaleWidthRange = scaleWidthRange;
                                if (target.lockScaleWidthHeight != lockScaleWidthHeight) target.lockScaleWidthHeight = lockScaleWidthHeight;
                                if (target.scaleMultiplierAtBoundaries != scaleAtBoundaries) target.scaleMultiplierAtBoundaries = scaleAtBoundaries;
                                if (target.densityByWeight != densityByWeight) target.densityByWeight = densityByWeight;
                                if (target.randomRotation != randomRotation) target.randomRotation = randomRotation;
                                if (target.sink != sink) target.sink = sink;
                                if (target.mapWeightToScale != mapWeightToScale) target.mapWeightToScale = mapWeightToScale;
                                if (target.mapHeightFilterToScale != mapHeightFilterToScale) target.mapHeightFilterToScale = mapHeightFilterToScale;
                                if (target.randomScale != randomizeScale) target.randomScale = randomizeScale;
                                tp.randomizations[targetIndex] = target;
                            }


                            EditorUtility.SetDirty(tp);
                        }

                    }
                }

                // mini toolbar: apply settings to all
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(CApplyToAll, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        if (selectedTreeInstance >= 0 && selectedTreeInstance < tp.randomizations.Count)
                        {

                            Undo.RegisterCompleteObjectUndo(tp, "Apply Tree Parameters to All");

                            List<int> allIndexes = new List<int>();

                            for (int i = 0; i < tp.randomizations.Count; i++)
                            {
                                allIndexes.Add(i);
                            }

                            ApplySettings(selectedTreeInstance, allIndexes);
                        }
                    }

                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();

            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(tp.GetBounds(), MicroVerse.InvalidateType.All); // because tree's can now mod height/splats
            }

        }

        /// <summary>
        /// Apply the settings of the object at a source index to all at the given target indexes
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndexes"></param>
        private void ApplySettings( int sourceIndex, List<int> targetIndexes)
        {

            TreeStamp tp = (TreeStamp)target;

            var source = tp.randomizations[sourceIndex];

            foreach ( int targetIndex in targetIndexes)
            {
                if (sourceIndex == targetIndex)
                    continue;

                var target = tp.randomizations[targetIndex];

                // target.disabled = source.disabled; // don't use the active flag anymore, it's depending on the selection
                target.weight = source.weight;
                target.scaleHeightRange = source.scaleHeightRange;
                target.scaleWidthRange = source.scaleWidthRange;
                target.lockScaleWidthHeight = source.lockScaleWidthHeight;
                target.scaleMultiplierAtBoundaries = source.scaleMultiplierAtBoundaries;
                target.densityByWeight = source.densityByWeight;
                target.randomRotation = source.randomRotation;
                target.sink = source.sink;
                target.mapHeightFilterToScale = source.mapHeightFilterToScale;
                target.mapWeightToScale = source.mapWeightToScale;
                target.randomScale = source.randomScale;
                tp.randomizations[targetIndex] = target;
            }
        }

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            SceneView.duringSceneGui += OnSceneRepaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            SceneView.duringSceneGui -= OnSceneRepaint;
        }

        static Texture2D overlayTex;
        private void OnSceneRepaint(SceneView sceneView)
        {
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTex == null)
                {
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_tree");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as TreeStamp);
                if (hs == null) return;
                Color color = Color.green;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.treeStampColor;
                }
                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.filterSet.falloffFilter, color, overlayTex);
            }
        }

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var treePlacement = (TreeStamp)target;
                if (treePlacement != null && treePlacement.transform.hasChanged)
                {
                    treePlacement.transform.hasChanged = false;
                    var r = treePlacement.transform.localRotation.eulerAngles;
                    r.z = 0;
                    treePlacement.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(treePlacement.GetBounds(), MicroVerse.InvalidateType.All);
                }
            }
        }
    }
}
