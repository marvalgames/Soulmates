using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.Placement
{
    public partial class SR_OnGridCorner : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "On Grid Corner"; }
        public override string Tooltip() { return "Allowing or disallowing running this spawner when spawn going to be placed on grid bounds corner positions\n[Lightweight] " + base.Tooltip(); }
        public EProcedureType Type { get { return EProcedureType.Coded; } }

        public float DetectionTolerance = 1f;
        public bool IgnoreY = true;

        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            Bounds bounds = grid.GetBounds();
            Vector3 targetSpawnPosition = spawn.GetWorldPositionWithFullOffset(preset, true);

            if ( IgnoreY)
            {
                targetSpawnPosition.y = 0f;
                bounds.size = new Vector3(bounds.size.x, 0.0001f, bounds.size.z);
                bounds.center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            }

            bounds = FEngineering.TransformBounding(bounds, Matrix4x4.Scale(preset.GetCellUnitSize()));

            CellAllow = false;

            if ( Vector3.Distance(targetSpawnPosition, bounds.min) < DetectionTolerance)
            {
                Detected();
            }
            else if (Vector3.Distance(targetSpawnPosition, bounds.max) < DetectionTolerance)
            {
                Detected();
            }
            else if (Vector3.Distance(targetSpawnPosition, new Vector3(bounds.min.x, bounds.center.y, bounds.max.z)) < DetectionTolerance)
            {
                Detected();
            }
            else if (Vector3.Distance(targetSpawnPosition, new Vector3(bounds.max.x, bounds.center.y, bounds.min.z)) < DetectionTolerance)
            {
                Detected();
            }
        }

        void Detected()
        {
            CellAllow = true;
        }

    }
}