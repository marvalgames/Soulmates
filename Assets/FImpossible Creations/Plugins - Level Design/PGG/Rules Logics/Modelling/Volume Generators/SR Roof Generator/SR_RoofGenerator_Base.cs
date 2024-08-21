#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

namespace FIMSpace.Generating.Rules.Modelling
{
    public abstract class SR_RoofGenerator_Base : SpawnRuleBase, ISpawnProcedureType
    {
        public override string Tooltip() { return "Procedural roof mesh generator node."; }
        public EProcedureType Type { get { return EProcedureType.Coded; } }

        [Tooltip("ID of volume indicators to use")]
        public string GetIndicatorID = "Roof 1";

        //[PGG_SingleLineSwitch("RoofScale", 58, "Select if you want to offset postion with cell size or world units", 140)]
        [Tooltip("How high should reach roof center connection level")]
        public float RoofHeight = 3;
        public SpawnerVariableHelper RoofHeightVariable = new SpawnerVariableHelper(FieldVariable.EVarType.Number);

        [Tooltip("Shifting roof connection point from center to defined roof points area front or back")]
        [Range(-1f, 1f)] public float RoofCenterOffset = 0;
        public SpawnerVariableHelper RoofCenterVariable = new SpawnerVariableHelper(FieldVariable.EVarType.Number);

        public bool Rotate90 = false;


        protected List<SpawnerVariableHelper> spawnerVariableHelpers = new List<SpawnerVariableHelper>();
        public override List<SpawnerVariableHelper> GetVariables()
        {
            spawnerVariableHelpers.Clear();
            spawnerVariableHelpers.Add(RoofHeightVariable);
            spawnerVariableHelpers.Add(RoofCenterVariable);
            return spawnerVariableHelpers;
        }


        #region Editor GUI

#if UNITY_EDITOR


        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("! Spawn the 'Volume Indicators' to define roof shape !", MessageType.None);
            base.NodeBody(so);
        }

#endif

        #endregion


        bool spawned = false;
        public override void PreGenerateResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
        {
            spawned = false;
        }

        public override void OnGeneratingCompleated(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset)
        {
            base.OnGeneratingCompleated(grid, preset);
            spawned = false;
        }

        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CheckRuleOn(mod, ref spawn, preset, cell, grid, restrictDirection);

            if (spawned == false)
            {
                _roof_Preset = preset;
                _roof_Mod = mod;
                _roof_Cell = cell;
                _roof_Grid = grid;

                CellAllow = !spawned;
                spawned = true;
            }
        }

        protected FieldSetup _roof_Preset = null;
        protected FieldModification _roof_Mod = null;
        protected FieldCell _roof_Cell = null;
        protected FGenGraph<FieldCell, FGenPoint> _roof_Grid = null;


        protected virtual void OnGenerateRoof(List<SpawnData> indicators, FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            PrepareIndicatorsRelation(indicators, preset);
            PrepareMesh(preset, ref thisSpawn, cell);
        }


        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            if (SR_VolumeIndicator.VolumeIndicators == null) return;
            if (SR_VolumeIndicator.VolumeIndicators.ContainsKey(GetIndicatorID) == false) return;

            OnGenerateRoof(SR_VolumeIndicator.VolumeIndicators[GetIndicatorID], mod, ref thisSpawn, preset, cell, grid
                );
        }


        // Roof gen methods
        protected Bounds indicatorsBounds;
        protected List<SpawnData> edgeLPositions = new List<SpawnData>();
        protected List<SpawnData> edgeRPositions = new List<SpawnData>();

        protected virtual void PrepareIndicatorsRelation(List<SpawnData> indicators, FieldSetup preset)
        {
            edgeLPositions.Clear();
            edgeRPositions.Clear();

            indicatorsBounds = new Bounds();
            bool boundsSet = false;

            List<SpawnData> roofInd = indicators;
            Vector3 boxSize = preset.GetCellUnitSize();
            boxSize.y = 0f;

            for (int i = 0; i < roofInd.Count; i++)
            {
                SpawnData spawn = roofInd[i];
                Vector3 lPos = spawn.GetWorldPositionWithFullOffset(preset);
                Bounds sBounds = new Bounds(lPos, boxSize);
                //FDebug.DrawBounds3D(sBounds, Color.green);

                if (!boundsSet) { indicatorsBounds = sBounds; boundsSet = true; }
                else indicatorsBounds.Encapsulate(sBounds);
            }

            for (int i = 0; i < roofInd.Count; i++)
            {
                var spawn = roofInd[i];
                Vector3 lPos = spawn.GetWorldPositionWithFullOffset(preset);

                if (lPos.x < indicatorsBounds.center.x) edgeLPositions.Add(spawn);
                else edgeRPositions.Add(spawn);
            }

            //FDebug.DrawBounds3D(indicatorsBounds, Color.red);

            edgeLPositions.Sort((a, b) => a.GetWorldPositionWithFullOffset(preset).z.CompareTo(b.GetWorldPositionWithFullOffset(preset).z));
            edgeRPositions.Sort((a, b) => a.GetWorldPositionWithFullOffset(preset).z.CompareTo(b.GetWorldPositionWithFullOffset(preset).z));

            //if (Is90Rotated)
            //{
            //    indicatorsBounds = FEngineering.RotateLocalBounds(indicatorsBounds, Quaternion.Euler(0f, 90f, 0f));
            //}
        }


        protected virtual void PrepareMesh(FieldSetup preset, ref SpawnData thisSpawn, FieldCell cell)
        {
        }


        protected FieldCell GetCellInPosition(Vector3 wpos, int getNearest = 0)
        {
            //return _roof_Cell;
            // TODO wPos to cell space -> for read get coords etc.
            Vector3 cellSize = _roof_Preset.GetCellUnitSize();
            Vector3 localPos = wpos;

            localPos.x /= cellSize.x;

            if (_roof_Grid.MaxY.Pos.y == _roof_Grid.MinY.Pos.y)
                localPos.y = _roof_Grid.MinY.Pos.y;
            else
                localPos.y /= cellSize.y;

            localPos.z /= cellSize.z;

            Vector3Int checkPos = localPos.V3toV3Int();
            FieldCell tgtCell = _roof_Grid.GetCell(checkPos, false);

            if (!tgtCell.Available())
            {
                if (getNearest > 0) // Try get nearest neightbour cell max 1 cell distance
                {
                    for (int x = 0; x <= getNearest; x++)
                        for (int z = 0; z <= getNearest; z++)
                            for (int y = 0; y <= getNearest; y++)
                            {
                                tgtCell = _roof_Grid.GetCell(checkPos + new Vector3Int(x, y, z), false);
                                if (tgtCell.Available()) return tgtCell;
                                tgtCell = _roof_Grid.GetCell(checkPos + new Vector3Int(-x, -y, -z), false);
                                if (tgtCell.Available()) return tgtCell;
                            }
                }

                // Return unchanged - not found
                return _roof_Cell;
            }

            return tgtCell;
        }


        // Utility methods

        protected Vector3 RotateAround(Vector3 pos, Vector3 origin, Quaternion rot)
        {
            return rot * (pos - origin) + origin;
        }


    }
}