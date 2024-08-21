using System.Collections;
using System.Collections.Generic;
using JBooth.MicroVerseCore;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    public class OcclusionData : StampData
    {
        public RenderTexture terrainMask; // collective mask for all of terrain
        public RenderTexture treeSDF;     // collective sdf for all trees
        public RenderTexture currentTreeMask; // buffer for current tree stamps mask
        public RenderTexture currentTreeSDF;
        public RenderTexture objectSDF;
        public RenderTexture currentObjectMask;
        public RenderTexture currentObjectSDF;

        public RenderTexture objectMask;

        public OcclusionData(Terrain terrain, int maskSize) : base(terrain)
        {
            this.terrain = terrain;
            RenderTextureDescriptor desc = new RenderTextureDescriptor(maskSize, maskSize, RenderTextureFormat.ARGB32, 0, 0);
            desc.enableRandomWrite = true;
            desc.autoGenerateMips = false;
            terrainMask = RenderTexture.GetTemporary(desc);
            terrainMask.name = "OcclusionData::mask";
            terrainMask.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = terrainMask;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = null;

            desc = new RenderTextureDescriptor(maskSize, maskSize, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, 0, 0);
            objectMask = RenderTexture.GetTemporary(desc);
            objectMask.name = "OcclusionData::mask";
            objectMask.wrapMode = TextureWrapMode.Clamp;
            RenderTexture.active = objectMask;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = null;

        }

        static Shader combineSDFShader = null;
        public void RenderTreeSDF(Terrain t, Dictionary<Terrain, OcclusionData> ods, bool others)
        {
            // we have to blend across edges, so we make the render texture target larger
            // than the mask data, blit all nine into a single texture, sdf it, then
            // get the middle bit back out, then merge with cumulative texture
            if (!ods.ContainsKey(t))
            {
                return;
            }
            var myMask = ods[t].currentTreeMask;
            if (myMask == null)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("Render Tree SDF");
            int expand = (int)(myMask.width * 0.25f);

            var expandedRT = MapGen.NineCombineCurrentTreeMask(t, ods, expand);

            UnityEngine.Profiling.Profiler.BeginSample("JumpFloodSDF");
            if (currentTreeSDF != null)
            {
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(currentTreeSDF);
            }
            currentTreeSDF = JumpFloodSDF.CreateTemporaryRT(expandedRT, 0, 1.25f, 2);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(expandedRT);
            UnityEngine.Profiling.Profiler.EndSample();
            if (others)
            {
                UnityEngine.Profiling.Profiler.BeginSample("CombineSDF");
                if (combineSDFShader == null)
                {
                    combineSDFShader = Shader.Find("Hidden/MicroVerse/CombineSDF");
                }
                Material mat = new Material(combineSDFShader);
                RenderTexture rt = RenderTexture.GetTemporary(currentTreeSDF.descriptor);
                rt.name = "MicroVerse::CombinedTreeSDF";
                mat.SetTexture("_SourceA", currentTreeSDF);
                mat.SetTexture("_SourceB", treeSDF);
                Graphics.Blit(null, rt, mat);
                if (treeSDF != null)
                {
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(treeSDF);
                }
                treeSDF = rt;
                UnityEngine.Profiling.Profiler.EndSample();
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void RenderObjectSDF(Terrain t, Dictionary<Terrain, OcclusionData> ods, bool others)
        {
            // we have to blend across edges, so we make the render texture target larger
            // than the mask data, blit all nine into a single texture, sdf it, then
            // get the middle bit back out, then merge with cumulative texture
            if (!ods.ContainsKey(t))
            {
                return;
            }
            var myMask = ods[t].currentObjectMask;
            if (myMask == null)
                return;

            int expand = (int)(myMask.width * 0.25f);

            var expandedRT = MapGen.NineCombineCurrentObjectMask(t, ods, expand);

            currentObjectSDF = JumpFloodSDF.CreateTemporaryRT(expandedRT, 0, 1.25f, 2);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(expandedRT);

            if (others)
            {
                if (combineSDFShader == null)
                {
                    combineSDFShader = Shader.Find("Hidden/MicroVerse/CombineSDF");
                }
                var mat = new Material(combineSDFShader);
                RenderTexture rt = RenderTexture.GetTemporary(currentObjectSDF.descriptor);
                rt.name = "MicroVerse::CombinedObjectSDF";
                mat.SetTexture("_SourceA", currentObjectSDF);
                mat.SetTexture("_SourceB", objectSDF);
                Graphics.Blit(null, rt, mat);
                if (objectSDF != null)
                {
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(objectSDF);
                }
                objectSDF = rt;
            }
        }

        public void Dispose()
        {
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(terrainMask);
            terrainMask = null;
            RenderTexture.ReleaseTemporary(objectMask);
            objectMask = null;

            if (treeSDF != null)
            {
                RenderTexture.ReleaseTemporary(treeSDF);
            }
            if (currentTreeMask != null)
            {
                RenderTexture.ReleaseTemporary(currentTreeMask);
            }
            if (currentTreeSDF != null)
            {
                RenderTexture.ReleaseTemporary(currentTreeSDF);
            }
            if (objectSDF != null)
            {
                RenderTexture.ReleaseTemporary(objectSDF);
            }
            if (currentObjectMask != null)
            {
                RenderTexture.ReleaseTemporary(currentObjectMask);
            }
            if (currentObjectSDF != null)
            {
                RenderTexture.ReleaseTemporary(currentObjectSDF);
            }

            currentTreeMask = null;
            treeSDF = null;
            currentTreeSDF = null;
            objectSDF = null;
            currentObjectMask = null;
            currentObjectSDF = null;
        }
    }
}
