using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FIMSpace.Generating
{

    public class MeshPaintHelper
    {
        public Transform parent;
        public Mesh mesh;
        public List<Color> colors;
        public List<Vector3> verts;
        public Vector3[] worldPosVerts;

        public Dictionary<Vector3Int, VertexCluster> clusterMap;
        public bool ClustersWasGenerated { get; private set; }
        float padding = 1f;

        #region Caching meshes

        static Dictionary<Mesh, MeshPaintHelper> cache = null;
        static double cacheTime = -1;


        public static void PrepareCache()
        {
            double currentTime = Time.unscaledTime;
#if UNITY_EDITOR
            if (Application.isPlaying == false) currentTime = EditorApplication.timeSinceStartup;
#endif

            if (cacheTime == currentTime) return;
            cacheTime = currentTime;
            cache = new Dictionary<Mesh, MeshPaintHelper>();
        }

        /// <summary> Using caching feature </summary>
        public static MeshPaintHelper GetMeshHelper(Transform parent, Mesh m)
        {
            if (cache != null) if (cache.ContainsKey(m)) return cache[m];

            MeshPaintHelper mesh = new MeshPaintHelper(parent, m);
            if (cache != null) cache.Add(m, mesh);

            return mesh;
        }

        #endregion

        public MeshPaintHelper(Transform parent, Mesh mesh)
        {
            ClustersWasGenerated = false;

            this.parent = parent;
            this.mesh = mesh;

            colors = new List<Color>();
            mesh.GetColors(colors);

            verts = new List<Vector3>();
            mesh.GetVertices(verts);

            worldPosVerts = new Vector3[(int)verts.Count];

            if (parent != null)
            {
                for (int v = 0; v < verts.Count; v++) worldPosVerts[v] = parent.TransformPoint(verts[v]);
            }
            else
            {
                for (int v = 0; v < verts.Count; v++) worldPosVerts[v] = (verts[v]);
            }
        }


        public bool IsValid
        {
            get
            {
                if (mesh == null) return false;
                if (colors == null) return false;
                if (verts == null) return false;
                if (verts.Count == 0) return false;
                if (verts.Count != colors.Count) return false;
                return true;
            }
        }


        //public struct VertexHelper
        //{
        //    public Vector3 worldPos;
        //    public Vector3 vertPos;
        //    public Vector3 freeDir;
        //    public float collisionDistance;

        //    public int nearest0;
        //    public int nearest1;
        //    public int nearest2;
        //    public int nearest3;
        //    public int nearest4;

        //    public VertexHelper(Vector3 vPos, Vector3 wPos)
        //    {
        //        vertPos = vPos;
        //        worldPos = wPos;

        //        freeDir = Vector3.zero;
        //        collisionDistance = 0f;
        //        nearest0 = -1;
        //        nearest1 = -1;
        //        nearest2 = -1;
        //        nearest3 = -1;
        //        nearest4 = -1;
        //    }
        //}

        public class VertexCluster
        {
            public Vector3Int clusterID;
            bool boundsSet;
            public Bounds clusterBounds;
            public Bounds neightbourCoverBounds;
            public List<int> neightCoverVertsIn;
            public List<int> vertsIn;

            public VertexCluster(Vector3Int id)
            {
                clusterID = id; boundsSet = false;
                clusterBounds = new Bounds(Vector3.zero, Vector3.zero);
                vertsIn = new List<int>();
                neightCoverVertsIn = new List<int>();
                boundsSet = false;
            }

            public void AddVertex(int i, Vector3 wPos)
            {
                vertsIn.Add(i);
                if (!boundsSet) { clusterBounds = new Bounds(wPos, Vector3.zero); boundsSet = true; }
                clusterBounds.Encapsulate(wPos);
            }
        }

        VertexCluster GetCluster(Vector3Int key)
        {
            VertexCluster get;
            if (clusterMap.TryGetValue(key, out get)) return get;
            get = new VertexCluster(key);
            clusterMap.Add(key, get);
            return get;
        }

        // Precomputing vertex structure to help out edges painting for each vertex
        public void GeneratePaintingGrid(float clusterSize = 0f)
        {
            Vector3 lossyScale = Vector3.one;
            if (parent != null) lossyScale = parent.lossyScale;

            if (clusterSize < 0.01f)
            {
                Vector3 wBounds = Vector3.Scale(lossyScale, mesh.bounds.size);
                padding = ((wBounds.x + wBounds.y + wBounds.z) / 3f) * 0.75f;
            }
            else
            {
                padding = clusterSize;
            }

            clusterMap = new Dictionary<Vector3Int, VertexCluster>();

            for (int v = 0; v < verts.Count; v++)
            {
                Vector3 wPos = worldPosVerts[v];
                Vector3Int key = V3toV3Int(RoundValueTo(wPos, padding));

                VertexCluster cluster = GetCluster(key);
                cluster.AddVertex(v, wPos);
            }

            float sqrtPadding = padding * padding * 0.5f;

            foreach (KeyValuePair<Vector3Int, VertexCluster> item in clusterMap)
            {
                item.Value.neightbourCoverBounds = item.Value.clusterBounds;

                VertexCluster c;
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(1, 0, 0), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(-1, 0, 0), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(0, 0, 1), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(0, 0, -1), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(0, 1, 0), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(0, -1, 0), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);

                if (clusterMap.TryGetValue(item.Key + new Vector3Int(1, 0, 1), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(-1, 0, 1), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(1, 0, -1), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                if (clusterMap.TryGetValue(item.Key + new Vector3Int(-1, 0, -1), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);

                //if (clusterMap.TryGetValue(item.Key + new Vector3Int(2, 0, 0), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                //if (clusterMap.TryGetValue(item.Key + new Vector3Int(-2, 0, 0), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                //if (clusterMap.TryGetValue(item.Key + new Vector3Int(0, 0, 2), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);
                //if (clusterMap.TryGetValue(item.Key + new Vector3Int(0, 0, -2), out c)) EncapsulateNeightbours(item.Value, c, sqrtPadding);

                Bounds fixBounds = item.Value.neightbourCoverBounds;
                if (fixBounds.size.x == 0f) fixBounds.size = new Vector3(padding * 0.05f, fixBounds.size.y, fixBounds.size.z);
                if (fixBounds.size.y == 0f) fixBounds.size = new Vector3(fixBounds.size.x, padding * 0.05f, fixBounds.size.z);
                if (fixBounds.size.z == 0f) fixBounds.size = new Vector3(fixBounds.size.x, fixBounds.size.y, padding * 0.05f);

                item.Value.neightbourCoverBounds = fixBounds;
            }



            // Debug Draw
            //float h = 0f;
            //foreach (var item in clusterMap)
            //{
            //    h += 0.135f;
            //    if (h > 1f) h -= 1f;
            //    Color debCol = Color.HSVToRGB(h, 0.6f, 0.6f);

            //    FDebug.DrawBounds3D(item.Value.neightbourCoverBounds, Color.red * 1.1f, 1f);
            //    //for (int i = 0; i < item.Value.vertsIn.Count - 1; i++) UnityEngine.Debug.DrawLine(worldPosVerts[item.Value.vertsIn[i]], worldPosVerts[item.Value.vertsIn[i + 1]], debCol, 1.01f);
            //}

            ClustersWasGenerated = true;
        }

        void EncapsulateNeightbours(VertexCluster cluster, VertexCluster other, float sqrtPadding)
        {
            for (int i = 0; i < other.vertsIn.Count; i++)
            {
                Vector3 pos = worldPosVerts[other.vertsIn[i]];
                float dist = cluster.clusterBounds.SqrDistance(pos);
                if (dist <= sqrtPadding)
                {
                    cluster.neightbourCoverBounds.Encapsulate(pos);
                    cluster.neightCoverVertsIn.Add(other.vertsIn[i]);
                }
            }

        }


        public float GetEdgeFactor(int i, Vector3 vertWPos, bool upper, float falloff)
        {
            Vector3Int key = V3toV3Int(RoundValueTo(vertWPos, padding));

            VertexCluster helperCluster;
            clusterMap.TryGetValue(key + (upper ? new Vector3Int(0, 1, 0) : new Vector3Int(0, -1, 0)), out helperCluster);

            var cluster = GetCluster(key);
            //if ( helperCluster == null) if (vertWPos.y > cluster.clusterBounds.max.y - cluster.clusterBounds.extents.y * 0.01f) return 1f;

            int highest = i;

            for (int c = 0; c < cluster.vertsIn.Count; c++)
            {
                int othId = cluster.vertsIn[c];
                Vector3 othWpos = worldPosVerts[othId];

                float dist = Vector2.Distance(new Vector2(othWpos.x, othWpos.z), new Vector2(vertWPos.x, vertWPos.z));
                if (dist > padding * 0.2f) continue;

                if (upper)
                {
                    if (othWpos.y > worldPosVerts[highest].y) highest = othId;
                }
                else
                    if (othWpos.y < worldPosVerts[highest].y) highest = othId;
            }


            if (helperCluster != null)
            {
                for (int c = 0; c < helperCluster.vertsIn.Count; c++)
                {
                    int othId = helperCluster.vertsIn[c];
                    Vector3 othWpos = worldPosVerts[othId];

                    float dist = Vector2.Distance(new Vector2(othWpos.x, othWpos.z), new Vector2(vertWPos.x, vertWPos.z));
                    if (dist > padding * 0.2f) continue;

                    if (upper)
                    {
                        if (othWpos.y > worldPosVerts[highest].y) highest = othId;
                    }
                    else
                        if (othWpos.y < worldPosVerts[highest].y) highest = othId;
                }
            }


            // Debug backup
            //if (highest != i) UnityEngine.Debug.DrawLine(vertWPos, worldPosVerts[highest], Color.green, 1.01f);

            float edgeDistance = 0f;
            if (highest != i) edgeDistance = Mathf.Abs(vertWPos.y - worldPosVerts[highest].y);
            //if (highest != i) edgeDistance = Vector3.Distance(vertWPos, worldPosVerts[highest]);

            if (falloff < 0.0001f)
            {
                if (edgeDistance < padding * 0.001f) return 1f;
            }

            return Mathf.InverseLerp(falloff, 0f, edgeDistance); // Far from edge = 0 blend, near edge = 1
        }

        Vector3Int WPosToKey(Vector3 wpos)
        {
            return V3toV3Int(RoundValueTo(wpos, padding));
        }

        public bool IsSideVertex(Vector3 vertWPos, float checkOffset)
        {
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(checkOffset, 0, 0))) return true;
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(-checkOffset, 0, 0))) return true;
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(0, 0, checkOffset))) return true;
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(0, 0, -checkOffset))) return true;

            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(checkOffset, 0, checkOffset))) return true;
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(-checkOffset, 0, checkOffset))) return true;
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(-checkOffset, 0, -checkOffset))) return true;
            if (!WorldPosContainedNotPrecise(vertWPos + new Vector3(checkOffset, 0, -checkOffset))) return true;

            return false;
        }

        bool WorldPosContainedNotPrecise(Vector3 wPos)
        {
            VertexCluster outClusterCheck;

            if (!clusterMap.TryGetValue(WPosToKey(wPos), out outClusterCheck)) return false;
            else
            if (outClusterCheck.neightbourCoverBounds.Contains(wPos))
            {
                //float range = padding * padding * 1f;
                //for (int v = 0; v < outClusterCheck.neightCoverVertsIn.Count; v++)
                //{
                //    float dist = Vector3.SqrMagnitude(wPos - worldPosVerts[outClusterCheck.neightCoverVertsIn[v]]);
                //    if ( dist < range) return true;
                //}

                return true;
            }

            return false;
        }

        private Vector3 RoundValueTo(Vector3 toRound, float to)
        {
            return new Vector3(RoundValueTo(toRound.x, to), RoundValueTo(toRound.y, to), RoundValueTo(toRound.z, to));
        }

        private float RoundValueTo(float toRound, float to)
        {
            return Mathf.Round(toRound / to);
        }

        private Vector3Int V3toV3Int(Vector3 v)
        {
            return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }


    }


}
