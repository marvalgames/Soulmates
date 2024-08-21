using UnityEngine;
using UnityEditor;

namespace VolumetricLights
{

    [CustomEditor(typeof(VolumetricLightsRenderFeature))]
    public class RenderFeatureEditor : Editor
    {

        SerializedProperty renderPassEvent;
        SerializedProperty blendMode, brightness, ditherStrength;
        SerializedProperty downscaling, blurPasses, blurDownscaling, blurSpread, blurHDR, blurEdgePreserve, blurEdgeDepthThreshold;

        private void OnEnable()
        {
            renderPassEvent = serializedObject.FindProperty("renderPassEvent");
            blendMode = serializedObject.FindProperty("blendMode");
            brightness = serializedObject.FindProperty("brightness");
            downscaling = serializedObject.FindProperty("downscaling");
            blurPasses = serializedObject.FindProperty("blurPasses");
            blurDownscaling = serializedObject.FindProperty("blurDownscaling");
            blurSpread = serializedObject.FindProperty("blurSpread");
            blurHDR = serializedObject.FindProperty("blurHDR");
            blurEdgePreserve = serializedObject.FindProperty("blurEdgePreserve");
            blurEdgeDepthThreshold = serializedObject.FindProperty("blurEdgeDepthThreshold");
            ditherStrength = serializedObject.FindProperty("ditherStrength");
        }

        public override void OnInspectorGUI() {

            if (VolumetricLightEditor.lastEditingLight != null) {
                if (GUILayout.Button("<< Back To Last Volumetric Light")) {
                    Selection.SetActiveObjectWithContext(VolumetricLightEditor.lastEditingLight, null);
                    GUIUtility.ExitGUI();
                }
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(renderPassEvent);
            EditorGUILayout.PropertyField(downscaling);
            EditorGUILayout.PropertyField(blurPasses);
            if (blurPasses.intValue > 0) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(blurDownscaling);
                EditorGUILayout.PropertyField(blurSpread);
                EditorGUILayout.PropertyField(blurHDR, new GUIContent("HDR"));
                EditorGUILayout.PropertyField(blurEdgePreserve, new GUIContent("Preserve Edges"));
                if (blurEdgePreserve.boolValue) {
                    EditorGUILayout.PropertyField(blurEdgeDepthThreshold, new GUIContent("Edge Threshold"));
                }
                EditorGUI.indentLevel--;
            }
            if (blurPasses.intValue == 0 && downscaling.floatValue <= 1f)
            {
                EditorGUILayout.HelpBox("No composition in effect (no downscaling and no blur applied).", MessageType.Info);
                GUI.enabled = false;
            }
            EditorGUILayout.PropertyField(blendMode);
            EditorGUILayout.PropertyField(brightness);
            EditorGUILayout.PropertyField(ditherStrength);
            GUI.enabled = true;
            serializedObject.ApplyModifiedProperties();

        }

    }
}