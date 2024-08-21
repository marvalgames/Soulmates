using FIMSpace.Graph;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.BuildSetup
{

    public class PR_GenerateBounds : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Generate Bounds"; }
        public override string GetNodeTooltipDescription { get { return "Generating bounds rectangle basing on the provided parameters."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(250, 120); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input)] public PGGVector3Port Center;
        [Port(EPortPinType.Input)] public PGGVector3Port Size;

        [Tooltip("Use 'GetFieldBounds' node to read bounds data")]
        [Port(EPortPinType.Output)] public PGGUniversalPort Bounds;

        public override void OnStartReadingNode()
        {
            Center.TriggerReadPort( true );
            Size.TriggerReadPort( true );
            Bounds b = new Bounds( Center.GetInputValue, Size.GetInputValue);
            Bounds.Variable.SetTemporaryReference(true, b);
        }
    }
}