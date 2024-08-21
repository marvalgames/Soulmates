using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.SpecificSolutions
{

    public class PR_ApplyPerlinNoiseOffset : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Perlin Noise Offset" : "Apply Perlin Noise Height Offset"; }
        public override bool IsFoldable { get { return false; } }
        public override string GetNodeTooltipDescription { get { return "Changing Y position of cells with use of perlin noise function"; } }
        public override Color GetNodeColor() { return new Color(0.3f, 0.825f, 0.6f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(230, _EditorFoldout ? 148 : 146); } }


        [Tooltip("Shape to be cutted out of the 'Planner' shape")]
        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ApplyTo;

        [Port(EPortPinType.Input, 1)] public IntPort YLevels;
        [Port(EPortPinType.Input, 1)] public FloatPort PerlinScale;

        public bool RefreshOnExecute = true;
        bool wasExec = false;
        int xRandomOffset;
        int zRandomOffset;

        public override void OnCreated()
        {
            base.OnCreated();
            YLevels.Value = 1;
            PerlinScale.Value = 0.5f;
        }

        public override void PreGeneratePrepare()
        {
            base.PreGeneratePrepare();
            wasExec = false;
        }

        void RefreshRandomOffset()
        {
            xRandomOffset = FGenerators.GetRandom(-1000, 1000);
            zRandomOffset = FGenerators.GetRandom(-1000, 1000);
        }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            ApplyTo.TriggerReadPort(true);
            FieldPlanner plan = GetPlannerFromPort(ApplyTo, false);
            CheckerField3D myChe = ApplyTo.GetInputCheckerSafe;
            if (myChe == null) { return; }

            if (RefreshOnExecute)
            {
                RefreshRandomOffset();
            }
            else
            {
                if (!wasExec)
                {
                    wasExec = true;
                    RefreshRandomOffset();
                }
            }

            YLevels.TriggerReadPort(true);
            PerlinScale.TriggerReadPort(true);

            List<Vector3Int> toRemove = new List<Vector3Int>();

            for (int i = myChe.AllCells.Count - 1; i >= 0; i--)
            {
                Vector3Int cellPos = myChe.AllCells[i].Pos;
                Vector3 wPos = myChe.GetWorldPos(cellPos);

                float perlinX = xRandomOffset + wPos.x * PerlinScale.GetInputValue;
                float perlinZ = zRandomOffset + wPos.z * PerlinScale.GetInputValue;

                float perlinNoiseValue = Mathf.PerlinNoise(perlinX, perlinZ);
                cellPos.y += Mathf.RoundToInt(perlinNoiseValue * YLevels.GetInputValue);

                if (cellPos != myChe.AllCells[i].Pos)
                {
                    toRemove.Add(myChe.AllCells[i].Pos);
                    var nCell = myChe.AddLocal(cellPos);
                    nCell.cellCustomData = myChe.AllCells[i].cellCustomData;

                    if (plan != null) if (plan.LatestResult != null)
                        {
                            for (int g = 0; g < plan.LatestResult.CellsInstructions.Count; g++)
                            {
                                if (plan.LatestResult.CellsInstructions[g].pos == myChe.AllCells[i].Pos)
                                {
                                    plan.LatestResult.CellsInstructions[g].pos = nCell.Pos;
                                }
                            }
                        }
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                myChe.RemoveLocal(toRemove[i]);
            }
        }


        #region Editor Class
#if UNITY_EDITOR

        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
        }

#endif
        #endregion


    }
}