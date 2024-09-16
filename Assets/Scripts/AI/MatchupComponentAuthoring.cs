using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct MatchupComponent : IComponentData
{
    public float currentStrikeDistanceAdjustment;
    public float currentStrikeDistanceZoneBegin;
    public bool matchupClosest;
    public bool leader;
    public float AngleRadians;
    public float ViewDistanceSQ;
    public bool View360;
    public Entity closestOpponentEntity;//player or enemy who can attack enemies
    public Entity closestEnemyEntity;//player only
    public bool validTarget;
    public float lookAtDistance;
    public float closestDistance;
    public bool lookAt;
    public float3 targetZone;
    public float3 wayPointTargetPosition;
    public Entity targetEntity;
    public LocalTransform aimTarget;
    public bool isWaypointTarget;
    public float3 opponentTargetPosition;
    //public float3 playerTargetPosition;
}




public class MatchupComponentAuthoring : MonoBehaviour
{
    public bool matchupClosest = true;
    public bool leader;

    public float AngleRadians = 180;
    public float ViewDistanceSQ = 100;

    public bool View360;


    class MatchupBaker : Baker<MatchupComponentAuthoring>
    {
        public override void Bake(MatchupComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e,
                new MatchupComponent
                {
                    matchupClosest = authoring.matchupClosest, leader = authoring.leader,
                    AngleRadians = authoring.AngleRadians, ViewDistanceSQ = authoring.ViewDistanceSQ,
                    View360 = authoring.View360,
                });

            
        }
    }
   
}
