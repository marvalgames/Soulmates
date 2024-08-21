using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Community
{

    public class PR_RemoveCellsUnder : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Remove Cells Under" : "Remove Cells Under"; }
        public override bool IsFoldable { get { return false; } }
        public override string GetNodeTooltipDescription { get { return "Cutting cells shape in other fields (removing cells) in provided direction"; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(246, 142); } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }


        [Tooltip("Shape to be cutted out of the 'Planner' shape")]
        [Port(EPortPinType.Input, 1)] public PGGPlannerPort CuttingShape;
        [Port(EPortPinType.Input, 1)] public PGGVector3Port Direction;
        [Port(EPortPinType.Input, 1)] public PGGPlannerPort RemoveFrom;
        [Port(EPortPinType.Input, 1)] public IntPort MaxSteps;

        public override void OnCreated()
        {
            Direction.Value = Vector3.down;
            MaxSteps.Value = 1;
            base.OnCreated();
        }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            CuttingShape.Switch_MinusOneReturnsMainField = true;
            RemoveFrom.TriggerReadPort(true);
            Direction.TriggerReadPort(true);
            CuttingShape.TriggerReadPort(true);
            MaxSteps.TriggerReadPort(true);

            CheckerField3D myChe = RemoveFrom.GetInputCheckerSafe;
            if (myChe == null) { return; }

            List<ICheckerReference> checkers;

            if (RemoveFrom.IsConnected == false) // Not connected = get all checkers
            {
                var bp = ParentPlanner.ParentBuildPlanner;
                checkers = new List<ICheckerReference>();
                foreach (var item in bp.CollectAllAvailablePlannersCheckers(true, true)) checkers.Add(item);
                checkers.Remove(myChe); // Skip self
            }
            else
            {
                checkers = RemoveFrom.Get_GetMultipleCheckers;
            }

            Vector3 removalDirection = Direction.GetInputValue.normalized;

            CheckerField3D cutterChecker = new CheckerField3D();
            cutterChecker.CopyParamsFrom(myChe);
            cutterChecker.Join(myChe);

            if (MaxSteps.Value < 1) MaxSteps.Value = 1;
            int steps = MaxSteps.GetInputValue;

            Bounds sweepBounds = cutterChecker.GetFullBoundsWorldSpace();
            sweepBounds.size *= 0.99f;
            sweepBounds = CheckerField3D.ScaleBoundsForSweep(sweepBounds, Direction.GetInputValue, Mathf.RoundToInt( steps * myChe.ScaleV3(removalDirection).magnitude) );
            if (_EditorDebugMode) FDebug.DrawBounds3D(sweepBounds, Color.magenta); // In case for debug


            for (int i = 0; i < checkers.Count; i++)
            {
                var removeFrom = checkers[i].CheckerReference;
                Bounds oB = removeFrom.GetFullBoundsWorldSpace();
                if (oB.size == Vector3.zero) continue;

                if (oB.Intersects(sweepBounds))
                {
                    // Collision possible -> proceed cells removal over all possible units in provided axe
                    cutterChecker.RootPosition = myChe.RootPosition;

                    for (int s = 0; s <= steps; s++) // TODO: Cells scale density aware steps
                    {
                        cutterChecker.RootPosition = myChe.RootPosition + myChe.ScaleV3(removalDirection) * s;
                        removeFrom.RemoveCellsCollidingWith(cutterChecker);
                    }
                }
            }
        }



        #region Editor Code

#if UNITY_EDITOR

        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            if (MaxSteps.Value < 1) MaxSteps.Value = 1;
            RemoveFrom.Editor_DefaultValueInfo = "(All)";
            base.Editor_OnNodeBodyGUI(setup);
        }

#endif

        #endregion

    }
}