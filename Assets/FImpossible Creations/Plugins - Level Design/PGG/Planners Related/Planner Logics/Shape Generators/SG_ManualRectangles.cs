using FIMSpace.Generating.Checker;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class SG_ManualRectangles : ShapeGeneratorBase
    {
        public override string TitleName() { return "Manual Rectangles"; }
        public List<ShapeCellGroup> CellSets = new List<ShapeCellGroup>();
        public int drawSize = 30;
        public int depthLevel = 0;
        public int selectedManualShape = 0;


        public override CheckerField3D GetChecker(FieldPlanner planner)
        {
            int choose = 0;
            
            if (CellSets.Count > 1)
            {
                choose = FGenerators.GetRandom(0, CellSets.Count);
            }

            if (CellSets.Count == 0) return null;
            if (!CellSets.ContainsIndex(choose)) return null;

            return CellSets[choose].GetChecker().Copy();
        }

#if UNITY_EDITOR
        public override void OnGUIModify()
        {
        }

        public override void DrawGUI(SerializedObject so, FieldPlanner parent)
        {
            if (CellSets == null)
            {
                CellSets = new List<ShapeCellGroup>();
            }

            if (CellSets.Count == 0) { CellSets.Add(new ShapeCellGroup()); return; }
            if (!CellSets.ContainsIndex(selectedManualShape)) { selectedManualShape = 0; return; }

            CellsSelectorDrawer.DrawCellsSelector(CellSets[selectedManualShape], ref drawSize, ref depthLevel);

        }
#endif
    }
}