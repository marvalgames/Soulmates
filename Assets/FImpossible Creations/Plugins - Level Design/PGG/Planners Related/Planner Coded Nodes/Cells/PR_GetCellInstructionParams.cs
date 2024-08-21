using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Cells
{

    public class PR_GetCellInstructionParams : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Instruction Parameters" : "Get Cell Instruction Parameters"; }
        public override string GetNodeTooltipDescription { get { return "Accessing some parameters of provided cell instruction reference"; } }
        public override Color GetNodeColor() { return new Color(0.64f, 0.9f, 0.0f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(230, _EditorFoldout ? 180 : 140); } }
        public override bool DrawInputConnector { get { return listed != null; } }
        public override bool DrawOutputConnector { get { return listed != null; } }
        public override bool IsFoldable { get { return true; } }

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }

        [Port(EPortPinType.Input)] public PGGUniversalPort CellInstruction;
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.NotEditable)] public IntPort InstructionID;
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.NotEditable)] public PGGVector3Port Direction;
        [Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.NotEditable)] public PGGCellPort Cell;
        [HideInInspector][Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.NotEditable)] public PGGStringPort HelperString;
        [Tooltip("Transforming Direction Output with planner rotation if planner was provided")]
        [HideInInspector] public bool WorldDirection = false;

        List<SpawnInstructionGuide> listed = null;
        int listedI = 0;

        public override void PreGeneratePrepare()
        {
            listed = null;
            base.PreGeneratePrepare();
        }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            listed = null;
            base.Execute(print, newResult);
        }

        public override void OnStartReadingNode()
        {
            SpawnInstructionGuide guide = null;
            InstructionID.Value = 0;
            Cell.Clear();
            Direction.Value = Vector3.zero;
            HelperString.StringVal = "";

            if( listed == null )
            {
                CellInstruction.TriggerReadPort( true );
                object cellVal = CellInstruction.GetPortValueSafe;


                if( cellVal is SpawnInstructionGuide )
                {
                    guide = cellVal as SpawnInstructionGuide;
                }
                else if( cellVal is List<SpawnInstructionGuide> )
                {
                    listed = cellVal as List<SpawnInstructionGuide>;
                    guide = listed[0];
                }

                if( guide == null ) return;

                if( listed != null ) // re-execute for list support
                {
                    for( int i = 0; i < listed.Count; i++ )
                    {
                        listedI = i;
                        guide = listed[i];
                    }

                    for( int i = 0; i < listed.Count; i++ )
                    {
                        listedI = i;
                        guide = listed[i];

                        if( FirstOutputConnection != null )
                        {
                            InstructionID.Value = guide.Id;

                            Cell.ProvideFullCellData( guide.HelperCellRef, guide.HelperPlannerReference.LatestChecker, guide.HelperPlannerReference.LatestResult );

                            if( guide.UseDirection ) Direction.Value = guide.rot * Vector3.forward;
                            else Direction.Value = Vector3.zero;

                            HelperString.StringVal = guide.HelperString;

                            //UnityEngine.Debug.Log("exec on " + FirstOutputConnection.GetDisplayName() + " with id = " + guide.Id);
                            CallOtherExecution( FirstOutputConnection, null );
                        }
                    }

                    return;
                }

            }
            else // Listed call
            {
                if( listed.ContainsIndex( listedI ) ) guide = listed[listedI];
            }

            if( guide == null ) return;

            //UnityEngine.Debug.Log("listed[" + listedI + "] = " + guide.Id);
            InstructionID.Value = guide.Id;

            HelperString.StringVal = guide.HelperString;

            if( guide.HelperPlannerReference )
                Cell.ProvideFullCellData( guide.HelperCellRef, guide.HelperPlannerReference.LatestChecker, guide.HelperPlannerReference.LatestResult );
            else
                Cell.ProvideFullCellData( guide.HelperCellRef, null, null );

            if( guide.UseDirection )
            {
                if ( guide.HelperPlannerReference && WorldDirection)
                    Direction.Value = (guide.HelperPlannerReference.CheckerReference.RootRotation * guide.rot) * Vector3.forward;
                else
                    Direction.Value = guide.rot * Vector3.forward;
            }
            else
                Direction.Value = Vector3.zero;
        }

        public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        {
            // Call only on one of the ports read
            if (InstructionID.IsConnected && port != InstructionID) return;
            else if (InstructionID.IsConnected == false && Direction.IsConnected && port != Direction) return;

            // Listed switch on read
            if( listed == null )
            {
                CellInstruction.TriggerReadPort( true );
                object cellVal = CellInstruction.GetPortValueSafe;

                if( cellVal is List<SpawnInstructionGuide> )
                {
                    listed = cellVal as List<SpawnInstructionGuide>;
                }
            }
        }

#if UNITY_EDITOR

        SerializedProperty sp_HelperString = null;

        public override void Editor_OnNodeBodyGUI( ScriptableObject setup )
        {
            base.Editor_OnNodeBodyGUI( setup );

            if (_EditorFoldout)
            {
                baseSerializedObject.Update();

                if( sp_HelperString == null ) sp_HelperString = baseSerializedObject.FindProperty( "HelperString" );
                EditorGUILayout.PropertyField( sp_HelperString );
                var spc = sp_HelperString.Copy(); spc.Next( false );
                EditorGUILayout.PropertyField( spc );


                baseSerializedObject.ApplyModifiedProperties();
            }

        }

#endif

    }
}