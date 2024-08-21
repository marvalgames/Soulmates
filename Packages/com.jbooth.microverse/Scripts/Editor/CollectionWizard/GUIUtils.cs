using System.Collections;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    public class GUIUtils
    {
        /// <summary>
        /// Show property for asset path and a selection button for the asset path selection
        /// </summary>
        /// <param name="property"></param>
        /// <param name="title"></param>
        public static void AssetPathSelector(SerializedProperty property, string title)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(property);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    string selectedPath = SelectAssetPath(title);
                    if (selectedPath != null)
                    {
                        property.stringValue = selectedPath;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public static string AssetPathSelector( string path, string title)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(title);
                path = EditorGUILayout.TextField(path);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    string selectedPath = SelectAssetPath(title);
                    if (selectedPath != null)
                    {
                        path = selectedPath;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            return path;
        }

        /// <summary>
        /// Open folder dialog and have user select an asset path
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string SelectAssetPath(string title)
        {
            string selectedPath = EditorUtility.OpenFolderPanel(title, "Assets", "");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                string assetPath = Application.dataPath.Substring("Assets/".Length);
                bool isAssetPath = selectedPath.StartsWith(Application.dataPath);
                if (isAssetPath)
                {
                    string newPath = selectedPath.Substring(assetPath.Length + 1); // +1 for the initial path separator
                    return newPath;
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Path must be in the Assets folder", "Ok");
                }
            }

            return null;
        }

        public static void DrawHeader( string title, ref bool helpBoxVisible)
        {
            /// 
            /// Info & Help
            /// 
            GUILayout.BeginVertical(GUIStyles.HelpBoxStyle);
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Asset Store", EditorStyles.miniButton, GUILayout.Width(120)))
                {
                    Application.OpenURL("https://u3d.as/2Uxf");
                }

                if (GUILayout.Button("Documentation", EditorStyles.miniButton))
                {
                    Application.OpenURL("https://bit.ly/genesis-for-microverse-doc");
                }

                if (GUILayout.Button("Forum", EditorStyles.miniButton, GUILayout.Width(120)))
                {
                    Application.OpenURL("https://forum.unity.com/threads/released-genesis-for-microverse.1354952");
                }

                if (GUILayout.Button(new GUIContent("?", "Help box visibility"), EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    // toggle help box visibility
                    helpBoxVisible = !helpBoxVisible;
                }

                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUIStyles.AppTitleBoxStyle);
            {
                EditorGUILayout.LabelField( title, GUIStyles.AppTitleBoxStyle, GUILayout.Height(30));
            }
            GUILayout.EndVertical();
        }
    }
}