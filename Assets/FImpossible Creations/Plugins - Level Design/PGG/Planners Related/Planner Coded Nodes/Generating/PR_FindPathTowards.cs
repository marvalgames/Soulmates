using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Checker;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Generating
{
    public class PR_FindPathTowards : PlannerRuleBase
    {
        public override string GetDisplayName( float maxWidth = 120 ) { return "Path Find Generate"; }
        public override string GetNodeTooltipDescription { get { return "Path find (A*) algorithm towards target position with collision detection. Supporting search towards Vector3 Position!"; } }
        public override bool IsFoldable { get { return true; } }
        public override Vector2 NodeSize { get { return new Vector2( 262, 172 + extraHeight ); } }
        public override Color GetNodeColor() { return new Color( 0.3f, 0.7f, .9f, 0.95f ); }

        [Port( EPortPinType.Input )] public PGGPlannerPort StartOn;
        [Port( EPortPinType.Input )] public PGGPlannerPort SearchTowards;
        [Port( EPortPinType.Input )] public PGGPlannerPort CollideWith;

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        [Port( EPortPinType.Output, EPortValueDisplay.HideValue )] public PGGPlannerPort PathShape;

        [HideInInspector][Port( EPortPinType.Output, EPortValueDisplay.HideValue, "Start On Cell:" )] public PGGCellPort From_StartCell;
        [HideInInspector][Port( EPortPinType.Output, EPortValueDisplay.HideValue, "PATH Start Cell:" )] public PGGCellPort Path_StartCell;
        [HideInInspector][Port( EPortPinType.Output, EPortValueDisplay.HideValue, "PATH End Cell:" )] public PGGCellPort Path_EndCell;
        [HideInInspector][Port( EPortPinType.Output, EPortValueDisplay.HideValue, "Towards End Cell:" )] public PGGCellPort Towards_EndCell;

        [Tooltip( "Base checker setup reference for the generated path (path scale, origin, rotation etc.). Leave empty if you want to base on graph's planner." )]
        [HideInInspector][Port( EPortPinType.Input, EPortValueDisplay.Default, "Path Base" )] public PGGPlannerPort PathBase;

        [Tooltip( "Enabling interpreting inputted Vector3 values into StartOn and SearchTowards values as target pathfind positions." )]
        [HideInInspector, SerializeField] protected bool PositionsMode = false;
        [Tooltip( "Removing path shape cells which are overlapping with start or target grids" )]
        [HideInInspector, SerializeField] protected bool RemoveOverlappingCells = true;

        [Tooltip( "Use to execute different doorway generation (From Start - Towards End) if path without shape was found. Without shape, so fields was aligning with each other, generating direct connection." )]
        [HideInInspector][Port( EPortPinType.Output, EPortValueDisplay.HideValue, "Towards End Cell:" )] public BoolPort SingleStepPath;

        [HideInInspector, SerializeField]
        protected Checker3DPathFindSetup PathfindSetup = new Checker3DPathFindSetup();


        public override int OutputConnectorsCount { get { return 2; } }
        public override string GetOutputHelperText( int outputId = 0 )
        {
            if( outputId == 0 ) return "On Fail";
            return "On Found";
        }

        public override int HotOutputConnectionIndex { get { return 1; } }
        public override int AllowedOutputConnectionIndex { get { return pathWasFound ? 1 : 0; } }


        bool pathWasFound = false;
        Vector3? searchTowardsPosition = null;


        protected virtual CheckerField3D CallPathfind( CheckerField3D baseChecker, CheckerField3D startChecker, CheckerField3D targetChecker, List<CheckerField3D> collisions, FieldPlanner aPlanner, FieldPlanner bPlanner, CheckerField3D overlapRemoveA = null, CheckerField3D overlapRemoveB = null )
        {
            return baseChecker.GeneratePathFindTowards( startChecker, targetChecker, collisions, PathfindSetup.ToCheckerFieldPathFindParams(), aPlanner, bPlanner, RemoveOverlappingCells, null,overlapRemoveA, overlapRemoveB );
        }

        public override void Execute( PlanGenerationPrint print, PlannerResult newResult )
        {

            #region Reseting on execution

            From_StartCell.Clear();
            Path_StartCell.Clear();
            Path_EndCell.Clear();
            Towards_EndCell.Clear();

            SingleStepPath.Value = false;
            CollideWith.Editor_DefaultValueInfo = "(None)";
            CollideWith.Switch_MinusOneReturnsMainField = false;
            searchTowardsPosition = null;

            #endregion


            #region Prepare for search, initial conditions, collision masks

            pathWasFound = false;

            StartOn.TriggerReadPort( true );
            SearchTowards.TriggerReadPort( true );
            CollideWith.TriggerReadPort( true );

            PathShape.Clear();
            PathShape.Switch_DisconnectedReturnsByID = false;

            FieldPlanner a;
            CheckerField3D aChecker = null;
            FieldPlanner b;
            CheckerField3D bChecker = null;

            a = GetPlannerFromPort( StartOn, false );
            b = GetPlannerFromPort( SearchTowards, false );

            var overlapRemA = aChecker;
            var overlapRemB = bChecker;

            if( a != null ) { aChecker = a.LatestChecker; overlapRemA = aChecker; }
            if( b != null ) { bChecker = b.LatestChecker; overlapRemB = bChecker; }

            if( PositionsMode )
            {
                object aValue = StartOn.GetInputValueSafe;
                if( aValue is Vector3 )
                {
                    Vector3 targetPosition = (Vector3)aValue;

                    aChecker = new CheckerField3D();
                    aChecker.CopyParamsFrom( newResult.Checker );
                    aChecker.RootPosition = targetPosition;
                    aChecker.AddLocal( Vector3.zero );

                    overlapRemA = new CheckerField3D();
                }

                object bValue = SearchTowards.GetInputValueSafe;
                if( bValue is Vector3 )
                {
                    Vector3 targetPosition = (Vector3)bValue;

                    bChecker = new CheckerField3D();
                    bChecker.CopyParamsFrom( newResult.Checker );
                    bChecker.RootPosition = targetPosition;
                    bChecker.AddLocal( Vector3.zero );

                    overlapRemB = new CheckerField3D();
                }
            }

            if( aChecker == null ) return;
            if( bChecker == null ) return;

            CheckerField3D bChec = null;

            if( SearchTowards.Connections.Count > 0 )
            {
                for( int c = 0; c < SearchTowards.Connections.Count; c++ )
                {
                    var conn = SearchTowards.Connections[c];

                    if( conn.PortReference.GetPortValue is Vector3 )
                    {
                        Vector3 pos = (Vector3)conn.PortReference.GetPortValue;
                        searchTowardsPosition = pos;

                        // Temporary checker to search for it
                        bChec = new CheckerField3D();
                        bChec.RootPosition = pos;
                        bChec.AddLocal( Vector3.zero );
                        break;
                    }
                    else
                    {
                        if( SearchTowards.Connections.Count == 1 )
                            if( conn.PortReference is PGGPlannerPort )
                            {
                                PGGPlannerPort plannerPrt = conn.PortReference as PGGPlannerPort;
                                if( plannerPrt.IsContaining_Null ) { return; }
                            }
                    }
                }
            }

            if( searchTowardsPosition != null )
            {
                SearchTowards.OverwriteName = "Towards POS" + searchTowardsPosition.Value.ToString();
                SearchTowards.Editor_DefaultValueInfo = "";
            }
            else
            {
                SearchTowards.OverwriteName = "";
            }

            if( bChec == null ) bChec = b.LatestChecker;

            List<FieldPlanner> coll = GetPlannersFromPort( CollideWith, false, false );
            List<CheckerField3D> masks = new List<CheckerField3D>();

            for( int c = 0; c < coll.Count; c++ )
            {
                masks.Add( coll[c].LatestChecker );
            }

            //masks.Remove(bChec);

            #endregion


            PlannerResult coreResult = newResult;
            CheckerField3D coreChecker = newResult.Checker;

            if( PathfindSetup.IgnoreSelfCollision ) masks.Remove( coreChecker );


            #region Defining Path Base


            if( PathBase.IsNotConnected )
            {
                if( PathBase.UniquePlannerID >= 0 )
                {
                    var newPlanner = GetPlannerFromPort( PathBase, false );
                    if( newPlanner )
                    {
                        if( newPlanner.LatestResult != null )
                        {
                            coreChecker = newPlanner.LatestChecker;
                            coreResult = newPlanner.LatestResult;
                        }
                    }
                }
                else
                {
                    //if (ParentPlanner)
                    {
                        coreResult = CurrentExecutingPlanner.LatestResult;
                        coreChecker = coreResult.Checker;
                    }
                }
            }
            else
            {
                PathBase.TriggerReadPort( true );

                FieldPlanner newPlanner = GetPlannerFromPort( PathBase, false );

                if( newPlanner )
                {
                    if( newPlanner.LatestResult != null )
                    {
                        coreChecker = newPlanner.LatestChecker;
                        coreResult = newPlanner.LatestResult;
                    }
                }
            }

            #endregion


            if( _EditorDebugMode ) CheckerField3D.DebugHelper = true;

            var generateFrom = aChecker;


            var path = CallPathfind( coreChecker, aChecker, bChec, masks, a, b, overlapRemA, overlapRemB );

            if( _EditorDebugMode ) CheckerField3D.DebugHelper = false;

            if( path != null )
            {
                //coreChecker = path;

                FieldCell cell = coreChecker._GeneratePathFindTowards_FromStartCell; // Start checker cell
                if( cell != null ) From_StartCell.ProvideFullCellData( cell, generateFrom, a == null ? newResult : a.LatestResult );


                //path.DebugLogDrawCellInWorldSpace(path.GetCell(0), Color.red);
                //if (path.AllCells.Count > 0)
                {

                    cell = coreChecker._GeneratePathFindTowards_PathBeginCell;
                    if( cell != null )
                    {
                        //FieldCell pathCell = new FieldCell();
                        //pathCell.Pos = coreChecker.WorldToLocal(path.LocalToWorld(cell.Pos)).V3toV3Int();
                        //Path_StartCell.ProvideFullCellData(pathCell, coreChecker, coreResult);
                        Path_StartCell.ProvideFullCellData( cell, coreChecker, coreResult );
                    }

                    cell = coreChecker._GeneratePathFindTowards_PathEndCell;
                    if( cell != null )
                    {
                        //FieldCell pathCell = cell;// new FieldCell();
                        //pathCell.Pos = coreChecker.WorldToLocal(path.LocalToWorld(cell.Pos)).V3toV3Int();
                        Path_EndCell.ProvideFullCellData( cell, coreChecker, coreResult );
                    }

                }

                cell = coreChecker._GeneratePathFindTowards_OtherTargetCell; // End checker cell
                if( cell != null ) Towards_EndCell.ProvideFullCellData( cell, bChecker, b == null ? null : b.LatestResult );


                if( path.AllCells.Count == 0 ) // else -> zero cells path (aligning checkers)
                {
                    SingleStepPath.Value = true;
                    // alternate outputs to avoid direction conflicts
                    //cell = coreChecker._GeneratePathFindTowards_OtherTargetCell; // End checker cell
                    //if (cell != null) Path_StartCell.ProvideFullCellData(cell, b.LatestChecker, b.LatestResult);

                    // If using all connections, don't provide data here to avoid inconsistences
                    if( ( From_StartCell.IsConnected && Path_StartCell.IsConnected && Path_EndCell.IsConnected ) || SingleStepPath.IsConnected )
                    {

                    }
                    else
                    {
                        if( cell != null ) Path_StartCell.ProvideFullCellData( cell, b.LatestChecker, b.LatestResult );
                    }

                    //Towards_EndCell.Clear();

                    //// Adjust start cell if moved far from start
                    //if (Path_StartCell.Checker != null && From_StartCell.Checker != null)
                    //{
                    //    for (int d = 0; d < findpa; d++)
                    //    {

                    //    }

                    //    UnityEngine.Debug.DrawLine(Path_StartCell.Checker.GetWorldPos(Path_StartCell.Cell), From_StartCell.Checker.GetWorldPos(From_StartCell.Cell), Color.green, 1.01f);
                    //}
                }


                PathShape.Output_Provide_Checker( path );

                pathWasFound = true;

            }

            #region Debugging Gizmos

#if UNITY_EDITOR

            if( Debugging )
            {
                if( a != null )
                    DebuggingInfo = "Generating path from " + a.name + "(" + a.ArrayNameString + ") " + " towards " + b.name + "(" + b.ArrayNameString + ")";
                else
                    DebuggingInfo = "Generating path from " + aChecker.RootPosition + " towards " + bChecker.RootPosition;

                if( path == null )
                {
                    DebuggingInfo += " NOT FOUND!";
                    return;
                }

                Bounds ba = aChecker.GetFullBoundsWorldSpace();
                Bounds bb = bChecker.GetFullBoundsWorldSpace();

                CheckerField3D pathChe = path;

                DebuggingGizmoEvent = () =>
                {
                    Gizmos.color = new Color( 0f, 1f, 0f, 0.5f );
                    Gizmos.DrawLine( ba.center, bb.center );
                    Gizmos.DrawWireCube( ba.center, ba.size );
                    Gizmos.color = new Color( .2f, .4f, 1f, 0.76f );
                    Gizmos.DrawWireCube( bb.center, bb.size );

                    pathChe.DrawFieldGizmos( true, false );
                };
            }
#endif
            #endregion

        }



        #region Editor View Code

        int extraHeight = 0;

#if UNITY_EDITOR

        UnityEditor.SerializedProperty spCell = null;
        GUIContent _guic = null;
        bool displayExtra = false;

        bool _wasCheckingParent = false;
        bool _isNotPre = false;

        public override void Editor_OnNodeBodyGUI( ScriptableObject setup )
        {
            base.Editor_OnNodeBodyGUI( setup );

            EditorGUILayout.BeginHorizontal();
            if( GUILayout.Button( "Display Path Find Setup" ) ) PathSettingsWindow.Init( this, PathfindSetup );

            if( _EditorFoldout )
            {
                if( _guic == null || _guic.image == null ) _guic = new GUIContent( FGUI_Resources.Tex_Module, "Click to display extra settings" );
                if( displayExtra ) GUI.backgroundColor = Color.green;
                if( GUILayout.Button( _guic, FGUI_Resources.ButtonStyle, GUILayout.Width( 24 ), GUILayout.Height( 18 ) ) ) { displayExtra = !displayExtra; }
                if( displayExtra ) GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            if( searchTowardsPosition == null )
                CollideWith.Editor_DefaultValueInfo = "(None)";
            else
                CollideWith.Editor_DefaultValueInfo = "";

            baseSerializedObject.Update();

            extraHeight = 0;
            SingleStepPath.AllowDragWire = false;

            if( _EditorFoldout )
            {
                extraHeight += 84;

                GUILayout.Space( 2 );
                if( spCell == null ) spCell = baseSerializedObject.FindProperty( "From_StartCell" );
                EditorGUILayout.PropertyField( spCell );
                var spc = spCell.Copy();
                spc.Next( false ); EditorGUILayout.PropertyField( spc );
                spc.Next( false ); EditorGUILayout.PropertyField( spc );
                spc.Next( false ); EditorGUILayout.PropertyField( spc );

                Editor_PathCellsWireAllow( true );

                if( displayExtra )
                {
                    extraHeight += 68;
                    GUILayout.Space( 8 );
                    spc.Next( false ); EditorGUILayout.PropertyField( spc );
                    spc.Next( false ); EditorGUILayout.PropertyField( spc );
                    spc.Next( false ); EditorGUILayout.PropertyField( spc );
                }
            }
            else
            {
                Editor_PathCellsWireAllow( false );
            }


            if( !_wasCheckingParent )
            {
                if( ParentPlanner )
                {
                    if( ParentPlanner.Procedures.Contains( this ) ) _isNotPre = true;
                    else
                    {
                        if( ParentPlanner.FSubGraphs != null )
                            for( int subs = 0; subs < ParentPlanner.FSubGraphs.Count; subs++ )
                            {
                                if( ParentPlanner.FSubGraphs[subs].ExecutionOrder == FieldPlanner.SubGraph.EExecutionOrder.AfterEachInstance )
                                    if( ParentPlanner.FSubGraphs[subs].Procedures.Contains( this ) )
                                    {
                                        _isNotPre = true;
                                    }
                            }
                    }
                }

                _wasCheckingParent = true;
            }

            if( _isNotPre )
            {
                extraHeight += 30;
                if( GUILayout.Button( "! Reminder ! Should this path-find be called in Post Procedures?", EditorStyles.helpBox ) )
                {
                    _isNotPre = false;
                }
                //EditorGUILayout.HelpBox("Reminder: Should this path-find be called in Post Procedures?", MessageType.None);
            }

            baseSerializedObject.ApplyModifiedProperties();
        }


        void Editor_PathCellsWireAllow( bool allow )
        {
            From_StartCell.AllowDragWire = allow;
            Path_StartCell.AllowDragWire = allow;
            Path_EndCell.AllowDragWire = allow;
            Towards_EndCell.AllowDragWire = allow;
        }


#endif

        #endregion


    }

}