using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [CreateAssetMenu(fileName = "Ad", menuName = "MicroVerse/ContentAd")]
    public class ContentAd : BrowserContent
    {
        [Tooltip("Image to display for add")]
        public Texture2D image;
        [Tooltip("HTTP path to go to when clicked")]
        public string downloadPath;
        [Tooltip("If true, the reference for installedObject will be used to determine if the package is installed instead of the id- and when it's not null, the ad will be hidden and content displayed")]
        public bool requireInstalledObject;
        [Tooltip("Set a reference to any object in the external package")]
        public Object installedObject;
        [Tooltip("Description of asset to display in the browser")]
        public string description;
    }

}