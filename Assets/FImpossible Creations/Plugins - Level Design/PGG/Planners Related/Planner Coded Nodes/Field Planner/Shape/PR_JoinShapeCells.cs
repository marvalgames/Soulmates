using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_JoinShapeCells : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "   Join Field-Shape Cells" : "Join Field-Shape Cells"; }
        public override string GetNodeTooltipDescription { get { return "Joining cells of one Field Shape with another."; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override bool IsFoldable { get { return true; } }

        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 230 : 210, (_EditorFoldout ? 124 : 86)); } }

        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort JoinWith;
        [HideInInspector][Port(EPortPinType.Output, true)] public PGGPlannerPort JoinWithContinue;

        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort ApplyTo;

        [Tooltip("Aligning center of joined shape with field with which shape is joined")]
        [HideInInspector] public bool AlignWithTargetField = false;
        //[Tooltip("Discarding joined planner, useful when joining sub-fields towards defined field planners")]
        //[HideInInspector] public bool DiscardJoined = false;


        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            ApplyTo.TriggerReadPort(true);
            JoinWith.TriggerReadPort(true);

            JoinWithContinue.Clear();
            JoinWithContinue.Switch_MinusOneReturnsMainField = false;

            FieldPlanner plan = GetPlannerFromPort(ApplyTo, false);

            CheckerField3D myChe = ApplyTo.GetInputCheckerSafe;
            if (plan) myChe = plan.LatestResult.Checker;

            if (myChe == null) { return; }

            PGGCellPort cellPort = JoinWith.IsConnectedJustWithCellPort();
            if (cellPort != null)
            {
                var oChe = cellPort.GetSelectiveCellChecker();
                if (oChe == null) { return; }
                myChe.Join(oChe);
                return;
            }

            List<ICheckerReference> checkers = JoinWith.Get_GetMultipleCheckers;
            if (checkers.Count == 0) return;

            for (int c = 0; c < checkers.Count; c++)
            {
                ICheckerReference chec = checkers[c];
                if (AlignWithTargetField) chec.CheckerReference.RootPosition = myChe.RootPosition;

                if (_EditorDebugMode)
                {
                    UnityEngine.Debug.Log("Joining with " + chec.CheckerReference.ChildPositionsCount + " apply to = " + plan.ArrayNameString);
                    chec.CheckerReference.DebugLogDrawCellsInWorldSpace(Color.red);
                }

                myChe.Join(chec.CheckerReference);
            }

            if (plan) plan.LatestResult.Checker = myChe;

            //JoinWithContinue.CopyValuesOfOtherPort(JoinWith);

            //if ( DiscardJoined)
            //{
            //    FieldPlanner joinPlan = GetPlannerFromPort(JoinWith, false);
            //    if (joinPlan) joinPlan.Discard(print);
            //}

            #region Debugging Gizmos
#if UNITY_EDITOR
            if (Debugging)
            {
                DebuggingInfo = "Joining fields cells";
                CheckerField3D myChec = myChe.Copy(false);
                CheckerField3D oChec = checkers[0].CheckerReference.Copy(false);

                DebuggingGizmoEvent = () =>
                {
                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                    myChec.DrawFieldGizmos(true, false);
                    //for (int i = 0; i < myChec.ChildPositionsCount; i++)
                    //    Gizmos.DrawCube(myChec.GetWorldPos(i), myChe.RootScale);
                    Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
                    oChec.DrawFieldGizmos(true, false);
                    //for (int i = 0; i < oChec.ChildPositionsCount; i++)
                    //    Gizmos.DrawCube(oChec.GetWorldPos(i), oChec.RootScale);
                };
            }
#endif
            #endregion

        }

#if UNITY_EDITOR

        private UnityEditor.SerializedProperty sp = null;
        private UnityEditor.SerializedProperty sp_JoinWith = null;
        //private UnityEditor.SerializedProperty sp_JoinWithContinue = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            baseSerializedObject.Update();

            if (sp_JoinWith == null) sp_JoinWith = baseSerializedObject.FindProperty("JoinWith");
            SerializedProperty spc = sp_JoinWith.Copy();

            EditorGUIUtility.labelWidth = 1;
            EditorGUILayout.PropertyField(spc, GUILayout.Width(NodeSize.x - 80));
            EditorGUIUtility.labelWidth = 0;

            /*GUILayout.Space(-19);*/ spc.Next(false);
            //EditorGUILayout.PropertyField(spc);


            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                ApplyTo.AllowDragWire = true;
                GUILayout.Space(1);

                if (sp == null) sp = baseSerializedObject.FindProperty("ApplyTo");
                UnityEditor.SerializedProperty scp = sp.Copy();
                UnityEditor.EditorGUILayout.PropertyField(scp);
                scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
                //scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
            }
            else
            {
                ApplyTo.AllowDragWire = false;
            }

            baseSerializedObject.ApplyModifiedProperties();
        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            CheckerField3D chA = ApplyTo.GetInputCheckerSafe;
            if (chA != null) GUILayout.Label("Planner Cells: " + chA.ChildPositionsCount);

            CheckerField3D chB = JoinWith.GetInputCheckerSafe;
            if (chB != null) GUILayout.Label("JoinWith Cells: " + chB.ChildPositionsCount);
        }

#endif

    }
}