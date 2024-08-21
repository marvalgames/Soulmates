using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Logic
{

    public class PR_IsNull : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Is Null?" : "If Is Null => return True or False"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.8f, 0.55f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(160, 80); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Logic; } }

        [HideInInspector] [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideValue, "To Check", 1)] public PGGUniversalPort AValue;
        [HideInInspector] [Port(EPortPinType.Output, true)] public BoolPort Output;

        public override void OnStartReadingNode()
        {
            AValue.TriggerReadPort(true);
            Output.Value = (AValue.GetPortValueSafe == null);
        }


        #region Editor Code
#if UNITY_EDITOR

        SerializedProperty sp_A = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();

            if (sp_A == null) sp_A = baseSerializedObject.FindProperty("AValue");

            EditorGUILayout.PropertyField(sp_A, GUILayout.Width(NodeSize.x - 84));

            SerializedProperty sp = sp_A.Copy(); sp.Next(false);

            GUILayout.Space(-21);
            GUILayout.BeginHorizontal();
            GUILayout.Space(19);
            EditorGUILayout.PropertyField(sp, GUIContent.none);
            GUILayout.EndHorizontal();

            baseSerializedObject.ApplyModifiedProperties();

        }

#endif
        #endregion


    }
}