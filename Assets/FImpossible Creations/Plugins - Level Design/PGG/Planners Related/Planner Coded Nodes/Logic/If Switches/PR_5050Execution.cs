using FIMSpace.Graph;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Logic
{

    public class PR_5050Execution : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "If => " + ProbabilityString() + " Execute A or B" : "If 50% => Execute A or B"; }
        public override Color GetNodeColor() { return new Color(0.3f, 0.8f, 0.55f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2( 210,  92); } }
        public override bool DrawInputConnector { get { return true; } }
        public override bool DrawOutputConnector { get { return true; } }
        public override int OutputConnectorsCount { get { return 2; } }
        public override int HotOutputConnectionIndex { get { return 1; } }
        public override int AllowedOutputConnectionIndex { get { return outputId; } }

        string ProbabilityString()
        {
            return Mathf.Round((float)Probability.GetPortValueSafe * 100) + "%";
        }

        public override string GetOutputHelperText(int outputId = 0)
        {
            if (outputId == 0) return "A";
            return "B";
        }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Logic; } }

        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.Default)] public FloatPort Probability;

        static System.Random myRandom;

        public override void OnCustomPrepare()
        {
            myRandom = new System.Random(FGenerators.GetRandom(-1000,1000));
        }

        public override void OnCreated()
        {
            base.OnCreated();
            Probability.Value = 0.5f;
        }

        int outputId = 0;


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            Probability.TriggerReadPort(true);
            float prob = (float)Probability.GetPortValueSafe;
            prob = Mathf.Clamp01(prob);

            if ( (float)myRandom.NextDouble() <= prob )
            {
                outputId = 1; 
            }
            else
            {
                outputId = 0;
            }
           
            if (Debugging)
            {
                DebuggingInfo = "Checking probability " + ProbabilityString() + "  Executing Port = " + outputId;
            }
        }

#if UNITY_EDITOR
        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            GUILayout.Label("Probablity: " + Probability.GetPortValueSafe);
        }

        //SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();
            Probability.Value = Mathf.Clamp01(Probability.Value);
            baseSerializedObject.ApplyModifiedProperties();
        }
#endif

    }
}