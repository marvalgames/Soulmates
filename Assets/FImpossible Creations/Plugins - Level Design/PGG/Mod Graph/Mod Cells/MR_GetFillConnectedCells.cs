using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Graph;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Rules;
using System;

namespace FIMSpace.Generating.Planning.ModNodes.Cells
{

    public class MR_GetFillConnectedCells : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Get Connected Cells" : "Get Connected Cells (fill)"; }
        public override string GetNodeTooltipDescription { get { return "Gathering all cells, which are reachable by current cell, when going cell-by-cell-neightbour (no diagonals)"; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override bool IsFoldable { get { return true; } }
        public override Vector2 NodeSize { get { return new Vector2(220, _EditorFoldout ? 142 : 104); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }


        [Port(EPortPinType.Input, 1)] public ESR_Space FillCondition = ESR_Space.InGrid;
        [Port(EPortPinType.Output)] public PGGModCellPort ResultCells;
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGModCellPort OriginCell;
        [HideInInspector] public bool IncludeSelf = false;


        public override void OnStartReadingNode()
        {
            OriginCell.TriggerReadPort(true);
            var cell = OriginCell.GetInputCellValue;

            if (FGenerators.IsNull(cell)) if (OriginCell.IsConnected == false) cell = MG_Cell;
            if (FGenerators.IsNull(cell)) return;

            List<FieldCell> cells = FieldCell.GatherFillPopulateCells(MG_Grid, cell, ConditionCheck, false);

            if (!IncludeSelf) if (MG_Cell != null) if (cells.Contains(MG_Cell)) cells.Remove(MG_Cell);

            ResultCells.ProvideCellsList(cells);
        }

        bool ConditionCheck(FieldCell originCell, FieldCell checkedCell)
        {
            return SpawnRuleBase.CellConditionsAllows(checkedCell, "", ESR_Details.Tag, FillCondition);
        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("OriginCell");

            if (_EditorFoldout)
            {
                OriginCell.AllowDragWire = true;
                EditorGUILayout.PropertyField(sp);
                SerializedProperty spc = sp.Copy(); spc.Next(false); EditorGUILayout.PropertyField(spc);
            }
            else
                OriginCell.AllowDragWire = false;

            baseSerializedObject.ApplyModifiedProperties();
        }
#endif

    }
}