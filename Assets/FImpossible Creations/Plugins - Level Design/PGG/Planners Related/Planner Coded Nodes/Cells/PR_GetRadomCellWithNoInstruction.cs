using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells
{
    public class PR_GetRadomCellWithNoInstruction : PlannerRuleBase
    {
        public override string GetDisplayName( float maxWidth = 120 ) { return wasCreated ? "Get Random Cell With No Instruction" : "Get Random Cell With No Instruction"; }
        public override string GetNodeTooltipDescription { get { return "Getting random cell out of the provided planner"; } }
        public override Color GetNodeColor() { return new Color( 0.64f, 0.9f, 0.0f, 0.9f ); }
        public override Vector2 NodeSize { get { return new Vector2( 278, 120 ); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port( EPortPinType.Input, 1 )] public PGGPlannerPort GetCellFrom;
        [Port( EPortPinType.Output )] public PGGCellPort ChoosedCell;
        [Tooltip("Not supporting repetitive cell selection but calculated faster")]
        public bool NonRepetitive = true;

        static List<int> toCheckIndexesPool = new List<int>();

        public override void OnCustomPrepare()
        {
            base.OnCustomPrepare();
            toCheckIndexesPool.Clear();
        }

        public override void OnStartReadingNode()
        {
            if( CurrentExecutingPlanner == null ) return;

            GetCellFrom.TriggerReadPort( true );
            ChoosedCell.Clear();

            FieldPlanner planner = GetCellFrom.Get_Planner;
            if( planner == null ) return;
            if( planner.LatestResult == null ) return;

            Checker.CheckerField3D checker = planner.LatestChecker;

            if( NonRepetitive )
            {
                // Preapre cells id list to avoid repetiveness
                if( toCheckIndexesPool.Count == 0 )
                {
                    for( int i = 0; i < checker.ChildPositionsCount; i++ ) toCheckIndexesPool.Add( i );
                }

                for( int i = 0; i < checker.ChildPositionsCount; i++ )
                {
                    // Get cell index from non-repetitive list
                    int poolIndex = FGenerators.GetRandom( 0, toCheckIndexesPool.Count );
                    int cellId = toCheckIndexesPool[poolIndex];

                    var cell = checker.GetCell( cellId );
                    toCheckIndexesPool.RemoveAt( poolIndex );

                    bool isInstructionIn = false;
                    // Check if some instruction is placed within this cell
                    for( int c = 0; c < planner.LatestResult.CellsInstructions.Count; c++ )
                    {
                        if( planner.LatestResult.CellsInstructions[c].HelperCellRef == cell ) { isInstructionIn = true; break; }
                    }

                    // If no instruction - provide this cell and complete node operation
                    if( isInstructionIn == false )
                    {
                        ChoosedCell.ProvideFullCellData( cell, checker, planner.LatestResult );
                        return;
                    }
                }
            }
            else // Basic calculation
            {
                for( int i = 0; i < checker.ChildPositionsCount; i++ ) // Tries count = checker cells count
                {
                    var cell = checker.GetCell( FGenerators.GetRandom( 0, checker.ChildPositionsCount ) );

                    bool isInstructionIn = false;
                    // Check if some instruction is placed within this cell
                    for( int c = 0; c < planner.LatestResult.CellsInstructions.Count; c++ )
                    {
                        if( planner.LatestResult.CellsInstructions[c].HelperCellRef == cell ) { isInstructionIn = true; break; }
                    }

                    // If no instruction - provide this cell and complete node operation
                    if( isInstructionIn == false )
                    {
                        ChoosedCell.ProvideFullCellData( cell, checker, planner.LatestResult );
                        return;
                    }
                }
            }

        }
    }
}