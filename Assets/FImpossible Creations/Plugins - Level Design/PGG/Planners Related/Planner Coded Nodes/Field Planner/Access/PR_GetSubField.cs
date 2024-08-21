using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetSubField : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Get Sub Field" : "Get Sub Field by Index"; }
        public override string GetNodeTooltipDescription { get { return "If you added some sub fields to the field planner, you can access them starting with index = 0."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(210, _EditorFoldout ? 116 : 98); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input, 1)] public IntPort Index;
        [Port(EPortPinType.Output, EPortValueDisplay.Default)] public PGGPlannerPort Planner;
        [HideInInspector] [Port(EPortPinType.Input, 1)] public PGGPlannerPort SubFieldOf;

        public override void OnCreated()
        {
            base.OnCreated();
            Planner.Switch_DisconnectedReturnsByID = false;
        }

        bool DrawInstInd { get { return Planner.PortState() == EPortPinState.Connected; } }

        public override void OnStartReadingNode()
        {
            Index.TriggerReadPort(true);
            Planner.Clear();
            Planner.Switch_DisconnectedReturnsByID = false;
            int instId = 0;

            if (Planner.PortState() == EPortPinState.Connected)
            {
                instId = Index.GetInputValue;
                if (instId < 0) instId = 0;
            }

            if (CurrentExecutingPlanner == null) return;

            SubFieldOf.TriggerReadPort(true);
            FieldPlanner planner;
            //if (SubFieldOf.IsNotConnected) planner = CurrentExecutingPlanner;
            //else 
                planner = GetPlannerFromPort(SubFieldOf, false);

            if (planner == null) return;

            FieldPlanner p = planner.GetSubField(instId);

            if (p == null) return;

            Planner.Output_Provide_Planner(p);
        }


        #region Editor GUI Code

#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                if (sp == null) sp = baseSerializedObject.FindProperty("SubFieldOf");
                EditorGUILayout.PropertyField(sp, true);
                SubFieldOf.AllowDragWire = true;
            }
            else
            {
                SubFieldOf.AllowDragWire = false;
            }
        }

#endif

        #endregion


    }
}