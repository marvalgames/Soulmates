using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field
{

    public class PR_GetLocalVariable : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Get Local Variable"; }
        public override Color GetNodeColor() { return new Color(1.0f, 0.4f, 0.4f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(200, 82); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [HideInInspector] public int VariableID = 0;
        [HideInInspector][Port(EPortPinType.Output, true)] public PGGUniversalPort Value;


        public override void OnStartReadingNode()
        {
            IPlanNodesContainer container = FieldPlanner.GetNodesContainer(this);

            if (container == null)
            {
                if (_EditorDebugMode) UnityEngine.Debug.Log("No Container!");
                return;
            }

            PR_SetLocalVariable variable = container.GraphLocalVariables.GetLocalVar(VariableID);
            if (variable == null) { if (_EditorDebugMode) UnityEngine.Debug.Log("No variable!"); return; }


            if (variable.OverrideVariable == null)
            {
                variable.OnStartReadingNode();
                variable.Input.TriggerReadPort(true);
                Value.Variable.SetValue(variable.Value.Variable);
                if (_EditorDebugMode) UnityEngine.Debug.Log(" Set " + variable.Value.Variable.GetValue());
            }
            else
            {
                Value.Variable.SetValue(variable.OverrideVariable);
                if (_EditorDebugMode) UnityEngine.Debug.Log("Set From Override Variable " + variable.OverrideVariable.GetValue());
            }
        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (sp == null) sp = baseSerializedObject.FindProperty("VariableID");
            SerializedProperty s = sp.Copy();

            IPlanNodesContainer container = FieldPlanner.GetNodesContainer(this);

            if (container != null)
            {
                if (container.GraphLocalVariables == null) UnityEngine.Debug.Log("nul");

                if (container.GraphLocalVariables.GetVariablesCount() == 0)
                {
                    ScriptableObject sc = container as ScriptableObject;
                }

                VariableID = EditorGUILayout.IntPopup(VariableID, container.GraphLocalVariables.GetLocalVarsNameList(), container.GraphLocalVariables.GetLocalVarIDList(), GUILayout.Width(NodeSize.x - 83));
            }
            else
            {
                EditorGUILayout.LabelField(GetTryRefreshGUIC);
            }

            //EditorGUILayout.PropertyField(s, GUIContent.none, GUILayout.Width(NodeSize.x - 84));

            s.Next(false);
            GUILayout.Space(-21);
            GUILayout.BeginHorizontal();
            GUILayout.Space(-27);
            EditorGUILayout.PropertyField(s, GUIContent.none);
            GUILayout.EndHorizontal();
        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            GUILayout.Label("Latest Value: " + Value.Variable.GetValue());
        }
#endif

    }
}