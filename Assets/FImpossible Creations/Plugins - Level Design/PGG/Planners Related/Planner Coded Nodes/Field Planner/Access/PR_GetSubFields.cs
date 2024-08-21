using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetSubFields : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return " Get Sub Fields of"; }
        public override string GetNodeTooltipDescription { get { return "Get all sub fields of provided parent field"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 200 : 186, _EditorFoldout ? 101 : 81); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }

        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, 1)] public PGGPlannerPort SubFieldsOf;
        [HideInInspector][Port(EPortPinType.Output, true)] public PGGPlannerPort MultiplePlanners;
        [HideInInspector] public bool RandomizeOrder = false;

        public override void OnStartReadingNode()
        {
            if (CurrentExecutingPlanner == null) return;
            MultiplePlanners.Clear();

            FieldPlanner rootPlanner = null;

            rootPlanner = GetPlannerFromPort(SubFieldsOf);
            if (rootPlanner.Available == false) rootPlanner = null;

            if (rootPlanner == null) { return; }

            if (rootPlanner.GetSubFieldsCount == 0) { return; }

            List<FieldPlanner> planners = new List<FieldPlanner>();

            for (int s = 0; s < rootPlanner.GetSubFieldsCount; s++)
            {
                var sub = rootPlanner.GetSubField(s);

                if (sub == null) { continue; }
                if (!sub.Available) { continue; }
                planners.Add(sub);
            }

            if (_EditorDebugMode) UnityEngine.Debug.Log("providing " + planners.Count + " vs " + rootPlanner.GetSubFieldsCount);
            if (RandomizeOrder) planners.Shuffle();

            //if (_EditorDebugMode)
            //    for (int g = 0; g < planners.Count; g++)
            //        UnityEngine.Debug.Log("Getted [" + g + "] " + planners[g].ArrayNameString);

            MultiplePlanners.Output_Provide_PlannersList(planners);
        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            SubFieldsOf.Editor_DisplayVariableName = false;

            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("SubFieldsOf");
            SerializedProperty s = sp.Copy();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(s, GUILayout.Width(NodeSize.x - 80));
            GUILayout.EndHorizontal();

            s.Next(false);
            GUILayout.Space(-21);
            GUILayout.BeginHorizontal();
            GUILayout.Space(-27);
            EditorGUILayout.PropertyField(s, GUIContent.none);
            GUILayout.EndHorizontal();

            s.Next(false);
            if (_EditorFoldout) EditorGUILayout.PropertyField(s);

            baseSerializedObject.ApplyModifiedProperties();
        }
#endif

    }
}