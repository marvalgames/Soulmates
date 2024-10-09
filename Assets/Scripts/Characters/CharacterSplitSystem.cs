using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(CleanupSystem))]
public partial struct SplitSystem : ISystem
{
    private EntityQuery enemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SplitterComponent>();
        var enemyBuilder = new EntityQueryBuilder(Allocator.Temp);
        enemyBuilder.WithAll<SplitComponent, EnemyComponent>();
        enemyQuery = state.GetEntityQuery(enemyBuilder);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EndSimulationEntityCommandBufferSystem.Singleton commandBufferSystem =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var enemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
        var damageGroup = SystemAPI.GetComponentLookup<DamageComponent>();
        var splitGroup = SystemAPI.GetComponentLookup<SplitComponent>();
        var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();

        var deltaTime = SystemAPI.Time.DeltaTime;
        // Schedule job
        SplitJob splitJob = new()
        {
            CommandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged),
            DamageGroup = damageGroup,
            SplitGroup = splitGroup,
            EnemyEntities = enemyEntities,
            TransformGroup = transformGroup,
            DeltaTime = deltaTime
        };
        state.Dependency = splitJob.Schedule(state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    private partial struct SplitJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer CommandBuffer;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public ComponentLookup<SplitComponent> SplitGroup;
        [ReadOnly] public ComponentLookup<DamageComponent> DamageGroup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformGroup;

        public void Execute(in SplitterComponent splitterComponent)
        {
            var enemies = EnemyEntities.Length;
            //Debug.Assert(enemies >= 1);
            for (var i = 0; i < enemies; i++)
            {
                var enemyE = EnemyEntities[i];
                var splitComponent = SplitGroup[enemyE];
                splitComponent.timeRemaining -= DeltaTime;
                if (splitComponent.timeRemaining <= 0)
                {
                    splitComponent.isRunning = false;
                    splitComponent.timeRemaining = 0;
                }

                if (DamageGroup.HasComponent(enemyE))
                {

                    var damage = DamageGroup[enemyE].DamageReceived;


                    if (damage >= 5 && splitComponent is { split: false, isRunning: false })
                    {
                        var instance = this.CommandBuffer.Instantiate(splitterComponent.splitPrefab);
                        var position = TransformGroup[enemyE].Position;
                        var rotation = TransformGroup[enemyE].Rotation;
                        var scale = TransformGroup[enemyE].Scale;
                        if (splitComponent.scale) scale *= 1.01f;
                        this.CommandBuffer.SetComponent(instance,
                            LocalTransform.FromPositionRotationScale(position, rotation, scale));
                        splitComponent.split = true;
                    }
                }

                this.CommandBuffer.SetComponent(enemyE, splitComponent);
            }
        }
    }
}