#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    [CreateAssetMenu] // Use to generate reference file
    public class RandomizeInstancesCountOperation : FieldPlannerOperationBase
    {
        public override string Description => "Randomize intanes count for the planner, called for main instance, before generating.";

        public override void OnStartPrepareFieldPlannerMainInstance(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper)
        {
            int min = helper.RequestVariable("Min", 1).GetIntValue();
            int max = helper.RequestVariable("Max", 4).GetIntValue();

            System.Random rand = new System.Random(build.LatestSeed);
            planner.Instances = FGenerators.GetRandomInclusive(min, max, rand);
        }

#if UNITY_EDITOR

        public override void Editor_DisplayGUI(FieldPlannerOperationHelper helper, BuildPlannerPreset build, FieldPlanner planner)
        {
            var minCount = helper.RequestVariable("Min", 1);
            var maxCount = helper.RequestVariable("Max", 4);

            minCount.SetValue(EditorGUILayout.IntField("Min:", minCount.GetIntValue()));
            if (minCount.GetIntValue() < 1) minCount.SetValue(1);
            maxCount.SetValue(EditorGUILayout.IntField("Max:", maxCount.GetIntValue()));
            if (maxCount.GetIntValue() < minCount.GetIntValue() ) maxCount.SetValue( minCount.GetIntValue());
        }

#endif

    }
}