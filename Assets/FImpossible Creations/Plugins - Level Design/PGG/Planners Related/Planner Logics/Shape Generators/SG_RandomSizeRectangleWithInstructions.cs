using FIMSpace.Generating.Checker;
using System.ComponentModel;
using UnityEngine;

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class SG_RandomSizeRectangleWithInstructions : ShapeGeneratorBase
    {
        public override string TitleName() { return "Complex/Random Rectangle + Instructions"; }

        public MinMax Width = new MinMax(3, 4);
        public MinMax Depth = new MinMax(3, 4);
        public MinMax YLevels = new MinMax(1, 1);
        public bool OriginInCenter = false;

        [Space(5)]
        public int InstructionID = 0;

        enum EInstructionPlacement
        {
            RandomCell, RandomCellOnEdge, RandomCellInside,
            AllCells, Edges, Inside
        }

        [SerializeField] private EInstructionPlacement InstructionsPosition = EInstructionPlacement.AllCells;

        public override CheckerField3D GetChecker(FieldPlanner planner)
        {
            CheckerField3D checker = new CheckerField3D();
            checker.SetSize(Width.GetRandom(), YLevels.GetRandom(), Depth.GetRandom());
            if (OriginInCenter) checker.CenterizeOrigin();
            

            if (InstructionsPosition == EInstructionPlacement.RandomCell)
            {
                AddCellIntruction(planner, checker.AllCells[FGenerators.GetRandom(0, checker.AllCells.Count)], InstructionID);
            }
            else if ( InstructionsPosition == EInstructionPlacement.AllCells)
            {
                for (int i = 0; i < checker.AllCells.Count; i++)
                {
                    var parentCell = checker.GetCell(checker.AllCells[i].Pos, false);
                    if (FGenerators.IsNull(parentCell)) continue;
                    AddCellIntruction(planner, parentCell, InstructionID);
                }
            }
            else
            {
                if (InstructionsPosition == EInstructionPlacement.RandomCellInside || InstructionsPosition == EInstructionPlacement.Inside)
                {
                    var inside = checker.GetInlineChecker(true);

                    if ( InstructionsPosition == EInstructionPlacement.RandomCellInside)
                    {
                        AddCellIntruction(planner, inside.AllCells[FGenerators.GetRandom(0, inside.AllCells.Count)], InstructionID);
                    }
                    else
                    {
                        for (int i = 0; i < inside.AllCells.Count; i++)
                        {
                            var parentCell = checker.GetCell(inside.AllCells[i].Pos, false);
                            if (FGenerators.IsNull(parentCell)) continue;
                            AddCellIntruction(planner, parentCell, InstructionID);
                        }
                    }
                }
                else if (InstructionsPosition == EInstructionPlacement.RandomCellOnEdge || InstructionsPosition == EInstructionPlacement.Edges)
                {
                    var edge = checker.GetInlineChecker();

                    if (InstructionsPosition == EInstructionPlacement.RandomCellOnEdge)
                    {
                        AddCellIntruction(planner, edge.AllCells[FGenerators.GetRandom(0, edge.AllCells.Count)], InstructionID);
                    }
                    else
                    {
                        for (int i = 0; i < edge.AllCells.Count; i++)
                        {
                            var parentCell = checker.GetCell(edge.AllCells[i].Pos, false);
                            if (FGenerators.IsNull(parentCell)) continue;
                            AddCellIntruction(planner, parentCell, InstructionID);
                        }
                    }
                }
            }

            return checker;
        }


    }
}