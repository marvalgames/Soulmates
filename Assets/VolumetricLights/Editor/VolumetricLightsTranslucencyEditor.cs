using UnityEngine;
using UnityEditor;

namespace VolumetricLights
{

    [CustomEditor(typeof(VolumetricLightsTranslucency))]
    public class VolumetricLightsTranslucencyEditor : Editor
    {

        SerializedProperty preserveOriginalShader;
        SerializedProperty intensityMultiplier;
        
        private void OnEnable()
        {
            preserveOriginalShader = serializedObject.FindProperty("preserveOriginalShader");
            intensityMultiplier = serializedObject.FindProperty("intensityMultiplier");
        }

        public override void OnInspectorGUI() {

            serializedObject.Update();
            EditorGUILayout.PropertyField(preserveOriginalShader);
            if (!preserveOriginalShader.boolValue)
            {
                EditorGUILayout.PropertyField(intensityMultiplier);
            }
            serializedObject.ApplyModifiedProperties();
        }

    }
}