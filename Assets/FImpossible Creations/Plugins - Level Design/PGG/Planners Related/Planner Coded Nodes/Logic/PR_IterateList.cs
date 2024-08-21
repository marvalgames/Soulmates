using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Loops
{

    public class PR_IterateList : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Iterate List" : "Iterate List - for() Loop"; }
        public override string GetNodeTooltipDescription { get { return "Running loop iteration using inversal port, which should provide list<object>"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Logic; } }
        public override Color GetNodeColor() { return new Color(0.3f, 0.8f, 0.55f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(188, 151); } }
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

        [Port(EPortPinType.Input, EPortNameDisplay.Default, 1)] public PGGUniversalPort ToIterate;
        [Port(EPortPinType.Output, EPortNameDisplay.Default, 1)] public PGGUniversalPort Iterated;
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public IntPort IterationIndex;
        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Stop (Break)")] public BoolPort Stop;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            ToIterate.TriggerReadPort(true);
            Iterated.Variable.SetTemporaryReference(true, null);
            var input = ToIterate.GetPortValueSafe;

            if (input == null) return;
            List<object> inputs = input as List<object>;
            if (inputs == null) return;

            for (int c = 0; c < inputs.Count; c++)
            {
                Stop.TriggerReadPort(true);
                if (Stop.GetInputValue == true) break;

                IterationIndex.Value = c;
                Iterated.Variable.SetTemporaryReference(true, inputs[c]);
                CallOtherExecutionWithConnector(1, print);
            }
        }

    }
}