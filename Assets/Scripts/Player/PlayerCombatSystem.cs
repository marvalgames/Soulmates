using Collisions;
using Enemy;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Player
{
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

                Debug.Log("xtap " + buttonXtap);

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
                    //playerCombat.SelectMove(10);
                }
                else if (leftBumperUp)
                {
                    //animator.SetInteger(CombatAction, 0);
                    melee.ValueRW.combatAction = 0;
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


    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerCombatSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerCombatManagedSystem : ISystem
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        private static readonly int ComboAnimationPlayed = Animator.StringToHash("ComboAnimationPlayed");
        private MovesComponentElement moveUsing;


        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actor, melee, checkedComponent, inputController, applyImpulse, e) in SystemAPI
                         .Query<ActorInstance, RefRW<MeleeComponent>, RefRW<CheckedComponent>,
                             RefRW<InputControllerComponent>, RefRW<ApplyImpulseComponent>>().WithEntityAccess())
            {
                var playerCombat = actor.actorPrefabInstance.GetComponent<PlayerCombat>();
                playerCombat.meleeEntity = e;
                playerCombat.entityManager = state.EntityManager;

                if (melee.ValueRW.selectMove > 0)
                {
                    SelectMove(e, actor, melee.ValueRW.selectMove, ref checkedComponent.ValueRW, ref state);
                    Debug.Log("Player Select move " + melee.ValueRW.selectMove);
                    //playerCombat.SelectMove(melee.ValueRW.selectMove);
                    var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                    melee.ValueRW.verticalSpeed = animator.GetFloat(Vertical);
                    //melee.ValueRW.comboAnimationPlayed = 1;
                    //melee.ValueRW.selectMove = 1;
                    animator.SetInteger(ComboAnimationPlayed, melee.ValueRW.comboAnimationPlayed);
                    animator.SetInteger(CombatAction, melee.ValueRW.selectMove);
                    melee.ValueRW.selectMove = 0;
                }
                else if (melee.ValueRW.selectMove == 0)
                {
                    var stage = actor.actorPrefabInstance.GetComponent<ActorEntityTracker>().animationStageTracker;
                    if (stage == AnimationStage.Enter)
                    {
                        //checkedComponent.anyAttackStarted = true;
                        Debug.Log("Start Attack SYSTEM");
                        checkedComponent.ValueRW.attackFirstFrame = true;
                        checkedComponent.ValueRW.AttackStages = AttackStages.Start;
                        checkedComponent.ValueRW.hitTriggered = false;
                    }
                    else if (stage == AnimationStage.Update)
                    {
                        checkedComponent.ValueRW.AttackStages = AttackStages.Action;
                        Debug.Log("Update Attack SYSTEM");
                    }
                    else if (stage == AnimationStage.Exit)
                    {
                        if (checkedComponent.ValueRW.hitTriggered == false && SystemAPI.HasComponent<ScoreComponent>(e))
                        {
                            var score = SystemAPI.GetComponent<ScoreComponent>(e);
                            score.combo = 0;
                            score.streak = 0;
                            SystemAPI.SetComponent(e, score);
                        }

                        Debug.Log("End Attack SYSTEM");
                        checkedComponent.ValueRW.hitLanded = false; //set at end of attack only
                        checkedComponent.ValueRW.anyDefenseStarted = false;
                        checkedComponent.ValueRW.anyAttackStarted = false;
                        checkedComponent.ValueRW.AttackStages = AttackStages.End; //only for one frame
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


            // var stage = actor.actorPrefabInstance.GetComponent<ActorEntityTracker>().animationStageTracker;
            // if (stage == AnimationStage.Enter)
            // {
            //     //checkedComponent.anyAttackStarted = true;
            //     Debug.Log("Start Attack SYSTEM");
            //     checkedComponent.attackFirstFrame = true;
            //     checkedComponent.AttackStages = AttackStages.Start;
            //     checkedComponent.hitTriggered = false;
            // }
            // else if (stage == AnimationStage.Update)
            // {
            //     checkedComponent.AttackStages = AttackStages.Action;
            //     Debug.Log("Update Attack SYSTEM");
            // }
            // else if (stage == AnimationStage.Exit)
            // {
            //     if (checkedComponent.hitTriggered == false && SystemAPI.HasComponent<ScoreComponent>(e))
            //     {
            //         var score = SystemAPI.GetComponent<ScoreComponent>(e);
            //         score.combo = 0;
            //         score.streak = 0;
            //         SystemAPI.SetComponent(e, score);
            //     }
            //     Debug.Log("End Attack SYSTEM");
            //     checkedComponent.hitLanded = false; //set at end of attack only
            //     checkedComponent.anyDefenseStarted = false;
            //     checkedComponent.anyAttackStarted = false;
            //     checkedComponent.AttackStages = AttackStages.End; //only for one frame
            //     
            // }
        }


        private void StartMove(int animationIndex, TriggerType primaryTrigger, bool defense,
            ref CheckedComponent checkedComponent)
        {
            // checkedComponent.anyAttackStarted = true;
            // checkedComponent.anyDefenseStarted = defense;
            // checkedComponent.primaryTrigger = primaryTrigger;
            // checkedComponent.animationIndex = animationIndex;
            // checkedComponent.totalAttempts += 1;
            //animator.SetInteger(CombatAction, animationIndex);
            //Debug.Log(" Start Move " + animationIndex);
        }
    }
}