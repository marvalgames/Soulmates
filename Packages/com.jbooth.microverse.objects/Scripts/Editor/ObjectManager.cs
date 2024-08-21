using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace JBooth.MicroVerseCore
{
    public class ObjectManager
    {
        List<GameObject> prototypes;
        List<ObjectStamp.Randomization> randomizations;

        internal const int kNavMeshLodFirst = -1;
        internal const int kNavMeshLodLast = int.MaxValue;

        public GameObject m_Object;
        public float m_BendFactor;
        public int m_NavMeshLod;
        private int m_PrototypeIndex = -1;
        public ObjectStamp owner;

        public ObjectManager(ObjectStamp owner, int index, List<GameObject> protos, List<ObjectStamp.Randomization> rands = null)
        {
            this.owner = owner;
            m_PrototypeIndex = index;
            prototypes = protos;
            randomizations = rands;

            if (m_PrototypeIndex == -1)
            {
                m_Object = null;
            }
            else
            {
                m_Object = prototypes[m_PrototypeIndex];
            }

        }

        public void SetObject(GameObject go)
        {
            m_Object = go;
        }
     
        public void DoApply()
        {
            if (m_PrototypeIndex < 0 || m_PrototypeIndex >= prototypes.Count)
            {
                prototypes.Add(m_Object);
                if (randomizations != null)
                {
                    var r = new ObjectStamp.Randomization();
                    r.scaleMultiplierAtBoundaries = 1;
                    r.weight = 50;
                    r.scaleRangeX = Vector2.one;
                    r.scaleRangeY = Vector2.one;
                    r.scaleRangeZ = Vector2.one;
                    randomizations.Add(r);
                }
            }
            else
            {
                prototypes[m_PrototypeIndex] = m_Object;
            }
            if (MicroVerse.instance != null)
                MicroVerse.instance.Invalidate(owner.GetBounds(), MicroVerse.InvalidateType.All);
        }

        public bool DrawWizardGUI()
        {
            EditorGUI.BeginChangeCheck();

            m_Object = (GameObject)EditorGUILayout.ObjectField("Object Prefab", m_Object, typeof(GameObject), false);


            bool changed = EditorGUI.EndChangeCheck();

            if (changed)
            {
                if (MicroVerse.instance != null)
                    MicroVerse.instance.Invalidate(owner.GetBounds(), MicroVerse.InvalidateType.All);
            }

            return changed;
        }
    }
}