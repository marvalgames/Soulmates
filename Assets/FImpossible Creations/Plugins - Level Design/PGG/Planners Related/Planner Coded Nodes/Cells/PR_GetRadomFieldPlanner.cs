using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetRadomFieldPlanner : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Get Random Field Instance" : "Get Random Field Instance"; }
        public override string GetNodeTooltipDescription { get { return "Getting random Field Setup instance"; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(228, _EditorFoldout ? 118 : 98); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port(EPortPinType.Input, EPortValueDisplay.Default, 1)] public PGGPlannerPort InstancesOf;
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGPlannerPort ChoosedInstance;

        public override void OnStartReadingNode()
        {
            if (CurrentExecutingPlanner == null) return;

            var planner = GetPlannerFromPort(InstancesOf);
            if (planner == null) return;

            int id = FGenerators.GetRandom(0, planner.Instances+1);

            if (id == 0 || (id-1) >= planner.GetDuplicatesPlannersList().Count) ChoosedInstance.Output_Provide_Planner(planner);
            else
                ChoosedInstance.Output_Provide_Planner(planner.GetDuplicatesPlannersList()[id-1]);

        }
    }
}