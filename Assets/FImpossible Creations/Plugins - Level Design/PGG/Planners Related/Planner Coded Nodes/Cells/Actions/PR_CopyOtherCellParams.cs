using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{

    public class PR_CopyOtherCellParams : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "   Copy Cell Datas" : "Copy Cell Instruction\\Data"; }
        public override string GetNodeTooltipDescription { get { return "Use to copy cell instructions/datas of other fields cells."; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(174, 140); } }

        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }
        public override bool IsFoldable { get { return false; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port(EPortPinType.Input, 1)] public PGGCellPort Cell;
        public bool CopyInstruction = true;
        public bool CopyCellData = true;
        [Port(EPortPinType.Input, 1)] public PGGCellPort ApplyToCell;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            Cell.TriggerReadPort(true);
            ApplyToCell.TriggerReadPort(true);

            FieldCell cell = Cell.GetInputCellValue;
            CheckerField3D cellsShape = null;

            if (FGenerators.CheckIfIsNull(cell))
            {
                var dta = Cell.GetAnyData();
                if (dta.ParentChecker != null) cellsShape = dta.ParentChecker;
                if (cellsShape == null) return;
            }

            FieldCell otherCell = ApplyToCell.GetInputCellValue;
            CheckerField3D otherCellsShape = null;

            if (FGenerators.CheckIfIsNull(otherCell))
            {
                var dta = ApplyToCell.GetAnyData();
                if (dta.ParentChecker != null) otherCellsShape = dta.ParentChecker;
                if (otherCellsShape == null) return;
            }

            FieldPlanner tgtOwner = Cell.GetInputPlannerIfPossible;
            PlannerResult reslt = Cell.GetInputResultValue;
            if (reslt == null) if (tgtOwner != null) reslt = tgtOwner.LatestResult;

            FieldPlanner otgtOwner = ApplyToCell.GetInputPlannerIfPossible;
            PlannerResult oreslt = ApplyToCell.GetInputResultValue;
            if (oreslt == null) if (otgtOwner != null) oreslt = otgtOwner.LatestResult;

            if (cellsShape != null && reslt == null) return;
            if (otherCellsShape != null && oreslt == null) return;

            if (FGenerators.CheckIfExist_NOTNULL(cell))
            if (FGenerators.CheckIfExist_NOTNULL(otherCell))
            {

                #region Debugging Gizmos
#if UNITY_EDITOR
                if (Debugging)
                    if (reslt.Checker != null)
                    {
                        DebuggingInfo = "Copying cell instruction\\data";
                        Vector3 scl = reslt.Checker.RootScale;
                        Vector3 wPos = reslt.Checker.GetWorldPos(cell);
                        Vector3 owPos = oreslt.Checker.GetWorldPos(cell);

                        DebuggingGizmoEvent = () =>
                        {
                            Gizmos.color = Color.green * 0.5f;
                            Gizmos.DrawCube(wPos, scl * 0.7f);
                            Gizmos.color = Color.green;
                            Gizmos.DrawCube(owPos, scl * 0.7f);
                        };
                    }
#endif
                #endregion
            }

        }


    }
}