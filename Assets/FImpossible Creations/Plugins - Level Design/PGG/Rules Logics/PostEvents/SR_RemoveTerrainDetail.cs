using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.PostEvents
{
    public class SR_RemoveTerrainDetail : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Remove Terrain Details"; }
        public override string Tooltip() { return "Detect Unity Terrain below spawned object and changing terrain detail layer values" + base.Tooltip(); }

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
        public float Radius = 0f;
        public bool Square = false;

        [Header("What to change in terrain detail layer")]
        public int DetailLayer = 0;
        [Space(5)]
        [Tooltip("You can change terrain detail layer value to zero to erase objects or change it to custom value")]
        public int ChangeDetailValueTo = 0;

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
                    DetectTerrainAndRemoveDatas(o, SR_RemoveTerrainTrees.GetTerrainIn(o.transform.position), Radius, Square, DetailLayer, ChangeDetailValueTo);
                };

                spawn.OnGeneratedEvents.Add(flattenTerrain);
        }


        public static int[,] DetectTerrainAndRemoveDatas(GameObject o, Terrain terr, float size, bool square, int detailLayer, int newValue)
        {
            int[,] details = null;

            if (terr)
            {
                Vector3 terrLocalPos = SR_CutTerrainHole.WorldPosToTerrainNormalizedPos(o.transform.position, terr);

                int tScale = terr.terrainData.detailResolution;
                int posXInTerrain = (int)(terrLocalPos.x * tScale);
                int posYInTerrain = (int)(terrLocalPos.z * tScale);

                details = terr.terrainData.GetDetailLayer(0, 0, tScale, tScale, detailLayer);
                int[,] newHoles = terr.terrainData.GetDetailLayer(0, 0, tScale, tScale, detailLayer);

                int radiusInSamples = Mathf.CeilToInt((size * tScale) / terr.terrainData.size.x);

                for (int x = -radiusInSamples; x <= radiusInSamples; x++)
                    for (int z = -radiusInSamples; z <= radiusInSamples; z++)
                    {
                        int tZ = posXInTerrain + x;
                        int tX = posYInTerrain + z;
                        if (tX < 0 || tZ < 0 || tX >= newHoles.GetLength(0) || tZ >= newHoles.GetLength(1)) continue;

                        if (!square) if (Vector2.Distance(Vector2.zero, new Vector2(x, z)) > radiusInSamples) continue;
                        newHoles[tX, tZ] = newValue;
                    }

                terr.terrainData.SetDetailLayer(0, 0, detailLayer, newHoles);
            }

            return details;
        }


    }
}