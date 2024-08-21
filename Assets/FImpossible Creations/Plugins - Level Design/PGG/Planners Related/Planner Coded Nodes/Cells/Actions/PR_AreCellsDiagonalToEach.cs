using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{
    public class PR_AreCellsDiagonalToEach : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Is cell diagonal to" : "Is cell diagonal to other"; }
        public override string GetNodeTooltipDescription { get { return ""; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(210, 122); } }
        public override bool IsFoldable { get { return false; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port(EPortPinType.Input, EPortValueDisplay.NotEditable)] public PGGCellPort CellA;
        [Port(EPortPinType.Input, EPortValueDisplay.NotEditable)] public PGGCellPort CellB;
        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public BoolPort AreDiagonal;

        public override void OnStartReadingNode()
        {
            CellA.TriggerReadPort(true);
            CellB.TriggerReadPort(true);
            AreDiagonal.Value = false;

            FieldCell cllA = CellA.GetInputCellValue;
            if (cllA == null) return;
            CheckerField3D chA = CellA.GetInputCheckerValue;
            if (chA == null) return;

            FieldCell cllB = CellB.GetInputCellValue;
            if (cllA == null) return;
            CheckerField3D chB = CellB.GetInputCheckerValue;
            if (chB == null) return;

            AreDiagonal.Value = chA.IsCellDiagonalWith(cllA, cllB, chB);
        }

    }
}