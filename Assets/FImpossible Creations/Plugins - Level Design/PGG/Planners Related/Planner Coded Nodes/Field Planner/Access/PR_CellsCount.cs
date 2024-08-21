using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_CellsCount : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Get Cells Count" : "Get Field's Cells Count"; }
        public override string GetNodeTooltipDescription { get { return "Getting count of field's cells in the grid"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 230 : 200, _EditorFoldout ? 104 : 84); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }


        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public IntPort Count;
        [HideInInspector][Port(EPortPinType.Input)] public PGGPlannerPort CellsOf;

        public override void OnStartReadingNode()
        {
            Count.Value = 0;
            if (CurrentExecutingPlanner == null) return;
            if (CurrentExecutingPlanner.ParentBuildPlanner == null) return;

            CellsOf.TriggerReadPort(true);
            var chec = GetCheckerFromPort(CellsOf, false);

            if (chec == null) return;
            Count.Value = chec.CheckerReference.ChildPositionsCount;
        }

        #region Editor GUI Code

#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                if (sp == null) sp = baseSerializedObject.FindProperty("CellsOf");
                EditorGUILayout.PropertyField(sp, true);
                CellsOf.AllowDragWire = true;
            }
            else
            {
                CellsOf.AllowDragWire = false;
            }
        }

#endif

        #endregion

    }
}