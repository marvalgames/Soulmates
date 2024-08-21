using FIMSpace.Graph;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field
{
    public class PR_GetInternalValueVariable : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? " Get Internal Value" : "Get Internal Value Variable"; }
        public override string GetNodeTooltipDescription { get { return "Getting custom value variable contained out of field planner"; } }
        public override Color GetNodeColor() { return new Color(0.92f, 0.5f, 0.5f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(240, 122 + _extraHeight); } }

        private int _extraHeight = 0;

        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }


        [Port(EPortPinType.Input, 1)] public PGGPlannerPort VariableOf;

        [Tooltip("Null will be returned if no such variable exists within provided planner")]
        [Port(EPortPinType.Input, 1)] public PGGStringPort VariableName;

        [Tooltip("Null will be returned if no variable exists within provided planner")]
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.NotEditable,  1)] public PGGUniversalPort Value;
       
        [Tooltip("If it will be new variable generation, it can assign custom value as initial value")]
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGUniversalPort DefaultValue;

        public override void OnStartReadingNode()
        {
            base.OnStartReadingNode();

            VariableName.TriggerReadPort(true);
            Value.Variable.SetValue(0);

            string varName = VariableName.GetInputValue;
            if (string.IsNullOrEmpty(varName)) return; // No Variable Name

            VariableOf.TriggerReadPort(true);
            FieldPlanner plan = GetPlannerFromPort(VariableOf, false);

            if (plan == null) return;

            object defaultVal = null;

            if (DefaultValue.IsConnected)
            {
                DefaultValue.TriggerReadPort(true);
                defaultVal = DefaultValue.GetPortValueSafe;
            }

            var internalVar = plan.GetInternalValueVariable(varName, defaultVal, false);
            if (internalVar == null) return;

            Value.Variable.SetValue(internalVar);

        }

        //public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        //{

        //}



#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            _extraHeight = 0;

            if (_EditorFoldout)
            {
                _extraHeight = 20;
                if (sp == null) sp = baseSerializedObject.FindProperty("DefaultValue");
                EditorGUILayout.PropertyField(sp);
            }

            baseSerializedObject.ApplyModifiedProperties();

        }

#endif


    }
}