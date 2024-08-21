using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetBoundsParameter : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Bounds Parameter" : "Get Bounds Parameter (Read Bounds)"; }
        public override string GetNodeTooltipDescription { get { return "Quick calculate target bounds, choosed parameter"; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(0.3f, 0.5f, 0.75f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(210, 120 + _extraH); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        [Port(EPortPinType.Input, EPortValueDisplay.HideOnConnected, 1)] public PGGUniversalPort Bounds;
        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGUniversalPort Value;
        enum EBoundsCompute
        {
            Center, Width, Height, Depth, Diagonal, Min, Max,
            BackCenter, FrontCenter, LeftCenter, RightCenter, TopCenter, BottomCenter
        }

        [InspectorName("")]
        [SerializeField] private EBoundsCompute ComputeValue = EBoundsCompute.Center;

        [Tooltip("When computing 'Back Center' or others, you can get offseted position instead of bounds edge position")]
        [HideInInspector] [Port(EPortPinType.Input)] public FloatPort MarginOffset;


        public override void OnStartReadingNode()
        {
            Value.Variable.SetTemporaryReference(true, null);

            Bounds.TriggerReadPort(true);

            object val = Bounds.GetPortValueSafe;
            if (val == null) return;

            Bounds b = PGGUniversalPort.TryReadAsBounds(val);
            if (b.size == Vector3.zero) return;

            MarginOffset.TriggerReadPort(true);

            Value.Variable.SetTemporaryReference(false, null);
            Value.Variable.SetValue(ComputeOutputValue(b));
        }

        bool ComputingWithMargin()
        {
            return (int)ComputeValue >= 7;
        }

        object ComputeOutputValue(Bounds b)
        {
            float off = MarginOffset.GetInputValue;

            #region Center / Width / Height / Depth / Diagonal / Min / Max

            if (ComputeValue == EBoundsCompute.Center)
            {
                return b.center;
            }
            else if (ComputeValue == EBoundsCompute.Width)
            {
                return b.size.x;
            }
            else if (ComputeValue == EBoundsCompute.Height)
            {
                return b.size.y;
            }
            else if (ComputeValue == EBoundsCompute.Depth)
            {
                return b.size.z;
            }
            else if (ComputeValue == EBoundsCompute.Diagonal)
            {
                return (b.max - b.min).magnitude;
            }
            else if (ComputeValue == EBoundsCompute.Min)
            {
                return b.min;
            }
            else if (ComputeValue == EBoundsCompute.Max)
            {
                return b.max;
            }

            #endregion

            #region Centered Sides

            else if (ComputeValue == EBoundsCompute.FrontCenter)
            {
                return new Vector3(b.center.x, b.center.y, b.max.z - off);
            }
            else if (ComputeValue == EBoundsCompute.BackCenter)
            {
                return new Vector3(b.center.x, b.center.y, b.min.z + off);
            }
            else if (ComputeValue == EBoundsCompute.RightCenter)
            {
                return new Vector3(b.max.x - off, b.center.y, b.center.z);
            }
            else if (ComputeValue == EBoundsCompute.LeftCenter)
            {
                return new Vector3(b.min.x + off, b.center.y, b.center.z);
            }
            else if (ComputeValue == EBoundsCompute.TopCenter)
            {
                return new Vector3(b.center.x, b.max.y - off, b.center.z);
            }
            else if (ComputeValue == EBoundsCompute.BottomCenter)
            {
                return new Vector3(b.center.x, b.min.y + off, b.center.z);
            }

            #endregion

            return 0;
        }

        #region Editor Code

        int _extraH = 0;

#if UNITY_EDITOR
        private UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            _extraH = 0;

            if (ComputingWithMargin())
            {
                _extraH += 20;
                MarginOffset.AllowDragWire = true;
                GUILayout.Space(1);

                if (sp == null) sp = baseSerializedObject.FindProperty("MarginOffset");
                UnityEditor.SerializedProperty scp = sp.Copy();
                UnityEditor.EditorGUILayout.PropertyField(scp);
            }
            else
            {
                MarginOffset.AllowDragWire = false;
            }
        }
#endif

        #endregion


    }
}
