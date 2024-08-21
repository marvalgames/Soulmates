using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace JBooth.MicroVerseCore
{
    public class TreeManager
    {
        List<TreePrototypeSerializable> prototypes;
        List<TreeStamp.Randomization> randomizations;

        internal const int kNavMeshLodFirst = -1;
        internal const int kNavMeshLodLast = int.MaxValue;

        public GameObject m_Tree;
        public float m_BendFactor;
        public int m_NavMeshLod;
        private int m_PrototypeIndex = -1;
        TreeStamp stamp;

        public TreeManager(TreeStamp owner, int index, List<TreePrototypeSerializable> protos, List<TreeStamp.Randomization> rands = null)
        {
            stamp = owner;
            m_PrototypeIndex = index;
            prototypes = protos;
            randomizations = rands;

            if (m_PrototypeIndex == -1)
            {
                m_Tree = null;
                m_BendFactor = 0.0f;
                m_NavMeshLod = kNavMeshLodLast;
            }
            else
            {
                var treePrototype = prototypes[m_PrototypeIndex];
                m_Tree = treePrototype.prefab;
                m_BendFactor = treePrototype.bendFactor;
                m_NavMeshLod = treePrototype.navMeshLod;
            }

        }

        public bool IsValidTree()
        {
            return IsValidTree(m_Tree, m_PrototypeIndex);
        }

        public bool HasTree()
        {
            return m_Tree != null;
        }
        private static bool IsValidTree(GameObject tree, int prototypeIndex)
        {
            if (tree == null)
                return false;
            /*
            for (int i = 0; i < prototypes.Length; ++i)
            {
                if (i != prototypeIndex && prototypes[i].prefab == tree)
                    return false;
            }
            */
            return true;
        }

        public void SetTree(GameObject prefab)
        {
            this.m_Tree = prefab;
        }

        public void DoApply()
        {
            if (m_PrototypeIndex < 0 || m_PrototypeIndex >= prototypes.Count)
            {

                var newTree = new TreePrototypeSerializable();
                newTree.prefab = m_Tree;
                newTree.bendFactor = m_BendFactor;
                newTree.navMeshLod = m_NavMeshLod;
                prototypes.Add(newTree);
                if (randomizations != null)
                {
                    var r = new TreeStamp.Randomization();
                    r.disabled = false;
                    r.scaleMultiplierAtBoundaries = 1;
                    r.weight = 50;
                    r.weightRange = new Vector2(0, 99999);
                    r.randomRotation = true;
                    r.scaleHeightRange = Vector2.one;
                    r.scaleWidthRange = Vector2.one;
                    randomizations.Add(r);
                }
            }
            else
            {
                prototypes[m_PrototypeIndex].prefab = m_Tree;
                prototypes[m_PrototypeIndex].bendFactor = m_BendFactor;
                prototypes[m_PrototypeIndex].navMeshLod = m_NavMeshLod;
            }
            if (MicroVerse.instance != null)
                MicroVerse.instance.Invalidate(stamp.GetBounds(), MicroVerse.InvalidateType.Tree);
        }
        internal static bool IsLODTreePrototype(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<LODGroup>() != null;
        }

        public bool DrawWizardGUI()
        {
            EditorGUI.BeginChangeCheck();

            m_Tree = (GameObject)EditorGUILayout.ObjectField("Tree Prefab", m_Tree, typeof(GameObject), false);

            if (m_Tree)
            {
                MeshRenderer meshRenderer = m_Tree.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    GUI.enabled = false;
                    EditorGUILayout.EnumPopup("Cast Shadows", meshRenderer.shadowCastingMode);
                    GUI.enabled = true;
                }
            }
            if (!IsLODTreePrototype(m_Tree))
            {
                m_BendFactor = EditorGUILayout.FloatField("Bend Factor", m_BendFactor);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                LODGroup lodGroup = m_Tree.GetComponent<LODGroup>();

                NavMeshLodIndex navMeshLodIndex = NavMeshLodIndex.Custom;
                if (m_NavMeshLod == kNavMeshLodLast)
                    navMeshLodIndex = NavMeshLodIndex.Last;
                else if (m_NavMeshLod == kNavMeshLodFirst)
                    navMeshLodIndex = NavMeshLodIndex.First;

                navMeshLodIndex = (NavMeshLodIndex)EditorGUILayout.EnumPopup("NavMesh LOD Index", navMeshLodIndex, GUILayout.MinWidth(250));

                if (navMeshLodIndex == NavMeshLodIndex.First)
                    m_NavMeshLod = kNavMeshLodFirst;
                else if (navMeshLodIndex == NavMeshLodIndex.Last)
                    m_NavMeshLod = kNavMeshLodLast;
                else
                    m_NavMeshLod = EditorGUILayout.IntSlider(m_NavMeshLod, 0, Mathf.Max(0, lodGroup.lodCount - 1));

                EditorGUILayout.EndHorizontal();
            }

            bool changed = EditorGUI.EndChangeCheck();

            if (changed)
            {
                if (MicroVerse.instance != null)
                    MicroVerse.instance.Invalidate(stamp.GetBounds(), MicroVerse.InvalidateType.Tree);
            }

            return changed;
        }
    }
}