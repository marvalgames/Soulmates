using FIMSpace.Graph;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.BuildSetup
{

    public class PR_ExpandBoundsSize : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Expand Bounds Size" : "Expand Bounds Size"; }
        public override string GetNodeTooltipDescription { get { return "Doing operations on provided bounds."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(290, 140); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input)] public PGGUniversalPort BoundsToModify;

        public enum EOperation
        { 
            Scale, Expand, EncapsulatePosition
        }

        public EOperation OperationType = EOperation.Scale;


        [Port(EPortPinType.Input)] public PGGVector3Port OperationValue;
        [Tooltip("Use 'GetFieldBounds' node to read bounds data")]
        [Port(EPortPinType.Output)] public PGGUniversalPort FullBounds;

        public override void OnStartReadingNode()
        {
            BoundsToModify.TriggerReadPort(true);
            object inputVal = BoundsToModify.GetPortValueSafe;
            
            if (inputVal == null) return;
            if ((inputVal is Bounds) == false) return;

            OperationValue.TriggerReadPort(true);
            Vector3 portValue = OperationValue.GetInputValue;

            Bounds newBounds = (Bounds)inputVal;

            if ( OperationType == EOperation.Scale)
            {
                newBounds.size = Vector3.Scale(newBounds.size, portValue);
            }
            else if ( OperationType == EOperation.Expand)
            {
                newBounds.Expand(portValue);
            }
            else if (OperationType == EOperation.EncapsulatePosition)
            {
                newBounds.Encapsulate(portValue);
            }

            FullBounds.Variable.SetTemporaryReference(true, newBounds);
        }

    }
}