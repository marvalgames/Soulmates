using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Generating.Rules;
using FIMSpace.Graph;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.ModNodes.Operations
{

    public class MR_AddExtraSpawn : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? ("Extra Spawn") : "Add Extra Spawn to Cell"; }
        public override string GetNodeTooltipDescription { get { return "Adding extra additional spawn to the cell spawning queue"; } }
        public override Color GetNodeColor() { return new Color(0.7f, 0.55f, 0.25f, 0.9f); }
        public override bool IsFoldable { get { return false; } }
        public override Vector2 NodeSize { get { return new Vector2(184, _EditorFoldout ? 144 : 100); } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }
        [Port(EPortPinType.Input, 1)] public PGGSpawnPort ExtraSpawn;
        [Port(EPortPinType.Input, 1)] public PGGModCellPort WhichCell;


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            ExtraSpawn.Clear();
            ExtraSpawn.TriggerReadPort(true);
            var spawn = ExtraSpawn.GetFirstConnectedSpawn;

            if (FGenerators.IsNull(spawn)) { return; }
            if (spawn == MG_Spawn) { return; }

            FieldCell cell = MG_Cell;

            if ( WhichCell.IsConnected)
            {
                WhichCell.TriggerReadPort(true);
                cell = WhichCell.GetInputCellValue;
            }

            if (cell.IsNull()) return;
            cell.AddSpawnToCell(spawn);

            //UnityEngine.Debug.Log("adding spawn, now count = " + MG_Cell.CollectSpawns().Count);
        }



    }
}