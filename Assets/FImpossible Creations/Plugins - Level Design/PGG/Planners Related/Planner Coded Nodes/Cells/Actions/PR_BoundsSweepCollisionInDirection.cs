using FIMSpace.Graph;
using UnityEngine;
using FIMSpace.Generating.Checker;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{

    public class PR_BoundsSweepCollisionInDirection : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Bounds Sweep Collision" : "Check Bounds Sweep Collision In Direction"; }
        public override string GetNodeTooltipDescription { get { return "Checking if there is collision with other field in choosed direction using other field's bounds as collision reference check."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }
        public override Color GetNodeColor() { return new Color(0.2f, 0.9f, 0.3f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(254, _EditorFoldout ? 201 : 141); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input, "Collision Of")] public PGGPlannerPort CheckCollisionOf;
        [Port(EPortPinType.Input)] public PGGVector3Port Direction;
        [Port(EPortPinType.Input)] public PGGPlannerPort CheckContactWith;

        [Tooltip("Returns null cell if no collision happened")]
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGCellPort FirstContact;
        [HideInInspector][Port(EPortPinType.Input)] public PGGStringPort SkipTagged;
        [HideInInspector][Port(EPortPinType.Input)] public IntPort CheckDistance;
        [HideInInspector] public bool SkipOverlaps = false;

        private FieldPlanner latestCaller = null;
        List<string> checkTags = new List<string>();

        public override void OnCreated()
        {
            base.OnCreated();
            CheckDistance.Value = 128;
        }

        public override void Prepare(PlanGenerationPrint print)
        {
            latestCaller = null;
            base.Prepare(print);
        }

        #region Execute if connected implementation

        public override void OnStartReadingNode()
        {
            base.OnStartReadingNode();

            if (CurrentExecutingPlanner == null) return;

            if (InputConnections.Count == 0)
            {
                if (FirstContact.IsConnected)
                {
                    Execute(null, CurrentExecutingPlanner.LatestResult);
                }
            }
        }

        #endregion

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FirstContact.Clear();

            // Call for contact mask only once for the planner
            if (latestCaller != newResult.ParentFieldPlanner)
            {
                CheckContactWith.TriggerReadPort(true);
                latestCaller = newResult.ParentFieldPlanner;
            }

            System.Collections.Generic.List<FieldPlanner> contactMask;

            if (CheckContactWith.IsConnected == false)
            {
                FieldPlanner cplan = FieldPlanner.CurrentGraphExecutingPlanner;
                if (cplan == null) return;
                BuildPlannerPreset build = cplan.ParentBuildPlanner;
                if (build == null) return;

                contactMask = build.CollectAllAvailablePlanners();
                //string info = "::: ";
                //for (int c = 0; c < contactMask.Count; c++) info += contactMask[c].ArrayNameString + "  |  ";
                //UnityEngine.Debug.Log(info);
            }
            else contactMask = GetPlannersFromPort(CheckContactWith, false, false);

            if (contactMask == null) { return; }
            if (contactMask.Count == 0) { return; }

            //CheckCollisionOf.TriggerReadPort(true);
            Bounds sweepBounds = new Bounds(Vector3.zero, Vector3.zero);

            FieldPlanner selfPlanner = GetPlannerFromPort(CheckCollisionOf, true);
            //UnityEngine.Debug.Log("check " + selfPlanner.ArrayNameString);
            CheckerField3D selfChecker = null;

            if (CheckCollisionOf.GetPortValueSafe is Bounds) sweepBounds = (Bounds)CheckCollisionOf.GetPortValueSafe;
            else if (CheckCollisionOf.GetPortValueSafe is CheckerField3D) selfChecker = CheckCollisionOf.GetPortValueSafe as CheckerField3D;
            else
            {
                if (selfPlanner == null)
                {
                    selfChecker = GetCheckerFromPort(CheckCollisionOf, false);
                    if (selfChecker == null) return;
                }
                else selfChecker = selfPlanner.LatestChecker;
            }

            if (selfChecker != null) sweepBounds = selfChecker.GetFullBoundsWorldSpace();

            if (sweepBounds.size == Vector3.zero) { return; }

            if (selfPlanner != null) contactMask.Remove(selfPlanner);

            Direction.TriggerReadPort(true);
            CheckDistance.TriggerReadPort(true);

            int dist = CheckDistance.GetInputValue;
            if (dist < 1) dist = 128;

            sweepBounds.size *= 0.99f;
            //Bounds selfBounds = sweepBounds; // Remember unsweeped bounds for extra check

            sweepBounds = CheckerField3D.ScaleBoundsForSweep(sweepBounds, Direction.GetInputValue, dist, true, SkipOverlaps);
            if (_EditorDebugMode) FDebug.DrawBounds3D(sweepBounds, Color.magenta);

            SkipTagged.TriggerReadPort(true);
            PGGStringPort.SplitTags(SkipTagged.GetInputValue, checkTags);

            for (int i = 0; i < contactMask.Count; i++)
            {
                Bounds oB = contactMask[i].LatestChecker.GetFullBoundsWorldSpace();
                if (oB.size == Vector3.zero) continue;

                bool skip = false;
                if (!string.IsNullOrWhiteSpace(contactMask[i].tag))
                    for (int s = 0; s < checkTags.Count; s++)
                    {
                        if (contactMask[i].IsTagged(checkTags[s]))
                        { skip = true; break; }
                    }

                if (skip) continue;

                if (oB.Intersects(sweepBounds))
                {
                    //if (selfChecker != null)
                    //    if (contactMask[i].LatestChecker.IsCollidingWith(selfChecker) == false)
                    //    {
                    //        // If it's just bounds collision but real cells are not colliding then
                    //        // We need to check sweep collision basing on the target side cells

                    //        continue;
                    //    }

                    //UnityEngine.Debug.Log(selfPlanner.ArrayNameString + " contacts with " + contactMask[i].ArrayNameString);
                    //contactMask[i].LatestChecker.DebugLogDrawCellInWorldSpace(contactMask[i].LatestChecker.GetCell(0), Color.red);
                    //UnityEngine.Debug.DrawLine(selfPlanner.LatestChecker.GetFullBoundsWorldSpace().center, contactMask[i].LatestChecker.GetFullBoundsWorldSpace().center, Color.green, 1.01f);
                    FirstContact.ProvideFullCellData(contactMask[i].LatestChecker.GetCell(0), contactMask[i].LatestChecker, newResult);
                    break;
                }
            }

        }


#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            if (CheckDistance.Value < 0) CheckDistance.Value = 0;
            CheckContactWith.Editor_DefaultValueInfo = "(All)";

            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                SkipTagged.AllowDragWire = true;
                baseSerializedObject.Update();

                if (sp == null) sp = baseSerializedObject.FindProperty("SkipTagged");
                EditorGUILayout.PropertyField(sp);
                var spc = sp.Copy(); spc.Next(false);
                EditorGUILayout.PropertyField(spc);
                spc.Next(false); EditorGUILayout.PropertyField(spc);

                baseSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                SkipTagged.AllowDragWire = false;
            }
        }

#endif

    }
}