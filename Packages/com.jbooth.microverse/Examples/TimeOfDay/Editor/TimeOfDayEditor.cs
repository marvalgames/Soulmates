using UnityEditor;
using UnityEngine;
#if USING_HDRP || USING_URP
namespace JBooth.MicroVerseCore.Demo.TimeOfDay
{
    [CustomEditor(typeof(TimeOfDay))]

    public class TimeOfDayEditor : LightAnchorEditor
    {
        private static GUIContent helpContent = new GUIContent("This is a wrapper for Unity's own Light Anchor class. The visuals depend on the render pipeline, your render end light settings." +
            "This script maps the transform to the directional light's transform. Otherwise Unity's Light Anchor needs to be a component inside the directional light."
            );

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(helpContent.text, MessageType.Info);

            base.OnInspectorGUI();
        }
    }
}
#endif