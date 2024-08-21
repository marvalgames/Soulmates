using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.PostEvents
{
    public class SR_CutTerrainHole : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Cut Terrain Hole"; }
        public override string Tooltip() { return "Detect Unity Terrain below spawned object and cut terrain hole" + base.Tooltip(); }

        public EProcedureType Type { get { return EProcedureType.Event; } }

        //[Header("Detecting Terrain Object")]
        //public LayerMask GroundRaycastMask = 1 << 0;
        //[Tooltip("Most cases it will be 0,-1,0 so straight down")]
        //public Vector3 RaycastDirection = Vector3.down;
        //[Tooltip("How far collision raycast can go")]
        //public float RaycastLength = 7f;
        //[Tooltip("Casting ray from upper or lower position of the object")]
        //public Vector3 OffsetRaycastOrigin = Vector3.up;

        [Header("Size of the cutted hole")]
        public float TerrainHoleRadius = 0f;
        public bool Square = false;

        //[Space(6)]
        //[Tooltip("If you don't want to cut terrain hole during edit mode")]
        //public bool ExecuteOnlyInPlaymode = true;

        #region There you can do custom modifications for inspector view
#if UNITY_EDITOR
        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("Beware, you will be most likely NOT ABLE to UNDO modified terrain holes! You will need to restore them manually if you need to.", MessageType.None);
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
                    DetectTerrainAndCutHole(o, SR_RemoveTerrainTrees.GetTerrainIn(o.transform.position), TerrainHoleRadius, Square);
                };

                spawn.OnGeneratedEvents.Add(flattenTerrain);
        }

        public static Vector3 WorldPosToTerrainNormalizedPos(Vector3 wPos, Terrain terr)
        {
            if (terr)
            {
                Vector3 onTerrain = ((wPos) - terr.gameObject.transform.position);
                Vector3 terrLocalPos;
                terrLocalPos.x = onTerrain.x / terr.terrainData.size.x;
                terrLocalPos.y = onTerrain.y / terr.terrainData.size.y;
                terrLocalPos.z = onTerrain.z / terr.terrainData.size.z;

                wPos = terrLocalPos;
            }

            return wPos;
        }



        public static bool[,] DetectTerrainAndCutHole(GameObject o, Terrain terr, float size, bool square)
        {
            bool[,] holes = null;

            if (terr)
            {
                Vector3 terrLocalPos = WorldPosToTerrainNormalizedPos(o.transform.position, terr);

                int tScale = terr.terrainData.holesResolution;
                int posXInTerrain = (int)(terrLocalPos.x * tScale);
                int posYInTerrain = (int)(terrLocalPos.z * tScale);

                holes = terr.terrainData.GetHoles(0, 0, tScale, tScale);
                bool[,] newHoles = terr.terrainData.GetHoles(0, 0, tScale, tScale);

                int radiusInSamples = Mathf.CeilToInt((size * tScale) / terr.terrainData.size.x);

                for (int x = -radiusInSamples; x <= radiusInSamples; x++)
                    for (int z = -radiusInSamples; z <= radiusInSamples; z++)
                    {
                        int tZ = posXInTerrain + x;
                        int tX = posYInTerrain + z;
                        if (tX < 0 || tZ < 0 || tX >= newHoles.GetLength(0) || tZ >= newHoles.GetLength(1)) continue;

                        if (!square) if (Vector2.Distance(Vector2.zero, new Vector2(x, z)) > radiusInSamples) continue;
                        newHoles[tX, tZ] = false;
                    }

                terr.terrainData.SetHoles(0, 0, newHoles);
            }

            return holes;
        }


    }
}