using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.FunctionNode
{

    public class FN_ReadOnExecute : PlannerRuleBase
    {
        [HideInInspector] public string ParameterName = "Read On Execute";
        public override string GetDisplayName(float maxWidth = 120) { return ParameterName; }
        public override string GetNodeTooltipDescription { get { return "Mainly useful for function node, when some port don't have execution input but it calculates all data on port read."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Externals; } }
        //public override EPlannerNodeVisibility NodeVisibility { get { return EPlannerNodeVisibility.JustFunctions; } }

        public override Vector2 NodeSize { get { return new Vector2(150, 84); } }
        public override Color GetNodeColor() { return new Color(.4f, .4f, .4f, .95f); }

        public override bool DrawInspector { get { return true; } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }

        [HideInInspector][Port(EPortPinType.Input, true)] public PGGUniversalPort RunExecute;
        [HideInInspector][Port(EPortPinType.Output, true)] public PGGUniversalPort Run;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            RunExecute.TriggerReadPort(true);
            Run.Variable.SetValue( RunExecute.GetPortValueSafe);
            //Run.Variable.SetTemporaryReference(true, RunExecute.GetPortValueSafe);
            //Run.Variable.SetTemporaryReference(true, RunExecute);
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