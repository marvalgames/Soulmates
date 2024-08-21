using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Math.Algebra
{

    public class PR_Lerp : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Lerp"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.5f, 0.75f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(120, 120); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Math; } }


        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "A", 1, typeof(int))] public PGGUniversalPort InValA;
        [HideInInspector] [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "B", 1, typeof(int))] public PGGUniversalPort InValB;
        [HideInInspector] [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "T")] public FloatPort InValT;
        [HideInInspector] [Port(EPortPinType.Output, true)] public PGGUniversalPort OutVal;

        public override void OnStartReadingNode()
        {
            InValA.TriggerReadPort(true);
            InValB.TriggerReadPort(true);
            InValT.TriggerReadPort(true);

            InValA.Variable.SetValue(InValA.GetPortValueSafe);
            InValB.Variable.SetValue(InValB.GetPortValueSafe);

            if (InValA.Variable.ValueType == FieldVariable.EVarType.Number )
            {
                OutVal.Variable.SetValue(Mathf.LerpUnclamped(InValA.Variable.Float, InValB.Variable.Float, InValT.GetInputValue));
            }
            else if (InValA.Variable.ValueType == FieldVariable.EVarType.Vector3)
            {
                OutVal.Variable.SetValue(Vector3.LerpUnclamped(InValA.Variable.GetVector3Value(), InValB.Variable.GetVector3Value(), InValT.GetInputValue));
            }
            else if (InValA.Variable.ValueType == FieldVariable.EVarType.Vector2)
            {
                OutVal.Variable.SetValue(Vector3.LerpUnclamped(InValA.Variable.GetVector2Value(), InValB.Variable.GetVector2Value(), InValT.GetInputValue));
            }
            else if (InValA.Variable.ValueType == FieldVariable.EVarType.Bool)
            {
                OutVal.Variable.SetValue(InValT.GetInputValue < 0.5f ? InValA.Variable.GetBoolValue() : InValB.Variable.GetBoolValue() );
            }
            else
            {
                UnityEngine.Debug.Log("To Implement");
            }
        }

#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (sp == null) sp = baseSerializedObject.FindProperty("InValB");
            SerializedProperty s = sp.Copy();


            EditorGUILayout.PropertyField(s, GUIContent.none);
            s.Next(false);
            EditorGUILayout.PropertyField(s, GUIContent.none);
            s.Next(false);
            GUILayout.Space(-21);
            GUILayout.BeginHorizontal();
            GUILayout.Space(19);
            EditorGUILayout.PropertyField(s, GUIContent.none);
            GUILayout.EndHorizontal();
        }
#endif

    }
}