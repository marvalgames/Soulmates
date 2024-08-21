using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace JBooth.MicroVerseCore
{
    [ExecuteAlways]
    public class MeshStamp : Stamp, IHeightModifier
    {
        public enum Resolution
        {
            k32 = 32,
            k64 = 64,
            k128 = 128,
            k256 = 256,
            k512 = 512,
            k1024 = 1024,
            k2048 = 2048
        }

        public enum BlendMode
        {
            Add = 0,
            Subtract = 1,
            Fillaround = 2,
            Connect = 3
        }    

        public GameObject targetObject;
        [Tooltip("When true, the renderers on the target object will be hidden and the objects removed in build/play mode. In essence, this is used when you just want to model with the mesh and not have it exist for gameplay")]
        public bool hideRenderers = false;
        [Tooltip("Offset to Y position of result")]
        public float offset = 0;
        [Tooltip("Scale Height Result of offset")]
        [Range(0,1)]
        public float heightScale = 1;
        [Tooltip("Min/Max range of height values")]
        public Vector2 heightClamp = new Vector2(0, 1);
        [Tooltip("Resolution of the depth rendering buffer")]
        public Resolution resolution = Resolution.k256;
       
        public RenderTexture targetDepthTexture { get; set; }
        public FalloffFilter falloff = new FalloffFilter();
        [Range(0,24)]
        [Tooltip("Blurs the area between the mesh stamp and the terrain")]
        public float blur;
        [Tooltip("Do we pull terrain up towards the mesh, or down away from the mesh")]
        public BlendMode blendMode = BlendMode.Add;
        [Range(0.9f, 0.1f)]
        [Tooltip("The heighest point on the mesh terrain connnects to")]
        public float connectHeight = 0.9f;
        Material material;
        static Shader meshShader;
        static Camera cam;

        public override void StripInBuild()
        {
            if (hideRenderers && targetObject)
            {
                if (Application.isPlaying)
                    Destroy(targetObject);
                else
                    DestroyImmediate(targetObject);
            }
            base.StripInBuild();
        }

        void FitCameraToTarget(Camera cam, Bounds bounds)
        {
            if (blendMode == BlendMode.Add)
            {
                cam.transform.position = new Vector3(bounds.center.x, bounds.max.y + 10001, bounds.center.z);
                cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            else
            {
                cam.transform.position = new Vector3(bounds.center.x, bounds.min.y + 9999, bounds.center.z);
                cam.transform.rotation = Quaternion.Euler(-90, 0, 0);
            }
            
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = bounds.size.y + 1;
            float objectSize = Mathf.Max(bounds.size.x, bounds.size.z);
            cam.orthographicSize = objectSize / 2;
            cam.depthTextureMode = DepthTextureMode.Depth;
            cam.orthographic = true;
        }

        public void SetHideRenderers(GameObject go, bool enabled)
        {
            if (go == null)
                return;
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                r.enabled = enabled;
            }
        }

        List<MeshFilter> tempFilters = new List<MeshFilter>(1);
        void ScanMeshFilters(GameObject go)
        {
            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
            tempFilters.Clear();
            foreach (var f in filters)
            {
                var lodGroup = f.GetComponentInParent<LODGroup>();
                if (lodGroup != null)
                {
                    var lods = lodGroup.GetLODs();
                    if (lods.Length > 0)
                    {
                        if (lods[0].renderers != null && lods[0].renderers.Length > 0)
                        {
                            foreach (var r in lods[0].renderers)
                            {
                                if (r.gameObject == f.gameObject)
                                {
                                    if (f.sharedMesh.isReadable)
                                    {
                                        tempFilters.Add(f);
                                    }
                                    else
                                    {
                                        Debug.LogError("Mesh Filter in game object " + f.gameObject + " is not read/write, cannot use for mesh stamp", f.gameObject);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (f.sharedMesh.isReadable)
                    {
                        tempFilters.Add(f);
                    }
                    else
                    {
                        Debug.LogError("Mesh Filter in game object " + f.gameObject + " is not read/write, cannot use for mesh stamp", f.gameObject);
                    }
                }
            }
        }

        Bounds GetPrefabBounds(GameObject go)
        {
            ScanMeshFilters(go);
            Bounds b = new Bounds();
            if (tempFilters.Count > 0)
            {
                b = GeometryUtility.CalculateBounds(tempFilters[0].sharedMesh.vertices, tempFilters[0].transform.localToWorldMatrix);
                for (int i = 1; i < tempFilters.Count; ++i)
                {
                    var axisAlignedBounds = GeometryUtility.CalculateBounds(tempFilters[i].sharedMesh.vertices, tempFilters[i].transform.localToWorldMatrix);
                    b.Encapsulate(axisAlignedBounds);
                }
            }
            // need to add the offset somehow..
            int sizeExtension = (int)blur * 4;
            var size = b.size;
            size.x += 3 + sizeExtension;
            size.z += 3 + sizeExtension;
            b.size = size;
            return b;
        }


        public void RenderCamera(Camera cam, RenderTexture texture)
        {
            var cmdBuf = new CommandBuffer();
            cmdBuf.SetRenderTarget(texture);
            cmdBuf.ClearRenderTarget(true, true, Color.clear);
            cmdBuf.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
            ScanMeshFilters(targetObject);
            for (int i = 0; i < tempFilters.Count; i++)
            {
                cmdBuf.DrawMesh(tempFilters[i].sharedMesh, tempFilters[i].transform.localToWorldMatrix, tempFilters[i].GetComponent<MeshRenderer>().sharedMaterial);
            }
            
            Graphics.ExecuteCommandBuffer(cmdBuf);
        }


        public RenderTexture Capture()
        {
#if UNITY_EDITOR

            // we have to keep the camera around, which I find really annoying, but the camera will
            // render black if we delete it after rendering. Yes, after.
            Camera depthCamera = cam;
            RenderTexture depthTexture;
            if (depthCamera == null)
            {
                GameObject go = new GameObject();
                depthCamera = go.AddComponent<Camera>();
                cam = depthCamera;
                go.hideFlags = HideFlags.HideAndDontSave;
            }
            if (hideRenderers)
            {
                SetHideRenderers(targetObject, true);
            }
            depthCamera.enabled = false;
            depthTexture = new RenderTexture((int)resolution, (int)resolution, 24, RenderTextureFormat.Depth);
            depthTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;
            
            depthTexture.Create();

            // Setup the camera
            depthCamera.clearFlags = CameraClearFlags.SolidColor;
            depthCamera.backgroundColor = Color.black;
            depthCamera.targetTexture = depthTexture;
            depthCamera.renderingPath = RenderingPath.Forward;
            depthCamera.depthTextureMode = DepthTextureMode.Depth;
            FitCameraToTarget(depthCamera, GetPrefabBounds(targetObject));
            var oldPos = targetObject.transform.position;
            targetObject.transform.position = oldPos + new Vector3(0, 10000, 0);

            bool fog = RenderSettings.fog;
            var ambInt = RenderSettings.ambientIntensity;
            var reflectInt = RenderSettings.reflectionIntensity;
            RenderSettings.ambientIntensity = 0;
            RenderSettings.reflectionIntensity = 0;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            RenderCamera(depthCamera, depthTexture);
            targetObject.transform.position = oldPos;
            Unsupported.SetRenderSettingsUseFogNoDirty(fog);
            RenderSettings.ambientIntensity = ambInt;
            RenderSettings.reflectionIntensity = reflectInt;

            if (hideRenderers)
            {
                SetHideRenderers(targetObject, false);
            }
            RenderTexture.active = null;
            return depthTexture;
#else
            return null;
#endif

        }

        public void Initialize()
        {
            if (targetObject != null && targetDepthTexture == null)
            {
                if (targetDepthTexture != null)
                    DestroyImmediate(targetDepthTexture);
                targetDepthTexture = Capture();
            }

            if (meshShader == null)
            {
                meshShader = Shader.Find("Hidden/MicroVerse/MeshStamp");
            }
            if (material == null)
            {
                material = new Material(meshShader);
            }
        }

        public override Bounds GetBounds()
        {
            FalloffOverride fo = GetComponentInParent<FalloffOverride>();
            var foType = falloff.filterType;
            var foFilter = falloff;
            if (fo != null && fo.enabled)
            {
                foType = fo.filter.filterType;
                foFilter = fo.filter;
            }
#if __MICROVERSE_SPLINES__
            if (foType == FalloffFilter.FilterType.SplineArea && foFilter.splineArea != null)
            {
                return foFilter.splineArea.GetBounds();
            }
#endif
            if (foType == FalloffFilter.FilterType.Global && foFilter.paintArea != null && foFilter.paintArea.clampOutsideOfBounds)
            {
                return foFilter.paintArea.GetBounds();
            }
            if (targetObject != null)
            {
                Bounds b = GetPrefabBounds(targetObject);
                var size = b.size;
                size.y = 999999;
                b.size = size;
                return b;
            }
            return new Bounds();
        }

        protected override void OnDestroy()
        {
            if (targetDepthTexture != null)
            {
                DestroyImmediate(targetDepthTexture);
                targetDepthTexture = null;
            }
            base.OnDestroy();
        }

        void OnDrawGizmosSelected()
        {
            if (MicroVerse.instance != null)
            {
                Gizmos.color = MicroVerse.instance.options.colors.heightStampColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(new Vector3(0, 0, 0), Vector3.one);
            }
        }

#if UNITY_EDITOR
        public override void OnMoved()
        {
            if (targetDepthTexture != null)
            {
                DestroyImmediate(targetDepthTexture);
                targetDepthTexture = null;
            }
            base.OnMoved();
        }
#endif

        static int _AlphaMapSize = Shader.PropertyToID("_AlphaMapSize");
        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");
        static int _YBounds = Shader.PropertyToID("_YBounds");
        static int _HeightScaleClamp = Shader.PropertyToID("_HeightScaleClamp");
        static int _ConnectHeight = Shader.PropertyToID("_ConnectHeight");

        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od)
        {
            if (targetDepthTexture != null)
            {
                keywordBuilder.Clear();
                PrepareMaterial(material, heightmapData, keywordBuilder.keywords);
                material.SetFloat(_AlphaMapSize, source.width);
                var noisePos = heightmapData.terrain.transform.position;
                noisePos.x /= heightmapData.terrain.terrainData.size.x;
                noisePos.z /= heightmapData.terrain.terrainData.size.z;

                material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, GetTerrainScalingFactor(heightmapData.terrain)));
                Bounds b = GetPrefabBounds(targetObject);
                material.SetMatrix(_Transform, ComputeStampMatrix(heightmapData.terrain, b));
                material.SetVector(_YBounds, new Vector4(b.min.y - 0.5f, b.max.y + 0.5f, b.size.y + 1, offset));
                material.SetVector(_HeightScaleClamp, new Vector3(heightScale, heightClamp.x, heightClamp.y));
                material.SetFloat(_ConnectHeight, 1.0f);
                if (blendMode == BlendMode.Subtract)
                {
                    keywordBuilder.Add("_SUBTRACT");
                }
                else if(blendMode == BlendMode.Connect)
                {
                    keywordBuilder.Add("_CONNECT");
                    material.SetFloat(_ConnectHeight, connectHeight);
                }
                else if(blendMode == BlendMode.Fillaround)
                {
                    keywordBuilder.Add("_FILLAROUND");
                }
                keywordBuilder.Assign(material);
                Graphics.Blit(source, dest, material);
                return true;
            }
            return false;
        }

        static int _Transform = Shader.PropertyToID("_Transform");
        static int _RealSize = Shader.PropertyToID("_RealSize");
        static int _StampTex = Shader.PropertyToID("_StampTex");

        Matrix4x4 ComputeStampMatrix(Terrain terrain, Bounds bounds)
        {
            // rotation is baked into the stamp render, so we just use the bounds to compute everything
            // instead of the stamp transform
            var ts = terrain.terrainData.size;
            var hms = terrain.terrainData.heightmapScale;
            var hmr = terrain.terrainData.heightmapResolution;
            var realSize = new Vector2(hms.x * hmr, hms.z * hmr);

            var localPosition = terrain.transform.worldToLocalMatrix.MultiplyPoint3x4(bounds.center);

            var size = bounds.size;
            float size2D = Mathf.Max(size.x, size.z);

            var pos = new Vector2(localPosition.x, localPosition.z);
            var pos01 = pos / realSize;
            var m = Matrix4x4.Translate(-pos01);
            // Use the actual size to compute the matrix scale
            m = Matrix4x4.Scale(new Vector3(ts.x / size2D, ts.z / size2D, 0)) * m;
            m = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.0f)) * m;
            return m;
        }

        void PrepareMaterial(Material material, HeightmapData heightmapData, List<string> keywords)
        {
            
            material.SetVector(_RealSize, TerrainUtil.ComputeTerrainSize(heightmapData.terrain));
            material.SetTexture(_StampTex, targetDepthTexture);
            falloff.PrepareTerrain(material, heightmapData.terrain, transform, keywords);
            falloff.PrepareMaterial(material, transform, keywords);
            material.SetFloat("_BlurSize", blur);
        }

        public void Dispose()
        {
            
        }
    }
}