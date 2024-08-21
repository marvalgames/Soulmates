using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Unity.Mathematics;
using Unity.Collections;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(CopyPasteStamp), true)]
    [CanEditMultipleObjects]
    class CopyPasteStampEditor : Editor
    {
        static Vector3 WorldToTerrain(Terrain terrain, Vector3 worldPos)
        {
            Vector3 ret = new Vector3();
            Vector3 terPosition = terrain.transform.position;
            ret.x = ((worldPos.x - terPosition.x) / terrain.terrainData.size.x);
            ret.z = ((worldPos.z - terPosition.z) / terrain.terrainData.size.z);
            return ret;
        }

        static void GenerateMega(Terrain terrain, out RenderTexture indexes, out RenderTexture weights)
        {
            Material mat = new Material(Shader.Find("Hidden/MicroVerse/SplatToMega"));

            indexes = RenderTexture.GetTemporary(terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            weights = RenderTexture.GetTemporary(terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            indexes.name = "GenerateMega::indexes";
            weights.name = "GenerateMega::weights";
            indexes.filterMode = FilterMode.Point;
            indexes.wrapMode = TextureWrapMode.Clamp;
            weights.wrapMode = TextureWrapMode.Clamp;
            // splats
            Texture2D[] splatMaps = terrain.terrainData.alphamapTextures;
            int count = terrain.terrainData.alphamapTextureCount;
            for (int i = 0; i < terrain.terrainData.alphamapTextureCount; ++i)
            {
                mat.SetTexture("_Control" + i, splatMaps[i]);
            }

            if (count > 7) { mat.EnableKeyword("_MAX32TEXTURES"); }
            else if (count > 6) { mat.EnableKeyword("_MAX28TEXTURES"); }
            else if (count > 5) { mat.EnableKeyword("_MAX24TEXTURES"); }
            else if (count > 4) { mat.EnableKeyword("_MAX20TEXTURES"); }
            else if (count > 3) { mat.EnableKeyword("_MAX16TEXTURES"); }
            else if (count > 2) { mat.EnableKeyword("_MAX12TEXTURES"); }
            else if (count > 1) { mat.EnableKeyword("_MAX8TEXTURES"); }
            else { mat.EnableKeyword("_MAX4TEXTURES"); }

            RenderBuffer[] _mrt = new RenderBuffer[2];
            _mrt[0] = indexes.colorBuffer;
            _mrt[1] = weights.colorBuffer;
            Graphics.SetRenderTarget(_mrt, indexes.depthBuffer);
            Graphics.Blit(null, mat, 0);
            RenderTexture.active = null;
            DestroyImmediate(mat);
        }

        static int FindIndex(TreePrototype proto, List<TreePrototypeSerializable> protos, TreePrototype[] terrainProtos)
        {
            for (int i = 0; i < terrainProtos.Length; ++i)
            {
                var p = terrainProtos[i];
                if (proto.prefab == p.prefab)
                {
                    for (int x = 0; x < protos.Count; ++x)
                    {
                        if (protos[x].prefab == proto.prefab)
                            return x;
                    }
                }
            }
            return -1;
        }


        public static CopyStamp.TreeCopyData CaptureTrees(Terrain[] terrains, Bounds bounds, Transform trans)
        {
            var culledInstances = new List<TreeInstance>(1024);
            List<TreePrototypeSerializable> prototypes = new List<TreePrototypeSerializable>();
            foreach (var t in terrains)
            {
                var tb = TerrainUtil.ComputeTerrainBounds(t);
                if (tb.Intersects(bounds))
                {
                    // add any missing prototypes
                    var terrainProtos = t.terrainData.treePrototypes;
                    foreach (var proto in terrainProtos)
                    {
                        var tps = new TreePrototypeSerializable(proto);
                        if (!prototypes.Contains(tps))
                        {
                            prototypes.Add(tps);
                        }
                    }
                    Vector2 center = new Vector2(bounds.center.x, bounds.center.z);
                    center.x -= t.transform.position.x;
                    center.y -= t.transform.position.z;
                    center.x /= t.terrainData.size.x;
                    center.y /= t.terrainData.size.z;
                    Vector2 range = new Vector2(bounds.size.x, bounds.size.z);
                    range.x /= t.terrainData.size.x;
                    range.y /= t.terrainData.size.z;
                    center.x -= range.x / 2;
                    center.y -= range.y / 2;

                    Rect cellRect = new Rect(center, range);

                    var instances = t.terrainData.treeInstances;
                    for (int x = 0; x < instances.Length; ++x)
                    {
                        var i = instances[x];
                        if (cellRect.Contains(new Vector2(i.position.x, i.position.z)))
                        {
                            i.position.x -= cellRect.xMin;
                            i.position.z -= cellRect.yMin;
                            i.position.x *= 1.0f / cellRect.size.x;
                            i.position.z *= 1.0f / cellRect.size.y;
                            i.prototypeIndex = FindIndex(terrainProtos[i.prototypeIndex], prototypes, terrainProtos);
                            culledInstances.Add(i);
                        }
                    }
                }
            }


            int count = culledInstances.Count;
            int yCount = count / 512 + 1;
                   
            var posTex = new Texture2D(512, yCount, TextureFormat.RGBAHalf, false, true);
            var randTex = new Texture2D(512, yCount, TextureFormat.RGBAHalf, false, true);
           
            for (int x = 0; x < culledInstances.Count; ++x)
            {
                var i = culledInstances[x];
                Color pos = new Color();
                Color rand = new Color();
                
                pos.r = i.position.x;
                pos.g = i.position.y;
                pos.b = i.position.z;
                pos.a = 1;

                rand.r = i.prototypeIndex;
                rand.g = i.heightScale;
                rand.b = i.widthScale;
                rand.a = i.rotation;

                int xp = x % 512;
                int yp = x / 512;

                posTex.SetPixel(xp, yp, pos);
                randTex.SetPixel(xp, yp, rand);
            }
            for (int x = culledInstances.Count; x < 512 * yCount; ++x)
            {
                int xp = x % 512;
                int yp = x / 512;
                posTex.SetPixel(xp, yp, Color.clear);
                randTex.SetPixel(xp, yp, Color.clear);
            }

            posTex.Apply(false, false);
            randTex.Apply(false, false);

            CopyStamp.TreeCopyData treeData = new CopyStamp.TreeCopyData();
            treeData.prototypes = prototypes.ToArray();
            treeData.positionsData = posTex.GetRawTextureData(); ;
            treeData.randomsData = randTex.GetRawTextureData();
            treeData.dataSize = new Vector2Int(posTex.width, posTex.height);
            return treeData;
        }


        public static CopyStamp.DetailCopyData CaptureDetails(Terrain[] terrains, Bounds bounds, Transform trans)
        {
            CopyStamp.DetailCopyData cd = new CopyStamp.DetailCopyData();
            Dictionary<DetailPrototypeSerializable, RenderTexture> bufferMap = new Dictionary<DetailPrototypeSerializable, RenderTexture>();
            Material copyMat = new Material(Shader.Find("Hidden/MicroVerse/CopyStamp"));

            foreach (var t in terrains)
            {
                var tb = TerrainUtil.ComputeTerrainBounds(t);
                if (tb.Intersects(bounds))
                {
                    var terrainProtos = t.terrainData.detailPrototypes;
                    for (int protoIdx = 0; protoIdx < terrainProtos.Length; ++protoIdx)
                    {
                        var proto = terrainProtos[protoIdx];
                        var dps = new DetailPrototypeSerializable(proto);
                        int res = t.terrainData.detailResolution;
                        int[,] data = t.terrainData.GetDetailLayer(0, 0, res, res, protoIdx);
                        // Due to terrible unity API, hack
                        // directly copy into byte[], then into texture,
                        // then have the shader just read the 8 bytes is uses
                        // of the R8 which this data actually represents. Fuckin Unity..
                        byte[] bytes = new byte[data.Length * sizeof(int)];
                        System.Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);

                        Texture2D dataTex = new Texture2D(res, res, TextureFormat.RGBA32, false, true);
                        dataTex.LoadRawTextureData(bytes);
                        dataTex.Apply(false, false);
                        // now we can just sample the R channel for the data to make the stamp

                        var scale = trans.lossyScale;
                        float fPixelsX = scale.x * t.terrainData.detailResolution / t.terrainData.size.x;
                        float fPixelsY = scale.z * t.terrainData.detailResolution / t.terrainData.size.z;

                        int pixelsX = Mathf.FloorToInt(fPixelsX);
                        int pixelsY = Mathf.FloorToInt(fPixelsY);

                        Vector3 ourPos = WorldToTerrain(t, trans.position);
                        Vector2 uv = new Vector2(ourPos.x, ourPos.z);
                        copyMat.SetVector("_UVCenter", uv);
                        RenderTexture old = null;
                        if (bufferMap.ContainsKey(dps))
                        {
                            old = bufferMap[dps];
                        }
                        else
                        {
                            old = RenderTexture.GetTemporary(pixelsX, pixelsY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
                            bufferMap[dps] = old;
                            old.name = "CopyPasteStampEditor::DetailRT";
                            old.wrapMode = TextureWrapMode.Clamp;
                            RenderTexture.active = old;
                            GL.Clear(false, true, Color.clear);
                        }
                        var tempRT = RenderTexture.GetTemporary(old.descriptor);
                        copyMat.SetVector("_UVRange", new Vector2(fPixelsX, fPixelsY) / new Vector2(res*2, res*2));
                        copyMat.SetTexture("_CurrentBuffer", old);
                        copyMat.SetTexture("_Source", dataTex);
                        Graphics.Blit(null, tempRT, copyMat);
                        bufferMap[dps] = tempRT;
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(old);
                    }
                }
            }
            DestroyImmediate(copyMat);
            foreach (var key in bufferMap.Keys)
            {
                var buffer = bufferMap[key];
                CopyStamp.DetailCopyData.Layer layer = new CopyStamp.DetailCopyData.Layer();
                layer.prototype = key;
                var tex = new Texture2D(buffer.width, buffer.height, TextureFormat.R8, false, true);
                RenderTexture.active = buffer;
                tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                RenderTexture.active = null;
                tex.Apply(false, true);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(buffer);
                layer.dataSize = new Vector2Int(tex.width, tex.height);
                layer.bytes = tex.GetRawTextureData();
                cd.layers.Add(layer);
            }
            
            return cd;
        }


        public static void Capture(CopyPasteStamp cpStamp, string path)
        {
            MicroVerse.instance.SyncTerrainList();
            Material copyMat = new Material(Shader.Find("Hidden/MicroVerse/CopyStamp"));
            var terrains = MicroVerse.instance.terrains;
            RenderTexture heightBuffer = null;
            RenderTexture indexBuffer = null;
            RenderTexture weightBuffer = null;
            RenderTexture holeBuffer = null;
            Vector2 renorm = new Vector2(0, 1);
            List<TerrainLayer> layers = new List<TerrainLayer>();
            var origTerrainScale = cpStamp.transform.localScale;
            foreach (var terrain in terrains)
            {
                var tl = terrain.terrainData.terrainLayers;
                
                // height stamp
                {
                    CopyPasteStamp.SetTerrainScale(cpStamp.transform, terrain, terrain.terrainData.heightmapResolution);
                    var cpBounds = cpStamp.GetBounds();
                    if (cpBounds.Intersects(TerrainUtil.ComputeTerrainBounds(terrain)))
                    {
                        Vector3 ourPos = WorldToTerrain(terrain, cpStamp.transform.position);
                        Vector2 uv = new Vector2(ourPos.x, ourPos.z);
                        copyMat.SetVector("_UVCenter", uv);

                        var scale = cpStamp.transform.lossyScale;
                        float realSize = terrain.terrainData.heightmapScale.y * 2.0f;
                        renorm.y = realSize / scale.y;
                        renorm.x = cpStamp.transform.position.y / realSize;

                        float fPixelsX = scale.x * terrain.terrainData.heightmapResolution / terrain.terrainData.size.x;
                        float fPixelsY = scale.z * terrain.terrainData.heightmapResolution / terrain.terrainData.size.z;

                        int pixelsX = Mathf.FloorToInt(fPixelsX);
                        int pixelsY = Mathf.FloorToInt(fPixelsY);
                        RenderTexture heightSource = terrain.terrainData.heightmapTexture;
                        if (heightBuffer == null)
                        {
                            var desc = heightSource.descriptor;
                            desc.width = pixelsX;
                            desc.height = pixelsY;
                            heightBuffer = RenderTexture.GetTemporary(desc);
                            heightBuffer.name = "CopyPasteStampEditor::heights";
                            heightBuffer.wrapMode = TextureWrapMode.Clamp;
                            heightBuffer.filterMode = FilterMode.Point;
                            RenderTexture.active = heightBuffer;
                            GL.Clear(false, true, Color.clear);
                        }

                        RenderTexture heightTemp = RenderTexture.GetTemporary(heightBuffer.descriptor);
                        heightTemp.name = "CopyPasteStampEditor::heightTemp";
                        Graphics.Blit(heightBuffer, heightTemp);
                        heightTemp.wrapMode = TextureWrapMode.Clamp;
                        copyMat.SetVector("_UVRange", new Vector2(fPixelsX, fPixelsY) / new Vector2(heightSource.height * 2, heightSource.width * 2));
                        copyMat.SetTexture("_CurrentBuffer", heightTemp);
                        copyMat.SetTexture("_Source", heightSource);
                        copyMat.EnableKeyword("_COPYHEIGHT");
                        copyMat.SetFloat("_YOffset", (cpStamp.transform.position.y - terrain.GetPosition().y) / realSize);
                        Graphics.Blit(heightSource, heightBuffer, copyMat);
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(heightTemp);
                        copyMat.DisableKeyword("_COPYHEIGHT");
                    }


                    cpStamp.transform.localScale = origTerrainScale;
                }
                // holes
                {
                    CopyPasteStamp.SetTerrainScale(cpStamp.transform, terrain, terrain.terrainData.holesResolution);
                    var cpBounds = cpStamp.GetBounds();
                    if (cpBounds.Intersects(TerrainUtil.ComputeTerrainBounds(terrain)))
                    {
                        Texture holeSource = terrain.terrainData.holesTexture;
                        Vector3 ourPos = WorldToTerrain(terrain, cpStamp.transform.position);
                        Vector2 uv = new Vector2(ourPos.x, ourPos.z);
                        copyMat.SetVector("_UVCenter", uv);

                        var scale = cpStamp.transform.lossyScale;
                        float realSize = terrain.terrainData.heightmapScale.y * 2.0f;

                        float fHolePixelsX = scale.x * terrain.terrainData.holesResolution / terrain.terrainData.size.x;
                        float fHolePixelsY = scale.z * terrain.terrainData.holesResolution / terrain.terrainData.size.z;
                        int holePixelsX = Mathf.FloorToInt(fHolePixelsX);
                        int holePixelsY = Mathf.FloorToInt(fHolePixelsY);

                        if (holeBuffer == null)
                        {
                            holeBuffer = RenderTexture.GetTemporary(holePixelsX, holePixelsY, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
                            holeBuffer.name = "CopyPasteStampEditor::holeBuffer";
                            holeBuffer.wrapMode = TextureWrapMode.Clamp;
                            holeBuffer.filterMode = FilterMode.Point;
                            RenderTexture.active = holeBuffer;
                            GL.Clear(false, true, Color.white);
                        }

                        RenderTexture holeTemp = RenderTexture.GetTemporary(holeBuffer.descriptor);
                        holeTemp.wrapMode = TextureWrapMode.Clamp;
                        holeTemp.filterMode = FilterMode.Point;
                        Graphics.Blit(holeBuffer, holeTemp);
                        copyMat.SetVector("_UVRange", new Vector2(fHolePixelsX, fHolePixelsY) / new Vector2(holeSource.width * 2, holeSource.height * 2));
                        copyMat.SetTexture("_CurrentBuffer", holeTemp);
                        copyMat.SetTexture("_Source", holeSource);
                        Graphics.Blit(holeSource, holeBuffer, copyMat);
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(holeTemp);
                    }
                    cpStamp.transform.localScale = origTerrainScale;
                }
                // splat
                {
                    foreach (var tlr in tl)
                    {
                        if (!layers.Contains(tlr))
                        {
                            layers.Add(tlr);
                        }
                    }
                    CopyPasteStamp.SetTerrainScale(cpStamp.transform, terrain, terrain.terrainData.alphamapHeight);
                    var cpBounds = cpStamp.GetBounds();
                    if (cpBounds.Intersects(TerrainUtil.ComputeTerrainBounds(terrain)))
                    {

                        Vector3 ourPos = WorldToTerrain(terrain, cpStamp.transform.position);
                        Vector2 uv = new Vector2(ourPos.x, ourPos.z);
                        copyMat.SetVector("_UVCenter", uv);

                        var scale = cpStamp.transform.lossyScale;
                        float realSize = terrain.terrainData.heightmapScale.y * 2.0f;
                        float fSplatPixelsX = scale.x * terrain.terrainData.alphamapResolution / terrain.terrainData.size.x;
                        float fSplatPixelsY = scale.z * terrain.terrainData.alphamapResolution / terrain.terrainData.size.z;
                        int splatPixelsX = Mathf.FloorToInt(fSplatPixelsX);
                        int splatPixelsY = Mathf.FloorToInt(fSplatPixelsY);


                        int splatRes = terrain.terrainData.alphamapResolution;
                        if (weightBuffer == null)
                        {
                            weightBuffer = RenderTexture.GetTemporary(splatPixelsX, splatPixelsY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                            heightBuffer.name = "CopyPasteStampEditor::weightBuffer";
                            weightBuffer.wrapMode = TextureWrapMode.Clamp;
                            weightBuffer.filterMode = FilterMode.Point;
                            RenderTexture.active = weightBuffer;
                            GL.Clear(false, true, Color.clear);
                        }
                        if (indexBuffer == null)
                        {
                            indexBuffer = RenderTexture.GetTemporary(splatPixelsX, splatPixelsY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                            heightBuffer.name = "CopyPasteStampEditor::indexBuffer";
                            indexBuffer.wrapMode = TextureWrapMode.Clamp;
                            indexBuffer.filterMode = FilterMode.Point;
                            RenderTexture.active = indexBuffer;
                            GL.Clear(false, true, Color.clear);
                        }
                        RenderTexture indexSource;
                        RenderTexture weightSource;
                        GenerateMega(terrain, out indexSource, out weightSource);


                        RenderTexture indexTemp = RenderTexture.GetTemporary(indexBuffer.descriptor);
                        indexTemp.name = "CopyPasteStampEditor::indexTemp";
                        indexTemp.wrapMode = TextureWrapMode.Clamp;
                        indexTemp.filterMode = FilterMode.Point;
                        Graphics.Blit(indexBuffer, indexTemp);
                        copyMat.SetVector("_UVRange", new Vector2(fSplatPixelsX, fSplatPixelsY) / new Vector2(indexSource.width * 2, indexSource.height * 2));
                        copyMat.SetTexture("_CurrentBuffer", indexTemp);
                        copyMat.SetTexture("_Source", indexSource);
                        Graphics.Blit(indexSource, indexBuffer, copyMat);
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(indexTemp);

                        RenderTexture weightTemp = RenderTexture.GetTemporary(weightBuffer.descriptor);
                        weightTemp.wrapMode = TextureWrapMode.Clamp;
                        weightTemp.filterMode = FilterMode.Point;
                        Graphics.Blit(weightBuffer, weightTemp);
                        copyMat.SetVector("_UVRange", new Vector2(fSplatPixelsX, fSplatPixelsY) / new Vector2(weightSource.width * 2, weightSource.height * 2));
                        copyMat.SetTexture("_CurrentBuffer", weightTemp);
                        copyMat.SetTexture("_Source", weightSource);
                        Graphics.Blit(weightSource, weightBuffer, copyMat);
                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(weightTemp);

                    }
                    cpStamp.transform.localScale = origTerrainScale;
                }
            }

            Texture2D heightTex = null;
            if (cpStamp.copyHeights)
            {
                heightTex = new Texture2D(heightBuffer.width, heightBuffer.height, TextureFormat.R16, false, true);
                heightTex.wrapMode = TextureWrapMode.Clamp;
                RenderTexture.active = heightBuffer;
                heightTex.ReadPixels(new Rect(0, 0, heightTex.width, heightTex.height), 0, 0);
                RenderTexture.active = null;
                heightTex.Apply(false, true);
            }
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(heightBuffer);

            Texture2D weightTex = null;
            Texture2D indexTex = null;
            Texture2D holeTex = null;
            TerrainLayer[] terrainLayers = null;
            if (cpStamp.copyTexturing)
            {
                weightTex = new Texture2D(weightBuffer.width, weightBuffer.height, TextureFormat.ARGB32, false, true);
                RenderTexture.active = weightBuffer;
                weightTex.ReadPixels(new Rect(0, 0, weightBuffer.width, weightBuffer.height), 0, 0);
                RenderTexture.active = null;
                weightTex.Apply(false, true);
                

                indexTex = new Texture2D(indexBuffer.width, indexBuffer.height, TextureFormat.ARGB32, false, true);
                RenderTexture.active = indexBuffer;
                indexTex.ReadPixels(new Rect(0, 0, indexBuffer.width, indexBuffer.height), 0, 0);
                RenderTexture.active = null;
                indexTex.Apply(false, true);
                terrainLayers = layers.Distinct().ToArray();

            }

            if (cpStamp.copyHoles)
            {
                holeTex = new Texture2D(holeBuffer.width, holeBuffer.height, TextureFormat.R8, false, true);
                RenderTexture.active = holeBuffer;
                holeTex.ReadPixels(new Rect(0, 0, holeBuffer.width, holeBuffer.height), 0, 0);
                RenderTexture.active = null;
                holeTex.Apply(false, true);
            }


            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(indexBuffer);
            RenderTexture.ReleaseTemporary(weightBuffer);

            CopyStamp.TreeCopyData tcd = null;
            CopyStamp.DetailCopyData dcd = null;

            if (cpStamp.copyTrees)
            {
                tcd = CaptureTrees(terrains, cpStamp.GetBounds(), cpStamp.transform);
            }
            if (cpStamp.copyDetails)
            {
                dcd = CaptureDetails(terrains, cpStamp.GetBounds(), cpStamp.transform);
            }
            if (cpStamp.stamp == null)
            {
                var cp = CopyStamp.Create(heightTex, indexTex, weightTex, holeTex, terrainLayers, renorm, tcd,
                    dcd);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                if (!path.EndsWith(".asset"))
                    path += ".asset";
                path = path.Replace("..", ".");

                AssetDatabase.CreateAsset(cp, AssetDatabase.GenerateUniqueAssetPath(path));
                cpStamp.stamp = AssetDatabase.LoadAssetAtPath<CopyStamp>(path);
            }
            else
            {
                Undo.RecordObject(cpStamp.stamp, "Update CopyStamp Data");
                
                cpStamp.stamp.layers = terrainLayers;
                cpStamp.stamp.heightRenorm = renorm;
                cpStamp.stamp.heightData = heightTex != null ? heightTex.GetRawTextureData() : null;
                cpStamp.stamp.indexData = indexTex != null ? indexTex.GetRawTextureData() : null;
                cpStamp.stamp.weightData = weightTex != null ? weightTex.GetRawTextureData() : null;
                cpStamp.stamp.holeData = holeTex != null ? holeTex.GetRawTextureData() : null;
                if (heightTex != null) cpStamp.stamp.heightSize = new Vector2Int(heightTex.width, heightTex.height);
                if (indexTex != null && weightTex != null) cpStamp.stamp.indexWeightSize = new Vector2Int(indexTex.width, indexTex.height);
                if (holeTex != null) cpStamp.stamp.holeSize = new Vector2Int(holeTex.width, holeTex.height);
                cpStamp.stamp.treeData = tcd;
                cpStamp.stamp.detailData = dcd;

                // Wipe the unpacked values so that Unpack can do its work
                cpStamp.stamp.heightMap = null;
                cpStamp.stamp.indexMap = null;
                cpStamp.stamp.weightMap = null;
                cpStamp.stamp.holeMap = null;

                EditorUtility.SetDirty(cpStamp.stamp);
            }
        }


        public static string lastDir = "Assets/";
        public override void OnInspectorGUI()
        {
            
            GUIUtil.DrawHeaderLogo();

            serializedObject.Update();
            var cpStamp = (CopyPasteStamp)target;

            if (cpStamp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyHeights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyTexturing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyHoles"));
#if __MICROVERSE_VEGETATION__
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyTrees"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("copyDetails"));
#endif
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pixelQuantization"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyHeights"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyTexturing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyHoles"));
#if __MICROVERSE_VEGETATION__
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyTrees"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("applyDetails"));
#endif
            
            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                MicroVerse.instance?.Invalidate(cpStamp.GetBounds());
            }
            GUIUtil.DrawSeparator();

            if (GUILayout.Button("Create New Copy Object"))
            {
                var path = EditorUtility.SaveFilePanel("Save Copy Stamp", lastDir, "CopyStamp", "asset");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                path = path.Replace("\\", "/");
                path = path.Substring(path.IndexOf("/Assets") + 1);

                lastDir = path.Substring(0, path.LastIndexOf("/"));

                Capture(cpStamp, path);
            }
            if (cpStamp.stamp != null)
            {
                if (GUILayout.Button("Re-Copy"))
                {
                    var path = AssetDatabase.GetAssetPath(cpStamp.stamp);
                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
                    path = path.Replace("\\", "/");
                    path = path.Substring(path.IndexOf("/Assets") + 1);

                    Capture(cpStamp, path);
                }
            }
            if (MicroVerse.instance != null)
            {
                if (MicroVerse.instance.enabled)
                {
                    if (GUILayout.Button("Disable MicroVerse"))
                    {
                        MicroVerse.instance.enabled = false;
                    }
                }
                else if (GUILayout.Button("Enable MicroVerse"))
                {
                    MicroVerse.instance.enabled = true;
                }
            }

            GUIUtil.DrawSeparator();
            using var changeScope = new EditorGUI.ChangeCheckScope();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stamp"));
            if (cpStamp.heightStamp != null)
            {
                SerializedObject hso = new SerializedObject(cpStamp.heightStamp);
                hso.Update();
                EditorGUILayout.PropertyField(hso.FindProperty("mode"));
                hso.ApplyModifiedProperties();
                GUIUtil.DrawFalloffFilter(cpStamp.heightStamp, cpStamp.heightStamp.falloff, cpStamp.transform, false);

                serializedObject.ApplyModifiedProperties();
            }
            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(cpStamp.GetBounds());
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (CopyPasteStamp)target;
            if (stamp.heightStamp.falloff.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.heightStamp.falloff.paintMask, stamp.GetBounds(), stamp.transform);
            }
        }

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            SceneView.duringSceneGui += OnSceneRepaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            SceneView.duringSceneGui -= OnSceneRepaint;
        }

        static Texture2D overlayTexCopy = null;
        static Texture2D overlayTexPaste = null;
        private void OnSceneRepaint(SceneView sceneView)
        {
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTexCopy == null)
                {
                    overlayTexCopy = Resources.Load<Texture2D>("microverse_stamp_copy");
                }
                if (overlayTexPaste == null)
                {
                    overlayTexPaste = Resources.Load<Texture2D>("microverse_stamp_paste");
                }
                var terrains = MicroVerse.instance.terrains;
                var cp = (target as CopyPasteStamp);
                if (cp == null) return;
                if (cp.heightStamp == null) return;
                Color color = Color.gray;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.copyStampColor;
                }
                PreviewRenderer.DrawStampPreview(cp, terrains, cp.transform, cp.heightStamp.falloff, color, cp.stamp == null ? overlayTexCopy : overlayTexPaste);
            }
        }

        class CachedTransform
        {
            public Vector3 pos;
            public Quaternion quat;
            public Vector3 scale;

            public void Capture(Transform t)
            {
                pos = t.position;
                quat = t.rotation;
                scale = t.lossyScale;
            }

            public bool Compare(Transform t)
            {
                if (t.position != pos) return false;
                if (t.rotation != quat) return false;
                if (t.localScale != t.lossyScale) return false;
                return true;
            }
        }

        Dictionary<CopyPasteStamp, CachedTransform> cachedTransforms = new Dictionary<CopyPasteStamp, CachedTransform>();
        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
               
                var stamp = (CopyPasteStamp)target;
                if (stamp.stamp == null)
                    continue;
                if (!cachedTransforms.ContainsKey(stamp))
                {
                    CachedTransform t = new CachedTransform();
                    t.Capture(stamp.transform);
                    cachedTransforms[stamp] = t;
                }
                var ct = cachedTransforms[stamp];
                if (!ct.Compare(stamp.transform))
                {
                    ct.Capture(stamp.transform);
                    var r = stamp.transform.localRotation.eulerAngles;
                    r.x = 0;
                    r.z = 0;
                    stamp.transform.localRotation = Quaternion.Euler(r);
                    stamp.GetComponentInParent<MicroVerse>()?.Invalidate(stamp.GetBounds());
                }
            }
        }
    }
}