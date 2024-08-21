using UnityEngine;
using UnityEngine.TerrainTools;
using System.Collections.Generic;


namespace JBooth.MicroVerseCore
{
    public class PreviewRenderer
    {
        static Material brushPreviewMat;
        static Material noiseMat;
        static Material filterSetMat;

        private static float kNormalizedHeightScale => PaintContext.kNormalizedHeightScale;


        public static Noise noisePreview = null;
        
        public static void DrawNoisePreview()
        {
            if (noisePreview == null)
                return;
            if (MicroVerse.instance == null)
                return;
            if (!noiseMat) noiseMat = new Material(Shader.Find("Hidden/MicroVerse/PreviewNoiseWorld"));

            foreach (var terrain in MicroVerse.instance.terrains)
            {
                if (terrain != null)
                {
                    System.Collections.Generic.List<string> keywords = new System.Collections.Generic.List<string>();
                    int vertexCount = SetupDrawing(terrain, noiseMat);
                    noiseMat.SetVector("_TerrainSize", new Vector2(terrain.terrainData.size.x, terrain.terrainData.size.z));
                    noiseMat.SetTexture("_NoiseTexture", noisePreview.texture);
                    noiseMat.SetTextureScale("_NoiseTexture", noisePreview.GetTextureScale());
                    noiseMat.SetTextureOffset("_NoiseTexture", noisePreview.GetTextureOffset());
                    noisePreview.EnableKeyword(noiseMat, "_", keywords);
                    noiseMat.SetVector("_Param", noisePreview.GetParamVector());
                    noiseMat.SetVector("_Param2", noisePreview.GetParam2Vector());
                    noiseMat.SetFloat("_NoiseChannel", (int)noisePreview.channel);
                    noiseMat.SetColor("_Color", MicroVerse.instance.options.colors.noisePreviewColor);
                    noiseMat.SetVector("_Remap", new Vector2(-noisePreview.displayGamma, 1 + noisePreview.displayGamma));
                    noiseMat.shaderKeywords = keywords.ToArray();
                    Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
                }
            }
        }

        public enum FilterSetType
        {
            Height,
            Slope,
            Angle,
            Curvature,
            Flow,
            Texture
        }
        public static FilterSet.Filter filter = null;
        public static FilterSet filterSet = null; // needed for texture filtering
        public static FilterSetType filterSetType = FilterSetType.Height;

        public static void DrawFilterSetPreview()
        {
            if (filter == null && filterSetType != FilterSetType.Texture)
                return;
            if (filterSetType == FilterSetType.Texture && filterSet == null)
                return;
            if (MicroVerse.instance == null)
                return;

            if (!filterSetMat) filterSetMat = new Material(Shader.Find("Hidden/MicroVerse/PreviewFilterWorld"));

            foreach (var terrain in MicroVerse.instance.terrains)
            {
                if (terrain == null)
                    continue;
                var realHeight = terrain.terrainData.heightmapScale.y * 2;
                if (filter != null)
                {
                    filterSetMat.SetVector("_HeightRange", filter.range / realHeight);
                    filterSetMat.SetVector("_HeightSmoothness", filter.smoothness / realHeight);

                    filterSetMat.SetVector("_SlopeRange", filter.range * Mathf.Deg2Rad);
                    filterSetMat.SetVector("_SlopeSmoothness", filter.smoothness * Mathf.Deg2Rad);

                    filterSetMat.SetVector("_AngleRange", filter.range * Mathf.Deg2Rad);
                    filterSetMat.SetVector("_AngleSmoothness", filter.smoothness * Mathf.Deg2Rad);

                    filterSetMat.SetVector("_CurvatureRange", filter.range);
                    filterSetMat.SetVector("_CurvatureSmoothness", filter.smoothness);

                    filterSetMat.SetVector("_FlowRange", filter.range);
                    filterSetMat.SetVector("_FlowSmoothness", filter.smoothness);

                    filterSetMat.SetColor("_Color", MicroVerse.instance.options.colors.filterPreviewColor);

                }
                filterSetMat.SetTexture("_Normalmap", terrain.normalmapTexture);

                filterSetMat.DisableKeyword("_HEIGHTFILTER");
                filterSetMat.DisableKeyword("_SLOPEFILTER");
                filterSetMat.DisableKeyword("_ANGLEFILTER");
                filterSetMat.DisableKeyword("_CURVATUREFILTER");
                filterSetMat.DisableKeyword("_FLOWFILTER");
                filterSetMat.DisableKeyword("_TEXTUREFILTER");
                filterSetMat.DisableKeyword("_USECURVE");
                if (filter != null && filter.filterType == FilterSet.Filter.FilterType.Curve)
                {
                    filterSetMat.EnableKeyword("_USECURVE");
                    filterSetMat.SetTexture("_Curve", filter.curveTexture);
                }
                RenderTexture tempCurvature = null;
                RenderTexture tempFlow = null;
                switch (filterSetType)
                {
                    case FilterSetType.Height:
                    {
                        filterSetMat.EnableKeyword("_HEIGHTFILTER");
                        break;
                    }
                    case FilterSetType.Slope:
                    {
                        filterSetMat.EnableKeyword("_SLOPEFILTER");
                        break;
                    }
                    case FilterSetType.Angle:
                    {
                        filterSetMat.EnableKeyword("_ANGLEFILTER");
                        break;
                    }
                    case FilterSetType.Curvature:
                    {
                        filterSetMat.EnableKeyword("_CURVATUREFILTER");
                        filterSetMat.SetFloat("_MipBias", filter.mipBias);

                        Dictionary<Terrain, RenderTexture> d = new Dictionary<Terrain, RenderTexture>();
                        d.Add(terrain, terrain.normalmapTexture);
                        if (terrain.leftNeighbor != null)
                            d.Add(terrain.leftNeighbor, terrain.leftNeighbor.normalmapTexture);
                        if (terrain.rightNeighbor != null)
                            d.Add(terrain.rightNeighbor, terrain.rightNeighbor.normalmapTexture);
                        if (terrain.topNeighbor != null)
                            d.Add(terrain.topNeighbor, terrain.topNeighbor.normalmapTexture);
                        if (terrain.bottomNeighbor != null)
                            d.Add(terrain.bottomNeighbor, terrain.bottomNeighbor.normalmapTexture);

                        var active = RenderTexture.active; // must restore this!
                        tempCurvature = MapGen.GenerateCurvatureMap(terrain, d, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
                        tempCurvature.name = "Temp::CurvaturePreview";
                        RenderTexture.active = active;
                        filterSetMat.SetTexture("_Curvemap", tempCurvature);
                        break;
                    }
                    case FilterSetType.Flow:
                        {
                            filterSetMat.EnableKeyword("_FLOWFILTER");

                            Dictionary<Terrain, RenderTexture> d = new Dictionary<Terrain, RenderTexture>();
                            d.Add(terrain, terrain.terrainData.heightmapTexture);
                            var active = RenderTexture.active; // must restore this!
                            tempFlow = MapGen.GenerateFlowMap(terrain, d);
                            tempFlow.name = "Temp::FlowPreview";
                            RenderTexture.active = active;
                            filterSetMat.SetTexture("_Flowmap", tempFlow);
                            break;
                        }
                    case FilterSetType.Texture:
                    {
                        if (filterSet != null)
                        {
                            filterSetMat.EnableKeyword("_TEXTUREFILTER");
                            filterSetMat.SetVectorArray("_TextureLayerWeights", filterSet.GetTextureWeights(terrain.terrainData.terrainLayers));
                            filterSetMat.SetTexture("_Control0", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control1", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control2", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control3", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control4", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control5", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control6", Texture2D.blackTexture);
                            filterSetMat.SetTexture("_Control7", Texture2D.blackTexture);
                            var splats = terrain.terrainData.alphamapTextures;
                            for (int i = 0; i < terrain.terrainData.alphamapTextureCount; ++i)
                            {
                                filterSetMat.SetTexture("_Control" + i, splats[i]);
                            }
                            
                        }
                        break;
                    }
                }

                int vertexCount = SetupDrawing(terrain, filterSetMat);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
                if (tempCurvature != null)
                {
                    RenderTexture.ReleaseTemporary(tempCurvature);
                }
                if (tempFlow != null)
                {
                    RenderTexture.ReleaseTemporary(tempFlow);
                }
            }
        }

        public static void DrawStampPreview(IModifier mod, Terrain[] terrains, Transform transform, FalloffFilter filter,
            Color color, Texture2D colorTex = null)
        {
            if (MicroVerse.instance.options.colors.drawStampPreviews == false)
            {
                DrawNoisePreview();
                DrawFilterSetPreview();
                return;
            }
            foreach (var terrain in terrains)
            {
                if (terrain == null)
                    continue;
                var terrainBounds = terrain.terrainData.bounds;
                terrainBounds.center = terrain.transform.position;
                terrainBounds.center += new Vector3(terrainBounds.size.x * 0.5f, 0, terrainBounds.size.z * 0.5f);

                if (terrainBounds.Intersects(mod.GetBounds()))
                {
                    if (filter.filterType == FalloffFilter.FilterType.Range)
                    {
                        Draw(terrain, transform, filter.falloffRange, color, colorTex);
                    }
                    else if (filter.filterType == FalloffFilter.FilterType.Texture)
                    {
                        Draw(terrain, transform, filter.texture, color, colorTex, (int)filter.textureChannel);
                    }
                }
            }
            DrawNoisePreview();
            DrawFilterSetPreview();
        }

        public static void Draw(Terrain terrain, Texture2D tex)
        {
            if (!terrain) return;
            if (!brushPreviewMat) brushPreviewMat = new Material(Shader.Find("Hidden/MicroVerse/PreviewStamp"));

            int vertexCount = SetupDrawing(terrain, brushPreviewMat);
            brushPreviewMat.DisableKeyword("_USEFALLOFFTEXTURE");
            brushPreviewMat.EnableKeyword("_NOFALLOFF");
            brushPreviewMat.SetTexture("_ColorTex", tex);
            brushPreviewMat.SetColor("_Color", Color.white);
            brushPreviewMat.SetTexture("_MainTex", Texture2D.whiteTexture);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
        }

        public static void Draw(Terrain terrain, Transform transform, Texture2D tex,
            Color color, Texture2D colorTex = null, int channel = 0)
        {
            if (!terrain) return;
            if (!brushPreviewMat) brushPreviewMat = new Material(Shader.Find("Hidden/MicroVerse/PreviewStamp"));
            if (color.a < 0.05) return;

            int vertexCount = SetupDrawing(terrain, transform, brushPreviewMat);
            brushPreviewMat.DisableKeyword("_USEFALLOFFTEXTURE");
            brushPreviewMat.SetTexture("_ColorTex", colorTex);
            brushPreviewMat.SetColor("_Color", color);
            brushPreviewMat.EnableKeyword("_USEFALLOFFTEXTURE");
            brushPreviewMat.SetTexture("_MainTex", tex);
            brushPreviewMat.SetFloat("_FalloffChannel", channel);
           
            Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
        }

        public static void Draw(Terrain terrain, Transform transform, Vector2 falloffRange,
            Color color, Texture2D colorTex = null, int falloffChannel = 0)
        {
            if (!terrain) return;
            if (!brushPreviewMat) brushPreviewMat = new Material(Shader.Find("Hidden/MicroVerse/PreviewStamp"));
            if (color.a < 0.01f) return;
            int vertexCount = SetupDrawing(terrain, transform, brushPreviewMat);
            brushPreviewMat.DisableKeyword("_USEFALLOFFTEXTURE");
            brushPreviewMat.SetTexture("_ColorTex", colorTex);
            brushPreviewMat.SetColor("_Color", color);
            brushPreviewMat.SetVector("_Falloff", falloffRange);
            brushPreviewMat.SetFloat("_FalloffChannel", falloffChannel);           
            Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
        }

        // whole terrain, no brush
        static int SetupDrawing(Terrain terrain, Material mat)
        {
            Texture heightmapTexture = terrain.terrainData.heightmapTexture;

            RectInt pixelRect = new RectInt(0, 0, heightmapTexture.width, heightmapTexture.height);
            Vector2 pixelSize = new Vector2(terrain.terrainData.size.x / heightmapTexture.width, terrain.terrainData.size.z / heightmapTexture.height);

            int quadsX = pixelRect.width + 1;
            int quadsY = pixelRect.height + 1;
            int vertexCount = quadsX * quadsY * (2 * 3);  // two triangles (2 * 3 vertices) per quad
            
            const int kMaxVerts = 256*256;
            int vertSkip = 1;
            while (vertexCount >= kMaxVerts / 2)   // in practice we want to stay well below 16 million verts, for perf sanity
            {
                quadsX = (quadsX + 1) / 2;
                quadsY = (quadsY + 1) / 2;
                vertexCount = quadsX * quadsY * (2 * 3);
                vertSkip *= 2;
            }

            // this is used to tessellate the quad mesh (from within the vertex shader)
            mat.SetVector("_QuadRez", new Vector4(quadsX, quadsY, vertexCount, vertSkip));

            // paint context pixels to heightmap uv:   uv = (pixels + 0.5) / width
            float invWidth = 1.0f / heightmapTexture.width;
            float invHeight = 1.0f / heightmapTexture.height;
            mat.SetVector("_HeightmapUV_PCPixelsX", new Vector4(invWidth, 0.0f, 0.0f, 0.0f));
            mat.SetVector("_HeightmapUV_PCPixelsY", new Vector4(0.0f, invHeight, 0.0f, 0.0f));
            mat.SetVector("_HeightmapUV_Offset", new Vector4(0.5f * invWidth, 0.5f * invHeight, 0.0f, 0.0f));

            mat.SetTexture("_Heightmap", heightmapTexture);

            float scaleX = pixelSize.x;
            float scaleY = (terrain.terrainData.heightmapScale.y) / kNormalizedHeightScale;
            float scaleZ = pixelSize.y;
            mat.SetVector("_ObjectPos_PCPixelsX", new Vector4(scaleX, 0.0f, 0.0f, 0.0f));
            mat.SetVector("_ObjectPos_HeightMapSample", new Vector4(0.0f, scaleY, 0.0f, 0.0f));
            mat.SetVector("_ObjectPos_PCPixelsY", new Vector4(0.0f, 0.0f, scaleZ, 0.0f));
            //Note slightly offset, so raise up so it doesn't clip through terrain
            mat.SetVector("_ObjectPos_Offset", new Vector4((pixelRect.xMin * scaleX), 1.0f, (pixelRect.yMin * scaleZ) + (pixelSize.y * 0.0f), 1.0f));

            BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, new Vector2(0.5f, 0.5f), terrain.terrainData.size.x, 0.0f);


            // paint context origin in terrain space
            // (note this is the UV space origin and size, not the mesh origin & size)
            float pcOriginX = pixelRect.xMin * pixelSize.x;
            float pcOriginZ = pixelRect.yMin * pixelSize.y;
            float pcSizeX = pixelSize.x;
            float pcSizeZ = pixelSize.y;


            Vector2 scaleU = pcSizeX * brushXform.targetX;
            Vector2 scaleV = pcSizeZ * brushXform.targetY;
            Vector2 offset = brushXform.targetOrigin + pcOriginX * brushXform.targetX + pcOriginZ * brushXform.targetY;
            mat.SetVector("_BrushUV_PCPixelsX", new Vector4(scaleU.x, scaleU.y, 0.0f, 0.0f));
            mat.SetVector("_BrushUV_PCPixelsY", new Vector4(scaleV.x, scaleV.y, 0.0f, 0.0f));
            mat.SetVector("_BrushUV_Offset", new Vector4(offset.x, offset.y, 0.0f, 1.0f));


            mat.SetVector("_TerrainObjectToWorldOffset", terrain.GetPosition());


            mat.SetPass(0);
            return vertexCount;
        }

        // with stamp
        static int SetupDrawing(Terrain terrain, Transform stampTransform, Material mat)
        {
            mat.SetMatrix("_Transform", TerrainUtil.ComputeStampMatrix(terrain, stampTransform)); ;

            return SetupDrawing(terrain, mat);
        }
    }
}