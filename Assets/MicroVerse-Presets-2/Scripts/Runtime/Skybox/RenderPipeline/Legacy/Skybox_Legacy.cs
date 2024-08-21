using JBooth.MicroVerseCore.Browser;
using UnityEngine;

namespace Rowlan.MicroVerse.Presets_2
{
    [ExecuteInEditMode]
    public class Skybox_Legacy : MonoBehaviour, IContentBrowserDropAction
    {
        public Material skybox;
        public Color realtimeShadowColor;

        public bool fogEnabled;
        public Color fogColor;
        public FogMode fogMode;
        public float density;
        public float fogStartDistance;
        public float fogEndDistance;
                
        public void Execute(out bool destroyAfterExecute)
        {

            Debug.Log($"Adding skybox {skybox} and fog");

            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
                RenderSettings.subtractiveShadowColor = realtimeShadowColor;

                RenderSettings.fog = fogEnabled;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogDensity = density;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;

                DynamicGI.UpdateEnvironment();
            }

            destroyAfterExecute = true;
        }
    }
}
