using UnityEditor;

namespace JBooth.MicroVerseCore
{
    /// <summary>
    /// Custom editor for content collection.
    /// Without it the default inspector of the reorderable list is broken, eg only max 3 elements visible, reordering doesn't work properlty, etc.
    /// This wrapper seems to suffice
    /// </summary>
    [CustomEditor(typeof(ContentCollection))]
    public class ContentCollectionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            ContentCollection cc = target as ContentCollection;
            EditorGUI.BeginChangeCheck();
            cc.renderPipeline = (ContentCollection.RenderPipeline)EditorGUILayout.EnumFlagsField("Render Pipeline", cc.renderPipeline);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(cc);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}