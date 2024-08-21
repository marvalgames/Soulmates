#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace FIMSpace.Generating.Rules.Transforming.Legacy
{
    public class SR_WorldOffset : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "World Offset"; }
        public override string Tooltip() { return "Just position offset without using rotation of spawn"; }
        public EProcedureType Type { get { return EProcedureType.Event; } }

        [HideInInspector] public bool OverrideOffset = true;
        [HideInInspector] public bool Randomize = false;

        [PGG_SingleLineSwitch("CheckMode", 58, "Select if you want to use Tags, SpawnStigma or CellData", 140)]
        public string GetOffsetFromTagged = "";
        [HideInInspector] public ESR_Details CheckMode = ESR_Details.Tag;

        [PGG_SingleLineSwitch("OffsetMode", 58, "Select if you want to offset postion with cell size or world units", 140)]
        public Vector3 WorldOffset = Vector3.zero;
        [HideInInspector] public ESR_Measuring OffsetMode = ESR_Measuring.Units;

        [HideInInspector] public Vector3 RandomWorldOffset = Vector3.zero;
        [HideInInspector] public Vector2 RandomRange = new Vector2(0f, 1f);

#if UNITY_EDITOR
        public override void NodeFooter(SerializedObject so, FieldModification mod)
        {
            base.NodeFooter(so, mod);

            EditorGUILayout.BeginHorizontal();

            if (string.IsNullOrEmpty(GetOffsetFromTagged))
                EditorGUILayout.PropertyField(so.FindProperty("OverrideOffset"));

            EditorGUILayout.PropertyField(so.FindProperty("Randomize"));

            EditorGUILayout.EndHorizontal();

            if (Randomize)
            {
                EditorGUILayout.PropertyField(so.FindProperty("RandomWorldOffset"));
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 72;
                EditorGUILayout.MinMaxSlider("Min-Max:", ref RandomRange.x, ref RandomRange.y, 0f, 1f);

                GUILayout.Space(6);
                EditorGUIUtility.labelWidth = 30;
                RandomRange.x = EditorGUILayout.FloatField("Min:", RandomRange.x);
                GUILayout.Space(4);
                RandomRange.y = EditorGUILayout.FloatField("Max:", RandomRange.y);
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                if (RandomRange.x > RandomRange.y) RandomRange.x = RandomRange.y;
                if (RandomRange.y < RandomRange.x) RandomRange.y = RandomRange.x;
            }
        }
#endif

        public override void CellInfluence(FieldSetup preset, FieldModification mod, FieldCell cell, ref SpawnData spawn, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            //bool tagged = false;
            if (string.IsNullOrEmpty(GetOffsetFromTagged) == false)
            {
                SpawnData sp = CellSpawnsHaveSpecifics(cell, GetOffsetFromTagged, CheckMode, spawn);
                if (sp != null) spawn.Offset = sp.Offset;
                //tagged = true;
            }

            Vector3 tgtOffset = WorldOffset;

            if (Randomize)
            {

                Vector3 rnd = new Vector3();
                if (RandomRange == Vector2.zero) RandomRange = new Vector2(0f, 1f);

                rnd.x = FGenerators.GetRandom(RandomRange.x, RandomRange.y);
                rnd.y = FGenerators.GetRandom(RandomRange.x, RandomRange.y);
                rnd.z = FGenerators.GetRandom(RandomRange.x, RandomRange.y);

                if (FGenerators.GetRandomFlip()) rnd.x = -rnd.x;
                if (FGenerators.GetRandomFlip()) rnd.y = -rnd.y;
                if (FGenerators.GetRandomFlip()) rnd.z = -rnd.z;

                tgtOffset += new Vector3
                    (
                    RandomWorldOffset.x * rnd.x,
                    RandomWorldOffset.y * rnd.y,
                    RandomWorldOffset.z * rnd.z
                    );

            }

            tgtOffset = GetUnitOffset(tgtOffset, OffsetMode, preset);

            if (OverrideOffset)
                spawn.Offset = tgtOffset;
            else
                spawn.Offset += tgtOffset;
        }

    }
}