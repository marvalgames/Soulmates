using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_GetFieldBounds : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return created ? "Get Bounds" : "Get Field Planner Bounds"; }
        public override string GetNodeTooltipDescription { get { return "Get bounds parameters of choosed field (world space unit size!)"; } }
        [HideInInspector] public bool created = false;

        public override void OnCreated()
        {
            created = true;
            base.OnCreated();
        }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(210, _EditorFoldout ? 240 : 140); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }

        [Port(EPortPinType.Input, EPortValueDisplay.HideOnConnected, 1)] public PGGPlannerPort Planner;
        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGVector3Port Center;
        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGVector3Port Size;

        [Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public FloatPort Diagonal;

        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.NotEditable, "Width (X)")] public FloatPort Width;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.NotEditable, "Height (Y)")] public FloatPort Height;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.NotEditable, "Depth (Z)")] public FloatPort Depth;

        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGVector3Port Min;
        [HideInInspector][Port(EPortPinType.Output, EPortValueDisplay.NotEditable)] public PGGVector3Port Max;

        public override void OnStartReadingNode()
        {
            CheckerField3D planner = PGGPlannerPort.GetCheckerFromPort(Planner);
            Bounds b = new Bounds();

            if (planner == null || planner == CurrentExecutingPlanner.LatestChecker)
            {
                object val = Planner.GetInputValueSafe;

                if (val is Bounds)
                {
                    b = (Bounds)val;
                }
                else
                {
                    if (planner != null) b = planner.GetFullBoundsWorldSpace();
                }
            }
            else
            {
                if (planner != null) b = planner.GetFullBoundsWorldSpace();
            }

            if (Planner.IsConnected)
            {
                var baseConn = Planner.BaseConnection;
                if (baseConn != null && baseConn.PortReference != null)
                {
                    object portVal = baseConn.PortReference.GetPortValue;
                    if (portVal is Bounds)
                    {
                        b = (Bounds)portVal;
                        planner = CurrentExecutingPlanner.LatestChecker;
                    }
                }

                var mCheckers = PGGPlannerPort.GetCheckersFromPort(Planner, false);
                if (mCheckers != null)
                    if (mCheckers.Count > 1)
                    {
                        foreach (var item in mCheckers)
                        {
                            b.Encapsulate(item.CheckerReference.GetFullBoundsWorldSpace());
                        }
                    }
            }

            if (planner != null)
            {
                Center.Value = b.center;
                Size.Value = b.size;
                Size.Value = b.size;
                Diagonal.Value = (b.max - b.min).magnitude;

                Width.Value = b.size.x;
                Depth.Value = b.size.z;
                Height.Value = b.size.y;

                Min.Value = b.min;
                Max.Value = b.max;
            }
        }



#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (_EditorFoldout)
            {
                if (sp == null) sp = baseSerializedObject.FindProperty("Width");

                SerializedProperty s = sp.Copy();
                EditorGUILayout.PropertyField(s); s.Next(false);
                EditorGUILayout.PropertyField(s); s.Next(false);
                EditorGUILayout.PropertyField(s); s.Next(false);
                EditorGUILayout.PropertyField(s); s.Next(false);
                EditorGUILayout.PropertyField(s);
            }
        }
#endif

    }
}
