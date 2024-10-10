using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public struct CheckedComponent : IComponentData
    {
        public AttackStages AttackStages;
        public bool anyDefenseStarted;
        public bool anyAttackStarted; //weapon or melee
        public bool attackFirstFrame;
        public bool attackCompleted;
        public bool attackInProgress;
        public bool hitTriggered; //on during frame only
        public bool hitLanded; //on until end of animation / move
        public bool hitReceived;
        public int totalHits;
        public int totalAttempts;
        public TriggerType primaryTrigger;
        public int animationIndex;

        public int comboIndexPlaying;

        //public int comboCounter;
        public bool comboButtonClicked;
    }

    public struct CollisionComponent : IComponentData
    {
        public int Part_entity;
        public int Part_other_entity;
        public Entity Character_entity;
        public Entity Character_other_entity;
        public bool isMelee;
        public bool isDefenseMove;
        public bool isHit;
    }

    public struct PowerTriggerComponent : IComponentData
    {
        public int TriggerType;
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PlayerMoveSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class CollisionSystem : SystemBase
    {
        EndFixedStepSimulationEntityCommandBufferSystem m_ecbSystem;

        protected override void OnCreate()
        {
            m_ecbSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var colliderKeyEntityPairs = SystemAPI.GetBufferLookup<PhysicsColliderKeyEntityPair>();


            var collisionJob = new CollisionJob
            {
                Ecb = m_ecbSystem.CreateCommandBuffer(),
                triggerGroup = GetComponentLookup<TriggerComponent>(true),
                healthGroup = GetComponentLookup<HealthComponent>(true),
                ammoGroup = GetComponentLookup<AmmoComponent>(),
                checkGroup = GetComponentLookup<CheckedComponent>(true),
                bossGroup = GetComponentLookup<BossComponent>(true),
                colliderKeyEntityPairs = colliderKeyEntityPairs
            };

            Dependency = collisionJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
            Dependency.Complete();
        }

        [BurstCompile]
        struct CollisionJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<TriggerComponent> triggerGroup;
            [ReadOnly] public ComponentLookup<HealthComponent> healthGroup;
            [ReadOnly] public ComponentLookup<CheckedComponent> checkGroup;
            [ReadOnly] public ComponentLookup<BossComponent> bossGroup;

            public ComponentLookup<AmmoComponent> ammoGroup;
            [ReadOnly] public BufferLookup<PhysicsColliderKeyEntityPair> colliderKeyEntityPairs;
            public EntityCommandBuffer Ecb;

            public void Execute(CollisionEvent ev) // this is never called
            {
                var a = ev.EntityA;
                var b = ev.EntityB;

                if (triggerGroup.HasComponent(a) == false || triggerGroup.HasComponent(b) == false) return;
                var triggerComponentA = triggerGroup[a];
                var triggerComponentB = triggerGroup[b];

                var hitColliderKeyA = ev.ColliderKeyA;
                var hitColliderKeyB = ev.ColliderKeyB;

                //var hitEntityA = ev.EntityA;
                //var hitEntityB = ev.EntityB;
                

                //Debug.Log("Count A " + colliderKeyEntityPairs[hitEntityA].Length);
                // for (int i = 0; i < colliderKeyEntityPairs[hitEntityA].Length; i++)
                // {
                //     if (colliderKeyEntityPairs[hitEntityA][i].Key.Equals(hitColliderKeyA))
                //     {
                //         // Return the corresponding entity from the pair
                //         var e = colliderKeyEntityPairs[hitEntityA][i].Entity;
                //         Debug.Log("Entity A " + e);
                //     }
                // }
                //
                // Debug.Log("Count " + colliderKeyEntityPairs[hitEntityB].Length);
                // for (int i = 0; i < colliderKeyEntityPairs[hitEntityB].Length; i++)
                // {
                //     if (colliderKeyEntityPairs[hitEntityB][i].Key.Equals(hitColliderKeyB))
                //     {
                //         // Return the corresponding entity from the pair
                //         var e = colliderKeyEntityPairs[hitEntityB][i].Entity;
                //         Debug.Log("Entity B " + e);
                //     }
                // }


                var chA = triggerComponentA.ParentEntity;
                var chB = triggerComponentB.ParentEntity;
                var typeA = triggerComponentA.Type;
                if (typeA == (int)TriggerType.Tail) typeA = (int)TriggerType.Melee;
                var typeB = triggerComponentB.Type;
                if (typeB == (int)TriggerType.Tail) typeB = (int)TriggerType.Melee;

                if (chA == chB && typeA != (int)TriggerType.Ammo && typeB != (int)TriggerType.Ammo) return; ////?????


                var alwaysDamageA = false;
                if (healthGroup.HasComponent(chA))
                {
                    var healthComponentA = healthGroup[chA];
                    alwaysDamageA = healthComponentA.alwaysDamage;
                }

                var alwaysDamageB = false;
                if (healthGroup.HasComponent(chB))
                {
                    var healthComponentB = healthGroup[chB];
                    alwaysDamageB = healthComponentB.alwaysDamage; //regardless of type trigger
                }


                if (triggerComponentA.Type == (int)TriggerType.Ground ||
                    triggerComponentB.Type == (int)TriggerType.Ground)
                {
                    return;
                }


                var primaryTriggerA = TriggerType.None;
                var primaryTriggerB = TriggerType.None;

                if (checkGroup.HasComponent(chA))
                {
                    primaryTriggerA = checkGroup[chA].primaryTrigger;
                    Debug.Log("check primary trigger A " + primaryTriggerA + " " + chA);

                }

                if (checkGroup.HasComponent(chB))
                {
                    primaryTriggerB = checkGroup[chB].primaryTrigger;
                    Debug.Log("check primary trigger B " + primaryTriggerB + " " + chB);
                }
                

                var punchingA = false;
                var punchingB = false;
                if (typeA is (int)TriggerType.Body or (int)TriggerType.Base or (int)TriggerType.Head)
                {
                    punchingB = true; //B punch landed
                    Debug.Log("punchingB " + punchingB);
                }
                else if (typeB is (int)TriggerType.Body or (int)TriggerType.Base or (int)TriggerType.Head)
                {
                    punchingA = true; //A punch landed
                    Debug.Log("punchingA " + punchingA);
                }


                //if punching A or B is true then we dont skip eventhough type a = type b 
                if (typeA == typeB && punchingA == false && punchingB == false && alwaysDamageA == false &&
                    alwaysDamageB == false)
                    return;


                if (bossGroup.HasComponent(chA))
                {
                    primaryTriggerA = TriggerType.Melee;
                }
                else if (bossGroup.HasComponent(chB))
                {
                    primaryTriggerB = TriggerType.Melee;
                }




                var meleeA = (punchingA) &&
                             (typeA == (int)TriggerType.Melee && typeA == (int)primaryTriggerA);

                var meleeB = (punchingB) &&
                             (typeB == (int)TriggerType.Melee && typeB == (int)primaryTriggerB);


                var defenseA = false;
                var defenseB = false;
                if (checkGroup.HasComponent(chA))
                {
                    defenseA = checkGroup[chA]
                        .anyDefenseStarted; //only true when trigger type is hand or similar so if true punching and similar still false
                }

                if (checkGroup.HasComponent(chB))
                {
                    defenseB = checkGroup[chB]
                        .anyDefenseStarted; //only true when trigger type is hand or similar so if true punching and similar still false
                }

                //check if arm/hands colliding with each other (feet for attacker? melee? setting trigger type to that instead of hand)
                var primaryDefenseTriggerMatchA = (typeA is (int)TriggerType.LeftHand or (int)TriggerType.RightHand) &&
                                                  (int)primaryTriggerB == typeB;
                var primaryDefenseTriggerMatchB = (typeB is (int)TriggerType.LeftHand or (int)TriggerType.RightHand) &&
                                                  (int)primaryTriggerA == typeA;
                defenseA = typeB is (int)TriggerType.Melee &&
                           primaryDefenseTriggerMatchA && defenseA;
                defenseB = typeA is (int)TriggerType.Melee &&
                           primaryDefenseTriggerMatchB && defenseB;

                var prA = (int)primaryTriggerA == typeA;
                var prB = (int)primaryTriggerB == typeB;

                var primaryTriggerMatchA = (typeA is (int)TriggerType.LeftHand or (int)TriggerType.RightHand
                                               or (int)TriggerType.LeftFoot or (int)TriggerType.RightFoot)
                                           && (int)primaryTriggerA == typeA;
                var primaryTriggerMatchB = (typeB is (int)TriggerType.LeftHand or (int)TriggerType.RightHand
                                               or (int)TriggerType.LeftFoot or (int)TriggerType.RightFoot)
                                           && (int)primaryTriggerB == typeB;


                punchingA = punchingA &&
                    primaryTriggerMatchA || meleeA;


                punchingB = punchingB &&
                    primaryTriggerMatchB || meleeB;


                //Debug.Log("trigger match A " + primaryTriggerMatchA + " trigger match  B " + primaryTriggerMatchB);


                var ammoA = typeB is (int)TriggerType.Base or (int)TriggerType.Head or (int)TriggerType.Body &&
                            (typeA == (int)TriggerType.Ammo);

                var ammoB = typeA is (int)TriggerType.Base or (int)TriggerType.Head or (int)TriggerType.Body &&
                            (typeB == (int)TriggerType.Ammo);

                var ammoBlockedA = (typeB == (int)TriggerType.Blocks) &&
                                   (typeA == (int)TriggerType.Ammo);

                var ammoBlockedB = (typeA == (int)TriggerType.Blocks) &&
                                   (typeB == (int)TriggerType.Ammo);

                var effectA = typeB is (int)TriggerType.Base or (int)TriggerType.Head or (int)TriggerType.Body &&
                              (typeA == (int)TriggerType.Particle);

                var effectB = typeA is (int)TriggerType.Base or (int)TriggerType.Head or (int)TriggerType.Body &&
                              (typeB == (int)TriggerType.Particle);


                if (ammoBlockedA)
                {
                    var ammoComponent = ammoGroup[triggerComponentA.Entity];
                    ammoComponent.Charged = true;
                    ammoGroup[triggerComponentA.Entity] = ammoComponent;
                }

                if (ammoBlockedB)
                {
                    var ammoComponent = ammoGroup[triggerComponentB.Entity];
                    ammoComponent.Charged = true;
                    ammoGroup[triggerComponentB.Entity] = ammoComponent;
                }

                if (ammoA || effectA)
                {
                    //coll component part other always ammo ?

                    var collisionComponent =
                        new CollisionComponent
                        {
                            Part_entity = triggerComponentB.Type,
                            Part_other_entity = triggerComponentA.Type,
                            Character_entity = triggerComponentB.ParentEntity, //actor hit by ammo
                            Character_other_entity = triggerComponentA.Entity,
                            isMelee = meleeA,
                            isHit = punchingA
                        };

                    Ecb.AddComponent(triggerComponentA.ParentEntity, collisionComponent);
                }
                else if (ammoB || effectB)
                {
                    var collisionComponent =
                        new CollisionComponent
                        {
                            Part_entity = triggerComponentA.Type,
                            Part_other_entity = triggerComponentB.Type,
                            Character_entity = triggerComponentA.ParentEntity,
                            Character_other_entity = triggerComponentB.Entity,
                            isMelee = meleeB,
                            isHit = punchingB
                        };

                    Ecb.AddComponent(triggerComponentB.ParentEntity, collisionComponent);
                }
                else if ((punchingA || meleeA || defenseA || alwaysDamageA) && !ammoA && !ammoB)
                {
                    Debug.Log("A " + (TriggerType) typeA + ", B " + (TriggerType) typeB);


                    var collisionComponent =
                        new CollisionComponent
                        {
                            Part_entity = triggerComponentA.Type,
                            Part_other_entity = triggerComponentB.Type,
                            Character_entity = chA,
                            Character_other_entity = chB,
                            isMelee = meleeA,
                            isDefenseMove = defenseA
                        };
                    Ecb.AddComponent(chA, collisionComponent);
                }
                else if (punchingB || meleeB || defenseB || alwaysDamageB && !ammoA && !ammoB)
                {
                    Debug.Log("B " + (TriggerType) typeB + ", A " + (TriggerType) typeA);

                    var collisionComponent =
                        new CollisionComponent
                        {
                            Part_entity = triggerComponentB.Type,
                            Part_other_entity = triggerComponentA.Type,
                            Character_entity = chB,
                            Character_other_entity = chA,
                            isMelee = meleeB,
                            isDefenseMove = defenseB
                        };
                    Ecb.AddComponent(chB, collisionComponent);
                }
            }
        }
    } // System
}