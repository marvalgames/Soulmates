using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Player
{
    [RequireMatchingQueriesForUpdate]
    public partial class NpcMovementSystem : SystemBase
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");


        protected override void OnUpdate()
        {
          

            var time = SystemAPI.Time.DeltaTime;

            Entities.WithoutBurst().ForEach(
                (
                    Entity e,
                    NpcAgentClass npcAgentAI,
                    //PlayerMoveGameObjectClass playerMove,
                    Animator animator,
                    //in RatingsComponent ratingsComponent,
                    in NpcMovementComponent npcMovementComponent
                ) =>
                {
                    var agent = npcAgentAI.agent;
                    if (SystemAPI.HasComponent<Pause>(e) == true)
                    {
                        agent.speed = 0;
                        animator.speed = 0;
                        return;
                    }

                    animator.speed = 1;
                    
                    //Debug.Log("NPC " + agent.isOnNavMesh);
                    

                    if (!agent) return;
                    if (!agent.enabled || !agent.isOnNavMesh) return;
                    


                    var targetEntity = npcMovementComponent.targetEntity;
                    if (!SystemAPI.HasComponent<LocalTransform>(targetEntity)) return;
                    var transform = SystemAPI.GetComponent<LocalTransform>(e);
                    var target = SystemAPI.GetComponent<LocalTransform>(targetEntity);
                    agent.SetDestination(target.Position);
                    var lookDir = target.Position - transform.Position;
                    lookDir.y = 0;
                    //if (math.length(lookDir) < .002f) return;
                    var targetRotation = quaternion.LookRotationSafe(lookDir, math.up());
                    var rotation = transform.Rotation;
                    rotation = math.slerp(rotation, targetRotation, npcAgentAI.rotateSpeed * time);
                    agent.speed = npcAgentAI.moveSpeed * npcAgentAI.switchSpeedMultiplier;
                    animator.SetFloat(Vertical, 1);
                    transform.Position = agent.nextPosition;
                    transform.Rotation = rotation;
                    agent.transform.position = agent.nextPosition;
                }
            ).Run();
        }
    }
}