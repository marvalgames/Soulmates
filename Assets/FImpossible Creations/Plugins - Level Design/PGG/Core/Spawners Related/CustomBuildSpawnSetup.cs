using FIMSpace.Generating.Planning;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    [System.Serializable]
    public class CustomBuildSpawnSetup
    {
        public enum ESpawnType { Prefabs, Stamper, MultiEmitter }
        public ESpawnType SpawnType = ESpawnType.Prefabs;
        //public Vector3 CellSize = Vector3.one;
        //public bool UniformCellSize = true;
        public bool MultipleSpawns = false;

        public List<SpawnSet> Spawns => _spawns;
        [SerializeField] private List<SpawnSet> _spawns = new List<SpawnSet>();


        #region Properties and Utils

        public bool Validated
        {
            get
            {
                if (Spawns.Count == 0) return false;
                if (Spawns[0].GetObject() != null) return true;
                return false;
            }
        }


        public void RefreshSpawnList()
        {
            if (_spawns == null) _spawns = new List<SpawnSet>();
            if (_spawns.Count == 0) _spawns.Add(new SpawnSet());
        }

        #endregion







        #region Editor Code
#if UNITY_EDITOR

        [HideInInspector] public bool _Foldout = false;

#endif
        #endregion








        public UnityEngine.Object GetSpawn(int index = 0)
        {
            return _spawns[index].GetObject();
        }

        public void SetSpawn(UnityEngine.Object obj, int index = 0)
        {
            if (index == 0 && _spawns.Count < 1) _spawns.Add(new SpawnSet());
            if (_spawns.ContainsIndex(index) == false) { return; }
            _spawns[index].SetObject(obj);
        }

        //public Vector3 GetCellSize()
        //{
        //    if (UniformCellSize) return new Vector3(CellSize.x, CellSize.x, CellSize.x);
        //    else return CellSize;
        //}

        //internal void PreparePlannerInstance(FieldPlanner plannerInst, System.Random rand)
        //{
        //    plannerInst.FieldType = FieldPlanner.EFieldType.Prefab;
        //    plannerInst.PreviewCellSize = GetCellSize();
        //    plannerInst.DefaultPrefab = ChooseTargetSpawn(rand);
        //}

        public GameObject ChooseTargetSpawn(System.Random rand)
        {
            if (Spawns.Count < 2 || MultipleSpawns == false) return GetSpawn() as GameObject;

            #region Priority Choose

            bool allSamePrior = false;
            for (int s = 0; s < Spawns.Count; s++)
            {
                if (Spawns[s].Probability != 1f) { allSamePrior = false; }
            }

            if (!allSamePrior) // Different priorities choose
            {
                float propSum = 0f;
                for (int i = 0; i < Spawns.Count; i++) propSum += Spawns[i].Probability;

                float selection = (float)rand.NextDouble() * propSum;
                float progress = 0f;

                for (int i = 0; i < Spawns.Count; i++)
                {
                    progress += Spawns[i].Probability;
                    if (selection < progress) return Spawns[i].PrefabReference;
                }

                return Spawns[Spawns.Count - 1].PrefabReference;
            }

            #endregion

            return GetSpawn(rand.Next(0, Spawns.Count)) as GameObject;
        }

        [System.Serializable]
        public class SpawnSet
        {
            public GameObject PrefabReference;
            [Range(0f, 1f)] public float Probability = 1f;

            // TODO - Choose count limits etc

            public void SetObject(UnityEngine.Object obj)
            {
                if (obj is GameObject)
                {
                    PrefabReference = obj as GameObject;
                    return;
                }

                PrefabReference = null;
            }

            public UnityEngine.Object GetObject()
            {
                return PrefabReference;
            }

            #region Editor Code
#if UNITY_EDITOR

            internal UnityEngine.Object DisplayPropertyField(int maxWidth = 200)
            {
                PrefabReference = (GameObject)UnityEditor.EditorGUILayout.ObjectField(PrefabReference, typeof(GameObject), false, GUILayout.MaxWidth(maxWidth));
                return PrefabReference;
            }

#endif
            #endregion

        }


    }
}