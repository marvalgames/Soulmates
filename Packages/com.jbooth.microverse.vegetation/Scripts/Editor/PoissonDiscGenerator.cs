using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
#if _MICROVERSE_DEVMODE__

    public class PoissonDiscGenerator : EditorWindow
    {
        public string texName = "PoissonDisk";
        public float density = 30;

        [MenuItem("Window/MicroVerse/PoissonDiskGenerator")]
        static void Init()
        {
            PoissonDiscGenerator window = (PoissonDiscGenerator)EditorWindow.GetWindow(typeof(PoissonDiscGenerator));
            window.Show();
        }

        void OnGUI()
        {
            texName = EditorGUILayout.TextField("Output Name", texName);
            density = EditorGUILayout.Slider("Density", density, 1, 100);
            if (GUILayout.Button("Go"))
            {
                Generate();
            }
        }


        public void Shuffle<T>(List<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        void Generate()
        {
            var list = GeneratePoints(1.0f / density, 1, 1, 30, new System.Random(10));
            Shuffle<Vector2>(list);
            Texture2D tex = new Texture2D(list.Count, 1, TextureFormat.RGFloat, false, true);
            for (int x = 0; x < list.Count; ++x)
            {
                var p = list[x];
                tex.SetPixel(x, 0, new Color(p.x, p.y, 0, 0));
            }
            tex.Apply(false, false);
            PoissonDiscImporter.Write(tex, "Assets/" + texName, false, true);
            DestroyImmediate(tex);

        }


        public static List<Vector2> GeneratePoints(float radius, float width, float height, int numSamplesBeforeRejection = 30, System.Random random = null)
        {
            random = random ?? new System.Random();

            Vector2 sampleRegionSize = new Vector2(width, height);
            float cellSize = radius / (float)Mathf.Sqrt(2);

            int[,] grid = new int[(int)System.Math.Ceiling(sampleRegionSize.x / cellSize), (int)System.Math.Ceiling(sampleRegionSize.y / cellSize)];
            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2> { sampleRegionSize / 2 };

            while (spawnPoints.Count > 0)
            {
                int spawnIndex = random.Next(0, spawnPoints.Count);
                Vector2 spawnCentre = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < numSamplesBeforeRejection; i++)
                {
                    double angle = random.NextDouble() * Mathf.PI * 2;
                    Vector2 dir = new Vector2((float)System.Math.Sin(angle), (float)System.Math.Cos(angle));

                    float scale = (float)random.NextDouble() * radius + radius;
                    Vector2 candidate = spawnCentre + dir * scale;
                    if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                        candidateAccepted = true;
                        break;
                    }
                }
                if (!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex);
                }

            }

            return points;
        }

        private static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
        {
            if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                int cellX = (int)(candidate.x / cellSize);
                int cellY = (int)(candidate.y / cellSize);
                int searchStartX = Mathf.Max(0, cellX - 2);
                int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
                int searchStartY = Mathf.Max(0, cellY - 2);
                int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

                for (int x = searchStartX; x <= searchEndX; x++)
                {
                    for (int y = searchStartY; y <= searchEndY; y++)
                    {
                        int pointIndex = grid[x, y] - 1;
                        if (pointIndex != -1)
                        {
                            float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                            if (sqrDst < radius * radius)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
#endif
}
