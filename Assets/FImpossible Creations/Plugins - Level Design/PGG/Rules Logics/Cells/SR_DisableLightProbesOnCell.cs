
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.Cells
{
    public class SR_DisableLightProbesOnCell : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Disable Light Probes On Cell"; }
        public override string Tooltip() { return "If you're generating light probes, you can ignore generating them for selective cells."; }

        public EProcedureType Type { get { return EProcedureType.OnConditionsMet; } }

#if UNITY_EDITOR
        public override void NodeBody(SerializedObject so)
        {
            UnityEditor.EditorGUILayout.HelpBox("If you're generating light probes, you can ignore generating them for selective cells.", MessageType.None);
            base.NodeBody(so);
        }
#endif

        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            cell.DontGenerateLightProbes = true;
        }
    }
}