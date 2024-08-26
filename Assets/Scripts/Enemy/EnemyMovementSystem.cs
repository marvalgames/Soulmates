using Collisions;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemy
{
    [RequireMatchingQueriesForUpdate]
    public partial class EnemyMovementSystem : SystemBase
    {
        private static readonly int Zone = Animator.StringToHash("Zone");

        [DeallocateOnJobCompletion] private NativeArray<Entity> PlayerEntities;

        private EntityQuery playerQuery;

        protected override void OnUpdate()
        {
            if (LevelManager.instance.endGame ||
                LevelManager.instance.currentLevelCompleted >= LevelManager.instance.totalLevels) return;


            var roleReversalDisabled =
                LevelManager.instance.levelSettings[LevelManager.instance.currentLevelCompleted].roleReversalMode ==
                RoleReversalMode.Off;

            var toggleEnabled =
                LevelManager.instance.levelSettings[LevelManager.instance.currentLevelCompleted].roleReversalMode ==
                RoleReversalMode.Toggle;
            

            var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();
            playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
            PlayerEntities = playerQuery.ToEntityArray(Allocator.Temp);
            var playerIsFiring = false;
            var playerInShootingRange = true;
            var enemyInShootingRange = true;
            for (var i = 0; i < PlayerEntities.Length; i++)
            {
                var e = PlayerEntities[i];
                var hasWeapon = SystemAPI.HasComponent<WeaponComponent>(e);
                if (hasWeapon && roleReversalDisabled == false)
                {
                    var player = SystemAPI.GetComponent<WeaponComponent>(e);
                    if (player is { IsFiring: 1, roleReversal: RoleReversalMode.On }) playerIsFiring = true;
                    if (player.roleReversal == RoleReversalMode.On) player.IsFiring = 0;
                    SystemAPI.SetComponent(e, player);
                }
            }

       
            Entities.WithoutBurst().WithNone<Pause>().WithAll<EnemyComponent>().ForEach
            (
                (
                    Entity e,
                    ref WeaponComponent weaponComponent,
                    in MatchupComponent matchupComponent,
                    in LevelCompleteComponent levelCompleteComponent,
                    in LocalTransform localTransform
                ) =>
                {
                    if (SystemAPI.HasComponent<DeadComponent>(e) == false) return;
                    if (SystemAPI.GetComponent<DeadComponent>(e).isDead) return;
                    if (matchupComponent.targetEntity == Entity.Null || matchupComponent.closestPlayerEntity == Entity.Null) return;
                    if (levelCompleteComponent.areaIndex > LevelManager.instance.currentLevelCompleted) return;
                    weaponComponent.tooFarTooAttack = false;

                    
                    var enemyPosition = localTransform.Position;
                    //var pl = matchupComponent.opponentTargetPosition;
                    var pl = SystemAPI.GetComponent<LocalTransform>(matchupComponent.closestPlayerEntity).Position;
                    pl.y = 0;
                    var en = enemyPosition;
                    en.y = 0;
                    var distFromOpponent = math.distance(pl, en);

                    if (distFromOpponent <= weaponComponent.roleReversalRangeMechanic &&
                        toggleEnabled)

                    {
                        enemyInShootingRange = false;
                    }
                    
                    var multiplier = 2.0f;
                    
                    if (distFromOpponent > weaponComponent.roleReversalRangeMechanic * multiplier &&
                        !roleReversalDisabled)

                    {
                        weaponComponent.tooFarTooAttack = true;
                    }
                }

            ).Run();



            Entities.WithoutBurst().WithNone<Pause>().WithAll<EnemyComponent>().WithAll<EnemyMeleeMovementComponent>()
                .WithAll<EnemyWeaponMovementComponent>().ForEach
                (
                    (
                        EnemyMove enemyMove,
                        Entity e,
                        LevelCompleteComponent levelCompleteComponent,
                        ref EnemyStateComponent enemyState,
                        ref CheckedComponent checkedComponent,
                        ref MatchupComponent matchupComponent
                    ) =>
                    {
                        if (SystemAPI.HasComponent<DeadComponent>(e) == false) return;
                        if (SystemAPI.GetComponent<DeadComponent>(e).isDead) return;
                        if (!SystemAPI.HasComponent<LocalTransform>(e)) return;
                        
                        if (matchupComponent.closestOpponent == Entity.Null || matchupComponent.closestPlayerEntity == Entity.Null) return;
                        if (levelCompleteComponent.areaIndex > LevelManager.instance.currentLevelCompleted) return;
                        var animator = enemyMove.anim;
                        var defensiveRole = SystemAPI.GetComponent<DefensiveStrategyComponent>(e).currentRole;
                        var enemyMovementComponent = SystemAPI.GetComponent<EnemyMovementComponent>(e);
                        var basicMovement = enemyMovementComponent.enabled;
                        var enemyMeleeMovementComponent = SystemAPI.GetComponent<EnemyMeleeMovementComponent>(e);
                        var enemyWeaponMovementComponent = SystemAPI.GetComponent<EnemyWeaponMovementComponent>(e);
                        var enemyBehaviourComponent = SystemAPI.GetComponent<EnemyBehaviourComponent>(e);
                        var meleeMovement = enemyMeleeMovementComponent.enabled;
                        var weaponMovement = enemyWeaponMovementComponent.enabled;
                        var enemyStrikeAllowed = enemyState.enemyStrikeAllowed;
                        
                        enemyMove.speedMultiple = 1;
                        enemyState.selectMove = false;
                        var role = enemyMove.enemyRole;
                        var localTransform = SystemAPI.GetComponent<LocalTransform>(e); 

                        if (role != EnemyRoles.None)
                        {
                            var enemyPosition = localTransform.Position;
                            var homePosition = enemyMove.originalPosition;
                            var stayHome = enemyBehaviourComponent.useDistanceFromStation;

                            var closestPlayerEntity = matchupComponent.closestPlayerEntity;
                            var closestPlayerPosition = SystemAPI.GetComponent<LocalTransform>(closestPlayerEntity).Position;
                            closestPlayerPosition.y = 0;

                            var closestOpponentEntity = matchupComponent.closestOpponent;
                            var closestOpponentPosition = SystemAPI.GetComponent<LocalTransform>(closestOpponentEntity).Position;
                            closestOpponentPosition.y = 0;
                            
                            var way = matchupComponent.wayPointTargetPosition;
                            var en = enemyPosition;
                            en.y = 0;
                            var distFromOpponent = math.distance(closestOpponentPosition, en);
                            var distFromPlayer = math.distance(closestPlayerPosition, en);
                            var distFromWaypoint = math.distance(way, en);
                            var distFromStation = math.distance(homePosition, enemyPosition);
                            var chaseRange = enemyBehaviourComponent.chaseRange;
                            var aggression = enemyBehaviourComponent.aggression;
                            var stopRange = enemyBehaviourComponent.stopRange;
                            var weaponRaised = WeaponMotion.None;
                            //if closer than weapon shooting stop range always melee if melee switch active 
                            var hasWeaponComponent = SystemAPI.HasComponent<WeaponComponent>(e);
                            
                            
                            var enemyXZ = new float3(closestPlayerPosition.x,  closestPlayerPosition.y, closestPlayerPosition.z);
                            var playerXZ = new float3(enemyPosition.x, enemyPosition.y, enemyPosition.z);
                            var direction = math.normalize( enemyXZ - playerXZ);
                            direction.y = 0;
                            var targetRotation = quaternion.LookRotationSafe(direction, math.up()); //always face player
                            localTransform.Rotation = targetRotation;
                            localTransform.Position = enemyMovementComponent.agentNextPosition;
                            //Debug.Log("agent " + localTransform.Position);
                            SystemAPI.SetComponent(e, localTransform);


                            if (hasWeaponComponent)
                            {
                                var weaponComponent = SystemAPI.GetComponent<WeaponComponent>(e);
                                if (weaponComponent.tooFarTooAttack)
                                {
                                    weaponMovement = false;
                                    meleeMovement = false;
                                }
                            }
                            
                            if (distFromOpponent < stopRange && weaponMovement && enemyMeleeMovementComponent.switchUp)
                            {
                                var weaponComponent = SystemAPI.GetComponent<WeaponComponent>(e);
                                weaponMovement = false;
                                meleeMovement = true;
                            }
                            
                            if (hasWeaponComponent)
                            {
                                var weaponComponent = SystemAPI.GetComponent<WeaponComponent>(e);
                                var roleReversal = weaponComponent.roleReversal;

                                if (distFromPlayer > weaponComponent.roleReversalRangeMechanic &&
                                    toggleEnabled ||
                                    roleReversalDisabled)
                                {
                                    roleReversal = RoleReversalMode.Off;
                                }
                                else
                                {
                                    roleReversal = RoleReversalMode.On; //fix need original
                                    playerInShootingRange = false;
                                }

                                if (roleReversal == RoleReversalMode.On) weaponMovement = true;

                                if (SystemAPI.HasComponent<ActorWeaponAimComponent>(e))
                                {
                                    var actorWeaponAim = SystemAPI.GetComponent<ActorWeaponAimComponent>(e);

                                    if (playerIsFiring && roleReversal == RoleReversalMode.On && !weaponComponent.tooFarTooAttack  || distFromOpponent <
                                        enemyWeaponMovementComponent.shootRangeDistance && weaponMovement &&
                                        roleReversal == RoleReversalMode.Off && enemyInShootingRange)
                                    {
                                        if (weaponComponent.firingStage == FiringStage.None)
                                        {
                                            weaponRaised = WeaponMotion.Started;
                                        }
                                        else if(weaponComponent is { IsFiring: 1, firingStage: FiringStage.Start })
                                        {
                                            weaponComponent.firingStage = FiringStage.Update;
                                            weaponRaised = WeaponMotion.Started;
                                        }
                                        

                                        weaponComponent.IsFiring = 1;//hmm
                                    }
                                    actorWeaponAim.weaponRaised = weaponRaised;
                                    SystemAPI.SetComponent(e, actorWeaponAim);
                                    SystemAPI.SetComponent(e, weaponComponent);

                                    
                                }
                            }
                            var backupZoneClose = enemyMeleeMovementComponent.combatStrikeDistanceZoneBegin;
                            var backupZoneFar = enemyMeleeMovementComponent.combatStrikeDistanceZoneEnd;

                            var strike = false;

                            if (distFromOpponent < backupZoneClose && meleeMovement)
                            {
                                enemyMove.backup = true; //only time to turn on 
                                enemyMove.speedMultiple = distFromOpponent / backupZoneClose; //try zero
                                float n = Random.Range(0, 100);
                                if (n <= aggression && enemyMove.backupTimer <= 0 &&
                                    distFromOpponent > backupZoneClose / 2)
                                {
                                    enemyMove.backup = false; //only time to turn on
                                    strike = true;
                                }
                            }

                            if (enemyMove.backup && distFromOpponent > backupZoneFar && meleeMovement)
                            {
                                enemyMove.backup = false; //only time to turn off
                                enemyMove.backupTimer = 0;
                            }
                            else if (distFromOpponent >= backupZoneClose && distFromOpponent <= backupZoneFar &&
                                     meleeMovement)
                            {
                                enemyMove.speedMultiple =
                                    math.sqrt((distFromOpponent - backupZoneClose) / (backupZoneFar - backupZoneClose));
                                //enemyMove.speedMultiple *= 2;
                                float n = Random.Range(0, 100);

                                if (n <= aggression * 1.0f && enemyMove.backupTimer <= 0)
                                {
                                    strike = true;
                                    enemyMove.backup = false; //try
                                }
                            }

                            
                            if(basicMovement || !enemyStrikeAllowed) strike = false;
                            
                            var backup = enemyMove.backup;
                            if (stayHome && distFromStation > chaseRange) chaseRange = distFromStation;
                            MoveStates moveState;
                            if (checkedComponent.anyAttackStarted == false && backup == false && strike &&
                                distFromOpponent < chaseRange)
                            {
                                moveState = MoveStates.Default;
                                enemyState.selectMove = true;
                                animator.SetInteger(Zone, 3);
                            }
                            else if (checkedComponent.anyAttackStarted == false)
                            {
                                if (backup && distFromOpponent < chaseRange && meleeMovement)
                                {
                                    moveState = MoveStates.Default;
                                    animator.SetInteger(Zone, 2);
                                    enemyMove.SetBackup();
                                }
                                else if (distFromOpponent < enemyMeleeMovementComponent.combatRangeDistance &&
                                         distFromOpponent < chaseRange &&
                                         meleeMovement)
                                {
                                    moveState = MoveStates.Default;
                                    animator.SetInteger(Zone, 2);
                                }
                                else if (distFromOpponent < chaseRange &&
                                         distFromOpponent > stopRange) //weapon 1st option
                                {
                                    if (animator.GetComponent<EnemyMelee>())
                                        matchupComponent.currentStrikeDistanceAdjustment =
                                            1; //reset when out of strike range

                                    moveState = MoveStates.Chase;
                                    animator.SetInteger(Zone, 1);
                                }
                                else if (distFromOpponent < chaseRange) //weapon 2nd
                                {
                                    animator.SetInteger(Zone, 1);
                                    moveState = MoveStates.Idle;
                                }
                                else if (distFromOpponent >= chaseRange &&
                                         (role == EnemyRoles.Chase || defensiveRole == DefensiveRoles.Chase))
                                {
                                    animator.SetInteger(Zone, 1);
                                    moveState = MoveStates.Idle;
                                }
                                else if (distFromOpponent >= chaseRange && role == EnemyRoles.Patrol)
                                {
                                    animator.SetInteger(Zone, 1);
                                    moveState = MoveStates.Patrol;
                                    enemyMove.Patrol();
                                }
                                else
                                {
                                    animator.SetInteger(Zone, 1);
                                    moveState = MoveStates.Stopped;
                                }

                                //enemyMove.FaceWaypoint();
                                var lastState = enemyState.MoveState; //reads previous
                                enemyState.currentStateTimer += SystemAPI.Time.DeltaTime;
                                if (moveState == lastState || enemyState.MoveState == MoveStates.Default) //no change
                                {
                                    enemyState.MoveState = moveState;
                                }
                                else if (moveState != lastState &&
                                         enemyState.currentStateTimer > 1) //switched but after time required in role
                                {
                                    enemyState.MoveState = moveState;
                                    enemyState.currentStateTimer = 0;
                                }

                                var state = enemyState.MoveState;
                                float3 wayPointTargetPosition = new float3();
                                float3 opponentTargetPosition = new float3();
                                float3 targetPosition = new float3();

                                var targetEntity = matchupComponent.targetEntity;
                                matchupComponent.isWaypointTarget = false;
                                opponentTargetPosition = transformGroup[targetEntity].Position;


                                wayPointTargetPosition =
                                    enemyMove.wayPoints[enemyMove.currentWayPointIndex].targetPosition;

                                if (state == MoveStates.Patrol)
                                {
                                    matchupComponent.isWaypointTarget = true;
                                    targetPosition = wayPointTargetPosition;
                                }
                                else
                                {
                                    matchupComponent.isWaypointTarget = false;
                                    targetPosition = opponentTargetPosition;
                                    matchupComponent.aimTarget = transformGroup[targetEntity];
                                }

                                matchupComponent.wayPointTargetPosition = wayPointTargetPosition;
                                matchupComponent.opponentTargetPosition = opponentTargetPosition;

                                enemyMove.UpdateEnemyMovement();
                                enemyMove.AnimationMovement(targetPosition);
                                //enemyMove.FaceWaypoint();
                            }
                        }
                    }
                ).Run();

            for (var i = 0; i < PlayerEntities.Length; i++)
            {
                var e = PlayerEntities[i];
                var hasWeapon = SystemAPI.HasComponent<WeaponComponent>(e);
                if (hasWeapon)
                {
                    var player = SystemAPI.GetComponent<WeaponComponent>(e);
                    player.roleReversal = playerInShootingRange ? RoleReversalMode.Off : RoleReversalMode.On;
                    SystemAPI.SetComponent(e, player);
                }
            }
        }
    }
}