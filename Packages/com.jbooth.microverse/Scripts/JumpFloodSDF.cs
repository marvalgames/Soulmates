using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace JBooth.MicroVerseCore
{
    public class JumpFloodSDF
    {
        public static RenderTexture CreateTemporaryRT(Texture source, int channel = 0, float zoom = 1, int downscale = 1, bool r8 = false)
        {
            if (source == null)
                return null;

            int w = (int)(source.width / zoom);
            int h = (int)(source.height / zoom);
            w /= downscale;
            h /= downscale;
            RenderTexture output;
            if (r8)
            {
                output = RenderTexture.GetTemporary(w, h, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm);
            }
            else
            {
                output = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.RHalf);
            }
            
            return Generate(source, output, channel, zoom, downscale);
        }
        static Shader jumpFloodShader = null;
        static RenderTexture Generate(Texture source, RenderTexture output, int channel, float zoom, int downscale)
        {
            if (jumpFloodShader == null)
            {
                jumpFloodShader = Shader.Find("Hidden/MicroVerse/JumpFloodSDF");
            }
            Material mat = new Material(jumpFloodShader);
            mat.SetInt("_Channel", channel);

            RenderTexture rtA = RenderTexture.GetTemporary(source.height, source.width, 0, RenderTextureFormat.RGHalf);
            RenderTexture rtB = RenderTexture.GetTemporary(source.height, source.width, 0, RenderTextureFormat.RGHalf);
            Graphics.Blit(source, rtA, mat, 0);
            int numMips = 8;
            int jfaIter = numMips - 1;

            for (int i = jfaIter; i >= 0; i--)
            {
                mat.SetFloat("_StepWidth", Mathf.Pow(2, i) + 0.5f);
                Graphics.Blit(rtA, rtB, mat, 1);
                (rtA, rtB) = (rtB, rtA);
            }


            mat.SetFloat("_Zoom", zoom);
            Graphics.Blit(rtA, output, mat, 2);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rtA);
            RenderTexture.ReleaseTemporary(rtB);
            return output;
        }
    }
}

