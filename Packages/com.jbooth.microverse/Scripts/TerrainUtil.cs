using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace JBooth.MicroVerseCore
{
    public class TerrainUtil
    {
        public static Mesh GenerateMesh(int segments, Vector3 tsize)
        {
            float size = tsize.x;
            var mesh = new Mesh();
            mesh.name = "TerrainProxy";
            mesh.hideFlags = HideFlags.DontSave;

            int hCount2 = segments + 1;
            int vCount2 = segments + 1;
            int numTriangles = segments * segments * 6;
            int numVertices = hCount2 * vCount2;

            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[numTriangles];

            int index = 0;
            float uvFactorX = 1.0f / segments;
            float uvFactorY = 1.0f / segments;
            float scaleX = size / segments;
            float scaleY = size / segments;
            for (float y = 0.0f; y < vCount2; y++)
            {
                for (float x = 0.0f; x < hCount2; x++)
                {
                    vertices[index] = new Vector3(x * scaleX, 0, y * scaleY);
                    uvs[index++] = new Vector2(x * uvFactorX, y * uvFactorY);
                }
            }

            index = 0;
            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    triangles[index] = (y * hCount2) + x;
                    triangles[index + 1] = ((y + 1) * hCount2) + x;
                    triangles[index + 2] = (y * hCount2) + x + 1;

                    triangles[index + 3] = ((y + 1) * hCount2) + x;
                    triangles[index + 4] = ((y + 1) * hCount2) + x + 1;
                    triangles[index + 5] = (y * hCount2) + x + 1;
                    index += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(new Vector3(tsize.x * 0.5f, tsize.y * 0.5f, tsize.z * 0.5f), tsize);
            mesh.RecalculateTangents();
            return mesh;
        }

        public static Bounds ComputeTerrainBounds(Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
                return new Bounds();
            var terrainBounds = terrain.terrainData.bounds;
            var ts = terrainBounds.size;
            terrainBounds.center = terrain.transform.position;
            terrainBounds.center += new Vector3(ts.x * 0.5f, 0, ts.z * 0.5f);
            return terrainBounds;
        }

        /// <summary>
        /// Compute the total bounds of the provided terrains
        /// </summary>
        /// <param name="terrains"></param>
        /// <returns></returns>
        public static Bounds ComputeTerrainBounds(Terrain[] terrains)
        {
            Bounds terrainBounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                Bounds terrainWorldBounds = ComputeTerrainBounds(terrain);

                if (i == 0)
                {
                    terrainBounds = terrainWorldBounds;
                }
                else
                {
                    terrainBounds.Encapsulate(terrainWorldBounds);
                }
            }

            return terrainBounds;
        }

        public static Bounds AdjustForRotation(Bounds b, Quaternion rot)
        {
            var mtx = Matrix4x4.TRS(b.center, rot, Vector3.one);
            b.Encapsulate(mtx.MultiplyPoint(b.size / 2));
            b.Encapsulate(mtx.MultiplyPoint(-b.size / 2));
            return b;
        }

        public static Bounds GetBounds(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            float max = Mathf.Max(scale.x, scale.z);
            var b = TerrainUtil.AdjustForRotation(new Bounds(transform.position, new Vector3(max, max, max)), transform.rotation);
            b.max = new Vector3(b.max.x, 99999, b.max.z);
            b.min = new Vector3(b.min.x, -99999, b.min.z);
            return b;

        }

        public static Vector3 ComputeTerrainSize(Terrain terrain)
        {
            var ts = terrain.terrainData.heightmapScale;
            var hr = terrain.terrainData.heightmapResolution;
            return new Vector3(ts.x * hr, ts.y * 2, ts.z * hr);
        }

        public static Matrix4x4 ComputeStampMatrix(Terrain terrain, Transform transform, bool heightStamp = false, int sizeXOffset = 0, int sizeZOffset = 0)
        {
            var ts = terrain.terrainData.size;
            
            Vector2 realSize = new Vector2(ts.x, ts.z);

            // We need to expand the height stamp slightly for parts of the equation,
            // but only when applying a height stamp, not a copy paste stamp!
            if (heightStamp)
            {
                var hms = terrain.terrainData.heightmapScale;
                var hmr = terrain.terrainData.heightmapResolution;
                realSize = new Vector2(hms.x * hmr, hms.z * hmr);
            }
        
            var localPosition = terrain.transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);
            var size = transform.lossyScale;
            Vector2 size2D = new Vector2(size.x + sizeXOffset, size.z + sizeZOffset);
            var pos = new Vector2(localPosition.x, localPosition.z);

            // use potentially expanded range to compute 01 value in height stamp
            var pos01 = pos / realSize;
            var rotation = transform.rotation.eulerAngles.y;
            var m = Matrix4x4.Translate(-pos01);
            m = Matrix4x4.Rotate(Quaternion.AngleAxis(rotation, Vector3.forward)) * m;
            // Use the actual size to compute the matrix scale
            m = Matrix4x4.Scale(new Vector2(ts.x, ts.z) / size2D) * m;
            m = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * m;
            return m;
        }

        public static int FindTextureChannelIndex(Terrain terrain, TerrainLayer layer)
        {
            var layers = terrain.terrainData.terrainLayers;
            for (var index = 0; index < layers.Length; index++)
            {
                var l = layers[index];
                if (!ReferenceEquals(l, layer))
                    continue;
                return index;
            }

            return -1;
        }

        public static int FindTreeIndex(Terrain terrain, GameObject prefab)
        {
            var protos = terrain.terrainData.treePrototypes;
            for (var index = 0; index < protos.Length; index++)
            {
                var l = protos[index];
                if (!ReferenceEquals(l.prefab, prefab))
                    continue;
                return index;
            }

            return -1;
        }


        public static void EnsureTexturesAreOnTerrain(Terrain terrain, List<TerrainLayer> prototypes)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;
            List<TerrainLayer> resultLayers = new List<TerrainLayer>(terrainLayers);
            bool edited = false;
            int index = -1;
            foreach (var prototype in prototypes.Distinct())
            {
                for (int i = 0; i < terrainLayers.Length; ++i)
                {
                    var tp = terrainLayers[i];
                    if (ReferenceEquals(prototype, tp))
                    {
                        index = i;
                    }
                }
                if (index < 0)
                {
                    resultLayers.Add(prototype);
                    
                    edited = true;

                }
            }
            if (edited)
            {
                terrain.terrainData.terrainLayers = resultLayers.ToArray();
            }
        }


    }
}
