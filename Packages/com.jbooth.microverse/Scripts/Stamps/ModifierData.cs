using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    public class StampData
    {
        public Terrain terrain;
        public float RealHeight { get { return terrain.terrainData.heightmapScale.y * 2; } }
        public Matrix4x4 WorldToTerrainMatrix { get { return terrain.transform.worldToLocalMatrix; } }
        public Vector2 RealSize
        {
            get
            {
                return new Vector2(
                terrain.terrainData.heightmapScale.x * (terrain.terrainData.heightmapResolution),
                terrain.terrainData.heightmapScale.z * (terrain.terrainData.heightmapResolution));
            }
        }

        public StampData(Terrain terrain)
        {
            this.terrain = terrain;
        }
    }
}
