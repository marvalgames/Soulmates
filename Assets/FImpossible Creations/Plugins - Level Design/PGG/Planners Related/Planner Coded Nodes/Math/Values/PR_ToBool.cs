using FIMSpace.Graph;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Math.Values
{

    public class PR_ToBool : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "To Bool" : "Convert Value to Bool (to True\\False)"; }
        public override string GetNodeTooltipDescription { get { return "Convert any port value to the bool value"; } }
        public override Color GetNodeColor() { return new Color(0.3f, 0.5f, 0.75f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 200 : 158, _EditorFoldout ? 104 : 84); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Math; } }

        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.HideOnConnected, 1)] public PGGUniversalPort Value;
        [HideInInspector][Port(EPortPinType.Output, true)] public BoolPort IsNull;
        [Tooltip("Enable if you want to output true instead of false or false instead of true value with the output port")]
        [HideInInspector] public bool InvertBoolValue = false;

        public override void OnStartReadingNode()
        {
            Value.TriggerReadPort(true);
            var val = Value.GetPortValueSafe;

            bool mainResult = FGenerators.NotNull(val); // If value is null -> false

            if (mainResult) // Not null, so let's check what is it
            {
                if (val is PGGCellPort.Data)
                {
                    mainResult = FGenerators.NotNull(((PGGCellPort.Data)val).CellRef);
                }
                if (val is bool) mainResult = (bool)val;
                else if (val is System.Single) mainResult = (Single)val > 0;
            }

            if (InvertBoolValue) mainResult = !mainResult;
            IsNull.Value = mainResult;
        }

#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            //if (InvertBoolValue) IsNull.DisplayName = "Is Not Null"; else IsNull.DisplayName = "Is Null";

            baseSerializedObject.Update();
            if (sp == null) sp = baseSerializedObject.FindProperty("Value");


            EditorGUIUtility.labelWidth = 1;
            EditorGUILayout.PropertyField(sp, GUILayout.Width(NodeSize.x - 80));
            EditorGUIUtility.labelWidth = 0;

            SerializedProperty spc = sp.Copy(); spc.Next(false);
            GUILayout.Space(-19);
            EditorGUILayout.PropertyField(spc);

            if (_EditorFoldout)
            {
                spc.Next(false);
                EditorGUILayout.PropertyField(spc);
            }

            baseSerializedObject.ApplyModifiedProperties();

        }
#endif

    }
}