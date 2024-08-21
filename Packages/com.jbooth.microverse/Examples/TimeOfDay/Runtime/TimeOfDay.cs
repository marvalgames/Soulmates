using JBooth.MicroVerseCore.Browser;
using UnityEngine;
#if USING_HDRP || USING_URP
namespace JBooth.MicroVerseCore.Demo.TimeOfDay
{
    /// <summary>
    /// Example about how to execute an action of a prefab when it gets dragged in from the content browser.
    /// If no other instance of the prefab exists, the prefab will be created as gameobject in the hierarchy.
    /// If there are already instances of this prefab, the settings of the content browser instance will be applied,
    /// after that the prefab will be destoryed leaving only the existing one.
    /// </summary>
    [ExecuteInEditMode]
    public class TimeOfDay : LightAnchor, IContentBrowserDropAction
    {
        /// <summary>
        /// Update directional light settings
        /// </summary>
        void Update()
        {
            if (Application.isPlaying)
                return;

            // get the direcitonal light
            Light directionalLight = RenderSettings.sun;

            // apply the transform's azimuth & elevation data (ie rotation) to the directional light
            directionalLight.transform.rotation = transform.rotation;
        }

        #region ContentBrowser
        /// <summary>
        /// Execute an action after this prefab was dropped into the scene from the content browser
        /// </summary>
        /// <param name="destroyAfterExecute"></param>
        public void Execute(out bool destroyAfterExecute)
        {
            // assuming this is the only gameobject of the type in the hierarchy
            destroyAfterExecute = false;

            // find all of type (including self)
            TimeOfDay[] timeOfDays = GameObject.FindObjectsByType<TimeOfDay>(FindObjectsSortMode.None);

            foreach (TimeOfDay timeOfDay in timeOfDays)
            {
                // exclude self
                if (timeOfDay.transform == this.transform)
                    continue;

                // Debug.Log($"Prefab {timeOfDay.name} exists, applying settings to it");

                // apply data: set azimuth & elevation
                timeOfDay.transform.rotation = transform.rotation;

                // the gameobject already existed, destroy this one
                destroyAfterExecute = true;
            }
        }
        #endregion ContentBrowser
    }
}
#endif