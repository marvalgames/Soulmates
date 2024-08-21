using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field
{

    public class PR_SetInternalValueVariable : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? " Set Internal Value" : "Set Internal Value Variable"; }
        public override string GetNodeTooltipDescription { get { return "Assigning value to custom variable contained per field planner"; } }

        public override Color GetNodeColor() { return new Color(0.92f, 0.5f, 0.5f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(240, 122 + _extraHeight); } }
        int _extraHeight = 0;
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }

        public override bool IsFoldable { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Tooltip("Variable will be assigned under target planner field")]
        [Port(EPortPinType.Input)] public PGGPlannerPort VariableOf;
        [Tooltip("Variable will be automatically generated with this string as ID")]
        [Port(EPortPinType.Input, 1)] public PGGStringPort VariableName;
        [Tooltip("Assign new value, if you want to Add/Subtract, foldout the node")]
        [Port(EPortPinType.Input, 1)] public PGGUniversalPort Value;
        [HideInInspector] public EOperation Operation = EOperation.Assign;
        [Tooltip("If it will be new variable generation, it can assign custom value as initial value")]
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGUniversalPort DefaultValue;

        public enum EOperation { Assign, Add, Subtract, Divide, Multiply }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            VariableName.TriggerReadPort(true);
            string varName = VariableName.GetInputValue;
            if (string.IsNullOrEmpty(varName)) return; // No Variable Name

            Value.TriggerReadPort(true);

            object varValue = null;
            varValue = Value.GetPortValueSafe;

            if (varValue == null) return; // No value to use

            DefaultValue.TriggerReadPort(true);
            object defaultVarVal = null;
            if (DefaultValue.IsConnected) defaultVarVal = DefaultValue.GetPortValueSafe;


            VariableOf.TriggerReadPort(true);
            FieldPlanner lastPlan = null;

            if (VariableOf.IsContaining_MultiplePlanners)
            {
                System.Collections.Generic.List<FieldPlanner> planners = GetPlannersFromPort(VariableOf, false, false);
                //UnityEngine.Debug.Log("multiple " + planners.Count);

                for (int p = 0; p < planners.Count; p++)
                {
                    SetVariable(planners[p], varName, varValue, defaultVarVal);
                    lastPlan = planners[p];
                }
            }
            else
            {
                FieldPlanner plan = GetPlannerFromPortAlways(VariableOf, false);
                SetVariable(plan, varName, varValue, defaultVarVal); lastPlan = plan;
            }


            if (Debugging)
            {
                if (lastPlan == null)
                    DebuggingInfo = "Failed Assigning Internal Value with ID: '" + varName + "'  because Null field was provided";
                else
                    DebuggingInfo = "Assigning Internal Value with ID: '" + varName + "'  for the field = " + lastPlan.ArrayNameString;
            }

        }

        void SetVariable(FieldPlanner field, string name, object value, object defaultValue)
        {
            if (field == null) return;

            if (defaultValue == null)
            {
                System.Type defType = value.GetType();
                if (defType.IsValueType)
                {
                    defaultValue = System.Activator.CreateInstance(defType);
                }
            }

            var internalVar = field.GetInternalValueVariable(name, defaultValue);
            if (internalVar != null)
            {
                if (Operation == EOperation.Assign)
                    internalVar.SetValue(value);
                else
                {
                    if (_algebraVarA == null) { _algebraVarA = new FieldVariable("", internalVar.GetValue()); _algebraVarB = new FieldVariable("", value); }

                    _algebraVarA.SetValue(internalVar.GetValue());
                    _algebraVarB.SetValue(value);

                    internalVar.AlgebraOperation(_algebraVarA, _algebraVarB, GetAlgebraOperation());
                }
            }
        }

        static FieldVariable _algebraVarA = null;
        static FieldVariable _algebraVarB = null;

        FieldVariable.EAlgebraOperation GetAlgebraOperation()
        {
            if (Operation == EOperation.Add) return FieldVariable.EAlgebraOperation.Add;
            if (Operation == EOperation.Subtract) return FieldVariable.EAlgebraOperation.Subtract;
            if (Operation == EOperation.Multiply) return FieldVariable.EAlgebraOperation.Multiply;
            if (Operation == EOperation.Divide) return FieldVariable.EAlgebraOperation.Divide;
            return FieldVariable.EAlgebraOperation.Add;
        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            _extraHeight = 0;

            if (_EditorFoldout)
            {
                _extraHeight = 38;
                if (sp == null) sp = baseSerializedObject.FindProperty("Operation");
                SerializedProperty s = sp.Copy();
                EditorGUILayout.PropertyField(s);
                s.Next(false); EditorGUILayout.PropertyField(s);
            }

            baseSerializedObject.ApplyModifiedProperties();

        }

#endif

    }
}