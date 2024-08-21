using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Loops
{

    public class PR_IterateCellsInLine : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Iterate Cells In Line"; }
        public override string GetNodeTooltipDescription { get { return "Running loop cell iteration starting on one cell and running up provided direction until no cell is detected"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }
        public override Color GetNodeColor() { return new Color(0.2f, 0.9f, 0.3f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(198, 131); } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }
        public override int OutputConnectorsCount { get { return 2; } }
        public override int AllowedOutputConnectionIndex { get { return 0; } }
        public override int HotOutputConnectionIndex { get { return 1; } }
        public override string GetOutputHelperText(int outputId = 0)
        {
            if (outputId == 0) return "Finish";
            return "Iteration";
        }

        [Port(EPortPinType.Input)] public PGGCellPort StartCell;
        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.NotEditable)] public PGGVector3Port CheckDirection;
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort IterationCell;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            StartCell.TriggerReadPort(true);
            FieldCell checkStartCell = StartCell.GetInputCellValue;

#if UNITY_EDITOR
            posOfIters.Clear();
#endif

            if (FGenerators.CheckIfIsNull(checkStartCell)) { return; }

            CheckerField3D checker = StartCell.GetInputCheckerValue;

            if (FGenerators.CheckIfIsNull(checker)) { /*UnityEngine.Debug.Log("null checker");*/ return; }

            //PlannerResult plannerOwner = StartCell.GetInputResultValue;

            //if (FGenerators.CheckIfIsNull(plannerOwner)) { /*UnityEngine.Debug.Log("null plannerOwner");*/ return; }
            //if (FGenerators.CheckIfIsNull(plannerOwner.ParentFieldPlanner)) { /*Debug.Log("null ParentFieldPlanner");*/ return; }

            FieldPlanner cellPlanner = StartCell.GetInputPlannerIfPossible;
            if (cellPlanner == null) return;

            IterationCell.ProvideFullCellData(checkStartCell, checker, newResult);

            CheckDirection.TriggerReadPort(true);
            Vector3Int dir = CheckDirection.GetInputValue.normalized.V3toV3Int();

            if (dir == Vector3Int.zero)
            {
#if UNITY_EDITOR
                if (Debugging) DebuggingInfo = "Check cells direction is zero!";
#endif
                    return;
            }

            Vector3Int startPos = checkStartCell.Pos;

            for (int i = 1; i < 1000; i++) // 1000 is just safety limit, for needs to stop with break;
            {
                Vector3Int newPos = startPos + dir * i;
                FieldCell checkedCell = checker.GetCell(newPos);

                if (FGenerators.NotNull(checkedCell))
                {
                    Iterate(checkedCell, checker, newResult, print);
                }
                else break;
            }

            #region Debugging Gizmos
#if UNITY_EDITOR
            if (Debugging)
            {
                DebuggingInfo = "Cells in line iteration";

                DebuggingGizmoEvent = () =>
                {
                    Gizmos.color = Color.green;

                    for (int i = 0; i < posOfIters.Count; i++)
                    {
                        Gizmos.DrawCube(posOfIters[i], checker.RootScale * 0.5f);
                    }

                };
            }
#endif
            #endregion

        }


#if UNITY_EDITOR
        private List<Vector3> posOfIters = new List<Vector3>();
#endif
        void Iterate(FieldCell cell, CheckerField3D checker, PlannerResult result, PlanGenerationPrint print)
        {
            IterationCell.ProvideFullCellData(cell, checker, result);
            CallOtherExecutionWithConnector(1, print);

#if UNITY_EDITOR
            if (Debugging)
            {
                posOfIters.Add(checker.GetWorldPos(cell));
            }
#endif
        }

    }
}