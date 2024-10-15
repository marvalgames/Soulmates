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


                var chA = triggerComponentA.ParentEntity;
                var chB = triggerComponentB.ParentEntity;
                var typeA = (TriggerType)triggerComponentA.Type;
                if (typeA == TriggerType.Tail) typeA = TriggerType.Melee;
                var typeB = (TriggerType)triggerComponentB.Type;
                if (typeB == TriggerType.Tail) typeB = TriggerType.Melee;

                if (chA == chB && typeA != TriggerType.Ammo && typeB != TriggerType.Ammo) return;
                
                Debug.Log("A " + a +" B " + b);


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


                if (typeA == TriggerType.Ground ||
                    typeB == TriggerType.Ground)
                {
                    return;
                }


                var primaryTriggerA = TriggerType.None;
                var primaryTriggerB = TriggerType.None;

                if (checkGroup.HasComponent(chA))
                {
                    primaryTriggerA = checkGroup[chA].primaryTrigger;
                    //Debug.Log("check primary trigger A " + primaryTriggerA + " " + chA);
                }

                if (checkGroup.HasComponent(chB))
                {
                    primaryTriggerB = checkGroup[chB].primaryTrigger;
                    //Debug.Log("check primary trigger B " + primaryTriggerB + " " + chB);
                }

                //var punchingA = false;
                //var punchingB = false;
                var defenderA = typeA is TriggerType.Body or TriggerType.Head or TriggerType.Base;
                var defenderB = typeB is TriggerType.Body or TriggerType.Head or TriggerType.Base;
                var attackerA = typeA is TriggerType.LeftHand or TriggerType.RightHand or TriggerType.LeftFoot
                    or TriggerType.RightFoot;
                var attackerB = typeB is TriggerType.LeftHand or TriggerType.RightHand or TriggerType.LeftFoot
                    or TriggerType.RightFoot;

                var punchingA = attackerA && defenderB && primaryTriggerA == typeA;
                var punchingB = attackerB && defenderA && primaryTriggerB == typeB;
                var meleeA = typeA == TriggerType.Melee && typeA == primaryTriggerA && defenderB;
                var meleeB = typeB == TriggerType.Melee && typeB == primaryTriggerB && defenderA;

                //do not skip when typeA is not typeB 
                if (typeA == typeB && punchingA == false && punchingB == false && alwaysDamageA == false &&
                    alwaysDamageB == false)
                    return;

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
                var primaryDefenseTriggerMatchA = (typeA is TriggerType.LeftHand or TriggerType.RightHand) &&
                                                  primaryTriggerB == typeB;
                var primaryDefenseTriggerMatchB = (typeB is TriggerType.LeftHand or TriggerType.RightHand) &&
                                                  primaryTriggerA == typeA;
                defenseA = typeB is TriggerType.Melee &&
                           primaryDefenseTriggerMatchA && defenseA;
                defenseB = typeA is TriggerType.Melee &&
                           primaryDefenseTriggerMatchB && defenseB;


                punchingA = punchingA || meleeA;

                punchingB = punchingB || meleeB;


                var ammoA = typeB is TriggerType.Base or TriggerType.Head or TriggerType.Body &&
                            (typeA == TriggerType.Ammo);

                var ammoB = typeA is TriggerType.Base or TriggerType.Head or TriggerType.Body &&
                            (typeB == TriggerType.Ammo);

                var ammoBlockedA = (typeB == TriggerType.Blocks) &&
                                   (typeA == TriggerType.Ammo);

                var ammoBlockedB = (typeA == TriggerType.Blocks) &&
                                   (typeB == TriggerType.Ammo);

                var effectA = typeB is TriggerType.Base or TriggerType.Head or TriggerType.Body &&
                              (typeA == TriggerType.Particle);

                var effectB = typeA is TriggerType.Base or TriggerType.Head or TriggerType.Body &&
                              (typeB == TriggerType.Particle);


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
                    //Debug.Log("A " + a + ", B " + b);

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
                    //Debug.Log("B " + typeB + ", A " + typeA);

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


//var hitColliderKeyA = ev.ColliderKeyA;
//var hitColliderKeyB = ev.ColliderKeyB;
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