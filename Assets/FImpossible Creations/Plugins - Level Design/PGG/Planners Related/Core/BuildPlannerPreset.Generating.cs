using FIMSpace.Generating.Checker;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning
{

    public class PlanGenerationPrint
    {

        public List<PlannerResult> PlannerResults = new List<PlannerResult>();
        public CheckerField3D _debugLatestExecuted = null;
        public string DebugInfo = "";
        public Action DebugGizmosAction = null;

        public PlanGenerationPrint Copy()
        {
            PlanGenerationPrint copy = (PlanGenerationPrint)MemberwiseClone();

            copy.PlannerResults = new List<PlannerResult>();
            for (int i = 0; i < PlannerResults.Count; i++)
            {
                copy.PlannerResults.Add(PlannerResults[i].Copy());
            }

            if (_debugLatestExecuted != null) copy._debugLatestExecuted = _debugLatestExecuted.Copy();

            return (copy);
        }

        public int InitialSeed { get; internal set; }

        #region Utilities

        //public static Color GeneratePlannerColor(BuildPlannerPreset buildPlanner, FieldPlanner plan)
        //{
        //    return GeneratePlannerColor(CountPlannerForColor(buildPlanner, plan));
        //}
        public static Color GeneratePlannerColor(float i, float sat = 0.9f, float val = 0.9f)
        {
            float hue = ((float)i) * 0.094f + 0.475f;
            if (hue < 1.35f) if (hue > 0.61f) hue -= 0.61f;

            return Color.HSVToRGB(hue % 1, sat, val);
        }
        public static float CountPlannerForColor(BuildPlannerPreset buildPlanner, FieldPlanner plan)
        {
            if (plan == null) return 0f;
            if (buildPlanner == null) return 0f;
            if (buildPlanner.BasePlanners == null) return 0f;

            for (int p = 0; p < buildPlanner.BasePlanners.Count; p++)
            {
                int i = 0;

                float modColStep = 0.1f;
                if (buildPlanner.BasePlanners != null) modColStep = 1f / (float)buildPlanner.BasePlanners.Count;

                if (plan == buildPlanner.BasePlanners[p]) return (float)i * modColStep;
                i++;
            }

            return 0f;
        }

        public Bounds GetFullBounds(bool ignoreNotYetExecuted = true)
        {
            Bounds? b = null;

            for (int i = 0; i < PlannerResults.Count; i++)
            {
                Bounds bnds = PlannerResults[i].GetBounds();
                if (b == null) b = bnds; else b.Value.Encapsulate(bnds);
            }

            if (b == null) b = new Bounds();

            return b.Value;
        }

        public CheckerField3D StoredFullMask = null;
        public Bounds StoredFullBounds;


        List<CheckerField3D> _checkersList = new List<CheckerField3D>();
        public List<CheckerField3D> GetCurrentCheckersList(FieldPlanner toIgnore = null)
        {
            _checkersList.Clear();

            for (int i = 0; i < PlannerResults.Count; i++)
            {
                if (PlannerResults[i].ParentFieldPlanner.WasPreExecuted == true)
                {
                    if (PlannerResults[i].ParentFieldPlanner != toIgnore)
                        _checkersList.Add(PlannerResults[i].Checker);
                }

                if (PlannerResults[i].DuplicateResults != null)
                    for (int d = 0; d < PlannerResults[i].DuplicateResults.Count; d++)
                    {
                        var dupl = PlannerResults[i].DuplicateResults[d];
                        if (dupl.ParentFieldPlanner.WasPreExecuted == false) continue;
                        if (dupl.ParentFieldPlanner == toIgnore) continue;
                        if (PlannerResults[i].ParentFieldPlanner == toIgnore) continue;

                        _checkersList.Add(PlannerResults[i].DuplicateResults[d].Checker);
                    }
            }

            return _checkersList;
        }

        public List<CheckerField3D> GetCurrentCheckersListInNearRelationTo(CheckerField3D relationTo, float maxDistance = 1f)
        {
            _checkersList.Clear();

            for (int i = 0; i < PlannerResults.Count; i++)
            {
                if (PlannerResults[i].ParentFieldPlanner.WasPreExecuted == true)
                {
                    if (PlannerResults[i].Checker != relationTo)
                    {
                        if (PlannerResults[i].Checker.BoundsDistanceTo(relationTo) <= maxDistance)
                            _checkersList.Add(PlannerResults[i].Checker);
                    }
                }

                if (PlannerResults[i].DuplicateResults != null)
                    for (int d = 0; d < PlannerResults[i].DuplicateResults.Count; d++)
                    {
                        var dupl = PlannerResults[i].DuplicateResults[d];
                        if (dupl.ParentFieldPlanner.WasPreExecuted == false) continue;
                        if (dupl.Checker == relationTo) continue;
                        if (PlannerResults[i].Checker == relationTo) continue;

                        if (PlannerResults[i].DuplicateResults[d].Checker.BoundsDistanceTo(relationTo) <= maxDistance)
                            _checkersList.Add(PlannerResults[i].DuplicateResults[d].Checker);
                    }
            }

            return _checkersList;
        }


        public CheckerField3D GetFullCheckerMask(FieldPlanner toIgnore = null)
        {
            CheckerField3D mask = new CheckerField3D();

            for (int i = 0; i < PlannerResults.Count; i++)
            {
                if (PlannerResults[i].ParentFieldPlanner.WasPreExecuted == true)
                {
                    if (PlannerResults[i].ParentFieldPlanner != toIgnore)
                    {
                        mask.Join(PlannerResults[i].ParentFieldPlanner.LatestChecker);
                    }
                }

                if (PlannerResults[i].DuplicateResults != null)
                    for (int d = 0; d < PlannerResults[i].DuplicateResults.Count; d++)
                    {
                        var dupl = PlannerResults[i].DuplicateResults[d];
                        if (dupl.ParentFieldPlanner.WasPreExecuted == false) continue;
                        if (dupl.ParentFieldPlanner == toIgnore) continue;
                        if (PlannerResults[i].ParentFieldPlanner == toIgnore) continue;

                        mask.Join(PlannerResults[i].DuplicateResults[d].ParentFieldPlanner.LatestChecker);
                    }
            }

            return mask;
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        public static void DrawPrintGizmos(PlanGenerationPrint print)
        {
            Matrix4x4 preMx = Gizmos.matrix;

            for (int i = 0; i < print.PlannerResults.Count; i++)
            {
                if (print.PlannerResults[i] == null) continue;

                if (print.PlannerResults[i].ParentFieldPlanner)
                {
                    if (print.PlannerResults[i].ParentFieldPlanner._EditorDisplayGizmosOnPlan == false) continue;
                }

                float aMul = 1f;
                //if (print.PlannerResults[i].GhostGizmos) aMul = 0.3f;
                bool selected = print.PlannerResults[i].IsSelected;
                Gizmos.color = GeneratePlannerColor(print.PlannerResults[i].ParentFieldPlanner.IndexOnPreset).ChangeColorAlpha((selected ? 0.8f : 0.5f) * aMul);

                if (print.PlannerResults[i].ParentFieldPlanner.Discarded == false)
                    print.PlannerResults[i].Checker.DrawFieldGizmos(true, true);

                if (selected)
                {
                    Gizmos.color = Gizmos.color.ChangeColorAlpha(0.5f * aMul);
                    print.PlannerResults[i].Checker.DrawFieldGizmosBounding();
                }

                Handles.color = Gizmos.color;

                if (print.PlannerResults[i].DuplicateResults != null)
                {
                    for (int d = 0; d < print.PlannerResults[i].DuplicateResults.Count; d++)
                    {
                        if (print.PlannerResults[i].DuplicateResults[d] == null) continue;
                        if (print.PlannerResults[i].DuplicateResults[d].Checker == null) continue;

                        //if (print.PlannerResults[i].DuplicateResults[d].GhostGizmos) aMul = 0.3f;
                        selected = print.PlannerResults[i].DuplicateResults[d].IsSelected;
                        if (selected) Gizmos.color = Gizmos.color.ChangeColorAlpha(0.8f * aMul);

                        if (print.PlannerResults[i].DuplicateResults[d].ParentFieldPlanner.Discarded == false)
                            print.PlannerResults[i].DuplicateResults[d].Checker.DrawFieldGizmos(true, true);

                        if (selected)
                        {
                            Gizmos.color = Gizmos.color.ChangeColorAlpha(0.5f * aMul);
                            print.PlannerResults[i].DuplicateResults[d].Checker.DrawFieldGizmosBounding();
                        }

                    }
                }


                // Display Sub Fields
                if (print.PlannerResults[i].ParentFieldPlanner)
                {
                    if (print.PlannerResults[i].ParentFieldPlanner.IsSubField == false)
                    {
                        if (selected)
                        {
                            float hsvDiv = (float)(print.PlannerResults[i].ParentFieldPlanner.GetSubFieldsCount - 1);
                            var planner = print.PlannerResults[i].ParentFieldPlanner;

                            for (int s = 0; s < planner.GetSubFieldsCount; s++)
                            {
                                if (planner.GetSubField(s).Available == false) continue;

                                Gizmos.color = Color.HSVToRGB((float)(s % 2 == 0 ? s : s / 2f) / hsvDiv, 0.7f, 0.7f);
                                planner.GetSubField(s).LatestChecker.DrawFieldGizmos();
                                //Gizmos.DrawSphere(planner.GetSubField(s).LatestChecker.GetFullBoundsWorldSpace().center, planner.PreviewCellSize.x * 0.3f);
                            }
                        }
                        else
                        {
                            Color rCol = Gizmos.color;
                            //float h, sat, v;
                            //Color.RGBToHSV(rCol, out h, out sat, out v);
                            //float stepA = 0.2f / (float)print.PlannerResults[i].ParentFieldPlanner.GetSubFieldsCount;

                            for (int s = 0; s < print.PlannerResults[i].ParentFieldPlanner.GetSubFieldsCount; s++)
                            {
                                var sub = print.PlannerResults[i].ParentFieldPlanner.GetSubField(s);
                                if (sub.Available == false) continue;

                                //Color vCol = Color.HSVToRGB((h + s * stepA + ((s % 2 == 0) ? 0.15f : 0f) ) - 0.1f, sat, v - 0.1f);
                                Color vCol = rCol * 0.8f;
                                if (s % 2 == 0) vCol.a *= 0.6f; else if (s % 3 == 0) vCol *= 0.875f;
                                Gizmos.color = vCol;

                                sub.LatestChecker.DrawFieldGizmos();
                            }
                        }

                    }
                }

            }

            if (print._debugLatestExecuted != null)
                print._debugLatestExecuted.DrawFieldGizmosBounding();

            if (FGenerators.CheckIfExist_NOTNULL(SelectedPlannerResult))
            {
                if (FGenerators.CheckIfExist_NOTNULL(SelectedCellRef))
                {
                    Gizmos.color = GeneratePlannerColor(SelectedPlannerResult.ParentFieldPlanner.IndexOnPreset);
                    Gizmos.matrix = preMx * SelectedPlannerResult.Checker.Matrix;
                    Gizmos.DrawWireCube(SelectedPlannerResult.Checker.GetLocalPos(SelectedCellRef), Vector3.one * 0.99f);
                    Gizmos.matrix = preMx;

                    for (int i = 0; i < SelectedPlannerResult.CellsInstructions.Count; i++)
                    {
                        if (SelectedPlannerResult.CellsInstructions[i].HelperCellRef != SelectedCellRef) continue;

                    }
                }
            }

            Gizmos.matrix = preMx;
        }



        //static int _LastSelectedID = -1;
        public static PlannerResult SelectedPlannerResult = null;
        public static FieldCell SelectedCellRef = null;
        public static int SelectedCellCommanndI = 0;
        public static SpawnInstructionGuide DisplayedCellCommand = null;
        public static int SelectedCellCommanndsCount = 0;
        public static void DrawPrintHandles(PlanGenerationPrint print)
        {
            if (print == null) return;
            if (print.PlannerResults == null) return;


            //if (SelectedPlannerResult != null)
            //{
            //    if (SelectedPlannerResult.ParentFieldPlanner != null)
            //        _LastSelectedID = SelectedPlannerResult.ParentFieldPlanner.IndexOnPreset;
            //}
            //else if (_LastSelectedID > -1)
            //{
            //    if (print.PlannerResults.ContainsIndex(_LastSelectedID))
            //        SelectedPlannerResult = print.PlannerResults[_LastSelectedID];
            //}


            bool wasClick = false;
            for (int i = 0; i < print.PlannerResults.Count; i++)
            {
                if (print.PlannerResults[i] == null) continue;

                if (print.PlannerResults[i].ParentFieldPlanner)
                {
                    if (print.PlannerResults[i].ParentFieldPlanner._EditorDisplayGizmosOnPlan == false) continue;
                }

                Handles.color = GeneratePlannerColor(print.PlannerResults[i].ParentFieldPlanner.IndexOnPreset).ChangeColorAlpha(0.25f);

                if (print.PlannerResults[i].Checker.DrawFieldHandles(1))
                {
                    SelectedCellRef = null;

                    if (SelectedPlannerResult != print.PlannerResults[i])
                        SelectedPlannerResult = print.PlannerResults[i];
                    else
                        SelectedPlannerResult = null;

                    wasClick = true;
                }

                print.PlannerResults[i].DrawHandles();

                if (print.PlannerResults[i].DuplicateResults != null)
                {
                    for (int d = 0; d < print.PlannerResults[i].DuplicateResults.Count; d++)
                    {
                        if (print.PlannerResults[i].DuplicateResults[d] == null) continue;
                        if (print.PlannerResults[i].DuplicateResults[d].Checker == null) continue;

                        if (!wasClick)
                            if (print.PlannerResults[i].DuplicateResults[d].Checker.DrawFieldHandles(1))
                            {
                                SelectedCellRef = null;

                                if (SelectedPlannerResult != print.PlannerResults[i].DuplicateResults[d])
                                    SelectedPlannerResult = print.PlannerResults[i].DuplicateResults[d];
                                else
                                    SelectedPlannerResult = null;

                                wasClick = true;
                            }

                        print.PlannerResults[i].DuplicateResults[d].DrawHandles();

                    }
                }
            }


        }
#endif

        #endregion

        #region Editor Draw Scene Utils

#if UNITY_EDITOR
        public static void DrawCellsSceneGUI(float yOffset = 120)
        {

            if (FGenerators.CheckIfExist_NOTNULL(PlanGenerationPrint.SelectedPlannerResult))
                if (FGenerators.CheckIfExist_NOTNULL(PlanGenerationPrint.SelectedCellRef))
                {
                    var cellOwner = SelectedPlannerResult;
                    var cell = SelectedCellRef;
                    var id = SelectedCellCommanndI;

                    Rect bRect = new Rect(16, yOffset, 420, 24);
                    GUI.Label(bRect, "Cell Local Position: " + cell.Pos + "     Commands: " + SelectedCellCommanndsCount);
                    bRect.width = 220;

                    if (SelectedCellCommanndsCount > 1)
                    {
                        Rect sRect = new Rect(16, yOffset + 20, 160, 24);
                        GUI.Label(sRect, "Command In Cell: ");
                        sRect.position += new Vector2(118, 5);
                        SelectedCellCommanndI = Mathf.RoundToInt(GUI.HorizontalSlider(sRect, SelectedCellCommanndI, 0, SelectedCellCommanndsCount - 1));
                    }
                    else
                        bRect.position -= new Vector2(0, 18);

                    int cellCounter = 0;
                    for (int i = 0; i < SelectedPlannerResult.CellsInstructions.Count; i++)
                    {
                        var instr = SelectedPlannerResult.CellsInstructions[i];
                        if (instr.HelperCellRef == SelectedCellRef)
                        {
                            cellCounter += 1;
                            if (cellCounter == SelectedCellCommanndI + 1)
                            {
                                DisplayedCellCommand = instr;
                                break;
                            }
                        }
                    }

                    if (FGenerators.CheckIfExist_NOTNULL(DisplayedCellCommand))
                    {
                        var cmd = DisplayedCellCommand;

                        Rect cRect = new Rect(16, bRect.y + 44, 160, 24);

                        if (SelectedCellCommanndsCount > 1)
                        {
                            GUI.Label(cRect, cellCounter + "/" + SelectedCellCommanndsCount);
                            cRect.position += new Vector2(32, 0);
                        }

                        GUI.Label(cRect, "Command Index: [" + cmd.Id + "]");

                        if (cmd.UseDirection)
                        {
                            cRect.position += new Vector2(120, 0);
                            GUI.Label(cRect, "Rotation: " + cmd.rot.eulerAngles);
                        }
                    }

                }

        }
#endif


        #endregion
    }

    public class PlannerResult
    {
        public BuildPlannerPreset ParentBuildPlanner;
        public FieldPlanner ParentFieldPlanner;
        public CheckerField3D Checker;
        public FGenGraph<PlannerCell, FGenPoint> Grid;
        public List<SpawnInstructionGuide> CellsInstructions;

        [System.NonSerialized] public List<PlannerResult> DuplicateResults = null;

#if UNITY_EDITOR
        public bool IsSelected { get { return PlanGenerationPrint.SelectedPlannerResult == this; } }
#endif

        public static PlannerResult GenerateInstance(BuildPlannerPreset build, FieldPlanner field)
        {
            PlannerResult result = new PlannerResult();
            result.ParentBuildPlanner = build;
            result.ParentFieldPlanner = field;
            result.Checker = new CheckerField3D();
            result.CellsInstructions = new List<SpawnInstructionGuide>();
            return result;
        }

        public PlannerResult Copy()
        {
            PlannerResult copy = (PlannerResult)MemberwiseClone();

            copy.Checker = Checker.Copy();

            if (DuplicateResults != null)
            {
                copy.DuplicateResults = new List<PlannerResult>();
                for (int i = 0; i < DuplicateResults.Count; i++)
                {
                    var resDup = DuplicateResults[i].Copy();
                    copy.DuplicateResults.Add(resDup);
                }
            }

            copy.CellsInstructions = new List<SpawnInstructionGuide>();
            for (int i = 0; i < CellsInstructions.Count; i++)
            {
                copy.CellsInstructions.Add(CellsInstructions[i]);
            }

            return (copy);
        }

        public void PrepareDuplicateSupport()
        {
            if (DuplicateResults == null) DuplicateResults = new List<PlannerResult>();
            //else DuplicateResults.Clear();
        }

        public void AddDuplicateResultSlot(PlannerResult latestResult)
        {
            DuplicateResults.Add(latestResult);
        }

        public Bounds GetBounds()
        {
            Bounds b = Checker.GetFullBoundsWorldSpace();

            if (DuplicateResults != null)
                for (int i = 0; i < DuplicateResults.Count; i++)
                {
                    b.Encapsulate(DuplicateResults[i].Checker.GetFullBoundsWorldSpace());
                }

            return b;
        }

        public void DrawHandles(bool setMatrix = true)
        {
#if UNITY_EDITOR
            Matrix4x4 preMx = Handles.matrix;
            Matrix4x4 mx = preMx * Checker.Matrix;
            Handles.matrix = mx;

            FieldCell preC = null;
            int drawedLimit = 0;

            //if (PlanGenerationPrint.SelectedPlannerResult == null)
            //{
            //    PlanGenerationPrint.SelectedCellRef = null;
            //}
            Color prehC = Handles.color;
            int drawLimit = 70;

            for (int i = 0; i < CellsInstructions.Count; i++)
            {
                if (FGenerators.CheckIfIsNull(CellsInstructions[i])) continue;

                FieldCell cll = CellsInstructions[i].HelperCellRef;

                if (FGenerators.CheckIfIsNull(cll)) continue;

                bool clicked = false;

                if (preC != CellsInstructions[i].HelperCellRef)
                {
                    Handles.color = prehC.ChangeColorAlpha(0.4f);
                    clicked = UnityEditor.Handles.Button(cll.Pos + new Vector3(0.35f, 0f, 0.35f), Quaternion.identity, 0.2f, 0.15f, UnityEditor.Handles.CubeHandleCap);
                    Handles.color = prehC;
                }
                preC = cll;

                if (PlanGenerationPrint.SelectedCellRef != CellsInstructions[i].HelperCellRef)
                {
                    CellsInstructions[i].DrawHandles(Handles.matrix);
                }
                else
                {
                    if (PlanGenerationPrint.DisplayedCellCommand == CellsInstructions[i])
                    {
                        CellsInstructions[i].DrawHandles(Handles.matrix);
                    }
                }

                if (clicked)
                {
                    PlanGenerationPrint.SelectedPlannerResult = this;
                    PlanGenerationPrint.SelectedCellCommanndI = 0;
                    PlanGenerationPrint.SelectedCellCommanndsCount = 0;

                    if (PlanGenerationPrint.SelectedCellRef == CellsInstructions[i].HelperCellRef)
                    {
                        PlanGenerationPrint.SelectedCellRef = null;
                    }
                    else
                    {
                        PlanGenerationPrint.SelectedCellRef = CellsInstructions[i].HelperCellRef;

                        for (int c = 0; c < CellsInstructions.Count; c++)
                        {
                            if (CellsInstructions[c].HelperCellRef == PlanGenerationPrint.SelectedCellRef)
                            {
                                PlanGenerationPrint.SelectedCellCommanndsCount += 1;
                            }
                        }
                    }

                    Event.current.Use();
                }

                if (PlanGenerationPrint.SelectedPlannerResult != this)
                {
                    drawedLimit += 1;
                    if (drawedLimit > drawLimit) break; // Don't draw too many when not selected
                }
            }



            if (drawedLimit >= drawLimit)
            {
                if (CellsInstructions.Count > drawedLimit)
                    Handles.Label(CellsInstructions[drawedLimit].pos, new GUIContent(" Draw Limit!", FGUI_Resources.Tex_Info, "Stopped drawing more instructions for performance reasons, select field to display all instructions (limit =" + drawLimit + "  Want to draw =" + CellsInstructions.Count + ")"));
            }




            // Display Sub Fields Cell Instructions
            if (ParentFieldPlanner)
            {
                if (ParentFieldPlanner.IsSubField == false)
                {
                    bool drawDirs = PlanGenerationPrint.SelectedPlannerResult == this;

                    Gizmos.color *= 0.85f;
                    for (int s = 0; s < ParentFieldPlanner.GetSubFieldsCount; s++)
                    {
                        var subf = ParentFieldPlanner.GetSubField(s);
                        if (subf == null) continue;
                        if (subf.Available == false) continue;

                        Handles.matrix = preMx * subf.LatestChecker.Matrix;

                        for (int i = 0; i < subf.LatestResult.CellsInstructions.Count; i++)
                        {
                            if (drawDirs) subf.LatestResult.CellsInstructions[i].DrawHandles(Handles.matrix);
                            
                            Handles.Button(subf.LatestResult.CellsInstructions[i].pos + new Vector3(0.35f, 0f, 0.35f), Quaternion.identity, 0.2f, 0.15f, UnityEditor.Handles.CubeHandleCap);
                            //subf.LatestResult.CellsInstructions[i].DrawHandles(Handles.matrix);
                        }
                    }
                }
            }


            Handles.matrix = preMx;

#endif
        }

        public bool ContainsAlready(SpawnInstructionGuide instr)
        {
            for (int i = 0; i < CellsInstructions.Count; i++)
            {
                if (CellsInstructions[i].HelperCellRef != instr.HelperCellRef) continue;
                if (CellsInstructions[i].Id != instr.Id) continue;
                if (CellsInstructions[i].WorldRot != instr.WorldRot) continue;
                if (CellsInstructions[i].UseDirection != instr.UseDirection) continue;
                if (CellsInstructions[i].pos != instr.pos) continue;
                if (CellsInstructions[i].rot != instr.rot) continue;

                if (instr.CustomDefinition != null)
                {
                    if (CellsInstructions[i].CustomDefinition == null) continue;
                }
                else // null custom def
                {
                    if (CellsInstructions[i].CustomDefinition != null) continue;
                }

                return true;
            }

            return false;
        }
    }

}