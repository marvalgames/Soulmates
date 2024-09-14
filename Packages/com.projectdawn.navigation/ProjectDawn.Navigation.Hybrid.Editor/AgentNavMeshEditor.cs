using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation.Editor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AgentNavMeshAuthoring))]
    class AgentNavMeshEditor : UnityEditor.Editor
    {
        static class Styles
        {
            public static readonly GUIContent AgentTypeId = EditorGUIUtility.TrTextContent("Agent Type Id", "The type ID for the agent in NavMesh.");
            public static readonly GUIContent AreaMask = EditorGUIUtility.TrTextContent("Area Mask", "Specifies which NavMesh areas are passable. Changing areaMask will make the path stale (see isPathStale).");
            public static readonly GUIContent AutoRepath = EditorGUIUtility.TrTextContent("Auto Repath", "Should the agent attempt to acquire a new path if the existing path becomes invalid?");
            public static readonly GUIContent Grounded = EditorGUIUtility.TrTextContent("Grounded", "Anchors the agent to the surface. It is useful to disable then used with physics, to allow more freedom motion and precision.");
            public static readonly GUIContent OverrideAreaCosts = EditorGUIUtility.TrTextContent("Override Area Costs", "If enabled, allows overriden area cost for this agent.");
            public static readonly GUIContent LinkTraversalMode = EditorGUIUtility.TrTextContent("Link Traversal Mode", "Should the agent move across OffMeshLinks automatically?");
            public static readonly GUIContent MappingExtent = EditorGUIUtility.TrTextContent("Mapping Extent", "Maximum distance on each axis will be used when attempting to map the agent's position or destination onto navmesh. The higher the value, the bigger the performance cost.");
        }

        SerializedProperty m_AgentTypeId;
        SerializedProperty m_AreaMask;
        SerializedProperty m_AutoRepath;
        SerializedProperty m_Grounded;
        SerializedProperty m_OverrideAreaCosts;
        SerializedProperty m_LinkTraversalMode;
        SerializedProperty m_MappingExtent;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            NavMeshComponentsGUIUtility.AgentTypePopup("Agent Type", m_AgentTypeId);
            AreaMaskField(m_AreaMask, Styles.AreaMask);
            EditorGUILayout.PropertyField(m_AutoRepath, Styles.AutoRepath);
            EditorGUILayout.PropertyField(m_Grounded, Styles.Grounded);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(m_OverrideAreaCosts, Styles.OverrideAreaCosts);
                EditorGUILayout.PropertyField(m_LinkTraversalMode, Styles.LinkTraversalMode);
            }
            EditorGUILayout.PropertyField(m_MappingExtent, Styles.MappingExtent);
            if (serializedObject.ApplyModifiedProperties())
            {
                // Update entities
                foreach (var target in targets)
                {
                    var authoring = target as AgentNavMeshAuthoring;
                    if (authoring.HasEntityPath)
                        authoring.EntityPath = authoring.DefaultPath;
                }
            }

            if (!serializedObject.isEditingMultipleObjects)
            {
                if (target is AgentNavMeshAuthoring navMesh && navMesh.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
                    EditorGUILayout.HelpBox("This component does not work with NavMeshObstacle!", MessageType.Error);
            }
        }

        void OnSceneGUI()
        {
            if (!Application.isPlaying)
                return;

            var authoring = target as AgentNavMeshAuthoring;
            using var corners = authoring.CreateCorners(1024, Unity.Collections.Allocator.Temp);
            if (!corners.TryGetCorners(out var locations))
                return;

            Handles.color = Color.green;
            for (int i = 1; i < locations.Length; i++)
            {
                Handles.DrawLine(locations[i - 1].position, locations[i].position);
            }
        }

        void OnEnable()
        {
            m_AgentTypeId = serializedObject.FindProperty("AgentTypeId");
            m_AreaMask = serializedObject.FindProperty("AreaMask");
            m_AutoRepath = serializedObject.FindProperty("AutoRepath");
            m_Grounded = serializedObject.FindProperty("m_Grounded");
            m_OverrideAreaCosts = serializedObject.FindProperty("m_OverrideAreaCosts");
            m_MappingExtent = serializedObject.FindProperty("MappingExtent");
            m_LinkTraversalMode = serializedObject.FindProperty("m_LinkTraversalMode");
        }

        void AreaMaskField(SerializedProperty property, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, label, property);

            var areaIndex = 0;
            var areaNames = UnityEngine.AI.NavMesh.GetAreaNames();
            for (var i = 0; i < areaNames.Length; i++)
            {
                var areaValue = 1 << UnityEngine.AI.NavMesh.GetAreaFromName(areaNames[i]);
                if ((areaValue & property.intValue) != 0)
                    areaIndex |= 1 << i;
            }

            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.MaskField(rect, label, areaIndex, areaNames);
            if (EditorGUI.EndChangeCheck())
            {
                areaIndex = 0;
                for (var i = 0; i < areaNames.Length; i++)
                {
                    var areaValue = 1 << UnityEngine.AI.NavMesh.GetAreaFromName(areaNames[i]);
                    if ((value & 1 << i) != 0)
                        areaIndex |= areaValue;
                }

                property.intValue = areaIndex;
            }

            EditorGUI.EndProperty();
        }
    }
}
