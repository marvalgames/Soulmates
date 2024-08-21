using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    //[CreateAssetMenu] // Use to generate reference file
    public class TestFieldPlannerOperation : FieldPlannerOperationBase
    {
        public override string Description => "Just console logging during generating stages to check if everything works properly.";

        public override void OnStartPrepareFieldPlannerMainInstance(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper)
        {
            UnityEngine.Debug.Log("On Prepare Field Planner Main Instance Operation");
        }

        public override void OnStartPrepareFieldPlanner(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper)
        {
            UnityEngine.Debug.Log("On Prepare Field Planner Instance Operation");
        }

        public override void OnFieldPlannerComplete(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper)
        {
            UnityEngine.Debug.Log("On Field Planner Graph Complete");
        }

        public override void OnPlannerGeneringComplete(FieldPlannerOperationHelper helper, BuildPlannerPreset build, BuildPlannerExecutor executor, FieldPlanner planner, PGGGeneratorRoot generator)
        {
            UnityEngine.Debug.Log("On Planner Grid Spawning Complete");
        }
    }
}