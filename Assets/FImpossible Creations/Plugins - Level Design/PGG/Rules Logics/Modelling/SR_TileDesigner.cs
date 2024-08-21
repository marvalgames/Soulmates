#if UNITY_EDITOR
using UnityEditor;
using FIMSpace.FEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Rules.Modelling
{
    public class SR_TileDesigner : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Tile Designer"; }
        public override string Tooltip() { return "Generating object to generate with tile designer"; }
        public EProcedureType Type { get { return EProcedureType.Event; } }
        //public EProcedureType Type { get { return EProcedureType.Coded; } }

        [HideInInspector] public TileDesign Design;
        private GameObject generatedDesign = null;

        [Tooltip("Temporary replace the prefab in 'ToSpawn' for next spawners to gather this tile design result instead of the target prefab")]
        [HideInInspector] public bool ReplacePrefabToSpawn = false;

        [HideInInspector] public TileDesignPreset ProjectPreset = null;
        [HideInInspector] public string DesignModifyCommand = "";
        [HideInInspector] public bool MutipleMeshes = false;

        TileDesign GetDesign { get { return ProjectPreset == null ? Design : ProjectPreset.BaseDesign; } }


        public override void PreGenerateResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
        {

            if (callFrom.TemporaryPrefabOverride != null)
            {
                return;
            }

            if (generatedDesign) { FGenerators.DestroyObject(generatedDesign); }

            if (Enabled == false) return;

            var Design = GetDesign;

            if (ProjectPreset != null)
            {
                if (string.IsNullOrWhiteSpace(DesignModifyCommand) == false)
                {
                    TileDesignPreset copied = Instantiate(ProjectPreset);
                    Design = copied.BaseDesign;
                    Design.ApplyCustomCommand(DesignModifyCommand);
                }
            }

            Design.FullGenerateStack();

            //if (MutipleMeshes) return;
            generatedDesign = Design.GeneratePrefab();
            generatedDesign.transform.position = new Vector3(10000, -10000, 10000);
            generatedDesign.hideFlags = HideFlags.HideAndDontSave;

            callFrom.SetTemporaryPrefabToSpawn(generatedDesign);

            if (ReplacePrefabToSpawn)
                if (callFrom.StampPrefabID >= 0)
                {
                    if (callFrom.Parent.PrefabsList.ContainsIndex(callFrom.StampPrefabID))
                    {
                        callFrom.Parent.PrefabsList[callFrom.StampPrefabID].TemporaryReplace(generatedDesign);
                    }
                }
        }

        public override void OnAddSpawnUsingRule(FieldModification mod, SpawnData spawn, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            base.OnAddSpawnUsingRule(mod, spawn, cell, grid);

            if (MutipleMeshes)
            {
                GetDesign.FullGenerateStack();
                spawn.Prefab = GetDesign.GeneratePrefab();
                spawn.Prefab.transform.position = new Vector3(10000, -10000, 10000);
                spawn.Prefab.hideFlags = HideFlags.HideAndDontSave;
            }
        }


        #region Editor GUI

#if UNITY_EDITOR

        void OpenDesignerWindow(bool quickEdit = false)
        {
            if (ProjectPreset)
                TileDesignerWindow.Init(Design, ProjectPreset, quickEdit);
            else
                TileDesignerWindow.Init(Design, this, quickEdit);
        }

        public override void NodeHeader()
        {
            var Design = GetDesign;
            base.NodeHeader();

            if (_editor_drawRule == false)
                if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_GearSetup), FGUI_Resources.ButtonStyle, GUILayout.Width(22), GUILayout.Height(20)))
                {
                    OpenDesignerWindow(true);
                }
        }


        SerializedProperty sp_mat;
        SerializedProperty sp_mat2;
        SerializedProperty sp_ReplacePrefabToSpawn;
        SerializedProperty sp_ProjectPreset;
        SerializedProperty sp_MutipleMeshes;
        SerializedObject so_ScrPreset;

        public override void NodeBody(SerializedObject so)
        {
            var Design = GetDesign;

            if (Design != null)
            {
                if (Design.DesignName == "New Tile")
                {
                    Design.DesignName = OwnerSpawner.Name;
                    EditorUtility.SetDirty(this);
                }
            }

            if (sp_ProjectPreset == null) sp_ProjectPreset = so.FindProperty("ProjectPreset");

            EditorGUILayout.HelpBox(" Replacing object to spawn with Tile Design", MessageType.Info);
            base.NodeBody(so);

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("  Open Tile Designer", FGUI_Resources.Tex_GearSetup), FGUI_Resources.ButtonStyle, GUILayout.Height(24)))
            {
                    OpenDesignerWindow();
            }

            if (Design.TileMeshes.Count == 1)
            {
                if (GUILayout.Button("Quick Edit", FGUI_Resources.ButtonStyle, GUILayout.Width(72), GUILayout.Height(24)))
                {
                    OpenDesignerWindow(true);
                }
            }

            GUILayout.EndHorizontal();

            if (Design._LatestGen_Meshes > 0)
            {

                EditorGUILayout.LabelField("Target mesh tris: " + Design._LatestGen_Tris, EditorStyles.centeredGreyMiniLabel);

                SerializedProperty sp_Design;

                if (ProjectPreset == null) sp_Design = so.FindProperty("Design");
                else
                {
                    if (so_ScrPreset == null || so_ScrPreset.targetObject != ProjectPreset) so_ScrPreset = new SerializedObject(ProjectPreset);
                    sp_Design = so_ScrPreset.FindProperty("Designs");
                    sp_Design = sp_Design.GetArrayElementAtIndex(0);
                }

                if (sp_mat == null) sp_mat = sp_Design.FindPropertyRelative("DefaultMaterial");
                SerializedProperty sp_draw = sp_mat;

                if (sp_mat.objectReferenceValue == null)
                {
                    if (Design.TileMeshes.Count > 0)
                    {
                        if (Design.TileMeshes[0].Material != null)
                        {
                            sp_draw = sp_Design.FindPropertyRelative("TileMeshes").GetArrayElementAtIndex(0).FindPropertyRelative("Material");
                        }
                    }
                }

                GUILayout.Space(3);
                EditorGUILayout.PropertyField(sp_draw);
                GUILayout.Space(3);

            }

            if (OwnerSpawner.StampPrefabID >= 0)
            {
                EditorGUIUtility.labelWidth = 170;
                if (sp_ReplacePrefabToSpawn == null) sp_ReplacePrefabToSpawn = so.FindProperty("ReplacePrefabToSpawn");
                EditorGUILayout.PropertyField(sp_ReplacePrefabToSpawn);
                EditorGUIUtility.labelWidth = 0;
            }
            else
            {
                ReplacePrefabToSpawn = false;
            }

            GUILayout.Space(4);
            EditorGUILayout.PropertyField(sp_ProjectPreset, new GUIContent("Optional Preset", sp_ProjectPreset.tooltip));
            
            if (ProjectPreset)
            {
                var spc = sp_ProjectPreset.Copy(); spc.Next(false);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(spc);
                if (GUILayout.Button(FGUI_Resources.GUIC_Info, FGUI_Resources.ButtonStyle, GUILayout.Width(19), GUILayout.Height(16)))
                    EditorUtility.DisplayDialog("Tile Designer Commands", "You can write command to be handled during generating tile designer mesh:\n\n" + TileDesign.CommandsInfo, "Ok");
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(2);

            if (sp_MutipleMeshes == null) sp_MutipleMeshes = so.FindProperty("MutipleMeshes");
            EditorGUILayout.PropertyField(sp_MutipleMeshes);
            GUILayout.Space(4);

        }

        //public override void NodeFooter(SerializedObject so, FieldModification mod)
        //{
        //    if (ReplaceSelectedSpawn)
        //    {
        //        EditorGUILayout.HelpBox("All other spawners using same 'To Spawn' will generate Tile Design Object", MessageType.None);
        //    }

        //    base.NodeFooter(so, mod);
        //}

#endif

        #endregion

    }
}