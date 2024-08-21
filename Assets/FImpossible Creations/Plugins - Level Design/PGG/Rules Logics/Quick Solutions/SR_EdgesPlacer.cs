using UnityEngine;

namespace FIMSpace.Generating.Rules.QuickSolutions
{
    public class SR_EdgesPlacer : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Edges Placer"; }
        public override string Tooltip() { return "Spawning wall tiles with pre-defined rules and aligning rotation. Works like 'Wall Placer's' Wall Module"; }
        public EProcedureType Type { get { return EProcedureType.Coded; } }
        
        
        public float YawOffset = 0f;
        [PGG_SingleLineSwitch("OffsetMode", 58, "Select if you want to offset postion with cell size or world units", 140)]
        public Vector3 DirectOffset = Vector3.zero;
        [HideInInspector] public ESR_Measuring OffsetMode = ESR_Measuring.Units;

        [PGG_SingleLineSwitch("CheckMode", 68, "Select if you want to use Tags, SpawnStigma or CellData", 40)]
        [/*HideInInspector, */SerializeField] private string OnTagged = "";
        [HideInInspector] public ESR_Details CheckMode = ESR_Details.Tag;

        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CheckRuleOn(mod, ref spawn, preset, cell, grid, restrictDirection);


            if (!(string.IsNullOrEmpty(OnTagged)))
            {
                if (!SpawnRules.CheckNeightbourCellAllow(ESR_Space.Occupied, cell, OnTagged, CheckMode))
                {
                    CellAllow = false;
                    return;
                }
            }

            CheckOnCell(cell.Pos, new Vector3Int(1, 0, 0), grid, ref spawn, preset);
            CheckOnCell(cell.Pos, new Vector3Int(-1, 0, 0), grid, ref spawn, preset);
            CheckOnCell(cell.Pos, new Vector3Int(0, 0, 1), grid, ref spawn, preset);
            CheckOnCell(cell.Pos, new Vector3Int(0, 0, -1), grid, ref spawn, preset);

            CellAllow = false;

            if (tempSpawns != null)
            {
                if (tempSpawns.Count > 0) CellAllow = true;
            }

            if (CellAllow)
            {
                spawn.TempRotationOffset += new Vector3(0, YawOffset, 0);
                spawn.TempPositionOffset = Quaternion.Euler(spawn.TempRotationOffset) * GetUnitOffset(DirectOffset, OffsetMode, preset);
            }
        }

        void CheckOnCell(Vector3Int origin, Vector3Int offset, FGenGraph<FieldCell, FGenPoint> grid, ref SpawnData spawn, FieldSetup preset)
        {
            FieldCell cell = grid.GetCell(origin + offset, false);

            if (!(string.IsNullOrEmpty(OnTagged)))
            {
                if (!cell.Available())
                {
                    CopySpawnToTempData(ref spawn, -offset, preset);
                    return;
                }

                if (!SpawnRules.CheckNeightbourCellAllow(ESR_Space.Occupied, cell, OnTagged, CheckMode))
                {
                    CopySpawnToTempData(ref spawn, -offset, preset);
                    return;
                }
            }

            if (!cell.Available())
            {
                CopySpawnToTempData(ref spawn, -offset, preset);
            }
        }

        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            var preSpawn = spawn;
            spawn.Enabled = false;

            if (tempSpawns != null) if (tempSpawns.Count > 0)
                {
                    spawn = tempSpawns[0];
                    spawn.LocalScaleMul = preSpawn.LocalScaleMul;
                    spawn.Enabled = true;

                    for (int i = 1; i < tempSpawns.Count; i++)
                    {
                        tempSpawns[i].LocalScaleMul = preSpawn.LocalScaleMul;
                        cell.AddSpawnToCell(tempSpawns[i]);
                    }
                }
        }

        protected void CopySpawnToTempData(ref SpawnData source, Vector3 normal, FieldSetup preset)
        {
            var tgtSpawn = source.Copy();
            AssignSpawnCoords(tgtSpawn, normal, preset);
            AddTempData(tgtSpawn, source);
            if (tempSpawns.Count == 1) AssignSpawnCoords(source, normal, preset);
        }

        public void AssignSpawnCoords(SpawnData spawn, Vector3 normal, FieldSetup preset)
        {
            spawn.RotationOffset = Quaternion.LookRotation(normal).eulerAngles + Vector3.up * YawOffset;
            spawn.DirectionalOffset = GetUnitOffset(DirectOffset, OffsetMode, preset);
        }

    }
}