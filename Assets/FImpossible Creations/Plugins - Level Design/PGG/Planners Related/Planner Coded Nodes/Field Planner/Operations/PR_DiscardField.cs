using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Operations
{

    public class PR_DiscardField : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Discard Field"; }
        public override string GetNodeTooltipDescription { get { return "Discarding field or field duplicate from further execution and from being generated"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(168, 79); } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort Planner;


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            var planners = GetPlannersFromPort(Planner);

            for (int i = 0; i < planners.Count; i++)
            {
                planners[i].Discard(print);
                if (_EditorDebugMode) UnityEngine.Debug.Log("Discarding " + planners[i].ArrayNameString);
            }
        }

#if UNITY_EDITOR
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            Planner.Editor_DisplayVariableName = true;
            Planner.OverwriteName = " ";
            base.Editor_OnNodeBodyGUI(setup);
        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            EditorGUILayout.LabelField("To Discard: " + Planner.GetPortValueSafe);
            if (Planner.GetPortValueSafe is FieldPlanner) EditorGUILayout.LabelField((Planner.GetPortValueSafe as FieldPlanner).ArrayNameString);
        }
#endif

    }
}