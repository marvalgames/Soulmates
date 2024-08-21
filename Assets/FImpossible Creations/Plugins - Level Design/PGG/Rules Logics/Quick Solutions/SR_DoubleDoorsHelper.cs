#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

namespace FIMSpace.Generating.Rules.QuickSolutions
{
    public class SR_DoubleDoorsHelper : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Double Doors Helper"; }
        public override string Tooltip() { return "Quick solution for replacing two small doorway commands placed next to each other with one bigger doorway"; }
        public EProcedureType Type { get { return EProcedureType.Coded; } }

        [Tooltip("Define on which side with command direction is placed doorway to detect and remove it")]
        public Vector3 WallPlacementGuide = new Vector3(0f, 1f, 1f);
        [Tooltip("Required index of the command to proceed check on")]
        public int MustBeCommandID = 0;
        [Tooltip("Distance range to detect spawns to remove")]
        public float DetectionTolerance = 1f;


        [PGG_SingleLineSwitch("CheckMode", 50, "Select if you want to use Tags, SpawnStigma or CellData", 110)]
        public string RemoveTagged = "";
        [HideInInspector] public ESR_Details CheckMode = ESR_Details.Tag;

        //[PGG_SingleLineTwoProperties("DebugDrawRays")]
        //[Tooltip("Prevent spawn if there are no two commands next to each other")]
        //public bool BeCondition = false;
        [Tooltip("Displaying measuring positions debug rays for about a second, after calling generating.\nGreen is 'WallPlacementGuide' position and yellow are spawns detected positions.\nGenerator need to be placed in zero position and zero rotation to align. To test it best will be Grid Painter component.")]
        /*[HideInInspector]*/ public bool DebugDrawRays = false;

        [Tooltip("Removing wall/door on the side  PLUS  removing check on the command cell  -  can be used to spawn completely new prefab using sub-spawner")]
        [HideInInspector] public bool RemoveOnSelf = false;

        [HideInInspector] public bool CallSubspawnerOn = false;

        [HideInInspector] public SR_CallSubSpawner.SubSpawnerCallHelper SubSpawnerCaller;

        //[HideInInspector] public NeightbourPlacement Placement = new NeightbourPlacement();
        [HideInInspector] public CheckCellsSelectorSetup checkSetup = new CheckCellsSelectorSetup(true, true);


        #region Editor Code
#if UNITY_EDITOR


        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("This rule will be executed only when two the same ID and the same direction commands are placed next to each other in the grid", MessageType.None);
            base.NodeBody(so);
        }

        SerializedProperty sp = null;
        SerializedProperty spt = null;

        public override void NodeFooter(SerializedObject so, FieldModification mod)
        {
            if (sp == null) sp = so.FindProperty("RemoveOnSelf");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(sp);
            DrawMultiCellSelector(checkSetup, OwnerSpawner);
            EditorGUILayout.EndHorizontal();

            SubSpawnerCaller.RefreshCaller(OwnerSpawner);

            if (spt == null) spt = so.FindProperty("CallSubspawnerOn");
            GUILayout.Space(5);
            spt.boolValue = EditorGUILayout.ToggleLeft(" Call Sub-Spawner On Met", spt.boolValue);

            if (CallSubspawnerOn)
            {
                EditorGUILayout.HelpBox("Call sub spawner to modify generated prefabs!", MessageType.None);

                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
                SubSpawnerCaller.NodeBody(so, "SubSpawnerCaller", true);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(5);
            if (DetectionTolerance < 0f) DetectionTolerance = 0f;
            base.NodeFooter(so, mod);
        }

#endif
        #endregion


        #region Helper Methods

        public override void Refresh()
        {
            base.Refresh();
            SubSpawnerCaller.RefreshCaller(OwnerSpawner);
        }

        void CallSubSpawner(FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, FieldModification mod, Quaternion rot)
        {
            if (SubSpawnerCaller.CallSpawner < 0) return;
            if (SubSpawnerCaller.targetSubSpawner == null) return;

            var nSpawn = SpawnData.GenerateSpawn(OwnerSpawner, mod, cell, -1);
            nSpawn.RotationOffset = rot.eulerAngles;
            //cell.AddSpawnToCell(nSpawn);
            SubSpawnerCaller.InheritCoords = true;

            SubSpawnerCaller.OnConditionsMetAction(ref nSpawn, preset, cell, grid);
        }

        #endregion


        static List<FieldCell> toCheck = null;
        bool anythingDetected = false;
        SpawnData ownedSpawn;

        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CheckRuleOn(mod, ref spawn, preset, cell, grid, restrictDirection);
            CellAllow = true;
            //if (BeCondition) ownedSpawn = spawn;
            //spawn.AddCustomStigma(GetInstanceID().ToString());
        }

        public override void CommandOnAfterAllCommandsCall(FGenGraph<FieldCell, FGenPoint> grid, FieldModification mod, FieldSetup preset, SpawnInstruction guide)
        {
            CheckOutput check = DoCheck(grid, guide, preset);

            if (check.found == false)
            {
                //if (BeCondition)
                //{
                //    ownedSpawn.Prefab = null;
                //    ownedSpawn.Enabled = false;
                //}

                return;
            }

            if (RemoveOnSelf)
            {
                ExecuteOn(check.cell, check.dir, check.rot, preset);
            }

            if (CallSubspawnerOn)
            {
                CallSubSpawner(preset, check.cell, grid, mod, check.rot);
            }
        }

        struct CheckOutput
        {
            public bool found;
            public FieldCell cell;
            public Quaternion rot;
            public Vector3 dir;
        }

        CheckOutput DoCheck(FGenGraph<FieldCell, FGenPoint> grid, SpawnInstruction guide, FieldSetup preset)
        {
            anythingDetected = false;
            CheckOutput output = new CheckOutput();
            output.found = false;

            if (guide.useDirection == false) return output;

            FieldCell cell = grid.GetCell(guide.gridPosition);
            if (FGenerators.IsNull(cell)) return output;

            Vector3 commandDirection = guide.desiredDirection;
            if (commandDirection == Vector3.zero) return output;

            if (toCheck == null) toCheck = new List<FieldCell>();
            toCheck.Clear();

            Quaternion rot = Quaternion.LookRotation(commandDirection);

            bool deflt = false;
            if (checkSetup.ToCheck.Count == 0) deflt = true;
            else
            if (checkSetup.ToCheck.Count == 1) if (checkSetup.ToCheck[0] == Vector3Int.zero) deflt = true;

            if (deflt)
            {
                FieldCell targetCell = grid.GetCell(cell.Pos + (rot * Vector3.left).V3toV3Int(), false);
                //if (FGenerators.IsNull(targetCell)) targetCell = grid.GetCell(cell.Pos + (rot * Vector3.right).V3toV3Int(), false);
                if (FGenerators.IsNull(targetCell)) return output;
                toCheck.Add(targetCell);
            }
            else
            {
                for (int i = 0; i < checkSetup.ToCheck.Count; i++)
                {
                    Vector3 offset = checkSetup.ToCheck[i];
                    FieldCell nCell;
                    if (checkSetup.UseRotor) nCell = grid.GetCell(cell.Pos + (rot * offset).V3toV3Int(), false);
                    else nCell = grid.GetCell(cell.Pos + (offset).V3toV3Int(), false);

                    if (FGenerators.NotNull(nCell)) toCheck.Add(nCell);
                }
            }

            if (toCheck.Count == 0) return output;
            if (SR_ModGraph.Graph_Instructions == null) return output;
            if (SR_ModGraph.Graph_Instructions.Count == 0) return output;

            anythingDetected = false;

            for (int i = 0; i < toCheck.Count; i++)
            {
                ExecuteOn(toCheck[i], commandDirection, rot, preset);
            }

            if (!anythingDetected) return output;

            output.cell = cell;
            output.rot = rot;
            output.dir = commandDirection;
            output.found = true;

            return output;
        }

        void ExecuteOn(FieldCell cell, Vector3 rootDirection, Quaternion rot, FieldSetup preset)
        {
            var instructions = SR_ModGraph.Graph_Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                if (instr.useDirection == false) continue;
                if (instr.HelperID != MustBeCommandID) continue;
                if (instr.gridPosition != cell.Pos) continue;
                if (instr.desiredDirection != rootDirection) continue;

                // Try removing target object in the cell
                if (string.IsNullOrEmpty(RemoveTagged) == false)
                {
                    var spawns = GetAllSpecificSpawns(cell, RemoveTagged, CheckMode);
                    for (int s = 0; s < spawns.Count; s++)
                    {
                        if (IsSpawnAlignedWithRoot(spawns[s], rot, preset))
                        {
                            anythingDetected = true;
                            cell.RemoveSpawnFromCell(spawns[s]);
                        }
                    }
                }
            }
        }

        bool IsSpawnAlignedWithRoot(SpawnData spawn, Quaternion rot, FieldSetup preset)
        {
            Vector3 frontPos = rot * WallPlacementGuide;
            Vector3 centeredPosition = GetSpawnCenterPositionCellSpace(spawn, ESR_Origin.RendererCenter);

            #region Debug
#if UNITY_EDITOR

            if (DebugDrawRays)
            {
                Vector3 wPos = spawn.OwnerCell.WorldPos(preset);
                wPos += frontPos;
                UnityEngine.Debug.DrawLine(spawn.OwnerCell.WorldPos(preset), wPos, Color.green, 1.2f);
                wPos = spawn.OwnerCell.WorldPos(preset);
                wPos += centeredPosition;
                UnityEngine.Debug.DrawLine(spawn.OwnerCell.WorldPos(preset), wPos, Color.yellow * 0.8f, 1.2f);
                //UnityEngine.Debug.Log("frontpos = " + frontPos + " vs " + centeredPosition + " = " + distance);
            }

#endif
            #endregion

            float distance = Vector3.Distance(frontPos, centeredPosition);

            return distance < DetectionTolerance;
        }

        public override void PreGenerateResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
        {
            if (OwnerSpawner == null)
            {
                OwnerSpawner = callFrom;
                SubSpawnerCaller.RefreshCaller(OwnerSpawner);
            }

            SubSpawnerCaller.OnPreGenerate(grid, preset, callFrom);
        }

    }

}