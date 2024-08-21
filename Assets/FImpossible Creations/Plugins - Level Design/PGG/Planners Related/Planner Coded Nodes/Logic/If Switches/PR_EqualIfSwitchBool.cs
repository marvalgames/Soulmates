using FIMSpace.Graph;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Logic
{

    public class PR_EqualIfSwitchBool : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "REMOVE IT" : "If compare switch X => Return True \\ False"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.8f, 0.55f, 0.9f); }
        public override EPlannerNodeVisibility NodeVisibility { get { return EPlannerNodeVisibility.Hidden; } }
        public override Vector2 NodeSize { get { return new Vector2(192, _EditorFoldout ? 122 : 101); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Logic; } }

        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "A", 1)] public PGGUniversalPort AValue;
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "B", 1)] public PGGUniversalPort BValue;
        [HideInInspector][Port(EPortPinType.Output, true)] public BoolPort Output;
        [HideInInspector] public FieldVariable.ELogicComparison LogicType = FieldVariable.ELogicComparison.Equal;
        [Tooltip("Invert resulting bool value")]
        [HideInInspector] public bool Negate = false;

        string GetSign()
        {
            switch (LogicType)
            {
                case FieldVariable.ELogicComparison.Equal: return "==";
                case FieldVariable.ELogicComparison.Greater: return ">";
                case FieldVariable.ELogicComparison.GreaterOrEqual: return ">=";
                case FieldVariable.ELogicComparison.Lower: return "<";
                case FieldVariable.ELogicComparison.LowerOrEqual: return "<=";
            }

            return " ";
        }

        bool GetResult()
        {
            return (FieldVariable.LogicComparison(AValue.Variable, BValue.Variable, LogicType));
        }

        public override void OnStartReadingNode()
        {
            AValue.TriggerReadPort();
            AValue.Variable.SetValue(AValue.GetPortValueSafe);
            BValue.TriggerReadPort();
            BValue.Variable.SetValue(BValue.GetPortValueSafe);
            Output.Value = GetResult();
        }

#if UNITY_EDITOR
        SerializedProperty sp = null;
        SerializedProperty spLog = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("AValue");
            if (spLog == null) spLog = baseSerializedObject.FindProperty("LogicType");
            SerializedProperty s = sp.Copy();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(s, GUIContent.none, GUILayout.Width(32));
            EditorGUILayout.PropertyField(spLog, GUIContent.none, GUILayout.Width(70));
            GUILayout.Space(2);
            EditorGUILayout.LabelField("B", GUILayout.Width(17));
            GUILayout.EndHorizontal();
            s.Next(false);
            EditorGUILayout.PropertyField(s);
            s.Next(false);

            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += 42;
            rect.width -= 40;

            if (  (AValue.GetPortValueSafe is Single) && (BValue.GetPortValueSafe is Single) )
            {
                int av = Mathf.RoundToInt((Single)AValue.GetPortValueSafe);
                int bv = Mathf.RoundToInt((Single)BValue.GetPortValueSafe);
                GUI.Label(rect, av + " " + GetSign() + " " + bv + " ?  " + (Output.Value ? "✓" : "x"));
            }
            else
            GUI.Label(rect, "A " + GetSign() + " B ?  " + (Output.Value ? "✓" : "x") );

            GUILayout.Space(-21);
            GUILayout.BeginHorizontal();
            GUILayout.Space(19);
            EditorGUILayout.PropertyField(s, GUIContent.none);
            GUILayout.EndHorizontal();

            if (_EditorFoldout)
            {
                s.Next(false);
                s.Next(false);
                EditorGUIUtility.labelWidth = 60;
                EditorGUILayout.PropertyField(s);
                EditorGUIUtility.labelWidth = 0;
            }

            baseSerializedObject.ApplyModifiedProperties();
        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            EditorGUILayout.LabelField("In A: " + AValue.GetPortValueSafe);
            EditorGUILayout.LabelField("In B: " + BValue.GetPortValueSafe);

            GUILayout.Space(2);
            EditorGUILayout.LabelField(" A " + GetSign() + " B ");

            GUILayout.Space(4);
            EditorGUILayout.LabelField(AValue.GetPortValueSafe + " " + GetSign() + " " + BValue.GetPortValueSafe + " ?  ->  " + GetResult());

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Out: " + Output.GetPortValueSafe);

        }

#endif

    }
}