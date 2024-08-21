#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Rules.QuickSolutions
{
    public class SR_CallSubSpawner : SpawnRuleBase, ISpawnProcedureType
    {

        [System.Serializable]
        public class SubSpawnerCallHelper
        {
            public FieldSpawner targetSubSpawner { get; private set; }

            [HideInInspector] public int CallSpawner = -1;

            [Tooltip("Inherit this spawner final coordinates onto sub-spawner execution")]
            [HideInInspector] public bool InheritCoords = false;

            [Tooltip("Call Sub-Spawner after all rules and modificators")]
            [HideInInspector] public bool PostCall = false;

            [Tooltip("Include extra spawns operations if using 'Call on each side' with Wall Placer or 'Run on repetition' with Get Coordinates nodes")]
            [HideInInspector] public bool UseTemps = false;

            [HideInInspector] public FieldModification SubSpawnersOf = null;


            #region Helper Fields

            public FieldSpawner toCall(FieldSpawner OwnerSpawner, FieldModification parentMod)
            {
                if (CallSpawner < 0) return null;
                if (OwnerSpawner == null) return null;
                if (parentMod == null) return null;
                if (CallSpawner >= parentMod.SubSpawners.Count) return null;
                return parentMod.SubSpawners[CallSpawner];
            }

            public FieldModification parentMod
            {
                get
                {
                    if (SubSpawnersOf != null) return SubSpawnersOf;
                    if (OwnerSpawner == null) return null;
                    if (OwnerSpawner.Parent != null) return OwnerSpawner.Parent;
                    return null;
                }
            }


            void RefreshSpawner(FieldSpawner OwnerSpawner, FieldModification parentMod)
            {
                targetSubSpawner = toCall(OwnerSpawner, parentMod);
            }

            #endregion


            #region Required Actions

            FieldSpawner OwnerSpawner = null;

            public void RefreshCaller(FieldSpawner OwnerSpawner)
            {
                this.OwnerSpawner = OwnerSpawner;

                RefreshSpawner(OwnerSpawner, parentMod);

                if (targetSubSpawner == null) return;
                var spawner = targetSubSpawner;

                if (spawner.Rules == null) return;
                for (int i = 0; i < spawner.Rules.Count; i++)
                {
                    var rl = spawner.Rules[i]; if (rl == null) continue;
                    rl.Refresh();
                }
            }

            #endregion


            #region Triggering


            public void OnConditionsMetAction(ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
            {

                if (!PostCall)
                {
                    ApplySpawnerCall(cell, thisSpawn, grid, preset);
                }
                else
                {
                    SpawnData spawn = thisSpawn;
                    if (OwnerSpawner.OnPostCallEvents == null) OwnerSpawner.OnPostCallEvents = new System.Collections.Generic.List<System.Action>();

                    OwnerSpawner.OnPostCallEvents.Add(
                        () =>
                        {
                            ApplySpawnerCall(cell, spawn, grid, preset);
                        });
                }


                if (UseTemps)
                    if (OwnerSpawner != null) // Search temp spawns
                    {
                        var tempSpawns = OwnerSpawner.GetExtraSpawns();

                        if (tempSpawns != null)
                        {
                            for (int t = 0; t < tempSpawns.Count; t++)
                            {
                                var tSpawn = tempSpawns[t];

                                if (!PostCall) ApplySpawnerCall(cell, tSpawn, grid, preset, true);
                                else
                                {
                                    SpawnData spawn = tSpawn;
                                    if (OwnerSpawner.OnPostCallEvents == null) OwnerSpawner.OnPostCallEvents = new System.Collections.Generic.List<System.Action>();
                                    OwnerSpawner.OnPostCallEvents.Add(() => { ApplySpawnerCall(cell, spawn, grid, preset, true); });
                                }
                            }
                        }
                    }

            }


            public void ApplySpawnerCall(FieldCell cell, SpawnData spawn, FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, bool isTemp = false)
            {
                if (targetSubSpawner == null) return;
                if (targetSubSpawner.Enabled == false) return;

                var data = targetSubSpawner.RunSpawnerOnCell(parentMod, preset, cell, grid, Vector3.zero, null, true);

                if (data != null)
                {
                    if (InheritCoords)
                    {
                        data.SpawnSpace = spawn.SpawnSpace;
                        data.Offset += spawn.Offset;
                        data.DirectionalOffset += spawn.DirectionalOffset;
                        data.RotationOffset += spawn.RotationOffset;
                        data.LocalRotationOffset += spawn.LocalRotationOffset;
                        if (_Debug) UnityEngine.Debug.Log("Spawner Call Result " + data.Prefab);
                    }
                }
            }


            public void OnPreGenerate(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
            {
                if (OwnerSpawner == null) OwnerSpawner = callFrom;
                RefreshSpawner(OwnerSpawner, parentMod);

                if (targetSubSpawner == null) return;
                if (targetSubSpawner.Rules == null) return;

                for (int i = 0; i < targetSubSpawner.Rules.Count; i++)
                {
                    var rl = targetSubSpawner.Rules[i]; if (rl == null) continue;
                    rl.PreGenerateResetRule(grid, preset, targetSubSpawner);
                }
            }

            #endregion


            #region Editor Code
#if UNITY_EDITOR

            SerializedProperty spbase = null;
            SerializedProperty sp = null;

            public void NodeBody(SerializedObject so, string subCallerVarName, bool dontDrawToggleBar = false)
            {
                if (spbase == null) spbase = so.FindProperty(subCallerVarName);

                if (spbase == null)
                {
                    EditorGUILayout.HelpBox("Cant find variable '" + subCallerVarName + "'", MessageType.None);
                    return;
                }

                if (parentMod.SubSpawners.Count == 0)
                {
                    EditorGUILayout.HelpBox("No Sub-Spawners!", MessageType.None);
                    if (GUILayout.Button("Add first sub-spawner")) { parentMod.AddSubSpawner(); }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 90;

                    string currentSubSpawner = "None";
                    if (CallSpawner > -1) if (CallSpawner < parentMod.SubSpawners.Count) currentSubSpawner = parentMod.SubSpawners[CallSpawner].Name;

                    EditorGUILayout.LabelField("Call Spawner:", GUILayout.Width(90));
                    if (GUILayout.Button(currentSubSpawner, EditorStyles.layerMaskField))
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(new GUIContent("None"), CallSpawner < 0, () => { CallSpawner = -1; });

                        for (int i = 0; i < parentMod.SubSpawners.Count; i++)
                        {
                            int spwn = i;
                            menu.AddItem(new GUIContent("[" + i + "] " + parentMod.SubSpawners[i].Name), CallSpawner == i, () => { CallSpawner = spwn; });
                        }

                        menu.ShowAsContext();
                    }

                    EditorGUIUtility.labelWidth = 0;

                    if (GUILayout.Button("Show Sub-Spawners", GUILayout.MaxWidth(150))) { /*if (SubSpawnersOf != null) { SeparatedModWindow.SelectMod(toDraw.FieldModificators[i]); }*/ FieldModification._subDraw = 1; }
                    if (GUILayout.Button(new GUIContent("+", "Add next sub-spawner and draw sub spawners list"), GUILayout.Width(22))) { parentMod.AddSubSpawner(); }

                    EditorGUILayout.EndHorizontal();
                }

                if (sp == null) sp = spbase.FindPropertyRelative("InheritCoords");
                if (sp != null)
                {
                    SerializedProperty spc = sp.Copy();

                    if (!dontDrawToggleBar)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 90;
                        EditorGUILayout.PropertyField(sp);
                        EditorGUIUtility.labelWidth = 70;
                        spc.Next(false); EditorGUILayout.PropertyField(spc);
                        spc.Next(false); EditorGUILayout.PropertyField(spc);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        spc.Next(false); spc.Next(false);
                    }

                    EditorGUIUtility.labelWidth = 186;
                    EditorGUIUtility.fieldWidth = 24;
                    spc.Next(false);
                    EditorGUILayout.PropertyField(spc, new GUIContent("(Optional) Call Sub-Spawner of:", "Call sub-spawners of other field modificator"));
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUIUtility.fieldWidth = 0;
                }

            }


#endif

            #endregion

            public static bool _Debug = false;

        }


        public override string TitleName() { return "Call Sub Spawner"; }
        public override string Tooltip() { return "Use this node to call sub spawner stored in this field modification"; }
        public EProcedureType Type { get { return EProcedureType.OnConditionsMet; } }
        public override bool CanBeGlobal() { return false; }

        [HideInInspector] public int CallSpawner = -1;

        [Tooltip("Inherit this spawner final coordinates onto sub-spawner execution")]
        [HideInInspector] public bool InheritCoords = false;

        [Tooltip("Call Sub-Spawner after all rules and modificators")]
        [HideInInspector] public bool PostCall = false;

        [Tooltip("Include extra spawns operations if using 'Call on each side' with Wall Placer or 'Run on repetition' with Get Coordinates nodes")]
        [HideInInspector] public bool UseTemps = false;

        [HideInInspector] public FieldModification SubSpawnersOf = null;

        private SubSpawnerCallHelper SubCaller
        {
            get
            {
                if (_subCaller == null) _subCaller = new SubSpawnerCallHelper();
                _subCaller.RefreshCaller(OwnerSpawner);
                _subCaller.CallSpawner = CallSpawner;
                _subCaller.SubSpawnersOf = SubSpawnersOf;
                _subCaller.InheritCoords = InheritCoords;
                _subCaller.SubSpawnersOf = SubSpawnersOf;
                _subCaller.UseTemps = UseTemps;
                _subCaller.PostCall = PostCall;
                return _subCaller;
            }
        }

        private SubSpawnerCallHelper _subCaller = null;


        public FieldModification parentMod
        {
            get
            {
                if (SubSpawnersOf != null) return SubSpawnersOf;
                if (OwnerSpawner.Parent != null) return OwnerSpawner.Parent;
                return null;
            }
        }


        public override void Refresh()
        {
            base.Refresh();
            SubCaller.RefreshCaller(OwnerSpawner);
        }


        #region Editor Related

#if UNITY_EDITOR

        SerializedProperty sp = null;

        public override void NodeBody(SerializedObject so)
        {
            base.NodeBody(so);

            if (parentMod.SubSpawners.Count == 0)
            {
                EditorGUILayout.HelpBox("No Sub-Spawners!", MessageType.None);
                if (GUILayout.Button("Add first sub-spawner")) { parentMod.AddSubSpawner(); }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 90;

                string currentSubSpawner = "None";
                if (CallSpawner > -1) if (CallSpawner < parentMod.SubSpawners.Count) currentSubSpawner = parentMod.SubSpawners[CallSpawner].Name;

                EditorGUILayout.LabelField("Call Spawner:", GUILayout.Width(90));
                if (GUILayout.Button(currentSubSpawner, EditorStyles.layerMaskField))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("None"), CallSpawner < 0, () => { CallSpawner = -1; });

                    for (int i = 0; i < parentMod.SubSpawners.Count; i++)
                    {
                        int spwn = i;
                        menu.AddItem(new GUIContent("[" + i + "] " + parentMod.SubSpawners[i].Name), CallSpawner == i, () => { CallSpawner = spwn; });
                    }

                    menu.ShowAsContext();
                }

                EditorGUIUtility.labelWidth = 0;

                if (GUILayout.Button("Show Sub-Spawneres", GUILayout.MaxWidth(150))) { FieldModification._subDraw = 1; }
                if (GUILayout.Button(new GUIContent("+", "Add next sub-spawner and draw sub spawners list"), GUILayout.Width(22))) { parentMod.AddSubSpawner(); }

                EditorGUILayout.EndHorizontal();
            }

            if (sp == null) sp = so.FindProperty("InheritCoords");
            if (sp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 90;
                EditorGUILayout.PropertyField(sp);
                SerializedProperty spc = sp.Copy();
                EditorGUIUtility.labelWidth = 70;
                spc.Next(false); EditorGUILayout.PropertyField(spc);
                spc.Next(false); EditorGUILayout.PropertyField(spc);
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 186;
                EditorGUIUtility.fieldWidth = 24;
                spc.Next(false);
                EditorGUILayout.PropertyField(spc, new GUIContent("(Optional) Call Sub-Spawner of:", "Call sub-spawners of other field modificator"));
                EditorGUIUtility.labelWidth = 0;
                EditorGUIUtility.fieldWidth = 0;
            }

        }

#endif

        #endregion


        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            if (SubCaller.targetSubSpawner == null) return;

            SubCaller.OnConditionsMetAction(ref thisSpawn, preset, cell, grid);
        }


        public override void PreGenerateResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
        {
            SubCaller.OnPreGenerate(grid, preset, callFrom);
        }

    }
}