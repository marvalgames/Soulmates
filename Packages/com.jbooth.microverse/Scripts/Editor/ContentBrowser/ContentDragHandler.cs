using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    public class ContentDragHandler
    {
        private ContentBrowser browser;

        public ContentDragHandler(ContentBrowser browser)
        {
            this.browser = browser;
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }


        public void OnGUI()
        {
            if (ContentBrowser.placementMode == ContentBrowser.PlacementMode.Place)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    bool accepted = IsDragAccepted();

                    if (accepted)
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    else
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    bool dragPerformed = TryDragPerform();

                    if (dragPerformed)
                    {
                        Event.current.Use();
                    }
                }
            }
        }

        /// <summary>
        /// Accept objects from hierarchy
        /// </summary>
        /// <returns></returns>
        private bool IsDragAccepted()
        {
            bool accepted = DragAndDrop.paths.Length == 0 && DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject;
            return accepted;

        }

        private bool TryDragPerform()
        {
            bool accepted = IsDragAccepted();

            if (!accepted)
                return false;

            // effectively accept the drop operation
            DragAndDrop.AcceptDrag();

            BrowserContent contentAsset = browser.GetSelectedBrowserContentAsset();

            if (contentAsset == null)
            {
                Debug.LogError("Content asset not found");
                return false;
            }

            ContentCollection collection = contentAsset as ContentCollection;

            string outputPath = EnsureFolderStructureExists(contentAsset, "Prefabs", collection.contentType.ToString());

            List<ContentData> contendDataList = collection.contents == null ? new List<ContentData>() : collection.contents.ToList();

            foreach (Object obj in DragAndDrop.objectReferences)
            {
                // output path
                string prefabPath = Path.Combine(outputPath, obj.name + ".prefab");

                // check if prefab exists and confirm overwrite
                Object prefabCheckObject = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
                if (prefabCheckObject != null)
                {
                    bool overwrite = EditorUtility.DisplayDialog("Overwrite", $"Prefab with name '{obj.name}' exsists:\n\n{prefabPath}\n\nOverwrite?", "Yes", "No");
                    if (!overwrite)
                        continue;
                }

                // create a gameobject instance
                GameObject clone = Object.Instantiate( obj as GameObject);

                if( PrefabUtility.IsAnyPrefabInstanceRoot(clone))
                {
                    Debug.LogError("Prefab detected. Only completely prefab instances supported, otherwise users will get errors when they import the preset while the assets themselves aren't installed");
                    continue;
                }

                // set name
                clone.name = obj.name;

                // reset transform except scale
                clone.transform.localPosition = Vector3.zero;
                clone.transform.rotation = Quaternion.identity;

                Debug.Log($"Creating prefab: {prefabPath}");

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);

                ContentData data = new ContentData();
                data.prefab = prefab;

                // replace prefab if it exists
                bool found = false;
                foreach (ContentData contentData in contendDataList)
                {
                    if (contentData.prefab != null && contentData.prefab.name == prefab.name)
                    {
                        Debug.Log($"Prefab name {prefab.name} exists. Replacing");

                        contentData.prefab = prefab;
                        found = true;
                    }
                }

                // prefab not registered yet, register as new
                if( !found)
                {
                    contendDataList.Add(data);
                }

                Object.DestroyImmediate(clone);
            }


            collection.contents = contendDataList.ToArray();

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssetIfDirty(collection);

            AssetDatabase.Refresh();

            return true;
        }

        private bool PrefabNameExists(ContentData newContent, List<ContentData> contendDataList)
        {
            foreach( ContentData data in contendDataList)
            {
                if( data.prefab != null && data.prefab.name == newContent.prefab.name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ensure the folder structure exists, if necessary create it
        /// </summary>
        /// <param name="assetObject"></param>
        /// <param name="subFolders"></param>
        /// <returns></returns>
        private string EnsureFolderStructureExists(BrowserContent assetObject, params string[] subFolders)
        {
            string path = AssetDatabase.GetAssetPath(assetObject);
            path = Path.GetDirectoryName(path);

            foreach (var folder in subFolders)
            {
                string currentPath = path;

                path = Path.Combine(path, folder);

                if (AssetDatabase.IsValidFolder(path)) 
                    continue;

                AssetDatabase.CreateFolder(currentPath, folder);
            }

            return path;
        }
    }
}