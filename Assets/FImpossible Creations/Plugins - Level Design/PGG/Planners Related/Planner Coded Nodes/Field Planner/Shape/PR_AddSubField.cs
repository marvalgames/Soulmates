using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_AddSubField : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Add Sub Field" : "Add Sub Field Planner"; }
        public override string GetNodeTooltipDescription { get { return "Adding extra field (separated grid) to the Field Planner instance."; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override bool IsFoldable { get { return true; } }

        public override Vector2 NodeSize { get { return new Vector2(210, _EditorFoldout ? 106 : 86); } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToAdd;

        [Tooltip("If sub-field should use different field setup than main Field Planner instance you can provide FieldSetup type object here")]
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGUniversalPort SetFieldSetup;

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            if (ToAdd.IsNotConnected) return;

            ToAdd.TriggerReadPort(true);
            var oChe = ToAdd.Get_GetMultipleCheckers;
            if (oChe == null) { return; }
            if (oChe.Count == 0) { return; }

            var myPlan = CurrentExecutingPlanner;
            if (myPlan == null) return;

            SetFieldSetup.TriggerReadPort(true);
            FieldSetup setp = SetFieldSetup.GetPortValue as FieldSetup;

            for (int o = 0; o < oChe.Count; o++)
            {
                var sub = myPlan.AddSubField(oChe[o].CheckerReference);
                if (sub == null) continue;
                sub.DefaultFieldSetup = setp;
            }


            #region Debugging Gizmos
#if UNITY_EDITOR
            if (Debugging)
            {
                DebuggingInfo = "Adding Sub Field";
                CheckerField3D myChec = myPlan.LatestChecker;
                CheckerField3D oChec = null;
                if (oChe.Count > 0) oChec = oChe[0].CheckerReference;

                DebuggingGizmoEvent = () =>
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                    for (int i = 0; i < myChec.ChildPositionsCount; i++)
                        Gizmos.DrawCube(myChec.GetWorldPos(i), myChec.RootScale);

                    if (oChec != null)
                    {
                        Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                        for (int i = 0; i < oChec.ChildPositionsCount; i++)
                            Gizmos.DrawCube(oChec.GetWorldPos(i), oChec.RootScale);
                    }
                };
            }
#endif
            #endregion

        }

#if UNITY_EDITOR

        private UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                baseSerializedObject.Update();
                if (sp == null) sp = baseSerializedObject.FindProperty("SetFieldSetup");
                EditorGUILayout.PropertyField(sp);
                baseSerializedObject.ApplyModifiedProperties();
            }

            ToAdd.Switch_DisconnectedReturnsByID = false;
        }

        //public override void Editor_OnAdditionalInspectorGUI()
        //{
        //    EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
        //    CheckerField3D chA = ApplyTo.GetInputCheckerSafe;
        //    if (chA != null) GUILayout.Label("Planner Cells: " + chA.ChildPositionsCount);

        //    CheckerField3D chB = JoinWith.GetInputCheckerSafe;
        //    if (chB != null) GUILayout.Label("JoinWith Cells: " + chB.ChildPositionsCount);
        //}

#endif

    }
}