using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FIMSpace.Generating
{

    public static class FTerrainHelpers
    {



        #region Terrain Temp Datas


        #region Splat paint temp datas



        #region Main Struct

        struct FTerrainSplatsTempData
        {
            public Terrain terrain;
            public TerrainData terrainData;
            public int alphamapWidth;
            public int alphamapHeight;

            public float[,,] splatmapData;
            public int numTextures;
            public int tScale;
            public bool dirty;

            public FTerrainSplatsTempData(Terrain terrain)
            {
                this.terrain = terrain;
                terrainData = terrain.terrainData;
                alphamapWidth = terrainData.alphamapWidth;
                alphamapHeight = terrainData.alphamapHeight;

                splatmapData = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
                numTextures = splatmapData.Length / (alphamapWidth * alphamapHeight);
                tScale = terrain.terrainData.heightmapResolution;
                dirty = false;
            }

            public void ApplySplatmaps()
            {
                if (dirty == false) return;
                terrain.terrainData.SetAlphamaps(0, 0, splatmapData);
                dirty = false;
            }


            #region Utils
            public void SplatPaintAt(int tZ, int tX, int splat, float power)
            {
                if (tX < 0 || tX >= splatmapData.GetLength(0)) { return; }
                if (tZ < 0 || tZ >= splatmapData.GetLength(1)) { return; }
                splatmapData[tZ, tX, splat] = power;
            }

            public void SplatPaintAtAdditive(int tZ, int tX, int splat, float power)
            {
                if (tX < 0 || tX >= splatmapData.GetLength(0)) { return; }
                if (tZ < 0 || tZ >= splatmapData.GetLength(1)) { return; }
                splatmapData[tZ, tX, splat] += power;
            }

            public void EraseSplatsAt(int tZ, int tX, int dominantSplat)
            {
                if (tX < 0 || tX >= splatmapData.GetLength(0)) { return; }
                if (tZ < 0 || tZ >= splatmapData.GetLength(1)) { return; }

                for (int i = 0; i < numTextures; i++)
                {
                    if (i == dominantSplat) continue;
                    splatmapData[tZ, tX, i] = 0f;
                }
            }

            public void SubtractSplatsAt(int tZ, int tX, int dominantSplat, float toSubtract)
            {
                if (tX < 0 || tX >= splatmapData.GetLength(0)) { return; }
                if (tZ < 0 || tZ >= splatmapData.GetLength(1)) { return; }

                for (int i = 0; i < numTextures; i++)
                {
                    if (i == dominantSplat) continue;
                    splatmapData[tZ, tX, i] = Mathf.Max(0f, splatmapData[tZ, tX, i] - toSubtract);
                }
            }

            public void NormalizeSplatsAt(int tZ, int tX, int dominantSplat, float sutract)
            {
                SubtractSplatsAt(tZ, tX, dominantSplat, sutract);
                NormalizeSplatsAt(tZ, tX);
            }

            public void NormalizeSplatsAt(int tZ, int tX)
            {
                float alpha = 0f;
                for (int i = 0; i < numTextures; i++) alpha += splatmapData[tZ, tX, i];
                // Normalize splatmaps power
                if (alpha > 0f) for (int i = 0; i < numTextures; i++) splatmapData[tZ, tX, i] /= alpha;
            }

            #endregion

        }

        #endregion



        static Dictionary<Terrain, FTerrainSplatsTempData> TerrainSplatTempDatas = new Dictionary<Terrain, FTerrainSplatsTempData>();
        public static void ResetSplatTempDatas() { TerrainSplatTempDatas.Clear(); }
        public static void RefreshSplatTempDataFor(Terrain terr)
        {
            if (TerrainSplatTempDatas.ContainsKey(terr))
            {
                TerrainSplatTempDatas[terr] = new FTerrainSplatsTempData(terr);
                return;
            }
            else
            {
                TerrainSplatTempDatas.Add(terr, new FTerrainSplatsTempData(terr));
            }
        }

        static FTerrainSplatsTempData GetSplatTempDataFor(Terrain terr)
        {
            if (TerrainSplatTempDatas.ContainsKey(terr))
            {
                return TerrainSplatTempDatas[terr];
            }
            else
            {
                TerrainSplatTempDatas.Add(terr, new FTerrainSplatsTempData(terr));
                return TerrainSplatTempDatas[terr];
            }
        }




        #endregion


        #region Height paint temp datas


        #region Main Struct


        struct FTerrainHeightTempData
        {
            public Terrain terrain;
            public float[,] heights;
            public int tScale;
            public bool dirty;

            public FTerrainHeightTempData(Terrain terr)
            {
                this.terrain = terr;
                tScale = terr.terrainData.heightmapResolution;
                heights = terr.terrainData.GetHeights(0, 0, tScale, tScale);
                dirty = false;
            }

            public void ApplyHeights()
            {
                if (dirty == false) return;
                terrain.terrainData.SetHeights(0, 0, heights);
                dirty = false;
            }


            #region Utils

            static AnimationCurve _DefaultHeightFalloff = AnimationCurve.EaseInOut(0.05f, 1f, 1f, 0f);

            public void SetHeightAt(Vector3 wPos, float targetHeight, float power, float radius, AnimationCurve falloff = null, float extraRadiusOnBigDiff = 0f)
            {
                if (falloff == null) falloff = _DefaultHeightFalloff;

                wPos.y = targetHeight;
                Vector3 terrLocalPos = wPos - terrain.transform.position;

                Vector3 terrSplatPos;
                terrSplatPos.x = terrLocalPos.x / terrain.terrainData.size.x;
                terrSplatPos.y = terrLocalPos.y / terrain.terrainData.size.y;
                terrSplatPos.z = terrLocalPos.z / terrain.terrainData.size.z;

                Vector2Int splatPosition = new Vector2Int();
                splatPosition.x = (int)(terrSplatPos.x * tScale);
                splatPosition.y = (int)(terrSplatPos.z * tScale);

                targetHeight = terrSplatPos.y;
                int radiusInSamples = FTerrainHelpers.UnitsToSamples(radius, terrain, terrain.terrainData.heightmapResolution);

                // If difference between target height and current height of the terrain is big, we can apply further smoothing
                if (extraRadiusOnBigDiff > 0f)
                {
                    float diff = Mathf.Abs(heights[splatPosition.x, splatPosition.y] - targetHeight);
                    if (diff * terrain.terrainData.size.y > radius)
                    {
                        float factor = 0.3f + Mathf.InverseLerp(0f, extraRadiusOnBigDiff * 8f, diff * terrain.terrainData.size.y) * 1.7f;
                        radiusInSamples += FTerrainHelpers.UnitsToSamples(extraRadiusOnBigDiff * factor, terrain, terrain.terrainData.heightmapResolution);
                    }
                }

                for (int x = -radiusInSamples; x <= radiusInSamples; x++)
                    for (int z = -radiusInSamples; z <= radiusInSamples; z++)
                    {
                        int tZ = splatPosition.x + x;
                        int tX = splatPosition.y + z;
                        if (tX < 0 || tZ < 0 || tX >= heights.GetLength(0) || tZ >= heights.GetLength(1)) continue;

                        float fallf = falloff.Evaluate(Vector2.Distance(Vector2.zero, new Vector2(x, z)) / (float)radiusInSamples);
                        heights[tX, tZ] = Mathf.Lerp(heights[tX, tZ], targetHeight, fallf * power);
                    }

                dirty = true;
            }

            #endregion

        }


        #endregion




        static Dictionary<Terrain, FTerrainHeightTempData> TerrainHeightTempDatas = new Dictionary<Terrain, FTerrainHeightTempData>();
        public static void ResetHeightTempDatas() { TerrainSplatTempDatas.Clear(); }
        public static void RefreshHeightTempDataFor(Terrain terr)
        {
            if (TerrainHeightTempDatas.ContainsKey(terr))
            {
                TerrainHeightTempDatas[terr] = new FTerrainHeightTempData(terr);
                return;
            }
            else
            {
                TerrainHeightTempDatas.Add(terr, new FTerrainHeightTempData(terr));
            }
        }

        static FTerrainHeightTempData GetHeightTempDataFor(Terrain terr)
        {
            if (TerrainHeightTempDatas.ContainsKey(terr))
            {
                return TerrainHeightTempDatas[terr];
            }
            else
            {
                TerrainHeightTempDatas.Add(terr, new FTerrainHeightTempData(terr));
                return TerrainHeightTempDatas[terr];
            }
        }


        #endregion


        #region Trees temp datas



        #endregion


        #region Details temp datas


        #region Main Struct

        static string Hash_Details(Terrain terr, int layerID)
        {
            if (terr == null) return "";
            return terr.GetHashCode().ToString() + layerID.GetHashCode().ToString();
        }

        struct FTerrainDetailsTempData
        {
            public Terrain terrain;
            public int detailLayer;
            public int[,] details;
            public int tScale;
            public bool dirty;

            public FTerrainDetailsTempData(Terrain terr, int detailLayer)
            {
                this.terrain = terr;
                this.detailLayer = detailLayer;
                tScale = terr.terrainData.detailResolution;
                details = terr.terrainData.GetDetailLayer(0, 0, tScale, tScale, detailLayer);
                dirty = false;
            }

            public void ApplyDetails()
            {
                if (dirty == false) return;
                terrain.terrainData.SetDetailLayer(0, 0, detailLayer, details);
                dirty = false;
            }


            #region Utils

            public void SetDetailAt(Vector3 wPos, int newValue, float radius, bool square = false)
            {
                Vector3 terrLocalPos = WorldPosToTerrainNormalizedPos(wPos, terrain);

                int posXInTerrain = (int)(terrLocalPos.x * tScale);
                int posYInTerrain = (int)(terrLocalPos.z * tScale);

                int radiusInSamples = Mathf.CeilToInt((radius * tScale) / terrain.terrainData.size.x);

                for (int x = -radiusInSamples; x <= radiusInSamples; x++)
                    for (int z = -radiusInSamples; z <= radiusInSamples; z++)
                    {
                        int tZ = posXInTerrain + x;
                        int tX = posYInTerrain + z;
                        if (tX < 0 || tZ < 0 || tX >= details.GetLength(0) || tZ >= details.GetLength(1)) continue;

                        if (!square) if (Vector2.Distance(Vector2.zero, new Vector2(x, z)) > radiusInSamples) continue;
                        details[tX, tZ] = newValue;
                    }

                dirty = true;
            }

            #endregion

        }


        #endregion


        static Dictionary<string, FTerrainDetailsTempData> TerrainDetailsTempDatas = new Dictionary<string, FTerrainDetailsTempData>();
        public static void ResetDetailsTempDatas() { TerrainSplatTempDatas.Clear(); }
        public static void RefresDetailsTempDataFor(Terrain terr, int layer)
        {
            string key = Hash_Details(terr, layer);

            if (TerrainDetailsTempDatas.ContainsKey(key))
            {
                TerrainDetailsTempDatas[key] = new FTerrainDetailsTempData(terr, layer);
                return;
            }
            else
            {
                TerrainDetailsTempDatas.Add(key, new FTerrainDetailsTempData(terr, layer));
            }
        }

        static FTerrainDetailsTempData GetDetailsTempDataFor(string key, Terrain terr, int detailLayer)
        {
            if (TerrainDetailsTempDatas.ContainsKey(key))
            {
                return TerrainDetailsTempDatas[key];
            }
            else
            {
                TerrainDetailsTempDatas.Add(key, new FTerrainDetailsTempData(terr, detailLayer));
                return TerrainDetailsTempDatas[key];
            }
        }

        static FTerrainDetailsTempData GetDetailsTempDataFor(Terrain terr, int detailLayer)
        {
            string key = Hash_Details(terr, detailLayer);
            return GetDetailsTempDataFor(key, terr, detailLayer);
        }


        #endregion



        public static void ApplyAllTempDatas()
        {
            foreach (var data in TerrainSplatTempDatas) data.Value.ApplySplatmaps();
            foreach (var data in TerrainHeightTempDatas) data.Value.ApplyHeights();
            foreach (var data in TerrainDetailsTempDatas) data.Value.ApplyDetails();
        }

        public static void ResetAllTempDatas()
        {
            TerrainSplatTempDatas.Clear();
            TerrainHeightTempDatas.Clear();
            TerrainDetailsTempDatas.Clear();
        }

        #endregion




        public static int UnitsToSamples(float units, Terrain terr, float terrDataResolution)
        {
            return Mathf.CeilToInt((units * terrDataResolution) / terr.terrainData.size.x);
        }

        public static void PaintAt(Vector3 wPos, Terrain terr, int splatID, float unitsRadius, bool update = false)
        {
            var tempData = GetSplatTempDataFor(terr);

            Vector3 splatCoord = WorldPosToSplatMapCoordinate(wPos, terr, terr.terrainData.alphamapResolution);
            int tX = (int)splatCoord.x;
            int tZ = (int)splatCoord.z;

            tempData.SplatPaintAt(tZ, tX, splatID, 1f);
            tempData.EraseSplatsAt(tZ, tX, splatID);

            int radiusSamples = UnitsToSamples(unitsRadius, terr, terr.terrainData.alphamapResolution);

            for (int r = 1; r <= radiusSamples; r++)
            {
                float powr = Mathf.Lerp(1f, 0.2f, (float)r / (float)radiusSamples);
                tempData.SplatPaintAtAdditive(tZ + r, tX, splatID, powr);
                tempData.SplatPaintAtAdditive(tZ, tX + r, splatID, powr);
                tempData.SplatPaintAtAdditive(tZ - r, tX, splatID, powr);
                tempData.SplatPaintAtAdditive(tZ, tX - r, splatID, powr);

                tempData.SplatPaintAtAdditive(tZ + r, tX + r, splatID, powr);
                tempData.SplatPaintAtAdditive(tZ - r, tX - r, splatID, powr);
                tempData.SplatPaintAtAdditive(tZ - r, tX + r, splatID, powr);
                tempData.SplatPaintAtAdditive(tZ + r, tX - r, splatID, powr);
            }

            for (int r = 1; r <= radiusSamples; r++)
            {
                tempData.NormalizeSplatsAt(tZ + r, tX);
                tempData.NormalizeSplatsAt(tZ, tX + r);
                tempData.NormalizeSplatsAt(tZ - r, tX);
                tempData.NormalizeSplatsAt(tZ, tX - r);

                tempData.NormalizeSplatsAt(tZ + r, tX + r);
                tempData.NormalizeSplatsAt(tZ - r, tX - r);
                tempData.NormalizeSplatsAt(tZ - r, tX + r);
                tempData.NormalizeSplatsAt(tZ + r, tX - r);
            }

            tempData.dirty = true;
            TerrainSplatTempDatas[terr] = tempData;
        }



        public static Terrain GetTerrainIn(Vector3 wPos)
        {
            if (Terrain.activeTerrain == null) return null;
            if (Terrain.activeTerrains == null) return null;

            for (int t = 0; t < Terrain.activeTerrains.Length; t++)
            {
                var terr = Terrain.activeTerrains[t];
                if (terr)
                {
                    Vector3 locPos = terr.transform.InverseTransformPoint(wPos);
                    if (locPos.x < 0) continue;
                    if (locPos.x > terr.terrainData.bounds.max.x) continue;
                    if (locPos.z < 0) continue;
                    if (locPos.z > terr.terrainData.bounds.max.z) continue;
                    return terr;
                }
            }

            return null;
        }


        public static Vector3 WorldPosToTerrainNormalizedPos(Vector3 wPos, Terrain terr)
        {
            if (terr)
            {
                Vector3 onTerrain = ((wPos) - terr.gameObject.transform.position);
                Vector3 terrLocalPos;
                terrLocalPos.x = onTerrain.x / terr.terrainData.size.x;
                terrLocalPos.y = onTerrain.y / terr.terrainData.size.y;
                terrLocalPos.z = onTerrain.z / terr.terrainData.size.z;

                wPos = terrLocalPos;
            }

            return wPos;
        }

        public static Vector3 WorldPosToSplatMapCoordinate(Vector3 worldPosition, Terrain terr, float resolution)
        {
            Vector3 splatPosition = new Vector3();
            Vector3 terrLocalPos = worldPosition - terr.transform.position;

            Vector3 terrSplatPos;
            terrSplatPos.x = terrLocalPos.x / terr.terrainData.size.x;
            terrSplatPos.z = terrLocalPos.z / terr.terrainData.size.z;

            splatPosition.x = (int)(terrSplatPos.x * resolution);
            splatPosition.z = (int)(terrSplatPos.z * resolution);

            return splatPosition;
        }

        public static int GetTerrainLayerIdAtPosition(Vector3 position, Terrain terr)
        {
            if (terr == null) return -1;

            //Vector3 terrainCord = WorldPosToTerrainNormalizedPos(position, terr);
            int splatID = 0;
            float largestOpacity = 0f;

            var terrData = GetSplatTempDataFor(terr);
            Vector3 terrainCord = WorldPosToSplatMapCoordinate(position, terr, terr.terrainData.alphamapResolution);

            int tX = (int)terrainCord.x;
            int tZ = (int)terrainCord.z;

            if (tX < 0 || tX >= terrData.splatmapData.GetLength(0)) { /*UnityEngine.Debug.Log("toofarX " + terr.name);*/ return splatID; }
            if (tZ < 0 || tZ >= terrData.splatmapData.GetLength(1)) { /*UnityEngine.Debug.Log("toofarZ " + terr.name);*/ return splatID; }

            for (int i = 0; i < terrData.numTextures; i++)
            {
                if (terrData.splatmapData[tZ, tX, i] > largestOpacity)
                {
                    splatID = i;
                    largestOpacity = terrData.splatmapData[tZ, tX, i];
                }
            }

            return splatID;
        }




        public static bool[,] DetectTerrainAndCutHole(GameObject o, Terrain terr, float size, bool square)
        {
            bool[,] holes = null;

            if (terr)
            {
                Vector3 terrLocalPos = WorldPosToTerrainNormalizedPos(o.transform.position, terr);

                int tScale = terr.terrainData.holesResolution;
                int posXInTerrain = (int)(terrLocalPos.x * tScale);
                int posYInTerrain = (int)(terrLocalPos.z * tScale);

                holes = terr.terrainData.GetHoles(0, 0, tScale, tScale);
                bool[,] newHoles = terr.terrainData.GetHoles(0, 0, tScale, tScale);

                int radiusInSamples = Mathf.CeilToInt((size * tScale) / terr.terrainData.size.x);

                for (int x = -radiusInSamples; x <= radiusInSamples; x++)
                    for (int z = -radiusInSamples; z <= radiusInSamples; z++)
                    {
                        int tZ = posXInTerrain + x;
                        int tX = posYInTerrain + z;
                        if (tX < 0 || tZ < 0 || tX >= newHoles.GetLength(0) || tZ >= newHoles.GetLength(1)) continue;

                        if (!square) if (Vector2.Distance(Vector2.zero, new Vector2(x, z)) > radiusInSamples) continue;
                        newHoles[tX, tZ] = false;
                    }

                terr.terrainData.SetHoles(0, 0, newHoles);
            }

            return holes;
        }


        public static void DetectTerrainAndRemoveTrees(GameObject o, float size, bool square)
        {
            if (o == null) return;

            var terr = GetTerrainIn(o.transform.position);

            if (terr)
            {
                Vector3 terrLocalPos = WorldPosToTerrainNormalizedPos(o.transform.position, terr);
                Vector2 normPos = new Vector2(terrLocalPos.x, terrLocalPos.z);
                float radiusInNormalizedcale = size / terr.terrainData.size.x;

                List<TreeInstance> treeInstances = terr.terrainData.treeInstances.ToList();

                if (!square)
                {
                    for (int i = treeInstances.Count - 1; i >= 0; i--)
                    {
                        var tree = terr.terrainData.GetTreeInstance(i);
                        if (Vector2.Distance(new Vector2(tree.position.x, tree.position.z), normPos) <= radiusInNormalizedcale)
                            treeInstances.RemoveAt(i);
                    }
                }
                else
                {
                    for (int i = treeInstances.Count - 1; i >= 0; i--)
                    {
                        var tree = terr.terrainData.GetTreeInstance(i);
                        if (DistanceManhattan2D(tree.position, terrLocalPos) <= radiusInNormalizedcale)
                            treeInstances.RemoveAt(i);
                    }
                }

                if (treeInstances.Count != terr.terrainData.treeInstances.Length)
                {
                    terr.terrainData.treeInstances = treeInstances.ToArray();
                }
            }
        }


        public static Vector3 GetTerrainNormalInWorldPos(Terrain terr, Vector3 wPos)
        {
            Vector3 normPos = FTerrainHelpers.WorldPosToTerrainNormalizedPos(wPos, terr);
            return terr.terrainData.GetInterpolatedNormal(normPos.x, normPos.z);
        }

        public static Vector3 GetTerrainNormalInWorldPos(Vector3 wPos)
        {
            Terrain terr = GetTerrainIn(wPos);
            if (terr == null) return Vector3.up;
            return GetTerrainNormalInWorldPos(terr, wPos);
        }

        public static float DistanceManhattan2D(Vector3 a, Vector3 b)
        {
            float diff = 0f;
            diff += Mathf.Abs(a.x - b.x);
            diff += Mathf.Abs(a.z - b.z);
            return diff;
        }

        public static void AdjustTerrainHeightAt(Vector3 wPos, float power, float radius, float extraRadiusOnBigDiff = 0f, bool update = false)
        {
            var terrain = GetTerrainIn(wPos);
            if (terrain == null) return;
            var tempData = GetHeightTempDataFor(terrain);
            tempData.SetHeightAt(wPos, wPos.y, power, radius, null, extraRadiusOnBigDiff);
            if (update) tempData.ApplyHeights();
            TerrainHeightTempDatas[terrain] = tempData;
        }

        public static void ChangeDetailAt(Vector3 wPos, int detailLayer, int changeDetailValueTo, float radius)
        {
            Terrain terr = GetTerrainIn(wPos);
            if (terr == null) return;
            ChangeDetailAt(wPos, terr, detailLayer, changeDetailValueTo, radius);
        }

        public static void ChangeDetailAt(Vector3 wPos, Terrain terr, int detailLayer, int changeDetailValueTo, float radius, bool update = false)
        {
            string key = Hash_Details(terr, detailLayer);
            var data = GetDetailsTempDataFor(key, terr, detailLayer);
            data.SetDetailAt(wPos, changeDetailValueTo, radius);
            TerrainDetailsTempDatas[key] = data;
            if (update) data.ApplyDetails(); 
        }

    }
}
