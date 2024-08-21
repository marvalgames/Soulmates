
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

namespace JBooth.MicroVerseCore
{
    public class MapGen
    {
        static Shader curvatureShader = null;
        public static RenderTexture GenerateCurvatureMap(Terrain t, Dictionary<Terrain, RenderTexture> normals, int width, int height)
        {
            Profiler.BeginSample("Generate Curvature Map");
            if (curvatureShader == null)
            {
                curvatureShader = Shader.Find("Hidden/MicroVerse/CurvatureMapGen");
            }
            var material = new Material(curvatureShader);
            var desc = new RenderTextureDescriptor(width, height, 0);
            desc.colorFormat = RenderTextureFormat.R8;
            desc.useMipMap = true;
            var rt = RenderTexture.GetTemporary(desc);
            rt.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = rt;
            material.SetTexture("_Normalmap", normals[t]);
            
            if (t.leftNeighbor && normals.ContainsKey(t.leftNeighbor))
            {
                material.SetTexture("_Normalmap_NX", normals[t.leftNeighbor]);
                material.EnableKeyword("_NX");
            }
            if (t.rightNeighbor && normals.ContainsKey(t.rightNeighbor))
            {
                material.SetTexture("_Normalmap_PX", normals[t.rightNeighbor]);
                material.EnableKeyword("_PX");
            }
            if (t.bottomNeighbor && normals.ContainsKey(t.bottomNeighbor))
            {
                material.SetTexture("_Normalmap_NY", normals[t.bottomNeighbor]);
                material.EnableKeyword("_NY");
            }
            if (t.topNeighbor && normals.ContainsKey(t.topNeighbor))
            {
                material.SetTexture("_Normalmap_PY", normals[t.topNeighbor]);
                material.EnableKeyword("_PY");
            }

            Graphics.Blit(null, rt, material);
            GameObject.DestroyImmediate(material);
            Profiler.EndSample();
            return rt;
        }

        static ComputeShader flowShader = null;
        static int _Width = Shader.PropertyToID("_Width");
        static int _Height = Shader.PropertyToID("_Height");
        static int _WaterMap = Shader.PropertyToID("_WaterMap");
        static int _OutFlow = Shader.PropertyToID("_OutFlow");
        static int _HeightMap = Shader.PropertyToID("_HeightMap");
        static int _VelocityMap = Shader.PropertyToID("_VelocityMap");

        public static RenderTexture QuadCombine(Terrain t, Dictionary<Terrain, RenderTexture> tempRenderData, int borderPixels = 32)
        {
            Profiler.BeginSample("Quad Expand");

            var myMask = tempRenderData[t];
            var expandedRT = RenderTexture.GetTemporary(myMask.width + borderPixels * 2, myMask.height + borderPixels * 2, 0, myMask.format);
            Graphics.CopyTexture(myMask, 0, 0, 0, 0, myMask.width, myMask.height, expandedRT, 0, 0, borderPixels, borderPixels);
            expandedRT.name = "MicroVerse::NineCombine";
            if (t.topNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.topNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor], 0, 0, 0, 0, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, expandedRT.height - borderPixels);
                }
            }
            if (t.leftNeighbor && tempRenderData.ContainsKey(t.leftNeighbor))
            {
                Graphics.CopyTexture(tempRenderData[t.leftNeighbor], 0, 0, myMask.width - borderPixels, 0, borderPixels, myMask.height, expandedRT, 0, 0, 0, borderPixels);
            }
            if (t.rightNeighbor && tempRenderData.ContainsKey(t.rightNeighbor))
            {
                Graphics.CopyTexture(tempRenderData[t.rightNeighbor], 0, 0, 0, 0, borderPixels, myMask.height, expandedRT, 0, 0, expandedRT.width-borderPixels, borderPixels);
            }
            if (t.bottomNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.bottomNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor], 0, 0, 0, myMask.height - borderPixels, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, 0);
                }
            }
            Profiler.EndSample(); // quad combine
            return expandedRT;
        }

        public static RenderTexture NineCombine(Terrain t, Dictionary<Terrain, RenderTexture> tempRenderData, int borderPixels = 32)
        {
            Profiler.BeginSample("Nine Expand");

            var myMask = tempRenderData[t];
            var expandedRT = RenderTexture.GetTemporary(myMask.width + borderPixels * 2, myMask.height + borderPixels * 2, 0, myMask.format);
            //RenderTexture.active = expandedRT;
            //GL.Clear(true, true, Color.black);
            Graphics.CopyTexture(myMask, 0, 0, 0, 0, myMask.width, myMask.height, expandedRT, 0, 0, borderPixels, borderPixels);
            expandedRT.name = "MicroVerse::NineCombine";
            if (t.topNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.topNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor], 0, 0, 0, 0, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, expandedRT.height - borderPixels);
                }
                if (t.topNeighbor.leftNeighbor != null && tempRenderData.ContainsKey(t.topNeighbor.leftNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor.leftNeighbor], 0, 0, myMask.width - borderPixels, 0, borderPixels, borderPixels, expandedRT, 0, 0, 0, expandedRT.height - borderPixels);
                }
                if (t.topNeighbor.rightNeighbor != null && tempRenderData.ContainsKey(t.topNeighbor.rightNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor.rightNeighbor], 0, 0, 0, 0, borderPixels, borderPixels, expandedRT, 0, 0, expandedRT.width-borderPixels, expandedRT.height - borderPixels);
                }
            }
            if (t.leftNeighbor && tempRenderData.ContainsKey(t.leftNeighbor))
            {
                Graphics.CopyTexture(tempRenderData[t.leftNeighbor], 0, 0, myMask.width - borderPixels, 0, borderPixels, myMask.height, expandedRT, 0, 0, 0, borderPixels);
            }
            if (t.rightNeighbor && tempRenderData.ContainsKey(t.rightNeighbor))
            {
                Graphics.CopyTexture(tempRenderData[t.rightNeighbor], 0, 0, 0, 0, borderPixels, myMask.height, expandedRT, 0, 0, expandedRT.width - borderPixels, borderPixels);
            }
            if (t.bottomNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.bottomNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor], 0, 0, 0, myMask.height - borderPixels, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, 0);
                }
                if (t.bottomNeighbor.leftNeighbor != null && tempRenderData.ContainsKey(t.bottomNeighbor.leftNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor.leftNeighbor], 0, 0, myMask.width - borderPixels, myMask.height - borderPixels, borderPixels, borderPixels, expandedRT, 0, 0, 0, 0);
                }
                if (t.bottomNeighbor.rightNeighbor != null && tempRenderData.ContainsKey(t.bottomNeighbor.rightNeighbor))
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor.rightNeighbor], 0, 0, 0, myMask.height - borderPixels, borderPixels, borderPixels, expandedRT, 0, 0, expandedRT.width - borderPixels, 0);
                }
            }
            Profiler.EndSample(); // quad combine
            return expandedRT;
        }

        public static RenderTexture NineCombineCurrentTreeMask(Terrain t, Dictionary<Terrain, OcclusionData> tempRenderData, int borderPixels = 32)
        {
            Profiler.BeginSample("Nine Expand");

            var myMask = tempRenderData[t].currentTreeMask;
            var expandedRT = RenderTexture.GetTemporary(myMask.width + borderPixels * 2, myMask.height + borderPixels * 2, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            //RenderTexture.active = expandedRT;
            //GL.Clear(true, true, Color.black);
            Graphics.CopyTexture(myMask, 0, 0, 0, 0, myMask.width, myMask.height, expandedRT, 0, 0, borderPixels, borderPixels);
            expandedRT.name = "MicroVerse::NineCombine";
            if (t.topNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.topNeighbor) && tempRenderData[t.topNeighbor].currentTreeMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor].currentTreeMask, 0, 0, 0, 0, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, expandedRT.height - borderPixels);
                }
                if (t.topNeighbor.leftNeighbor != null && tempRenderData.ContainsKey(t.topNeighbor.leftNeighbor) && tempRenderData[t.topNeighbor.leftNeighbor].currentTreeMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor.leftNeighbor].currentTreeMask, 0, 0, myMask.width - borderPixels, 0, borderPixels, borderPixels, expandedRT, 0, 0, 0, expandedRT.height - borderPixels);
                }
                if (t.topNeighbor.rightNeighbor != null && tempRenderData.ContainsKey(t.topNeighbor.rightNeighbor) && tempRenderData[t.topNeighbor.rightNeighbor].currentTreeMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor.rightNeighbor].currentTreeMask, 0, 0, 0, 0, borderPixels, borderPixels, expandedRT, 0, 0, expandedRT.width - borderPixels, expandedRT.height - borderPixels);
                }
            }
            if (t.leftNeighbor && tempRenderData.ContainsKey(t.leftNeighbor) && tempRenderData[t.leftNeighbor].currentTreeMask != null)
            {
                Graphics.CopyTexture(tempRenderData[t.leftNeighbor].currentTreeMask, 0, 0, myMask.width - borderPixels, 0, borderPixels, myMask.height, expandedRT, 0, 0, 0, borderPixels);
            }
            if (t.rightNeighbor && tempRenderData.ContainsKey(t.rightNeighbor) && tempRenderData[t.rightNeighbor].currentTreeMask != null)
            {
                Graphics.CopyTexture(tempRenderData[t.rightNeighbor].currentTreeMask, 0, 0, 0, 0, borderPixels, myMask.height, expandedRT, 0, 0, expandedRT.width - borderPixels, borderPixels);
            }
            if (t.bottomNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.bottomNeighbor) && tempRenderData[t.bottomNeighbor].currentTreeMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor].currentTreeMask, 0, 0, 0, myMask.height - borderPixels, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, 0);
                }
                if (t.bottomNeighbor.leftNeighbor != null && tempRenderData.ContainsKey(t.bottomNeighbor.leftNeighbor) && tempRenderData[t.bottomNeighbor.leftNeighbor].currentTreeMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor.leftNeighbor].currentTreeMask, 0, 0, myMask.width - borderPixels, myMask.height - borderPixels, borderPixels, borderPixels, expandedRT, 0, 0, 0, 0);
                }
                if (t.bottomNeighbor.rightNeighbor != null && tempRenderData.ContainsKey(t.bottomNeighbor.rightNeighbor) && tempRenderData[t.bottomNeighbor.rightNeighbor].currentTreeMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor.rightNeighbor].currentTreeMask, 0, 0, 0, myMask.height - borderPixels, borderPixels, borderPixels, expandedRT, 0, 0, expandedRT.width - borderPixels, 0);
                }
            }
            Profiler.EndSample(); 
            return expandedRT;
        }

        public static RenderTexture NineCombineCurrentObjectMask(Terrain t, Dictionary<Terrain, OcclusionData> tempRenderData, int borderPixels = 32)
        {
            Profiler.BeginSample("Nine Expand");

            var myMask = tempRenderData[t].currentObjectMask;
            var expandedRT = RenderTexture.GetTemporary(myMask.width + borderPixels * 2, myMask.height + borderPixels * 2, 0, myMask.format);
            //RenderTexture.active = expandedRT;
            //GL.Clear(true, true, Color.black);
            Graphics.CopyTexture(myMask, 0, 0, 0, 0, myMask.width, myMask.height, expandedRT, 0, 0, borderPixels, borderPixels);
            expandedRT.name = "MicroVerse::NineCombine";
            if (t.topNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.topNeighbor) && tempRenderData[t.topNeighbor].currentObjectMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor].currentObjectMask, 0, 0, 0, 0, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, expandedRT.height - borderPixels);
                }
                if (t.topNeighbor.leftNeighbor != null && tempRenderData.ContainsKey(t.topNeighbor.leftNeighbor) && tempRenderData[t.topNeighbor.leftNeighbor].currentObjectMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor.leftNeighbor].currentObjectMask, 0, 0, myMask.width - borderPixels, 0, borderPixels, borderPixels, expandedRT, 0, 0, 0, expandedRT.height - borderPixels);
                }
                if (t.topNeighbor.rightNeighbor != null && tempRenderData.ContainsKey(t.topNeighbor.rightNeighbor) && tempRenderData[t.topNeighbor.rightNeighbor].currentObjectMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.topNeighbor.rightNeighbor].currentObjectMask, 0, 0, 0, 0, borderPixels, borderPixels, expandedRT, 0, 0, expandedRT.width - borderPixels, expandedRT.height - borderPixels);
                }
            }
            if (t.leftNeighbor && tempRenderData.ContainsKey(t.leftNeighbor) && tempRenderData[t.leftNeighbor].currentObjectMask != null)
            {
                Graphics.CopyTexture(tempRenderData[t.leftNeighbor].currentObjectMask, 0, 0, myMask.width - borderPixels, 0, borderPixels, myMask.height, expandedRT, 0, 0, 0, borderPixels);
            }
            if (t.rightNeighbor && tempRenderData.ContainsKey(t.rightNeighbor) && tempRenderData[t.rightNeighbor].currentObjectMask != null)
            {
                Graphics.CopyTexture(tempRenderData[t.rightNeighbor].currentObjectMask, 0, 0, 0, 0, borderPixels, myMask.height, expandedRT, 0, 0, expandedRT.width - borderPixels, borderPixels);
            }
            if (t.bottomNeighbor != null)
            {
                if (tempRenderData.ContainsKey(t.bottomNeighbor) && tempRenderData[t.bottomNeighbor].currentObjectMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor].currentObjectMask, 0, 0, 0, myMask.height - borderPixels, myMask.width, borderPixels, expandedRT, 0, 0, borderPixels, 0);
                }
                if (t.bottomNeighbor.leftNeighbor != null && tempRenderData.ContainsKey(t.bottomNeighbor.leftNeighbor) && tempRenderData[t.bottomNeighbor.leftNeighbor].currentObjectMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor.leftNeighbor].currentObjectMask, 0, 0, myMask.width - borderPixels, myMask.height - borderPixels, borderPixels, borderPixels, expandedRT, 0, 0, 0, 0);
                }
                if (t.bottomNeighbor.rightNeighbor != null && tempRenderData.ContainsKey(t.bottomNeighbor.rightNeighbor) && tempRenderData[t.bottomNeighbor.rightNeighbor].currentObjectMask != null)
                {
                    Graphics.CopyTexture(tempRenderData[t.bottomNeighbor.rightNeighbor].currentObjectMask, 0, 0, 0, myMask.height - borderPixels, borderPixels, borderPixels, expandedRT, 0, 0, expandedRT.width - borderPixels, 0);
                }
            }
            Profiler.EndSample();
            return expandedRT;
        }


        public static RenderTexture GenerateFlowMap(Terrain t, Dictionary<Terrain, RenderTexture> heights)
        {
            int passes = 5;
            float initialWaterLevel = 0.00013f;
            Profiler.BeginSample("Generate Flow Map");
            int padding = 16;
            int outputSize = 512;
            // 512 + (32/4) = 520
            int scaledPadding = Mathf.RoundToInt(padding / (heights[t].width / outputSize));

            int tempSize = outputSize + scaledPadding * 2;


            if (flowShader == null)
            {
                flowShader = Resources.Load<ComputeShader>("MicroVerseComputeFlowMap");
            }
            // 2049 + 64 = 2113
            var heightMapQ = QuadCombine(t, heights, padding);
            // saves about 0.5ms to downsample
            var heightMap = RenderTexture.GetTemporary(outputSize, outputSize, 0, heightMapQ.format);
            Graphics.Blit(heightMapQ, heightMap);
            RenderTexture.ReleaseTemporary(heightMapQ);

            var waterMap = new RenderTexture(tempSize, tempSize, 0, RenderTextureFormat.RHalf, 0);
            var outFlow = new RenderTexture(tempSize, tempSize, 0, RenderTextureFormat.ARGBHalf, 0);
            waterMap.enableRandomWrite = true;
            outFlow.enableRandomWrite = true;
            RenderTextureDescriptor desc = new RenderTextureDescriptor(tempSize, tempSize, RenderTextureFormat.R8, 0);
            desc.enableRandomWrite = true;
            desc.useMipMap = false;
            var velocityMap = RenderTexture.GetTemporary(desc);
            velocityMap.enableRandomWrite = true;

            RenderTexture.active = waterMap;
            GL.Clear(false, true, new Color(initialWaterLevel, 0, 0 , 0));
            
            int kernal0 = flowShader.FindKernel("CSComputeOutflow");
            int kernal1 = flowShader.FindKernel("CSUpdateWater");
            int kernal2 = flowShader.FindKernel("CSVelocityField");

            int threadSize = 16;
            int dispatchSizeX = Mathf.CeilToInt(tempSize / threadSize);
            int dispatchSizeY = Mathf.CeilToInt(tempSize / threadSize);

            flowShader.SetInt(_Width, tempSize);
            flowShader.SetInt(_Height, tempSize);
            flowShader.SetTexture(kernal0, _WaterMap, waterMap);
            flowShader.SetTexture(kernal0, _OutFlow, outFlow);
            flowShader.SetTexture(kernal0, _HeightMap, heightMap);
            flowShader.SetTexture(kernal0, _VelocityMap, velocityMap);

            flowShader.SetTexture(kernal1, _WaterMap, waterMap);
            flowShader.SetTexture(kernal1, _OutFlow, outFlow);
            flowShader.SetTexture(kernal1, _VelocityMap, velocityMap);

            for (int i = 0; i < passes; i++)
            {
                flowShader.Dispatch(kernal0, dispatchSizeX, dispatchSizeY, 1);
                flowShader.Dispatch(kernal1, dispatchSizeX, dispatchSizeY, 1);
            }

            flowShader.SetTexture(kernal2, _OutFlow, outFlow);
            flowShader.SetTexture(kernal2, _VelocityMap, velocityMap);
            flowShader.Dispatch(kernal2, dispatchSizeX, dispatchSizeY, 1);
            RenderTexture.active = null;
            GameObject.DestroyImmediate(waterMap);
            GameObject.DestroyImmediate(outFlow);
            RenderTexture.ReleaseTemporary(heightMap);
            Profiler.EndSample();

            desc.width = outputSize;
            desc.height = outputSize;
            var velocityMap2 = RenderTexture.GetTemporary(desc);
            
            Graphics.CopyTexture(velocityMap, 0, 0, scaledPadding, scaledPadding, outputSize, outputSize, velocityMap2, 0, 0, 0, 0);

            RenderTexture.ReleaseTemporary(velocityMap);

            return velocityMap2;
        }


        static Shader normalShader = null;
        static int _Heightmap = Shader.PropertyToID("_Heightmap");
        static int _Heightmap_PX = Shader.PropertyToID("_Heightmap_PX");
        static int _Heightmap_PY = Shader.PropertyToID("_Heightmap_PY");
        static int _Heightmap_NX = Shader.PropertyToID("_Heightmap_NX");
        static int _Heightmap_NY = Shader.PropertyToID("_Heightmap_NY");
        // because Unity defers normal map generation until it's too late
        public static RenderTexture GenerateNormalMap(Terrain t, Dictionary<Terrain, RenderTexture> heightMaps, int width, int height)
        {
            Profiler.BeginSample("Generate Normal");
            if (normalShader == null)
            {
                normalShader = Shader.Find("Hidden/MicroVerse/NormalMapGen");
            }
            var material = new Material(normalShader);
            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0);
            desc.useMipMap = true;
            var rt = RenderTexture.GetTemporary(desc);
            rt.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = rt;
            material.SetTexture(_Heightmap, heightMaps[t]);
            if (t.rightNeighbor && heightMaps.ContainsKey(t.rightNeighbor))
            { 
                material.SetTexture(_Heightmap_PX, heightMaps[t.rightNeighbor]);
                material.SetKeyword(new UnityEngine.Rendering.LocalKeyword(material.shader, "_PX"), true);
            }
            if (t.topNeighbor && heightMaps.ContainsKey(t.topNeighbor))
            {
                material.SetTexture(_Heightmap_PY, heightMaps[t.topNeighbor]);
                material.SetKeyword(new UnityEngine.Rendering.LocalKeyword(material.shader, "_PY"), true);
            }

            if (t.leftNeighbor && heightMaps.ContainsKey(t.leftNeighbor))
            {
                material.SetTexture(_Heightmap_NX, heightMaps[t.leftNeighbor]);
                material.EnableKeyword("_NX");
            }

            if (t.bottomNeighbor && heightMaps.ContainsKey(t.bottomNeighbor))
            {
                material.SetTexture(_Heightmap_NY, heightMaps[t.bottomNeighbor]);
                material.EnableKeyword("_NY");
            }

            Graphics.Blit(null, rt, material);
            GameObject.DestroyImmediate(material);
            Profiler.EndSample();
            return rt;
        }
    }
}
