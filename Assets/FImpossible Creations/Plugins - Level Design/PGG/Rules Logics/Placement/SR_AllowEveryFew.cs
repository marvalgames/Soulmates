using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Rules.Placement
{
    public partial class SR_AllowEveryFew : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Allow Spawn Every Few"; }
        public override string Tooltip() { return "Allowing to spawn every few cells\n[Lightweight] " + base.Tooltip(); }
        public EProcedureType Type { get { return EProcedureType.Rule; } }

        public int OffsetX = 0;
        public int OffsetZ = 0;
        public int Every = 2;
        [Tooltip( "Checking if there are empty cells around to prevent using unnecessary axes" )]
        public bool CheckForWalls = false;

        public override void CheckRuleOn( FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null )
        {
            if( CheckForWalls )
            {
                bool xEmpty = false;
                if( !grid.GetCell( cell.Pos - new Vector3Int( 1, 0, 0 ) ).Available() ) { xEmpty = true; }
                else if( !grid.GetCell( cell.Pos + new Vector3Int( 1, 0, 0 ) ).Available() ) xEmpty = true;

                bool zEmpty = false;
                if( !grid.GetCell( cell.Pos - new Vector3Int( 0, 0, 1 ) ).Available() ) { zEmpty = true; }
                else if( !grid.GetCell( cell.Pos + new Vector3Int( 0, 0, 1 ) ).Available() ) zEmpty = true;

                if( zEmpty )
                {
                    if( ( cell.Pos.x + OffsetX ) % Every == 0 )
                    {
                        CellAllow = true;
                    }
                }
                else if( xEmpty )
                {
                    if( ( cell.Pos.z + OffsetZ ) % Every == 0 )
                    {
                        CellAllow = true;
                    }
                }

                return;
            }

            if( ( ( cell.Pos.x + OffsetX ) % Every == 0 ) && ( cell.Pos.z + OffsetZ ) % Every == 0 )
            {
                CellAllow = true;
            }
        }
    }
}