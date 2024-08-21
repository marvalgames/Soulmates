using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_RemoveFieldCells : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Remove Field Cells" : "Remove Field Cells"; }
        public override bool IsFoldable { get { return true; } }
        public override string GetNodeTooltipDescription { get { return "Removing cells of one Field which are intersecting with another"; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(230, _EditorFoldout ? 104 : 86); } }



        [Tooltip("Shape to be cutted out of the 'Planner' shape")]
        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToRemove;

        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort RemoveFrom;

        public override EPlannerNodeType NodeType
        {
            get
            {
                return EPlannerNodeType.WholeFieldPlacement;
            }
        }
        //public bool db = false;
        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            ToRemove.Switch_MinusOneReturnsMainField = true;

            RemoveFrom.TriggerReadPort(true);
            ToRemove.TriggerReadPort(true);

            FieldPlanner plan = GetPlannerFromPort(RemoveFrom, false);

            CheckerField3D myChe = RemoveFrom.GetInputCheckerSafe;
            if (myChe == null) { return; }

            CheckerField3D oChe = ToRemove.GetInputCheckerSafe;
            if (oChe == null) { return; }

            PGGCellPort cellPort = ToRemove.IsConnectedJustWithCellPort();
            if (cellPort != null)
            {
                oChe = cellPort.GetSelectiveCellChecker();
                if (oChe == null) { return; }

                myChe.RemoveCellsCollidingWith(oChe);
                if (plan) plan.LatestResult.Checker = myChe;
                return; // Just With Cell Port- avoid removing using all cells of this cell owner
            }

            // Implementing removing cells out of multiple planners if provided (without using 'iterate fields' node)

            var removeFrom = RemoveFrom.Get_GetMultipleCheckers;
            for (int i = 0; i < removeFrom.Count; i++)
            {
                if (removeFrom[i].CheckerReference == null) continue;
                removeFrom[i].CheckerReference.RemoveCellsCollidingWith(oChe);
            }

            var toRemove = ToRemove.Get_GetMultipleCheckers;
            for (int i = 0; i < toRemove.Count; i++)
            {
                if (toRemove[i].CheckerReference == null) continue;
                myChe.RemoveCellsCollidingWith(toRemove[i].CheckerReference);
            }
        }

#if UNITY_EDITOR
        private UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            ToRemove.Switch_MinusOneReturnsMainField = false;
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                RemoveFrom.AllowDragWire = true;
                GUILayout.Space(1);

                if (sp == null) sp = baseSerializedObject.FindProperty("RemoveFrom");
                UnityEditor.SerializedProperty scp = sp.Copy();
                UnityEditor.EditorGUILayout.PropertyField(scp);
            }
            else
            {
                RemoveFrom.AllowDragWire = false;
            }
        }
#endif


    }
}