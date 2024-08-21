using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace JBooth.MicroVerseCore
{
    [ExecuteAlways]
    public class Stamp : MonoBehaviour
    {
        public class KeywordBuilder
        {
            public List<string> keywords = new List<string>(32);
            public List<string> initialKeywords = new List<string>(16);

            public void Add(string k)
            {
                keywords.Add(k);
            }

            public void Clear()
            {
                keywords.Clear();
            }

            public void ClearInitial()
            {
                initialKeywords.Clear();
            }

            static List<string> kws = new List<string>(64);
            public void Assign(Material mat)
            {
                kws.Clear();
                kws.AddRange(initialKeywords);
                kws.AddRange(keywords);
                mat.shaderKeywords = kws.ToArray();
            }

            public void Remove(string k)
            {
                keywords.Remove(k);
            }
        }

        protected KeywordBuilder keywordBuilder = new KeywordBuilder();

        public virtual void StripInBuild()
        {
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }
        public bool IsEnabled() { return gameObject.activeInHierarchy && enabled; }

        public virtual Bounds GetBounds() { return new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)); }


        protected void ClearCachedBounds()
        {
#if UNITY_EDITOR
            cachedBounds = default;
#endif
        }

        public virtual void OnEnable()
        {
#if UNITY_EDITOR
            cachedMtx = transform.localToWorldMatrix;
            cachedBounds = GetBounds();
#endif
            transform.hasChanged = false;
            MicroVerse.instance?.Invalidate(GetBounds());
        }

#if UNITY_EDITOR
        
        public bool gizmoVisible { get; set; }
        string typeName;
        static Dictionary<string, string> typeToIconPath = new Dictionary<string, string>();
        public void OnDrawGizmos()
        {
            if (gizmoVisible && MicroVerse.instance != null)
            {
                Vector3 worldPos = this.transform.position;
                worldPos.y += transform.lossyScale.y;
                string path;
                if (typeName == null)
                {
                    typeName = GetType().Name;
                }
                if (!typeToIconPath.TryGetValue(typeName, out path))
                {
                    path = "Packages/com.jbooth.microverse/Scripts/Gizmos/microverse_icon_" + typeName + ".png";
                    typeToIconPath.Add(typeName, path);
                }
                Gizmos.DrawIcon(worldPos, path, false);
            }
        }
#endif

        public virtual void OnDisable()
        {
            MicroVerse.instance?.Invalidate(GetBounds());
        }

        public virtual FilterSet GetFilterSet()
        {
            return null;
        }

        protected virtual void OnDestroy()
        {

        }

        public int stampVersion = 0;
        public static float terrainReferenceSize = 1000;
        // get scaling factor to make stamp data not related to terrain size
        protected float GetTerrainScalingFactor(Terrain t)
        {
            if (t != null && t.terrainData != null)
            {
                return t.terrainData.size.x / terrainReferenceSize;
            }
            
            return 1;
        }


#if UNITY_EDITOR

        public virtual void OnMoved()
        {
            var bounds = GetBounds();
            cachedBounds.Encapsulate(bounds);
            MicroVerse.instance?.Invalidate(cachedBounds);
            cachedBounds = bounds;
        }


        
        Matrix4x4 cachedMtx;
        Bounds cachedBounds;
        void Update()
        {
            if (cachedMtx != transform.localToWorldMatrix)
            {
                cachedMtx = transform.localToWorldMatrix;
                OnMoved();
            }
            
        }
#endif
    }
}
