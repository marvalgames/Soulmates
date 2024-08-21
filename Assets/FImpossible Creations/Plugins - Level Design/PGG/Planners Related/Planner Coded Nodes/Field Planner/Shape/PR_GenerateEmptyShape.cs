using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_GenerateEmptyShape : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Generate Empty Shape" : "Generate Empty Shape"; }
        public override string GetNodeTooltipDescription { get { return "Can be used as list of cells.\nExeute to generate new instance (helpful for iterative generating)"; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override bool IsFoldable { get { return false; } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override Vector2 NodeSize { get { return new Vector2(210, _EditorFoldout ? 106 : 84); } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }
        [Port(EPortPinType.Output)] public PGGPlannerPort ShapeField;

        FieldPlanner latelyServedPlanner = null;
        CheckerField3D theInstance = null;

        public override void OnCustomPrepare()
        {
            latelyServedPlanner = null;
            ResetPlannerPort();
        }

        void ResetPlannerPort()
        {
            ShapeField.Switch_DisconnectedReturnsByID = false;
            ShapeField.Switch_MinusOneReturnsMainField = false;
            ShapeField.Switch_ReturnOnlyCheckers = true;
            ShapeField.Editor_DefaultValueInfo = "(New)";
        }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            latelyServedPlanner = null;
            ResetPlannerPort();
        }

        public override void OnCreated()
        {
            ResetPlannerPort();
            base.OnCreated();
        }


        public override void OnStartReadingNode()
        {
            if (CurrentExecutingPlanner == null) return;

            if (latelyServedPlanner != CurrentExecutingPlanner)
            {
                latelyServedPlanner = CurrentExecutingPlanner;
                theInstance = new CheckerField3D();

                theInstance.CopyParamsFrom(CurrentExecutingPlanner.LatestChecker);
                ShapeField.Output_Provide_Checker(theInstance);

                if (_EditorDebugMode) UnityEngine.Debug.Log("Generating New Shape");
            }
            else
            {
                ShapeField.Output_Provide_Checker(theInstance);
            }
        }

        //#if UNITY_EDITOR

        //        private UnityEditor.SerializedProperty sp = null;
        //        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        //        {
        //            base.Editor_OnNodeBodyGUI(setup);

        //            if (_EditorFoldout)
        //            {
        //                baseSerializedObject.Update();
        //                if (sp == null) sp = baseSerializedObject.FindProperty("SetFieldSetup");
        //                EditorGUILayout.PropertyField(sp);
        //                baseSerializedObject.ApplyModifiedProperties();
        //            }

        //            ToAdd.DefaultValueIsNumberedID = false;
        //        }

        //#endif

    }
}