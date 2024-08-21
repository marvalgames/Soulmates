using FIMSpace.Graph;
using System;
using UnityEngine;
using FIMSpace.Generating.Checker;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_SplitField : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Split Field"; }
        public override string GetNodeTooltipDescription { get { return "Splitting each rectangle area of the field into two pieces. If it's rectangle, result will be two pieces. If it's not a rectangle, result will contain more pieces."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(216, 101); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToSplit;
        [Port(EPortPinType.Output)] public PGGPlannerPort ResultingSplits;

        public enum ESplitMode
        {
            Random, AlwaysHorizontalSlice, AlwaysVerticalSlice
        }

        public override void OnCreated()
        {
            base.OnCreated();
            ResultingSplits.Switch_ReturnOnlyCheckers = true;
            ToSplit.Switch_ReturnOnlyCheckers = false;
        }

        public override void OnStartReadingNode()
        {
            ResultingSplits.Clear();

            ToSplit.TriggerReadPort(true);
            ResultingSplits.Switch_ReturnOnlyCheckers = true;

            FieldPlanner plan = GetPlannerFromPort(ToSplit, false);
            ToSplit.Switch_ReturnOnlyCheckers = false;

            CheckerField3D myChe = null;

            if (plan!=null) if (plan.LatestChecker != null) myChe = plan.LatestChecker;
            if (myChe == null) myChe = ToSplit.GetInputCheckerSafe;
            if (myChe == null) { return; }

            myChe = myChe.Copy();
            var splits = CheckerField3D.SplitChecker(myChe, 1);

            List<CheckerField3D> chSplits = new List<CheckerField3D>();

            for (int s = 0; s < splits.Count; s++)
            {
                chSplits.Add(splits[s].ToCheckerField(plan, true));
            }

            ResultingSplits.Output_Provide_CheckerList(chSplits);

            //            #region Debugging Gizmos
            //#if UNITY_EDITOR
            //            if (Debugging)
            //            {
            //                DebuggingInfo = "Splitting Field";
            //                CheckerField3D myChec = myChe.Copy(false);

            //                DebuggingGizmoEvent = () =>
            //                {
            //                    Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            //                    myChec.DrawFieldGizmos(true, false);
            //                    for (int i = 0; i < myChec.ChildPositionsCount; i++)
            //                        Gizmos.DrawCube(myChec.GetWorldPos(i), myChe.RootScale);
            //                };
            //            }
            //#endif
            //            #endregion

        }


#if UNITY_EDITOR
        //SerializedProperty sp = null;
        //public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        //{
        //    base.Editor_OnNodeBodyGUI(setup);

        //    if (sp == null) sp = baseSerializedObject.FindProperty("OutVal");

        //    GUILayout.Space(-21);
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Space(19);
        //    EditorGUILayout.PropertyField(sp, GUIContent.none);
        //    GUILayout.EndHorizontal();
        //}


#endif
    }
}