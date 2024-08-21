using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    //[CreateAssetMenu] // Use to generate reference file
    public class TestBuildPlanOperation : BuildPlannerOperationBase
    {
        public override string Description => "Just console logging during generating stages to check if everything works properly.";

        public override void OnPrepareBuildPlan(BuildPlannerPreset planner, BuildPlannerOperationHelper helper)
        {
            UnityEngine.Debug.Log("On Prepare Build Plan Operation");
        }

        public override void OnBuildPlanComplete(BuildPlannerPreset planner, BuildPlannerOperationHelper helper)
        {
            UnityEngine.Debug.Log("On Complete Build Plan Operation");
        }

        public override void OnBuildPlannerExecutorComplete(BuildPlannerPreset planner, BuildPlannerExecutor executor, BuildPlannerOperationHelper helper)
        {
            UnityEngine.Debug.Log("On Complete Build Planner Executor Operation");
        }
    }
}