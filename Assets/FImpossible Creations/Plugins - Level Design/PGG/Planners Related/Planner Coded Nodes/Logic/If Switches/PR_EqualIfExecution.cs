using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Logic
{

    public class PR_EqualIfExecution : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? ("   If A" + GetSign() + "B : False/True") : "If compare switch => Execute A or B"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.8f, 0.55f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(198, _EditorFoldout ? 132 : 110); } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }
        public override bool IsFoldable { get { return true; } }
        public override int OutputConnectorsCount { get { return 2; } }
        public override int HotOutputConnectionIndex { get { return 1; } }
        public override int AllowedOutputConnectionIndex { get { return outputId; } }
        int outputId = 0;

        public override string GetOutputHelperText(int outputId = 0)
        {
            if (outputId == 0) return "False";
            return "True";
        }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Logic; } }

        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "A", 1)] public PGGUniversalPort AValue;
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "B", 1)] public PGGUniversalPort BValue;
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
            if (_EditorDebugMode)
            {
                UnityEngine.Debug.Log("Comparing " + AValue.Variable.GetValue() + " with " + BValue.Variable.GetValue());
            }

            bool val = FieldVariable.LogicComparison(AValue.Variable, BValue.Variable, LogicType);
            return Negate ? !val : val;
        }


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            AValue.TriggerReadPort();
            AValue.Variable.SetValue(AValue.GetPortValueSafe);
            BValue.TriggerReadPort();
            BValue.Variable.SetValue(BValue.GetPortValueSafe);

            bool result = GetResult();
            if (result) outputId = 1; else outputId = 0;

            if (Debugging)
            {
                DebuggingInfo = "Comparing " + AValue.Variable.GetValue() + "   " + GetSign() + "   " + BValue.Variable.GetValue() + "   RESULT: " + (outputId == 0 ? "x" : "✓");
            }
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

            var rect = GUILayoutUtility.GetLastRect();
            rect.position = new Vector2(rect.position.x + 31, rect.position.y);
            rect.width -= 31;
            GUI.Label(rect, R(AValue) + " " + GetSign() + " " + R(BValue) + " ?   " + (outputId == 0 ? "x" : "✓"), EditorStyles.centeredGreyMiniLabel);

            if (_EditorFoldout)
            {
                s.Next(false);
                s.Next(false);
                EditorGUIUtility.labelWidth = 60;
                EditorGUILayout.PropertyField(s);
                EditorGUIUtility.labelWidth = 0;
            }

            //s.Next(false);
            //GUILayout.Space(-21);
            //GUILayout.BeginHorizontal();
            //GUILayout.Space(19);

            //if (_resLbl == null) _resLbl = new GUIContent();
            //_resLbl.text = AValue.GetPortValueSafe + " " + GetSign() + " " + BValue.GetPortValueSafe + " ?";
            //float lblWdth = EditorStyles.label.CalcSize(_resLbl).x;
            //if (lblWdth < 400)
            //    EditorGUILayout.LabelField(_resLbl, GUILayout.Width(lblWdth));

            ////EditorGUILayout.LabelField(AValue.GetPortValueSafe + " " + GetSign() + " " + BValue.GetPortValueSafe + " ?");
            ////EditorGUILayout.PropertyField(s, GUIContent.none);
            //GUILayout.EndHorizontal();
            baseSerializedObject.ApplyModifiedProperties();
        }

        object R(PGGUniversalPort p)
        {
            var val = p.Variable.GetValue();
            if (val is float) { return (float)System.Math.Round((float)val, 2); }
            return val;
        }

        GUIContent _resLbl = null;
        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            EditorGUILayout.LabelField("In A: " + AValue.GetPortValueSafe);
            EditorGUILayout.LabelField("In B: " + BValue.GetPortValueSafe);

            GUILayout.Space(2);
            EditorGUILayout.LabelField(" A " + GetSign() + " B ");

            GUILayout.Space(4);

            if (_resLbl == null) _resLbl = new GUIContent();
            _resLbl.text = AValue.Variable.GetValue() + " " + GetSign() + " " + BValue.Variable.GetValue() + " ?  ->  " + GetResult();
            EditorGUILayout.LabelField(_resLbl);

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Actual Out: " + (outputId == 0 ? "False" : "True"));

        }

#endif

    }
}