using FIMSpace.FEditor;
using FIMSpace.Generating.Planner.Nodes;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;


namespace FIMSpace.Generating.Planning
{
    public class FieldPlannerWindow : EditorWindow
    {
        public static FieldPlannerWindow Get;
        Vector2 mainScroll = Vector2.zero;
        bool flex = false;
        public static FieldPlanner LatestFieldPlanner;
        public static IPlanNodesContainer LatestGraphNodesContainer;
        public Texture2D VignPlanner;

        //[MenuItem("Window/FImpossible Creations/Level Design/Field Planner Window (Beta)", false, 51)]
        static void Init(Vector3? position = null)
        {
            FieldPlannerWindow window = (FieldPlannerWindow)GetWindow(typeof(FieldPlannerWindow));
            window.titleContent = new GUIContent("Field Planner", Resources.Load<Texture>("PGG_PlannerSmall"));
            window.Show();
            window.minSize = new Vector2(240, 160);
            Get = window;

            if (position != null)
            {
                var pos = window.position;
                pos.position = position.Value;
                window.position = pos;
            }
        }


        [MenuItem("Window/FImpossible Creations/Level Design/Field Planner Window (If not appear)", false, 153)]
        static void InitFPlanner()
        {
            Init(new Vector3(100, 100));
        }


        [OnOpenAssetAttribute(1)]
        public static bool OpenBuildPlannerScriptableFile(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj as FieldPlanner != null)
            {
                Init();
                LatestFieldPlanner = obj as FieldPlanner;
                LatestGraphNodesContainer = obj as IPlanNodesContainer;
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            Get = this;
        }

        #region Simple Utilities

        public static void SelectFieldPlanner(FieldPlanner mod, bool show = true)
        {
            FieldPlannerWindow window = (FieldPlannerWindow)GetWindow(typeof(FieldPlannerWindow));
            Get = window;
            LatestFieldPlanner = mod;
            Get.prem = null;

            if (show)
            {
                window = (FieldPlannerWindow)GetWindow(typeof(FieldPlannerWindow));
                window.Show();
            }
        }

        [OnOpenAssetAttribute(1)]
        public static bool OpenFieldScriptableFile(int instanceID, int line)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj as FieldPlanner != null)
            {
                Init();
                LatestFieldPlanner = (FieldPlanner)obj;
                LatestGraphNodesContainer = (IPlanNodesContainer)obj;
                return true;
            }

            return false;
        }

        #endregion

        FieldPlanner prem = null;
        public static bool forceChanged = false;


        private void OnGUI()
        {
            PGGUtils.SetDarkerBacgroundOnLightSkin();
            bool changed = false;
            //EditorGUIUtility.labelWidth = 340;
            //flex = EditorGUILayout.Toggle("Toggle this if there is too many vertical elements to view", flex);
            //EditorGUIUtility.labelWidth = 0;

            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
            GUILayout.Space(5);

            //latestMod = (FieldPlanner)EditorGUILayout.ObjectField("Edited Planner", latestMod, typeof(FieldPlanner), false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Field Planner:", EditorStyles.boldLabel, GUILayout.Width(82));
            EditorGUILayout.HelpBox("Generator and placer of the Grid Area (Cells) to run Field Setup on", MessageType.None);
            //EditorGUILayout.LabelField("Generator and placer of the Grid Area / Cells to run Field Setup on");

            //FieldPlannerEditor.DrawFieldPlannerSelector(LatestFieldPlanner);

            EditorGUILayout.EndHorizontal();
            //EditorGUILayout.HelpBox("  Field Planner: Generator and placer of grid to run Field Setup on", MessageType.None);

            if (ForceSelectPlanner != null)
            {
                AssetDatabase.OpenAsset(ForceSelectPlanner);
                ForceSelectPlanner = null;
            }


            if (LatestFieldPlanner == null)
                if (Selection.activeObject is FieldPlanner)
                {
                    LatestFieldPlanner = (FieldPlanner)Selection.activeObject;
                    LatestGraphNodesContainer = (IPlanNodesContainer)Selection.activeObject;
                    mainScroll = Vector2.zero;
                }

            if (LatestFieldPlanner == null)
            {
                EditorGUILayout.HelpBox("Select some  'Field Planner'  through  'Build Planner Window'  to edit it here", MessageType.Info);
                GUILayout.Space(5);

                if (BuildPlannerWindow.Get == null)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button("Open new Build Planner Window"))
                    {
                        BuildPlannerWindow.Init();
                    }
                }

                //flex = EditorGUILayout.Toggle(flex);
                mainScroll = Vector2.zero;

                EditorGUILayout.EndScrollView();

            }
            else
            {
                if (prem != LatestFieldPlanner)
                {
                    //FieldPlannerEditor.RefreshSpawnersList(latestMod);
                    mainScroll = Vector2.zero;
                }

                SerializedObject so = new SerializedObject(LatestFieldPlanner);
                //FieldPlannerEditor.DrawHeaderGUI(so, latestMod);

                bool pre = EditorGUIUtility.wideMode;
                bool preh = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.hierarchyMode = true;

                if (flex)
                    EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle, GUILayout.Height(2200));
                else
                    EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxBlankStyle, GUILayout.ExpandHeight(true), GUILayout.MinHeight(position.height * 0.8f));

                if (drawGraph)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                }

                changed = FieldPlannerEditor.DrawGUI(LatestFieldPlanner, so, this, position);

                if (!flex) GUILayout.FlexibleSpace();

                if (!drawGraph) GUILayout.Space(5);

                EditorGUIUtility.hierarchyMode = preh;
                EditorGUIUtility.wideMode = pre;

                if (!drawGraph)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                }

            }

            prem = LatestFieldPlanner;

            if (forceChanged)
            {
                changed = true;
                forceChanged = false;
            }

            if (changed) BuildPlannerWindow.ForceUpdateView();

            PGGUtils.EndVerticalIfLightSkin();
        }

        public static void RefreshGraphView()
        {
            if (Get != null)
                if (Get.BuildPlannerGraphDraw != null)
                {
                    Get.BuildPlannerGraphDraw.ForceGraphRepaint();
                }
        }


        public Texture2D Tex_Net;
        public Texture2D Tex_Net2;
        public Texture2D Tex_Net3;

        private SerializedObject so_currentSetup = null;
        private PlannerGraphDrawer BuildPlannerGraphDraw = null;
        internal bool drawGraph = true;

        public static FieldPlanner ForceSelectPlanner = null;

        //public static bool AutoRefreshInitialShapePreview = true;

        internal PlannerGraphDrawer DrawGraph(bool endVerts, SerializedObject so, FieldPlanner.EViewGraph graphView, IPlanNodesContainer container)
        {
            if (so != null) so_currentSetup = so;


            #region Prepare Drawer / Re-initialize if required


            #region Commented but can be helpful later

            //ScriptableObject parentSO = null;
            //List<PGGPlanner_NodeBase> targetDrawList = null;

            //if (LatestFieldPlanner != null)
            //{
            //    parentSO = LatestFieldPlanner;
            //    LatestGraphDisplayMode = LatestFieldPlanner.Graph_DisplayMode;

            //    //if (LatestGraphDisplayMode != FieldPlanner.EViewGraph.Procedures_CustomGraphs)
            //        LatestGraphNodesContainer = LatestFieldPlanner as IPlanNodesContainer;

            //    if (LatestGraphDisplayMode == FieldPlanner.EViewGraph.Procedures_Placement) targetDrawList = LatestFieldPlanner.Procedures;
            //    else
            //    if (LatestGraphDisplayMode == FieldPlanner.EViewGraph.PostProcedures_Cells) targetDrawList = LatestFieldPlanner.PostProcedures;
            //    else
            //    {
            //        if (LatestGraphDisplayMode == FieldPlanner.EViewGraph.Procedures_CustomGraphs)
            //        {
            //            if (LatestGraphNodesContainer != null)
            //            {
            //                targetDrawList = LatestGraphNodesContainer.Procedures;
            //                parentSO = LatestGraphNodesContainer.ScrObj;
            //            }

            //            LatestFieldPlanner = null;
            //        }
            //    }

            //}
            //else
            //{
            //    if (LatestGraphNodesContainer != null)
            //    {
            //        targetDrawList = LatestGraphNodesContainer.Procedures;
            //        LatestGraphDisplayMode = FieldPlanner.EViewGraph.Procedures_CustomGraphs;
            //        parentSO = LatestGraphNodesContainer.ScrObj;
            //    }
            //}


            //bool reInitialize = false;

            //if (BuildPlannerGraphDraw == null) reInitialize = true;

            //if (LatestNodesList == null || LatestNodesList != targetDrawList)
            //    reInitialize = true;

            //if (so_currentSetup == null) reInitialize = true;

            //if (reInitialize)
            //{

            //    if (so_currentSetup == null || so_currentSetup.targetObject != parentSO)
            //    {
            //        //if (so.targetObject == parentSO) so_currentSetup = so;
            //        //else
            //            so_currentSetup = new SerializedObject(parentSO);
            //    }

            //    BuildPlannerGraphDraw = new PlannerGraphDrawer(this, LatestGraphNodesContainer);
            //}
            #endregion


            if (graphView != FieldPlanner.EViewGraph.Procedures_CustomGraphs)
            {
                IPlanNodesContainer targetDraw = container;
                if (LatestGraphNodesContainer != targetDraw) LatestGraphNodesContainer = targetDraw;

                if (LatestGraphNodesContainer != null)
                {
                    if (so_currentSetup == null) so_currentSetup = new SerializedObject(LatestGraphNodesContainer.ScrObj);

                    if (BuildPlannerGraphDraw == null || BuildPlannerGraphDraw.currentSetup != LatestGraphNodesContainer)
                    {
                        BuildPlannerGraphDraw = new PlannerGraphDrawer(this, LatestGraphNodesContainer);
                    }
                }
            }
            else
            {
                IPlanNodesContainer targetDraw = container;
                if (LatestGraphNodesContainer != targetDraw) LatestGraphNodesContainer = container;

                if (LatestGraphNodesContainer != null)
                {
                    if (so_currentSetup == null) so_currentSetup = new SerializedObject(LatestGraphNodesContainer.ScrObj);

                    if (BuildPlannerGraphDraw == null || BuildPlannerGraphDraw.currentSetup != LatestGraphNodesContainer)
                    {
                        BuildPlannerGraphDraw = new PlannerGraphDrawer(this, LatestGraphNodesContainer);
                    }
                }
            }



            #endregion


            if (BuildPlannerGraphDraw != null)
            {
                BuildPlannerGraphDraw.DrawedInsideInspector = true;

                #region Visuals Customization

                BuildPlannerGraphDraw.displayPadding = new Vector4(5, 0, 12, 8);
                if (VignPlanner != null) BuildPlannerGraphDraw.AltVignette = VignPlanner;

                BuildPlannerGraphDraw.Tex_Net = Tex_Net;
                if (graphView == FieldPlanner.EViewGraph.PostProcedures_Cells)
                { BuildPlannerGraphDraw.Tex_Net = FieldPlannerWindow.Get.Tex_Net2; }
                if (graphView == FieldPlanner.EViewGraph.Procedures_CustomGraphs)
                {
                    if (Get.Tex_Net3 != null) BuildPlannerGraphDraw.Tex_Net = FieldPlannerWindow.Get.Tex_Net3;
                    FieldPlanner.SubGraph subGr = container as FieldPlanner.SubGraph;
                    if (subGr != null) BuildPlannerGraphDraw.CustomGraphName = subGr.GetDisplayName();
                }

                #endregion

                BuildPlannerGraphDraw.Parent = this;
                BuildPlannerGraphDraw.DrawGraph();

                if (so_currentSetup != null)
                    if (BuildPlannerGraphDraw.AsksForSerializedPropertyApply)
                    {
                        so_currentSetup.ApplyModifiedProperties();
                        so_currentSetup.Update();
                        BuildPlannerGraphDraw.AsksForSerializedPropertyApply = false;
                    }
            }

            //UnityEngine.Debug.Log("Drawing " + BuildPlannerGraphDraw.DisplayMode + " procedures? " + (BuildPlannerGraphDraw.Nodes == ((FieldPlanner)container).Procedures) );

            return BuildPlannerGraphDraw;
        }


        private void Update()
        {
            if (FGenerators.RefIsNull(BuildPlannerGraphDraw)) return;
            BuildPlannerGraphDraw.Update();

            if (BuildPlannerGraphDraw.CheckDisplayRepaintRequest(PlannerGraphWindow._RefreshDrawFlag))
                Repaint();
        }

    }
}
