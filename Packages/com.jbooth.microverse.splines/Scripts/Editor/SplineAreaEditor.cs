using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
#if UNITY_2021_3_OR_NEWER

using UnityEngine.Splines;
using UnityEditor.Splines;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(SplineArea), true)]
    [CanEditMultipleObjects]
    public class SplineAreaEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            serializedObject.Update();
            SplineArea area = target as SplineArea;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spline"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sdfRes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSDF"));

            GUIUtil.DrawNoise(area, area.positionNoise, "Position Noise", FilterSet.NoiseOp.Add, false, false);
            
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                var path = target as SplineArea;
                path.UpdateSplineSDFs();
                MicroVerse.instance?.Invalidate(path.GetBounds());
            }
        }

        private void OnEnable()
        {
            EditorSplineUtility.AfterSplineWasModified += OnAfterSplineWasModified;
        }
        private void OnDisable()
        {
            EditorSplineUtility.AfterSplineWasModified -= OnAfterSplineWasModified;
        }

        void OnAfterSplineWasModified(Spline spline)
        {
            var path = target as SplineArea;
            if (path != null && path.spline != null && path.spline.Splines != null)
            {
                foreach (var s in path.spline.Splines)
                {
                    if (ReferenceEquals(spline, s))
                    {
                        path.UpdateSplineSDFs();
                        MicroVerse.instance?.Invalidate(path.GetBounds());
                        return;
                    }
                }
            }
        }
    }
}
#endif