using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CollisionSystem))]
    public partial class AttackerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>()); //player 0
            var playerList = playerQuery.ToEntityArray(Allocator.Temp);
            if (playerList.Length == 0) return;


            Entities.ForEach(
                (
                    in DeadComponent dead,
                    in CollisionComponent collisionComponent,
                    in Entity entity
                ) =>
                {
                    if (dead.isDead == true) return;

                    var typeA = collisionComponent.Part_entity;
                    var typeB = collisionComponent.Part_other_entity;
                    var entityA = collisionComponent.Character_entity;
                    var entityB = collisionComponent.Character_other_entity;
                    if (entityA == entityB && typeA != (int)TriggerType.Ammo && typeB != (int)TriggerType.Ammo) return;

                    var isMelee = collisionComponent.isMelee;
                    var playerA = SystemAPI.HasComponent<PlayerComponent>(entityA);
                    var playerB = SystemAPI.HasComponent<PlayerComponent>(entityB);
                    var enemyA = SystemAPI.HasComponent<EnemyComponent>(entityA);
                    var enemyB = SystemAPI.HasComponent<EnemyComponent>(entityB);
                    float hwA = 0;
                    float hwB = 0;
                    if (SystemAPI.HasComponent<AnimatorWeightsComponent>(entityA))
                    {
                        hwA = SystemAPI.GetComponent<AnimatorWeightsComponent>(entityA).hitWeight;
                    }

                    if (SystemAPI.HasComponent<AnimatorWeightsComponent>(entityB))
                    {
                        hwB = SystemAPI.GetComponent<AnimatorWeightsComponent>(entityB).hitWeight;
                    }

                    if ((playerA && enemyB || playerB && enemyA) || (enemyA && enemyB))
                    {
                        var checkedComponentA = SystemAPI.GetComponent<CheckedComponent>(entityA);
                        var isDefenseA = checkedComponentA.animationIndex == (int)AnimationType.Deflect;
                        var checkedComponentB = SystemAPI.GetComponent<CheckedComponent>(entityB);
                        var isDefenseB = checkedComponentB.animationIndex == (int)AnimationType.Deflect;
                        if (
                            checkedComponentA is
                            {
                                hitTriggered: false,
                                //anyAttackStarted: true, 
                                //anyDefenseStarted: true,
                                attackCompleted: false
                            } &&
                            hwB >= .3 && hwB < 1 && isDefenseB) //can change as skill
                        {
                            var deflectPoints = 10;
                            var effectsIndex = 1; //0 dead usually 1 hurt 2 deflect?
                            ecb.AddComponent(entityB,
                                new DeflectComponent
                                    { DeflectLanded = deflectPoints, DeflectReceived = 0, EntityDeflecting = entityB });

                            ecb.AddComponent(entityA,
                                new DeflectComponent
                                    { DeflectLanded = 0, DeflectReceived = deflectPoints, EntityDeflecting = entityB });


                            ecb.AddComponent(entityB,
                                new DamageComponent
                                {
                                    DamageLanded = deflectPoints, DamageReceived = 0, EntityCausingDamage = entityB,
                                    LosingDamage = false
                                });


                            ecb.AddComponent(entityA,
                                new DamageComponent
                                {
                                    EffectsIndex = 1, DamageLanded = 0, DamageReceived = deflectPoints,
                                    EntityCausingDamage = entityB, LosingDamage = false
                                });


                            if (SystemAPI.HasComponent<SkillTreeComponent>(entityB))
                            {
                                var skill = SystemAPI.GetComponent<SkillTreeComponent>(entityB);
                                skill.CurrentLevelXp += deflectPoints;
                                SystemAPI.SetComponent(entityB, skill);
                            }


                            if (SystemAPI.HasComponent<ScoreComponent>(entityB) && deflectPoints >= 10) //test
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(entityB);
                                scoreComponent.pointsScored = true;
                                scoreComponent.combo = 1; //triggers score streak to increment (using  1 currently) 
                                scoreComponent.scoredAgainstEntity = entityA;
                                SystemAPI.SetComponent(entityB, scoreComponent);
                            }

                            if (SystemAPI.HasComponent<ScoreComponent>(entityA) && deflectPoints >= 10) //test
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(entityA);
                                scoreComponent.combo = 0;
                                scoreComponent.streak = 0;
                                SystemAPI.SetComponent(entityA, scoreComponent);
                                SystemAPI.SetComponent(entityA, scoreComponent);
                            }


                            checkedComponentA.anyDefenseStarted = false; //????
                            //checkedComponent.anyAttackStarted = false; 
                            checkedComponentA.hitTriggered = true;
                            checkedComponentA.hitLanded = true;
                            checkedComponentA.totalHits += 1;
                            ecb.SetComponent(entityA, checkedComponentA);
                        }
                        else if (checkedComponentA is
                                 {
                                     //hitLanded: false, anyAttackStarted: true, attackCompleted: false,
                                     hitTriggered: false,
                                     anyDefenseStarted: false
                                 } && hwA >= .6 && !isDefenseA)
                        {
                            var effectsIndex = 1; //0 dead usually 1 hurt
                            float hitPower = 10; //need to be able to change eventually
                            if (SystemAPI.HasComponent<RatingsComponent>(entityA))
                            {
                                hitPower = SystemAPI.GetComponent<RatingsComponent>(entityA).hitPower;
                            }

                            if (SystemAPI.HasComponent<HealthComponent>(entityA))
                            {
                                var he = SystemAPI.GetComponent<HealthComponent>(entityA);
                                var alwaysDamage = he.alwaysDamage;
                                if (alwaysDamage) hwA = 1;
                                effectsIndex = he.meleeDamageEffectsIndex;
                            }

                            var damage = hitPower * hwA;

                            if (SystemAPI.HasComponent<EvadeComponent>(entityB))
                            {
                                var evade = SystemAPI.GetComponent<EvadeComponent>(entityB);
                                if (evade.evadeStrike && damage <= evade.evadeStrikeRating)
                                {
                                    //Debug.Log("ZERO DAMAGE " +  (damage * 1) );
                                    damage = 0;
                                }
                            }

                            ecb.AddComponent(entityA,
                                new DamageComponent
                                {
                                    DamageLanded = damage, DamageReceived = 0, EntityCausingDamage = entityA,
                                    LosingDamage = false
                                });


                            ecb.AddComponent(entityB,
                                new DamageComponent
                                {
                                    EffectsIndex = effectsIndex, DamageLanded = 0, DamageReceived = damage,
                                    EntityCausingDamage = entityA, LosingDamage = false
                                });


                            if (SystemAPI.HasComponent<SkillTreeComponent>(entityA))
                            {
                                var skill = SystemAPI.GetComponent<SkillTreeComponent>(entityA);
                                skill.CurrentLevelXp += damage;
                                SystemAPI.SetComponent(entityA, skill);
                            }


                            if (SystemAPI.HasComponent<ScoreComponent>(entityA) && damage >= 5) //test
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(entityA);
                                scoreComponent.pointsScored = true;
                                scoreComponent.combo = 1; //triggers score streak to increment (using  1 currently) 
                                scoreComponent.scoredAgainstEntity = entityB;
                                SystemAPI.SetComponent(entityA, scoreComponent);
                            }

                            if (SystemAPI.HasComponent<ScoreComponent>(entityB) && damage >= 5) //test
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(entityB);
                                scoreComponent.combo = 0;
                                scoreComponent.streak = 0;
                                SystemAPI.SetComponent(entityB, scoreComponent);
                            }

                            checkedComponentA.anyDefenseStarted = false;
                            checkedComponentA.hitTriggered = true; //one frame then turned off
                            checkedComponentA.hitLanded = true; //on until end of animation / move
                            checkedComponentA.totalHits += 1;
                            ecb.SetComponent(entityA, checkedComponentA);
                        }
                    }


                    if (typeB == (int)TriggerType.Ammo && SystemAPI.HasComponent<TriggerComponent>(entityA)
                                                       && SystemAPI
                                                           .HasComponent<
                                                               TriggerComponent>(
                                                               entityB)) //b is ammo so causes damage to entity
                    {
                        var shooter = Entity.Null;
                        shooter = SystemAPI.GetComponent<TriggerComponent>(entityB)
                            .ParentEntity;

                        //shooter always enemy for GMTK 2023
                        //shooter = SystemAPI.GetComponent<TriggerComponent>(entityA)
                        //  .ParentEntity;


                        if (shooter != Entity.Null && SystemAPI.HasComponent<AmmoComponent>(entityB))
                        {
                            var isEnemyShooter = SystemAPI.HasComponent<EnemyComponent>(shooter);
                            //isEnemyShooter = true;
                            var target = SystemAPI.GetComponent<TriggerComponent>(entityA)
                                .ParentEntity;
                            var isEnemyTarget = SystemAPI.HasComponent<EnemyComponent>(target);
                            var ammo =
                                SystemAPI.GetComponent<AmmoComponent>(entityB);
                            var ammoData =
                                SystemAPI.GetComponent<AmmoDataComponent>(entityB);

                            float damage = 0; //why using enemy data and not ammo data ?? change this
                            damage = ammoData.GameDamage; //overrides previous
                            ammo.AmmoDead = true;

                            if (ammo.DamageCausedPreviously &&
                                ammo.frameSkipCounter > ammo.framesToSkip) //count in ammosystem
                            {
                                ammo.DamageCausedPreviously = false;
                                ammo.frameSkipCounter = 0;
                            }

                            var shootsSelf = shooter == entityA && isEnemyShooter;


                            //if (ammo.DamageCausedPreviously || ammoData.ChargeRequired == true && ammo.Charged == false || isEnemyShooter == isEnemyTarget
                            if (ammo.DamageCausedPreviously || ammoData.ChargeRequired == true && ammo.Charged == false)
                            {
                                damage = 0;
                            }

                            if (SystemAPI.HasComponent<DeadComponent>(entityA) == false ||
                                SystemAPI.GetComponent<DeadComponent>(entityA).isDead)
                            {
                                damage = 0;
                            }

                            ammo.DamageCausedPreviously = true;
                            var playerDamaged = false;

                            if (shootsSelf)
                            {
                                shooter = playerList[0];
                                //(shooter, entityA) = (entityA, shooter);
                            }
                            else
                            {
                                playerDamaged = true;
                            }


                            ecb.AddComponent(shooter,
                                new DamageComponent
                                {
                                    DamageLanded = damage, DamageReceived = 0, EntityCausingDamage = entityB,
                                    LosingDamage = false
                                });

                            ecb.AddComponent(entityA,
                                new DamageComponent
                                {
                                    DamageLanded = 0,
                                    DamageReceived = damage,
                                    StunLanded = damage,
                                    EffectsIndex = ammo.effectIndex,
                                    LosingDamage = false,
                                    EntityCausingDamage = entityB
                                });

                            if (SystemAPI.HasComponent<SkillTreeComponent>(shooter))
                            {
                                var skill = SystemAPI.GetComponent<SkillTreeComponent>(shooter);
                                skill.CurrentLevelXp += damage;
                                SystemAPI.SetComponent(shooter, skill);
                            }


                            //var isPlayerShooter = SystemAPI.HasComponent<PlayerComponent>(shooter);
                            if (SystemAPI.HasComponent<ScoreComponent>(shooter) && damage != 0)
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(shooter);
                                scoreComponent.addBonus = 0;
                                if (!isEnemyShooter) //player GMTK where can't score after hit - backwards
                                {
                                    //scoreComponent.zeroPoints = true;
                                    //scoreComponent.pointsScored = false;
                                    //scoreComponent.combo = 0;
                                    //scoreComponent.streak = 0;
                                    //scoreComponent.score = scoreComponent.startShotValue;
                                }

                                //for gmtk bonus for charged (blocked)
                                if (ammo.Charged && isEnemyShooter == false && isEnemyTarget == true)
                                {
                                    scoreComponent.addBonus = scoreComponent.defaultPointsScored * 1;
                                    ammo.Charged = false;
                                }

                                if (!scoreComponent.zeroPoints)
                                {
                                    scoreComponent.scoringAmmoEntity = ammo.ammoEntity;
                                    scoreComponent.pointsScored = true;
                                    scoreComponent.combo = 1;
                                    scoreComponent.scoredAgainstEntity = entityA;
                                }

                                SystemAPI.SetComponent(shooter, scoreComponent);
                            }

                            if (SystemAPI.HasComponent<ScoreComponent>(entityA) && damage >= 0)
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(entityA);
                                scoreComponent.combo = 0;
                                scoreComponent.streak = 0;
                                SystemAPI.SetComponent(entityA, scoreComponent);
                            }


                            ecb.SetComponent(entityB, ammo);
                        }
                    }


                    if (typeB == (int)TriggerType.Particle && SystemAPI.HasComponent<TriggerComponent>(entityA)
                                                           && SystemAPI
                                                               .HasComponent<
                                                                   TriggerComponent>(
                                                                   entityB)) //b is damage effect so causes damage to entity
                    {
                        var shooter = SystemAPI.GetComponent<TriggerComponent>(entityB)
                            .ParentEntity;

                        if (shooter != Entity.Null &&
                            SystemAPI.HasComponent<VisualEffectEntityComponent>(entityB))
                        {
                            var isEnemyShooter = SystemAPI.HasComponent<EnemyComponent>(shooter);
                            var target = SystemAPI.GetComponent<TriggerComponent>(entityA)
                                .ParentEntity;
                            var isEnemyTarget = SystemAPI.HasComponent<EnemyComponent>(target);
                            var visualEffectComponent =
                                SystemAPI.GetComponent<VisualEffectEntityComponent>(entityB);

                            float damage = 0;
                            var effectsIndex = 0;
                            var skip = false;
                            //if (visualEffectComponent.frameSkipCounter < visualEffectComponent.framesToSkip)
                            if (visualEffectComponent.frameSkipCounter == 0)
                            {
                                visualEffectComponent.frameSkipCounter += 1;
                                skip = false;
                            }
                            else if (visualEffectComponent.frameSkipCounter < visualEffectComponent.framesToSkip)

                            {
                                visualEffectComponent.frameSkipCounter += 1;
                                skip = true;
                            }
                            else if (visualEffectComponent.frameSkipCounter >= visualEffectComponent.framesToSkip)

                            {
                                visualEffectComponent.frameSkipCounter = 0;
                                skip = true;
                            }

                            if (skip == false)
                            {
                                damage = visualEffectComponent.damageAmount;
                                //effectsIndex = (int)EffectType.Damaged;
                                effectsIndex = visualEffectComponent.effectsIndex; //???
                            }

                            if (SystemAPI.HasComponent<DeadComponent>(entityA) == false ||
                                SystemAPI.GetComponent<DeadComponent>(entityA).isDead)
                            {
                                damage = 0;
                            }


                            ecb.AddComponent(shooter,
                                new DamageComponent
                                {
                                    DamageLanded = damage,
                                    DamageReceived = 0,
                                    EntityCausingDamage = entityB
                                });


                            ecb.AddComponent(entityA,
                                new DamageComponent
                                {
                                    DamageLanded = 0, DamageReceived = damage, StunLanded = damage,
                                    EntityCausingDamage = entityB, EffectsIndex = effectsIndex
                                });

                            if (SystemAPI.HasComponent<SkillTreeComponent>(shooter))
                            {
                                var skill = SystemAPI.GetComponent<SkillTreeComponent>(shooter);
                                skill.CurrentLevelXp += damage;
                                SystemAPI.SetComponent(shooter, skill);
                            }


                            if (SystemAPI.HasComponent<ScoreComponent>(shooter) && damage != 0)
                            {
                                var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(shooter);
                                scoreComponent.addBonus = 0;
                                scoreComponent.pointsScored = true;
                                scoreComponent.scoredAgainstEntity = entityA;
                                SystemAPI.SetComponent(shooter, scoreComponent);
                            }

                            ecb.SetComponent(entityB, visualEffectComponent);
                        }
                    }
                }
            ).Run();

            playerList.Dispose();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}