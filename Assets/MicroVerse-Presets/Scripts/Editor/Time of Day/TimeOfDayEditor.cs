#if USING_HDRP
using UnityEditor;
using UnityEngine;

namespace Rowlan.MicroVerse.Presets
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(TimeOfDay))]
    public class TimeOfDayEditor : LightAnchorEditor
    {
        private static GUIContent helpContent = new GUIContent("This is a wrapper for Unity's own Light Anchor class. It maps the transform to the directional light's transform. Unity's Light Anchor needs to be a direct component in the directional light. It would be nice if they could make this a global setting.");

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(helpContent.text, MessageType.Info);
            base.OnInspectorGUI();
        }
    }
}
#endif