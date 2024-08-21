using FIMSpace.Generating.Checker;
using UnityEngine;
using System.Collections.Generic;
using System.Drawing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.GeneratingLogics
{
    public class SG_DividedRectangle : ShapeGeneratorBase
    {
        public override string TitleName() { return "Complex/Divided Rectangle"; }

        public MinMax ZoneWidth = new MinMax(10, 12);
        public MinMax ZoneDepth = new MinMax(7, 8);
        [Space(5)]
        public MinMax ClusterWidthRange = new MinMax(3, 5);
        public MinMax ClusterDepthRange = new MinMax(3, 5);

        [Space(4)]
        [PGG_SingleLineTwoProperties("OverrideRowsCount")]
        [Tooltip("Zero = Not Using this feature.\nIf you want to avoid creating too many room columns, you can use this hard limit value.")]
        public int OverrideColumnsCount = 0;
        [HideInInspector] public int OverrideRowsCount = 0;

        [Space(4)]
        [Tooltip("Randomizing Width of rectangles which are aligning with the same depth dimension.\nIt allows to resize width of room by one more cell than max width!")]
        //[Tooltip("Restrict rooms sizes to fit their min max ranges as much as possible")]
        [PGG_SingleLineTwoProperties("RandomizeDepth")]
        public bool RandomizeRects = true;
        public bool AdvancedMode = false;

        [HideInInspector] public bool RandomizeDepth = true;
        [HideInInspector] public MinMax MinMaxWidthsDivs = new MinMax(0, 10000);
        [HideInInspector] public MinMax MinMaxDepthDivs = new MinMax(0, 10000);

        static FGenerators.DefinedRandom rand = new FGenerators.DefinedRandom(0);
        static List<DivideCluster> clusterList = new List<DivideCluster>();
        static List<int> clusterWidthDivs = new List<int>();
        static List<int> clusterDepthDivs = new List<int>();

        public override CheckerField3D GetChecker(FieldPlanner planner)
        {
            if (ClusterWidthRange.Min < 1) ClusterWidthRange.Min = 1;
            if (ClusterWidthRange.Max < 1) ClusterWidthRange.Max = 1;

            if (ClusterDepthRange.Min < 1) ClusterDepthRange.Min = 1;
            if (ClusterDepthRange.Max < 1) ClusterDepthRange.Max = 1;

            clusterList.Clear();
            clusterWidthDivs.Clear();
            clusterDepthDivs.Clear();

            rand.ReInitializeSeed(FGenerators.GetRandom(-10000, 10000));

            CheckerField3D checker = new CheckerField3D();
            Vector3Int size = new Vector3Int(ZoneWidth.GetRandom(), 1, ZoneDepth.GetRandom());
            checker.SetSize(size.x, 1, size.z);
            checker.RootScale = planner.PreviewCellSize;


            var divs = GenerateDivides(checker, ClusterWidthRange, ClusterDepthRange, OverrideColumnsCount, OverrideRowsCount, RandomizeRects,
                AdvancedMode, RandomizeDepth, MinMaxWidthsDivs, MinMaxDepthDivs);

            foreach (var chec in divs) planner.AddSubField(chec);


            checker = new CheckerField3D(); // It's container for sub fields so default checker gets empty
            checker.RootScale = planner.PreviewCellSize;

            return checker;
        }



        public static List<CheckerField3D> GenerateDivides(CheckerField3D source, MinMax ClusterWidthRange, MinMax ClusterDepthRange, int OverrideColumnsCount, int OverrideRowsCount,
            bool RandomizeRects, bool AdvancedMode = false, bool RandomizeDepth = true, MinMax? minMaxWidthsDivs = null, MinMax? minMaxDepthDivs = null)
        {
            List<CheckerField3D> divides = new List<CheckerField3D>();

            clusterList.Clear();
            clusterWidthDivs.Clear();
            clusterDepthDivs.Clear();

            Vector2Int clustersCount = Vector2Int.zero;
            Vector3 srcMin = source.Grid.GetMin();
            Vector3 srcMax = source.Grid.GetMax();
            Vector3Int size = new Vector3Int(Mathf.Abs(srcMax.x - srcMin.x).ToInt() + 1, 1, Mathf.Abs(srcMax.z - srcMin.z).ToInt() + 1);

            #region Prepare

            MinMax MinMaxWidthsDivs, MinMaxDepthDivs;
            if (minMaxWidthsDivs == null) MinMaxWidthsDivs = new MinMax(0, 10000); else MinMaxWidthsDivs = minMaxWidthsDivs.Value;
            if (minMaxDepthDivs == null) MinMaxDepthDivs = new MinMax(0, 10000); else MinMaxDepthDivs = minMaxDepthDivs.Value;

            #endregion


            #region Width dividing

            int posX = 0;

            int targetColumnsCount = OverrideColumnsCount;
            if (OverrideColumnsCount <= 0)
            {
                clusterWidthDivs.Clear();

                int toDistribZ = size.z;
                while (toDistribZ > 0)
                {
                    int w = rand.GetRandom(ClusterWidthRange);
                    toDistribZ -= w;
                    clusterWidthDivs.Add(w);
                }


                AdjustDivCount(clusterWidthDivs, size.x, ClusterWidthRange);
                targetColumnsCount = clusterWidthDivs.Count;

                if (AdvancedMode)
                {
                    targetColumnsCount = Mathf.RoundToInt(Mathf.Max(targetColumnsCount, MinMaxWidthsDivs.Min));
                    if (MinMaxWidthsDivs.Max > 0) targetColumnsCount = Mathf.RoundToInt(Mathf.Min(targetColumnsCount, MinMaxWidthsDivs.Max));
                }
            }


            clusterWidthDivs.Clear();
            ComputeDivs(clusterWidthDivs, size.x, targetColumnsCount);

            // Use prepared div sizes to create base rects
            int toAdd = clusterWidthDivs.Count;
            for (int i = 0; i < toAdd; i++)
            {
                DivideCluster cluster = new DivideCluster();
                cluster.Position = new Vector2Int(posX, 0);

                // Pick random width and remove from random list
                int randomDivId = rand.GetRandom(0, clusterWidthDivs.Count);
                int targetWidth = clusterWidthDivs[randomDivId];
                clusterWidthDivs.RemoveAt(randomDivId);

                cluster.Width = targetWidth;
                cluster.ClusterID = new Vector2Int(i, 0);
                clusterList.Add(cluster);
                posX += targetWidth;
            }

            #endregion


            #region Depth Divides

            int xDivides = clusterList.Count;
            clustersCount.x = xDivides;

            // Depth distribution over Width Divides
            for (int i = 0; i < xDivides; i++)
            {
                int posZ = 0;
                var xCluster = clusterList[i];

                int targetRowsCount = OverrideRowsCount;
                if (OverrideRowsCount <= 0)
                {
                    clusterDepthDivs.Clear();
                    int toDistribZ = size.z;
                    while (toDistribZ > 0)
                    {
                        toDistribZ -= ClusterDepthRange.Min;
                        clusterDepthDivs.Add(ClusterDepthRange.Min);
                    }

                    AdjustDivCount(clusterDepthDivs, size.z, ClusterDepthRange);
                    targetRowsCount = clusterDepthDivs.Count;

                    if (AdvancedMode)
                    {
                        targetRowsCount = Mathf.RoundToInt(Mathf.Max(targetRowsCount, MinMaxDepthDivs.Min));
                        if (MinMaxDepthDivs.Max > 0) targetRowsCount = Mathf.RoundToInt(Mathf.Min(targetRowsCount, MinMaxDepthDivs.Max));
                    }
                }

                clusterDepthDivs.Clear();
                ComputeDivs(clusterDepthDivs, size.z, targetRowsCount);

                // Use prepared div sizes to create base rects
                toAdd = clusterDepthDivs.Count;
                clustersCount.y = toAdd;

                for (int a = 0; a < toAdd; a++)
                {
                    // Pick random width and remove from random list
                    int randomDivId = rand.GetRandom(0, clusterDepthDivs.Count);
                    int targetDepth = clusterDepthDivs[randomDivId];
                    clusterDepthDivs.RemoveAt(randomDivId);

                    if (a > 0) // Don't add new clusters on the bottom xDivs placements
                    {
                        DivideCluster cluster = new DivideCluster();
                        cluster.Position = new Vector2Int(xCluster.Position.x, posZ);
                        cluster.Width = xCluster.Width;
                        cluster.Depth = targetDepth;
                        cluster.ClusterID = new Vector2Int(i, a);
                        clusterList.Add(cluster);
                    }
                    else // Define depth for the bottom xDivs
                    {
                        xCluster.Depth = targetDepth;
                        clusterList[i] = xCluster;
                    }

                    posZ += targetDepth;
                }

            }

            #endregion


            #region Randomize Rectangles


            #region Randomize Width Sizes

            if (RandomizeRects)
            {

                for (int x = 1; x < clustersCount.x; x++) // Dont randomize first X clusters
                {
                    for (int y = 0; y < clustersCount.y; y++)
                    {
                        Vector2Int id = new Vector2Int(x, y);
                        int c = GetClusterIndex(id);
                        if (c == -1) continue;

                        int cBack = GetClusterIndex(new Vector2Int(x - 1, y));
                        if (cBack == -1) continue;


                        var cluster = clusterList[c];
                        var backCluster = clusterList[cBack];

                        if (cluster.Randomized) continue;
                        if (backCluster.Randomized) continue;

                        if (cluster.Position.y != backCluster.Position.y) continue;
                        if (cluster.Depth != backCluster.Depth) continue;

                        if ((cluster.Width + backCluster.Width) / 2 < ClusterWidthRange.Min) continue;

                        if ((cluster.Width + backCluster.Width) / 2 == ClusterWidthRange.Min)
                        {
                            if (rand.GetRandomFlip()) continue;
                        }

                        bool doBackClus = true;
                        if (backCluster.Width == cluster.Width) doBackClus = rand.GetRandomFlip();
                        else if (backCluster.Width < cluster.Width) doBackClus = false;

                        if (!doBackClus)
                        {   // make 'cluster' smaller
                            if (cluster.Width - 1 < ClusterWidthRange.Min) continue; // Too small result

                            cluster.Width -= 1;
                            cluster.Position.x += 1;
                            cluster.Randomized = true;
                            clusterList[c] = cluster;

                            backCluster.Width += 1;
                            clusterList[cBack] = backCluster;
                        }
                        else // make 'backCluster' smaller
                        {
                            //if (cluster.Width + 1 > ClusterWidthRange.Max) continue; // Too big
                            if (backCluster.Width - 1 < ClusterWidthRange.Min) continue; // Too small result

                            cluster.Width += 1;
                            cluster.Position.x -= 1;
                            clusterList[c] = cluster;

                            backCluster.Width -= 1;
                            backCluster.Randomized = true;
                            clusterList[cBack] = backCluster;
                        }
                    }
                }

            }
            #endregion


            #region Randomize Depth Sizes



            if (RandomizeDepth)
            {

                for (int x = 0; x < clustersCount.x; x++)
                {
                    for (int y = 1; y < clustersCount.y; y++) // Dont randomize first Y (z)
                    {
                        Vector2Int id = new Vector2Int(x, y);
                        int c = GetClusterIndex(id);
                        if (c == -1) continue;

                        int cBack = GetClusterIndex(new Vector2Int(x, y - 1));
                        if (cBack == -1) continue;

                        var cluster = clusterList[c];
                        var backCluster = clusterList[cBack];

                        if (cluster.Randomized) continue;
                        if (backCluster.Randomized) continue;

                        if (cluster.Position.x != backCluster.Position.x) continue;
                        if (cluster.Width != backCluster.Width) continue;

                        if ((cluster.Depth + backCluster.Depth) / 2 < ClusterDepthRange.Min) continue;

                        if ((cluster.Depth + backCluster.Depth) / 2 == ClusterDepthRange.Min)
                        {
                            if (rand.GetRandomFlip()) continue;
                        }

                        bool doBackClus = true;
                        if (backCluster.Width == cluster.Width) doBackClus = rand.GetRandomFlip();
                        else if (backCluster.Width < cluster.Width) doBackClus = false;


                        if (!doBackClus)
                        {   // make 'cluster' smaller
                            if (cluster.Depth - 1 < ClusterDepthRange.Min) continue; // Too small result

                            cluster.Depth -= 1;
                            cluster.Position.y += 1;
                            cluster.Randomized = true;
                            clusterList[c] = cluster;

                            backCluster.Depth += 1;
                            clusterList[cBack] = backCluster;
                        }
                        else // make 'backCluster' smaller
                        {
                            //if (cluster.Width + 1 > ClusterWidthRange.Max) continue; // Too big
                            if (backCluster.Depth - 1 < ClusterDepthRange.Min) continue; // Too small result

                            cluster.Depth += 1;
                            cluster.Position.y -= 1;
                            clusterList[c] = cluster;

                            backCluster.Depth -= 1;
                            backCluster.Randomized = true;
                            clusterList[cBack] = backCluster;
                        }
                    }
                }

            }



            #endregion


            #endregion


            // Apply all calculated rects
            float scaleMul = source.RootScale.x;

            for (int s = 0; s < clusterList.Count; s++)
            {
                var cluster = clusterList[s];
                CheckerField3D shapeChecker = new CheckerField3D();
                shapeChecker.SetSize(cluster.Width, 1, cluster.Depth);
                shapeChecker.RootPosition = new Vector3(cluster.Position.x, source.RootPosition.y, cluster.Position.y) * scaleMul;
                shapeChecker.RootScale = source.RootScale;

                divides.Add(shapeChecker);
            }

            return divides;
        }



        static void AdjustDivCount(List<int> divs, int space, MinMax range)
        {
            int sum = 0;
            for (int i = 0; i < divs.Count; i++) sum += divs[i];
            if (sum == space) return;

            int diff = space - sum;

            if (diff > 0) // Too few
            {
                int newSum = sum;

                while (space - newSum > 0)
                {
                    newSum = 0;
                    divs.Add(rand.GetRandom(range));
                    for (int i = 0; i < divs.Count; i++) newSum += divs[i];
                }
            }
            else // Too much
            {
                int newSum = sum;

                while (space - newSum < 0)
                {
                    newSum = 0;
                    divs.RemoveAt(divs.Count - 1);
                    if (divs.Count < 2) break;
                    for (int i = 0; i < divs.Count; i++) newSum += divs[i];
                }
            }
        }

        struct DivideCluster
        {
            public Vector2Int Position;
            public int Width;
            public int Depth;

            public Vector2Int ClusterID;
            public bool Randomized;
        }


        static int GetClusterIndex(Vector2Int pos)
        {
            for (int c = 0; c < clusterList.Count; c++) if (clusterList[c].ClusterID == pos) return c;
            return -1;
        }


        static void ComputeDivs(List<int> divsList, int availableSpace, int divsCount)
        {
            if (divsCount <= 0) return;

            int singleDivWidth = availableSpace / divsCount;
            int toDistr = availableSpace;

            for (int i = 0; i < divsCount; i++) // Prepare div sizes
            {
                divsList.Add(singleDivWidth);
                toDistr -= singleDivWidth;
            }

            #region Correct div sizes

            int divStep = 0;
            while (toDistr != 0)
            {
                if (toDistr < 0)
                {
                    divsList[divStep] -= 1;
                    toDistr += 1;
                }
                else
                {
                    divsList[divStep] += 1;
                    toDistr -= 1;
                }

                divStep += 1;
                if (divStep >= divsList.Count) divStep = 0;
            }

            #endregion
        }




#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void DrawGUI(SerializedObject so, FieldPlanner parent)
        {
            so.Update();

            EditorGUILayout.HelpBox("This Shape is providing multiple Sub-Fields inside the main Field!\nIt's dedicated for house interior rooms layout generating.\n(It's different than 'Shattered Rectangle', this shape is Using Custom Algorithm)", MessageType.Info);
            base.DrawGUI(so,parent);

            if (AdvancedMode)
            {
                if (sp == null) sp = so.FindProperty("MinMaxWidthsDivs");
                EditorGUILayout.PropertyField(sp);
                SerializedProperty spc = sp.Copy(); spc.Next(false);
                EditorGUILayout.PropertyField(spc);
            }

            so.ApplyModifiedProperties();

        }
#endif

    }
}