using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace FIMSpace.Generating
{
    public static partial class FMeshUtils
    {
        public static Mesh GetSourceMeshCopy(Mesh SourceMesh, Vector3 rotate, EOrigin origin = EOrigin.Unchanged)
        {
            return GetSourceMeshCopy(SourceMesh, rotate, Vector3.one, origin);
        }

        public static Mesh GetSourceMeshCopy(Mesh SourceMesh, Vector3 rotate, Vector3 scale, EOrigin origin = EOrigin.Unchanged)
        {
            Mesh src = GameObject.Instantiate(SourceMesh);

            if (rotate != Vector3.zero || scale != Vector3.one)
            {
                Matrix4x4 mx = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rotate), scale);
                var verts = src.vertices;
                for (int v = 0; v < verts.Length; v++) verts[v] = mx.MultiplyPoint3x4(verts[v]);
                src.SetVertices(verts);
            }

            if (origin != EOrigin.Unchanged) FMeshUtils.AdjustOrigin(src, origin);

            return src;
        }

        public static Mesh GeneratePlaneMesh(List<FMeshUtils.PolyShapeHelpPoint> vGenPoints, Matrix4x4 mx)
        {
            for (int t = 0; t < vGenPoints.Count; t++) vGenPoints[t].helpIndex = t;
            vGenPoints.Reverse();

            List<Vector3> verts = new List<Vector3>();
            for (int i = 0; i < vGenPoints.Count; i++)
            {
                verts.Add(mx.MultiplyPoint3x4(new Vector3(vGenPoints[i].vxPos.x, vGenPoints[i].vxPos.z, 0f)));
                vGenPoints[i].index = i;
            }

            var tris = FMeshUtils.TriangulateConcavePolygon(vGenPoints);
            tris.Reverse();

            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            RenameMesh(mesh);

            return mesh;
        }

        /// <summary>
        /// Requires positions set in x and z axes
        /// </summary>
        public static Mesh GenerateCoverageMesh(List<FMeshUtils.PolyShapeHelpPoint> vGenPoints, Matrix4x4 mx, float depth)
        {
            List<Vector3> verts = new List<Vector3>();
            for (int i = 0; i < vGenPoints.Count; i++)
            {
                verts.Add(mx.MultiplyPoint3x4(new Vector3(vGenPoints[i].vxPos.x, vGenPoints[i].vxPos.z, 0f)));
                vGenPoints[i].index = i;
            }

            for (int i = 0; i < vGenPoints.Count; i++)
                verts.Add(mx.MultiplyPoint3x4(new Vector3(vGenPoints[i].vxPos.x, vGenPoints[i].vxPos.z, depth)));

            var tris = new List<int>();

            int depthOff = vGenPoints.Count;
            for (int i = 0; i < vGenPoints.Count - 1; i += 1)
            {
                // generating triangle bridge
                // u -> uf   uf -> d   d -> df
                tris.Add(vGenPoints[i].index);
                tris.Add(vGenPoints[i].index + depthOff + 1);
                tris.Add(vGenPoints[i].index + 1);

                tris.Add(vGenPoints[i].index + depthOff + 1);
                tris.Add(vGenPoints[i].index);
                tris.Add(vGenPoints[i].index + depthOff);
            }

            // Loop last poly
            tris.Add(vGenPoints[vGenPoints.Count - 1].index);
            tris.Add(depthOff);
            tris.Add(0);

            tris.Add(depthOff);
            tris.Add(vGenPoints[vGenPoints.Count - 1].index);
            tris.Add(vGenPoints[vGenPoints.Count - 1].index + depthOff);
            tris.Reverse();

            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            RenameMesh(mesh);

            return mesh;
        }


        public static Mesh ExtractSingleSubmesh(Mesh sourceMesh, int subMeshIndex)
        {
            if (sourceMesh.subMeshCount < 2) return sourceMesh;
            if (subMeshIndex >= sourceMesh.subMeshCount) return sourceMesh;
            if (subMeshIndex < 0) return sourceMesh;

            var vertices = sourceMesh.vertices;
            var normals = sourceMesh.normals;
            var uvs = sourceMesh.uv;
            var cols = sourceMesh.colors;
            var tang = sourceMesh.tangents;

            var newVerts = new List<Vector3>();
            var newNorms = new List<Vector3>();
            var newTangents = new List<Vector4>();
            var newTris = new List<int>();
            var newUVs = new List<Vector2>();
            var newColors = new List<Color>();
            var triangles = sourceMesh.GetTriangles(subMeshIndex);

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var A = triangles[i + 0];
                var B = triangles[i + 1];
                var C = triangles[i + 2];

                newVerts.Add(vertices[A]);
                newVerts.Add(vertices[B]);
                newVerts.Add(vertices[C]);

                newNorms.Add(normals[A]);
                newNorms.Add(normals[B]);
                newNorms.Add(normals[C]);

                if (cols.Length > 0)
                {
                    newColors.Add(cols[A]);
                    newColors.Add(cols[B]);
                    newColors.Add(cols[C]);
                }

                newUVs.Add(uvs[A]);
                newUVs.Add(uvs[B]);
                newUVs.Add(uvs[C]);

                if (tang.Length > 0)
                {
                    newTangents.Add(tang[A]);
                    newTangents.Add(tang[B]);
                    newTangents.Add(tang[C]);
                }

                newTris.Add(newTris.Count);
                newTris.Add(newTris.Count);
                newTris.Add(newTris.Count);
            }

            var mesh = new Mesh();
            mesh.name = sourceMesh.name;

            mesh.indexFormat = newVerts.Count > 65536 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(newVerts);
            mesh.SetNormals(newNorms);
            mesh.SetUVs(0, newUVs);
            if (newTangents.Count > 0) mesh.SetTangents(newTangents);
            if (newColors.Count > 0) mesh.SetColors(newColors);
            mesh.SetTriangles(newTris, 0, true);

            return mesh;
        }




        public static Mesh CombineMeshes(List<CombineInstance> meshes)
        {
            if (meshes == null) return null;
            if (meshes.Count == 0) return null;

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(meshes.ToArray());
            RenameMesh(mesh);
            return mesh;
        }

        public static Mesh CombineMeshes(List<Mesh> meshes)
        {
            if (meshes == null) return null;
            if (meshes.Count == 0) return null;

            CombineInstance[] combines = new CombineInstance[meshes.Count];

            for (int i = 0; i < meshes.Count; i += 1)
            {
                CombineInstance comb = new CombineInstance();
                comb.mesh = meshes[i];
                comb.transform = Matrix4x4.identity;
                combines[i] = comb;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combines);

            RenameMesh(mesh);
            return mesh;
        }


        public static Mesh CombineMeshesForSubMesh(Mesh joinWith, List<CombineInstance> groupToMerge)
        {
            if (joinWith == null) return null;
            if (groupToMerge == null) return joinWith;

            CombineInstance[] combines = groupToMerge.ToArray();
            Mesh tempMesh = new Mesh();
            tempMesh.CombineMeshes(combines, true, true);

            combines = new CombineInstance[2];
            combines[0] = new CombineInstance();
            combines[0].mesh = joinWith;
            combines[1] = new CombineInstance();
            combines[1].mesh = tempMesh;

            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combines, false, false);
            RenameMesh(newMesh);
            return newMesh;
        }




        public static MeshFilter GenerateMeshFilterAndRenderer(Transform parent, List<Material> mats)
        {
            return GenerateMeshFilterAndRenderer(parent, mats.ToArray());
        }

        public static GameObject GenerateChildOf(Transform parent)
        {
            GameObject newO = new GameObject(parent.name + "-Child");
            newO.transform.SetParent(parent, true);
            newO.transform.position = parent.position;
            newO.transform.rotation = parent.rotation;
            newO.transform.localScale = Vector3.one;
            return newO;
        }

        public static MeshFilter GenerateMeshFilterAndRenderer(Transform parent, Material[] mats)
        {
            GameObject newO = GenerateChildOf(parent);
            MeshFilter filt = newO.AddComponent<MeshFilter>();
            MeshRenderer rend = newO.AddComponent<MeshRenderer>();
            rend.sharedMaterials = mats;
            return filt;
        }

        public static MeshFilter GenerateMeshFilterAndRenderer(Transform parent, Material mat)
        {
            GameObject newO = GenerateChildOf(parent);
            MeshFilter filt = newO.AddComponent<MeshFilter>();
            MeshRenderer rend = newO.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            return filt;
        }

        public static LODGroup GenerateLODGroupFor(Transform transform)
        {
            LODGroup lodG = transform.GetComponent<LODGroup>();
            if (lodG == null) lodG = transform.gameObject.AddComponent<LODGroup>();
            return lodG;
        }

        public static MeshRenderer AddMeshFilterAndRendererFor(Transform transform)
        {
            MeshFilter filt = transform.GetComponent<MeshFilter>();
            if (filt == null) /*filt = */ transform.gameObject.AddComponent<MeshFilter>();
            MeshRenderer rend = transform.GetComponent<MeshRenderer>();
            if (rend == null) rend = transform.gameObject.AddComponent<MeshRenderer>();
            return rend;
        }


        public static void ApplyLODMeshesOn(LODGroup group, Renderer rendLOD0, Renderer rendLOD1, Renderer rendLOD2 = null, float cullAtScreenSize = 0.1f, float firstLODAt = 0.9f, float LODBias = 1f)
        {
            if (group == null) return;

            Renderer[] lod0 = new Renderer[1] { rendLOD0 };
            Renderer[] lod1 = null;
            if (rendLOD1) lod1 = new Renderer[1] { rendLOD1 };
            Renderer[] lod2 = null;
            if (rendLOD2) lod2 = new Renderer[1] { rendLOD2 };

            ApplyLODMeshesOn(group, lod0, lod1, lod2, cullAtScreenSize, firstLODAt, LODBias);
        }

        /// <summary>
        /// Automatically generating/assigning child renderers for parent of rendLOD0
        /// </summary>
        public static void ApplyLODMeshesOnParentRenderer(LODGroup group, Renderer rendLOD0, Mesh lod1, Mesh lod2 = null, float cullAtScreenSize = 0.1f, float firstLODAt = 0.9f, float LODBias = 1f)
        {
            if (group == null) return;

            Renderer[] lod0r = new Renderer[1] { rendLOD0 };

            var rends = rendLOD0.transform.GetComponentsInChildren<Renderer>().ToList();
            rends.Remove(rendLOD0);

            Renderer rend1;
            if (rends.Count >= 1)
            {
                MeshFilter filter = rends[0].GetComponent<MeshFilter>();
                filter.sharedMesh = lod1;
                rend1 = rends[0];
            }
            else
            {
                MeshFilter filter = GenerateMeshFilterAndRenderer(rendLOD0.transform, rendLOD0.sharedMaterials);
                filter.sharedMesh = lod1;
                rend1 = filter.GetComponent<Renderer>();
            }

            Renderer[] lod1r = null;
            if (rend1) lod1r = new Renderer[1] { rend1 };

            Renderer[] lod2r = null;
            if (lod2 != null)
            {
                Renderer rend2;
                if (rends.Count >= 2)
                {
                    MeshFilter filter = rends[1].GetComponent<MeshFilter>();
                    filter.sharedMesh = lod2;
                    rend2 = rends[1];
                }
                else
                {
                    MeshFilter filter = GenerateMeshFilterAndRenderer(rendLOD0.transform, rendLOD0.sharedMaterials);
                    filter.sharedMesh = lod2;
                    rend2 = filter.GetComponent<Renderer>();
                }

                if (rend2) lod2r = new Renderer[1] { rend2 };
            }

            ApplyLODMeshesOn(group, lod0r, lod1r, lod2r, cullAtScreenSize, firstLODAt, LODBias);
        }

        public static void DestroyChildRenderers(Transform parent)
        {
            var rends = parent.transform.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < rends.Length; r++)
            {
                if (rends[r] == null) continue;
                if (rends[r].gameObject == null) continue;
                if (rends[r].transform == parent) continue;
                FGenerators.DestroyObject(rends[r].gameObject);
            }
        }

        public static void ApplyLODMeshesOn(LODGroup group, Renderer[] rendLOD0, Renderer[] rendLOD1, Renderer[] rendLOD2 = null, float cullAtScreenSize = 0.1f, float firstLODAt = 0.9f, float LODBias = 1f)
        {
            if (group == null) return;

            int count = 1;
            if (rendLOD1 != null) count += 1;
            if (rendLOD2 != null) count += 1;
            
            LOD[] lods = new LOD[count];
            if (rendLOD1 == null) firstLODAt = cullAtScreenSize;
            lods[0] = new LOD(firstLODAt, rendLOD0);

            if (rendLOD1 != null)
            {
                float lodPerc = (rendLOD2 != null) ? (Mathf.Lerp(Mathf.Max(firstLODAt - 0.1f, cullAtScreenSize), cullAtScreenSize, 0.6f) * LODBias) : (cullAtScreenSize * LODBias);
                lods[1] = new LOD(lodPerc, rendLOD1);

                if (rendLOD2 != null) lods[2] = new LOD(cullAtScreenSize * LODBias, rendLOD2);
            }

            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        public static void ApplyLODMeshesOn(LODGroup group, Mesh mesh0, Mesh mesh1, Material[] mats, Mesh mesh2 = null, float cullAtScreenSize = 0.1f, float firstLODAt = 0.9f, float LODBias = 1f)
        {
            if (group == null) return;
            if (!mesh0) return;

            List<Renderer> renderers = new List<Renderer>();
            if (group.transform.childCount != 0)
            {
                for (int c = 0; c < group.transform.childCount; c++)
                {
                    Renderer rend = group.transform.GetChild(c).GetComponent<MeshRenderer>();
                    if (rend) renderers.Add(rend);
                }
            }

            int tgtCount = 1;
            if (mesh1) tgtCount++;
            if (mesh2) tgtCount++;

            for (int i = renderers.Count; i < tgtCount; i++)
            {
                renderers.Add(FMeshUtils.GenerateMeshFilterAndRenderer(group.transform, mats).GetComponent<MeshRenderer>());
            }

            renderers[0].GetComponent<MeshFilter>().sharedMesh = mesh0;
            if (mesh1) renderers[1].GetComponent<MeshFilter>().sharedMesh = mesh1;
            if (mesh2) renderers[2].GetComponent<MeshFilter>().sharedMesh = mesh2;

            ApplyLODMeshesOn(group, renderers[0], renderers.Count > 1 ? renderers[1] : null, renderers.Count > 2 ? renderers[2] : null, cullAtScreenSize, firstLODAt, LODBias);
        }

    }
}