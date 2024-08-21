using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_FieldInstancesCount : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "    Instances Count" : "Get Field Instances Count"; }
        public override string GetNodeTooltipDescription { get { return "Getting count of Field instances"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 230 : 200, _EditorFoldout ? 104 : 84); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }


        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public IntPort Count;
        [HideInInspector][Port(EPortPinType.Input)] public PGGPlannerPort InstancesOf;

        public override void OnStartReadingNode()
        {
            Count.Value = 0;
            if (CurrentExecutingPlanner == null) return;
            if (CurrentExecutingPlanner.ParentBuildPlanner == null) return;

            InstancesOf.TriggerReadPort(true);
            FieldPlanner planner = GetPlannerFromPort(InstancesOf, false);

            if (planner == null) return;
            Count.Value = planner.Instances;
            if (planner.IsDuplicate) Count.Value = planner.DuplicateParent.Instances;
        }

        #region Editor GUI Code

#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                if (sp == null) sp = baseSerializedObject.FindProperty("InstancesOf");
                EditorGUILayout.PropertyField(sp, true);
                InstancesOf.AllowDragWire = true;
            }
            else
            {
                InstancesOf.AllowDragWire = false;
            }
        }

#endif

        #endregion

    }
}