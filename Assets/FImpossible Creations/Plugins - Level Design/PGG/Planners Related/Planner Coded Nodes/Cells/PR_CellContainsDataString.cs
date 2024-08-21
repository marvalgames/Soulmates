using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells
{

    public class PR_CellContainsDataString : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Cell Contains Data String" : "Check if Cell Contains Data String"; }
        public override string GetNodeTooltipDescription { get { return "Checking if cell contains some string in its data strings list"; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(208, 122); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port(EPortPinType.Input)] public PGGCellPort Cell;
        [Port(EPortPinType.Input)] public PGGStringPort TargetString;
        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public BoolPort Contains;


        public override void OnStartReadingNode()
        {
            Contains.Value = false;
            Cell.TriggerReadPort(true);
            var inputCell = Cell.GetInputCellValue;
            if (inputCell == null) return;

            TargetString.TriggerReadPort(true);

            string tgt = TargetString.GetInputValue;
            if (string.IsNullOrEmpty(tgt)) return;

            if ( inputCell.HaveCustomData(tgt))
            {
                Contains.Value = true;
            }
        }

//#if UNITY_EDITOR

//        private SerializedProperty sp = null;
//        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
//        {
//            base.Editor_OnNodeBodyGUI(setup);

//            if (sp == null) sp = baseSerializedObject.FindProperty("PositionSpace");
//            EditorGUILayout.PropertyField(sp, GUIContent.none);

//            baseSerializedObject.ApplyModifiedProperties();
//        }

//#endif

    }
}