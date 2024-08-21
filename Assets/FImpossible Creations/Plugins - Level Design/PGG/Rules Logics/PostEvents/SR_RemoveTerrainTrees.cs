using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.PostEvents
{
    public class SR_RemoveTerrainTrees : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Remove Terrain Trees"; }
        public override string Tooltip() { return "Detect Unity Terrain below spawned object and removing tree instances around" + base.Tooltip(); }

        public EProcedureType Type { get { return EProcedureType.Event; } }

        [Header("Size of the cutted hole")]
        public float Radius = 0f;
        public bool Square = false;


        #region There you can do custom modifications for inspector view
#if UNITY_EDITOR
        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("Beware, you will be most likely NOT ABLE to UNDO modified terrain trees placement! You will need to restore them manually if you need to.", MessageType.None);
            base.NodeBody(so);
        }
#endif
        #endregion


        public override void CellInfluence(FieldSetup preset, FieldModification mod, FieldCell cell, ref SpawnData spawn, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            base.CellInfluence(preset, mod, cell, ref spawn, grid);

                Action<GameObject> flattenTerrain =
                (o) =>
                {
                    DetectTerrainAndRemoveTrees(o, Radius, Square);
                };

                spawn.OnGeneratedEvents.Add(flattenTerrain);
        }

        public static Terrain GetTerrainIn(Vector3 wPos)
        {
            if (Terrain.activeTerrain == null) return null;
            if (Terrain.activeTerrains == null) return null;

            for (int t = 0; t < Terrain.activeTerrains.Length; t++)
            {
                var terr = Terrain.activeTerrains[t];
                if (terr)
                {
                    Vector3 locPos = terr.transform.InverseTransformPoint(wPos);
                    if (locPos.x < 0) continue;
                    if (locPos.x > terr.terrainData.bounds.max.x) continue;
                    if (locPos.z < 0) continue;
                    if (locPos.z > terr.terrainData.bounds.max.z) continue;
                    return terr;
                }
            }

            return null;
        }

        public static void DetectTerrainAndRemoveTrees(GameObject o, float size, bool square)
        {
            if (o == null) return;

            var terr = GetTerrainIn(o.transform.position);

            if (terr)
            {
                Vector3 terrLocalPos = SR_CutTerrainHole.WorldPosToTerrainNormalizedPos(o.transform.position, terr);
                Vector2 normPos = new Vector2(terrLocalPos.x, terrLocalPos.z);
                float radiusInNormalizedcale = size / terr.terrainData.size.x;

                List<TreeInstance> treeInstances = terr.terrainData.treeInstances.ToList();

                if (!square)
                {
                    for (int i = treeInstances.Count - 1; i >= 0; i--)
                    {
                        var tree = terr.terrainData.GetTreeInstance(i);
                        if (Vector2.Distance(new Vector2(tree.position.x, tree.position.z), normPos) <= radiusInNormalizedcale)
                            treeInstances.RemoveAt(i);
                    }
                }
                else
                {
                    for (int i = treeInstances.Count - 1; i >= 0; i--)
                    {
                        var tree = terr.terrainData.GetTreeInstance(i);
                        if (DistanceManhattan2D(tree.position, terrLocalPos) <= radiusInNormalizedcale)
                            treeInstances.RemoveAt(i);
                    }
                }

                if (treeInstances.Count != terr.terrainData.treeInstances.Length)
                {
                    terr.terrainData.treeInstances = treeInstances.ToArray();
                }
            }
        }

        public static float DistanceManhattan2D( Vector3 a, Vector3 b)
        {
            float diff = 0f;
            diff += Mathf.Abs(a.x - b.x);
            diff += Mathf.Abs(a.z - b.z);
            return diff;
        }

    }
}