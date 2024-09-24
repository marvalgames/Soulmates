using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemy
{
    [UpdateAfter(typeof(EnemyActorMovementSystem))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct EnemySetupMoveMeleeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (enemyAttackComponent, checkedComponent,
                         matchupComponent,
                         enemyLocalTransform, entity)
                     in SystemAPI
                         .Query<RefRW<EnemyStateComponent>, RefRW<CheckedComponent>, RefRO<MatchupComponent>,
                             RefRO<LocalTransform>>()
                         .WithEntityAccess().WithAll<EnemyComponent, MeleeComponent, DeadComponent>())
            {
                var enemyPosition = enemyLocalTransform.ValueRO.Position;
                var playerPosition = matchupComponent.ValueRO.wayPointTargetPosition;
                var dist = math.distance(playerPosition, enemyPosition);
                enemyAttackComponent.ValueRW.selectMoveUsing = false;
                if (enemyAttackComponent.ValueRW is { selectMove: true })
                {
                    enemyAttackComponent.ValueRW.selectMoveUsing = true;
                }
                else if (checkedComponent.ValueRW is { AttackStages: AttackStages.End, anyAttackStarted: false })
                    //AttackStarted set from animation - false at end so reset
                {
                    checkedComponent.ValueRW.totalAttempts += 1; //totalHits in AttackerSystem
                    checkedComponent.ValueRW.hitReceived = false;
                    checkedComponent.ValueRW.anyAttackStarted = false;
                    checkedComponent.ValueRW.AttackStages = AttackStages.No;
                }
            }
        }
    }

    [UpdateAfter(typeof(EnemySetupMoveMeleeSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemySelectMoveMeleeSystem : ISystem
    {
        private Unity.Mathematics.Random random;

        public void OnCreate(ref SystemState state)
        {
            // Seed the random generator (for example, with a frame count or a time-based value)
            random = new Unity.Mathematics.Random(65535);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (enemyState, entity) in SystemAPI.Query<RefRW<EnemyStateComponent>>().WithEntityAccess())
            {
                var movesList = SystemAPI.GetBufferLookup<MovesComponentElement>(true);
                enemyState.ValueRW.startMove = false;
                if (enemyState.ValueRW.selectMoveUsing)
                {
                    var combatAction = random.NextInt(0, movesList[entity].Length);
                    var moveUsing = movesList[entity];
                    var animationIndex = moveUsing[combatAction].animationType;
                    var primaryTrigger = moveUsing[combatAction].triggerType;
                    if (SystemAPI.HasComponent<CheckedComponent>(entity))
                    {
                        var defense = animationIndex == AnimationType.Deflect;
                        var checkedComponent = SystemAPI.GetComponent<CheckedComponent>(entity);
                        checkedComponent.anyDefenseStarted = defense;
                        checkedComponent.primaryTrigger = primaryTrigger;
                        checkedComponent.animationIndex = (int)animationIndex;
                        SystemAPI.SetComponent(entity, checkedComponent);
                        enemyState.ValueRW.startMove = true;
                        enemyState.ValueRW.animationIndex = animationIndex;
                        enemyState.ValueRW.triggerType = primaryTrigger;
                        enemyState.ValueRW.combatAction = combatAction;
                    }
                }
            }
        }
    }

    [UpdateAfter(typeof(EnemySelectMoveMeleeSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemySelectMoveManagedMeleeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (movesHolder, melee, entity)
                     in SystemAPI.Query<MovesClassHolder, RefRW<MeleeComponent>>().WithEntityAccess())
            {
                if (!melee.ValueRW.instantiated)
                {
                    var go = GameObject.Instantiate(movesHolder.meleeAudioSourcePrefab);
                    go.SetActive(true);
                    commandBuffer.AddComponent(entity, new MovesInstance { meleeAudioSourceInstance = go });
                    melee.ValueRW.instantiated = true;
                }
            }


            foreach (var (actor, movesHolder, movesInstance, enemyState, entity) in SystemAPI
                         .Query<ActorInstance, MovesClassHolder, MovesInstance, RefRW<EnemyStateComponent>>()
                         .WithEntityAccess())
            {
                var combatAction = Animator.StringToHash("CombatAction");
                var animationStage = Animator.StringToHash("State");
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                //var movesList = SystemAPI.GetBufferLookup<MovesComponentElement>(true);
                var audioClipElement = movesHolder.movesClassList;
                //var animatorState = animator.GetCurrentAnimatorStateInfo(0);
                enemyState.ValueRW.animationStage = (AnimationStage)animator.GetInteger(animationStage);
                var stage = enemyState.ValueRW.animationStage;

                if (stage == AnimationStage.Exit && enemyState.ValueRW.lastFrame == false)
                {
                    enemyState.ValueRW.lastFrame = true;
                    animator.SetInteger(animationStage, 0);
                    enemyState.ValueRW.animationStage = AnimationStage.None;
                }
                else
                {
                    enemyState.ValueRW.lastFrame = false;
                }

                
                if (enemyState.ValueRW.firstFrame)
                {
                    enemyState.ValueRW.firstFrame = false;
                }
                else if (stage == AnimationStage.Enter && enemyState.ValueRW.firstFrame == false)
                {
                    enemyState.ValueRW.firstFrame = true;
                }

                Debug.Log("Move ended " + enemyState.ValueRW.lastFrame);

                
                if (enemyState.ValueRW is
                    { startMove: true, firstFrame: true }) //check strike allowed always true for testing
                {
                    enemyState.ValueRW.enemyStrikeAllowed = false;
                    enemyState.ValueRW.animationStage = AnimationStage.Enter;
                    Debug.Log("Move started " + enemyState.ValueRW.firstFrame);
                    var animationIndex = enemyState.ValueRW.animationIndex;
                    var combatActionIndex = enemyState.ValueRW.combatAction;
                    var clip = audioClipElement[combatActionIndex].moveAudioClip;
                    var audioSource = movesInstance.meleeAudioSourceInstance.GetComponent<AudioSource>();
                    audioSource.PlayOneShot(clip);
                    animator.SetInteger(combatAction, (int)animationIndex);
                }
            }
        }
    }
}