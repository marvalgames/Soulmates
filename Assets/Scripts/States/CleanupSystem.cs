using Collisions;
using Unity.Physics;
using Unity.Entities;
using Sandbox.Player;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(Collisions.CollisionSystem))]

[RequireMatchingQueriesForUpdate]
public partial class CleanupSystem : SystemBase
{

    //private EndSimulationEntityCommandBufferSystem ecbSystem;

    protected override void OnUpdate()
    {

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        //Clean up ... Move to DestroySystem
        var dep0 = Entities.ForEach
        (
            (Entity e, ref DamageComponent damageComponent) =>
            {
                ecb.RemoveComponent<DamageComponent>(e);

            }
        ).Schedule(this.Dependency);
        dep0.Complete();


        var dep1 = Entities.ForEach
        (
            (Entity e, ref CollisionComponent collisionComponent) =>
            {
                ecb.RemoveComponent<CollisionComponent>(e);
                //Debug.Log("destroy collision from ch ef sys");

            }
        ).Schedule(this.Dependency);
        dep1.Complete();
        
        var dep2 = Entities.ForEach
        (
            (ref ScoreComponent scoreComponent) =>
            {
                scoreComponent.lastPointValue = 0;
            }
        ).Schedule(this.Dependency);
        dep2.Complete();


        ecb.Playback(EntityManager);
        ecb.Dispose();



    }



}




