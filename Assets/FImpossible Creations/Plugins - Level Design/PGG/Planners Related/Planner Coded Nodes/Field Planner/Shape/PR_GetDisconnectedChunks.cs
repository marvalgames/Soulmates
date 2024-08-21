using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.BuildSetup
{

    public class PR_GetDisconnectedChunks : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Get Disconnected chunks" : "Get Disconnected chunks (fill)"; }
        public override string GetNodeTooltipDescription { get { return "If grid is generated out of multiple chunks\\islands, then collecting all chunks into cell groups."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(240, _EditorFoldout ? 121 : 101); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return true; } }


        [Port(EPortPinType.Input, 1)] public PGGPlannerPort CheckOn;
        [Port(EPortPinType.Output)] public PGGPlannerPort Chunks;
        [HideInInspector] public bool DontReturnSingleChunk = false;

        List<FieldCell> checkPool = new List<FieldCell>();
        List<CheckerField3D> collected;

        public override void OnStartReadingNode()
        {
            Chunks.Clear();
            Chunks.Switch_DisconnectedReturnsByID = false;
            Chunks.Switch_MinusOneReturnsMainField = false;
            Chunks.Switch_ReturnOnlyCheckers = true;

            CheckOn.TriggerReadPort(true);
            CheckerField3D mainChecker = PGGPlannerPort.GetCheckerFromPort(CheckOn, false);

            if (mainChecker == null) return;

            checkPool.Clear();
            PGGUtils.TransferFromListToList(mainChecker.AllCells, checkPool, false);
            collected = new List<CheckerField3D>();

            while (checkPool.Count > 0)
            {
                CheckerField3D chunkChe = new CheckerField3D();
                chunkChe.CopyParamsFrom(mainChecker);

                FieldCell originCell = checkPool[0];
                var fillCells = FieldCell.GatherFillPopulateCells(mainChecker.Grid, originCell, null);

                for (int i = 0; i < fillCells.Count; i++)
                {
                    checkPool.Remove(fillCells[i]);
                    chunkChe.AddLocal(fillCells[i].Pos);
                }

                if (checkPool.Contains(originCell)) checkPool.Remove(originCell); // ensure removing origin cell

                if (chunkChe.AllCells.Count > 0) collected.Add(chunkChe);
            }

            if (_EditorDebugMode)
            {
                FieldPlanner db = GetPlannerFromPort(CheckOn, false);
                UnityEngine.Debug.Log("Collected: " + collected.Count + " out of " + db?.ArrayNameString);
            }

            if (collected.Count > (DontReturnSingleChunk ? 1 : 0) ) Chunks.Output_Provide_CheckerList(collected);
        }



#if UNITY_EDITOR
        UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            Chunks.Editor_DefaultValueInfo = "(None)";
            base.Editor_OnNodeBodyGUI(setup);

            baseSerializedObject.Update();

            if (_EditorFoldout)
            {
                GUILayout.Space(1);
                if (sp == null) sp = baseSerializedObject.FindProperty("DontReturnSingleChunk");
                UnityEditor.EditorGUILayout.PropertyField(sp, true);
            }

            baseSerializedObject.ApplyModifiedProperties();
        }


#endif

    }
}