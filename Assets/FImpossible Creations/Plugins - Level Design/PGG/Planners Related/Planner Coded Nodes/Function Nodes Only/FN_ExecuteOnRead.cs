using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.FunctionNode
{

    public class FN_ExecuteOnRead : PlannerRuleBase
    {
        [HideInInspector] public string ParameterName = "Execute On Read";
        public override string GetDisplayName(float maxWidth = 120) { return ParameterName; }
        public override string GetNodeTooltipDescription { get { return "Useful for function node if some execution needs to be triggered on reading some port."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Externals; } }
        public override EPlannerNodeVisibility NodeVisibility { get { return EPlannerNodeVisibility.JustFunctions; } }

        public override Vector2 NodeSize { get { return new Vector2(150, 84); } }
        public override Color GetNodeColor() { return new Color(.4f, .4f, .4f, .95f); }

        public override bool DrawInspector { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return true; } }

        [HideInInspector][Port(EPortPinType.Input, true)] public PGGUniversalPort RunExecute;
        [HideInInspector] [Port(EPortPinType.Output, true)] public PGGUniversalPort Run;

        public override void OnStartReadingNode()
        {
            CallOtherExecutionWithConnector(-1, null);

            RunExecute.TriggerReadPort(true);
            Run.Variable.SetTemporaryReference(true, RunExecute);
        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("RunExecute");
            SerializedProperty s = sp.Copy();


            EditorGUILayout.PropertyField(s, GUIContent.none);
            s.Next(false);

            GUILayout.Space(-21);
            GUILayout.BeginHorizontal();
            GUILayout.Space(26);
            EditorGUILayout.PropertyField(s, GUIContent.none, GUILayout.Width(NodeSize.x - 80));
            GUILayout.EndHorizontal();


            baseSerializedObject.ApplyModifiedProperties();
        }
#endif

    }
}