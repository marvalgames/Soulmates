#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    //[CreateAssetMenu] // Use to generate reference file
    public class SyncInstancesCountOperation : FieldPlannerOperationBase
    {
        public override string Description => "Sync instances count with other planner instances count.";

        public override void OnStartPrepareFieldPlannerMainInstance( BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper )
        {
            int id = helper.RequestVariable( "ID", -1 ).GetIntValue();
            if( id < 0 ) return;
            if( id >= build.BasePlanners.Count ) return;
            var otherPlanner = build.BasePlanners[id];
            if( otherPlanner == null ) return;
            planner.Instances = otherPlanner.Instances;
        }

#if UNITY_EDITOR

        public override void Editor_DisplayGUI( FieldPlannerOperationHelper helper, BuildPlannerPreset build, FieldPlanner planner )
        {
            var idV = helper.RequestVariable( "ID", -1 );

            if( build == null )
            {
                EditorGUILayout.HelpBox( "No build planner reference!", MessageType.Error );
                return;
            }

            int i = idV.GetIntValue();

            string name;
            if( i < 0 || i >= build.BasePlanners.Count ) name = "None";
            else name = build.BasePlanners[i].name;

            EditorGUILayout.BeginHorizontal();

            if( GUILayout.Button( "Sync Instances Count With:", EditorStyles.label, GUILayout.Width( 160 ) ) )
            {
                SelectorGenericMenu( helper, build, planner );
            }

            if( GUILayout.Button( name, EditorStyles.popup ) )
            {
                SelectorGenericMenu( helper, build, planner );
            }

            EditorGUILayout.EndHorizontal();
        }

        void SelectorGenericMenu( FieldPlannerOperationHelper helper, BuildPlannerPreset build, FieldPlanner planner )
        {
            GenericMenu menu = new GenericMenu();
            var idV = helper.RequestVariable( "ID", -1 );
            int i = idV.GetIntValue();

            menu.AddItem( new GUIContent( "None" ), i < 0, () => { idV.SetValue( -1 ); EditorUtility.SetDirty( planner ); } );

            for( int p = 0; p < build.BasePlanners.Count; p++ )
            {
                int tgtPlanner = p;
                menu.AddItem( new GUIContent( build.BasePlanners[p].name), i == p, () => { idV.SetValue( tgtPlanner ); EditorUtility.SetDirty( planner ); } );
            }

            menu.ShowAsContext();
        }

#endif

    }
}