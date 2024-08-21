using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Transforming
{

    public class PR_RoundFieldPosition : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Round Field Position"; }
        public override string GetNodeTooltipDescription { get { return "Rounding field position to be aligned with it's cell scale"; } }
        public override Color GetNodeColor() { return new Color(0.2f, 0.72f, 0.9f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 220 : 200, _EditorFoldout ? 122 : 82); } }
        public override bool IsFoldable { get { return false; } }

        [Port(EPortPinType.Input, 1)][Tooltip("Using self if no input")] public PGGPlannerPort Planner;

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            CheckerField3D checker = GetCheckerFromPort(Planner);
            if (checker == null) return;
            checker.RoundRootPositionToScale();
        }


    }
}