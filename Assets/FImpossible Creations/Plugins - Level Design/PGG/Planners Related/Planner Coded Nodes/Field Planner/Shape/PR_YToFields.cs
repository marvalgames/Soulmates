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

    public class PR_YToFields : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Each Y Level To Shape" : "Convert Each Y Level To Field Shape"; }
        public override string GetNodeTooltipDescription { get { return "Splitting field Y levels into multiple shapes."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(216, 101); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToSplit;
        [Port(EPortPinType.Output)] public PGGPlannerPort ResultingSplits;

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



            if (plan != null) if (plan.LatestChecker != null) myChe = plan.LatestChecker;
            if (myChe == null) myChe = ToSplit.GetInputCheckerSafe;
            if (myChe == null) { return; }

            if (myChe.AllCells.Count < 1) return;

            int minY = myChe.Grid.GetMin().y;
            int maxY = myChe.Grid.GetMax().y;
            List<CheckerField3D> chLevels = new List<CheckerField3D>();

            for (int y = minY; y <= maxY; y++)
            {
                CheckerField3D sub = new CheckerField3D();
                sub.CopyParamsFrom(myChe);

                for (int a = 0; a < myChe.Grid.AllApprovedCells.Count; a++)
                {
                    var cell = myChe.Grid.AllApprovedCells[a];
                    if ( cell.Pos.y == y) sub.AddLocal(cell.Pos);
                }

                if (sub.ChildPositionsCount > 0) chLevels.Add(sub);
            }

            ResultingSplits.Output_Provide_CheckerList(chLevels);
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