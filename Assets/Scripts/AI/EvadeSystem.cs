using Trackers;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using UnityEngine.AI;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BossStrategySystem))]
[RequireMatchingQueriesForUpdate]
public partial class EvadeSystem : SystemBase
{

    private uint _index = 1;
    protected override void OnUpdate()
    {
        var time = SystemAPI.Time.DeltaTime;
        var random = new Random();
        random.InitState(_index++);

        Entities.WithBurst().WithAny<EnemyComponent>().ForEach((
                Entity e,
                ref EvadeComponent evade,
                ref LocalTransform enemyTransform, ref DefensiveStrategyComponent defensiveStrategy) =>
            {
                var dodge = defensiveStrategy.currentRole == DefensiveRoles.Evade &&
                            SystemAPI.HasComponent<PhysicsVelocity>(e);
                var enemyState = MoveStates.Default;
                bool hasEnemyStateComponent = SystemAPI.HasComponent<EnemyStateComponent>(e);
                if (dodge && evade.EvadeMoveTimer <= evade.evadeMoveTime)
                {
                    if (evade.InEvade == false && hasEnemyStateComponent) //so no bosses
                    {
                        evade.startAnimation = true;
                    }

                    evade.InEvade = true;
                    evade.EvadeMoveTimer += time;
                    if (evade.EvadeMoveTimer > evade.evadeMoveTime)
                    {
                        if (hasEnemyStateComponent)
                        {
                            //animator.SetBool(evade1, false);
                            evade.startAnimation = false;
                        }

                        evade.agentStart = enemyTransform.Position;
                        var randomValue = random.NextFloat(evade.originalEvadeMoveSpeed * .2f,
                            evade.originalEvadeMoveSpeed);

                        evade.evadeMoveTime = evade.randomEvadeMoveTime
                            ? randomValue
                            : evade.originalEvadeMoveSpeed;

                        var addX = random.NextFloat(-1, 1);
                        float addZ = 0;
                        evade.addX = addX;
                        if (evade.zMovement)
                        {
                            addZ = random.NextFloat(-1f, 1f);
                            evade.addZ = addZ;
                        }

                        evade.InEvade = false;
                        evade.EvadeMoveTimer = 0;
                        defensiveStrategy.currentRole = DefensiveRoles.None;
                        enemyState = MoveStates.Idle;
                    }
                }

                dodge = evade.InEvade;
                if (dodge)
                {
                    var isAgent = SystemAPI.HasComponent<NavMeshAgentComponent>(e);
                    if (!isAgent)
                    {
                        enemyTransform.Position.x += evade.addX * time * evade.evadeMoveSpeed;
                        enemyTransform.Position.z += evade.addZ * time * evade.evadeMoveSpeed;
                    }
                    else
                    {
                        if (hasEnemyStateComponent)
                        {
                            var enemyStateComponent = SystemAPI.GetComponent<EnemyStateComponent>(e);
                            var isStopped = enemyStateComponent.MoveState == MoveStates.Stopped &&
                                            enemyState != MoveStates.Idle;
                            if (enemyState == MoveStates.Idle)
                            {
                                enemyStateComponent.MoveState = enemyState;
                                SystemAPI.SetComponent(e, enemyStateComponent);
                            }

                            var agent = SystemAPI.GetComponent<NavMeshAgentComponent>(e);
                            agent.isStopped = isStopped;
                            SystemAPI.SetComponent(e, agent);
                        }

                    }
                }
            }
        ).Schedule();


    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(EvadeSystem))]
[RequireMatchingQueriesForUpdate]
public partial class EvadeManagedSystem : SystemBase
{
    private static readonly int Evade1 = Animator.StringToHash("Evade");
    
    protected override void OnUpdate()
    {
        var time = SystemAPI.Time.DeltaTime;
        
        //animate evade
        Entities.WithoutBurst().WithAny<EnemyComponent>().ForEach((Entity e, Animator animator,
                ref EvadeComponent evade) =>
        {
            animator.SetBool(Evade1, evade.startAnimation);
        }
        ).Run();

        Entities.WithoutBurst().WithAny<EnemyComponent>().ForEach((Entity e, NavMeshAgent agent,
                in EvadeComponent evade, in NavMeshAgentComponent agentComponent) =>
        {
            if ((agent.hasPath || agentComponent.isStopped) && evade.InEvade)
            {
                var forward = agent.transform.forward;
                float3 agentPosition = forward;
                float3 agentTarget = forward * time * evade.evadeMoveSpeed;
                agentTarget.x += evade.addX;
                agentTarget.z += evade.addZ;
                var next = math.lerp(agentPosition, agentTarget, evade.evadeMoveSpeed * 1 / 60);
                var offset = agentPosition - next;
                agent.speed = agentComponent.agentSpeed;
                agent.Move(-offset);
            }

        }
        ).Run();
    }
}