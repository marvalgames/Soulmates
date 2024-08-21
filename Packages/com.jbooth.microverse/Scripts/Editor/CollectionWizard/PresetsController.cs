using JBooth.MicroVerseCore;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using static JBooth.MicroVerseCore.Browser.CollectionWizard.BrowserToolsData;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    public class PresetsController
    {
        private BrowserToolsModule editor;

        private SerializedProperty basePath;
        private SerializedProperty author;
        private SerializedProperty packName;
        private SerializedProperty id;
        private SerializedProperty image;
        private SerializedProperty downloadPath;
        private SerializedProperty requireInstalledObject;
        private SerializedProperty installedObject;

        private Vector2 scrollPosition = Vector2.zero;

        private Dictionary<ContentType, bool> contentTypeSelection = new Dictionary<ContentType, bool>();

        public PresetsController(BrowserToolsModule editor)
        {
            this.editor = editor;

            SerializedProperty settings = editor.GetSerializedObject().FindProperty("presetsSettings");

            basePath = settings.FindPropertyRelative("basePath");
            author = settings.FindPropertyRelative("author");
            packName = settings.FindPropertyRelative("packName");
            id = settings.FindPropertyRelative("id");
            image = settings.FindPropertyRelative("image");
            downloadPath = settings.FindPropertyRelative("downloadPath");
            requireInstalledObject = settings.FindPropertyRelative("requireInstalledObject");
            installedObject = settings.FindPropertyRelative("installedObject");

            InitContentTypeSelection();
        }

        private void InitContentTypeSelection()
        {
            contentTypeSelection[ContentType.Height] = false;
            contentTypeSelection[ContentType.Texture] = true;
            contentTypeSelection[ContentType.Vegetation] = true;
            contentTypeSelection[ContentType.Objects] = true;
            contentTypeSelection[ContentType.Audio] = false;
            contentTypeSelection[ContentType.Biomes] = true;
            contentTypeSelection[ContentType.Roads] = false;
            contentTypeSelection[ContentType.Caves] = false;
            contentTypeSelection[ContentType.Global] = false;
        }
        public void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Collection Settings", GUIStyles.BoxTitleStyle);

            EditorGUILayout.HelpBox("Create Ad and Preset Content Collection files for the MicroVerse Content Browser", MessageType.None);

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if( GUILayout.Button("Reset", EditorStyles.miniButton))
                    {
                        editor.GetEditorTarget().Reset();
                        InitContentTypeSelection();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Folder", EditorStyles.miniBoldLabel);

                GUIUtils.AssetPathSelector(basePath, "Base Path");

                EditorGUILayout.HelpBox("The path must exist.", MessageType.None);

            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Content Types", EditorStyles.miniBoldLabel);

                ContentType[] contentTypes = Enum.GetValues(typeof(ContentType)) as ContentType[];
                foreach( ContentType contentType in contentTypes)
                {
                    bool value = false;
                    contentTypeSelection.TryGetValue(contentType, out value);

                    bool newValue = EditorGUILayout.Toggle(contentType.ToString(), value);

                    if (newValue != value)
                    {
                        contentTypeSelection[contentType] = newValue;
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Content Data", EditorStyles.miniBoldLabel);

                EditorGUILayout.PropertyField(author);
                EditorGUILayout.PropertyField(packName);
                EditorGUILayout.PropertyField(id);
                EditorGUILayout.PropertyField(image);
                EditorGUILayout.PropertyField(downloadPath);
                EditorGUILayout.PropertyField(requireInstalledObject);
                EditorGUILayout.PropertyField(installedObject);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button("Create Collection", GUILayout.Height(GUIStyles.BigButtonSize)))
            {
                CreateCollections();
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool ValidateSettings()
        {
            if (!RequireTextField(author))
                return false;

            if (!RequireTextField(packName))
                return false;

            if (!RequireTextField(id))
                return false;

            if (!RequireTextField(downloadPath))
                return false;

            if (!RequireFolder(basePath))
                return false;

            return true;
        }

        private bool RequireTextField( SerializedProperty property)
        {
            if (string.IsNullOrEmpty(property.stringValue))
            {
                EditorUtility.DisplayDialog("Error", $"Data required: {property.displayName}", "Ok");
                return false;
            }

            return true;
        }

        private bool RequireFolder(SerializedProperty property)
        {
            if (!AssetDatabase.IsValidFolder( property.stringValue))
            {
                EditorUtility.DisplayDialog("Error", $"Folder of {property.displayName} must exist: {property.stringValue}", "Ok");
                return false;
            }

            return true;
        }

        private void CreateCollections()
        {
            if (!ValidateSettings())
                return;

            PresetsSettings template = editor.GetEditorTarget().presetsSettings;

            ContentType[] contentTypes = Enum.GetValues(typeof(ContentType)) as ContentType[];

            int dialogOk = 0;
            // int dialogNo = 1;
            int dialogCancel = 2;

            // create ads
            foreach (ContentType contentType in contentTypes)
            {
                bool selected = contentTypeSelection[contentType] == true;
                if (!selected)
                    continue;

                ContentAd contentAd = ScriptableObject.CreateInstance<ContentAd>();

                contentAd.contentType = contentType;

                // apply template
                contentAd.author = template.author;
                contentAd.packName = template.packName;
                contentAd.id = template.id;
                contentAd.image = template.image;
                contentAd.downloadPath = template.downloadPath;
                contentAd.requireInstalledObject = template.requireInstalledObject;
                contentAd.installedObject = template.installedObject;

                string assetName = template.packName + "-" + "Ad" + "-" + contentType.ToString() + ".asset";
                string assetPath = Path.Combine(template.basePath, assetName); // eg Assets/<collectioname>/Forest Environment-Ad-Biomes.asset

                // create (or overwrite) as default
                int dialogResult = dialogOk;

                // check if asset exists
                if ( AssetDatabase.LoadAssetAtPath( assetPath, typeof( ContentAd)))
                {
                    dialogResult = EditorUtility.DisplayDialogComplex("Confirm", $"File exists: {assetPath}. Overwrite?", "Yes", "No", "Cancel");

                    // user pressed cancel
                    if (dialogResult == dialogCancel)
                        return;
                }

                if (dialogResult == dialogOk)
                {
                    UnityEditor.AssetDatabase.CreateAsset(contentAd, assetPath);
                }

            }

            // create content collections
            foreach (ContentType contentType in contentTypes)
            {
                bool selected = contentTypeSelection[contentType] == true;
                if (!selected)
                    continue;

                ContentCollection contentCollection = ScriptableObject.CreateInstance<ContentCollection>();

                contentCollection.contentType = contentType;

                // apply template
                contentCollection.author = template.author;
                contentCollection.packName = template.packName;
                contentCollection.id = template.id;

                string assetName = template.packName + "-" + contentType.ToString() + ".asset";
                string assetPath = Path.Combine(template.basePath, assetName); // eg Assets/<collectioname>/Forest Environment-Biomes.asset

                // create (or overwrite) as default
                int dialogResult = dialogOk;

                // check if asset exists
                if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(ContentCollection)))
                {
                    dialogResult = EditorUtility.DisplayDialogComplex("Confirm", $"File exists: {assetPath}. Overwrite?", "Yes", "No", "Cancel");

                    // user pressed cancel
                    if (dialogResult == dialogCancel)
                        return;
                }

                if (dialogResult == dialogOk)
                {
                    UnityEditor.AssetDatabase.CreateAsset(contentCollection, assetPath);
                }

            }

            // collect paths to create
            // this is actually not required anymore, code got moved to the content browser so that paths are created as needed
            /*
            string prefabsPath = "Prefabs";
             
            AssetDatabase.CreateFolder(template.basePath, prefabsPath); // Assets/<collectioname>/Prefabs
            string prefabsPath = Path.Combine(template.basePath, prefabsPath);
            Debug.Log("Created " + prefabsPath);

            foreach (ContentType contentType in contentTypes)
            {
                AssetDatabase.CreateFolder(prefabsPath, contentType.ToString()); // eg Assets/<collectioname>/Prefabs/Biomes
            }
            */

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }

}