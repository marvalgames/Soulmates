using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetCellInstruction : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Get Cell Instruction" : "Get Field's Cell Instruction"; }
        public override string GetNodeTooltipDescription { get { return "Getting some Field instruction by index. If index is out of range, returns null."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2( 224, _EditorFoldout ? 124 : 104); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }


        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGUniversalPort Instruction;
        [Port(EPortPinType.Input)] public IntPort Index;
        [HideInInspector][Port(EPortPinType.Input)] public PGGPlannerPort InstructionsOf;

        public override void OnStartReadingNode()
        {
            Instruction.Variable.SetTemporaryReference( true, null );

            InstructionsOf.TriggerReadPort(true);
            FieldPlanner planner = GetPlannerFromPort( InstructionsOf, false );

            if (planner == null) return;
            if (planner.LatestResult == null) return;

            SpawnInstructionGuide guide = null;

            Index.TriggerReadPort( true );
            int index = Index.GetInputValue;
            if( planner.LatestResult.CellsInstructions.ContainsIndex( index ) ) guide = planner.LatestResult.CellsInstructions[index];

            Instruction.Variable.SetTemporaryReference( true, guide);
        }

        #region Editor GUI Code

#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                if (sp == null) sp = baseSerializedObject.FindProperty( "InstructionsOf" );
                EditorGUILayout.PropertyField(sp, true);
                InstructionsOf.AllowDragWire = true;
            }
            else
            {
                InstructionsOf.AllowDragWire = false;
            }
        }

#endif

        #endregion

    }
}