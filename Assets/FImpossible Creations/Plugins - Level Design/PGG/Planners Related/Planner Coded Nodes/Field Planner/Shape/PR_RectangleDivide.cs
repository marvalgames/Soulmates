using FIMSpace.Graph;
using UnityEngine;
using FIMSpace.Generating.Checker;
using FIMSpace.Generating.Planning.GeneratingLogics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_RectangleDivide : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Rectangle Divide"; }
        public override string GetNodeTooltipDescription { get { return "Dividing rectangle area of the field into multiple pieces, like the 'Divided Rectangle' shape generator does it."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(216, 181); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToDivide;
        [Port(EPortPinType.Output)] public PGGPlannerPort ResultingSplits;

        [Port(EPortPinType.Input, 1)] public IntPort ClusterMinRange;
        [Port(EPortPinType.Input, 1)] public IntPort ClusterMaxRange;

        [Port(EPortPinType.Input, 1)] public IntPort ColumnsCount;
        [Port(EPortPinType.Input, 1)] public IntPort RowsCount;


        public enum ESplitMode
        {
            Random, AlwaysHorizontalSlice, AlwaysVerticalSlice
        }

        public override void OnCreated()
        {
            base.OnCreated();
            ResultingSplits.Switch_ReturnOnlyCheckers = true;
            ToDivide.Switch_ReturnOnlyCheckers = false;

            ClusterMinRange.Value = 3;
            ClusterMaxRange.Value = 5;

            ColumnsCount.Value = 3;
            RowsCount.Value = 2;
        }

        public override void OnStartReadingNode()
        {
            ResultingSplits.Clear();

            ToDivide.TriggerReadPort(true);
            ResultingSplits.Switch_ReturnOnlyCheckers = true;

            FieldPlanner plan = GetPlannerFromPort(ToDivide, false);
            ToDivide.Switch_ReturnOnlyCheckers = false;

            CheckerField3D myChe = null;

            if (plan!=null) if (plan.LatestChecker != null) myChe = plan.LatestChecker;
            if (myChe == null) myChe = ToDivide.GetInputCheckerSafe;
            if (myChe == null) { return; }

            if (ColumnsCount.Value < 0) ColumnsCount.Value = 1;
            if (RowsCount.Value < 0) RowsCount.Value = 1;

            if (ClusterMinRange.Value < 0) ClusterMinRange.Value = 1;
            if (ClusterMaxRange.Value < 0) ClusterMaxRange.Value = 1;

            ClusterMinRange.TriggerReadPort(true);
            ClusterMaxRange.TriggerReadPort(true);

            Vector2Int minMax = new Vector2Int(ClusterMinRange.GetInputValue, ClusterMaxRange.GetInputValue);

            ColumnsCount.TriggerReadPort(true);
            RowsCount.TriggerReadPort(true);

            myChe = myChe.Copy();

            var divs = SG_DividedRectangle.GenerateDivides(myChe, new MinMax(minMax.x, minMax.y), new MinMax(minMax.x, minMax.y), ColumnsCount.GetInputValue, RowsCount.GetInputValue,
                true, false, true);

            ResultingSplits.Output_Provide_CheckerList(divs);
        }

    }
}