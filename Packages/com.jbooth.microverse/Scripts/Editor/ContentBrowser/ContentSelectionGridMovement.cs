using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    public class ContentSelectionGridMovement
    {

        /// <summary>
        /// Content collection of the preset to be moved. null if none is set
        /// </summary>
        private static ContentCollection moveContentCollection = null;

        /// <summary>
        /// Index of the preset to be moved. -1 if none is set
        /// </summary>
        private static int moveCollectionIndex = -1;

        public static void OnInspectorGUI( PresetItem presetItem)
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.X)
                {
                    PrepareMove(presetItem);
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.V)
                {
                    InsertBefore(presetItem);
                    Event.current.Use();
                }
            }
        }

        public static bool MoveHereEnabled(PresetItem presetItem)
        {
            return moveContentCollection != null && moveCollectionIndex != -1 && presetItem != null && moveContentCollection == presetItem.collection;
        }

        public static void PrepareMove(PresetItem presetItem)
        {
            if (presetItem == null)
                return;

            moveCollectionIndex = presetItem.collectionIndex;
            moveContentCollection = presetItem.collection;

            Debug.Log($"copyCollectionIndex = {moveCollectionIndex}");


        }

        public static void InsertBefore(PresetItem presetItem)
        {
            if (presetItem == null)
                return;

            if (moveContentCollection == null || moveCollectionIndex == -1)
                return;

            int currentIndex = presetItem.collectionIndex;

            if ((currentIndex == -1))
                return;

            if (currentIndex == moveCollectionIndex)
                return;

            List<ContentData> list = presetItem.collection.contents.ToList();

            ContentData moveable = list[moveCollectionIndex];
            ContentData target = presetItem.content;

            list.Remove(moveable);

            int insertIndex = list.IndexOf(target);
            list.Insert(insertIndex, moveable);

            presetItem.collection.contents = list.ToArray();

            EditorUtility.SetDirty(presetItem.collection);

            // AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();

            moveCollectionIndex = -1;


        }
    }
}