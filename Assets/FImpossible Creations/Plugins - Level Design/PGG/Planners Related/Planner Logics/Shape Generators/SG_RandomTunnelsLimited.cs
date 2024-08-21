using FIMSpace.Generating.Checker;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class SG_RandomTunnelsLimited : SG_RandomTunnels
    {
        public override string TitleName() { return "Complex/Random Tunnels (Limited)"; }

        // Only this variables was added in comparison to source SG_RandomTunnels shape generator
        [Space( 5 )]
        public Vector3 MaxSize = new Vector3( 10, 10, 10 );
        public Vector3 MinSize = new Vector3( -10, -10, -10 );

        public override CheckerField3D GetChecker( FieldPlanner planner )
        {
            CheckerField3D checker = GetTunnel( null, null, BranchLength.GetRandom(), BranchThickness.GetRandom() );
            CheckerField3D initTunnel = checker.Copy();

            List<CheckerField3D> tunnels = new List<CheckerField3D>();
            tunnels.Add( initTunnel );

            Bounds limitsBounds = new Bounds( MinSize, Vector3.zero );
            limitsBounds.Encapsulate( MaxSize );

            //FDebug.DrawBounds3D( limitsBounds, Color.red );

            for( int i = 0; i < TargetBranches.GetRandom(); i++ )
            {
                CheckerField3D tunnel = null;

                for( int tries = 0; tries < 8; tries++ )
                {
                    tunnel = GetTunnel( tunnels[FGenerators.GetRandom( 0, tunnels.Count )], checker, BranchLength.GetRandom(), BranchThickness.GetRandom(), SeparationFactor, AvoidOverlaps/*, ThicknessSnap*/);

                    // Only this lines was added in comparison to source SG_RandomTunnels shape generator
                    if( tunnel != null )
                    {
                        Bounds tunnelBounds = tunnel.GetBasicBoundsLocal( false );
                        tunnelBounds.center += tunnel.RootPosition;
                        //FDebug.DrawBounds3D( tunnelBounds, Color.yellow );

                        if( ( limitsBounds.Contains( tunnelBounds.min ) && limitsBounds.Contains( tunnelBounds.max ) ) == false ) { tunnel = null; continue; }
                    }

                    if( tunnel != null ) break;
                }

                if( tunnel == null ) continue;

                if( tunnel.Bounding.Count > 0 ) checker.Bounding.Add( tunnel.Bounding[0] );

                checker.Join( tunnel );
                tunnels.Add( tunnel );

                RefreshPreview( checker );
            }

            checker.Bounding.Clear();

            return checker;
        }
    }
}