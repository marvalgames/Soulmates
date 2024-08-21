using System.Collections.Generic;
using Collisions;
using Sandbox.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[InternalBufferCapacity(8)]
public struct BossAmmoListBuffer : IBufferElementData
{
    public Entity E;
    public Entity StartLocationEntity;
    //public LocalToWorld ammoStartLocalToWorld;
    public LocalTransform AmmoStartTransform;
}
//public struct BossWaypointDurationBufferElement : IBufferElementData
//{
//  public float duration;
//}

public struct BossMovementComponent : IComponentData
{
    public int CurrentIndex;
    public float CurrentWayPointTimer;
    public bool CurrentAnimationStarted;
    public float Speed;
    public bool Repeat;
    public int StartStrike;
    public float RotateSpeed;
    public bool WayPointReached;
}

public struct BossStrategyComponent : IComponentData
{
    public bool AimAtPlayer;
    public float StopDistance;
}

public struct BossComponent : IComponentData
{
}


public class BossComponentAuthoring : MonoBehaviour
{
    [SerializeField] float BossSpeed = 1;
    [SerializeField] float RotateSpeed = 90;
    [SerializeField] bool Repeat = true;
    [SerializeField] bool AimAtPlayer = true;
    [SerializeField] float StopDistance = 5;
    [Header("Misc")] [SerializeField] bool checkWinCondition = true;
    [SerializeField] bool paused = true;
    [SerializeField] int areaIndex;
    public List<WayPoint> wayPoints = new List<WayPoint>();
    public bool enemiesAttack;

    class BossComponentBaker : Baker<BossComponentAuthoring>
    {
        public override void Bake(BossComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, new EnemiesAttackComponent  {enemiesAttack = authoring.enemiesAttack} );
            SetComponentEnabled<EnemiesAttackComponent>(e, authoring.enemiesAttack);

            
            AddComponent(e, 
                new BossMovementComponent
                {
                    WayPointReached = false,
                    Speed = authoring.BossSpeed, Repeat = authoring.Repeat, RotateSpeed = authoring.RotateSpeed
                });

            AddComponent(e, 
                new BossStrategyComponent
                {
                    AimAtPlayer = authoring.AimAtPlayer, StopDistance = authoring.StopDistance
                });

            AddComponent(e, new DeadComponent());
            AddComponent(e, new BossComponent());
            AddComponent(e, new EnemyComponent()); //keep?
            AddComponent(e, new CheckedComponent());

            if (authoring.paused == true)
            {
                AddComponent(e, new Pause());
            }

            AddComponent(e, new StatsComponent()
                {
                    shotsFired = 0,
                    shotsLanded = 0
                }
            );



            
            AddComponent(e, new SkillTreeComponent()
                {
                    e = e,
                    availablePoints = 0,
                    SpeedPts = 0,
                    PowerPts = 0,
                    ChinPts = 0,
                    baseSpeed = 0,
                    CurrentLevel = 1,
                    CurrentLevelXp = 0,
                    PointsNextLevel = 10
                }
            );


            AddComponent(e, new WinnerComponent
                {
                    active = true,
                    goalCounter = 0,
                    goalCounterTarget = 0, //ie how many players you have to save - usually zero
                    targetReached = false,
                    endGameReached = false,
                    checkWinCondition = authoring.checkWinCondition
                }
            );

            AddComponent(e, 
                new LevelCompleteComponent
                {
                    active = true,
                    targetReached = false,
                    checkWinCondition = authoring.checkWinCondition,
                    areaIndex = authoring.areaIndex
                }
            );

            var buffer = AddBuffer<BossWaypointBufferElement>(e);

            for (var i = 0; i < authoring.wayPoints.Count; i++)
            {
                var bossWaypoint = new BossWaypointBufferElement
                {
                    wayPointPosition = authoring.wayPoints[i].targetPosition + authoring.transform.position,
                    wayPointSpeed = authoring.wayPoints[i].speed,
                    wayPointChase = authoring.wayPoints[i].chase,
                    duration = authoring.wayPoints[i].duration,
                    wayPointAction = (int)authoring.wayPoints[i].action,
                    audioListIndex = authoring.wayPoints[i].audioListIndex,
                    weaponListIndex = authoring.wayPoints[i].weaponListIndex,
                    ammoListIndex = authoring.wayPoints[i].ammoListIndex
                };

                
                buffer.Add
                (
                    bossWaypoint
                );
            }
        }
    }

    
}