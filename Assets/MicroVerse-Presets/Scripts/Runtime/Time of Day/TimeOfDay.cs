#if USING_HDRP
using UnityEngine;

namespace Rowlan.MicroVerse.Presets
{
    /// <summary>
    /// Wrapper for LightAnchor which binds it to the directional light in the scene
    /// </summary>
    [ExecuteInEditMode]
    public class TimeOfDay : LightAnchor
    {
        void Update()
        {
            // currently an editor only feature
            if (Application.isPlaying)
                return;

            // map the current transform to the light
            Light directionalLight = RenderSettings.sun;
            directionalLight.transform.rotation = transform.rotation;
        }
    }
}
#endif