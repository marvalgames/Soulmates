using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    [AddComponentMenu( "FImpossible Creations/Level Design/Build Planner Command Indicator", 3)]
    public class PlannerCommandIndicator : MonoBehaviour
    {
        public bool RotationCommand = true;

        [Tooltip("ID for command to execute")]
        public int ID = 0;

        [Tooltip( "Can be used for specific indicating, by reading this value from command in the node graph" )]
        public string HelperString = "";

        #region Editor Class

#if UNITY_EDITOR

        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor( typeof( PlannerCommandIndicator ) )]
        public class PlannerCommandIndicatorEditor : UnityEditor.Editor
        {
            public PlannerCommandIndicator Get { get { if( _get == null ) _get = (PlannerCommandIndicator)target; return _get; } }
            private PlannerCommandIndicator _get;

            public override void OnInspectorGUI()
            {
                UnityEditor.EditorGUILayout.HelpBox( "Helper component which provides cell commands for prefab fields. (Build Layout)", UnityEditor.MessageType.Info );

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