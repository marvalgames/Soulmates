using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.Modelling
{
    public class SR_VolumeIndicator : SpawnRuleBase, ISpawnProcedureType
    {
        // Base parameters implementation
        public override string TitleName() { return "Volume Indicator"; }
        public override string Tooltip() { return "Position indicator for volume generators."; }

        // Define what your script will do
        public EProcedureType Type { get { return EProcedureType.Event; } }

        [Tooltip("ID Used by volume generators to define indicators for volume generator nodes")]
        public string IndicatorID = "Roof 1";

        public static Dictionary<string, List<SpawnData>> VolumeIndicators = new Dictionary<string, List<SpawnData>>();
        public override void PreGenerateResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
        {
            VolumeIndicators = new Dictionary<string, List<SpawnData>>();
        }


        #region There you can do custom modifications for inspector view
#if UNITY_EDITOR
        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("Setting internal indicator for volume generator nodes", MessageType.None);
            // GUIIgnore.Clear(); GUIIgnore.Add("Tag"); // Custom ignores drawing properties
            base.NodeBody(so);
        }
#endif
        #endregion


        public override void CellInfluence(FieldSetup preset, FieldModification mod, FieldCell cell, ref SpawnData spawn, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            if (VolumeIndicators.ContainsKey(IndicatorID) == false) VolumeIndicators.Add(IndicatorID, new List<SpawnData>());
            VolumeIndicators[IndicatorID].Add(spawn);
        }
    }
}