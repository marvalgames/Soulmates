using FIMSpace.FEditor;
using FIMSpace.Generating.Checker;
using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class SG_PrefabToGrid : ShapeGeneratorBase
    {
        public override string TitleName() { return "Prefab to Grid"; }

        [Tooltip( "Scale up detected models bounds" )]
        public Vector3 MultiplyBoundScale = Vector3.one;

        [PGG_SingleLineTwoProperties( "OnlyZeroY" )]
        [Tooltip( "Calculate Y-Height Cells or use just flat 2D height cells" )]
        public bool ComputeYCells = false;
        [HideInInspector] public bool OnlyZeroY = false;
        //public bool OriginInCenter = false;
        [PGG_SingleLineTwoProperties( "UseRounding" )]
        [Tooltip( "Calculate grid as single rect covering whole model area" )]
        public bool BoundsToGrid = false;
        [HideInInspector]
        [Tooltip( "If models bounds are exceeding cells range, new cells will be generated accordingly to the exceeded area" )]
        public bool UseRounding = true;

        [Space( 4 )] public Vector3Int OffsetOrigin = Vector3Int.zero;

        //[HideInInspector] public GameObject Prefab = null;
        [HideInInspector] public bool DisplayPrefabField = true;
        [HideInInspector] public CustomBuildSpawnSetup CustomSpawn = new CustomBuildSpawnSetup();

        internal void SetDefaultPrefab( GameObject defaultPrefab )
        {
            if( CustomSpawn == null ) CustomSpawn = new CustomBuildSpawnSetup();
            CustomSpawn.SetSpawn( defaultPrefab );
        }


        public override CheckerField3D GetChecker( FieldPlanner planner )
        {
            CheckerField3D checker = new CheckerField3D();

            GameObject pr = planner.DefaultPrefab;

            if( planner.FieldType == FieldPlanner.EFieldType.Prefab )
            {
                if( CustomSpawn != null ) pr = CustomSpawn.ChooseTargetSpawn( FGenerators.GlobalRandomInstance );
            }

            if( pr == null ) return checker;
            planner.DefaultPrefab = pr;

            Vector3 prePos = pr.transform.position;
            pr.transform.position = Vector3.zero;

            checker.RootScale = planner.GetScale;
            Bounds fullBounds = new Bounds( Vector3.zero, Vector3.zero );
            bool wasAssigned = false;

            var allChildTr = pr.GetComponentsInChildren<Transform>();
            List<PlannerCommandIndicator> commands = new List<PlannerCommandIndicator>();

            foreach( var tr in allChildTr )
            {
                MeshRenderer rend = tr.GetComponent<MeshRenderer>();
                if( rend )
                {
                    if( tr.GetComponent<PlannerMeshIgnore>() == null )
                    {
                        if( wasAssigned == false ) { fullBounds = rend.bounds; wasAssigned = true; }
                        else fullBounds.Encapsulate( rend.bounds );

                        if( BoundsToGrid == false )
                        {
                            Bounds addB = rend.bounds;

                            if( OnlyZeroY )
                            {
                                addB.size = new Vector3( addB.size.x, 0.1f, addB.size.z );
                                addB.center = new Vector3( addB.center.x, 0.05f, addB.center.z );
                            }

                            addB.center += OffsetOrigin;

                            if( !ComputeYCells ) addB = new Bounds( new Vector3( addB.center.x, addB.min.y + checker.RootScale.y / 10f, addB.center.z ), new Vector3( addB.size.x, checker.RootScale.y * 0.75f, addB.size.z ) );
                            if( MultiplyBoundScale != Vector3.one ) addB.size = Vector3.Scale( MultiplyBoundScale, addB.size );
                            var cells = checker.BoundsToCells( checker.WorldToLocalBounds( addB ), UseRounding, true );
                            for( int c = 0; c < cells.Count; c++ ) { checker.AddLocal( cells[c].Pos ); }
                        }
                    }
                }

                PlannerCommandIndicator command = tr.GetComponent<PlannerCommandIndicator>();
                if( command != null ) commands.Add( command );
            }

            for( int c = 0; c < commands.Count; c++ )
            {
                var command = commands[c];
                var cell = checker.GetCellInWorldPos( command.transform.position, false );
                if( cell.Available() )
                {
                    var guide = AddCellIntruction(planner, cell, command.ID);
                    guide.HelperPlannerReference = planner;
                    guide.HelperCellRef = cell;
                    guide.rot = command.transform.rotation;
                    guide.UseDirection = command.RotationCommand;
                    guide.HelperString = command.HelperString;
                }
            }

            if( BoundsToGrid )
            {
                //FDebug.DrawBounds3D(fullBounds, Color.red, 1f);
                Bounds addB = fullBounds;
                if( !ComputeYCells ) addB = new Bounds( new Vector3( addB.center.x, addB.min.y + checker.RootScale.y / 10f, addB.center.z ), new Vector3( addB.size.x, checker.RootScale.y * 0.75f, addB.size.z ) );
                if( MultiplyBoundScale != Vector3.one ) addB.size = Vector3.Scale( MultiplyBoundScale, addB.size );

                if( OnlyZeroY )
                {
                    addB.size = new Vector3( addB.size.x, 0.1f, addB.size.z );
                    addB.center = new Vector3( addB.center.x, 0.05f, addB.center.z );
                }

                addB.center += OffsetOrigin;

                var cells = checker.BoundsToCells( checker.WorldToLocalBounds( addB ), UseRounding, true );
                for( int c = 0; c < cells.Count; c++ ) { checker.AddLocal( cells[c].Pos ); }
            }

            pr.transform.position = prePos;
            //if (OriginInCenter) checker.CenterizeOrigin();

            return checker;
        }

        public void RefreshPrefabsSpawnSetup( FieldPlanner planner )
        {
            if( CustomSpawn == null ) CustomSpawn = new CustomBuildSpawnSetup();
            CustomSpawn.RefreshSpawnList();
            if( CustomSpawn.GetSpawn() == null ) if( planner ) CustomSpawn.SetSpawn( planner.DefaultPrefab );
        }


#if UNITY_EDITOR

        SerializedProperty sp_Prefab = null;
        public override void DrawGUI( SerializedObject so, FieldPlanner parent )
        {
            PrefabsFieldTypeSelector( parent, true );

            base.DrawGUI( so, parent );

            if( DisplayPrefabField )
            {
                if( sp_Prefab == null ) sp_Prefab = so.FindProperty( "Prefab" );
                EditorGUILayout.PropertyField( sp_Prefab );
            }
        }

        GUIContent _rButtGC = null;
        internal bool PrefabsFieldTypeSelector( FieldPlanner planner, bool full = true )
        {
            RefreshPrefabsSpawnSetup( planner );

            EditorGUILayout.BeginHorizontal();
            if( full && CustomSpawn.MultipleSpawns )
            {
                if( GUILayout.Button( CustomSpawn._Foldout ? FGUI_Resources.GUIC_FoldGrayDown : FGUI_Resources.GUIC_FoldGrayRight, EditorStyles.label, GUILayout.Width( 20 ), GUILayout.Height( 18 ) ) ) CustomSpawn._Foldout = !CustomSpawn._Foldout;
            }

            bool changed = false;
            GameObject p = CustomSpawn.GetSpawn( 0 ) as GameObject;
            GameObject newObj = p;

            if( _rButtGC == null || _rButtGC.image == null ) _rButtGC = new GUIContent( "  Choose Random ", EditorGUIUtility.IconContent( "Prefab Icon" ).image );

            if( full )
            {
                newObj = CustomSpawn.Spawns[0].DisplayPropertyField( 140 ) as GameObject;// (GameObject)EditorGUILayout.ObjectField(PrefabsSpawn.GetSpawn(0), typeof(GameObject), false, GUILayout.MaxWidth(140));
                if( CustomSpawn.MultipleSpawns ) GUI.backgroundColor = Color.green; else { GUI.backgroundColor = Color.white; GUI.color = Color.white * 0.7f; }
                if( GUILayout.Button( _rButtGC, GUILayout.Height( 19 ) ) )
                {
                    CustomSpawn.MultipleSpawns = !CustomSpawn.MultipleSpawns;
                    if( CustomSpawn.MultipleSpawns ) CustomSpawn._Foldout = true;
                }

                if( CustomSpawn.Spawns.Count > 1 ) { EditorGUILayout.LabelField( "(" + CustomSpawn.Spawns.Count + ")", GUILayout.MaxWidth( 31 ) ); }

                EditorGUILayout.EndHorizontal();

                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;

                if( CustomSpawn.MultipleSpawns && CustomSpawn._Foldout )
                {
                    EditorGUI.indentLevel += 1;

                    // Randomization settings
                    int toRemove = -1;

                    for( int i = 0; i < CustomSpawn.Spawns.Count; i++ )
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField( "[" + i + "]", GUILayout.Width( 36 ) );
                        if( full ) CustomSpawn.Spawns[i].DisplayPropertyField();

                        GUILayout.Space( 6 );
                        EditorGUIUtility.labelWidth = 34;
                        CustomSpawn.Spawns[i].Probability = EditorGUILayout.Slider( new GUIContent( "  ", FGUI_Resources.Tex_Random, "Being chosen priority level, for controlled randomization. Higher value = chosen more often" ), CustomSpawn.Spawns[i].Probability, 0f, 1f );
                        EditorGUIUtility.labelWidth = 0;

                        if( i > 0 )
                        {
                            GUI.backgroundColor = new Color( 1f, 0.7f, 0.7f, 1f );
                            GUILayout.Space( 6 );
                            if( GUILayout.Button( FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Width( 22 ), GUILayout.Height( 18 ) ) ) toRemove = i;
                            GUI.backgroundColor = Color.white;
                        }
                        else
                        {
                            GUILayout.FlexibleSpace();
                            GUI.backgroundColor = Color.green;
                            if( GUILayout.Button( "+", FGUI_Resources.ButtonStyle, GUILayout.MaxWidth( 24 ), GUILayout.Height( 18 ) ) ) { CustomSpawn.Spawns.Add( new CustomBuildSpawnSetup.SpawnSet() ); changed = true; }
                            GUI.backgroundColor = Color.white;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel -= 1;

                    if( toRemove > 0 ) { CustomSpawn.Spawns.RemoveAt( toRemove ); changed = true; }
                }
            }
            else
            {
                EditorGUILayout.EndHorizontal();
                if( full ) newObj = CustomSpawn.Spawns[0].DisplayPropertyField( 1000 ) as GameObject;// (GameObject)EditorGUILayout.ObjectField(PrefabsSpawn.GetSpawn(0), typeof(GameObject), false, GUILayout.MaxWidth(140));
            }

            GUILayout.Space( 8 );

            if( p != newObj || changed ) return true;
            return false;
        }

#endif
    }
}