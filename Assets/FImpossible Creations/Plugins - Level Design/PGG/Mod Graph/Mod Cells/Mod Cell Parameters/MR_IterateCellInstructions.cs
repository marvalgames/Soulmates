using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Graph;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.ModNodes.Cells
{

    public class MR_IterateCellInstructions : PlannerRuleBase
    {

        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Iterate Instructions" : "Iterate Cell Instructions"; }
        public override string GetNodeTooltipDescription { get { return "Iterating through every cell instruction within cell."; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override bool IsFoldable { get { return false; } }
        public override Vector2 NodeSize { get { return new Vector2(220, _EditorFoldout ? 160 : 154); } }

        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }
        public override int OutputConnectorsCount { get { return 2; } }
        public override int HotOutputConnectionIndex { get { return 1; } }
        public override int AllowedOutputConnectionIndex { get { return 0; } }
        public override string GetOutputHelperText(int outputId = 0)
        {
            if (outputId == 0) return "Finish";
            return "Iteration";
        }


        [Port(EPortPinType.Input, 1)] public PGGModCellPort InstructionsOf;

        [Space(4)]
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Iteration")] public IntPort IterationIndex;
        [Space(4)]
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Instruction ID")] public IntPort InstructionID;
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Direction")] public PGGVector3Port InstructionDirection;


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            var instr = MG_GridInstructions;
            if (instr == null) return;

            FieldCell instrOf = null;
            if (InstructionsOf.IsConnected)
            {
                InstructionsOf.TriggerReadPort(true);
                instrOf = InstructionsOf.GetInputCellValue;
            }

            if (instrOf == null) instrOf = MG_Cell;
            if (instrOf == null) return;

            int totalIter = 0;

            for (int i = 0; i < instr.Count; i++)
            {
                if (instr[i].gridPosition == instrOf.Pos)
                {
                    IterationIndex.Value = totalIter;
                    InstructionID.Value = instr[i].HelperID;
                    InstructionDirection.Value = instr[i].useDirection ? instr[i].FlatDirection : Vector3.zero;
                    CallOtherExecutionWithConnector(1, print);
                    totalIter += 1;
                }
            }
        }


    }
}