using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Utilities
{

    public class PR_DebugDrawFieldHighlight : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "   Debug Field Highlight" : "Debug Draw Field Highlight"; }
        public override string GetNodeTooltipDescription { get { return "(This node will break async generation)\nJust calling Debug.DrawLines to display it in scene view"; } }
        public override Color GetNodeColor() { return new Color(0.4f, 0.4f, 0.4f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(208, _EditorFoldout ? 104 : 84); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Debug; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToDraw;

        [HideInInspector] public Color LogColor = Color.green;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            if (IsGeneratingAsync)
            {
                return;
            }

            ToDraw.TriggerReadPort(true);
            CheckerField3D checker = GetCheckerFromPort(ToDraw, false);

            FieldPlanner pl = GetPlannerFromPort(ToDraw, false);
            if (pl)
            {
                checker = pl.LatestChecker;
            }

            if (checker == null) return;

            //if (ToDraw.GetContainedCount() > 1)
            //{
            //    UnityEngine.Debug.Log("asd");
            //    foreach (var item in ToDraw.GetAllInputCheckers())
            //    {
            //        if (item == null) continue;
            //        FDebug.DrawBounds3D(item.GetFullBoundsWorldSpace(), LogColor);
            //        item.DebugLogDrawCellsInWorldSpace(LogColor);
            //    }
            //}
            //else
            {
                Bounds b = checker.GetFullBoundsWorldSpace();

                if (b.size == Vector3.zero)
                {
                    b.size = new Vector3(0.25f, 2f, 0.25f);
                    FDebug.DrawBounds3D(b, Color.red);
                    return;
                }

                FDebug.DrawBounds3D(b, LogColor);
                checker.DebugLogDrawCellsInWorldSpace(LogColor);
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