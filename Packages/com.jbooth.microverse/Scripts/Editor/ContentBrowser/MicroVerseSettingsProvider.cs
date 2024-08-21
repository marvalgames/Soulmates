using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    public class MicroVerseSettingsProvider : SettingsProvider
    {
        const string k_menu = "JBooth/MicroVerse";
        const SettingsScope k_scope = SettingsScope.User;

        // registry keys
        const string k_optionalVisible = "JBooth.MicroVerse.ContentBrowser.OptionalVisible";
        const string k_descriptionVisible = "JBooth.MicroVerse.ContentBrowser.DescriptionVisible";
        const string k_helpVisible = "JBooth.MicroVerse.ContentBrowser.HelpVisible";

        public static bool OptionalVisible
        {
            get { return EditorPrefs.GetBool(k_optionalVisible, true); }
            set { EditorPrefs.SetBool(k_optionalVisible, value); }
        }

        public static bool DescriptionVisible
        {
            get { return EditorPrefs.GetBool(k_descriptionVisible, true); }
            set { EditorPrefs.SetBool(k_descriptionVisible, value); }
        }

        public static bool HelpVisible
        {
            get { return EditorPrefs.GetBool(k_helpVisible, true); }
            set { EditorPrefs.SetBool(k_helpVisible, value); }
        }

        public MicroVerseSettingsProvider(string menuPath, SettingsScope scope) : base(menuPath, scope)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            // reset button
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset"))
                {
                    OptionalVisible = true;
                    DescriptionVisible = true;
                    HelpVisible = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            // content browser
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField("Content Browser", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                bool optionalVisibleValue = EditorGUILayout.Toggle("Optional Visible", OptionalVisible);
                bool descriptionVisibleValue = EditorGUILayout.Toggle("Description Visible", DescriptionVisible);
                bool helpVisibleValue = EditorGUILayout.Toggle("Help Visible", HelpVisible);

                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();

                if (check.changed)
                {
                    OptionalVisible = optionalVisibleValue;
                    DescriptionVisible = descriptionVisibleValue;
                    HelpVisible = helpVisibleValue;
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new MicroVerseSettingsProvider(k_menu, k_scope);
        }
    }
}