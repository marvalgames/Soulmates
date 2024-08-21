using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    /// <summary>
    /// Popup menu for renaming a content browser item.
    /// </summary>
    public class PresetItemPopup : PopupWindowContent
    {
        private string newName = "";
        private PresetItem presetItem;

        public PresetItemPopup( PresetItem presetItem)
        {
            this.newName = presetItem.content.prefab.name;
            this.presetItem = presetItem;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(250, 100);
        }

        public override void OnGUI(Rect rect)
        {
            // label
            GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel);
            style.wordWrap = true;

            EditorGUILayout.LabelField("Please enter new name for prefab and thumbnail image:", style);

            EditorGUILayout.Space();

            // name textfield
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50f;
            {
                newName = EditorGUILayout.TextField("Name", newName);
            }
            EditorGUIUtility.labelWidth = prevLabelWidth;

            GUILayout.FlexibleSpace();

            // button bar
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Ok", GUILayout.Width(100)))
                {
                    if (!Validate())
                        return;

                    RenameAsset();

                    editorWindow.Close();
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(100)))
                {
                    editorWindow.Close();
                }

                GUILayout.FlexibleSpace();

            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

        }

        public override void OnOpen()
        {
        }

        public override void OnClose()
        {
        }

        private bool Validate()
        {
            bool valid = !string.IsNullOrEmpty(newName);

            if (!valid)
            {
                EditorUtility.DisplayDialog("Error", $"Invalid name", "Ok");
            }

            return valid;
        }

        private void RenameAsset()
        {
            if (!Validate())
                return;

            if (presetItem.content.prefab != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(presetItem.content.prefab);

                AssetDatabase.RenameAsset(prefabPath, newName);
            }

            if (presetItem.content.previewImage != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(presetItem.content.previewImage);

                AssetDatabase.RenameAsset(prefabPath, newName);
            }

            EditorUtility.SetDirty(presetItem.collection);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}