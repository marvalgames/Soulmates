using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions
{

    public class PR_GetCollidingCells : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? (Get == EGetType.Colliding ? "Get Colliding Cells" : "Get Not Colliding Cells") : "Get Colliding \\ Not Colliding Cell"; }
        public override string GetNodeTooltipDescription { get { return "Get self cels which are colliding or the ones which are not colliding with other provided fields"; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(230, _EditorFoldout ? 204 : 142); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }



        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port(EPortPinType.Input)] public PGGPlannerPort CellsOf;
        [Port(EPortPinType.Input)] public PGGPlannerPort CollidingWith;
        [Port(EPortPinType.Output)] public PGGPlannerPort SelectedCells;
        enum EGetType {  Colliding, NotColliding }
        [SerializeField] private EGetType Get = EGetType.Colliding;


        public override void OnStartReadingNode()
        {
            CellsOf.TriggerReadPort(true);
            CollidingWith.TriggerReadPort(true);

            SelectedCells.Clear();
            SelectedCells.Switch_DisconnectedReturnsByID = false;
            SelectedCells.Switch_MinusOneReturnsMainField = false;

            ICheckerReference ofChecker = CellsOf.Get_CheckerReference;

            if (ofChecker == null) return;
            if (ofChecker.CheckerReference == null) return;

            CheckerField3D collectedCells = new CheckerField3D();
            collectedCells.CopyParamsFrom(ofChecker.CheckerReference);

            var collides = CollidingWith.Get_GetMultipleCheckers;

            for (int i = 0; i < collides.Count; i++)
            {
                if ( Get == EGetType.Colliding)
                {
                    var coll = ofChecker.CheckerReference.GetCollisionCells(collides[i].CheckerReference);
                    for (int c = 0; c < coll.Count; c++) collectedCells.AddLocal(coll[c].Pos);
                }
                else if (Get == EGetType.NotColliding)
                {
                    var coll = ofChecker.CheckerReference.GetCellsNotCollidingWith(collides[i].CheckerReference);
                    for (int c = 0; c < coll.Count; c++) collectedCells.AddLocal(coll[c].Pos);
                }
            }

            SelectedCells.Output_Provide_Checker(collectedCells);
        }

    }
}