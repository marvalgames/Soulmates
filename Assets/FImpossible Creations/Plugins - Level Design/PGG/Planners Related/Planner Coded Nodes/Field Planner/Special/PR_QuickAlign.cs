using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Special
{

    public class PR_QuickAlign : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Quick Align" : "Quick Align"; }
        public override string GetNodeTooltipDescription { get { return "Putting Field in align field position, then Pushing it out in 4 directions and choosing push result with smallest push distance."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }
        public override Vector2 NodeSize { get { return new Vector2(222, 145); } }
        public override bool IsFoldable { get { return false; } }
        public override Color GetNodeColor() { return new Color(0.1f, 0.7f, 1f, 0.95f); }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToPush;
        [Tooltip("If inputing multiple planners, random to align will be chosen")]
        [Port(EPortPinType.Input)] public PGGPlannerPort AlignTo;
        [Space(3)]
        [Tooltip("If 'Collision With' left empty or -1 then colliding with every field in the current plan stage")]
        [Port(EPortPinType.Input)] public PGGPlannerPort CollisionWith;
        [Port(EPortPinType.Input, 1)] public FloatPort ExtraPushOut;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner planner = GetPlannerFromPort(ToPush);
            if (planner == null) return;

            FieldPlanner alignTo = AlignTo.GetPlannerFromPort(true);

            var aligns = AlignTo.Get_GetMultipleFields;

            if (aligns.Count > 1)
            {
                aligns.Remove(planner);
                alignTo = aligns[FGenerators.GetRandom(0, aligns.Count)];
            }

            if (alignTo == null) return;

            planner.LatestChecker._IsCollidingWith_MyFirstCollisionCell = null;

            bool collideWithAll = false;
            if (CollisionWith.PortState() != EPortPinState.Connected)
                if (CollisionWith.UniquePlannerID < 0)
                    collideWithAll = true;

            float pushOutDistance = ExtraPushOut.GetInputValue;

            List<ICheckerReference> collisionCheckers;

            if (collideWithAll)
            {
                collisionCheckers = ParentPlanner.ParentBuildPlanner.CollectAllAvailablePlannersCheckerRefs(true, true, true, true);
            }
            else
            {
                collisionCheckers = CollisionWith.Get_GetMultipleCheckers;
            }

            collisionCheckers.Remove(planner.LatestChecker);


            var checker = planner.LatestChecker;
            var alignToChecker = alignTo.LatestChecker;

            Vector3 initialPosition = checker.RootPosition;
            Quaternion initialRotation = checker.RootRotation;

            checker.RootPosition = alignToChecker.RootPosition;
            checker.RootRotation = initialRotation;

            Vector3 nearestPos = new Vector3(float.MaxValue, 0, 0);
            Vector3Int nearestPosDir = Vector3Int.zero;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                checker.RootPosition = alignToChecker.RootPosition;

                Vector3Int dir = new Vector3Int(0,0,1);
                if (i == 0) dir = Vector3Int.right; else if (i == 1) dir = Vector3Int.left; else if (i == 2) dir = new Vector3Int(0,0,1); else if (i == 3) dir = new Vector3Int(0,0,-1);
                checker.StepPushOutOfCollision(collisionCheckers, dir, 256, false);
                float dist = Vector3.Distance(checker.RootPosition, alignToChecker.RootPosition);

                if (dist != 0f)
                    if (dist < nearestDist) { nearestPosDir = dir; nearestDist = dist; nearestPos = checker.RootPosition; }
            }

            if (nearestPos.x != float.MaxValue)
            {
                planner.LatestChecker.RootPosition = nearestPos + checker.ScaleV3(nearestPosDir.V3IntToV3()) * pushOutDistance;
            }
            else
            {
                planner.LatestChecker.RootPosition = initialPosition;
            }

        }


        #region Editor Related

#if UNITY_EDITOR
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            CollisionWith.Editor_DefaultValueInfo = "(All)";
            base.Editor_OnNodeBodyGUI(setup);
        }
#endif

        #endregion


    }
}