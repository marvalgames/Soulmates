using Audio;
using Collisions;
using Rukhanka;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemy
{
    //[UpdateAfter(typeof(EnemyActorMovementSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
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
                //enemyAttackComponent.ValueRW.selectMoveUsing = false;
                //if (enemyAttackComponent.ValueRW is { selectMove: true, selectMoveUsing: false })
                //{
                //  enemyAttackComponent.ValueRW.selectMoveUsing = true;
                //}
                //else if (checkedComponent.ValueRW is { AttackStages: AttackStages.End, anyAttackStarted: false })
                //AttackStarted set from animation - false at end so reset
                //{
                //  checkedComponent.ValueRW.totalAttempts += 1; //totalHits in AttackerSystem
                //checkedComponent.ValueRW.hitReceived = false;
                //checkedComponent.ValueRW.anyAttackStarted = false;
                //checkedComponent.ValueRW.AttackStages = AttackStages.No;
                //}
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
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
                if (movesList[entity].Length == 0) continue;

                if (enemyState.ValueRW is { selectMove: true, startMove: false })
                {
                    var combatAction = random.NextInt(0, movesList[entity].Length);
                    var moveUsing = movesList[entity];
                    var animationIndex = moveUsing[combatAction].animationType;
                    var primaryTrigger = moveUsing[combatAction].triggerType;
                    enemyState.ValueRW.startMove = true;
                    if (SystemAPI.HasComponent<CheckedComponent>(entity))
                    {
                        var defense = animationIndex == AnimationType.Deflect;
                        var checkedComponent = SystemAPI.GetComponent<CheckedComponent>(entity);
                        checkedComponent.anyDefenseStarted = defense;
                        checkedComponent.primaryTrigger = primaryTrigger;
                        checkedComponent.animationIndex = (int)animationIndex;
                        checkedComponent.hitTriggered = false;
                        SystemAPI.SetComponent(entity, checkedComponent);
                        enemyState.ValueRW.animationIndex = animationIndex;
                        enemyState.ValueRW.triggerType = primaryTrigger;
                        enemyState.ValueRW.lastCombatAction = enemyState.ValueRW.combatAction;
                        enemyState.ValueRW.combatAction = combatAction;
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySelectMoveMeleeSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemySelectMoveManagedMeleeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (movesHolder, melee, entity)
                     in SystemAPI.Query<MovesClassHolder, RefRW<MeleeComponent>>().WithEntityAccess()
                         .WithAny<EnemyComponent>())
            {
                if (!melee.ValueRW.instantiated)
                {
                    var go = GameObject.Instantiate(movesHolder.meleeAudioSourcePrefab);
                    go.SetActive(true);
                    commandBuffer.AddComponent(entity, new MovesInstance { meleeAudioSourceInstance = go });
                    //Debug.Log("ENEMY INST " + melee.ValueRW.instantiated);
                    melee.ValueRW.instantiated = true;
                }
            }

            foreach (var (aces, checkedComponent, entity)
                     in SystemAPI.Query<DynamicBuffer<AnimatorControllerEventComponent>, RefRW<CheckedComponent>>()
                         .WithEntityAccess().WithAny<EnemyComponent>())
            {
                foreach (var ace in aces)
                {
                    if (ace.eventType == AnimatorControllerEventComponent.EventType.StateEnter)
                    {
                        checkedComponent.ValueRW.animationStage = AnimationStage.Enter;
                        //Debug.Log("STAGE ENTER " + AnimationStage.Enter);
                    }
                    else if (ace.eventType == AnimatorControllerEventComponent.EventType.StateUpdate)
                    {
                        checkedComponent.ValueRW.animationStage = AnimationStage.Update;
                        //Debug.Log("STAGE UPDATE " + AnimationStage.Update);
                    }
                    else if (ace.eventType == AnimatorControllerEventComponent.EventType.StateExit)
                    {
                        checkedComponent.ValueRW.animationStage = AnimationStage.Exit;
                        Debug.Log("STAGE EXIT " + AnimationStage.Exit);
                    }
                }
            }


            foreach (var (animatorStateQuery, animator, actor, audioClass, movesHolder, audio, entity) in SystemAPI
                         .Query<AnimatorStateQueryAspect, AnimatorParametersAspect, ActorInstance, AudioManagerClass,
                             MovesClassHolder,
                             RefRW<AudioManagerComponent>>()
                         .WithEntityAccess())
            {
                if (SystemAPI.HasComponent<EnemyStateComponent>(entity) == false ||
                    SystemAPI.HasComponent<CheckedComponent>(entity) == false) continue;
                var enemyState = SystemAPI.GetComponent<EnemyStateComponent>(entity);
                var checkedComponent = SystemAPI.GetComponent<CheckedComponent>(entity);
                var combatAction = new FastAnimatorParameter("CombatAction");
                var animationStage = new FastAnimatorParameter("State");
                var audioClipElement = movesHolder.movesClassList;
                //var animatorState = animator.GetCurrentAnimatorStateInfo(0);
                var animatorState = animatorStateQuery.GetLayerCurrentStateInfo(0);
                var animatorTransition = animatorStateQuery.GetLayerCurrentTransitionInfo(0);
                enemyState.animationStage = (AnimationStage)animator.GetIntParameter(animationStage);
                var stageTracker = SystemAPI.GetComponent<CheckedComponent>(entity).animationStage;
                audioClass.stage = stageTracker;
                if (stageTracker == AnimationStage.Exit && enemyState.lastFrame == false)
                {
                    enemyState.lastFrame = true;
                    animator.SetIntParameter(animationStage, 0);
                    enemyState.animationStage = AnimationStage.None;
                }
                else
                {
                    enemyState.lastFrame = false;
                }
                if (enemyState is
                    { startMove: true, firstFrame: false }) //check strike allowed always true for testing
                {
                    enemyState.firstFrame = true;
                    enemyState.selectMove = false;
                    enemyState.startMove = false;
                    enemyState.enemyStrikeAllowed = false;
                    enemyState.animationStage = AnimationStage.Enter;
                    stageTracker = AnimationStage.Enter;
                    var animationIndex = enemyState.animationIndex;
                    var clip = audioClipElement[enemyState.lastCombatAction].moveAudioClip;
                    audio.ValueRW.play = true;
                    audioClass.clip = clip;
                    Debug.Log("move " + animationIndex);
                    animator.SetIntParameter(combatAction, (int)animationIndex);
                }

                checkedComponent.animationStage = stageTracker;
                SystemAPI.SetComponent(entity, checkedComponent);
                SystemAPI.SetComponent(entity, enemyState);
            }
        }
    }
}