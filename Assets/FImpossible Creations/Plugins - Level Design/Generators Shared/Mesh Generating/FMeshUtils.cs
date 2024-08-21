using Parabox.CSG;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public static partial class FMeshUtils
    {

        public static void SmoothMeshNormals(Mesh m, float hard)
        {
            m.RecalculateNormals();
            if (hard <= 0f) return;
            
            RecalculateNormals(m, Mathf.Lerp(180f, 0f, hard));

            //var verts = m.vertices;
            //var triangles = m.triangles;
            //Vector3[] normals = new Vector3[verts.Length];

            //List<Vector3>[] vertexNormals = new List<Vector3>[verts.Length];

            //for (int i = 0; i < vertexNormals.Length; i++)
            //{
            //    vertexNormals[i] = new List<Vector3>();
            //}

            //for (int i = 0; i < triangles.Length; i += 3)
            //{
            //    Vector3 currNormal = Vector3.Cross(
            //        (verts[triangles[i + 1]] - verts[triangles[i]]).normalized,
            //        (verts[triangles[i + 2]] - verts[triangles[i]]).normalized);

            //    vertexNormals[triangles[i]].Add(currNormal);
            //    vertexNormals[triangles[i + 1]].Add(currNormal);
            //    vertexNormals[triangles[i + 2]].Add(currNormal);
            //}

            //for (int i = 0; i < vertexNormals.Length; i++)
            //{
            //    normals[i] = Vector3.zero;

            //    float numNormals = vertexNormals[i].Count;
            //    for (int j = 0; j < numNormals; j++)
            //    {
            //        normals[i] += vertexNormals[i][j];
            //    }

            //    normals[i] /= numNormals;

            //    if (hard > 0.05f)
            //    {
            //        if (normals[i].sqrMagnitude > Mathf.Epsilon)
            //        {
            //            Quaternion look = Quaternion.LookRotation(normals[i]);
            //            Vector3 sm = look.eulerAngles;
            //            sm = FVectorMethods.FlattenVector(sm, hard * 90f);
            //            normals[i] = Quaternion.Euler(sm) * Vector3.forward;
            //        }
            //    }
            //}

            //m.normals = normals;
        }


        public static Mesh MeshesOperation(Mesh combined, Mesh removeCombination, Parabox.CSG.CSG.BooleanOp operation, bool flipCaps = false)
        {
            if (operation == Parabox.CSG.CSG.BooleanOp.None) return combined;

            Material defMat = new Material(Shader.Find("Diffuse"));
            Model result;

            if (operation == CSG.BooleanOp.Intersection)
                result = CSG.Intersect(combined, defMat, Matrix4x4.identity, removeCombination, defMat, Matrix4x4.identity, true);
            else if (operation == CSG.BooleanOp.Subtraction)
                result = CSG.Subtract(combined, defMat, Matrix4x4.identity, removeCombination, defMat, Matrix4x4.identity, true, flipCaps);
            else //if (operation == CSG.BooleanOp.Union)
                result = CSG.Union(combined, defMat, Matrix4x4.identity, removeCombination, defMat, Matrix4x4.identity, true);


            return result.mesh;
        }


        public static Mesh AdjustOrigin(Mesh m, EOrigin origin)
        {
            m.RecalculateBounds();

            if (origin == EOrigin.Unchanged) return m;
            else if (origin == EOrigin.Center)
            {
                Vector3 off = -m.bounds.center;
                var verts = m.vertices;

                // Center Offset
                for (int v = 0; v < verts.Length; v++) verts[v] += off;


                m.SetVerticesUnity2018(verts);
            }
            else if (origin == EOrigin.BottomCenter)
            {
                Vector3 off = new Vector3(-m.bounds.center.x, -m.bounds.min.y, -m.bounds.center.z);

                var verts = m.vertices;
                for (int v = 0; v < verts.Length; v++) verts[v] += off;

                m.SetVerticesUnity2018(verts);
            }
            else if (origin == EOrigin.TopCenter)
            {
                Vector3 off = new Vector3(-m.bounds.center.x, -m.bounds.max.y, -m.bounds.center.z);

                var verts = m.vertices;
                for (int v = 0; v < verts.Length; v++) verts[v] += off;

                m.SetVerticesUnity2018(verts);
            }
            else if (origin == EOrigin.BottomLeft)
            {
                Vector3 off = new Vector3(-m.bounds.min.x, -m.bounds.min.y, -m.bounds.min.z);

                var verts = m.vertices;
                for (int v = 0; v < verts.Length; v++) verts[v] += off;

                m.SetVerticesUnity2018(verts);
            }
            else if (origin == EOrigin.BottomCenterBack)
            {
                Vector3 off = new Vector3(-m.bounds.center.x, -m.bounds.min.y, -m.bounds.min.z);

                var verts = m.vertices;
                for (int v = 0; v < verts.Length; v++) verts[v] += off;

                m.SetVerticesUnity2018(verts);
            }
            else if (origin == EOrigin.BottomCenterFront)
            {
                Vector3 off = new Vector3(-m.bounds.center.x, -m.bounds.min.y, -m.bounds.max.z);

                var verts = m.vertices;
                for (int v = 0; v < verts.Length; v++) verts[v] += off;

                m.SetVerticesUnity2018(verts);
            }

            return m;
        }


        #region Unity 2018 Support


        public static void SetVerticesUnity2018(this Mesh m, Vector3[] verts)
        {
#if UNITY_2019_4_OR_NEWER
            m.SetVertices(verts);
#else
            m.vertices = verts;
#endif
        }

        public static void SetUVUnity2018(this Mesh m, Vector2[] uv)
        {
#if UNITY_2019_4_OR_NEWER
            m.SetUVs(0, uv);
#else
            m.uv = uv;
#endif
        }

        public static void SetNormalsUnity2018(this Mesh m, Vector3[] norm)
        {
#if UNITY_2019_4_OR_NEWER
            m.SetNormals(norm);
#else
            m.normals = norm;
#endif
        }

        public static void SetTrianglesUnity2018(this Mesh m, int[] tris)
        {
#if UNITY_2019_4_OR_NEWER
            m.SetTriangles(tris, 0);
#else
            m.triangles = tris;
#endif
        }


        public static void SetColorsUnity2018(this Mesh m, List<Color> c)
        {
#if UNITY_2019_4_OR_NEWER
            m.SetColors(c);
#else
            m.colors = c.ToArray();
#endif
        }


        #endregion



        public static void OffsetUV(Mesh mesh, Vector2 uVOffset)
        {
            Vector2[] uvs = mesh.uv;
            for (int u = 0; u < uvs.Length; u++)
            {
                uvs[u] = new Vector2((uvs[u].x + uVOffset.x), (uvs[u].y + uVOffset.y));
            }

            mesh.SetUVUnity2018(uvs);
        }

        public static void RotateUV(Mesh mesh, float angle)
        {
            Vector2[] uvs = mesh.uv;

            float rad = angle * Mathf.Deg2Rad;

            float rotMatrix00 = Mathf.Cos(rad);
            float rotMatrix01 = -Mathf.Sin(rad);
            float rotMatrix10 = Mathf.Sin(rad);
            float rotMatrix11 = Mathf.Cos(rad);

            Vector2 halfV2 = new Vector2(0.5f, 0.5f);

            for (int j = 0; j < uvs.Length; j++)
            {
                uvs[j] = uvs[j] - halfV2;
                float u = rotMatrix00 * uvs[j].x + rotMatrix01 * uvs[j].y;
                float v = rotMatrix10 * uvs[j].x + rotMatrix11 * uvs[j].y;
                uvs[j].x = u; uvs[j].y = v;
                uvs[j] = uvs[j] + halfV2;
            }

            mesh.SetUVUnity2018(uvs);
        }

        public static void RescaleUV(Mesh mesh, Vector2 uVReScale)
        {
            Vector2[] uvs = mesh.uv;
            for (int u = 0; u < uvs.Length; u++)
            {
                uvs[u] = new Vector2((uvs[u].x * uVReScale.x), (uvs[u].y * uVReScale.y));
            }

            mesh.SetUVUnity2018(uvs);
        }

        public static void FlipNormals(Mesh mesh)
        {
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < normals.Length; i++) normals[i] = -normals[i];
            mesh.SetNormalsUnity2018(normals);

            int[] triangles = mesh.GetTriangles(0);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i + 0];
                triangles[i + 0] = triangles[i + 1];
                triangles[i + 1] = temp;
            }

            mesh.SetTrianglesUnity2018(triangles);
        }

        public static void SmoothNormals(Mesh mesh)
        {
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < mesh.vertices.Length; i++)
                for (int j = i + 1; j < mesh.vertices.Length; j++)
                    if (mesh.vertices[i] == mesh.vertices[j])
                    {
                        Vector3 averagedNormal = (normals[i] + normals[j]) / 2;
                        normals[i] = averagedNormal;
                        normals[j] = averagedNormal;
                    }

            mesh.normals = normals;
        }










        #region Recalculate angles thanks to the article by Francesco Cucchiara: https://medium.com/@fra3point/runtime-normals-recalculation-in-unity-a-complete-approach-db42490a5644

        public static void RecalculateNormals(this Mesh mesh, float angle)
        {
            UnweldVertices(mesh);

            float cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = new Vector3[vertices.Length];

            // Holds the normal of each triangle in each sub mesh.
            Vector3[][] triNormals = new Vector3[mesh.subMeshCount][];

            Dictionary<VertexKey, List<VertexEntry>> dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
            {

                int[] triangles = mesh.GetTriangles(subMeshIndex);

                triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    // Calculate the normal of the triangle
                    Vector3 p1 = vertices[i2] - vertices[i1];
                    Vector3 p2 = vertices[i3] - vertices[i1];
                    Vector3 normal = Vector3.Cross(p1, p2);
                    float magnitude = normal.magnitude;
                    if (magnitude > 0)
                    {
                        normal /= magnitude;
                    }

                    int triIndex = i / 3;
                    triNormals[subMeshIndex][triIndex] = normal;

                    List<VertexEntry> entry;
                    VertexKey key;

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out entry))
                    {
                        entry = new List<VertexEntry>(4);
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                }
            }

            // Each entry in the dictionary represents a unique vertex position.

            foreach (List<VertexEntry> vertList in dictionary.Values)
            {
                for (int i = 0; i < vertList.Count; ++i)
                {

                    Vector3 sum = new Vector3();
                    VertexEntry lhsEntry = vertList[i];

                    for (int j = 0; j < vertList.Count; ++j)
                    {
                        VertexEntry rhsEntry = vertList[j];

                        if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                        {
                            sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                        }
                        else
                        {
                            // The dot product is the cosine of the angle between the two triangles.
                            // A larger cosine means a smaller angle.
                            float dot = Vector3.Dot(
                                triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                                triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                            if (dot >= cosineThreshold)
                            {
                                sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            }
                        }
                    }

                    normals[lhsEntry.VertexIndex] = sum.normalized;
                }
            }

            mesh.normals = normals;
        }

        private struct VertexKey
        {
            private readonly long _x;
            private readonly long _y;
            private readonly long _z;

            // Change this if you require a different precision.
            private const int Tolerance = 100000;

            // Magic FNV values. Do not change these.
            private const long FNV32Init = 0x811c9dc5;
            private const long FNV32Prime = 0x01000193;

            public VertexKey(Vector3 position)
            {
                _x = (long)(Mathf.Round(position.x * Tolerance));
                _y = (long)(Mathf.Round(position.y * Tolerance));
                _z = (long)(Mathf.Round(position.z * Tolerance));
            }

            public override bool Equals(object obj)
            {
                VertexKey key = (VertexKey)obj;
                return _x == key._x && _y == key._y && _z == key._z;
            }

            public override int GetHashCode()
            {
                long rv = FNV32Init;
                rv ^= _x;
                rv *= FNV32Prime;
                rv ^= _y;
                rv *= FNV32Prime;
                rv ^= _z;
                rv *= FNV32Prime;

                return rv.GetHashCode();
            }
        }

        private struct VertexEntry
        {
            public int MeshIndex;
            public int TriangleIndex;
            public int VertexIndex;

            public VertexEntry(int meshIndex, int triIndex, int vertIndex)
            {
                MeshIndex = meshIndex;
                TriangleIndex = triIndex;
                VertexIndex = vertIndex;
            }
        }


        public static void UnweldVertices(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;

            List<Vector3> unweldedVerticesList = new List<Vector3>();
            int[][] unweldedSubTriangles = new int[mesh.subMeshCount][];
            List<Vector2> unweldedUvsList = new List<Vector2>();
            int currVertex = 0;

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] triangles = mesh.GetTriangles(i);
                Vector3[] unweldedVertices = new Vector3[triangles.Length];
                int[] unweldedTriangles = new int[triangles.Length];
                Vector2[] unweldedUVs = new Vector2[unweldedVertices.Length];

                for (int j = 0; j < triangles.Length; j++)
                {
                    //unwelded vertices are just all the vertices as they appear in the triangles array
                    unweldedVertices[j] = vertices[triangles[j]];
                    if (uvs.Length > triangles[j])
                    {
                        unweldedUVs[j] = uvs[triangles[j]];
                    }
                    //the unwelded triangle array will contain global progressive vertex indexes (1, 2, 3, ...) 
                    unweldedTriangles[j] = currVertex;
                    currVertex++;
                }

                unweldedVerticesList.AddRange(unweldedVertices);
                unweldedSubTriangles[i] = unweldedTriangles;
                unweldedUvsList.AddRange(unweldedUVs);
            }

            mesh.vertices = unweldedVerticesList.ToArray();
            mesh.uv = unweldedUvsList.ToArray();

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                mesh.SetTriangles(unweldedSubTriangles[i], i, false);
            }

            RecalculateTangents(mesh);
        }


        public static void RecalculateTangents(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Vector3[] normals = mesh.normals;

            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            Vector4[] tangents = new Vector4[vertexCount];

            for (int a = 0; a < triangleCount; a += 3)
            {
                int i1 = triangles[a + 0];
                int i2 = triangles[a + 1];
                int i3 = triangles[a + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uv[i1];
                Vector2 w2 = uv[i2];
                Vector2 w3 = uv[i3];

                float x1 = v2.x - v1.x;
                float x2 = v3.x - v1.x;
                float y1 = v2.y - v1.y;
                float y2 = v3.y - v1.y;
                float z1 = v2.z - v1.z;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;
                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                float div = s1 * t2 - s2 * t1;
                float r = div == 0.0f ? 0.0f : 1.0f / div;

                Vector3 sDir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tDir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sDir;
                tan1[i2] += sDir;
                tan1[i3] += sDir;

                tan2[i1] += tDir;
                tan2[i2] += tDir;
                tan2[i3] += tDir;
            }

            for (int a = 0; a < vertexCount; ++a)
            {
                Vector3 n = normals[a];
                Vector3 t = tan1[a];

                Vector3.OrthoNormalize(ref n, ref t);
                tangents[a].x = t.x;
                tangents[a].y = t.y;
                tangents[a].z = t.z;

                tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
            }

            mesh.tangents = tangents;
        }


        #endregion


    }
}