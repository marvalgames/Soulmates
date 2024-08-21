using FIMSpace.Generating.Rules.Helpers;
using UnityEngine;

namespace FIMSpace.Generating.Rules.Collision.Legacy
{
    public class SR_CheckWorldCollision : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Check World Collision"; }
        public override string Tooltip() { return "Checking collisions with objects which are already on the scene, using UnityEngine.Physics operations"; }
        public EProcedureType Type { get { return EProcedureType.Coded; } }

        public LayerMask CollisionMask = ~(0 << 0);
        public Vector3 ScaleBoundingBox = Vector3.one;

        public enum ECheckOrder
        {
            OnCheck, OnInfluence, OnConditionsMet
        }

        [Tooltip("Define at which execution stage, rule should check spawns state")]
        public ECheckOrder CheckOrder = ECheckOrder.OnConditionsMet;

        Collider[] _buffer = new Collider[1];

        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CheckRuleOn(mod, ref spawn, preset, cell, grid, restrictDirection);
            CellAllow = true;
            if (CheckOrder == ECheckOrder.OnCheck) CheckOn(preset, spawn);
        }

        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            base.OnConditionsMetAction(mod, ref thisSpawn, preset, cell, grid);
            if (CheckOrder == ECheckOrder.OnConditionsMet) CheckOn(preset, thisSpawn);
        }

        public override void CellInfluence(FieldSetup preset, FieldModification mod, FieldCell cell, ref SpawnData spawn, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CellInfluence(preset, mod, cell, ref spawn, grid, restrictDirection);
            if (CheckOrder == ECheckOrder.OnInfluence) CheckOn(preset, spawn);
        }

        void CheckOn(FieldSetup preset, SpawnData spawn)
        {
            if (OwnerSpawner == null) return;
            if (OwnerSpawner.WorldMatrix == null) return;

            Matrix4x4 mx = OwnerSpawner.WorldMatrix.Value;

            if (CheckOn(mx, preset, spawn) > 0) CellAllow = false;
        }

        int CheckOn(Matrix4x4 mx, FieldSetup preset, SpawnData spawn)
        {
            CollisionOffsetData thisOffset = new CollisionOffsetData(spawn);

            Vector3 center = mx.MultiplyPoint(spawn.GetWorldPositionWithFullOffset(preset, true));
            Quaternion rot = mx.rotation * Quaternion.Euler(spawn.GetFullRotationOffset());

            int count = Physics.OverlapBoxNonAlloc(center, Vector3.Scale(thisOffset.bounds.extents, ScaleBoundingBox), _buffer, rot, CollisionMask, QueryTriggerInteraction.Ignore);
            if (count > 0) spawn.Enabled = false;
            return count;
        }
    }
}