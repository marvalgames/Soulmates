using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [System.Serializable]
    public class FalloffFilter
    {
        public enum FilterType
        {
            Global = 0,
            Box = 1,
            Range = 2,
            Texture = 3,
            SplineArea = 4,
            PaintMask = 5,
        }

        public enum FilterTypeNoGlobal
        {
            Box = 1,
            Range = 2,
            Texture = 3,
            SplineArea = 4,
            PaintMask = 5,
        }

        public enum FilterTypeNoPaintMask
        {
            Global = 0,
            Box = 1,
            Range = 2,
            Texture = 3,
            SplineArea = 4,
        }

        public enum FilterTypeNoGlobalNoPaintMask
        {
            Box = 1,
            Range = 2,
            Texture = 3,
            SplineArea = 4,
        }

        public enum TextureChannel
        {
            R = 0,
            G,
            B,
            A
        }



        public static TTarget CastEnum<TSource, TTarget>(TSource source, TTarget fallback)
            where TSource : Enum
            where TTarget : Enum
        {
            string sourceName = Enum.GetName(typeof(TSource), source);

            if (Enum.GetNames(typeof(TTarget)).Contains(sourceName))
            {
                return (TTarget)Enum.Parse(typeof(TTarget), sourceName);
            }
            else
            {
                return fallback;
            }
        }



        public FilterType filterType;
        public Texture2D texture;
        public TextureChannel textureChannel = TextureChannel.R;
        public Vector2 textureParams = new Vector2(1, 0); // amplitude, balance
        public Vector4 textureRotationScale = new Vector4(0, 1, 0, 0);
        public bool clampTexture;

        [System.Serializable]
        public class PaintMask
        {
            public enum Size
            {
                k64 = 64,
                k128 = 128,
                k256 = 256,
                k512 = 512,
                k1024 = 1024
            };

            [System.NonSerialized] public Texture2D texture;

            public byte[] bytes;
            public Size size = Size.k256;

            [System.NonSerialized]
            public bool painting = false;

            public enum UpdateMode
            {
                EveryChange,
                EndStroke
            }
            public UpdateMode updateMode = UpdateMode.EveryChange;

            public void Clear()
            {
                if (texture != null)
                    GameObject.DestroyImmediate(texture);
                texture = null;
                bytes = null;
            }

            public void Resize(Size newSize)
            {
                if (newSize != size && texture != null)
                {
                    size = newSize;
                    var rt = RenderTexture.GetTemporary((int)newSize, (int)newSize, 0, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);
                    Graphics.Blit(texture, rt);
                    Clear();
                    Unpack();
                    RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    texture.Apply();
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rt);
                    Pack();
                }
                else if (texture == null)
                    size = newSize;
                

            }

            public void Fill(float val)
            {
                if (texture == null)
                {
                    Unpack();
                }
                Color c = new Color(val, 0, 0, 0);
                for (int x = 0; x < texture.width; ++x)
                {
                    for (int y = 0; y < texture.height; ++y)
                    {
                        texture.SetPixel(x, y, c);
                    }
                }
                texture.Apply(false, false);
                Pack();
            }

            public void Unpack()
            {
                if (texture == null || texture.width != (int)size)
                {
                    texture = new Texture2D((int)size, (int)size, TextureFormat.R16, false, true);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.hideFlags = HideFlags.DontSave;
                }
                if (bytes != null && bytes.Length == (int)size * (int)size * 2)
                {
                    texture.LoadRawTextureData(bytes);
                    texture.Apply();
                    texture.hideFlags = HideFlags.DontSave;
                }
                else
                {
                    Fill(1.0f);
                    Pack();
                }
            }

            public void Pack()
            {
                if (texture != null)
                {
                    bytes = texture.GetRawTextureData();
                }
            }

            public void Paint(float x, float y, float brushSize,
                float brushFalloff, float brushFlow, float targetValue, double deltaTime)
            {
                if (texture == null)
                    Unpack();
                int isize = texture.width;
                float brushSizeMult = brushSize * ((float)isize / 512);
                int bx = Mathf.RoundToInt(Mathf.Clamp(x * isize - brushSizeMult, 0, isize));
                int by = Mathf.RoundToInt(Mathf.Clamp(y * isize - brushSizeMult, 0, isize));
                int tx = Mathf.RoundToInt(Mathf.Clamp(x * isize + brushSizeMult, 0, isize));
                int ty = Mathf.RoundToInt(Mathf.Clamp(y * isize + brushSizeMult, 0, isize));

                for (int xp = bx; xp < tx; ++xp)
                {
                    for (int yp = by; yp < ty; ++yp)
                    {
                        float w = Vector2.Distance(new Vector2(x * isize, y * isize), new Vector2(xp, yp)) / brushSizeMult;
                        w = 1 - Mathf.Clamp01(w);
                        w = Mathf.Pow(w, brushFalloff);
                        w *= brushFlow;
                        w *= (float)deltaTime;
                        Color c = texture.GetPixel(xp, yp);
                        c.r = Mathf.Lerp(c.r, (float)targetValue, w);
                        texture.SetPixel(xp, yp, c);
                    }
                }
                texture.Apply();
                Pack();
            }


            public void Smooth(float x, float y, float brushSize,
                float brushFalloff, float brushFlow, float targetValue, double deltaTime)
            {
                if (texture == null)
                    Unpack();

                int isize = texture.width;
                int bx = Mathf.RoundToInt(Mathf.Clamp(x * isize - brushSize, 0, isize));
                int by = Mathf.RoundToInt(Mathf.Clamp(y * isize - brushSize, 0, isize));
                int tx = Mathf.RoundToInt(Mathf.Clamp(x * isize + brushSize, 0, isize));
                int ty = Mathf.RoundToInt(Mathf.Clamp(y * isize + brushSize, 0, isize));

                // Compute smoothRadius based on brushSize
                int smoothRadius = Mathf.RoundToInt(brushSize);

                // Apply a smoothing operation
                for (int xp = bx - smoothRadius; xp < tx + smoothRadius; ++xp)
                {
                    for (int yp = by - smoothRadius; yp < ty + smoothRadius; ++yp)
                    {
                        if (xp >= 0 && xp < isize && yp >= 0 && yp < isize)
                        {
                            float sumWeights = 0f;
                            Color avgColor = new Color(0, 0, 0, 0);
                            float w = Vector2.Distance(new Vector2(x * isize, y * isize), new Vector2(xp, yp)) / brushSize;
                            w = 1 - Mathf.Clamp01(w);
                            w = Mathf.Pow(w, brushFalloff);
                            w *= brushFlow;
                            w *= (float)deltaTime;
                            w *= 10;
                            for (int i = -3; i < 3; i++)
                            {
                                for (int j = -3; j < 1; j++)
                                {
                                    int nx = xp + i;
                                    int ny = yp + j;

                                    if (nx >= 0 && nx < isize && ny >= 0 && ny < isize)
                                    {
                                        float neighborDistance = Vector2.Distance(new Vector2(xp, yp), new Vector2(nx, ny)) / brushSize;
                                        float neighborWeight = Mathf.Pow(1 - Mathf.Clamp01(neighborDistance), brushFalloff);

                                        avgColor += texture.GetPixel(nx, ny) * neighborWeight;
                                        sumWeights += neighborWeight;
                                    }
                                }
                            }

                            if (sumWeights > 0)
                            {
                                avgColor /= sumWeights;
                                Color c = texture.GetPixel(xp, yp);
                                avgColor = Color.Lerp(c, avgColor, w);
                                texture.SetPixel(xp, yp, avgColor);
                            }
                        }
                    }
                }

                texture.Apply();
                Pack();
            }


        }


#if __MICROVERSE_SPLINES__
        public SplineArea splineArea;
        public float splineAreaFalloff;
        public float splineAreaFalloffBoost;
#endif

        public PaintFalloffArea paintArea;


        public Easing easing = new Easing();
        public Noise noise = new Noise();

        public Vector2 falloffRange = new Vector2(0.8f, 1.0f);
        public PaintMask paintMask = new PaintMask();

        static int _Falloff = Shader.PropertyToID("_Falloff");
        static int _FalloffTexture = Shader.PropertyToID("_FalloffTexture");
        static int _FalloffTextureChannel = Shader.PropertyToID("_FalloffTextureChannel");
        static int _FalloffTextureParams = Shader.PropertyToID("_FalloffTextureParams");
        static int _FalloffTextureRotScale = Shader.PropertyToID("_FalloffTextureRotScale");
        static int _FalloffAreaRange = Shader.PropertyToID("_FalloffAreaRange");
        static int _FalloffAreaBoost = Shader.PropertyToID("_FalloffAreaBoost");
        static int _PaintAreaMatrix = Shader.PropertyToID("_PaintAreaMatrix");
        static int _PaintAreaFalloffTexture = Shader.PropertyToID("_PaintAreaFalloffTexture");
        static int _PaintAreaClamp = Shader.PropertyToID("_PaintAreaClamp");
        static int _TerrainSize = Shader.PropertyToID("_TerrainSize");

        FalloffFilter useFilter = null;
        FalloffFilter GetUseFilter(Transform transform)
        {
            if (useFilter != null)
                return useFilter;

            FalloffOverride fo = transform.GetComponentInParent<FalloffOverride>();
            useFilter = this;
            if (fo != null)
            {
                useFilter = fo.filter;
            }
            return useFilter;


        }
        public void PrepareTerrain(Material mat, Terrain terrain, Transform transform, List<string> keywords)
        {
            var useFilter = GetUseFilter(transform);
#if __MICROVERSE_SPLINES__
            if (useFilter.filterType == FilterType.SplineArea && useFilter.splineArea != null)
            {
                keywords.Add("_USEFALLOFFSPLINEAREA");
                mat.SetTexture(_FalloffTexture, useFilter.splineArea.GetSDF(terrain));
                mat.SetFloat(_FalloffAreaRange, useFilter.splineAreaFalloff);
                mat.SetFloat(_FalloffAreaBoost, useFilter.splineAreaFalloffBoost);
            }
#else
            if (filterType == FilterType.SplineArea)
            {
                keywords.Add("_USEFALLOFFRANGE");
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
#endif
            if (useFilter.paintArea != null)
            {
                keywords.Add("_USEFALLOFFPAINTAREA");
                if (useFilter.paintArea.paintMask.texture == null)
                    useFilter.paintArea.paintMask.Unpack();
 
                mat.SetTexture(_PaintAreaFalloffTexture, useFilter.paintArea.paintMask.texture);
                mat.SetMatrix(_PaintAreaMatrix, useFilter.paintArea.transform.worldToLocalMatrix);
                mat.SetFloat(_PaintAreaClamp, useFilter.paintArea.clampOutsideOfBounds ? 1.0f : 0.0f);
                mat.SetVector(_TerrainSize, terrain.terrainData.size);
            }
        }

        public void PrepareMaterial(Material mat, Transform transform, List<string> keywords)
        {
            this.useFilter = null;
            var useFilter = GetUseFilter(transform);
            if (useFilter.filterType != FilterType.Global)
            {
                useFilter.easing.PrepareMaterial(mat, "_FALLOFF", keywords);
                useFilter.noise.PrepareMaterial(mat, "_FALLOFF", "_Falloff", keywords);
            }

            if (useFilter.filterType == FilterType.Box)
            {
                keywords.Add("_USEFALLOFF");
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
            else if (useFilter.filterType == FilterType.Range)
            {
                keywords.Add("_USEFALLOFFRANGE");
                mat.SetVector(_Falloff, useFilter.falloffRange);
            }
            else if  (useFilter.filterType == FilterType.Texture)
            {
                keywords.Add("_USEFALLOFFTEXTURE");
                mat.SetTexture(_FalloffTexture, useFilter.texture);
                mat.SetFloat(_FalloffTextureChannel, (int)useFilter.textureChannel);
                mat.SetVector(_FalloffTextureParams, useFilter.textureParams);
                mat.SetVector(_FalloffTextureRotScale, useFilter.textureRotationScale);
                mat.SetVector(_Falloff, useFilter.falloffRange);
                if (clampTexture)
                    keywords.Add("_CLAMPFALLOFFTEXTURE");
            }
            else if (useFilter.filterType == FilterType.PaintMask)
            {
                keywords.Add("_USEFALLOFFTEXTURE");
                keywords.Add("_CLAMPFALLOFFTEXTURE");
                if (useFilter.paintMask.texture == null)
                {
                    useFilter.paintMask.Unpack();
                }
                mat.SetTexture(_FalloffTexture, useFilter.paintMask.texture);
                mat.SetFloat(_FalloffTextureChannel, 0);
                mat.SetVector(_FalloffTextureParams, new Vector2(1,0));
                mat.SetVector(_FalloffTextureRotScale, new Vector4(0,1,0,0));
                mat.SetVector(_Falloff, Vector2.one);

            }
        }
    }
}
