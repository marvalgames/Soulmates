using FIMSpace.Graph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.CustomNodes
{
    public class BPNode_WindowsPlacer : PlannerRuleBase
    {

        #region Node Display Setup

        public override string GetDisplayName(float maxWidth = 120) { return "Windows Placer"; }
        public override string GetNodeTooltipDescription { get { return "Adding commands around the field"; } }
        public override Color GetNodeColor() { return new Color(0.4f, 0.72f, 0.7f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(276, 202); } }

        #endregion

        // Node Parameters
        [Port(EPortPinType.Input)] public PGGPlannerPort ExecuteOnField;
        [Port(EPortPinType.Input)] public IntPort WindowCommandID;
        [Range(0, 3)] public int Spacing = 1;
        [Range(0f, 1f)] public float X2X1Amount = 0f;
        public bool AvoidCorners = false;
        [Range(0, 2)] public int AllowOtherEdges = 0;
        [Range(0f, 1f)] public float SkipAmount = 0f;
        enum EEdge { Left = 2, Right = 4, Front = 8, Back = 16 }

        List<PlannedCommand> plannedCommands = new List<PlannedCommand>();

        /// <summary> [0]R [1]L [2]F [3]B </summary>
        List<List<bool>> planned2x1 = null;
        int max2x1 = 0;

        List<FieldCell> edgeR = null;
        List<FieldCell> edgeL = null;
        List<FieldCell> edgeF = null;
        List<FieldCell> edgeB = null;
        List<bool> skips = null;
        int iterationCount = 0;


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner planner = GetPlannerFromPort(ExecuteOnField);
            if (planner == null) return;
            if (planner != CurrentExecutingPlanner) newResult = planner.LatestResult;

            WindowCommandID.TriggerReadPort(true);
            int id = WindowCommandID.GetInputValue;

            // Getting commands of the same id already added on the field
            for (int i = 0; i < newResult.CellsInstructions.Count; i++)
            {
                if (newResult.CellsInstructions[i].Id == id)
                {
                    plannedCommands.Add(new PlannedCommand(newResult.CellsInstructions[i].pos, (newResult.CellsInstructions[i].rot * Vector3.forward).V3toV3Int(), true));
                }
            }

            // Prepare edges
            int edgeCells = 0;
            edgeR = planner.LatestChecker.GetCellsOnEdge(Vector3Int.right);
            edgeCells += edgeR.Count;
            edgeL = planner.LatestChecker.GetCellsOnEdge(Vector3Int.left);
            edgeCells += edgeL.Count;
            edgeF = planner.LatestChecker.GetCellsOnEdge(new Vector3Int(0,0,1));
            edgeCells += edgeF.Count;
            edgeB = planner.LatestChecker.GetCellsOnEdge(new Vector3Int(0,0,-1));
            edgeCells += edgeB.Count;

            #region 2x1 windows preparing

            if (X2X1Amount > 0f)
            {
                if (planned2x1 == null) planned2x1 = new List<List<bool>>();
                planned2x1.Clear();

                max2x1 = Mathf.FloorToInt(Mathf.Lerp(0, edgeCells / Mathf.Max(1, Spacing), X2X1Amount)) + 1;
                int toDistrib = max2x1;

                if (toDistrib > 0)
                {
                    for (int i = 0; i < 4; i++) planned2x1.Add(new List<bool>());
                    List<int> edgeShuffler = new List<int>() { 0, 1, 2, 3 };

                    int iters = 0;
                    while (toDistrib > 0)
                    {
                        edgeShuffler.Shuffle();

                        for (int i = 0; i < edgeShuffler.Count; i++)
                        {
                            if (planned2x1[edgeShuffler[i]].Count > GetEdge(edgeShuffler[i]).Count) continue;

                            planned2x1[edgeShuffler[i]].Add(true);
                            toDistrib -= 1;
                            if (toDistrib == 0) break;
                        }

                        iters += 1;
                        if (iters > edgeCells * 4) break; // Safety break
                    }

                    int s = planned2x1[0].Count;
                    for (int i = s; i < edgeR.Count; i++) planned2x1[0].Add(false);
                    s = planned2x1[1].Count; for (int i = s; i < edgeL.Count; i++) planned2x1[1].Add(false);
                    s = planned2x1[2].Count; for (int i = s; i < edgeF.Count; i++) planned2x1[2].Add(false);
                    s = planned2x1[3].Count; for (int i = s; i < edgeB.Count; i++) planned2x1[3].Add(false);

                    for (int i = 0; i < 4; i++) planned2x1[i].Shuffle();
                }


                #region Commented but can be helpful later

                //string report = "";
                //for (int i = 0; i < 4; i++)
                //{
                //    report += "\n{ " + i + " } : ";
                //    for (int a = 0; a < planned2x1[i].Count; a++)
                //    {
                //        report += "[" + a + "] = " + planned2x1[i][a] + "  ";
                //    }
                //}

                //UnityEngine.Debug.Log(report);

                #endregion
            }

            #endregion


            iterationCount = 0;
            if (SkipAmount > 0f)
            {
                if (skips == null) skips = new List<bool>();
                skips.Clear();
                for (int i = 0; i < edgeCells; i++) skips.Add(false);
                int skipCount = Mathf.Min(edgeCells, 1 + Mathf.RoundToInt(edgeCells * SkipAmount));
                for (int i = 0; i < skipCount; i++) skips[i] = true;
                skips.Shuffle();
            }


            ProceedPlacingWith(plannedCommands, edgeR, planner, Vector3Int.right, 0);
            ProceedPlacingWith(plannedCommands, edgeL, planner, Vector3Int.left, 1);
            ProceedPlacingWith(plannedCommands, edgeF, planner, new Vector3Int(0,0,1), 2);
            ProceedPlacingWith(plannedCommands, edgeB, planner, new Vector3Int(0,0,-1), 3);

            for (int p = 0; p < plannedCommands.Count; p++)
            {
                if (plannedCommands[p].WasByPlanner) continue;

                SpawnInstructionGuide instruction = new SpawnInstructionGuide();
                instruction.Id = id;
                instruction.pos = plannedCommands[p].Cell;
                instruction.UseDirection = true;
                instruction.WorldRot = true;
                instruction.rot = Quaternion.LookRotation(plannedCommands[p].TargetDirection);
                newResult.CellsInstructions.Add(instruction);
            }

            plannedCommands.Clear();
        }

        List<FieldCell> GetEdge(int id)
        {
            if (id == 0) return edgeR;
            else if (id == 1) return edgeL;
            else if (id == 2) return edgeF;
            else if (id == 3) return edgeB;
            return null;
        }

        void ProceedPlacingWith(List<PlannedCommand> planned, List<FieldCell> edgeCells, FieldPlanner planner, Vector3Int direction, int edgeID)
        {
            Vector3Int roatedDir = PGGUtils.GetRotatedFlatDirectionFrom(direction);

            for (int i = 0; i < edgeCells.Count; i++)
            {

                if (SkipAmount > 0f)
                {
                    if (skips[iterationCount]) { iterationCount += 1; continue; }
                    iterationCount += 1;
                }

                var cell = edgeCells[i];

                if (AvoidCorners)
                {
                    if (planner.LatestChecker.ContainsLocal(cell.Pos + roatedDir) == false) continue;
                    if (planner.LatestChecker.ContainsLocal(cell.Pos - roatedDir) == false) continue;
                }

                int nearestPl = CheckNearestOn(planned, cell.Pos, direction);

                if (nearestPl == -1)
                {
                    if (X2X1Amount >= 1f) // All 2x1s
                    {
                        Proceed2x1Placement(cell.Pos, planned, edgeCells, planner, direction, roatedDir);
                    }
                    else if (X2X1Amount > 0f) // Some 2x1s randomized
                    {
                        if (planned2x1[edgeID][i])
                            Proceed2x1Placement(cell.Pos, planned, edgeCells, planner, direction, roatedDir);
                        else
                            planned.Add(new PlannedCommand(cell.Pos, direction, false));
                    }
                    else // zero 2x1s, only 1x1s
                    {
                        planned.Add(new PlannedCommand(cell.Pos, direction, false));
                    }
                }

            }
        }

        void Proceed2x1Placement(Vector3Int pos, List<PlannedCommand> planned, List<FieldCell> edgeCells, FieldPlanner planner, Vector3Int direction, Vector3Int rotatedDir)
        {
            if (!planner.LatestChecker.ContainsLocal(pos + rotatedDir))
            {
                if (!planner.LatestChecker.ContainsLocal(pos - rotatedDir))
                    return;
                else
                    rotatedDir = -rotatedDir;
            }

            if (AvoidCorners) if (!planner.LatestChecker.ContainsLocal(pos + rotatedDir * 2)) return;

            int nearestOn = CheckNearestOn(planned, pos + rotatedDir, direction);
            if (nearestOn != -1) return;

            planned.Add(new PlannedCommand(pos, direction, false));
            planned.Add(new PlannedCommand(pos + rotatedDir, direction, false));
        }

        int CheckNearestOn(List<PlannedCommand> planned, Vector3Int cellPos, Vector3Int direction, int spacingOffset = 0)
        {
            int nearestPl = -1;

            // Check if this cell is not too near to other command
            for (int p = 0; p < planned.Count; p++)
            {
                int distance = Mathf.FloorToInt(Vector3Int.Distance(planned[p].Cell, cellPos));
                if (AllowOtherEdges > 0) if (direction != planned[p].TargetDirection) distance += AllowOtherEdges;

                if (distance <= Spacing + spacingOffset)
                {
                    nearestPl = p;
                    break;
                }
            }

            return nearestPl;
        }


        struct PlannedCommand
        {
            public Vector3Int Cell;
            public Vector3Int TargetDirection;
            public bool WasByPlanner;
            public bool Is2x1Plan;

            public PlannedCommand(Vector3Int pos, Vector3Int dir, bool wasAlready, bool x2x1 = false) : this()
            {
                Cell = pos;
                TargetDirection = dir;
                WasByPlanner = wasAlready;
                Is2x1Plan = x2x1;
            }
        }


    }
}
