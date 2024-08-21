using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    [AddComponentMenu( "FImpossible Creations/Level Design/Build Planner Mesh Ignore", 4)]
    public class PlannerMeshIgnore : MonoBehaviour
    {

        #region Editor Class

#if UNITY_EDITOR

        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor( typeof( PlannerMeshIgnore ) )]
        public class PlannerMeshIgnoreEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                UnityEditor.EditorGUILayout.HelpBox( "Thanks to this component, this mesh will be ignored in computing grid, for example when using Prefab To Grid Shape Generator", UnityEditor.MessageType.Info );
                serializedObject.Update();
                GUILayout.Space( 4f );
                DrawPropertiesExcluding( serializedObject, "m_Script" );
                serializedObject.ApplyModifiedProperties();
            }
        }

#endif

        #endregion

    }
}