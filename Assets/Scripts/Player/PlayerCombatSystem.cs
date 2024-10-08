using Audio;
using Collisions;
using Enemy;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace Sandbox.Player
{
    //[UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerCombatSystem : ISystem
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        private static readonly int ComboAnimationPlayed = Animator.StringToHash("ComboAnimationPlayed");


        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actor, melee, checkedComponent, inputController, applyImpulse, e) in SystemAPI
                         .Query<ActorInstance, RefRW<MeleeComponent>, RefRW<CheckedComponent>,
                             RefRW<InputControllerComponent>, RefRW<ApplyImpulseComponent>>().WithEntityAccess())
            {
                //var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                //var playerCombat = actor.actorPrefabInstance.GetComponent<PlayerCombat>();
                //playerCombat.meleeEntity = e;
                //playerCombat.entityManager = state.EntityManager;
                var buttonXpressed = inputController.ValueRW.buttonX_Press; //kick types
                var buttonXtap = inputController.ValueRW.buttonX_Tap; //punch types
                var leftBumperPressed = inputController.ValueRW.leftBumperPressed;
                var leftBumperUp = inputController.ValueRW.leftBumperReleased;
                var allowKick = buttonXpressed &&
                                (melee.ValueRW.verticalSpeed < 2 || applyImpulse.ValueRO.Grounded == false);
                var buttonXunPressed = inputController.ValueRW.buttonTimeX_UnPressed;
                var comboBufferTimeMax = inputController.ValueRW.comboBufferTimeMax;
                //melee.ValueRW.selectMove = 0;

                if (buttonXtap &&
                    (checkedComponent.ValueRW is { comboIndexPlaying: 0, AttackStages: AttackStages.End } ||
                     checkedComponent.ValueRW.AttackStages == AttackStages.No))
                {
                    checkedComponent.ValueRW.comboIndexPlaying = 1;
                    inputController.ValueRW.comboBufferTimeStart = 0;
                    inputController.ValueRW.comboBufferTimeEnd = 0;
                    melee.ValueRW.selectMove = 1;
                    Debug.Log("Player Select xtap ");
                    //playerCombat.SelectMove(1);
                    //animator.SetInteger(ComboAnimationPlayed, 1);
                    melee.ValueRW.comboAnimationPlayed = 1;
                }
                else if (buttonXtap && checkedComponent.ValueRW is
                             { AttackStages: AttackStages.Action, comboIndexPlaying: >= 1 })
                {
                    checkedComponent.ValueRW.comboButtonClicked = true;
                }
                else if (allowKick) //kick
                {
                    melee.ValueRW.selectMove = 2;
                    //playerCombat.SelectMove(2);
                }
                else if (leftBumperPressed)
                {
                    melee.ValueRW.selectMove = 4;
                    Debug.Log("LEFT BUMPER DOWN");
                    //playerCombat.SelectMove(10);
                }
                else if (leftBumperUp)
                {
                    //animator.SetInteger(CombatAction, 0);
                    melee.ValueRW.selectMove = 0;
                    melee.ValueRW.cancelMove = true;
                    Debug.Log("LEFT BUMPER UP");
                }

                if (checkedComponent.ValueRW is { AttackStages: AttackStages.End, comboButtonClicked: true })
                {
                    checkedComponent.ValueRW.comboIndexPlaying += 1;
                    //animator.SetInteger(ComboAnimationPlayed, checkedComponent.ValueRW.comboIndexPlaying);
                    melee.ValueRW.comboAnimationPlayed = checkedComponent.ValueRW.comboIndexPlaying;
                    melee.ValueRW.selectMove = 1;
                    //playerCombat.SelectMove(1);
                    inputController.ValueRW.comboBufferTimeStart = 0;
                    checkedComponent.ValueRW.comboButtonClicked = false;
                }

                if (checkedComponent.ValueRW.AttackStages == AttackStages.End)
                {
                    if (inputController.ValueRW.comboBufferTimeStart == 0)
                    {
                        inputController.ValueRW.comboBufferTimeStart = buttonXunPressed;
                    }

                    inputController.ValueRW.comboBufferTimeEnd = buttonXunPressed;
                    var timeSincePressed =
                        inputController.ValueRW.comboBufferTimeEnd - inputController.ValueRW.comboBufferTimeStart;
                    if (timeSincePressed > comboBufferTimeMax || timeSincePressed < 0)
                    {
                        checkedComponent.ValueRW.comboIndexPlaying = 0;
                        //animator.SetInteger(ComboAnimationPlayed, 0);
                        melee.ValueRW.comboAnimationPlayed = 0;
                        checkedComponent.ValueRW.comboButtonClicked = false;
                    }
                }
            }
        }
    }


    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerCombatSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerCombatManagedSystem : ISystem
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        private static readonly int ComboAnimationPlayed = Animator.StringToHash("ComboAnimationPlayed");
        private static readonly int CombatMode = Animator.StringToHash("CombatMode");
        private static readonly int Zone = Animator.StringToHash("Zone");

        private MovesComponentElement moveUsing;
        private bool instantiated;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            instantiated = false;
        }


        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);


            if (instantiated == false)
            {
                foreach (var (actor, movesHolder, melee, entity)
                         in SystemAPI.Query<ActorInstance, MovesClassHolder, RefRW<MeleeComponent>>()
                             .WithEntityAccess().WithAny<PlayerComponent>())
                {
                    if (melee.ValueRW.instantiated) continue;
                    var movesList = SystemAPI.GetBufferLookup<MovesComponentElement>(true);
                    var count = movesList[entity].Length;
                    //if (count < movesHolder.moveCount) return; //hack
                    var go = GameObject.Instantiate(movesHolder.meleeAudioSourcePrefab);
                    go.SetActive(true);
                    commandBuffer.AddComponent(entity, new MovesInstance { meleeAudioSourceInstance = go });
                    var zone = actor.actorPrefabInstance.GetComponent<TargetZone>().headZone.transform;
                    var rhZone = actor.actorPrefabInstance.GetComponent<TargetZone>().rightHandZone.transform;
                    var rfZone = actor.actorPrefabInstance.GetComponent<TargetZone>().rightFootZone.transform;
                    var lhZone = actor.actorPrefabInstance.GetComponent<TargetZone>().leftHandZone.transform;
                    var lfZone = actor.actorPrefabInstance.GetComponent<TargetZone>().leftFootZone.transform;
                    for (var i = 0; i < count; i++)
                    {
                        
                        var movesClass = movesHolder.movesClassList[i];
                        var target = movesList[entity][i].triggerType;
                        switch (target)
                        {
                            case TriggerType.RightHand:
                                zone = rhZone;
                                Debug.Log("ZONE RH");
                                break;
                            case TriggerType.LeftHand:
                                zone = lhZone;
                                Debug.Log("ZONE LH");
                                break;
                            case TriggerType.RightFoot:
                                zone = rfZone;
                                Debug.Log("ZONE RF");
                                break;
                            case TriggerType.LeftFoot:
                                zone = lfZone;
                                Debug.Log("ZONE LF");
                                break;
                        }
                        var prefab = movesClass.moveParticleSystem;
                        var vfxGo = GameObject.Instantiate(prefab);
                        Debug.Log("PREFAB " + vfxGo);
                        movesClass.moveParticleSystemInstance = vfxGo;
                        //movesClass.moveParticleSystemInstance.transform.parent = actor.actorPrefabInstance.transform;
                        movesClass.moveParticleSystemInstance.transform.parent = zone;
                        movesClass.moveParticleSystemInstance.transform.localPosition = Vector3.zero;
                        if (movesClass.moveParticleSystemInstance.GetComponent<VisualEffect>())
                        {
                            movesClass.moveParticleSystemInstance.GetComponent<VisualEffect>().Stop();
                        }
                    }

                    //melee.ValueRW.instantiated = true;
                    instantiated = true;
                }
            }


            foreach (var (actor, movesHolder, audioClass, melee, checkedComponent, inputController, applyImpulse, e) in
                     SystemAPI
                         .Query<ActorInstance, MovesClassHolder, AudioManagerClass, RefRW<MeleeComponent>,
                             RefRW<CheckedComponent>,
                             RefRW<InputControllerComponent>, RefRW<ApplyImpulseComponent>>().WithEntityAccess()
                         .WithAny<PlayerComponent>())
            {
                //var playerCombat = actor.actorPrefabInstance.GetComponent<PlayerCombat>();
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();


                if (SystemAPI.HasComponent<ActorWeaponAimComponent>(e))
                {
                    var aimComponent = SystemAPI.GetComponent<ActorWeaponAimComponent>(e);
                    //Debug.Log("COMBAT MODE " + aimComponent.combatMode);
                    animator.SetInteger(Zone, aimComponent.combatMode ? 1 : 0);
                    animator.SetBool(CombatMode, aimComponent.combatMode);
                }


                if (melee.ValueRW.selectMove > 0)
                {
                    melee.ValueRW.cancelMovement = melee.ValueRW.selectMove == 4 ? 1 : .95f;
                    melee.ValueRW.lastCombatAction = melee.ValueRW.selectMove - 1;
                    SelectMove(e, actor, melee.ValueRW.selectMove, ref checkedComponent.ValueRW, ref state);
                    //Debug.Log("Player Select move " + melee.ValueRW.selectMove);
                    melee.ValueRW.verticalSpeed = animator.GetFloat(Vertical);
                    animator.SetInteger(ComboAnimationPlayed, melee.ValueRW.comboAnimationPlayed);
                    animator.SetInteger(CombatAction, melee.ValueRW.selectMove);
                    melee.ValueRW.selectMove = 0;
                }
                else if (melee.ValueRW.selectMove == 0)
                {
                    if (melee.ValueRW.cancelMove)
                    {
                        animator.SetInteger(CombatAction, 0);
                        melee.ValueRW.cancelMove = false;
                        melee.ValueRW.cancelMovement = 0;
                    }
                    
                    var vfxGraph = movesHolder.movesClassList[melee.ValueRW.lastCombatAction]
                        .moveParticleSystemInstance.GetComponent<VisualEffect>();


                    var stage = actor.actorPrefabInstance.GetComponent<ActorEntityTracker>().animationStageTracker;
                    if (stage == AnimationStage.Enter)
                    {
                        var audioClipElement = movesHolder.movesClassList;
                        var clip = audioClipElement[melee.ValueRW.lastCombatAction].moveAudioClip;
                        var audio = SystemAPI.GetComponent<AudioManagerComponent>(e);
                        audio.play = true;
                        audioClass.clip = clip;
                        SystemAPI.SetComponent(e, audio);
                        //play vfx code here but may change to pass to VfxManager similar to AudioManagerSystem
                        vfxGraph.Play();

                        //checkedComponent.anyAttackStarted = true;
                        Debug.Log("Start Attack SYSTEM");
                        checkedComponent.ValueRW.attackFirstFrame = true;
                        checkedComponent.ValueRW.AttackStages = AttackStages.Start;
                        checkedComponent.ValueRW.hitTriggered = false;
                    }
                    else if (stage == AnimationStage.Update)
                    {
                        checkedComponent.ValueRW.AttackStages = AttackStages.Action;
                        //play vfx code here but may change to pass to VfxManager similar to AudioManagerSystem
                        vfxGraph.Stop();
                        Debug.Log("Update Attack SYSTEM");
                    }
                    else if (stage == AnimationStage.Exit)
                    {
                        if (checkedComponent.ValueRW.AttackStages != AttackStages.End)
                        {
                            if (checkedComponent.ValueRW.hitTriggered == false &&
                                SystemAPI.HasComponent<ScoreComponent>(e))
                            {
                                var score = SystemAPI.GetComponent<ScoreComponent>(e);
                                score.combo = 0;
                                score.streak = 0;
                                SystemAPI.SetComponent(e, score);
                            }


                            Debug.Log("End Attack SYSTEM");
                            melee.ValueRW.cancelMovement = 0;
                            checkedComponent.ValueRW.hitLanded = false; //set at end of attack only
                            checkedComponent.ValueRW.anyDefenseStarted = false;
                            checkedComponent.ValueRW.anyAttackStarted = false;
                            checkedComponent.ValueRW.AttackStages = AttackStages.End; //only for one frame
                        }
                    }
                }
            }
        }

        private void SelectMove(Entity e, ActorInstance actor, int combatAction, ref CheckedComponent checkedComponent,
            ref SystemState state)
        {
            var movesList = SystemAPI.GetBufferLookup<MovesComponentElement>(true);
            if (movesList[e].Length <= 0) return;
            var animationIndex = -1;
            var primaryTrigger = TriggerType.None;
            moveUsing = new MovesComponentElement
            {
                active = false
            };


            for (var i = 0; i < movesList[e].Length; i++) //pick from list defined in inspector
            {
                //if ((int)movesList[e][i].animationType == combatAction)
                if (i == combatAction -
                    1) //check list and match animation in animator to trigger and type based on this
                {
                    moveUsing = movesList[e][i];
                    animationIndex = (int)moveUsing.animationType;
                    primaryTrigger = moveUsing.triggerType;
                }
            }

            Debug.Log("Move Using " + moveUsing.animationType + " to " + moveUsing.triggerType);
            //Debug.Log("SELECT MOVE " + combatAction);
            if (animationIndex <= 0 || moveUsing.active == false) return; //0 is none on enum
            var defense = animationIndex == (int)AnimationType.Deflect;
            //StartMove(animationIndex, primaryTrigger, defense, ref checkedComponent);
            checkedComponent.anyAttackStarted = true;
            checkedComponent.anyDefenseStarted = defense;
            checkedComponent.primaryTrigger = primaryTrigger;
            checkedComponent.animationIndex = animationIndex;
            checkedComponent.totalAttempts += 1;
        }
    }
}