using System.Collections.Generic;
using Collisions;
using Enemy;
using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        public MovesManager movesInspector;
        [HideInInspector] public Animator animator;
        private List<Moves> moveList = new List<Moves>();
        public Moves moveUsing = new Moves();
        private Entity meleeEntity;
        private EntityManager entityManager;
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        public int lastCombatAction;
        private static readonly int CombatMode = Animator.StringToHash("CombatMode");
        private static readonly int Zone = Animator.StringToHash("Zone");

        void Start()
        {
            animator = GetComponent<Animator>();

            for (var i = 0; i < movesInspector.Moves.Count; i++)
            {
                var move = movesInspector.Moves[i];
                move.target = moveUsing.target; //default target assigned in system
                //move.targetEntity = meleeEntity;
                moveList.Add(move);
            }


            if (meleeEntity == Entity.Null)
            {
                meleeEntity = GetComponent<CharacterEntityTracker>().linkedEntity;
                if (entityManager == default)
                {
                    entityManager = GetComponent<CharacterEntityTracker>().entityManager;
                }

                if (meleeEntity != Entity.Null) entityManager.AddComponentObject(meleeEntity, this);
            }
        }

        public void SelectMove(int combatAction)
        {
            if (moveList.Count <= 0) return;
            var animationIndex = -1;
            var primaryTrigger = TriggerType.None;

            for (var i = 0; i < moveList.Count; i++) //pick from list defined in inspector
            {
                if ((int)moveList[i].animationType == combatAction)
                {
                    moveUsing = moveList[i];
                    animationIndex = (int)moveUsing.animationType;
                    primaryTrigger = moveUsing.triggerType;
                }
            }

            Debug.Log("SELECT MOVE");
            if (animationIndex <= 0 || moveUsing.active == false) return; //0 is none on enum
            var defense = animationIndex == (int)AnimationType.Deflect;
            lastCombatAction = combatAction;
            StartMove(animationIndex, primaryTrigger, defense);
        }

        public void StartMove(int animationIndex, TriggerType primaryTrigger, bool defense)
        {
            if (moveList.Count <= 0) return;
            if (entityManager.HasComponent<CheckedComponent>(meleeEntity))
            {
                var checkedComponent = entityManager.GetComponentData<CheckedComponent>(meleeEntity);
                checkedComponent.anyAttackStarted = true;
                checkedComponent.anyDefenseStarted = defense;
                checkedComponent.primaryTrigger = primaryTrigger;
                checkedComponent.animationIndex = animationIndex;
                entityManager.SetComponentData(meleeEntity, checkedComponent);
            }

            animator.SetInteger(CombatAction, animationIndex);
        }


        public void Aim()
        {
            if (entityManager.HasComponent<ActorWeaponAimComponent>(meleeEntity))
            {
                var aimComponent = entityManager.GetComponentData<ActorWeaponAimComponent>(meleeEntity);
                //Debug.Log("COMBAT MODE " + aimComponent.combatMode);
                animator.SetInteger(Zone, aimComponent.combatMode ? 1 : 0);
                animator.SetBool(CombatMode, aimComponent.combatMode);
            }
        }

        public void LateUpdateSystem()
        {
            if (moveList.Count == 0) return;
            Aim();
        }

        public void StartMotionUpdateCheckComponent() //event
        {
        }

        public void StateUpdateCheckComponent()
        {
            if (entityManager.HasComponent<CheckedComponent>(meleeEntity))
            {
                var checkedComponent = entityManager.GetComponentData<CheckedComponent>(meleeEntity);
                checkedComponent.AttackStages = AttackStages.Action;
                entityManager.SetComponentData(meleeEntity, checkedComponent);
            }
            
        }


        public void StartAttackUpdateCheckComponent() //event
        {
            if (entityManager.HasComponent<MeleeComponent>(meleeEntity))
            {
                var melee = entityManager.GetComponentData<MeleeComponent>(meleeEntity);
                moveUsing.target = melee.target;
            }

            // if (moveUsing.moveAudioSource && moveUsing.moveAudioClip)
            // {
            //     moveUsing.moveAudioSource.clip = moveUsing.moveAudioClip;
            //     moveUsing.moveAudioSource.PlayOneShot(moveUsing.moveAudioClip);
            // }

            if (moveUsing.moveParticleSystem)
            {
                moveUsing.moveParticleSystem.Play(true);
            }

            if (entityManager.HasComponent<CheckedComponent>(meleeEntity))
            {
                var checkedComponent = entityManager.GetComponentData<CheckedComponent>(meleeEntity);
                checkedComponent.anyAttackStarted = true;
                checkedComponent.attackFirstFrame = true;
                checkedComponent.AttackStages = AttackStages.Start;
                //checkedComponent.anyDefenseStarted = false;
                checkedComponent.hitTriggered = false;
                entityManager.SetComponentData(meleeEntity, checkedComponent);
            }
        }

        public void EndAttack()
        {
            if (entityManager.HasComponent<CheckedComponent>(meleeEntity))
            {
                var checkedComponent = entityManager.GetComponentData<CheckedComponent>(meleeEntity);
                if (checkedComponent.hitTriggered == false && entityManager.HasComponent<ScoreComponent>(meleeEntity))
                {
                    var score = entityManager.GetComponentData<ScoreComponent>(meleeEntity);
                    score.combo = 0;
                    score.streak = 0;
                    entityManager.SetComponentData(meleeEntity, score);
                }
                Debug.Log("End Attack");
                checkedComponent.hitLanded = false; //set at end of attack only
                checkedComponent.anyDefenseStarted = false;
                checkedComponent.anyAttackStarted = false;
                checkedComponent.AttackStages = AttackStages.End; //only for one frame
                entityManager.SetComponentData(meleeEntity, checkedComponent);
            }
        }
    }
}