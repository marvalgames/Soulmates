using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Transforming
{

    public class PR_CenterFieldOrigin : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Center Field Origin"; }
        public override string GetNodeTooltipDescription { get { return "Change Field origin point which is very important for rotating field"; } }
        public override Color GetNodeColor() { return new Color(0.2f, 0.72f, 0.9f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(190, 84); } }
        public override bool IsFoldable { get { return false; } }

        [Port(EPortPinType.Input, EPortNameDisplay.Default)][Tooltip("Using self if no input")] public PGGPlannerPort Planner;
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner planner = GetPlannerFromPort(Planner);
            if (planner == null) { return; }

            Vector3 pos = planner.LatestChecker.RootPosition;
            Vector3 center = planner.LatestChecker.GetFullBoundsLocalSpace().center;
            center = center.V3toV3Int();
            planner.LatestChecker.ChangeOrigin(center);
            planner.LatestChecker.RootPosition = pos + (planner.LatestChecker.LocalToWorld(center) - pos);
        }

    }
}