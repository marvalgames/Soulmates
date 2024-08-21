using System;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    public class BrowserToolsData : ScriptableObject
    {
        public enum Module
        {
            Presets,
        }

        /// <summary>
        /// Show help box for information
        /// </summary>
        public bool helpBoxVisible = false;

        public Module module = Module.Presets;

        public PresetsSettings presetsSettings = new PresetsSettings();

        [Serializable]
        public class PresetsSettings
        {
            [Tooltip("The path in which the files are created. The path must exist")]
            public string basePath = "";

            [Tooltip("The author of the package")]
            public string author = "";

            [Tooltip("The name of the package, usually the asset name. Also used for grouping in the Content Browser")]
            public string packName = "";

            [Tooltip("Unique identifier of the asset. Preferrably use the asset url on the Unity Asset Store")]
            public string id = "";

            [Tooltip("The ad image. Use the card image dimensions which are 420 x 280")]
            public Texture2D image;

            [Tooltip("The url of the asset on the Unity Asset Store")]
            public string downloadPath = "";

            [Tooltip("Use auto-detection if the asset is installed or not")]
            public bool requireInstalledObject;

            [Tooltip("If this object is installed, then the asset is considered installed. Use an object of the asset to b eused")]
            public UnityEngine.Object installedObject;
        }

        public void Reset()
        {
            presetsSettings = new PresetsSettings();
        }
    }
}