using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(PaintFalloffArea))]
    public class PaintFalloffAreaEditor : Editor
    {
        static GUIContent CTextureSize = new GUIContent("Texture Size", "Size of the backing texture");
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            serializedObject.Update();
            PaintFalloffArea area = target as PaintFalloffArea;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clampOutsideOfBounds"));
            EditorGUI.BeginChangeCheck();
            var size = (FalloffFilter.PaintMask.Size)EditorGUILayout.EnumPopup(CTextureSize, area.paintMask.size);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(area, "Adjust Falloff");
                area.paintMask.Resize(size);
            }
            GUIUtil.DoPaintGUI(area, area.paintMask);

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                MicroVerse.instance?.Invalidate(area.GetBounds());
            }
            
        }

        private void OnSceneGUI()
        {
            var stamp = (PaintFalloffArea)target;
            GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.paintMask, stamp.GetBounds(), stamp.transform);
        }

        private bool HasFrameBounds() => Selection.objects.Length > 0;

        public Bounds OnGetFrameBounds()
        {
            var transforms = Selection.GetTransforms(SelectionMode.Unfiltered);
            Bounds result = new Bounds(transforms[0].position, transforms[0].lossyScale);
            for (int i = 1; i < transforms.Length; i++)
                result.Encapsulate(new Bounds(transforms[i].position, transforms[i].lossyScale));

            result.extents *= 0.5f;
            return result;
        }

    }
}
