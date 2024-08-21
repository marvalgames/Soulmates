using FIMSpace.Graph;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Utilities
{

    public class PR_DebugDrawPosition : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "   Debug Draw Position" : "Debug Log Draw Position"; }
        public override string GetNodeTooltipDescription { get { return "(This node will break async generation)\nJust calling Debug.DrawLines to display it in scene view"; } }
        public override Color GetNodeColor() { return new Color(0.4f, 0.4f, 0.4f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(208, _EditorFoldout ? 104 : 84); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Debug; } }

        [Port(EPortPinType.Input, EPortNameDisplay.HideName, 1)] public PGGVector3Port Position;

        [HideInInspector] public Color LogColor = Color.green;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            Position.TriggerReadPort(true);

            object inPort = Position.GetPortValueSafe;

            if (inPort is Vector3)
            {
                var planner = FieldPlanner.CurrentGraphExecutingPlanner;
                planner.LatestChecker.DebugLogDrawCellIn(Position.GetInputValue, LogColor, 0.5f);
            }
            else
            {
                #region Supporting Cell Port

                if (inPort is PGGCellPort.Data)
                {
                    PGGCellPort.Data dta = (PGGCellPort.Data)inPort;

                    if (FGenerators.IsNull(dta.CellRef))
                    {
                        if (FGenerators.NotNull(dta.ParentChecker))
                        {
                            dta.ParentChecker.DebugLogDrawCellsInWorldSpace(LogColor);
                        }
                    }
                    else
                    {
                        if (FGenerators.NotNull(dta.ParentChecker))
                        {
                            dta.ParentChecker.DebugLogDrawCellInWorldSpace(dta.CellRef, LogColor);
                        }
                    }
                }
                else
                {
                    var planner = FieldPlanner.CurrentGraphExecutingPlanner;
                    planner.LatestChecker.DebugLogDrawCellIn(Position.GetInputValue, LogColor, 0.5f);
                }

                #endregion
            }
        }


        #region Node GUI Code

#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("LogColor");

            if (_EditorFoldout)
            {
                EditorGUILayout.PropertyField(sp);
            }

            baseSerializedObject.ApplyModifiedProperties();
        }
#endif

        #endregion

    }
}