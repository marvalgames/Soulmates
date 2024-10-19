using AI;
using Collisions;
using ProjectDawn.Navigation;
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
    [RequireMatchingQueriesForUpdate]
    //[UpdateInGroup(typeof(SimulationSystemGroup))]
    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MatchupSystem))]
    public partial struct EnemyActorMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var weaponGroup = SystemAPI.GetComponentLookup<WeaponComponent>();
            var actorWeaponAimGroup = SystemAPI.GetComponentLookup<ActorWeaponAimComponent>();
            var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var randomNumber = Random.Range(0f, 100f);


            var job = new EnemyActorMovementJob
            {
                weaponGroup = weaponGroup,
                actorWeaponAimGroup = actorWeaponAimGroup,
                transformGroup = transformGroup,
                deltaTime = deltaTime,
                randomNumber = randomNumber
            };

            job.Schedule();
        }


        [BurstCompile]
        partial struct EnemyActorMovementJob : IJobEntity
        {
            public ComponentLookup<WeaponComponent> weaponGroup;
            public ComponentLookup<ActorWeaponAimComponent> actorWeaponAimGroup;
            public ComponentLookup<LocalTransform> transformGroup;
            public float deltaTime;
            public float randomNumber;

            void Execute(Entity e, ref MatchupComponent matchup, ref DefensiveStrategyComponent defensiveStrategy,
                ref EnemyMovementComponent enemyMovement,
                ref EnemyStateComponent enemyState,
                ref ImpulseComponent impulse,
                ref AgentLocomotion locomotion,
                AnimatorParametersAspect animator,
                in EnemyBehaviourComponent enemyBehaviour, in EnemyMeleeMovementComponent enemyMeleeMovement,
                in EnemyWeaponMovementComponent enemyWeaponMovement,
                in CheckedComponent checkedComponent)
            {
                //if (defensiveStrategy.currentRole == DefensiveRoles.None || matchup.isWaypointTarget) return;

                var zone = new FastAnimatorParameter("Zone");
                var velz = new FastAnimatorParameter("velz");


                defensiveStrategy.botState = BotState.MOVING;
                var enemyTransform = transformGroup[e];
                var closestOpponent = matchup.closestOpponentEntity;
                var enemyPosition = enemyTransform.Position;
                var homePosition = enemyMovement.originalPosition;
                var stayHome = enemyBehaviour.useDistanceFromStation;
                var distFromStation = math.distance(homePosition, enemyPosition);


                var distanceToOpponent = matchup.closestDistance;
                var chaseRange = enemyBehaviour.chaseRange;
                var stopRange = enemyBehaviour.stopRange;
                var aggression = enemyBehaviour.aggression;
                var weaponRaised = WeaponMotion.None;
                var meleeMovement = enemyMeleeMovement.enabled;
                var weaponMovement = enemyWeaponMovement.enabled;
                var basicMovement = enemyMovement.enabled;
                var shootRange = enemyWeaponMovement.shootRangeDistance;

                var hasWeapon = weaponGroup.HasComponent(e);
                var hasAim = actorWeaponAimGroup.HasComponent(e);

                var agentNextPosition = enemyMovement.agentNextPosition;


                if (distanceToOpponent < stopRange && enemyMeleeMovement.switchUp)
                {
                    weaponMovement = false;
                    meleeMovement = true;
                }

                if (hasWeapon && hasAim && weaponMovement && !meleeMovement)
                {
                    var weapon = weaponGroup[e];
                    var actorWeaponAim = actorWeaponAimGroup[e];
                    if (weapon is { IsFiring: 1, tooFarTooAttack: false } ||
                        distanceToOpponent < shootRange)
                    {
                        if (weapon.firingStage == FiringStage.None)
                        {
                            weaponRaised = WeaponMotion.Started;
                        }
                        else if (weapon is { IsFiring: 1, firingStage: FiringStage.Start })
                        {
                            weapon.firingStage = FiringStage.Update;
                            weaponRaised = WeaponMotion.Started;
                        }

                        weapon.IsFiring = 1;
                    }

                    actorWeaponAim.weaponRaised = weaponRaised;
                    weaponGroup[e] = weapon;
                    actorWeaponAimGroup[e] = actorWeaponAim;
                }

                var backupZoneClose = enemyMeleeMovement.combatStrikeDistanceZoneBegin;
                var backupZoneFar = enemyMeleeMovement.combatStrikeDistanceZoneEnd;
                var strike = false;

                if (distanceToOpponent < backupZoneClose && meleeMovement)
                {
                    enemyMovement.backup = true;
                    enemyMovement.updateAgent = false;
                    enemyMovement.speedMultiple = distanceToOpponent / backupZoneClose;
                    var n = randomNumber;
                    if (n <= aggression && enemyMovement.backupTimer <= 0 && distanceToOpponent > backupZoneClose / 2)
                    {
                        enemyMovement.backup = false;
                        enemyMovement.updateAgent = true;
                        strike = true;
                    }
                }

                if (enemyMovement.backup && distanceToOpponent > backupZoneFar && meleeMovement)
                {
                    enemyMovement.backup = false;
                    enemyMovement.updateAgent = true;
                    enemyMovement.backupTimer = 0;
                }
                else if (distanceToOpponent >= backupZoneClose && distanceToOpponent <= backupZoneFar && meleeMovement)
                {
                    enemyMovement.speedMultiple =
                        math.sqrt((distanceToOpponent - backupZoneClose) / (backupZoneFar - backupZoneClose));
                    var n = randomNumber;
                    if (n <= aggression && enemyMovement.backupTimer <= 0)
                    {
                        strike = true;
                        enemyMovement.backup = false;
                        enemyMovement.updateAgent = true;
                    }
                }

                if (basicMovement)
                {
                    strike = false;
                }

                var backup = enemyMovement.backup; //read only after set above
                var selectMove = enemyState.currentStateTimer > enemyState.currentStateRequiredTime;

                MoveStates moveState = MoveStates.Chase;
                if (stayHome && distFromStation > chaseRange)
                {
                    chaseRange = distFromStation;
                }

                if (!backup && strike && distanceToOpponent < chaseRange && selectMove)
                {
                    //checkedComponent.anyAttackStarted = true;
                    //Debug.Log("move state " + enemyState.MoveState);
                    enemyState.selectMove = true;
                    enemyState.enemyStrikeAllowed = true;
                    enemyState.Zone = 3;
                }
                else if (checkedComponent.anyAttackStarted == false) //needed?
                {
                    if (backup && distanceToOpponent < chaseRange && meleeMovement)
                    {
                        moveState = MoveStates.Default;
                        enemyState.Zone = 2;
                        enemyMovement.AgentBackupMovement = true; //SetBackup EnemyMove
                        agentNextPosition = enemyTransform.Position + matchup.backupDirection;
                        enemyMovement.backupTimer += deltaTime;
                        if (enemyMovement.backupTimer >= enemyMovement.backupSeconds)
                        {
                            enemyMovement.backupTimer = 0;
                        }
                    }
                    else if (distanceToOpponent < enemyMeleeMovement.combatRangeDistance &&
                             distanceToOpponent < chaseRange && meleeMovement)
                    {
                        moveState = MoveStates.Default;
                        enemyState.Zone = 2;
                    }
                    else if (distanceToOpponent < chaseRange && distanceToOpponent > stopRange)
                    {
                        matchup.currentStrikeDistanceAdjustment = 1;
                        moveState = MoveStates.Chase;
                        enemyState.Zone = 1;
                    }
                    else if (distanceToOpponent < chaseRange && distanceToOpponent > stopRange && meleeMovement)
                    {
                        enemyState.Zone = 1;
                        moveState = MoveStates.Idle;
                    }
                    else if (distanceToOpponent >= chaseRange && distanceToOpponent < stopRange && meleeMovement &&
                             defensiveStrategy.currentRole == DefensiveRoles.Chase)
                    {
                        enemyState.Zone = 1;
                        moveState = MoveStates.Idle;
                    }
                    else if (distanceToOpponent >= chaseRange && defensiveStrategy.currentRole == DefensiveRoles.Patrol)
                    {
                        enemyState.Zone = 1;
                        moveState = MoveStates.Patrol;
                    }
                    else
                    {
                        enemyState.Zone = 1;
                        moveState = MoveStates.Stopped;
                    }

                    var lastState = enemyState.MoveState; //reads previous
                    // enemyState.currentStateTimer += deltaTime;
                    // if (moveState == lastState || enemyState.MoveState == MoveStates.Default) //no change
                    // {
                    //     enemyState.MoveState = moveState;
                    // }
                    // else if (moveState != lastState &&
                    //          enemyState.currentStateTimer >
                    //          enemyState.currentStateRequiredTime) //switched but after time required in role
                    // {
                    //     enemyState.MoveState = moveState;
                    //     enemyState.currentStateTimer = 0;
                    // }


                    var updateAgent = enemyMovement.updateAgent;

                    if (updateAgent && moveState != MoveStates.Stopped)
                    {
                        enemyMovement.AgentAnimationMovement = true;
                    }
                    else //if stopped or agent off control rotation
                    {
                        enemyMovement.agentNextPosition = agentNextPosition;
                        if (transformGroup.HasComponent(closestOpponent)) //Required?
                        {
                            var direction = matchup.backupDirection;
                            direction.y = 0;

                            var targetRotation =
                                quaternion.LookRotationSafe(-direction, math.up()); //always face player
                            enemyTransform.Rotation = targetRotation;
                            transformGroup[e] = enemyTransform;
                        }
                    }
                }

                if (enemyState.isAnimatingMelee && enemyState.MoveState != MoveStates.Combat) //combat start
                {
                    enemyState.currentStateTimer = 0;
                }
                else if (enemyState.isAnimatingMelee) // combat updating
                {
                    enemyState.MoveState = MoveStates.Combat;
                }
                else if (enemyState.isAnimatingMelee == false)
                {
                    enemyState.currentStateTimer += deltaTime;
                }

                enemyState.MoveState = enemyState.isAnimatingMelee ? MoveStates.Combat : moveState;
                
                var impulseFactor = 1f;
                if (impulse.activate)
                {
                    impulseFactor = impulse.animSpeedRatio;
                }
                else if (impulse.activateOnReceived)
                {
                    impulseFactor = impulse.animSpeedRatioOnReceived;
                }

                enemyMovement.animatorSpeed = impulseFactor;
                enemyMovement.forwardVelocity = impulseFactor;
                var moveSpeed = defensiveStrategy.botSpeed;
                var velZ = 1f;
                moveState = enemyState.MoveState;
                var speed = enemyState.Zone >= 2 ? moveSpeed : moveSpeed * 1.5f;
                if (moveState is MoveStates.Chase)
                {
                    speed = moveSpeed * 2;
                }
                else if (moveState is MoveStates.Idle or MoveStates.Stopped or MoveStates.Defensive
                         or MoveStates.Combat)
                {
                    speed = 0;
                    velZ = 0;
                    enemyState.Zone = 1;
                }
                else if (moveState == MoveStates.Patrol)
                {
                    speed = moveSpeed * 1f;
                    velZ = 1f;
                }

                locomotion.Speed = speed * impulseFactor;
                velZ *= impulseFactor;
                enemyMovement.forwardVelocity = velZ;
            }
        }
    }
}