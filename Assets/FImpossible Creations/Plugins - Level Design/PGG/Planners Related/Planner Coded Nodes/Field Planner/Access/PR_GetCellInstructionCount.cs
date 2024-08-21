using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetCellInstructionCount : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Cell Instructions Count" : "Get Field's Cell Instructions Count"; }
        public override string GetNodeTooltipDescription { get { return "Getting count of already added instructions inside Field Planner"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2( 224, _EditorFoldout ? 104 : 84); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }


        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public IntPort Count;
        [HideInInspector][Port(EPortPinType.Input)] public PGGPlannerPort CommandsOf;

        public override void OnStartReadingNode()
        {
            Count.Value = 0;

            if (CurrentExecutingPlanner == null) return;
            if (CurrentExecutingPlanner.ParentBuildPlanner == null) return;

            CommandsOf.TriggerReadPort(true);
            FieldPlanner planner = GetPlannerFromPort( CommandsOf, false );

            if (planner == null) return;
            if (planner.LatestResult == null) return;
            Count.Value = planner.LatestResult.CellsInstructions.Count;
        }

        #region Editor GUI Code

#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                if (sp == null) sp = baseSerializedObject.FindProperty("CommandsOf");
                EditorGUILayout.PropertyField(sp, true);
                CommandsOf.AllowDragWire = true;
            }
            else
            {
                CommandsOf.AllowDragWire = false;
            }
        }

#endif

        #endregion

    }
}