using UnityEditor;
using UnityEngine;
namespace JBooth.MicroVerseCore.Browser
{
    [CustomEditor(typeof(PresetInstance))]

    public class PresetInstanceEditor : Editor
    {
        private static GUIContent helpContent = new GUIContent("Marker for a prefab that's coming from the Content Browser. Existing presets will be replaced with new ones");

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(helpContent.text, MessageType.Info);

            base.OnInspectorGUI();
        }
    }
}