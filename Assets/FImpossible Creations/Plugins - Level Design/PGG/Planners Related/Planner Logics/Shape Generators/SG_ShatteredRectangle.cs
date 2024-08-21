using FIMSpace.Generating.Checker;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class SG_ShatteredRectangle : ShapeGeneratorBase
    {
        public override string TitleName() { return "Complex/Shattered Rectangle"; }

        public MinMax ZoneWidth = new MinMax(10, 14);
        public MinMax ZoneDepth = new MinMax(8, 10);

        [Space(5)]
        public MinMax MaxIterations = new MinMax(100, 110);
        public MinMax LimitShapesCount = new MinMax(4, 5);
        [Tooltip("Measuring size by the bounds diagonal length")]
        public MinMax MinimumFractionSize = new MinMax(2, 2);

        [Space(5)]
        [Tooltip("Cutting pieces not in half but with random split")]
        [HideInInspector] [Range(0, 1f)] public float RandomizeSlicing = 0f;

        [Tooltip("Use this if you want drive algorithm to keep some fractions in bigger size for specific rooms/fields.")]
        [Space(5)]
        public List<CheckerField3D.ShatterFractionRequest> UniqueFractionRequests = new List<CheckerField3D.ShatterFractionRequest>();

        static FGenerators.DefinedRandom rand = new FGenerators.DefinedRandom(0);


        public override CheckerField3D GetChecker(FieldPlanner planner)
        {
            rand.ReInitializeSeed(FGenerators.GetRandom(-10000, 10000));
            //UnityEngine.Debug.Log("planner size = " + planner.PreviewCellSize);
            
            CheckerField3D checker = new CheckerField3D();
            Vector3Int size = new Vector3Int(ZoneWidth.GetRandom(), 1, ZoneDepth.GetRandom());
            checker.SetSize(size.x, 1, size.z);

            var shapes = CheckerField3D.ShatterChecker(checker, MinimumFractionSize.GetRandom(), MaxIterations.GetRandom(), LimitShapesCount.GetRandom(), UniqueFractionRequests, rand, RandomizeSlicing);
            
            for (int s = 0; s < shapes.Count; s++)
            {
                var shape = shapes[s];
                CheckerField3D shapeChecker = shape.ToCheckerField(planner, true);
                planner.AddSubField(shapeChecker);
            }

            checker = new CheckerField3D(); 

            return checker;
        }

#if UNITY_EDITOR
        public override void DrawGUI(SerializedObject so, FieldPlanner parent)
        {
            EditorGUILayout.HelpBox("This Shape is providing multiple Sub-Fields inside the main Field!\n(Using 'Rectangle Packing' Algorithm)", MessageType.Info);
            base.DrawGUI(so,parent);
        }
#endif

    }
}