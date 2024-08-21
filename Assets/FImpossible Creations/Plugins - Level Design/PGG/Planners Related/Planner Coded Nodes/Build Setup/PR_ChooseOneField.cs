using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.BuildSetup
{

    public class PR_ChooseOneField : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? " Choose One Field" : "Choose One Field with Condition"; }
        public override string GetNodeTooltipDescription { get { return "Checking provided group of fields and choosing one with most relevant condition provided."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(246, _EditorFoldout ? 195 : 175); } }

        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return true; } }
        public override int AllowedOutputConnectionIndex { get { return 0; } }
        public override string GetOutputHelperText(int outputId = 0)
        {
            return "Condition Met Callback";
        }

        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.Default, "Fields to Check")] public PGGPlannerPort FieldsToCheck;
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Checked Field")] public PGGPlannerPort IterationField;
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "A")] public PGGUniversalPort CompareA;
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "B   :  Choose with condition")] public PGGUniversalPort CompareB;
        //[HideInInspector][Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Condition Met : B")] public PGGUniversalPort OnConditionMet;

        [HideInInspector] public FieldVariable.ELogicComparison LogicType = FieldVariable.ELogicComparison.Equal;

        [HideInInspector][Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "Choosen Field")] public PGGPlannerPort ChoosenField;

        [Tooltip("When searching for the biggest/smallest element, then i should be disabled.\nIf searching for first with some condition it should be enabled.\n(it's automatically enabled when using 'Equal' switch)")]
        [HideInInspector] public bool StopOnFirstCorrect = false;

        public override void OnCustomPrepare()
        {
            ChoosenField.Switch_MinusOneReturnsMainField = false;
        }

        public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        {
            if (port != ChoosenField) return;
            ChoosenField.Clear();

            if (FieldsToCheck.IsNotConnected)
            {
                var planner = GetPlannerFromPort(FieldsToCheck);
                if (planner != null) IterateList(new List<FieldPlanner>() { planner });
                return;
            }

            FieldsToCheck.TriggerReadPort(true);

            if (FieldsToCheck.IsContaining_Checker)
            {
                // Try iterate checker list
                List<ICheckerReference> checkers = FieldsToCheck.Get_GetMultipleCheckers;
                IterateList(checkers);
                return;
            }

            List<FieldPlanner> planners = GetPlannersFromPort(FieldsToCheck, false, false);
            IterationField.Switch_ReturnOnlyCheckers = false;

            IterateList(planners);
        }

        void IterateList(List<FieldPlanner> planners)
        {
            if (planners == null) return;
            if (planners.Count == 0) return;

            FieldPlanner choosen = null;
            bool breakMode = StopOnFirstCorrect || LogicType == FieldVariable.ELogicComparison.Equal;

            for (int c = 0; c < planners.Count; c++)
            {
                if (planners[c].Available == false) continue;

                IterationField.Output_Provide_Planner(planners[c]);

                CompareA.TriggerReadPort(true);
                CompareA.Variable.SetValue(CompareA.GetPortValueSafe);
                CompareB.TriggerReadPort(true);
                CompareB.Variable.SetValue(CompareB.GetPortValueSafe);
                //UnityEngine.Debug.Log("comparing to : " + CompareB.GetPortValueSafe);
                bool conditionsMet = FieldVariable.LogicComparison(CompareA.Variable, CompareB.Variable, LogicType);
                //UnityEngine.Debug.Log("[" + c + "] " + CompareA.Variable.GetValue() + "   VS  " + CompareB.Variable.GetValue());



                if (conditionsMet)
                {
                    choosen = planners[c];
                    if (breakMode) break;
                    CallOtherExecutionWithConnector(-1, null);
                }
            }

            if (choosen == null) return;
            ChoosenField.Output_Provide_Planner(choosen);
            //FDebug.DrawBounds3D(choosen.LatestChecker.GetFullBoundsWorldSpace(), Color.yellow);
        }


        void IterateList(List<ICheckerReference> checkers)
        {
            if (checkers == null) return;
            if (checkers.Count == 0) return;

            CheckerField3D choosen = null;
            bool breakMode = StopOnFirstCorrect || LogicType == FieldVariable.ELogicComparison.Equal;

            for (int c = 0; c < checkers.Count; c++)
            {
                IterationField.Output_Provide_Checker(checkers[c].CheckerReference);

                CompareA.TriggerReadPort(true);
                CompareA.Variable.SetValue(CompareA.GetPortValueSafe);
                CompareB.TriggerReadPort(true);
                CompareB.Variable.SetValue(CompareB.GetPortValueSafe);
                bool conditionsMet = FieldVariable.LogicComparison(CompareA.Variable, CompareB.Variable, LogicType);

                if (_EditorDebugMode) UnityEngine.Debug.Log(c + " checking grid cells:" + checkers[c].CheckerReference.ChildPositionsCount + "  ::  a = " + CompareA.GetPortValueSafe + " vs b = " + CompareB.GetPortValueSafe);

                if (conditionsMet)
                {
                    choosen = checkers[c].CheckerReference;
                    if (breakMode) break;
                    CallOtherExecutionWithConnector(-1, null);
                }
            }

            if (choosen == null) return;
            ChoosenField.Output_Provide_Checker(choosen);
            //UnityEngine.Debug.Log("choosen = " + choosen.ChildPositionsCount);
        }



#if UNITY_EDITOR
        SerializedProperty sp = null;
        SerializedProperty spLog = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            FieldsToCheck.Editor_DefaultValueInfo = "";

            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("CompareA");
            if (spLog == null) spLog = baseSerializedObject.FindProperty("LogicType");
            SerializedProperty s = sp.Copy();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(s, GUIContent.none, GUILayout.Width(32));
            EditorGUILayout.PropertyField(spLog, GUIContent.none, GUILayout.Width(70));
            GUILayout.Space(2);
            EditorGUILayout.LabelField("B ?", GUILayout.Width(24));
            GUILayout.EndHorizontal();
            s.Next(false);
            //GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(s);//, GUILayout.Width(54));
            //s.Next(false);
            //EditorGUILayout.PropertyField(s, GUILayout.Width(NodeSize.x - 80));
            //GUILayout.EndHorizontal();
            s.Next(false); s.Next(false);
            EditorGUILayout.PropertyField(s);

            if (_EditorFoldout)
            {
                s.Next(false);
                EditorGUILayout.PropertyField(s);
            }

            baseSerializedObject.ApplyModifiedProperties();
        }

#endif


    }
}