using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Special
{

    public class PR_PushInDirUntilFullyContained : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Push Until Contained By" : "Push In Dir Push Until Fully Contained By"; }
        public override string GetNodeTooltipDescription { get { return "Pushing field per cell to the point in which it's fully contained by other field and just before leaving this condition."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }
        public override Vector2 NodeSize { get { return new Vector2(268, _EditorFoldout ? 124 : 112); } }
        public override bool IsFoldable { get { return true; } }
        public override Color GetNodeColor() { return new Color(0.1f, 0.7f, 1f, 0.95f); }

        [Tooltip("If 'Collision With' left empty or -1 then colliding with every field in the current plan stage")]
        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ContainedBy;
        [Port(EPortPinType.Input, EPortValueDisplay.Default, 1)] public PGGVector3Port PushDirection;
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort ToPush;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner planner = GetPlannerFromPort(ToPush);
            PushDirection.TriggerReadPort(true);

            Vector3Int pushDir = PushDirection.GetInputValue.normalized.V3toV3Int();
            if (pushDir == Vector3.zero) return;

            ContainedBy.TriggerReadPort(true);
            FieldPlanner checkCollWith = GetPlannerFromPort(ContainedBy, false);

            if (checkCollWith == null) return;
            if (checkCollWith == planner) return;
            if (checkCollWith.LatestChecker == null) return;

            planner.LatestChecker.StepPushContainedBounds(checkCollWith.LatestChecker, pushDir, 256);
        }



#if UNITY_EDITOR

        private UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            ContainedBy.Editor_DefaultValueInfo = "(None)";

            if (_EditorFoldout)
            {
                GUILayout.Space(1);

                ToPush.AllowDragWire = true;
                baseSerializedObject.Update();
                if (sp == null) sp = baseSerializedObject.FindProperty("ToPush");
                UnityEditor.SerializedProperty scp = sp.Copy();
                UnityEditor.EditorGUILayout.PropertyField(scp);
                baseSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                ToPush.AllowDragWire = false;
            }
        }

#endif

    }
}