using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Checker;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Generating
{
    public class PR_PathFindGenerate : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Path Find (deprecated)"; }
        public override string GetNodeTooltipDescription { get { return "Path find (A*) algorithm towards target position with collision detection. Supporting search towards Vector3 Position!"; } }
        public override bool IsFoldable { get { return true; } }
        public override EPlannerNodeVisibility NodeVisibility { get { return EPlannerNodeVisibility.Hidden; } } // Hide deprecated nodes from search prompt
        public override Vector2 NodeSize { get { return new Vector2(262, 170 + extraHeight); } }
        public override Color GetNodeColor() { return new Color(0.3f, 0.7f, .9f, 0.95f); }

        [Port(EPortPinType.Input)] public PGGPlannerPort SearchFrom;
        [Port(EPortPinType.Input)] public PGGPlannerPort SearchTowards;
        [Port(EPortPinType.Input)] public PGGPlannerPort CollideWith;
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        [Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGPlannerPort PathShape;

        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGVector3Port StartPathCellPos;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGVector3Port EndPathCellPos;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGVector3Port StartPathDir;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort ContactCell;

        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort From_StartCell;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort Path_StartCell;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort Path_EndCell;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort Towards_EndCell;

        [HideInInspector, SerializeField]
        private Checker3DPathFindSetup PathfindSetup = new Checker3DPathFindSetup();
        internal bool pathWasFound = false;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {

            #region Reseting on execution

            ContactCell.Clear();

            From_StartCell.Clear();
            Path_StartCell.Clear();
            Path_EndCell.Clear();
            Towards_EndCell.Clear();

            #endregion


            #region Prepare for search, initial conditions, collision masks

            pathWasFound = false;

            SearchFrom.TriggerReadPort(true);
            SearchTowards.TriggerReadPort(true);
            CollideWith.TriggerReadPort(true);

            PathShape.Clear();
            PathShape.Switch_DisconnectedReturnsByID = false;

            FieldPlanner a = GetPlannerFromPort(SearchFrom, false);
            FieldPlanner b = GetPlannerFromPort(SearchTowards, false);

            if (a == null) return;
            if (b == null) return;

            CheckerField3D bChec = null;

            if (SearchTowards.Connections.Count > 0)
            {
                for (int c = 0; c < SearchTowards.Connections.Count; c++)
                {
                    var conn = SearchTowards.Connections[c];

                    if (conn.PortReference.GetPortValue is Vector3)
                    {
                        Vector3 pos = (Vector3)conn.PortReference.GetPortValue;

                        // Temporary checker to search for it
                        bChec = new CheckerField3D();
                        bChec.RootPosition = pos;
                        bChec.AddLocal(Vector3.zero);
                        break;
                    }
                    else
                    {
                        if (SearchTowards.Connections.Count == 1)
                            if (conn.PortReference is PGGPlannerPort)
                            {
                                PGGPlannerPort plannerPrt = conn.PortReference as PGGPlannerPort;
                                if (plannerPrt.IsContaining_Null) { return; }
                            }
                    }
                }
            }

            if (bChec == null) bChec = b.LatestChecker;

            List<FieldPlanner> coll = GetPlannersFromPort(CollideWith, false, false);
            List<CheckerField3D> masks = new List<CheckerField3D>();

            for (int c = 0; c < coll.Count; c++)
            {
                masks.Add(coll[c].LatestChecker);
            }

            //masks.Remove(a.LatestChecker);
            //masks.Remove(bChec);

            #endregion

            var baseChek = newResult.Checker;
            var path = baseChek.GeneratePathFindTowards(a.LatestChecker, bChec, masks, PathfindSetup.ToCheckerFieldPathFindParams(), a,b);

            if (path != null)
            {

                FieldCell cell = baseChek._GeneratePathFindTowards_FromStartCell; // Start checker cell
                if (cell != null) From_StartCell.ProvideFullCellData(cell, a.LatestChecker, a.LatestResult);


                //path.DebugLogDrawCellInWorldSpace(path.GetCell(0), Color.red);
                if (path.AllCells.Count > 0)
                {
                    cell = baseChek._GeneratePathFindTowards_PathBeginCell;

                    StartPathCellPos.Value = path.GetWorldPos(cell);
                    if (path.ChildPositionsCount > 1) StartPathDir.Value = FVectorMethods.ChooseDominantAxis((path.GetWorldPos(1) - path.GetWorldPos(0)).normalized).normalized;
                    if (path.ChildPositionsCount > 1) EndPathCellPos.Value = path.GetWorldPos(baseChek._GeneratePathFindTowards_PathEndCell);

                    if (path.ChildPositionsCount > 1)
                        if (FGenerators.NotNull(baseChek._GeneratePathFindTowards_OtherTargetCell))
                            ContactCell.ProvideFullCellData(baseChek._GeneratePathFindTowards_OtherTargetCell, bChec, b.LatestResult);

                    if (cell != null) Path_StartCell.ProvideFullCellData(cell, path, newResult);
                    //path.DebugLogDrawCellInWorldSpace(cell, Color.red);

                    cell = baseChek._GeneratePathFindTowards_PathEndCell;
                    if (cell != null) Path_EndCell.ProvideFullCellData(cell, path, newResult);
                    //path.DebugLogDrawCellInWorldSpace(cell, Color.cyan);

                }


                cell = baseChek._GeneratePathFindTowards_OtherTargetCell; // End checker cell
                if (cell != null) Towards_EndCell.ProvideFullCellData(cell, b.LatestChecker, b.LatestResult);
                //b.LatestChecker.DebugLogDrawCellInWorldSpace(cell, Color.blue);



                PathShape.Output_Provide_Checker(path);

                pathWasFound = true;
            }

            #region Debugging Gizmos

#if UNITY_EDITOR

            if (Debugging)
            {
                DebuggingInfo = "Generating path from " + a.name + "(" + a.ArrayNameString + ") " + " towards " + b.name + "(" + b.ArrayNameString + ")";

                if (path == null)
                {
                    DebuggingInfo += " NOT FOUND!";
                    return;
                }

                Bounds ba = a.LatestChecker.GetFullBoundsWorldSpace();
                Bounds bb = b.LatestChecker.GetFullBoundsWorldSpace();

                CheckerField3D pathChe = path;

                DebuggingGizmoEvent = () =>
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                    Gizmos.DrawLine(ba.center, bb.center);
                    Gizmos.DrawWireCube(ba.center, ba.size);
                    Gizmos.color = new Color(.2f, .4f, 1f, 0.76f);
                    Gizmos.DrawWireCube(bb.center, bb.size);

                    pathChe.DrawFieldGizmos(true, false);
                };
            }
#endif
            #endregion

        }



        #region Editor View Code

        int extraHeight = 0;

#if UNITY_EDITOR

        UnityEditor.SerializedProperty sp = null;
        UnityEditor.SerializedProperty spCell = null;

        bool pathCellsFoldout = false;
        bool pathPosFoldout = false;

        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (GUILayout.Button("Display Path Find Setup"))
            {
                PathSettingsWindow.Init(this, PathfindSetup);
            }

            Editor_PathPositionsWireAllow(false);
            Editor_PathCellsWireAllow(false);

            baseSerializedObject.Update();

            extraHeight = 0;

            if (_EditorFoldout)
            {
                extraHeight += 46;

                GUILayout.Space(4);

                string foldStr = FGUI_Resources.GetFoldSimbol(pathCellsFoldout, true);

                if (GUILayout.Button(foldStr + "   Path Cells Helpers", EditorStyles.boldLabel)) { pathCellsFoldout = !pathCellsFoldout; }
                if (pathCellsFoldout)
                {
                    extraHeight += 82;
                    GUILayout.Space(4);
                    if (spCell == null) spCell = baseSerializedObject.FindProperty("From_StartCell");
                    EditorGUILayout.PropertyField(spCell);
                    var spc = spCell.Copy();
                    spc.Next(false); EditorGUILayout.PropertyField(spc);
                    spc.Next(false); EditorGUILayout.PropertyField(spc);
                    spc.Next(false); EditorGUILayout.PropertyField(spc);
                    pathPosFoldout = false;

                    Editor_PathCellsWireAllow(true);
                }

                foldStr = FGUI_Resources.GetFoldSimbol(pathPosFoldout, true);
                GUILayout.Space(4);

                if (GUILayout.Button(foldStr + "   Path Position Helpers", EditorStyles.boldLabel)) { pathPosFoldout = !pathPosFoldout; }
                if (pathPosFoldout)
                {
                    extraHeight += 88;
                    GUILayout.Space(4);
                    if (sp == null) sp = baseSerializedObject.FindProperty("StartPathCellPos");
                    SerializedProperty spc = sp.Copy();
                    EditorGUILayout.PropertyField(spc, true);
                    spc.Next(false); EditorGUILayout.PropertyField(spc, true);
                    spc.Next(false); EditorGUILayout.PropertyField(spc, true);
                    spc.Next(false); EditorGUILayout.PropertyField(spc, true);
                    pathCellsFoldout = false;

                    Editor_PathPositionsWireAllow(true);
                }

            }

            baseSerializedObject.ApplyModifiedProperties();
        }


        void Editor_PathCellsWireAllow(bool allow)
        {
            From_StartCell.AllowDragWire = allow;
            Path_StartCell.AllowDragWire = allow;
            Path_EndCell.AllowDragWire = allow;
            Towards_EndCell.AllowDragWire = allow;
        }

        void Editor_PathPositionsWireAllow(bool allow)
        {
            StartPathCellPos.AllowDragWire = allow;
            EndPathCellPos.AllowDragWire = allow;
            StartPathDir.AllowDragWire = allow;
            ContactCell.AllowDragWire = allow;
        }

#endif

        #endregion


    }

}