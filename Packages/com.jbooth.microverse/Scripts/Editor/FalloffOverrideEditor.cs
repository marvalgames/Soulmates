using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(FalloffOverride))]
    public class FalloffOverrideEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            GUIUtil.DrawHeaderLogo();
            FalloffOverride fo = (FalloffOverride)target;
            var old = fo.filter.filterType;
            GUIUtil.DrawFalloffFilter(fo, fo.filter, fo.transform, true, false);
            if (fo.filter.filterType == FalloffFilter.FilterType.PaintMask)
            {
                fo.filter.filterType = old;
            }    
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                MicroVerse.instance?.Invalidate(null); 
            }

        }
    }


}
