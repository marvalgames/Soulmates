using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Checker;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Access
{

    public class PR_ChooseNeightbourFields : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Choose Neightbour Fields" : "Choose Neightbour Fields"; }
        public override string GetNodeTooltipDescription { get { return "Getting fields which are aligning with each other (not separated by a single cell)"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(1.0f, 0.75f, 0.25f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(260, _EditorFoldout ? 184 : 164); } }
        public override bool IsFoldable { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        [Port(EPortPinType.Input)] public PGGPlannerPort ChoosePool;
        [Port(EPortPinType.Output)] public PGGPlannerPort Selected;

        [Port(EPortPinType.Input)] public IntPort MaxConnected;
        [Port(EPortPinType.Input)] public IntPort MaxCellsCount;
        [Port(EPortPinType.Input)] public FloatPort MaxBoundsSize;

        [Tooltip("Choose neightbours with most alignment points")]
        [HideInInspector] public bool PrioritizeAlignmentCount = false;
        [HideInInspector][Port(EPortPinType.Input, EPortValueDisplay.NotEditable)] public BoolPort ExtraCondition;
        [HideInInspector][Port(EPortPinType.Output)] public PGGPlannerPort CheckedNeightbour;

        public override void OnCreated()
        {
            base.OnCreated();

            MaxConnected.Value = 2;
            MaxCellsCount.Value = 0;
            MaxBoundsSize.Value = 0;
        }

        List<FieldPlanner> selectedFields = new List<FieldPlanner>();
        List<CheckerField3D> selectedCheckers = new List<CheckerField3D>();

        public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        {
            // Call only when reading 'Selected' neightbour field port!
            if (port != Selected) return;

            selectedFields.Clear();
            selectedCheckers.Clear();
            ExtraCondition.Value = false;

            Selected.Clear();
            Selected.Switch_MinusOneReturnsMainField = false;
            Selected.Switch_DisconnectedReturnsByID = false;

            ChoosePool.TriggerReadPort(true);

            if (MaxConnected.IsConnected) MaxConnected.TriggerReadPort(true);
            int maxConn = MaxConnected.GetInputValue;
            int limitCellsCount = 0;
            float limitBoundsSize = 0;
            if (MaxCellsCount.IsConnected) { MaxCellsCount.TriggerReadPort(true); limitCellsCount = MaxCellsCount.GetInputValue; if (limitCellsCount < 2) limitCellsCount = 0; }
            if (MaxBoundsSize.IsConnected) { MaxBoundsSize.TriggerReadPort(true); limitBoundsSize = MaxBoundsSize.GetInputValue; if (limitBoundsSize < 0.1f) limitBoundsSize = 0; }

            //bool useExtraCondition = ExtraCondition.IsConnected;
            var checkers = ChoosePool.Get_GetMultipleCheckers;

            if (checkers.Count > 0)
            {
                generatedChunks.Clear();
                PGGUtils.TransferFromListToList(checkers, checkersPool);
                checkersPool.Reverse();

                while (checkersPool.Count > 0)
                {
                    NeightbourChunk chunk = new NeightbourChunk();

                    var parent = checkersPool[checkersPool.Count - 1];
                    checkersPool.RemoveAt(checkersPool.Count - 1);

                    chunk.Checkers.Add(parent);

                    for (int i = checkersPool.Count - 1; i >= 0; i--)
                    {
                        if (PrioritizeAlignmentCount)
                        {
                            int mostAlignP = 0;
                            ICheckerReference best = null;

                            for (int a = checkersPool.Count - 1; a >= 0; a--)
                            {
                                int aligns = parent.CheckerReference.CountAlignmentsWith(checkersPool[a].CheckerReference);
                                if (aligns > mostAlignP)
                                {
                                    mostAlignP = aligns;
                                    best = checkersPool[a];
                                }
                            }

                            if (best != null)
                            {
                                checkersPool.Remove(best);
                                chunk.Checkers.Add(best);
                            }
                        }
                        else
                        {
                            var check = checkersPool[i];

                            if (parent.CheckerReference.IsAnyAligning(check.CheckerReference))
                            {
                                checkersPool.RemoveAt(i);
                                chunk.Checkers.Add(check);
                            }
                        }

                        if (chunk.Checkers.Count >= maxConn) break;
                        if (limitCellsCount > 0) if (chunk.CalculateCellsCount() >= limitCellsCount) break;
                        if (limitBoundsSize > 0) if (chunk.CalcluateBoundsSize() >= limitBoundsSize) break;
                    }

                    if (chunk.Checkers.Count > 0) generatedChunks.Add(chunk);
                }

                NeightbourChunk biggest = null;

                float biggestS = 0f;
                for (int i = 0; i < generatedChunks.Count; i++)
                {
                    float siz = generatedChunks[i].CalcluateBoundsSize();
                    if (generatedChunks[i].Checkers.Count > 1) siz *= 100;

                    if (siz > biggestS)
                    {
                        biggest = generatedChunks[i];
                        biggestS = siz;
                    }
                }

                if (biggest != null)
                {
                    Selected.Output_Provide_CheckerReferenceList(biggest.Checkers);
                }
            }

        }

        List<ICheckerReference> checkersPool = new List<ICheckerReference>();
        List<NeightbourChunk> generatedChunks = new List<NeightbourChunk>();
        class NeightbourChunk
        {
            public List<ICheckerReference> Checkers = new List<ICheckerReference>();

            public float CalcluateBoundsSize()
            {
                float size = 0f;
                for (int i = 0; i < Checkers.Count; i++) size += Checkers[i].CheckerReference.GetFullBoundsWorldSpace().size.magnitude;
                return size;
            }

            public int CalculateCellsCount()
            {
                int cells = 0;
                for (int i = 0; i < Checkers.Count; i++) cells += Checkers[i].CheckerReference.ChildPositionsCount;
                return cells;
            }
        }



        #region Editor Code

#if UNITY_EDITOR
        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            baseSerializedObject.Update();

            if (MaxConnected.Value < 2) MaxConnected.Value = 2;
            if (MaxCellsCount.Value < 0) MaxCellsCount.Value = 0;
            if (MaxBoundsSize.Value < 0) MaxBoundsSize.Value = 0;

            if (MaxCellsCount.IsNotConnected && MaxCellsCount.Value < 2)
                MaxCellsCount.OverwriteName = "Max Cells (off) ";
            else
                MaxCellsCount.OverwriteName = "Max Cells Count";

            if (MaxBoundsSize.IsNotConnected && MaxBoundsSize.Value < 0.1f)
                MaxBoundsSize.OverwriteName = "Max Bounds Size (off)";
            else
                MaxBoundsSize.OverwriteName = "Max Bounds Size";

            baseSerializedObject.ApplyModifiedProperties();

            base.Editor_OnNodeBodyGUI(setup);

            ExtraCondition.AllowDragWire = false;
            CheckedNeightbour.AllowDragWire = false;

            if (_EditorFoldout)
            {
                baseSerializedObject.Update();

                if (sp == null) sp = baseSerializedObject.FindProperty("PrioritizeAlignmentCount");
                var spc = sp.Copy();

                EditorGUIUtility.labelWidth = 180;
                EditorGUILayout.PropertyField(spc); spc.Next(false);
                EditorGUIUtility.labelWidth = 0;
                //EditorGUILayout.PropertyField(spc);

                //ExtraCondition.AllowDragWire = true;
                //CheckedNeightbour.AllowDragWire = true;

                baseSerializedObject.ApplyModifiedProperties();

            }
        }

#endif

        #endregion

    }
}