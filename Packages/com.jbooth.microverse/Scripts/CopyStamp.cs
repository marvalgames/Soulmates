using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    public class CopyStamp : ScriptableObject
    {
        [System.Serializable]
        public class TreeCopyData
        {
            public Texture2D positonsTex { get; set; }
            public Texture2D randomsTex { get; set; }
            public TreePrototypeSerializable[] prototypes;

            // serialized data
            public byte[] randomsData;
            public byte[] positionsData;
            [HideInInspector] public Vector2Int dataSize;

            public void Unpack()
            {
                if (positonsTex == null && positionsData != null && positionsData.Length != 0)
                {
                    positonsTex = new Texture2D(dataSize.x, dataSize.y, TextureFormat.RGBAHalf, false, true);
                    positonsTex.wrapMode = TextureWrapMode.Clamp;
                    positonsTex.LoadRawTextureData(positionsData);
                    positonsTex.Apply(false, true);
                    positonsTex.name = "CopyStampTreePositionsMap";
                    positonsTex.hideFlags = HideFlags.DontSave;
                }
                if (randomsTex == null && randomsData != null && randomsData.Length != 0)
                {
                    randomsTex = new Texture2D(dataSize.x, dataSize.y, TextureFormat.RGBAHalf, false, true);
                    randomsTex.wrapMode = TextureWrapMode.Clamp;
                    randomsTex.LoadRawTextureData(randomsData);
                    randomsTex.Apply(false, true);
                    randomsTex.name = "CopyStampRandomsMap";
                    randomsTex.hideFlags = HideFlags.DontSave;
                }
            }
        }

        [System.Serializable]
        public class DetailCopyData
        {
            [System.Serializable]
            public class Layer
            {
                public Texture2D texture { get; set; }
                public byte[] bytes;
                public DetailPrototypeSerializable prototype;
                public Vector2Int dataSize;
            }
            public List<Layer> layers = new List<Layer>();

            public Layer FindOrCreateLayer(DetailPrototypeSerializable prototype)
            {
                foreach (var l in layers)
                {
                    if (l.prototype.Equals(prototype))
                        return l;
                }
                var nl = new Layer();
                nl.prototype = prototype;
                layers.Add(nl);
                return nl;
            }

            public void Unpack()
            {
                foreach (var l in layers)
                {
                    if (l.texture == null && l.bytes != null && l.bytes.Length > 0)
                    {
                        l.texture = new Texture2D(l.dataSize.x, l.dataSize.y, TextureFormat.R8, false, true);
                        l.texture.wrapMode = TextureWrapMode.Clamp;
                        l.texture.LoadRawTextureData(l.bytes);
                        l.texture.Apply(false, true);
                        l.texture.name = "CopyStampDetailMap";
                        l.texture.hideFlags = HideFlags.DontSave;
                    }
                }

            }
        }

        public Texture2D heightMap { get; set; }
        public Texture2D indexMap { get; set; }
        public Texture2D weightMap { get; set; }
        public Texture2D holeMap { get; set; }
        public TerrainLayer[] layers;
        public Vector2 heightRenorm;
        public TreeCopyData treeData;
        public DetailCopyData detailData;


        [HideInInspector] public byte[] heightData;
        [HideInInspector] public byte[] indexData;
        [HideInInspector] public byte[] weightData;
        [HideInInspector] public byte[] holeData;
        [HideInInspector] public Vector2Int heightSize;
        [HideInInspector] public Vector2Int indexWeightSize;
        [HideInInspector] public Vector2Int holeSize;

        public static CopyStamp Create(
                Texture2D height,
                Texture2D index,
                Texture2D weight,
                Texture2D hole,
                TerrainLayer[] tLayers,
                Vector2 heightRenorm,
                TreeCopyData treeData,
                DetailCopyData detailData)
        {
            CopyStamp cs = CopyStamp.CreateInstance<CopyStamp>();
            cs.layers = tLayers;
            cs.heightRenorm = heightRenorm;
            cs.heightData = height != null ? height.GetRawTextureData() : null;
            cs.indexData = index != null ? index.GetRawTextureData() : null;
            cs.weightData = weight != null ? weight.GetRawTextureData() : null;
            cs.holeData = hole != null ? hole.GetRawTextureData() : null;
            if (height != null)
                cs.heightSize = new Vector2Int(height.width, height.height);
            if (index != null && weight != null)
                cs.indexWeightSize = new Vector2Int(index.width, index.height);
            if (hole != null)
                cs.holeSize = new Vector2Int(hole.width, hole.height);
            cs.treeData = treeData;
            cs.detailData = detailData;
            return cs;

        }


        public void Unpack()
        {
            if (heightMap == null && heightData != null && heightData.Length != 0)
            {
                heightMap = new Texture2D(heightSize.x, heightSize.y, TextureFormat.R16, false, true);
                heightMap.wrapMode = TextureWrapMode.Clamp;
                heightMap.LoadRawTextureData(heightData);
                heightMap.Apply(false, true);
                heightMap.name = "CopyStampHeightMap";
                heightMap.hideFlags = HideFlags.DontSave;
            }
            if (indexMap == null && indexData != null && indexData.Length != 0)
            {
                indexMap = new Texture2D(indexWeightSize.x, indexWeightSize.y, TextureFormat.ARGB32, false, true);
                indexMap.LoadRawTextureData(indexData);
                indexMap.wrapMode = TextureWrapMode.Clamp;
                indexMap.filterMode = FilterMode.Point;
                indexMap.Apply(false, true);
                indexMap.name = "CopyStampIndexMap";
                indexMap.hideFlags = HideFlags.DontSave;
            }
            if (weightMap == null && weightData != null && weightData.Length != 0)
            {
                weightMap = new Texture2D(indexWeightSize.x, indexWeightSize.y, TextureFormat.ARGB32, false, true);
                weightMap.LoadRawTextureData(weightData);
                weightMap.wrapMode = TextureWrapMode.Clamp;
                weightMap.Apply(false, true);
                weightMap.name = "CopyStampWeightMap";
                weightMap.hideFlags = HideFlags.DontSave;
            }
            if (holeMap == null && holeData != null && holeData.Length != 0)
            {
                holeMap = new Texture2D(holeSize.x, holeSize.y, TextureFormat.R8, false, true);
                holeMap.LoadRawTextureData(holeData);
                holeMap.wrapMode = TextureWrapMode.Clamp;
                holeMap.Apply(false, true);
                holeMap.name = "CopyStampWeightMap";
                holeMap.hideFlags = HideFlags.DontSave;
            }
            if (treeData != null)
                treeData.Unpack();
            if (detailData != null)
                detailData.Unpack();
        }
    }
}