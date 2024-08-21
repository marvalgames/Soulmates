using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    /// <summary>
    /// Wrapper for a content collection item. To be used on selection grid.
    /// </summary>
    public class PresetItem
    {
        public ContentCollection collection;
        public ContentData content;
        public int collectionIndex;

        public GUIContent GetGUIContent()
        {
            return collection.GetPreview(content);
        }

        public PresetItem(ContentCollection collection, ContentData content, int collectionIndex)
        {
            this.collection = collection;
            this.content = content;
            this.collectionIndex = collectionIndex;
        }
    }
}