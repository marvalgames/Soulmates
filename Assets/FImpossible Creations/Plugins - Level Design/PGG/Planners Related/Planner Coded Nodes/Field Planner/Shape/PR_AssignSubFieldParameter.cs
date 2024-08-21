using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_AssignSubFieldParameter : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Assign Sub Field Param" : "Assign Sub Field Parameter"; }
        public override string GetNodeTooltipDescription { get { return "Setting some variables specific for the sub fields."; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override bool IsFoldable { get { return false; } }

        public override Vector2 NodeSize { get { return new Vector2(210, _EditorFoldout ? 106 : 106); } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort Subfield;

        [Tooltip("If sub-field should use different field setup than main Field Planner instance you can provide FieldSetup type object here")]
        [Port(EPortPinType.Input, 1)] public PGGUniversalPort SetFieldSetup;

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            if (Subfield.IsNotConnected) return;

            var oChe = GetPlannersFromPort(Subfield);
            if (oChe == null) { return; }
            if (oChe.Count == 0) { return; }

            SetFieldSetup.TriggerReadPort(true);
            FieldSetup setp = SetFieldSetup.GetPortValue as FieldSetup;
            if (setp == null) return;

            for (int o = 0; o < oChe.Count; o++)
            {
                var sub = oChe[o];
                if (sub == null) continue;
                sub.DefaultFieldSetup = setp;
            }

        }

#if UNITY_EDITOR

        //private UnityEditor.SerializedProperty sp = null;
        //public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        //{
        //    base.Editor_OnNodeBodyGUI(setup);

        //    if (_EditorFoldout)
        //    {
        //        baseSerializedObject.Update();
        //        if (sp == null) sp = baseSerializedObject.FindProperty("SetFieldSetup");
        //        EditorGUILayout.PropertyField(sp);
        //        baseSerializedObject.ApplyModifiedProperties();
        //    }

        //}

#endif

    }
}