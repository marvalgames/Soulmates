using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(AmbientArea))]
    public class AmbientAreaEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambient"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("falloff"));
            serializedObject.ApplyModifiedProperties();

            AmbientArea aa = (target as AmbientArea);
            
            Vector2 falloffParams = aa.falloffParams;
            EditorGUI.BeginChangeCheck();
            if (aa.falloff == AmbientArea.AmbianceFalloff.Box ||
                aa.falloff == AmbientArea.AmbianceFalloff.Range)
            {
                falloffParams.x = EditorGUILayout.Slider("Range", falloffParams.x, 0.0f, 1.0f);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty("falloffParams").vector2Value = falloffParams;
            }

            if (aa.falloff == AmbientArea.AmbianceFalloff.Spline || aa.falloff == AmbientArea.AmbianceFalloff.SplineArea)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spline"));
                EditorGUI.BeginChangeCheck();

                Vector2 fp = new Vector2(falloffParams.x, falloffParams.y);
                fp = EditorGUILayout.Vector2Field("Range", fp);
                falloffParams.x = fp.x;
                falloffParams.y = fp.y;
                
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.FindProperty("falloffParams").vector2Value = falloffParams;
                }
                if (aa.falloff == AmbientArea.AmbianceFalloff.SplineArea)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("worldHeightRange"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("worldHeightFalloff"));
                }

            }
#if __MICROVERSE_MASKS__
            if (aa.falloff == AmbientArea.AmbianceFalloff.SDFMask)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maskTarget"));
                Vector2 fp = new Vector2(falloffParams.x, falloffParams.y);
                fp = EditorGUILayout.Vector2Field("Range", fp);
                falloffParams.x = fp.x;
                falloffParams.y = fp.y;

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.FindProperty("falloffParams").vector2Value = falloffParams;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("worldHeightRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("worldHeightFalloff"));
            }
#endif
            serializedObject.ApplyModifiedProperties();
        }

    }
}
