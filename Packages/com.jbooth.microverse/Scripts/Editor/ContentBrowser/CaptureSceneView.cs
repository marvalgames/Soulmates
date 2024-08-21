using System;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    public class CaptureSceneView
    {
        private static bool updateBehaviour = false;

        /// <summary>
        /// Capture a screenshot of the sceneview
        /// </summary>
        public static Texture2D Capture( int width, int height)
        {
            Texture2D texture = null;

            Camera camera = SceneView.lastActiveSceneView.camera;

            // save data which we'll modify
            RenderTexture prevRenderTexture = RenderTexture.active;
            RenderTexture prevCameraTargetTexture = camera.targetTexture;
            bool prevCameraEnabled = camera.enabled;
            float prevFieldOfView = camera.fieldOfView;

            // create rendertexture
            int msaaSamples = 1;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, msaaSamples);

            try
            {
                // make the enabled status optional. enabled = true works for hdrp.
                // it didn't work in older unity versions, we had enabled = false:
                // old info:
                //   disabling the camera is important, otherwise you get e. g. a blurry image with different focus than the one the camera displays
                //   see https://docs.unity3d.com/ScriptReference/Camera.Render.html
                // camera.enabled = false;
                camera.enabled = updateBehaviour;

                // set rendertexture into which the camera renders
                camera.targetTexture = renderTexture;

                // render a single frame
                camera.Render();

                texture = CreateTexture2D(renderTexture);
                
            }
            catch (Exception ex)
            {
                Debug.LogError("Screenshot capture exception: " + ex);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);

                // restore modified data
                RenderTexture.active = prevRenderTexture;
                camera.targetTexture = prevCameraTargetTexture;
                camera.enabled = prevCameraEnabled;
                camera.fieldOfView = prevFieldOfView;

            }

            return texture;

        }

        /// <summary>
        /// Create a Texture2D from a RenderTexture
        /// </summary>
        /// <param name="renderTexture"></param>
        /// <returns></returns>
        private static Texture2D CreateTexture2D(RenderTexture renderTexture)
        {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            RenderTexture prevRT = RenderTexture.active;
            {
                RenderTexture.active = renderTexture;

                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
            }
            RenderTexture.active = prevRT;

            return texture;
        }

        /// <summary>
        /// Create a texture with a random color
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Texture2D CreateRandomColorTexture(int width, int height)
        {
            Color randomColor = UnityEngine.Random.ColorHSV();

            Texture2D texture = new Texture2D(width, height);
            var pixels = texture.GetPixels();

            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = randomColor;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;

        }
    }
}