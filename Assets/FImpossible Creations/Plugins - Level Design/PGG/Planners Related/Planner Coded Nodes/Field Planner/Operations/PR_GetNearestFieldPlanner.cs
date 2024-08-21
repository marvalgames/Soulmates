using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Operations
{

    public class PR_GetNearestFieldPlanner : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return (wasCreated && IsFoldable) ? "   Nearest Field Planner" : "Nearest Field Planner"; }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(250, _EditorFoldout ? 192 : 140); } }
        public override bool IsFoldable { get { return Measure == EMeasureMode.ByNearestCell; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Tooltip("Measure mode for finding nearest field")]
        public EMeasureMode Measure = EMeasureMode.ByNearestCell;

        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default)] public PGGStringPort Tagged;
        [HideInInspector][Port(EPortPinType.Output, true)] public PGGPlannerPort Planner;

        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort NearestFrom;
        [HideInInspector][Port(EPortPinType.Input, 0)] public PGGPlannerPort ChooseFrom;
        [HideInInspector]
        [Port(EPortPinType.Output)]
        [Tooltip("Nearest cell of current field")]
        public PGGCellPort MyNearestCell;
        [HideInInspector]
        [Port(EPortPinType.Output)]
        [Tooltip("Nearest contact cell of the nearest found field")]
        public PGGCellPort OtherNearestCell;


        [Tooltip("!Right Side INPUT Port! Custom Condition Port, use it if you want to prevent some fields from being considered in the search")]
        [HideInInspector][Port(EPortPinType.Input, true)] public BoolPort CustomCondition;

        public enum EMeasureMode
        {
            [Tooltip("'By Nearest Cell' mode is most performance heavy!")]
            ByNearestCell, ByOrigin, ByBoundsCenter
        }


        private FieldCell latestNearestCell = null;
        private FieldCell latestOtherNearestCell = null;
        private FieldCell targetNearestCell = null;
        private FieldCell targetOtherNearestCell = null;

        public override void OnCustomPrepare()
        {
            isSearching = false;
        }

        bool isSearching = false;
        public override void OnStartReadingNode()
        {
            if (isSearching) { return; } // Prevent Stack Overflow when using custom condition looped connections

            isSearching = false;
            if (CurrentExecutingPlanner == null) return;
            ChooseFrom.Editor_DefaultValueInfo = "(all)";

            Planner.Clear();
            latestNearestCell = null;
            MyNearestCell.Clear();
            OtherNearestCell.Clear();
            NearestFrom.TriggerReadPort(true);

            FieldPlanner myPlanner = GetPlannerFromPort(NearestFrom, false);

            if (myPlanner == null) myPlanner = CurrentExecutingPlanner;
            CheckerField3D myChecker = NearestFrom.GetInputCheckerSafe;

            if (myPlanner)
                if (myPlanner.LatestResult != null)
                    myChecker = myPlanner.LatestResult.Checker;

            if (myChecker == null) { return; }

            if (myChecker.AllCells.Count == 0)
            {
                if (Debugging) DebuggingInfo = "!!! Trying to find cells of empty checker! (no cells)";
                return;
            }

            if (myPlanner == null) return;

            BuildPlannerPreset planner = myPlanner.ParentBuildPlanner;
            if (planner == null) return;

            if (planner.BasePlanners.Count == 0) return;


            isSearching = true;

            ChooseFrom.TriggerReadPort(true);
            System.Collections.Generic.List<FieldPlanner> planners;

            if ( ChooseFrom.IsConnected == false && ChooseFrom.UniquePlannerID < 0)
            {
                planners = planner.CollectAllAvailablePlanners(true, true);
            }
            else
            {
                planners = GetPlannersFromPort(ChooseFrom, false, false);
            }

            planners.Remove(myPlanner);


            for (int p = planners.Count - 1; p >= 0; p--)
            {
                if (planners[p].LatestChecker.AllCells.Count == 0) planners.RemoveAt(p);
                //planners[p].LatestChecker.DebugLogDrawCellsInWorldSpace(Color.blue);
            }

            //System.Collections.Generic.List<FieldPlanner> planners = planner.CollectAllAvailablePlanners(true, true);

            string tagged = Tagged.GetInputValue;

            FieldPlanner nearest = null;
            float nearestDist = float.MaxValue;

            if (string.IsNullOrEmpty(tagged)) // All available fields
            {

                for (int i = 0; i < planners.Count; i++)
                {
                    FieldPlanner plan = planners[i];

                    if (plan == myPlanner)
                    {
                        continue;
                    }

                    float dist = MeasureDistance(myChecker, plan.LatestChecker);

                    if (dist < nearestDist)
                    {
                        if (CustomConditionCheck(plan) == false) continue;

                        targetNearestCell = latestNearestCell;
                        targetOtherNearestCell = latestOtherNearestCell;
                        nearestDist = dist;
                        nearest = plan;
                    }
                }
            }
            else // Just fields with certain tags
            {
                for (int i = 0; i < planners.Count; i++)
                {
                    FieldPlanner plan = planners[i];
                    if (plan == myPlanner) continue;
                    if (plan.IsTagged(tagged) == false) continue;

                    float dist = MeasureDistance(myChecker, plan.LatestChecker);
                    if (dist < nearestDist)
                    {
                        if (CustomConditionCheck(plan) == false) continue;

                        targetNearestCell = latestNearestCell;
                        targetOtherNearestCell = latestOtherNearestCell;
                        nearestDist = dist;
                        nearest = plan;
                    }
                }
            }

            Planner.Output_Provide_Planner(null);

            if (nearest != null)
            {
                if (Measure == EMeasureMode.ByNearestCell)
                {
                    //myChecker.CellsFieldParent = myPlanner;
                    MyNearestCell.ProvideFullCellData(targetNearestCell, myChecker, myPlanner.LatestResult);

                    //nearest.LatestChecker.CellsFieldParent = nearest;
                    OtherNearestCell.ProvideFullCellData(targetOtherNearestCell, nearest.LatestChecker, nearest.LatestResult);
                }

                Planner.Output_Provide_Planner(nearest);
            }

            isSearching = false;
        }


        // Prevent Stack Overflow on looped read during search with custom condition
        //public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        //{
        //    if ( port == Planner)
        //    {
        //        if (isSearching) { UnityEngine.Debug.Log("SEARCHING");return; }
        //    }

        //    base.DONT_USE_IT_YET_OnReadPort(port);
        //}


        bool CustomConditionCheck(FieldPlanner plan)
        {
            if (plan == null) return false;

            if (CustomCondition.IsConnected)
            {
                Planner.Output_Provide_Planner(plan);
                CustomCondition.TriggerReadPort(true);
                if (CustomCondition.GetInputValue == false) return false;
            }

            return true;
        }

        float MeasureDistance(CheckerField3D from, CheckerField3D to)
        {
            if (Measure == EMeasureMode.ByNearestCell)
            {
                FieldCell nearest = from.GetNearestCellTo(to, false);
                latestNearestCell = nearest;

                FieldCell otherNearest = from._nearestCellOtherField;
                latestOtherNearestCell = otherNearest;

                if (FGenerators.NotNull(nearest) && FGenerators.NotNull(otherNearest))
                {
                    return Vector3.Distance(from.GetWorldPos(nearest), to.GetWorldPos(otherNearest));
                }
            }
            else if (Measure == EMeasureMode.ByOrigin)
            {
                return Vector3.Distance(from.RootPosition, to.RootPosition);
            }
            else if (Measure == EMeasureMode.ByBoundsCenter)
            {
                //UnityEngine.Debug.DrawLine(from.GetFullBoundsWorldSpace().center, to.GetFullBoundsWorldSpace().center, Color.green, 1.01f);
                return Vector3.Distance(from.GetFullBoundsWorldSpace().center, to.GetFullBoundsWorldSpace().center);
            }

            return float.MaxValue;
        }

#if UNITY_EDITOR
        SerializedProperty sp = null;
        SerializedProperty spCust = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("Tagged");

            SerializedProperty s = sp.Copy();
            EditorGUILayout.PropertyField(s, GUILayout.Width(NodeSize.x - 89));

            GUILayout.Space(-20);
            s.Next(false); EditorGUILayout.PropertyField(s);
            s.Next(false); EditorGUILayout.PropertyField(s, GUILayout.Width(NodeSize.x - 76));

            if (spCust == null)
            {
                spCust = baseSerializedObject.FindProperty("CustomCondition");
                //spCust.tooltip = "Custom Condition Port, use it if you want to prevent some fields from being considered in the search";
            }

            GUILayout.Space(-18);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(NodeSize.x - 72);
            EditorGUILayout.PropertyField(spCust, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            s.Next(false); EditorGUILayout.PropertyField(s);


            if (Measure == EMeasureMode.ByNearestCell)
            {
                if (_EditorFoldout)
                {
                    s.Next(false); EditorGUILayout.PropertyField(s);
                    s.Next(false); EditorGUILayout.PropertyField(s);
                }
            }
            else
            {
                _EditorFoldout = false;
            }

            baseSerializedObject.ApplyModifiedProperties();
            if (ChooseFrom.IsConnected == false)
            {
                GUILayout.Space(-1);
                EditorGUILayout.HelpBox("Choose From All Fields", MessageType.None);
            }
        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            GUILayout.Label("Found Planner Index: [" + Planner.GetPlannerIndex() + "] [" + Planner.GetPlannerDuplicateIndex() + "]");
        }
#endif

    }
}