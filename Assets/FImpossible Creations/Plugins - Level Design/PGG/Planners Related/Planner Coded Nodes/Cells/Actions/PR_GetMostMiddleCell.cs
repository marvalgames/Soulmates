using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{

    public class PR_GetMostMiddleCell : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Get Most Middle Cell"; }
        public override string GetNodeTooltipDescription { get { return "Getting cell most in the middle out of provided cells group"; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 230 : 224, _EditorFoldout ? (141 + extraH) : 104); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort Cells;
        [HideInInspector][Port(EPortPinType.Output, 1)] public PGGCellPort MiddleCell;

        [HideInInspector][Port(EPortPinType.Input, 1)] public IntPort TryGetCount;
        [HideInInspector][Port(EPortPinType.Input, EPortValueDisplay.NotEditable, 1)] public BoolPort SkipCondition;

        [HideInInspector][Port(EPortPinType.Output, 1)] public PGGPlannerPort CellsGroup;
        [HideInInspector][Port(EPortPinType.Output, 1)] public PGGCellPort CheckedCell;


        public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        {
            if (port != MiddleCell && port != CellsGroup) return;

            CellsGroup.Switch_MinusOneReturnsMainField = false;
            CellsGroup.Switch_DisconnectedReturnsByID = false;
            CellsGroup.Switch_ReturnOnlyCheckers = true;

            SkipCondition.Value = false;

            MiddleCell.Clear();
            CellsGroup.Clear();
            CheckedCell.Clear();
            Cells.TriggerReadPort(true);

            var checker = GetCheckerFromPort(Cells, false);
            if (checker == null) return;
            if (checker.AllCells.Count == 0) return;

            int count = TryGetCount.Value;

            if (TryGetCount.IsConnected)
            {
                TryGetCount.TriggerReadPort(true);
                count = TryGetCount.GetInputValue;
            }

            if (count < 0) count = 0;

            Bounds b = new Bounds(checker.AllCells[0].Pos, Vector3.zero);
            for (int c = 0; c < checker.AllCells.Count; c++) b.Encapsulate(checker.AllCells[c].Pos);

            if (SkipCondition.IsNotConnected && TryGetCount.IsNotConnected && count <= 0)
            {
                // Simple Mode
                var mCell = checker.GetNearestCellInWorldPos(checker.LocalToWorld(b.center));
                MiddleCell.ProvideFullCellData(mCell, checker, null);

                if (_EditorDebugMode)
                {
                    checker.DebugLogDrawBoundings(Color.white);
                    checker.DebugLogDrawCellsInWorldSpace(Color.green);
                }
            }
            else
            {
                #region Advanced mode

                discardedCells.Clear();

                CheckerField3D nChecker = new CheckerField3D();
                nChecker.CopyParamsFrom(checker);

                if (count < 1) count = 1;

                for (int c = 0; c < count * 8; c++) // max count * 8 tries
                {
                    float nearest = float.MaxValue;
                    FieldCell nearestCell = null;

                    for (int i = 0; i < checker.AllCells.Count; i++)
                    {
                        var chC = checker.AllCells[i];
                        if (discardedCells.Contains(chC)) continue;

                        float dist = Vector3.SqrMagnitude(b.center - chC.Pos);
                        if (dist < nearest)
                        {
                            nearestCell = chC;
                            nearest = dist;
                        }
                    }

                    CheckedCell.ProvideFullCellData(nearestCell, checker, null);
                    discardedCells.Add(nearestCell);

                    if (SkipCondition.IsConnected)
                    {
                        SkipCondition.TriggerReadPort(true);

                        if (SkipCondition.GetInputValue)
                        {
                            //checker.DebugLogDrawCellInWorldSpace(nearestCell, Color.red);
                            continue;
                        }
                    }

                    if (FGenerators.NotNull(nearestCell))
                    {
                        nChecker.AddLocal(nearestCell.Pos);
                        if (nChecker.AllCells.Count >= count) break;
                    }
                }

                if (nChecker.AllCells.Count > 0) CellsGroup.Output_Provide_Checker(nChecker);

                #endregion
            }
        }

        static List<FieldCell> discardedCells = new List<FieldCell>();

        #region Editor Code
        int extraH = 0;

#if UNITY_EDITOR
        SerializedProperty sp = null;
        SerializedProperty sp_cellsGr = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            CellsGroup.Editor_DefaultValueInfo = "(None)";

            base.Editor_OnNodeBodyGUI(setup);
            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("MiddleCell");
            if (sp_cellsGr == null) sp_cellsGr = baseSerializedObject.FindProperty("CellsGroup");

            TryGetCount.AllowDragWire = _EditorFoldout;

            int count = TryGetCount.Value;
            if (TryGetCount.IsConnected) count = TryGetCount.GetInputValue;
            if (count < 0) count = 0;

            SkipCondition.AllowDragWire = _EditorFoldout;
            CheckedCell.AllowDragWire = _EditorFoldout;
            CellsGroup.AllowDragWire = (count > 0) || SkipCondition.IsConnected || TryGetCount.IsConnected;
            MiddleCell.AllowDragWire = !CellsGroup.AllowDragWire;

            if (MiddleCell.AllowDragWire) EditorGUILayout.PropertyField(sp);
            else EditorGUILayout.PropertyField(sp_cellsGr);
            extraH = 0;

            if (_EditorFoldout)
            {
                if (TryGetCount.Value < 0) TryGetCount.Value = 0;

                SerializedProperty spc = sp.Copy();
                spc.Next(false);
                EditorGUILayout.PropertyField(spc); spc.Next(false);
                EditorGUILayout.PropertyField(spc);

                if (SkipCondition.IsConnected)
                {
                    extraH = 20;
                    spc.Next(false); spc.Next(false);
                    EditorGUILayout.PropertyField(spc);
                }
            }

            baseSerializedObject.ApplyModifiedProperties();
        }
#endif
        #endregion


    }
}