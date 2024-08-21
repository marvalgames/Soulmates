using System.IO;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    public class ThumbnailCreator
    {
        private static int previewIconSize = 128;

        public static void SaveIcon(PresetItem presetItem, bool confirmOverwrite = true)
        {
            if (presetItem == null)
                return;

            SaveIcon(presetItem.collection, presetItem.collectionIndex, confirmOverwrite);
        }

        public static void SaveIcon(ContentCollection selectedCollection, int collectionIndex, bool confirmOverwrite = true)
        {

            ContentData item = selectedCollection.contents[collectionIndex];

            if (item.prefab == null)
            {
                Debug.LogError("Prefab missing");
                return;
            }

            // create image
            GameObject prefab = item.prefab;
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            string directory = Path.GetDirectoryName(prefabPath);
            string fileName = prefab.name;

            Texture2D texture = CaptureSceneView.Capture(previewIconSize, previewIconSize);

            string imageOutputPath;

            bool isTextureSaved = SaveTexture(directory, fileName, texture, confirmOverwrite, out imageOutputPath);

            Texture2D.DestroyImmediate(texture);
            texture = null;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (isTextureSaved)
            {

                // update browser content
                string collectionPath = AssetDatabase.GetAssetPath(selectedCollection);

                UnityEngine.Object collectionObject = AssetDatabase.LoadAssetAtPath<ContentCollection>(collectionPath);
                ContentCollection contentCollection = collectionObject as ContentCollection;

                SerializedObject contentCollectionSerializedObject = new SerializedObject(contentCollection);

                Texture2D previewImage = AssetDatabase.LoadAssetAtPath(imageOutputPath, typeof(Texture2D)) as Texture2D;

                contentCollection.contents[collectionIndex].previewImage = previewImage;

                EditorUtility.SetDirty(contentCollection);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }



        }

        private static bool SaveTexture(string path, string fileName, Texture2D texture, bool confirmOverwrite, out string outputPath)
        {

            outputPath = path + Path.DirectorySeparatorChar + fileName + ".png";

            // overwrite check
            if (confirmOverwrite)
            {
                bool exists = File.Exists(outputPath);

                if (exists)
                {
                    bool isOverwrite = EditorUtility.DisplayDialog($"Overwrite File", "File exists:\n\n" + outputPath + "\n\nOverwrite?", "Yes", "No");

                    if (!isOverwrite)
                        return false;
                }
            }

            Debug.Log("Saving: " + outputPath);

            byte[] bytes = ImageConversion.EncodeToPNG(texture);

            System.IO.File.WriteAllBytes(outputPath, bytes);

            return true;

        }
    }
}