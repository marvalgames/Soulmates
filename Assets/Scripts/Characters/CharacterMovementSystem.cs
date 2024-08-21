using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[RequireMatchingQueriesForUpdate]
public partial class CharacterMovementSystem : SystemBase
{
    
    

    protected override void OnUpdate()
    {

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var npcQuery = GetEntityQuery(ComponentType.ReadOnly<NpcComponent>());
        var npcEntities = npcQuery.ToEntityArray(Allocator.TempJob);
        var npcs = npcEntities.Length;

        var dep = Entities.WithoutBurst().WithAny<PlayerComponent>().WithNone<NpcComponent>().ForEach((in Entity playerE,
                in LocalTransform playerTransform) =>
            {

                for (var i = 0; i < npcs; i++)
                {
                    var npcE = npcEntities[i];
                    var npcTransform = SystemAPI.GetComponent<LocalTransform>(npcE);
                    var switchDistance = SystemAPI.GetComponent<NpcComponent>(npcE).switchDistance;
                    var inAction = false;
                    if (SystemAPI.HasComponent<PlayerJumpComponent>(npcE))
                    {
                        var jump = SystemAPI.GetComponent<PlayerJumpComponent>(npcE);
                        inAction = jump.JumpStage != JumpStages.Ground;//is jumping
                        //ebug.Log("ACTION Jumping " + inAction);
                    }
                    if (SystemAPI.HasComponent<PlayerDashComponent>(npcE))
                    {
                        var dash = SystemAPI.GetComponent<PlayerDashComponent>(npcE);
                        inAction = dash.InDash;//is dashing
                        //Debug.Log("ACTION Dashing " + inAction);
                    }

                    if(math.distance(npcTransform.Position, playerTransform.Position) > switchDistance && !inAction)
                    {
                        if (!SystemAPI.HasComponent<NpcMovementComponent>(npcE)) 
                        {
                            ecb.AddComponent(npcE, new NpcMovementComponent { targetEntity = playerE } );
                        }
                    }
                    else
                    {
                        if (SystemAPI.HasComponent<NpcMovementComponent>(npcE))
                        {
                            ecb.RemoveComponent<NpcMovementComponent>(npcE);
                        }
                    }
                }

            }

        ).Schedule(this.Dependency);
        dep.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
        npcEntities.Dispose();
    }


    
    
}
