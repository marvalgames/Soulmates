using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Logic
{

    public class PR_TrueFalseSwitch : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? ("    If A" + GetSign() + "B : False/True") : "If compare switch => return True\\False"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.8f, 0.55f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(190, _EditorFoldout ? 124 : 104); } }
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
            if (Negate)
                switch (LogicType)
                {
                    case FieldVariable.ELogicComparison.Equal: return "!=";
                    case FieldVariable.ELogicComparison.Greater: return "<=";
                    case FieldVariable.ELogicComparison.GreaterOrEqual: return "<";
                    case FieldVariable.ELogicComparison.Lower: return ">=";
                    case FieldVariable.ELogicComparison.LowerOrEqual: return ">";
                }

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
            bool reslt = FieldVariable.LogicComparison(AValue.Variable, BValue.Variable, LogicType);
            return Negate ? !reslt : reslt;
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
            EditorGUILayout.PropertyField(s, GUILayout.Width(NodeSize.x - 84));
            s.Next(false);

            var rect = GUILayoutUtility.GetLastRect();
            rect.position = new Vector2(rect.position.x + 31, rect.position.y);
            rect.width -= 31;
            GUI.Label(rect, Rnd(AValue.GetPortValueSafe) + " " + GetSign() + " " + Rnd(BValue.GetPortValueSafe) + " ?   " + (GetResult() ? "✓" : "x"), EditorStyles.centeredGreyMiniLabel);


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

        object Rnd(object val)
        {
            if (val is float) return System.Math.Round((float)val, 1).ToString();
            return val;
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
            EditorGUILayout.LabelField("Actual Out: " + Output.Value);

        }

#endif

    }
}