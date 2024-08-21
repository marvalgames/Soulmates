using System.Collections.Generic;
using UnityEngine;
using static FIMSpace.Generating.GridPainter;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating
{
    //public partial class GridPainter
    //{
        [AddComponentMenu("", 0)]
        public class GridPainterData : MonoBehaviour
        {
            public GridPainter Parent;
            /*[HideInInspector] */
            public List<PainterCell> CellsMemory = new List<PainterCell>();
            public List<SpawnInstructionGuide> CellsInstructions = new List<SpawnInstructionGuide>();

            #region Editor Class

#if UNITY_EDITOR
            [UnityEditor.CanEditMultipleObjects]
            [UnityEditor.CustomEditor(typeof(GridPainterData))]
            public class GridPainterDataEditor : UnityEditor.Editor
            {
                public GridPainterData Get { get { if (_get == null) _get = (GridPainterData)target; return _get; } }
                private GridPainterData _get;

                public override void OnInspectorGUI()
                {
                    UnityEditor.EditorGUILayout.HelpBox("This component is keeping grid data saved. Thanks to separated component, you will not lost performance in scene view when working with big grids.", UnityEditor.MessageType.Info);

                    serializedObject.Update();

                    GUILayout.Space(4f);
                    DrawPropertiesExcluding(serializedObject, "m_Script");

                    serializedObject.ApplyModifiedProperties();

                    GUILayout.Space(4f);
                    EditorGUILayout.LabelField("Containing " + Get.CellsMemory.Count + " saved cells.");
                }
            }
#endif

            #endregion

        }
    //}
}