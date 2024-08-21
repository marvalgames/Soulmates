using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;

// !!!!! NAMESPACE IS DEFINING PATH TO THE NODE IN GRAP NODE CREATION MENU
namespace FIMSpace.Generating.Planning.PlannerNodes.CustomNodes
{

    public class BPNode_RectShape : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Rect Shape"; }
        public override string GetNodeTooltipDescription { get { return "Your custom tooltip which will appear after entering on the node header"; } }
        public override Color GetNodeColor() { return new Color(0.2f, 0.72f, 0.9f, 0.9f); }


        // Enable connectors if you will use node 'Execute()' override
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }


        // You need to adjust node size manually!
        public override Vector2 NodeSize { get { return new Vector2(230, 104); } }


        // Basic exmaple ports
        public Vector2Int Size;
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue, 1)] public PGGPlannerPort Shape;


        // HERE PUT CODE FOR NODES WHICH ARE NOT EXECUTED BUT ONLY ARE 
        // GIVING ACCESS TO SOME VARIABLES WITH OUTPUT NODE PORTS
        // EXECUTE() IS NOT NEEDED WHEN YOU'RE NOT USING TOP/BOTTOM CONNECTORS
        public override void OnStartReadingNode()
        {
            var currentPlannerExecuting = CurrentExecutingPlanner;
            if (currentPlannerExecuting == null) return;

            CheckerField3D checker = new CheckerField3D();
            checker.SetSize(Size.x, 1, Size.y);
            checker.CenterizeOrigin();

            checker.RootScale = currentPlannerExecuting.LatestChecker.RootScale;
            checker.RootPosition = currentPlannerExecuting.LatestChecker.RootPosition;

            Shape.Output_Provide_Checker(checker);
        }

    }
}
