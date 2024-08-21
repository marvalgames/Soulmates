using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using FIMSpace.FEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating
{
    [CreateAssetMenu(fileName = "OS_", menuName = "FImpossible Creations/Procedural Generation/Object Stamper Preset", order = 10100)]
    public partial class OStamperSet : ScriptableObject
    {
        [Tooltip("Tag used for ignoring placement on stamp objects")]
        public string StampersetTag = "";
        [FPD_Header("Placement Randomization Settings", 2, 5)]
        [Tooltip("Randomize positioning basing on model's bounds scale for raycasting")]
        [Range(0f, 1f)]
        public float RandomizePosition = 0.2f;

        public Vector3 RandPositionAxis = new Vector3(1f, 0f, 1f);
        //public MinMaxF RandomizePosition = new MinMaxF(0f, 0.1f);

        [Space(8)]
        [Tooltip("Randomize rotation")]
        public Vector2 RotationRanges = new Vector2(-180, 180);

        public Vector3 RandRotationAxis = Vector3.up;

        //public MinMaxF RandomizeRotation = new MinMaxF(-180f, 180f);
        [Tooltip("How dense can be rotation changes during randomization process")]
        public Vector3 AngleStepForAxis = Vector3.one;

        [Space(8)]
        [Tooltip("Randomize positioning basing on model's bounds scale for raycasting")]
        [Range(0f, 1f)]
        public float RandomizeScale = 0f;

        [Tooltip("Axis on which object should scale, leave Y and Z as 1 and change X to negative value for uniform scale!")]
        public Vector3 RandScaleAxis = Vector3.one;
        //public MinMaxF RandomizeScale = new MinMaxF(0f, 0f);

        [FPD_Header("Raycasting Settings", 8, 5)]
        public LayerMask RayCheckLayer = ~(0 << 0);

        [Tooltip("Local space direction to check alignment for prefabs in list (can be overrided)")]
        public Vector3 RayCheckDirection = Vector3.down;

        [Range(0f, 1f)]
        public float RaycastAlignment = 1f;
        [Range(-0.5f, 1f)]
        [Tooltip("Offsetting placing object on floor/wall to avoid Z-fight on flat models")]
        public float AlignOffset = 0f;

        //[Range(1, 5)]
        public EOSPlacement PlacementMode = EOSPlacement.LayAlign;

        //public int RaycastDensity = 1;
        public bool RaycastWorldSpace = true;
        //[Tooltip("When obstacle hit occured then algorithm will check ground below (world Vector down) to place object on floor in front of obstacle (for example wall)\nIt will work nicely for furniture which needs to stand under wall")]
        //public bool DropDown = false;
        [Range(0f, 1.15f)]
        [Tooltip("When spawning checking if object is not overlapping through OverlapCheckMask collision objects")]
        public float OverlapCheckScale = 0f;
        public LayerMask OverlapCheckMask = 0;

        //public float RaycastUpOffset = 0.1f;

        [Tooltip("Raycast length distance is multiplier of stamper set reference bounds size")]
        public float RayDistanceMul = 1.5f;

        [Space(8)]
        [Tooltip("Reference bounding box to compute emitting coordinates correctly (local - based on shared meshes)")]
        public Bounds ReferenceBounds;

        [HideInInspector] public List<OSPrefabReference> Prefabs;

        #region Composition API

        [System.Serializable]
        /// <summary> Use for overriding prefabs to spawn with use of Stamper Set file </summary>
        public class StamperSetOverrider
        {
            [HideInInspector] public bool Editor_Foldout = false;
            public bool Enabled = false;
            public OStamperSet Source;
            public List<OverrideInstance> OverrideInstances = new List<OverrideInstance>();

            public StamperSetOverrider(OStamperSet src)
            {
                Source = src;
                Enabled = true;
            }

            public void PrepareOverriders()
            {
                if (Source == null) return;
                FGenerators.AdjustCount(OverrideInstances, Source.Prefabs.Count);
            }

            public OStamperSet GetOverridedSetup()
            {
                if (Source == null) return null;
                if (Enabled == false) return null;
                OStamperSet set = Instantiate(Source);

                for (int c = 0; c < OverrideInstances.Count; c++)
                {
                    if (c >= set.Prefabs.Count) break;
                    if (OverrideInstances[c].Prefab == null) continue;
                    set.Prefabs[c].SetPrefab(OverrideInstances[c].Prefab);
                }

                set.RefreshBounds();
                return set;
            }

            [System.Serializable]
            public class OverrideInstance
            {
                public GameObject Prefab;
            }
        }

        #region Editor Code
#if UNITY_EDITOR

        /// <summary> Returns true if something changed (set dirty then) </summary>
        public static bool Editor_DrawCompositionGUI(OStamperSet.StamperSetOverrider compos, bool allowChangingPreset)
        {
            if (compos == null) return false;

            EditorGUI.BeginChangeCheck();

            if (compos.Enabled == false)
            {
                compos.Enabled = EditorGUILayout.Toggle("Use Stamper Prefabs Overriding", compos.Enabled);
                return EditorGUI.EndChangeCheck();
            }

            if (compos.Source == null)
            {
                if (!allowChangingPreset)
                {
                    EditorGUILayout.LabelField("No Stamper Preset!", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUIUtility.labelWidth = 250;
                    compos.Source = (OStamperSet)EditorGUILayout.ObjectField("Assign Stamper to use Overriding", compos.Source, typeof(OStamperSet), true);
                    EditorGUIUtility.labelWidth = 0;

                    if (compos.Source != null) compos.Editor_Foldout = true;
                }

                return EditorGUI.EndChangeCheck();
            }

            EditorGUILayout.BeginHorizontal();

            compos.Enabled = EditorGUILayout.Toggle(compos.Enabled, GUILayout.Width(24));
            GUILayout.Space(4);
            compos.Editor_Foldout = EditorGUILayout.Foldout(compos.Editor_Foldout, " Prefabs Overrider", true);
            var preSrc = compos.Source;
            GUI.enabled = allowChangingPreset;
            compos.Source = (OStamperSet)EditorGUILayout.ObjectField(compos.Source, typeof(OStamperSet), true);
            GUI.enabled = true;
            if (preSrc != compos.Source) compos.PrepareOverriders();
            EditorGUILayout.EndHorizontal();

            if (compos.Source != null)
                if (compos.Editor_Foldout)
                {
                    EditorGUI.indentLevel += 1;

                    if (compos.Source.Prefabs.Count != compos.OverrideInstances.Count) compos.PrepareOverriders();

                    float maxWidth = EditorGUIUtility.currentViewWidth - 60;
                    float currWdth = 40f;

                    GUILayout.Space(8);

                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0; i < compos.Source.Prefabs.Count; i++)
                    {
                        if (currWdth > maxWidth)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            currWdth = 40f;
                        }
                        else
                        {
                            if (i > 0) GUILayout.Space(8);
                        }

                        EditorGUILayout.BeginVertical(GUILayout.Width(120), GUILayout.Height(50));
                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(compos.Source.Prefabs[i].GameObject, typeof(GameObject), false, GUILayout.Width(120));
                        GUI.enabled = true;
                        EditorGUILayout.LabelField("Replace With:", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(120));
                        compos.OverrideInstances[i].Prefab = (GameObject)EditorGUILayout.ObjectField(compos.OverrideInstances[i].Prefab, typeof(GameObject), false, GUILayout.Width(120));
                        EditorGUILayout.EndVertical();

                        currWdth += 148;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel -= 1;
                }

            return EditorGUI.EndChangeCheck();
        }

#endif
        #endregion


        #endregion


        [HideInInspector] public EOSRaystriction StampRestriction = EOSRaystriction.None;
        [Tooltip("Give spawn info in the generated objects (adding StampStigma component) in order for other stamps to detect details of this generated object")]
        [HideInInspector] public bool IncludeSpawnDetails = true;

        public List<OStamperSet> RestrictionSets;
        [HideInInspector] public int PlacementLimitCount = 0;

        [FPD_Header("Physical Restrictions", 8, 5)]
        [HideInInspector][Range(0, 90)] public int MaxSlopeAngle = 60;

        [HideInInspector][Range(0f, 1f)] public float MinimumStandSpace = 0.2f;

        [FPD_Header("GameObject Tag Based", 8, 5)]
        public List<string> AllowJustOnTags = new List<string>();

        public List<string> DisallowOnTags = new List<string>();

        [Space(5)]
        public List<string> IgnoreCheckOnTags = new List<string>();

        // Rest of the code inside partial classes -------------------
    }
}