using FIMSpace.Graph;
using UnityEngine;
using FIMSpace.Generating.Checker;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{

    public class PR_SelectCellsOnEdge : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Select Cells On Edge" : "Select Cells On The Side Edge"; }
        public override string GetNodeTooltipDescription { get { return "Selecting cells which are on the side or on the extreme edge on the provided side"; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(240, 140); } }
        public override bool IsFoldable { get { return false; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort Cells;
        [Port(EPortPinType.Input, 1)] public PGGVector3Port Side;
        [Tooltip("If checker shape creates multiple edges on the side, algoritm will use one edge, the one in farthest position in desired direction.")]
        public bool JustExtremeSide = false;
        [Port(EPortPinType.Output, 1)] public PGGPlannerPort SelectedCells;

        public override void OnStartReadingNode()
        {
            SelectedCells.Clear();
            SelectedCells.Switch_ReturnOnlyCheckers = true;
            SelectedCells.Switch_MinusOneReturnsMainField = false;

            Side.TriggerReadPort(true);
            Vector3 side = Side.GetInputValue;

            if (side == Vector3.zero) return;

            Cells.TriggerReadPort(true);

            var checkerRef = Cells.Get_CheckerReference;
            if (checkerRef == null) return;
            var checker = checkerRef.CheckerReference;
            if (checker == null) return;
            if (checker.ChildPositionsCount == 0) return;

            // Prepare container for the cells
            CheckerField3D selectedCells = new CheckerField3D();
            selectedCells.CopyParamsFrom(checker);

            side = FVectorMethods.ChooseDominantAxis(side).normalized;
            Vector3 sideWorld = checker.ScaleV3(side);

            for (int i = 0; i < checker.ChildPositionsCount; i++)
            {
                Vector3 checkPos = checker.GetWorldPos(i);
                checkPos += sideWorld;
                if (!checker.ContainsWorld(checkPos)) selectedCells.AddLocal(checker.GetLocalPos(i));
            }

            if (selectedCells.ChildPositionsCount == 0) return;

            if (JustExtremeSide )
            {
                Bounds gBounds = new Bounds(selectedCells.GetWorldPos(selectedCells.AllCells[0]), Vector3.zero);
                for (int i = 0; i < selectedCells.ChildPositionsCount; i++) gBounds.Encapsulate(selectedCells.GetWorldPos(selectedCells.AllCells[i]));

                float edgeVal = 0f;
                if (side.x > 0) edgeVal = gBounds.max.x;
                else if (side.x < 0) edgeVal = gBounds.min.x;
                else if (side.z > 0) edgeVal =  gBounds.max.z;
                else if (side.z < 0) edgeVal =  gBounds.min.z;
                else if (side.y > 0) edgeVal =  gBounds.max.y;
                else if (side.y < 0) edgeVal =  gBounds.min.y;

                #region Commented but can be helpful later
                //if ( side.x > 0) edgePos = new Vector3(gBounds.max.x, 0, 0);
                //else if ( side.x < 0) edgePos = new Vector3(gBounds.min.x, 0, 0);
                //else if ( side.z > 0) edgePos = new Vector3(0, 0, gBounds.max.z);
                //else if ( side.z < 0) edgePos = new Vector3(0, 0, gBounds.min.z);
                //else if ( side.y > 0) edgePos = new Vector3(0, gBounds.max.y, 0);
                //else if ( side.y < 0) edgePos = new Vector3(0, gBounds.min.y, 0);
                #endregion

                if (_toRemove == null) _toRemove = new List<FieldCell>();
                else _toRemove.Clear();

                float rootScaleRange = ExtractAxisValue(selectedCells.RootScale, side) * 0.4f;

                for (int i = 0; i < selectedCells.ChildPositionsCount; i++)
                {
                    float eVal = ExtractAxisValue( selectedCells.GetWorldPos(i), side);
                    if (Mathf.Abs(eVal - edgeVal) > rootScaleRange) _toRemove.Add(selectedCells.AllCells[i]);
                }

                for (int i = 0; i < _toRemove.Count; i++)
                {
                    selectedCells.RemoveLocal(_toRemove[i].Pos);
                }
            }

            if (selectedCells.ChildPositionsCount == 0) return;
            SelectedCells.Output_Provide_Checker(selectedCells);
        }

        float ExtractAxisValue(Vector3 pos, Vector3 side)
        {
            if (side.x != 0) return pos.x;
            else if (side.z != 0) return pos.z;
            else if (side.y != 0) return pos.y;
            return 0f;
        }

        static List<FieldCell> _toRemove = null;

#if UNITY_EDITOR
        //private SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (Side.IsNotConnected && Side.Value == Vector3.zero)
            {
                EditorGUILayout.HelpBox("With Side = 0 node will do nothing!", MessageType.None);
            }
        }

#endif

    }
}