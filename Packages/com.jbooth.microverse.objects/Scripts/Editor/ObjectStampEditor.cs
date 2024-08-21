using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static JBooth.MicroVerseCore.SelectionGrid;

namespace JBooth.MicroVerseCore
{

    [CustomEditor(typeof(ObjectStamp))]
    public class ObjectStampEditor : Editor
    {
        static GUIContent CScaleMultiplierAtBoundaries = new GUIContent("Size Multiplier on Boundaries", "As weight of an area is reduced, you can increase or decrease the scale of objects");
        static GUIContent CDensityByWeight = new GUIContent("Density by weight", "Areas with less weight will spawn less objects");
        static GUIContent CSink = new GUIContent("Sink", "Pushes the object down into the terrain");
        static GUIContent CWeight = new GUIContent("Weight", "Increases the chance that this object gets picked over others based on their weighting. Note this is not a percent where 0 is no chance; if 2 tree's are in the system, one with a weight of 0 and the other 100, then there is a 1 in 101 chance that the tree with 0 weights is placed");
        static GUIContent CWeightRange = new GUIContent("Weight Range", "Range of weight values that will allow objects to be spawned");
        static GUIContent CApplyToAll = new GUIContent("Apply to All", "Apply the settings of the current object instance to all the others in the object stamp");
        static GUIContent CAlignDownhill = new GUIContent("Align Downhill", "Aligns rotation of the object to point downhill");
        Selectable[] selectableObjects;
        List<int> selectedObjectIndexes = new List<int>();
        int selectedObjectInstance = -1;

        void LoadObjectIcons(List<GameObject> objs)
        {
            if (objs == null || objs.Count == 0)
            {
                selectableObjects = new Selectable[0];
            }
            else
            {
                // Locate the proto types asset preview textures
                selectableObjects = new Selectable[objs.Count];
                for (int i = 0; i < objs.Count; i++)
                {
                    selectableObjects[i] = new Selectable();

                    Texture tex = AssetPreview.GetAssetPreview(objs[i]);

                    selectableObjects[i].image = tex != null ? tex : null;
                    selectableObjects[i].text = selectableObjects[i].tooltip = objs[i] != null ? objs[i].name : "Missing";
                    selectableObjects[i].active = true;

                }

                // select focused instance for multi-selection in selection grid
                if (selectedObjectInstance >= 0 && selectedObjectInstance < selectableObjects.Length)
                    selectableObjects[selectedObjectInstance].focused = true;
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (ObjectStamp)target;
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

            ObjectStamp os = (ObjectStamp)target;

            if (os.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the heriarchy, will have no effect", MessageType.Warning);
            }

            if (os.poissonDisk == null)
            {
                os.poissonDisk = GUIUtil.FindDefaultTexture("microverse_default_poissondisk");
                EditorUtility.SetDirty(os);
            }
            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                EditorGUILayout.LabelField("Placement Settings:");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hideInHierarchy"));

                EditorGUILayout.BeginHorizontal();
                {
                    SerializedProperty parentObjectProperty = serializedObject.FindProperty("parentObject");

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(parentObjectProperty);

                    if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        // get object stamp for the name creation
                        ObjectStamp editorTarget = (ObjectStamp)target;

                        // create new container
                        GameObject newContainer = new GameObject();
                        newContainer.name = editorTarget.name + " (Container)";

                        // ensure name uniqueness
                        GameObjectUtility.EnsureUniqueNameForSibling(newContainer);

                        // set as new value
                        parentObjectProperty.objectReferenceValue = newContainer;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnAsPrefab"));
                if (EditorGUI.EndChangeCheck())
                {
                    os.destroyOnNextClear = true;
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("poissonDisk"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("poissonDiskStrength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("density"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeOthers"));
                serializedObject.ApplyModifiedProperties();
                if (os.occludeOthers)
                {
                    EditorGUILayout.HelpBox("Stamp objects will not be included in signed distance field generation", MessageType.Info);
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("occludedByOthers"));
                GUIUtil.DoSDFFilter(serializedObject);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("heightModAmount"));
                serializedObject.ApplyModifiedProperties();
                if (os.heightModAmount != 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("heightModWidth"));
                }
                GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("layer"), os.GetBounds(), "Texture Mod Layer");
                serializedObject.ApplyModifiedProperties();
                if (os.layer != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layerWeight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("layerWidth"));
                }
                
            }


            var otherTextureWeight = serializedObject.FindProperty("filterSet").FindPropertyRelative("otherTextureWeight");
            var filters = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilters");
            var filtersEnabled = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilterEnabled");
            GUIUtil.DrawFilterSet(os, os.filterSet, otherTextureWeight, filters, filtersEnabled, os.transform, true);
            
            LoadObjectIcons(os.prototypes);

            EditorGUILayout.LabelField("Object variations to place");


            // drop area
            Rect prefabDropArea = GUILayoutUtility.GetRect(0.0f, 34.0f, GUIUtil.DropAreaStyle, GUILayout.ExpandWidth(true));

            Color prevColor = GUI.backgroundColor;
            GUI.color = GUIUtil.DropAreaBackgroundColor;
            GUI.Box(prefabDropArea, "Drop Prefabs Here", GUIUtil.DropAreaStyle);
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

                                ObjectManager objectManager = new ObjectManager(os, -1, os.prototypes, os.randomizations);
                                objectManager.SetObject(droppedObject as GameObject);
                                objectManager.DoApply();
                            }

                            EditorUtility.SetDirty(os);

                        }
                    }
                    break;
            }

            // get current active state
            for (int i = 0; i < selectableObjects.Length; i++)
            {
                selectableObjects[i].active = !os.randomizations[i].disabled;
            }

            // grid selection
            bool changed = SelectionGrid.ShowSelectionGrid(selectedObjectIndexes, selectableObjects, 128);

            // set the attributes of the objects in case anything changed on the selection grid
            if (changed)
            {
                for (int i = 0; i < selectableObjects.Length; i++)
                {
                    var randoms = os.randomizations[i];

                    randoms.disabled = !selectableObjects[i].active;

                    os.randomizations[i] = randoms;
                }

                EditorUtility.SetDirty(os);
            }

            bool multiObjectEditMode = selectedObjectIndexes.Count > 1;

            
            GUILayout.BeginHorizontal();

            selectedObjectInstance = selectedObjectIndexes.Count > 0 ? selectedObjectIndexes[0] : -1;

            if (GUILayout.Button("Remove"))
            {
                // get top index, we need to select any cell after all selected were removed
                int topIndex = selectedObjectIndexes.Count > 0 ? selectedObjectIndexes[0] : -1;

                // iterate backwards for multi-delete
                selectedObjectIndexes.Reverse();

                // remove cells
                foreach (int index in selectedObjectIndexes)
                {
                    if (index >= 0 && os.prototypes.Count > index)
                    {
                        os.prototypes.RemoveAt(index);
                        os.randomizations.RemoveAt(index);
                    }
                }

                EditorUtility.SetDirty(os);

                // pre-select cell: either previous one of the selected cell or the first one
                int newSelectedIndex = topIndex >= 0 ? topIndex - 1 : -1;
                if (newSelectedIndex < 0 && selectedObjectIndexes.Count > 0)
                {
                    newSelectedIndex = 0;
                }

                selectedObjectIndexes.Clear();
                selectedObjectIndexes.Add(newSelectedIndex);
            }

            GUI.enabled = true;

            if (GUILayout.Button("Clear"))
            {
                os.prototypes.Clear();
                os.randomizations.Clear();
                EditorUtility.SetDirty(os);
            }
            
            GUILayout.EndHorizontal();

            if (os.prototypes.Count == 0)
            {
                EditorGUILayout.HelpBox("Please add one or more objects to begin", MessageType.Info);
            }
            else
            {
                using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
                {
                    EditorGUILayout.LabelField("Randomization:");
                    if (os.prototypes.Count > 0 && os.randomizations.Count == os.prototypes.Count && selectedObjectInstance >= 0 && selectedObjectInstance < os.randomizations.Count)
                    {
                        // prototype
                        EditorGUI.BeginChangeCheck();

                        if (multiObjectEditMode)
                        {
                            EditorGUILayout.LabelField("Prefab", "<Multiple Objects>");
                        }
                        else
                        {
                            Object prefab = EditorGUILayout.ObjectField("Prefab", os.prototypes[selectedObjectInstance], typeof(GameObject), true);

                            if (EditorGUI.EndChangeCheck())
                            {
                                ObjectManager objectManager = new ObjectManager(os, selectedObjectInstance, os.prototypes, os.randomizations);
                                objectManager.SetObject(prefab as GameObject);
                                objectManager.DoApply();
                            }

                        }

                        // details

                        EditorGUI.BeginChangeCheck();

                        var randoms = os.randomizations[selectedObjectInstance];
                        float weight = EditorGUILayout.Slider(CWeight, randoms.weight, 0, 100);
                        Vector2 weightRange = EditorGUILayout.Vector2Field(CWeightRange, randoms.weightRange);


                        var rotLock = randoms.rotationLock;

                        GUILayout.BeginHorizontal();
                        {
                            rotLock = (ObjectStamp.Lock)EditorGUILayout.EnumPopup("Rotation Lock", randoms.rotationLock);
   
                            
                            // right align the quick buttons
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("X", EditorStyles.miniButton))
                            {
                                randoms.rotationRangeX = new Vector2(-180, 180);
                                randoms.rotationRangeY = Vector2.zero;
                                randoms.rotationRangeZ = Vector2.zero;
                            }
                            if (GUILayout.Button("Y", EditorStyles.miniButton))
                            {
                                randoms.rotationRangeX = Vector2.zero;
                                randoms.rotationRangeY = new Vector2(-180, 180);
                                randoms.rotationRangeZ = Vector2.zero;
                            }
                            if (GUILayout.Button("Z", EditorStyles.miniButton))
                            {
                                randoms.rotationRangeX = Vector2.zero;
                                randoms.rotationRangeY = Vector2.zero;
                                randoms.rotationRangeZ = new Vector2(-180, 180);
                            }
                            if (GUILayout.Button("XYZ", EditorStyles.miniButton))
                            {
                                randoms.rotationRangeX = new Vector2(-180, 180);
                                randoms.rotationRangeY = new Vector2(-180, 180);
                                randoms.rotationRangeZ = new Vector2(-180, 180);
                            }
                            

                        }
                        GUILayout.EndHorizontal();

                        var rotX = randoms.rotationRangeX;
                        var rotY = randoms.rotationRangeY;
                        var rotZ = randoms.rotationRangeZ;
                        switch (rotLock)
                        {
                            case ObjectStamp.Lock.None:
                                GUIUtil.DrawMinMax(new GUIContent("Rotation X"), ref rotX.x, ref rotX.y, -180, 180);
                                GUIUtil.DrawMinMax(new GUIContent("Rotation Y"), ref rotY.x, ref rotY.y, -180, 180);
                                GUIUtil.DrawMinMax(new GUIContent("Rotation Z"), ref rotZ.x, ref rotZ.y, -180, 180);
                                break;
                            case ObjectStamp.Lock.XY:
                                GUIUtil.DrawMinMax(new GUIContent("Rotation XY"), ref rotX.x, ref rotX.y, -180, 180);
                                GUIUtil.DrawMinMax(new GUIContent("Rotation Z"), ref rotZ.x, ref rotZ.y, -180, 180);
                                rotY = rotX;
                                break;
                            case ObjectStamp.Lock.XZ:
                                GUIUtil.DrawMinMax(new GUIContent("Rotation XZ"), ref rotX.x, ref rotX.y, -180, 180);
                                GUIUtil.DrawMinMax(new GUIContent("Rotation Y"), ref rotY.x, ref rotY.y, -180, 180);
                                rotZ = rotX;
                                break;
                            case ObjectStamp.Lock.YZ:
                                GUIUtil.DrawMinMax(new GUIContent("Rotation X"), ref rotX.x, ref rotX.y, -180, 180);
                                GUIUtil.DrawMinMax(new GUIContent("Rotation YZ"), ref rotY.x, ref rotY.y, -180, 180);
                                rotZ = rotY;
                                break;
                            case ObjectStamp.Lock.XYZ:
                                GUIUtil.DrawMinMax(new GUIContent("Rotation XYZ"), ref rotX.x, ref rotX.y, -180, 180);
                                rotY = rotX;
                                rotZ = rotX;
                                break;
                        }

                        var slopeAlignment = EditorGUILayout.Slider("Slope Alignment", randoms.slopeAlignment, 0, 1);
                        var alignDownhill = EditorGUILayout.Toggle(CAlignDownhill, randoms.alignDownhill);

                        // scale
                        var scaleLock = (ObjectStamp.Lock)EditorGUILayout.EnumPopup("Scale Lock", randoms.scaleLock);

                        var scaleX = randoms.scaleRangeX;
                        var scaleY = randoms.scaleRangeY;
                        var scaleZ = randoms.scaleRangeZ;

                        switch (scaleLock)
                        {
                            case ObjectStamp.Lock.None:
                                scaleX = GUIUtil.ScaleRange("Scale X", ref randoms.scaleRangeX);
                                scaleY = GUIUtil.ScaleRange("Scale Y", ref randoms.scaleRangeY);
                                scaleZ = GUIUtil.ScaleRange("Scale Z", ref randoms.scaleRangeZ);
                                break;
                            case ObjectStamp.Lock.XY:
                                scaleX = GUIUtil.ScaleRange("Scale XY", ref randoms.scaleRangeX);
                                scaleY = scaleX;
                                scaleZ = GUIUtil.ScaleRange("Scale Z", ref randoms.scaleRangeZ);
                                break;
                            case ObjectStamp.Lock.XZ:
                                scaleX = GUIUtil.ScaleRange("Scale XZ", ref randoms.scaleRangeX);
                                scaleY = GUIUtil.ScaleRange("Scale Y", ref randoms.scaleRangeY);
                                scaleZ = scaleX;
                                break;
                            case ObjectStamp.Lock.YZ:
                                scaleX = GUIUtil.ScaleRange("Scale X", ref randoms.scaleRangeX);
                                scaleY = GUIUtil.ScaleRange("Scale YZ", ref randoms.scaleRangeY);
                                scaleZ = scaleY;
                                break;
                            case ObjectStamp.Lock.XYZ:
                                scaleX = GUIUtil.ScaleRange("Scale X", ref randoms.scaleRangeX);
                                scaleY = scaleX;
                                scaleZ = scaleX;
                                break;
                        }

                        float scaleAtBoundaries = EditorGUILayout.Slider(CScaleMultiplierAtBoundaries, randoms.scaleMultiplierAtBoundaries, 0.2f, 4.0f);
                        bool densityByWeight = EditorGUILayout.Toggle(CDensityByWeight, randoms.densityByWeight);
                        Vector2 sink = EditorGUILayout.Vector2Field(CSink, randoms.sink);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RegisterCompleteObjectUndo(os, "Object Parameter Change");

                            foreach (var targetIndex in selectedObjectIndexes)
                            {
                                var target = os.randomizations[targetIndex];

                                if (weight != randoms.weight) { target.weight = weight; }
                                if (weightRange != randoms.weightRange) { target.weightRange = weightRange; }

                                if (target.scaleRangeX != scaleX) target.scaleRangeX = scaleX;
                                if (target.scaleRangeY != scaleY) target.scaleRangeY = scaleY;
                                if (target.scaleRangeZ != scaleZ) target.scaleRangeZ = scaleZ;

                                if (target.rotationRangeX != rotX) target.rotationRangeX = rotX;
                                if (target.rotationRangeY != rotY) target.rotationRangeY = rotY;
                                if (target.rotationRangeZ != rotZ) target.rotationRangeZ = rotZ;

                                if (target.scaleLock != scaleLock) target.scaleLock = scaleLock;
                                if (target.rotationLock != rotLock) target.rotationLock = rotLock;
                                if (target.alignDownhill != alignDownhill) target.alignDownhill = alignDownhill;

                                if (target.slopeAlignment != slopeAlignment) target.slopeAlignment = slopeAlignment;
                                if (target.sink != sink) target.sink = sink;
                                if (target.scaleMultiplierAtBoundaries != scaleAtBoundaries) target.scaleMultiplierAtBoundaries = scaleAtBoundaries;
                                if (target.densityByWeight != densityByWeight) target.densityByWeight = densityByWeight;

                                os.randomizations[targetIndex] = target;
                            }

                            EditorUtility.SetDirty(os);
                        }

                    }
                }

                // mini toolbar: apply settings to all
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(CApplyToAll, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        if (selectedObjectInstance >= 0 && selectedObjectInstance < os.randomizations.Count)
                        {

                            Undo.RegisterCompleteObjectUndo(os, "Apply Object Parameters to All");

                            List<int> allIndexes = new List<int>();

                            for (int i = 0; i < os.randomizations.Count; i++)
                            {
                                allIndexes.Add(i);
                            }

                            ApplySettings(selectedObjectInstance, allIndexes);

                        }
                    }

                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
            
            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(os.GetBounds(), MicroVerse.InvalidateType.All);
            }

        }

        /// <summary>
        /// Apply the settings of the object at a source index to all at the given target indexes
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndexes"></param>
        private void ApplySettings(int sourceIndex, List<int> targetIndexes)
        {
            ObjectStamp os = (ObjectStamp)target;

            var source = os.randomizations[sourceIndex];

            foreach (int targetIndex in targetIndexes)
            {
                if (sourceIndex == targetIndex)
                    continue;

                var target = os.randomizations[targetIndex];

                // target.disabled = source.disabled; // don't use the active flag anymore, it's depending on the selection
                target.weight = source.weight;
                target.weightRange = source.weightRange;

                target.scaleRangeX = source.scaleRangeX;
                target.scaleRangeY = source.scaleRangeY;
                target.scaleRangeZ = source.scaleRangeZ;

                target.rotationRangeX = source.rotationRangeX;
                target.rotationRangeY = source.rotationRangeY;
                target.rotationRangeZ = source.rotationRangeZ;

                target.scaleLock = source.scaleLock;
                target.rotationLock = source.rotationLock;

                target.slopeAlignment = source.slopeAlignment;
                target.sink = source.sink;
                target.scaleMultiplierAtBoundaries = source.scaleMultiplierAtBoundaries;
                target.densityByWeight = source.densityByWeight;

                os.randomizations[targetIndex] = target;
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
                var hs = (target as ObjectStamp);
                if (hs == null) return;
                Color color = Color.green;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.objectStampColor;
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
                var objectPlacement = (ObjectStamp)target;
                if (objectPlacement != null && objectPlacement.transform.hasChanged)
                {
                    objectPlacement.transform.hasChanged = false;
                    var r = objectPlacement.transform.localRotation.eulerAngles;
                    r.z = 0;
                    objectPlacement.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(objectPlacement.GetBounds(), MicroVerse.InvalidateType.All);
                }
            }
        }
    }
}
