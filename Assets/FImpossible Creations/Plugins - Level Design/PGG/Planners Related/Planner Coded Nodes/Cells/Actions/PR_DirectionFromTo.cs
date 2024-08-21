using FIMSpace.Graph;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{

    public class PR_DirectionFromTo : PlannerRuleBase
    {
        public override string GetDisplayName( float maxWidth = 120 ) { return "Get Direction from to"; }
        public override string GetNodeTooltipDescription { get { return "Getting direction from one position to another, or from cell to cell"; } }
        public override Color GetNodeColor() { return new Color( 0.64f, 0.9f, 0.0f, 0.9f ); }
        public override Vector2 NodeSize { get { return new Vector2( 238, 118 ); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port( EPortPinType.Input, 1 )] public PGGVector3Port From;
        [Port( EPortPinType.Input, 1 )] public PGGVector3Port To;
        [Port( EPortPinType.Output, EPortValueDisplay.NotEditable )] public PGGVector3Port Direction;

        public override void OnStartReadingNode()
        {
            From.TriggerReadPort( true );
            To.TriggerReadPort( true );
            Vector3 from = Vector3.zero;

            if( From.GetPortValueSafe is PGGCellPort.Data )
            {
                var cData = (PGGCellPort.Data)From.GetPortValueSafe;
                var cell = cData.CellRef;
                if( cell != null )
                {
                    var planner = cData.AcquirePlanner;
                    if( planner != null ) from = planner.LatestChecker.LocalToWorld( cell.Pos );
                    else from = cell.Pos;
                }
            }
            else
                from = From.GetInputValue;

            Vector3 to = Vector3.zero;
            if( To.GetPortValueSafe is PGGCellPort.Data )
            {
                var cData = (PGGCellPort.Data)To.GetPortValueSafe;
                var cell = cData.CellRef;

                if( cell != null )
                {
                    var planner = cData.AcquirePlanner;
                    if( planner != null ) to = planner.LatestChecker.LocalToWorld( cell.Pos );
                    else to = cell.Pos;
                }
            }
            else
                from = To.GetInputValue;

            Vector3 dir = ( to - from ).normalized;

            Direction.Value = dir;
        }

    }
}