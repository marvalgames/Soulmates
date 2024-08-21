using FIMSpace.Generating.Planning;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FIMSpace.Generating.Checker
{
    public partial class CheckerField3D
    {


        [System.Serializable]
        public struct ShatterFractionRequest
        {
            public float TryKeepSize;
            //[Tooltip("Algorithm will try to apply this Field Setup on the shape")]
            //public FieldSetup OverrideFieldSetup;

            public bool ConditionsMet(ShatterFraction fract)
            {
                float magn = fract.Bounding.size.magnitude;
                if (magn > TryKeepSize * 0.5f && magn < TryKeepSize * 1.5f) return true;
                return false;
            }
        }


        public struct ShatterFraction
        {
            public Bounds Bounding;
            public Bounds B { get { return Bounding; } }

            public float MaxAxisSize { get { if (B.size.x > B.size.z) return B.size.x; else return B.size.z; } }
            public float MinAxisSize { get { if (B.size.x < B.size.z) return B.size.x; else return B.size.z; } }

            public bool Freeze;
            public ShatterFractionRequest RequestApplied;

            public ShatterFraction(Bounds bounds)
            {
                Bounding = bounds;
                Freeze = false;
                RequestApplied = new ShatterFractionRequest();
            }

            public ShatterFraction Split(List<ShatterFraction> shapes, FGenerators.DefinedRandom random = null, float sliceRandomize = 0f)
            {
                Bounds mainBounds = B;
                ShatterFraction splitA = this;
                ShatterFraction splitB = this;

                bool horizontal;

                if (mainBounds.size.x > mainBounds.size.z * 1.75f) horizontal = true;
                else if (mainBounds.size.z > mainBounds.size.x * 1.75f) horizontal = false;
                else
                {
                    if (random == null) horizontal = FGenerators.GetRandomFlip();
                    else horizontal = random.GetRandom(0f, 1f) > 0.5f; // Horizontal Split
                }

                int maxDiv;
                float sliceFactor = 2f;
                //if (sliceRandomize > 0f)
                //{
                //    sliceFactor = 2f + random.GetRandom(0f, sliceRandomize * 4f);
                //}

                if (horizontal) maxDiv = Mathf.FloorToInt(mainBounds.size.x / sliceFactor);
                else maxDiv = Mathf.FloorToInt(mainBounds.size.z / sliceFactor);

                if (maxDiv <= 0)
                {
                    return splitA; // Cant split!
                }

                Bounds boundsA;
                Bounds boundsB;
                //FDebug.DrawBounds3D(mainBounds, Color.green);

                if (horizontal) // Slicing bounds into two LEFT-RIGHT rects of rounded scale
                {
                    Vector3 minP = mainBounds.min;
                    Vector3 maxP = mainBounds.max - new Vector3(Mathf.FloorToInt(mainBounds.size.x / sliceFactor), 0f, 0f);

                    boundsA = new Bounds(minP, Vector3.zero);
                    boundsA.Encapsulate(maxP);

                    minP = mainBounds.max;
                    maxP = mainBounds.min + new Vector3(Mathf.CeilToInt(mainBounds.size.x / sliceFactor), 0f, 0f);

                    boundsB = new Bounds(minP, Vector3.zero);
                    boundsB.Encapsulate(maxP);
                }
                else // Slicing bounds into two UPPER-LOWER rects of rounded scale
                {
                    Vector3 minP = mainBounds.min;
                    Vector3 maxP = mainBounds.max - new Vector3(0f, 0f, Mathf.FloorToInt(mainBounds.size.z / sliceFactor));

                    boundsA = new Bounds(minP, Vector3.zero);
                    boundsA.Encapsulate(maxP);

                    minP = mainBounds.max;
                    maxP = mainBounds.min + new Vector3(0f, 0f, Mathf.CeilToInt(mainBounds.size.z / sliceFactor));

                    boundsB = new Bounds(minP, Vector3.zero);
                    boundsB.Encapsulate(maxP);
                }

                splitA.Bounding = boundsA;
                splitB.Bounding = boundsB;

                //FDebug.DrawBounds3D(boundsA, Color.blue);
                //FDebug.DrawBounds3D(boundsB, Color.red);

                shapes.Add(splitB);
                return splitA;
            }

            public CheckerField3D ToCheckerField(FieldPlanner copySettingsFrom = null, bool centerize = false)
            {
                CheckerField3D checker = new CheckerField3D();

                Vector3Int centerOffset = Vector3Int.zero;
                if (centerize) centerOffset = (Bounding.center).V3toV3Int();

                if (copySettingsFrom != null)
                {
                    checker.RootScale = copySettingsFrom.PreviewCellSize;
                    checker.SubFieldPlannerReference = copySettingsFrom;
                }

                checker.JoinWithLocalBoundsToCells(Bounding);
                if (centerOffset != Vector3Int.zero) checker.OffsetOriginByLocal(centerOffset);

                return checker;
            }
        }

        /// <summary> Helper reference for calculations to avoid creating multiple list instances </summary>
        static List<FieldCell> _cellsListHelperInstance = null;
        static List<FieldCell> CellsListHelperInstance { get { if (_cellsListHelperInstance == null) _cellsListHelperInstance = new List<FieldCell>(); _cellsListHelperInstance.Clear(); return _cellsListHelperInstance; } }

        /// <summary> Keep all cells in the same position in world but move checker field origin point </summary>
        private void OffsetOriginByLocal(Vector3Int localOffset)
        {
            var cellsBuffer = CellsListHelperInstance;

            for (int c = 0; c < AllCells.Count; c++) cellsBuffer.Add(AllCells[c]); // Backup cells

            ClearAllCells();

            RootPosition += Vector3.Scale(RootScale, localOffset);

            for (int c = 0; c < cellsBuffer.Count; c++)
            {
                cellsBuffer[c].Pos = cellsBuffer[c].Pos - localOffset;
                Grid.AddCell(cellsBuffer[c]);
            }
        }

        public static List<ShatterFraction> SplitChecker(CheckerField3D checker, int splitIteractions = 1)
        {
            if (splitIteractions <= 0) return new List<ShatterFraction>();
            return ShatterChecker(checker, 0, splitIteractions - 1, 1000);
        }


        public static List<ShatterFraction> ShatterChecker(CheckerField3D checker, int minimumSize = 2, int maxDepth = 40, int limitShapesCount = 100, List<ShatterFractionRequest> fractRequests = null, FGenerators.DefinedRandom random = null, float sliceRandomize = 0f)
        {
            bool preUseB = checker.UseBounds = false; checker.UseBounds = true;
            checker.RecalculateMultiBounds();
            checker.UseBounds = preUseB;

            List<ShatterFraction> shapes = new List<ShatterFraction>();
            for (int i = 0; i < checker.Bounding.Count; i++)
            {
                shapes.Add(new ShatterFraction(checker.Bounding[i]));
            }

            List<ShatterFractionRequest> requests = new List<ShatterFractionRequest>();
            if (fractRequests != null) for (int i = 0; i < fractRequests.Count; i++) requests.Add(fractRequests[i]);

            int iter = 0;

            while (true)
            {
                if (shapes.Count > limitShapesCount) break;

                shapes = shapes.OrderByDescending(x => x.MaxAxisSize).ToList(); // Always set biggest field as first in the list

                #region Debugging Backup

                //string sizeReport = "";
                //for (int d = 0; d < shapes.Count; d++)
                //{
                //    sizeReport += "[" + d + "] " + shapes[d].Bounding.size + "  |  ";
                //}
                //UnityEngine.Debug.Log("["+iter+"] Size Report : " + sizeReport);

                #endregion


                int targetShape = 0;
                if (targetShape >= shapes.Count) break; // Index out of list bounds check ---

                while (shapes[targetShape].Freeze || shapes[0].B.size.magnitude < minimumSize) // If some shapes are freezed or are too small, skip until not freezed one
                {
                    targetShape += 1;
                    if (targetShape >= shapes.Count) break;
                }

                #region Checking if current fraction is meeting some request conditions

                if (targetShape >= shapes.Count) break; // Index out of list bounds check ---

                if (requests.Count != 0)
                {
                    for (int r = 0; r < requests.Count; r++) // Check all requests
                    {
                        if (requests[r].ConditionsMet(shapes[targetShape]))
                        {
                            var toFreeze = shapes[targetShape];
                            toFreeze.Freeze = true;
                            toFreeze.RequestApplied = requests[r];
                            shapes[targetShape] = toFreeze;
                            requests.RemoveAt(r);
                            targetShape += 1;
                            break;
                        }
                    }
                }

                #endregion

                if (targetShape >= shapes.Count) break; // Index out of list bounds check ---

                shapes[targetShape] = shapes[targetShape].Split(shapes, random, sliceRandomize); // Split randomly bounds, update index shape...
                // ...and add second split to the shapes list

                iter += 1;

                if (iter > maxDepth) break; // Provided Limit Exceeded
                if (iter > 10000000) break; // Main iteration limit to prevent too long calculations
            }

            #region Debugging Backup

            // Debug Display
            //float cnt = (float)shapes.Count - 1;
            //for (int s = 0; s < shapes.Count; s++)
            //{
            //    FDebug.DrawBounds3D(shapes[s].Bounding, Color.HSVToRGB((float)s / cnt, 0.65f, 0.75f), 1f, 0.1f);
            //}

            #endregion

            return shapes;
        }


    }
}