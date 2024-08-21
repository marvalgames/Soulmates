using System.Collections.Generic;
using UnityEngine;
using FIMSpace.FEditor;
using UnityEditor;
using System;
using FIMSpace.Generating.Planning.GeneratingLogics;
using FIMSpace.Graph;
using FIMSpace.Generating.Planning.PlannerNodes;

namespace FIMSpace.Generating.Planning
{
    /// <summary>
    /// FM: Editor class component to enchance controll over component from inspector window
    /// </summary>
    [UnityEditor.CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(FieldPlanner))]
    public class FieldPlannerEditor : UnityEditor.Editor
    {
        public FieldPlanner Get { get { if (_get == null) _get = (FieldPlanner)target; return _get; } }
        private FieldPlanner _get;

        public static SerializedObject sp_shapeSo;
        public static SerializedProperty sp_fieldType = null;
        public static SerializedProperty sp_fieldPrefab = null;
        public static SerializedProperty sp_PreviewCellSize = null;
        //public static SerializedProperty sp_shapeSp;
        public static FieldPlanner latestViewed = null;
        public static Vector2 shapeScroll = Vector2.zero;
        public static bool focusOnShape = false;
        static Generating.FieldDesignWindow.EDrawVarMode _EditorDrawMode = FieldDesignWindow.EDrawVarMode.All;
        static int _selectedSubGraph = -1;
        static IPlanNodesContainer _lastDrawnContainer = null;

        private void OnEnable()
        {
            RefreshFieldVariables(serializedObject, Get);
            _selectedSubGraph = -1;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Field Planner in dedicated window", GUILayout.Height(38))) AssetDatabase.OpenAsset(Get);
            UnityEditor.EditorGUILayout.HelpBox("Field Planner should be displayed via Field Planner Window with Build Planner Designer Window!", UnityEditor.MessageType.Info);
            GUILayout.Space(4f);

            DrawDefaultInspector();

            GUILayout.Space(4f);
            DrawGUI(Get, serializedObject, null, new Rect(GUILayoutUtility.GetLastRect()));
        }


        public static void RefreshFieldVariables(SerializedObject so, FieldPlanner field)
        {
            if (latestViewed != field)
            {
                latestViewed = field;
            }

            if (field.ShapeGenerator != null)
            {
                bool gennew = false;

                try
                {
                    if (sp_shapeSo == null) gennew = true;
                    else if (sp_shapeSo.targetObject == null) gennew = true;
                    else if (sp_shapeSo.targetObject != field.ShapeGenerator) gennew = true;
                }
                catch (Exception)
                {
                    gennew = true;
                }

                if (gennew)
                {
                    //sp_shapeSo = so.FindProperty("ShapeGenerator");
                    //sp_shapeSp = so.FindProperty("ShapeGenerator");
                    sp_shapeSo = new SerializedObject(field.ShapeGenerator);
                }
            }

            if (sp_fieldType != null) if (sp_fieldType.serializedObject != so)
                {
                    sp_fieldType = null;
                    sp_fieldPrefab = null;
                    sp_PreviewCellSize = null;
                }

            if (sp_fieldType == null) sp_fieldType = so.FindProperty("FieldType");
            if (sp_fieldPrefab == null) sp_fieldPrefab = so.FindProperty("DefaultPrefab");
            if (sp_PreviewCellSize == null) sp_PreviewCellSize = so.FindProperty("PreviewCellSize");
        }


        static string editingName = "";
        static ScriptableObject isEditingName = null;
        public static void Editor_ResetNameEdit()
        {
            editingName = "";
            isEditingName = null;
            GUI.FocusControl("");
        }

        //static bool drawGraph = false;

        public static bool DrawGUI(FieldPlanner Get, SerializedObject so, FieldPlannerWindow plannerWindow, Rect position)
        {
            Color preBg = GUI.backgroundColor;
            bool changed = false;
            if (FieldPlannerWindow.forceChanged) changed = true;

            if (plannerWindow) if (plannerWindow.drawGraph)
                {
                    GUILayout.Space(-10);
                    EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle);
                }

            so.Update();
            RefreshFieldVariables(so, Get);
            EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
            GUILayout.Label(PGGUtils._PlannerIcon, GUILayout.Height(24), GUILayout.Width(24));

            #region Rename Field

            if (!isEditingName) editingName = Get.name;

            string nameEditName = "fptxt";
            GUI.SetNextControlName(nameEditName);
            editingName = GUILayout.TextField(editingName);

            if (GUI.GetNameOfFocusedControl() == nameEditName)
            {
                isEditingName = Get;
                if (Event.current != null)
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        Event.current.Use();
                        GUI.FocusControl("");
                    }
            }
            else
            {
                if (string.IsNullOrEmpty(editingName) == false)
                    if (isEditingName == Get)
                    {
                        //AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(Get), editingName);
                        Get.name = editingName;
                        EditorUtility.SetDirty(Get);
                        //AssetDatabase.SaveAssets();
                    }

                isEditingName = null;
            }

            #endregion

            //if (GUILayout.Button(Get.name, EditorStyles.boldLabel)) { ModificatorsPackEditor.RenamePopup(Get); }
            //if (GUILayout.Button(Get.name, EditorStyles.boldLabel)) { ModificatorsPackEditor.RenamePopup(Get); }
            EditorGUIUtility.labelWidth = 80;

            GUIContent tags = new GUIContent(Get.tag);
            Vector2 size = EditorStyles.label.CalcSize(tags);
            int sizeX = (int)Mathf.Max(80 + 40, 47 + 40 + size.x * 1.03f);
            GUILayout.Space(4f);
            Get.tag = EditorGUILayout.TextField("Planner Tag:", Get.tag, GUILayout.Width(sizeX));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            //SerializedProperty sp_params = so.FindProperty("FieldType");
            //SerializedProperty sp_PreviewCellSize = so.FindProperty("PreviewCellSize");
            //SerializedProperty sp_params = so.FindProperty("DefaultFieldSetup");

            GUI.backgroundColor = new Color(.35f, .6f, 1f, 1f);
            //DrawPGGFoldHeader(ref Get._EditorDrawParameters, "   Planner Preparation   ");

            //string foldS = FoldSimbol(Get._EditorDrawParameters);
            string foldS = " " + FGUI_Resources.GetFoldSimbol(Get._EditorDrawParameters, true);
            EditorGUILayout.BeginHorizontal(FGUI_Resources.BGInBoxStyleH);

            if (BuildPlannerWindow.Get && Get._EditorDrawParameters)
            {
                //GUI.backgroundColor = preBg;
                if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_Refresh, "Refresh planner preview (basically calling 'Next Preview')"), FGUI_Resources.ButtonStyle, GUILayout.Height(20), GUILayout.Width(19)))
                {
                    BuildPlannerWindow.Get.ClearGeneration();
                    BuildPlannerWindow.Get.RunGeneration();
                }
                //GUI.backgroundColor = new Color(.35f, .6f, 1f, 1f);
            }
            else
            {
                if (GUILayout.Button(new GUIContent(PGGUtils.TEX_Prepare), EditorStyles.label, GUILayout.Height(22), GUILayout.Width(21)))
                {
                    Get._EditorDrawParameters = !Get._EditorDrawParameters;
                }
            }


            if (GUILayout.Button(foldS + "   Field Planner Preparation", EditorStyles.boldLabel, GUILayout.Height(22)))
            //if (GUILayout.Button(foldS + "   Field Planner Preparation   " + foldS, FGUI_Resources.HeaderStyle, GUILayout.Height(22)))
            {
                Get._EditorDrawParameters = !Get._EditorDrawParameters;
            }

            if (BuildPlannerWindow.Get) if (Get._EditorDrawParameters) GUILayout.Space(27);

            GUILayout.FlexibleSpace();


            if (GUILayout.Button(FGUI_Resources.GUIC_Info, EditorStyles.label, GUILayout.Width(16), GUILayout.Height(20)))
            {
                EditorUtility.DisplayDialog("Planner Preparation", "In the 'Planner Preparation' tab you will define parameters which should be visible later in the 'Build Planner Executor' component, define how many copies of this Planner you want to generate on the build plan, define helper preview params and define graph variables.", "Ok");
            }

            EditorGUILayout.EndHorizontal();



            if (Get._EditorDrawParameters)
            {

                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
                GUI.backgroundColor = preBg;

                SerializedProperty sp = sp_fieldType.Copy();

                GUILayout.Space(4);


                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 40;
                int wdth = Get.FieldType == FieldPlanner.EFieldType.Prefab ? 114 : 142;
                EditorGUILayout.PropertyField(sp, new GUIContent("Type:", "Field Type"), GUILayout.Width(wdth)); sp.Next(false);

                if (Get.FieldType == FieldPlanner.EFieldType.BuildField)
                {
                    Get.FieldType = FieldPlanner.EFieldType.FieldPlanner;
                    UnityEngine.Debug.Log("[PGG] BuildField is not yet implemented!");
                }

                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                GUILayout.Label(" | ", GUILayout.Width(12));
                GUI.color = preBg;

                if (Get.FieldType == FieldPlanner.EFieldType.InternalField)
                {
                    EditorGUILayout.LabelField("(Ignored in the Executor)");
                }
                else
                {
                    if (Get.FieldType == FieldPlanner.EFieldType.Prefab)
                    {
                        Get.UpdateShapeGeneratorType();

                        EditorGUIUtility.labelWidth = 94;
                        EditorGUILayout.PropertyField(sp_fieldPrefab);
                        var spc = sp_fieldPrefab.Copy(); spc.Next(false);
                        //GUILayout.Space(12);
                        //EditorGUIUtility.labelWidth = 64;
                        //EditorGUILayout.PropertyField(spc, new GUIContent("Flat Grid", spc.tooltip));
                        //EditorGUILayout.LabelField("(Optional)", GUILayout.Width(56));
                    }
                    else
                    {
                        // Field for planner etc.
                        EditorGUIUtility.labelWidth = 74;
                        EditorGUILayout.PropertyField(sp, new GUIContent("Default:", "Default Field Setup"));
                        EditorGUILayout.LabelField("(Optional)", GUILayout.Width(56));
                    }
                }

                EditorGUIUtility.labelWidth = 0;
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();


                GUILayout.Space(4);
                sp.NextVisible(false); EditorGUILayout.PropertyField(sp);
                sp.NextVisible(false); //EditorGUILayout.PropertyField(sp);
                //EditorGUILayout.PropertyField(so.FindProperty("AllowRotateBy90"));
                /*sp.NextVisible(false);*/ //EditorGUILayout.PropertyField(sp);
                //sp.NextVisible(false); EditorGUILayout.PropertyField(sp);
                //sp.NextVisible(false); EditorGUILayout.PropertyField(sp);

                SerializedProperty sp_cellSzCpy = sp_PreviewCellSize.Copy();
                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 68;
                if (Get.DefaultFieldSetup)
                {
                    Get.PreviewCellSize = Get.DefaultFieldSetup.GetCellUnitSize();
                    GUI.enabled = false;

                    if (Get.PreviewCellSize.x == Get.PreviewCellSize.y && Get.PreviewCellSize.x == Get.PreviewCellSize.z)
                    {
                        EditorGUILayout.FloatField("Cell Size:", Get.PreviewCellSize.x);
                    }
                    else
                    {
                        EditorGUILayout.Vector3Field("Cell Size:", Get.PreviewCellSize);
                    }

                    GUI.enabled = true;
                }
                else
                {
                    EditorGUIUtility.labelWidth = 110;
                    Vector3 cellSz = new Vector3();
                    cellSz.x = EditorGUILayout.FloatField("Preview Cell Size", sp_cellSzCpy.vector3Value.x);
                    cellSz.y = cellSz.x;
                    cellSz.z = cellSz.x;
                    sp_cellSzCpy.vector3Value = cellSz;
                }

                EditorGUIUtility.labelWidth = 0;

                GUILayout.Space(8);
                sp_cellSzCpy.Next(false); EditorGUILayout.PropertyField(sp_cellSzCpy);
                EditorGUILayout.EndHorizontal();

                if (Get.Instances < 1) Get.Instances = 1;

                GUILayout.Space(8);
                //EditorGUILayout.BeginHorizontal(FGUI_Resources.BGInBoxStyle);
                //string varTitle = "   Variables";
                //if (GUILayout.Button(varTitle, EditorStyles.boldLabel)) { }
                //GUILayout.FlexibleSpace();
                //if (GUILayout.Button("+", GUILayout.Width(20))) { Get.Variables.Add(new FieldVariable("Variable " + (Get.Variables.Count+1).ToString(), 1f )); }
                //EditorGUILayout.EndHorizontal();

                //EditorGUILayout.BeginVertical();

                FieldDesignWindow.DrawFieldVariablesList(Get.FVariables, FGUI_Resources.BGInBoxStyle, "Planner Variables", ref Get._EditorSelectedVar, ref _EditorDrawMode, Get, false);
                //for (int i = 0; i < Get.Variables.Count; i++)
                //{
                //    FieldVariable.Editor_DrawTweakableVariable(Get.Variables[i]);
                //}
                //EditorGUILayout.EndVertical();
                GUILayout.Space(4);



                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("   " + FGUI_Resources.GetFoldSimbol(Get._Editor_OperationsFoldout, 10, "►") + "  " + "Operations:", FGUI_Resources.FindIcon("SPR_PlanPrint"), "Field Planner Operations list, to call scripted calculations during custom generating stages."), FGUI_Resources.FoldStyle, GUILayout.Height(22))) Get._Editor_OperationsFoldout = !Get._Editor_OperationsFoldout;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+", FGUI_Resources.ButtonStyle, GUILayout.Width(22))) { Get.Operations.Add(new FieldPlannerOperationHelper()); Get._Editor_OperationsFoldout = true; }
                EditorGUILayout.EndHorizontal();

                if (Get._Editor_OperationsFoldout)
                {
                    GUILayout.Space(4);
                    int toRemove = -1;

                    for (int i = 0; i < Get.Operations.Count; i++)
                    {
                        if (DisplayFieldPlannerOperations(Get.Operations[i], Get.ParentBuildPlanner, Get)) toRemove = i;
                    }

                    if (toRemove >= 0) Get.Operations.RemoveAt(toRemove);
                }

                GUILayout.Space(2);
                EditorGUILayout.EndVertical();


                so.ApplyModifiedProperties();

                EditorGUILayout.EndVertical();
            }
            GUI.backgroundColor = preBg;

            GUILayout.Space(4f);

            //bool pre = Get._EditorDrawShape;
            GUI.backgroundColor = new Color(.15f, .75f, .9f, 1f);
            DrawPGGFoldHeader(ref Get._EditorDrawShape, "   Field Initial Shape   ", false, Get);

            //EditorGUIUtility.labelWidth = 30;
            //FieldPlannerWindow.AutoRefreshInitialShapePreview = EditorGUILayout.Toggle(new GUIContent(FGUI_Resources.Tex_Refresh), FieldPlannerWindow.AutoRefreshInitialShapePreview, GUILayout.Width(60));
            //EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndHorizontal();

            //if (pre != Get._EditorDrawShape) changed = true;
            //EditorGUI.BeginChangeCheck();

            if (Get._EditorDrawShape)
            {
                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);

                Get.ExposeShape = EditorGUILayout.Toggle(new GUIContent("Expose Detection Shape", "Expose detection shape settings for planner executor - so it will be editable also there"), Get.ExposeShape);
                GUILayout.Space(3);

                GUI.backgroundColor = preBg;

                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);

                if (Get.ShapeGenerator == null)
                {
                    Get.SetShapeGenerator(CreateInstance<SG_StaticSizeRectangle>());
                }

                EditorGUILayout.BeginHorizontal();

                if (Get.FieldType != FieldPlanner.EFieldType.Prefab)
                {
                    EditorGUILayout.LabelField("Shape Algorithm:", GUILayout.Width(110));
                    var content = new GUIContent(Get.ShapeGenerator.TitleName());
                    var rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);

                    if (GUI.Button(rect, content, EditorStyles.popup))
                    //if ( GUILayout.Button(content, EditorStyles.popup ) )
                    {
                        GenericMenu menu = new GenericMenu();
                        AddShapeGeneratorContextMenuItems(menu, Get);

                        menu.AddItem(new GUIContent("+ Create Custom Shape Algorithm"), false, () =>
                        {
                            MonoScript script = MonoScript.FromScriptableObject(SG_NoShape.CreateInstance<SG_NoShape>());
                            if (script) EditorGUIUtility.PingObject(script);
                            EditorUtility.DisplayDialog("Custom Shape Generators", "There are not yet templates for custom shape generators.\n\nFor now you need to take a look on other generators in order to learn how to use them.", "Ok");
                        });

                        menu.DropDown(rect);
                    }

                }
                else
                {
                    EditorGUILayout.HelpBox("Prefab Field is using only  ' Prefab To Grid ' Shape Generator", MessageType.None);
                }

                GUI.backgroundColor = preBg;

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (Get.ShapeGenerator != null)
                {
                    //EditorGUI.BeginChangeCheck();

                    if (sp_shapeSo == null || sp_shapeSo.targetObject == null) RefreshFieldVariables(so, Get);

                    if (sp_shapeSo != null)
                    {
                        sp_shapeSo.Update();
                        Get.ShapeGenerator.DrawGUI(sp_shapeSo, Get);
                        sp_shapeSo.ApplyModifiedProperties();
                    }

                    //if (/*EditorGUI.EndChangeCheck() ||*/ Get.ShapeGenerator._editorForceChanged)
                    //{
                    //    Get.ShapeGenerator._editorForceChanged = false;
                    //    Get.ShapeGenerator.OnGUIModify();
                    //    sp_shapeSo.ApplyModifiedProperties();
                    //    UnityEditor.EditorUtility.SetDirty(sp_shapeSo.targetObject);

                    //    if (BuildPlannerWindow.Get.FocusOnShape) Get.PrepareInitialChecker();

                    //    changed = true;
                    //}
                }

                EditorGUILayout.EndVertical();

                GUILayout.Space(2);

                if (BuildPlannerWindow.Get)
                {
                    GUILayout.Space(4);

                    EditorGUILayout.BeginHorizontal();

                    if (BuildPlannerWindow.Get.FocusOnShape) GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Display Shape On Scene Gizmos", GUILayout.Height(24)))
                    {
                        BuildPlannerWindow.Get.FocusOnShape = !BuildPlannerWindow.Get.FocusOnShape;
                        changed = true;
                    }

                    GUI.backgroundColor = preBg;

                    if (BuildPlannerWindow.Get.FocusOnShape)
                    {
                        if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_Random, "Randomize Preview"), FGUI_Resources.ButtonStyle, GUILayout.Width(30), GUILayout.Height(24)))
                        {
                            Get.PrepareInitialChecker();
                            changed = true;
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }


                EditorGUILayout.EndVertical();
            }


            GUILayout.Space(4);




            FGUI_Inspector.DrawUILine(0.45f, 0.3f, 1, 9, 1);

            GUI.backgroundColor = new Color(0.65f, 1f, 0.65f, 1f);
            EditorGUIUtility.labelWidth = 90;
            FieldPlanner.EViewGraph preVw = Get.GraphView;

            EditorGUILayout.BeginHorizontal();
            //Get.GraphView = (FieldPlanner.EViewGraph)EditorGUILayout.EnumPopup("Graph Stage:", Get.GraphView);

            //GUI.color = new Color(1f, 1f, 1f, 0.5f);
            //GUILayout.Label(" | ", GUILayout.Width(12));
            //GUI.color = Color.white;
            EditorGUILayout.LabelField(new GUIContent("Stage:", "Graph Stage"), GUILayout.Width(42));

            GUI.backgroundColor = preBg;
            if (Get.GraphView == FieldPlanner.EViewGraph.Procedures_Placement) GUI.backgroundColor = new Color(0.4f, 1f, 0.4f, 1f);
            else if (Get.Procedures.Count > 1) GUI.backgroundColor = new Color(0.825f, 0.825f, 1f, 1f);

            if (GUILayout.Button("First Procedures", EditorStyles.miniButtonLeft)) { Get.GraphView = FieldPlanner.EViewGraph.Procedures_Placement; }
            GUI.backgroundColor = preBg;

            FieldPlanner.SubGraph subGraph = null;
            bool customGrph = Get.GraphView == FieldPlanner.EViewGraph.Procedures_CustomGraphs;

            if (customGrph)
            {
                FGenerators.CheckForNulls(Get.FSubGraphs);
                if (Get.FSubGraphs.ContainsIndex(_selectedSubGraph) == false) _selectedSubGraph = -1;
                if (_selectedSubGraph == -1) if (Get.FSubGraphs.Count > 0) { subGraph = Get.FSubGraphs[0]; _selectedSubGraph = 0; }

                if (_selectedSubGraph > -1) subGraph = Get.FSubGraphs[_selectedSubGraph];
            }

            if (customGrph) GUI.backgroundColor = new Color(0.4f, 1f, 0.4f, 1f);
            else if (Get.FSubGraphs != null) if (Get.FSubGraphs.Count > 0) GUI.backgroundColor = new Color(0.7f, 0.7f, 1f, 1f);

            if (GUILayout.Button(FGUI_Resources.Tex_Customize, EditorStyles.miniButtonMid, GUILayout.MaxWidth(32))) { Get.GraphView = FieldPlanner.EViewGraph.Procedures_CustomGraphs; }
            if (customGrph) if (GUILayout.Button(FGUI_Resources.Tex_DownFold, EditorStyles.miniButtonMid, GUILayout.Width(20)))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("+ Add New Custom Graph"), false, () => { Get.AddNewSubGraph(); _selectedSubGraph = Get.FSubGraphs.Count - 1; });

                    if (Get.FSubGraphs.Count > 0)
                    {
                        menu.AddItem(GUIContent.none, false, () => { });
                        menu.AddItem(GUIContent.none, false, () => { });

                        for (int i = 0; i < Get.FSubGraphs.Count; i++)
                        {
                            int ind = i;
                            menu.AddItem(new GUIContent("[" + (i + 1) + "] " + Get.FSubGraphs[i].GetDisplayName()), _selectedSubGraph == ind, () => { _selectedSubGraph = ind; });
                        }

                        if (subGraph != null)
                        {
                            menu.AddItem(GUIContent.none, false, () => { });
                            menu.AddItem(GUIContent.none, false, () => { });
                            menu.AddItem(new GUIContent("Rename " + subGraph.GetDisplayName()), false, () => { subGraph.Name = FGenerators.RenamePopup(null, subGraph.GetDisplayName(), false); });
                            menu.AddItem(GUIContent.none, false, () => { });
                            menu.AddItem(GUIContent.none, false, () => { });
                            menu.AddItem(new GUIContent("X  Remove  [" + (_selectedSubGraph + 1) + "] " + subGraph.GetDisplayName() + "  X"), false, () => { Get.RemoveSubGraph(_selectedSubGraph); _selectedSubGraph -= 1; });
                        }
                    }

                    menu.ShowAsContext();
                }

            GUI.backgroundColor = preBg;

            if (Get.GraphView == FieldPlanner.EViewGraph.PostProcedures_Cells) GUI.backgroundColor = new Color(0.4f, 1f, 0.4f, 1f);
            else if (Get.PostProcedures.Count > 1) GUI.backgroundColor = new Color(0.7f, 0.7f, 1f, 1f);

            if (GUILayout.Button(new GUIContent("Post Procedures", "Post procedures are executed after execution of all 'Procedures'. It's dedicated to generate relations between Fields after settings up layout."), EditorStyles.miniButtonRight)) { Get.GraphView = FieldPlanner.EViewGraph.PostProcedures_Cells; }
            GUI.backgroundColor = preBg;

            DrawFieldPlannerSelector(Get);

            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 0;
            GUI.backgroundColor = preBg;

            if (subGraph != null)
            {
                FieldPlannerWindow.LatestGraphNodesContainer = subGraph;

                GUILayout.Space(4);
                subGraph.ExecutionOrder = (FieldPlanner.SubGraph.EExecutionOrder)EditorGUILayout.EnumPopup("       Execution Mode:", subGraph.ExecutionOrder);
                GUILayout.Space(4);
            }
            //else
            {
                FieldPlannerWindow.LatestFieldPlanner = Get;
            }

            FGUI_Inspector.DrawUILine(0.45f, 0.3f, 1, 9, 1);


            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(2);


            if (plannerWindow != null)
            {
                if (plannerWindow.drawGraph) GUI.backgroundColor = Color.green;

                string foldStr = FGUI_Resources.GetFoldSimbol(plannerWindow.drawGraph, false);
                if (GUILayout.Button(foldStr + "   Show Graph   " + foldStr, GUILayout.Height(22)))
                {
                    plannerWindow.drawGraph = !plannerWindow.drawGraph;
                }

                if (plannerWindow.drawGraph) GUI.backgroundColor = preBg;
            }


            GUILayout.Space(6);
            if (GUILayout.Button(new GUIContent("  Graph In Separated Window", PGGUtils.TEX_PrintIcon), GUILayout.Height(22)))
            {
                if (!customGrph || subGraph == null)
                    PlannerGraphWindow.Init(Get, Get.GraphView);
                else
                    PlannerGraphWindow.Init(Get, subGraph, subGraph.GetDisplayName());
            }


            GUILayout.Space(5);

            EditorGUILayout.EndHorizontal();

            if (plannerWindow) if (plannerWindow.drawGraph) EditorGUILayout.EndVertical();

            //bool graphDrawn = false;
            if (plannerWindow != null)
                if (plannerWindow.drawGraph)
                {
                    bool drawGraph = true;

                    #region Custom Graph Prepare

                    if (customGrph)
                    {
                        if (subGraph == null)
                        {
                            GUILayout.Space(16);
                            if (GUILayout.Button("+ Add Custom Execution Graph +", GUILayout.Height(32)))
                            {
                                Get.AddNewSubGraph();
                            }

                            drawGraph = false;
                        }
                    }

                    #endregion

                    if (drawGraph)
                    {
                        IPlanNodesContainer container = Get;

                        if (Get.Graph_DisplayMode == FieldPlanner.EViewGraph.Procedures_CustomGraphs) container = subGraph;

                        #region Sub graph refresh

                        if (_lastDrawnContainer != container)
                        {
                            _lastDrawnContainer = container;
                            if (container is FieldPlanner.SubGraph && subGraph != null)
                            {
                                PlannerRuleBase.EnsurePlannerRulesOwner(subGraph.Procedures, subGraph);
                            }
                        }

                        #endregion


                        PlannerGraphDrawer graphDraw = plannerWindow.DrawGraph(true, so, Get.GraphView, container);

                        if (graphDraw != null)
                        {
                            graphDraw.DisplayMode = Get.GraphView;
                            graphDraw.RefreshTitle();

                            if (preVw != Get.GraphView)
                            {
                                if (graphDraw.Nodes != null) graphDraw.ResetGraphPosition();
                                graphDraw.TopTitle = "";
                            }
                        }
                    }


                }

            return changed;
        }

        #region Utilties

        public static List<Type> GetShapeGenerators()
        {
            List<Type> types = FieldModification.GetDerivedTypes(typeof(ShapeGeneratorBase));
            List<Type> myTypes = new List<Type>();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                if (type == typeof(ShapeGeneratorBase)) continue;
                myTypes.Add(type);
            }

            return myTypes;
        }

        public static string GetMenuName(Type type)
        {
            string name = type.ToString();
            name = name.Replace("FIMSpace.Generating.Planning.GeneratingLogics.", "");
            if (name.StartsWith("SG_")) name = name.Replace("SG_", "");
            return name.Replace('.', '/');
        }

        public static void AddShapeGeneratorContextMenuItems(GenericMenu menu, FieldPlanner planner)
        {
            List<Type> types = FieldModification.GetDerivedTypes(typeof(ShapeGeneratorBase));
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];

                if (type == typeof(ShapeGeneratorBase)) continue;
                string path = GetMenuName(type);
                if (string.IsNullOrEmpty(path)) continue;

                string name = path;
                ShapeGeneratorBase rule = CreateInstance(type) as ShapeGeneratorBase;
                if (rule != null) name = rule.TitleName();

                bool sel = type == planner.ShapeGenerator.GetType();

                menu.AddItem(new GUIContent(name), sel, () =>
                {
                    planner.SetShapeGenerator(rule);
                });
            }
        }

        public static void DrawFieldPlannerSelector(FieldPlanner f)
        {

            if (f.ParentBuildPlanner)
            {
                var parBuild = f.ParentBuildPlanner;

                int i = 0;
                for (i = 0; i < parBuild.BasePlanners.Count; i++)
                {
                    if (parBuild.BasePlanners[i] == FieldPlannerWindow.LatestFieldPlanner) break;
                }

                if (i < parBuild.BasePlanners.Count)
                {
                    int pre = i;

                    //GUI.backgroundColor = new Color(0.75f, 1f, 0.75f, 1f);
                    var names = parBuild.GetPlannersNameList();

                    float wdth = EditorStyles.label.CalcSize(names[i]).x + 26;
                    if (wdth < 140)
                        i = EditorGUILayout.IntPopup(i, names, parBuild.GetPlannersIDList(), GUILayout.Width(wdth));
                    else
                        i = EditorGUILayout.IntPopup(i, names, parBuild.GetPlannersIDList(), GUILayout.MaxWidth(140));

                    //GUI.backgroundColor = Color.white;
                    if (pre != i)
                    {
                        FieldPlannerWindow.LatestFieldPlanner = parBuild.BasePlanners[i];
                        FieldPlannerWindow.ForceSelectPlanner = FieldPlannerWindow.LatestFieldPlanner;
                    }
                }
            }
        }

        #endregion



        static bool DisplayFieldPlannerOperations(FieldPlannerOperationHelper operationHelper, BuildPlannerPreset build, FieldPlanner planner)
        {
            bool remove = false;
            EditorGUILayout.BeginHorizontal();
            operationHelper.Enabled = EditorGUILayout.Toggle(operationHelper.Enabled, GUILayout.Width(21));

            if (operationHelper.Operation)
                if (operationHelper.Operation.Foldable)
                    if (GUILayout.Button(FGUI_Resources.GetFoldSimbol(operationHelper.Foldout, true), EditorStyles.label, GUILayout.Height(20), GUILayout.Width(21))) { operationHelper.Foldout = !operationHelper.Foldout; }

            operationHelper.Operation = EditorGUILayout.ObjectField(operationHelper.Operation, typeof(FieldPlannerOperationBase), false) as FieldPlannerOperationBase;

            FGUI_Inspector.RedGUIBackground();
            if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(20), GUILayout.Width(22))) { remove = true; }
            FGUI_Inspector.RestoreGUIBackground();
            EditorGUILayout.EndHorizontal();

            if (operationHelper.Operation)
            {
                if (operationHelper.Foldout)
                {
                    GUILayout.Space(2);
                    if (!string.IsNullOrWhiteSpace(operationHelper.Operation.Description)) { EditorGUILayout.HelpBox(operationHelper.Operation.Description, MessageType.None); GUILayout.Space(2); }
                    operationHelper.Operation.Editor_DisplayGUI(operationHelper, build, planner);
                }
            }

            return remove;
        }

        public static bool DrawPGGFoldHeader(ref bool foldout, string title, bool endHor = true, FieldPlanner planner = null, Texture2D icon = null)
        {
            bool clicked = false;
            string foldS = "   " + FGUI_Resources.GetFoldSimbol(foldout, true);
            //string foldS = FoldSimbol(foldout);

            EditorGUILayout.BeginHorizontal(FGUI_Resources.BGInBoxStyleH);

            //GUI.color = new Color(1f, 1f, 1f, 0.7f);
            //if (GUILayout.Button(new GUIContent(PGGUtils.Tex_Selector), EditorStyles.label, GUILayout.Height(21), GUILayout.Width(20)))
            //{
            //    foldout = !foldout;
            //    clicked = true;
            //}
            //GUI.color = new Color(1f, 1f, 1f, 1f);

            if (GUILayout.Button(new GUIContent(foldS + title, PGGUtils.Tex_Selector), EditorStyles.boldLabel, GUILayout.Height(21)))
            //if (GUILayout.Button(foldS + title + foldS, FGUI_Resources.HeaderStyle, GUILayout.Height(21)))
            {
                foldout = !foldout;
                clicked = true;
            }

            //GUILayout.Space(20);

            if (GUILayout.Button(FGUI_Resources.GUIC_Info, EditorStyles.label, GUILayout.Width(16), GUILayout.Height(22)))
            {
                EditorUtility.DisplayDialog("Initial Shape", "In the 'Initial Shape' tab you will define base shape for this Field on the build plan.\nYou can make first field with complex shape and then use graph logics to place other fields (with less complex shapes like rectangles) on it.\nYou can also not use initial shape and generate it with nodes in the graph.\nThere are multiple approaches depending what is needed.", "Ok");
            }

            if (endHor)
                EditorGUILayout.EndHorizontal();

            return clicked;
        }
        public static string FoldSimbol(bool foldout) { if (foldout) return "▼"; else return "▲"; }


    }

}