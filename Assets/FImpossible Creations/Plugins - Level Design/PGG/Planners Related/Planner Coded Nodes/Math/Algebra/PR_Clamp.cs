using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Math.Algebra
{

    public class PR_Clamp : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Clamp"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.5f, 0.75f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(180, 120); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Math; } }


        [Port(EPortPinType.Input, EPortNameDisplay.Default, "Min", 1, typeof(int))] public FloatPort InValA;
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, "Max", 1, typeof(int))] public FloatPort InValB;
        [Tooltip("Value to be clamped")]
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, "Value")] public FloatPort InValT;
        [HideInInspector][Port(EPortPinType.Output, true)] public FloatPort OutVal;

        public override void OnStartReadingNode()
        {
            InValA.TriggerReadPort(true);
            InValB.TriggerReadPort(true);
            InValT.TriggerReadPort(true);

            OutVal.Value = Mathf.Clamp(InValT.GetInputValue, InValA.GetInputValue, InValB.GetInputValue);
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
            EditorGUILayout.PropertyField(s, GUIContent.none, GUILayout.Width(NodeSize.x - 78));
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