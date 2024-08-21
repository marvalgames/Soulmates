using FIMSpace.Graph;
using System.Collections.Generic;
using FIMSpace.Generating.Checker;
using FIMSpace.Generating.Planning.PlannerNodes.Generating;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Community
{
    public class PR_PathFindGenerateLowerCost : PR_FindPathTowards
    {
        public override string GetDisplayName( float maxWidth = 120 ) { return "Path Find - Lower Cost On (custom)"; }
        public override Vector2 NodeSize => base.NodeSize + new Vector2( 40, 40 );

        [Port( EPortPinType.Input )] public PGGPlannerPort LowerCostOn;
        public float CostMultiplierOn = 0.2f;
        List<CheckerField3D> lowerCostOn = new List<CheckerField3D>();

        public override void Execute( PlanGenerationPrint print, PlannerResult newResult )
        {
            LowerCostOn.TriggerReadPort( true );

            var checkers = LowerCostOn.Get_GetMultipleCheckers;
            lowerCostOn.Clear();
            if( checkers != null && checkers.Count > 0 ) for( int i = 0; i < checkers.Count; i++ ) lowerCostOn.Add( checkers[i].CheckerReference );

            base.Execute( print, newResult );
        }

        protected override CheckerField3D CallPathfind( CheckerField3D baseChecker, CheckerField3D startChecker, CheckerField3D targetChecker, List<CheckerField3D> collisions, FieldPlanner aPlanner, FieldPlanner bPlanner, CheckerField3D overlapRemoveA, CheckerField3D overlapRemoveB )
        {
            var pathfindSetup = PathfindSetup.ToCheckerFieldPathFindParams();
            pathfindSetup.StepCostAction = CheckCost;
            return baseChecker.GeneratePathFindTowards( startChecker, targetChecker, collisions, pathfindSetup, aPlanner, bPlanner, RemoveOverlappingCells, null, overlapRemoveA, overlapRemoveB );
        }

        float CheckCost( CheckerField3D path, FieldCell cell, FieldCell targetCell, float currentCost )
        {
            float multiply = 1f;
            Vector3 cellWPos = path.GetWorldPos( targetCell );

            for( int i = 0; i < lowerCostOn.Count; i++ )
            {
                if( lowerCostOn[i].ContainsWorld( cellWPos) ) { multiply = CostMultiplierOn; break; }
            }

            return currentCost * multiply;
        }
    }
}