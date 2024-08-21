using FIMSpace.Generating.Checker;
using UnityEngine;

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class SG_PerlinNoiseGenerating : ShapeGeneratorBase
    {
        public override string TitleName() { return "Custom/Perlin Noise Field"; }

        public int Width = 32;
        public int YLevels = 1;
        public int Depth = 20;
        public float PerlinScale = 0.5f;

        public override CheckerField3D GetChecker(FieldPlanner planner)
        {
            CheckerField3D checker = new CheckerField3D();

            int xRandomOffset = FGenerators.GetRandom(-1000, 1000);
            int zRandomOffset = FGenerators.GetRandom(-1000, 1000);

            for (int x = 0; x < Width; x++)
                for (int z = 0; z < Depth; z++)
                {
                    Vector3Int cell = new Vector3Int(x, 0, z);

                    float perlinX = xRandomOffset + x * PerlinScale;
                    float perlinZ = zRandomOffset + z * PerlinScale;

                    float perlinNoiseValue = Mathf.PerlinNoise(perlinX, perlinZ);
                    cell.y = Mathf.RoundToInt(perlinNoiseValue * YLevels);

                    checker.AddLocal(cell);
                }


            return checker;
        }

    }
}