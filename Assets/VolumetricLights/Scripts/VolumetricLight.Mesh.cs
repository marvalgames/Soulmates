//------------------------------------------------------------------------------------------------------------------
// Volumetric Lights
// Created by Kronnect
//------------------------------------------------------------------------------------------------------------------

using System;
using UnityEngine;
using System.Collections.Generic;

namespace VolumetricLights {

    public partial class VolumetricLight : MonoBehaviour {

        const int SIDES = 32;
        readonly List<Vector3> vertices = new List<Vector3>(32);
        readonly List<int> indices = new List<int>(32);

        public float generatedRange = -1;
        public float generatedTipRadius = -1;
        public float generatedSpotAngle = -1;
        public float generatedBaseRadius;
        public float generatedAreaWidth, generatedAreaHeight, generatedAreaFrustumAngle, generatedAreaFrustumMultiplier;
        public LightType generatedType;

        bool CheckMesh() {

            if (meshRenderer != null && !autoToggle) {
                bool isEnabled = lightComp.enabled || alwaysOn;
                meshRenderer.enabled = isEnabled;
            }

            if (!useCustomSize) {
#if UNITY_EDITOR
                areaWidth = lightComp.areaSize.x;
                areaHeight = lightComp.areaSize.y;
#endif
                customRange = lightComp.range;
            }

            bool needMesh = false;
            mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) {
                needMesh = true;
            }

            switch (lightComp.type) {
                case LightType.Spot:
                    if (needMesh || generatedType != LightType.Spot || customRange != generatedRange || lightComp.spotAngle != generatedSpotAngle || tipRadius != generatedTipRadius) {
                        GenerateConeMesh();
                        return true;
                    }
                    break;
                case LightType.Point:
                    if (needMesh || generatedType != LightType.Point || customRange != generatedRange) {
                        GenerateSphereMesh();
                        return true;
                    }
                    break;
                case LightType.Rectangle:
                    if (needMesh || generatedType != LightType.Rectangle || customRange != generatedRange || areaWidth != generatedAreaWidth || areaHeight != generatedAreaHeight || frustumAngle != generatedAreaFrustumAngle) {
                        GenerateCubeMesh();
                        return true;
                    }
                    break;
                case LightType.Disc:
                    if (needMesh || generatedType != LightType.Disc || customRange != generatedRange || areaWidth != generatedAreaWidth || frustumAngle != generatedAreaFrustumAngle) {
                        GenerateCylinderMesh();
                        return true;
                    }
                    break;
                default:
                    if (meshRenderer != null) {
                        meshRenderer.enabled = false;
                    }
                    break;
            }
            return false;
        }

        void UpdateMesh(string name) {
            mf = GetComponent<MeshFilter>();
            if (mf == null) {
                mf = gameObject.AddComponent<MeshFilter>();
            }

            Mesh mesh = mf.sharedMesh;
            if (mesh == null) {
                mesh = new Mesh();
            } else {
                mesh.Clear();
            }
            mesh.name = name;
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mf.mesh = mesh;

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            } else if (!autoToggle) {
                meshRenderer.enabled = true;
            }
        }

        void NormalizeScale() {
            Transform parent = transform.parent;
            if (parent == null) return;
            Vector3 lossyScale = parent.lossyScale;
            if ((lossyScale.x != 1 || lossyScale.y != 1 || lossyScale.z != 1) && lossyScale.x != 0 && lossyScale.y != 0 && lossyScale.z != 0) {
                lossyScale.x = 1f / lossyScale.x;
                lossyScale.y = 1f / lossyScale.y;
                lossyScale.z = 1f / lossyScale.z;
                if (transform.localScale != lossyScale) {
                    transform.localScale = lossyScale;
                }
            }
        }


#region Cone mesh generation

        void GenerateConeMesh() {

            NormalizeScale();

            vertices.Clear();
            indices.Clear();

            generatedType = LightType.Spot;
            generatedTipRadius = tipRadius;
            generatedSpotAngle = lightComp.spotAngle;
            generatedRange = customRange;
            generatedBaseRadius = Mathf.Tan(lightComp.spotAngle * Mathf.Deg2Rad * 0.5f) * customRange;

            Vector3 p = Vector3.zero;

            // Top & Bottom vertices
            for (int k = 0; k < SIDES; k++) {
                float r = 2f * Mathf.PI * k / SIDES;
                float cos = Mathf.Cos(r);
                float sin = Mathf.Sin(r);
                p.x = cos * generatedTipRadius;
                p.y = sin * generatedTipRadius;
                p.z = 0;
                vertices.Add(p);
                p.x = cos * generatedBaseRadius;
                p.y = sin * generatedBaseRadius;
                p.z = customRange;
                vertices.Add(p);
            }

            // Build faces
            int vertexCount = SIDES * 2;
            for (int k = 0; k < vertexCount; k += 2) {
                int tl = k;
                int bl = k + 1;
                int tr = (k + 2) % vertexCount;
                int br = (k + 3) % vertexCount;
                // First tri
                indices.Add(tl);
                indices.Add(tr);
                indices.Add(bl);
                // Second tri
                indices.Add(tr);
                indices.Add(br);
                indices.Add(bl);
            }

            // Build top cap
            vertices.Add(Vector3.zero);
            int topCapCenterIndex = vertexCount;
            for (int k = 0; k < vertexCount; k += 2) {
                // First tri
                indices.Add(k);
                indices.Add(topCapCenterIndex);
                indices.Add((k + 2) % vertexCount);
            }

            // Build bottom cap
            vertices.Add(new Vector3(0, 0, customRange));
            int bottomCapCenterIndex = vertexCount + 1;
            for (int k = 0; k < vertexCount; k += 2) {
                // First tri
                indices.Add(bottomCapCenterIndex);
                indices.Add(k + 1);
                indices.Add((k + 3) % vertexCount);
            }

            UpdateMesh("Capped Cone");
        }

#endregion

#region Cube mesh generation

        static readonly Vector3[] faceVerticesForward = {
            new Vector3 (0.5f, -0.5f, 1f),
            new Vector3 (0.5f, 0.5f, 1f),
            new Vector3 (-0.5f, -0.5f, 1f),
            new Vector3 (-0.5f, 0.5f, 1f)
        };
        static readonly Vector3[] faceVerticesBack = {
            new Vector3 (-0.5f, -0.5f, 0f),
            new Vector3 (-0.5f, 0.5f, 0f),
            new Vector3 (0.5f, -0.5f, 0f),
            new Vector3 (0.5f, 0.5f, 0f)
        };
        static readonly Vector3[] faceVerticesLeft = {
            new Vector3 (-0.5f, -0.5f, 1f),
            new Vector3 (-0.5f, 0.5f, 1f),
            new Vector3 (-0.5f, -0.5f, 0f),
            new Vector3 (-0.5f, 0.5f, 0f)
        };
        static readonly Vector3[] faceVerticesRight = {
            new Vector3 (0.5f, -0.5f, 0f),
            new Vector3 (0.5f, 0.5f, 0f),
            new Vector3 (0.5f, -0.5f, 1f),
            new Vector3 (0.5f, 0.5f, 1f)
        };
        static readonly Vector3[] faceVerticesTop =  {
            new Vector3 (-0.5f, 0.5f, 0f),
            new Vector3 (-0.5f, 0.5f, 1f),
            new Vector3 (0.5f, 0.5f, 0f),
            new Vector3 (0.5f, 0.5f, 1f)
        };
        static readonly Vector3[] faceVerticesBottom = {
            new Vector3 (0.5f, -0.5f, 0f),
            new Vector3 (0.5f, -0.5f, 1f),
            new Vector3 (-0.5f, -0.5f, 0f),
            new Vector3 (-0.5f, -0.5f, 1f)
        };

        void GenerateCubeMesh() {

            NormalizeScale();

            generatedType = LightType.Rectangle;
            generatedRange = customRange;
            generatedAreaWidth = areaWidth;
            generatedAreaHeight = areaHeight;

            generatedAreaFrustumAngle = frustumAngle;
            generatedAreaFrustumMultiplier = 1f + Mathf.Tan(frustumAngle * Mathf.Deg2Rad);

            vertices.Clear();
            indices.Clear();

            AddFace(faceVerticesBack);
            AddFace(faceVerticesForward);
            AddFace(faceVerticesLeft);
            AddFace(faceVerticesRight);
            AddFace(faceVerticesTop);
            AddFace(faceVerticesBottom);

            UpdateMesh("Box");
        }

        void AddFace(Vector3[] faceVertices) {
            int index = vertices.Count;
            for (int k = 0; k < faceVertices.Length; k++) {
                Vector3 v = faceVertices[k];
                v.x *= generatedAreaWidth;
                v.y *= generatedAreaHeight;
                if (v.z > 0) {
                    v.x *= generatedAreaFrustumMultiplier;
                    v.y *= generatedAreaFrustumMultiplier;
                }
                v.z *= generatedRange;
                vertices.Add(v);
            }
            indices.Add(index);
            indices.Add(index + 1);
            indices.Add(index + 3);
            indices.Add(index + 3);
            indices.Add(index + 2);
            indices.Add(index);
        }

#endregion

#region Sphere mesh generation

        void GenerateSphereMesh() {

            NormalizeScale();

            generatedRange = customRange;
            generatedType = LightType.Point;

            vertices.Clear();
            indices.Clear();

            const int nbLong = 24;
            const int nbLat = 16;

            const float _2pi = Mathf.PI * 2f;

            vertices.Add(Vector3.up * generatedRange);
            for (int lat = 0; lat < nbLat; lat++) {
                float a1 = Mathf.PI * (float)(lat + 1) / (nbLat + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= nbLong; lon++) {
                    float a2 = _2pi * (float)(lon == nbLong ? 0 : lon) / nbLong;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices.Add(new Vector3(sin1 * cos2, cos1, sin1 * sin2) * generatedRange);
                }
            }
            vertices.Add(Vector3.down * generatedRange);

            //Top Cap
            for (int lon = 0; lon < nbLong; lon++) {
                indices.Add(lon + 2);
                indices.Add(lon + 1);
                indices.Add(0);
            }

            //Middle
            for (int lat = 0; lat < nbLat - 1; lat++) {
                for (int lon = 0; lon < nbLong; lon++) {
                    int current = lon + lat * (nbLong + 1) + 1;
                    int next = current + nbLong + 1;

                    indices.Add(current);
                    indices.Add(current + 1);
                    indices.Add(next + 1);

                    indices.Add(current);
                    indices.Add(next + 1);
                    indices.Add(next);
                }
            }

            //Bottom Cap
            int vertexCount = vertices.Count;
            for (int lon = 0; lon < nbLong; lon++) {
                indices.Add(vertexCount - 1);
                indices.Add(vertexCount - (lon + 2) - 1);
                indices.Add(vertexCount - (lon + 1) - 1);
            }

            UpdateMesh("Sphere");

        }

#endregion

#region Cylinder mesh generation

        void GenerateCylinderMesh() {

            NormalizeScale();

            generatedType = LightType.Disc;
            generatedRange = customRange;
            generatedAreaWidth = generatedAreaHeight = areaWidth;
            generatedAreaFrustumAngle = frustumAngle;
            generatedAreaFrustumMultiplier = 1f + Mathf.Tan(frustumAngle * Mathf.Deg2Rad);

            vertices.Clear();
            indices.Clear();

            Vector3 p = Vector3.zero;

            // Top & Bottom vertices
            for (int k = 0; k < SIDES; k++) {
                float r = 2f * Mathf.PI * k / SIDES;
                float cos = Mathf.Cos(r);
                float sin = Mathf.Sin(r);
                p.x = cos * generatedAreaWidth;
                p.y = sin * generatedAreaWidth;
                p.z = 0;
                vertices.Add(p);
                p.x *= generatedAreaFrustumMultiplier;
                p.y *= generatedAreaFrustumMultiplier;
                p.z = generatedRange;
                vertices.Add(p);
            }

            // Build faces
            int vertexCount = SIDES * 2;
            for (int k = 0; k < vertexCount; k += 2) {
                int tl = k;
                int bl = k + 1;
                int tr = (k + 2) % vertexCount;
                int br = (k + 3) % vertexCount;
                // First tri
                indices.Add(tl);
                indices.Add(tr);
                indices.Add(bl);
                // Second tri
                indices.Add(tr);
                indices.Add(br);
                indices.Add(bl);
            }

            // Build top cap
            vertices.Add(Vector3.zero);
            int topCapCenterIndex = vertexCount;
            for (int k = 0; k < vertexCount; k += 2) {
                // First tri
                indices.Add(k);
                indices.Add(topCapCenterIndex);
                indices.Add((k + 2) % vertexCount);
            }

            // Build bottom cap
            vertices.Add(new Vector3(0, 0, generatedRange));
            int bottomCapCenterIndex = vertexCount + 1;
            for (int k = 0; k < vertexCount; k += 2) {
                // First tri
                indices.Add(bottomCapCenterIndex);
                indices.Add(k + 1);
                indices.Add((k + 3) % vertexCount);
            }

            UpdateMesh("Cylinder");
        }

#endregion

    }


}
