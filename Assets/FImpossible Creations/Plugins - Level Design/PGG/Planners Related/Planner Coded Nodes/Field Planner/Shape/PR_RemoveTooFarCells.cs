using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Shape
{

    public class PR_RemoveTooFarCells : PlannerRuleBase
    {
        public override string GetDisplayName( float maxWidth = 120 ) { return wasCreated ? "   Remove Too Far Cells" : "Remove Too Far Cells"; }
        public override string GetNodeTooltipDescription { get { return "Removing cells outside provided area."; } }
        public override Color GetNodeColor() { return new Color( 1.0f, 0.75f, 0.25f, 0.9f ); }
        public override bool IsFoldable { get { return false; } }

        public override Vector2 NodeSize { get { return new Vector2( LimitShape == ELimitShape.Rectangle ? 280 : 220, ( LimitShape == ELimitShape.Rectangle ? 144 : 124 ) ); } }

        [Port( EPortPinType.Input, 1 )] public PGGPlannerPort ApplyTo;

        public enum ELimitShape { Rectangle, Radius, Box, Bounds }
        public ELimitShape LimitShape = ELimitShape.Rectangle;

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }

        /// <summary> Used for rectangle limit </summary>
        public Vector3 MinRange = new Vector3( -10f, -10f, -10f );
        public Vector3 MaxRange = new Vector3( 10f, 10f, 10f );

        /// <summary> Used for radius and bounds multiplication </summary>
        public float Range = 10f;

        [Port( EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.NotEditable, 1 )] public PGGUniversalPort Bounds;

        public override void Execute( PlanGenerationPrint print, PlannerResult newResult )
        {
            ApplyTo.TriggerReadPort( true );

            FieldPlanner plan = GetPlannerFromPort( ApplyTo, false );

            CheckerField3D checker = ApplyTo.GetInputCheckerSafe;
            if( plan ) checker = plan.LatestResult.Checker;

            if( checker == null ) { return; }

            if( LimitShape == ELimitShape.Rectangle || LimitShape == ELimitShape.Box )
            {
                // Measure using rectangle shape in checker local space
                Vector3 min;
                Vector3 max;

                if( LimitShape == ELimitShape.Rectangle )
                {
                    min = Vector3.Scale( MinRange, checker.RootScale );
                    max = Vector3.Scale( MaxRange, checker.RootScale );
                }
                else // Box
                {
                    min = Vector3.one * -Range;
                    max = Vector3.one * Range;
                }

                for( int i = checker.AllCells.Count - 1; i >= 0; i-- )
                {
                    var cell = checker.AllCells[i];
                    if( cell.Pos.x < min.x ) checker.RemoveLocal( cell.Pos );
                    else if( cell.Pos.y < min.y ) checker.RemoveLocal( cell.Pos );
                    else if( cell.Pos.z < min.z ) checker.RemoveLocal( cell.Pos );
                    else if( cell.Pos.x > max.x ) checker.RemoveLocal( cell.Pos );
                    else if( cell.Pos.y > max.y ) checker.RemoveLocal( cell.Pos );
                    else if( cell.Pos.z > max.z ) checker.RemoveLocal( cell.Pos );
                }
            }
            else if( LimitShape == ELimitShape.Radius )
            {
                float radius = Range;
                radius *= radius; // For faster sqrt measure

                for( int i = checker.AllCells.Count - 1; i >= 0; i-- )
                {
                    var cell = checker.AllCells[i];
                    if( Vector3.SqrMagnitude( cell.Pos ) > radius ) checker.AllCells.RemoveAt( i );
                }
            }
            else if( LimitShape == ELimitShape.Bounds )
            {
                Bounds.TriggerReadPort( true );
                object val = Bounds.GetPortValueSafe;
                Bounds b = PGGUniversalPort.TryReadAsBounds( val );
                if( b.size == Vector3.zero ) return;

                if( _EditorDebugMode ) FDebug.DrawBounds3D( b, Color.red, 1f );

                for( int i = checker.AllCells.Count - 1; i >= 0; i-- )
                {
                    var cell = checker.AllCells[i];
                    if( !b.Contains( checker.LocalToWorld( cell.Pos ) ) ) checker.AllCells.RemoveAt( i );
                }
            }

        }

#if UNITY_EDITOR

        private UnityEditor.SerializedProperty sp = null;
        private UnityEditor.SerializedProperty sp_MinRange = null;
        private UnityEditor.SerializedProperty sp_Bounds = null;
        private UnityEditor.SerializedProperty sp_Range = null;
        public override void Editor_OnNodeBodyGUI( ScriptableObject setup )
        {
            baseSerializedObject.Update();

            if( sp == null ) sp = baseSerializedObject.FindProperty( "ApplyTo" );

            UnityEditor.SerializedProperty scp = sp.Copy();
            UnityEditor.EditorGUILayout.PropertyField( scp ); scp.Next( false );
            EditorGUIUtility.labelWidth = 80;
            UnityEditor.EditorGUILayout.PropertyField( scp ); scp.Next( false );
            EditorGUIUtility.labelWidth = 0;

            if( sp_MinRange == null ) sp_MinRange = baseSerializedObject.FindProperty( "MinRange" );
            if( sp_Range == null ) sp_Range = baseSerializedObject.FindProperty( "Range" );
            var spm = sp_MinRange.Copy();

            if( LimitShape == ELimitShape.Rectangle )
            {
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField( spm ); spm.Next( false );
                EditorGUILayout.PropertyField( spm );
                EditorGUIUtility.labelWidth = 0;
            }
            else if( LimitShape == ELimitShape.Radius )
            {
                EditorGUILayout.PropertyField( sp_Range, new GUIContent( "Max Distance Radius:" ) );
            }
            else if( LimitShape == ELimitShape.Box )
            {
                EditorGUILayout.PropertyField( sp_Range, new GUIContent( "Bounds Volume Multiply:" ) );
            }

            if( LimitShape == ELimitShape.Bounds )
            {
                Bounds.AllowDragWire = true;
                if( sp_Bounds == null ) sp_Bounds = baseSerializedObject.FindProperty( "Bounds" );
                EditorGUILayout.PropertyField( sp_Bounds );
            }
            else
            {
                Bounds.AllowDragWire = false;
            }

            baseSerializedObject.ApplyModifiedProperties();
        }

#endif

    }
}