using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Checker;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetNeightbourField : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Get Neightbour Field" : "Get Neightbour Field"; }
        public override string GetNodeTooltipDescription { get { return "Getting one of the nearest fields from the provided list o fields"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(210, 124); } }
        public override bool IsFoldable { get { return false; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input)] public PGGPlannerPort SearchIn;
        [Port(EPortPinType.Output)] public PGGPlannerPort Selected;

        [Tooltip("Connect if you want to find neightbour of some other node than currently executed one")]
        [Port(EPortPinType.Input)] public PGGPlannerPort NeightbourOf;

        //[HideInInspector][Port(EPortPinType.Input, EPortValueDisplay.NotEditable, 1)] public BoolPort SkipCondition;
        //[HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGPlannerPort Checked;

        public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        {
            if (port != Selected) return;

            Selected.Clear();
            Selected.Switch_MinusOneReturnsMainField = false;
            SearchIn.TriggerReadPort(true);

            List<FieldPlanner> checkMask = null;

            if (SearchIn.IsConnected == false && SearchIn.UniquePlannerID < 0)
            {
                checkMask = null;
                FieldPlanner cplan = FieldPlanner.CurrentGraphExecutingPlanner;
                if (cplan != null)
                {
                    BuildPlannerPreset build = cplan.ParentBuildPlanner;
                    if (build != null) checkMask = build.CollectAllAvailablePlanners();
                }
            }
            else
                checkMask = GetPlannersFromPort(SearchIn, false, false);

            if (checkMask == null || checkMask.Count == 0)
            {
                //UnityEngine.Debug.Log("no checkers!");
                return;
            }

            NeightbourOf.TriggerReadPort(true);
            FieldPlanner neightbourOf = GetPlannerFromPort(NeightbourOf, false);

            if (neightbourOf == null) { if (_EditorDebugMode) UnityEngine.Debug.Log("no neightbour checker"); return; }

            CheckerField3D inlineShape = neightbourOf.LatestChecker.GetInlineChecker(false, true, true);
            inlineShape.AllCells.Shuffle();

            for (int i = 0; i < inlineShape.AllCells.Count; i++)
            {
                var cell = inlineShape.AllCells[i];
                Vector3 wPos = neightbourOf.LatestChecker.LocalToWorld(cell.Pos + FVectorMethods.ChooseDominantAxis(cell.HelperVector));

                for (int c = 0; c < checkMask.Count; c++)
                {
                    if (checkMask[c] == neightbourOf) continue;

                    if (checkMask[c].LatestChecker.ContainsWorld(wPos))
                    {
                        //UnityEngine.Debug.DrawLine(wPos, neightbourOf.LatestChecker.LocalToWorld(cell.Pos), Color.green, 1.01f);
                        Selected.Output_Provide_Planner(checkMask[c]);
                        return;
                    }
                }
            }
        }


#if UNITY_EDITOR
        //SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            Selected.Editor_DefaultValueInfo = "(None)";
            SearchIn.Editor_DefaultValueInfo = "(All)";
            base.Editor_OnNodeBodyGUI(setup);

            //Iteration.AllowDragWire = false;
            //if (_EditorFoldout)
            //{
            //    if (sp == null) sp = baseSerializedObject.FindProperty("Planner");
            //    EditorGUILayout.PropertyField(sp, true);

            //    if (DrawInstInd)
            //    {
            //        SerializedProperty spc = sp.Copy();
            //        spc.Next(false);
            //        EditorGUILayout.PropertyField(spc);
            //        Iteration.AllowDragWire = true;
            //    }
            //}

        }

#endif

    }
}