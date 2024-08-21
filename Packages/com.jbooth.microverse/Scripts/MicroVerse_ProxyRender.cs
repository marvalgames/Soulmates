using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Linq;

namespace JBooth.MicroVerseCore
{
    public partial class MicroVerse : MonoBehaviour
    {
#if UNITY_EDITOR && __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__

        public enum ProxyRenderMode
        {
            AlwaysUnity,
            ProxyWhileUpdating,
            AlwaysProxy
        };

        [HideInInspector] public ProxyRenderMode _proxyRenderMode = ProxyRenderMode.AlwaysUnity;
        [Tooltip("This requires setup on the MicroSplat side, see documentation for more details")]
        public ProxyRenderMode proxyRenderMode
        {
            get { return _proxyRenderMode; }
            set
            {
                _proxyRenderMode = value;
                if (value == ProxyRenderMode.AlwaysProxy)
                {
                    IsUsingProxyRenderer = true;
                }
                else
                {
                    IsUsingProxyRenderer = false;
                }
            }
        }

        bool _isUsingProxyRenderer = false;
        public bool IsUsingProxyRenderer
        {
            get
            {
                return _isUsingProxyRenderer;
            }
            set
            {
                if (value)
                {
                    EnableProxyRenderer();
                }
                else if (_proxyRenderMode == ProxyRenderMode.ProxyWhileUpdating)
                {
                    
                }
                else
                {
                    DisableProxyRenderer();
                }

            }
        }

        

        class ProxyData
        {
            public GameObject root; 
            public Material instance;
            public MeshRenderer renderer;
            public MeshFilter filter;
            public RenderTexture heightMap;
            public RenderTexture normalMap;
            public RenderTexture[] controlMaps = new RenderTexture[8];
            public RenderTexture holeMap;

            public void Cleanup()
            {
                if (instance != null) GameObject.DestroyImmediate(instance);
                if (renderer != null)
                {
                    if (renderer.sharedMaterial != null)
                        GameObject.DestroyImmediate(renderer.sharedMaterial);
                    GameObject.DestroyImmediate(renderer);
                }
                if (filter != null) GameObject.DestroyImmediate(filter);
                if (root != null) GameObject.DestroyImmediate(root);
                if (heightMap != null) RenderTexture.ReleaseTemporary(heightMap);
                if (normalMap != null) RenderTexture.ReleaseTemporary(normalMap);
                if (holeMap != null) RenderTexture.ReleaseTemporary(holeMap);

                for (int i = 0; i < controlMaps.Length; ++i)
                {
                    if (controlMaps[i] != null) RenderTexture.ReleaseTemporary(controlMaps[i]);
                    controlMaps[i] = null;
                }
                heightMap = null;
                normalMap = null;
                instance = null;
                holeMap = null;
                root = null;
                filter = null;
                renderer = null;
            }

            public ProxyData(Terrain t, Material template, Mesh mesh)
            {
                root = new GameObject();
                root.transform.parent = t.transform;
                root.transform.localPosition = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                instance = new Material(template);
                renderer = root.gameObject.AddComponent<MeshRenderer>();
                filter = root.gameObject.AddComponent<MeshFilter>();
                instance.hideFlags = HideFlags.HideAndDontSave;
                renderer.hideFlags = HideFlags.HideAndDontSave;
                filter.hideFlags = HideFlags.HideAndDontSave;
                root.hideFlags = HideFlags.HideAndDontSave;
                renderer.sharedMaterial = instance;
                filter.sharedMesh = mesh;
            }
        }

        Mesh proxyMesh;
        Vector3 meshTSize;

        Dictionary<Terrain, ProxyData> proxyData = new Dictionary<Terrain, ProxyData>();

        ProxyData AddProxyData(Terrain t)
        {
            if (proxyMesh == null || t.terrainData.size != meshTSize)
            {
                meshTSize = t.terrainData.size;
                proxyMesh = TerrainUtil.GenerateMesh(32, t.terrainData.size);
            }
            var mst = t.GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
            if (mst == null)
            {
                Debug.LogError("Cannot find MicroSplat on terrain");
                return null;
            }
            var pd = new ProxyData(t, mst.matInstance, proxyMesh);
            string path = UnityEditor.AssetDatabase.GetAssetPath(mst.matInstance.shader);
            path = path.Replace(".shader", "_MVPreview.shader");
            pd.instance.shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);
            proxyData.Add(t, pd);
            return pd;
        }

        ProxyData FindOrCreateProxyData(Terrain t)
        {
            ProxyData pd = null;
            if (proxyData.ContainsKey(t))
            {
                pd = proxyData[t];
            }
            else
            {
                pd = AddProxyData(t);
            }
            return pd;
        }

        void UpdateProxyHeightmap(Terrain t, RenderTexture heightMap)
        {
            var pd = FindOrCreateProxyData(t);
            if (pd == null)
                return;
            if (pd.heightMap != null) RenderTexture.ReleaseTemporary(pd.heightMap);
            pd.heightMap = heightMap;
            pd.instance.SetTexture("_TerrainHeightmapTexture", heightMap);
            pd.instance.SetFloat("_TerrainHeight", t.terrainData.size.y);
            //half4 _TessData1; // tess, displacement, mipBias, edge length
            //half4 _TessData2; // distance min, max, shaping, upbias
            pd.instance.SetVector("_TessData1", new Vector4(12, t.terrainData.size.y, 0, 20));
            pd.instance.SetVector("_TessData2", new Vector4(800, 3000, 0, 1));
        }

        void UpdateProxyNormalmap(Terrain t, RenderTexture normalMap)
        {
            var pd = FindOrCreateProxyData(t);
            if (pd == null)
                return;
            if (pd.normalMap != null) RenderTexture.ReleaseTemporary(pd.normalMap);
            pd.normalMap = normalMap;
            pd.instance.SetTexture("_TerrainNormalmapTexture", normalMap);
        }

        void UpdateControlmap(Terrain t, int index, RenderTexture control)
        {
            var pd = FindOrCreateProxyData(t);
            if (pd == null) return;
            if (pd.controlMaps[index] != null) RenderTexture.ReleaseTemporary(pd.controlMaps[index]);
            pd.controlMaps[index] = control;
            pd.instance.SetTexture("_Control" + index, control);
        }

        void UpdateHolemap(Terrain t, RenderTexture holeMap)
        {
            var pd = FindOrCreateProxyData(t);
            if (pd == null)
                return;
            if (pd.holeMap != null) RenderTexture.ReleaseTemporary(pd.holeMap);
            pd.normalMap = holeMap;
            pd.instance.SetTexture("_TerrainHolesTexture", holeMap);
        }

        void EnableProxyRenderer()
        {
            if (MicroVerse.instance != null)
            {
                var allterrains = GetAllTerrains();
                foreach (var t in allterrains)
                {
                    if (t.drawHeightmap != false) t.drawHeightmap = false;
                }
            }
            _isUsingProxyRenderer = true;
        }

        void DisableProxyRenderer()
        {
            foreach (var v in proxyData.Values)
            {
                v.Cleanup();
            }
            proxyData.Clear();
            SyncTerrainList();
            if (MicroVerse.instance != null)
            {
                var allterrains = GetAllTerrains();
                foreach (var t in allterrains)
                {
                    if (t.drawHeightmap != true)
                    {
                        t.drawHeightmap = true;
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(t);
#endif
                    }
                }
            }
            _isUsingProxyRenderer = false;
        }

#endif
    }
}
