using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Generating
{

    public class PR_BoundsToCells : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Bounds To Cells"; }
        public override string GetNodeTooltipDescription { get { return "Converting provided bounds into cells (iheriting cell scale of this field planner)"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }
        public override Color GetNodeColor() { return new Color(0.45f, 0.9f, 0.15f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(224, _EditorFoldout ? 125 : 105); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }

        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.NotEditable, 1)] public PGGUniversalPort Bounds;
        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGPlannerPort Shape;
        [Tooltip("Assign if you want to inherit cell scale and rest of the settings of other field")]
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideOnConnected, 1)] public PGGPlannerPort BaseShape;


        public override void OnStartReadingNode()
        {
            Shape.Switch_DisconnectedReturnsByID = false;
            Shape.Switch_MinusOneReturnsMainField = false;

            if (Bounds.IsConnected == false) return;
            Bounds.TriggerReadPort(true);

            object val = Bounds.GetPortValueSafe;

            if ( val == null) return;
            Bounds bounds = PGGUniversalPort.TryReadAsBounds(val);

            if (bounds.size == Vector3.zero) return;

            var basePlanner = GetPlannerFromPort(BaseShape);
            if (basePlanner == null) basePlanner = CurrentExecutingPlanner;
            if (basePlanner == null) return;

            var baseChecker = basePlanner.LatestChecker;
            if (baseChecker == null) return;

            var checkerVolume = new CheckerField3D();
            checkerVolume.CopyParamsFrom(baseChecker);

            if (_EditorDebugMode) FDebug.DrawBounds3D(bounds, Color.green);

            var cells = checkerVolume.BoundsToCells(checkerVolume.WorldToLocalBounds(bounds), true, true);
            for (int c = 0; c < cells.Count; c++) checkerVolume.AddLocal(cells[c].Pos);

            Shape.Output_Provide_Checker(checkerVolume);
        }


#if UNITY_EDITOR
        UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            Shape.Editor_DefaultValueInfo = "(None)";

            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();
            if (_EditorFoldout)
            {
                GUILayout.Space(1);
                if (sp == null) sp = baseSerializedObject.FindProperty("BaseShape");
                UnityEditor.EditorGUILayout.PropertyField(sp, true);
            }
            baseSerializedObject.ApplyModifiedProperties();
        }


#endif

    }
}