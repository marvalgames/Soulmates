#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Rules.QuickSolutions
{
    public class SR_DirectionalRemove : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Directional Remove"; }
        public override string Tooltip() { return "Removing spawn in the defined offsetted position.\nIt's almost the same as 'Doorway Placer' but contains different parameters and is not moving/rotating target spawn."; }
        public EProcedureType Type { get { return EProcedureType.Coded; } }

        [PGG_SingleLineSwitch("CheckMode", 50, "Select if you want to use Tags, SpawnStigma or CellData", 110)]
        public string replaceOnTag = "";
        [HideInInspector] public ESR_Details CheckMode = ESR_Details.Tag;

        [PGG_SingleLineSwitch("OffsetMode", 58, "Select if you want to offset postion with cell size or world units", 140, 5)]
        public Vector3 LocalRemovePosition = Vector3.zero;
        [HideInInspector] public ESR_Measuring OffsetMode = ESR_Measuring.Cells;

        [Space(4)]
        [PGG_SingleLineSwitch("DistanceSource", 78, "Choose if you want to measure distance from prefab origin or first-mesh bounds center", 140, 5)]
        [Tooltip("Distance from 'replaceOnTag' object which will be removed")]
        public float RemoveDistance = 0.1f;
        [HideInInspector] public ESR_Origin DistanceSource = ESR_Origin.RendererCenter;

        [Space(5)]
        [Tooltip("You can call this directional removal on the cell on the right/left (+ - X) or in front/back (+ - Z)")]
        public Vector3Int ExecuteOnOffsettedCell;

        [Space(5)]
        [Tooltip("Displaying blue sphere gizmo to identify start measurement position and red sphere on the nearest detected object to remove")]
        public bool DrawDebugGizmos = false;
        private float debugNearestDist = 0f;
        string debugobjName = "";
        private Vector3 debugMeasurePos = Vector3.zero;
        private Vector3 debugMeasureNearestPos = Vector3.zero;

        private SpawnData targetSpawn = null;
        private FieldCell targetCell = null;
        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            CellAllow = false; // Not allow until rules met
            targetCell = null;

            if (Enabled == false || Ignore) return;


            #region Determinate desired direction

            // From cell instruction or get direction to nearest spawn with desired tag
            Vector3 targetDirection = Vector3.zero;

            if (ExecuteOnOffsettedCell != Vector3Int.zero)
            {
                Vector3Int off = SpawnRuleBase.GetOffset(spawn.GetRotationOffset(), ExecuteOnOffsettedCell);
                cell = grid.GetCell(cell.Pos + off, false);
                if (FGenerators.IsNull(cell)) return;
            }

            targetCell = cell;

            SpawnData anySpawn = null;
            if (cell.GetJustCellSpawnCount() > 0) anySpawn = cell.GetSpawnsJustInsideCell()[0];

            if (restrictDirection != null)
                targetDirection = restrictDirection.Value;
            else
            {
                if (FGenerators.CheckIfExist_NOTNULL(anySpawn))
                    targetDirection = anySpawn.GetRotationOffset() * Vector3.forward;
            }

            if (targetDirection.sqrMagnitude == 0)
            {
                return; // No direction
            }

            #endregion


            // Check if there is desired tagged object on cell with right distance
            targetSpawn = null;


            var spawns = cell.CollectSpawns(OwnerSpawner.ScaleAccess);
            float nearest = float.MaxValue;
            float dbgnearest = float.MaxValue; // For debugging purposes

            // Preparing rotation for spawn, position basing on node's parameters
            Quaternion dir = Quaternion.LookRotation(targetDirection); // Direction from cell
            Vector3 targetPosInCell = dir * LocalRemovePosition; // Directional offset with node 'Offset' parameter
            targetPosInCell = GetUnitOffset(targetPosInCell, OffsetMode, preset); // Using cell/units

            Vector3 thisPos = targetPosInCell;

            // Applying coords
            Vector3 preTempOff = spawn.TempPositionOffset;
            Vector3 preOff = spawn.Offset;
            Vector3 preDirOff = spawn.DirectionalOffset;

            spawn.TempPositionOffset = targetPosInCell;
            spawn.Offset = targetPosInCell;
            spawn.DirectionalOffset = Vector3.zero;

            Vector3 thisMeasurePos = thisPos;
            thisMeasurePos = spawn.GetPosWithFullOffset(true);


            spawn.TempPositionOffset = preTempOff;
            spawn.Offset = preOff;
            spawn.DirectionalOffset = preDirOff;

            debugMeasurePos = thisMeasurePos;

            Vector3 spawnPos;
            Vector3 oSpawnMeasurePos = Vector3.zero;

            // Finding nearest cell spawn in desired placement
            for (int s = 0; s < spawns.Count; s++)
            {
                if (spawns[s].OwnerMod == null) continue;
                if (spawns[s] == spawn) continue;

                if (string.IsNullOrEmpty(replaceOnTag) == false)  // assigned tag to search
                    if (SpawnHaveSpecifics(spawns[s], replaceOnTag, CheckMode) == false)
                        continue; // Not found required tags then skip this spawn

                spawnPos = spawns[s].GetPosWithFullOffset(true);
                float distance;

                if (DistanceSource == ESR_Origin.SpawnPosition)
                {
                    oSpawnMeasurePos = spawnPos;
                    distance = Vector3.Distance(thisPos, oSpawnMeasurePos);
                }
                else if (DistanceSource == ESR_Origin.BoundsCenter)
                {

                    if (spawns[s].PreviewMesh == null)
                    {
                        oSpawnMeasurePos = spawnPos;
                        distance = Vector3.Distance(thisPos, spawnPos);
                    }
                    else
                    {
                        oSpawnMeasurePos = spawns[s].GetPosWithFullOffset(true) + spawns[s].GetRotationOffset() * Vector3.Scale(spawns[s].Prefab.transform.localScale, spawns[s].GetRotationOffset() * spawns[s].PreviewMesh.bounds.center);
                        distance = Vector3.Distance(thisMeasurePos, oSpawnMeasurePos);
                    }

                }
                else
                {
                    Renderer thisRend = null;

                    if (spawns[s].Prefab != null)
                        thisRend = spawns[s].Prefab.GetComponentInChildren<Renderer>();
                    else
                        thisPos = thisMeasurePos;

                    Renderer otherSpawnRend = null;
                    if (spawns[s].Prefab)
                        otherSpawnRend = spawns[s].Prefab.GetComponentInChildren<Renderer>();


                    if (thisRend == null)
                    {
                        if (otherSpawnRend == null) // All null
                        {
                            oSpawnMeasurePos = spawnPos;
                            distance = Vector3.Distance(thisPos, spawnPos);
                        }
                        else // Just other exists
                        {
                            oSpawnMeasurePos = spawns[s].GetPosWithFullOffset(true) + spawns[s].GetRotationOffset() * Vector3.Scale(spawns[s].Prefab.transform.localScale, spawns[s].Prefab.transform.InverseTransformPoint(otherSpawnRend.bounds.center));
                            distance = Vector3.Distance(thisMeasurePos, oSpawnMeasurePos);
                        }
                    }
                    else // This renderer exists
                    {
                        if (otherSpawnRend == null) // Other renderer not exists
                        {
                            oSpawnMeasurePos = spawnPos;
                            distance = Vector3.Distance(thisPos, spawnPos);
                        }
                        else // All renderer exists
                        {
                            oSpawnMeasurePos = spawns[s].GetPosWithFullOffset(true) + spawns[s].GetRotationOffset() * Vector3.Scale(spawns[s].Prefab.transform.localScale, spawns[s].Prefab.transform.InverseTransformPoint(otherSpawnRend.bounds.center));
                            distance = Vector3.Distance(thisMeasurePos, oSpawnMeasurePos);
                        }
                    }
                }


                if (_EditorDebug)
                {
                    if (distance <= dbgnearest)
                    {
                        debugMeasureNearestPos = spawnPos;
                        debugNearestDist = distance;
                        debugobjName = spawns[s].Spawner.Name;
                        dbgnearest = distance;
                    }
                }

                if (distance > RemoveDistance) continue; // Dont remove spawn if it's too far

                if (distance <= nearest)
                {
                    targetSpawn = spawns[s]; // remembering previous object spawn to be removed at OnConditionsMet
                    nearest = distance;
                }
            }


            CellAllow = true;
        }

        public override void CellInfluence(FieldSetup preset, FieldModification mod, FieldCell cell, ref SpawnData spawn, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CellInfluence(preset, mod, cell, ref spawn, grid);
            _EditorDebug = DrawDebugGizmos;
        }


        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            if (FGenerators.CheckIfIsNull(targetSpawn)) return;
            if (FGenerators.IsNull(targetCell)) return;

            targetCell.RemoveSpawnFromCell(targetSpawn);
            targetSpawn.Enabled = false;
        }


#if UNITY_EDITOR
        public override void OnDrawDebugGizmos(FieldSetup preset, SpawnData spawn, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            base.OnDrawDebugGizmos(preset, spawn, cell, grid);

            Color preC = Gizmos.color;
            Gizmos.color = new Color(0.1f, 0.1f, 1f, 1f);
            Vector3 size = preset.GetCellUnitSize();
            Vector3 cellpos = cell.WorldPos(preset);
            Vector3 pos = cellpos + debugMeasurePos;
            Vector3 opos = cellpos + debugMeasureNearestPos;

            Gizmos.DrawWireSphere(pos, size.x * 0.1f);

            Handles.color = Gizmos.color;
            Handles.DrawLine(pos, opos);
            Handles.DrawLine(pos + size * 0.002f, opos + size * 0.002f);
            Gizmos.color = new Color(0.5f, 0.1f, .1f, 1f);
            Gizmos.DrawWireSphere(opos, size.x * 0.065f);

            Handles.color = preC;
            Gizmos.color = preC;

            Handles.Label(pos, new GUIContent("Nearest: " + debugNearestDist, "Nearest distance in cell " + cell.Pos + "\nto spawner: " + debugobjName + "\nwith local cell offset = " + debugMeasureNearestPos + " measured from local pos = " + debugMeasurePos), UnityEditor.EditorStyles.boldLabel);
        }
#endif

    }

}