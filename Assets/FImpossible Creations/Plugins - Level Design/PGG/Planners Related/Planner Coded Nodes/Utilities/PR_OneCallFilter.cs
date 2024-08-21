using FIMSpace.Graph;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Utilities
{

    public class PR_OneCallFilter : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "     Redirect Value" : "Single Call Filter (Redirect Value)"; }
        public override string GetNodeTooltipDescription { get { return "Calling execution on the input port just once, or just forwarding value. Then providing read value for other nodes.\nUseful when using nodes which ports are triggering calculations."; } }
        public override Color GetNodeColor() { return new Color(0.4f, 0.4f, 0.4f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 198 : 158, (_EditorFoldout ? 104 : 84)); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Debug; } }

        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.HideOnConnected, 1)] public PGGUniversalPort In;
        [HideInInspector][Port(EPortPinType.Output, true)] public PGGUniversalPort Out;
        [HideInInspector] public bool DontCallEvenOnce = false;

        FieldPlanner _wasCalledBy = null;

        public override void PreGeneratePrepare()
        {
            _wasCalledBy = null;
        }

        public override void OnCustomReadNode()
        {
            Out.Variable.SetTemporaryReference(true, In);
            //Out.Variable.SetValue(In.GetPortValueSafe);
        }

        public override void OnStartReadingNode()
        {
            if (CurrentExecutingPlanner == _wasCalledBy)
            {
                Out.Variable.SetTemporaryReference(true, In);
                //Out.Variable.SetValue(In.GetPortValueSafe);
                return;
            }

            _wasCalledBy = CurrentExecutingPlanner;

            if (!DontCallEvenOnce) In.TriggerReadPort(true);

            Out.Variable.SetTemporaryReference(true, In);
            //Out.Variable.SetValue(In.GetPortValueSafe);
        }


#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (sp == null) sp = baseSerializedObject.FindProperty("In");
            EditorGUIUtility.labelWidth = 1;
            EditorGUILayout.PropertyField(sp, GUILayout.Width(NodeSize.x - 80));
            EditorGUIUtility.labelWidth = 0;

            SerializedProperty spc = sp.Copy(); spc.Next(false);
            GUILayout.Space(-19);
            EditorGUILayout.PropertyField(spc);

            if (_EditorFoldout)
            {
                baseSerializedObject.Update();
                spc.Next(false);
                EditorGUILayout.PropertyField(spc);
                baseSerializedObject.ApplyModifiedProperties();
            }
        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            EditorGUILayout.LabelField("Forwarding Value: " + In.GetPortValueSafe);
            if (In.GetPortValueSafe is FieldPlanner) EditorGUILayout.LabelField((In.GetPortValueSafe as FieldPlanner).ArrayNameString);
            //EditorGUILayout.LabelField("Out: " + Out.GetPortValueSafe);
        }

#endif

    }
}