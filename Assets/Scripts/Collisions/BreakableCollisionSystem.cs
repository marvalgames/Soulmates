using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Collisions
{
    public struct BrokenComponent : IComponentData, IEnableableComponent
    {
        public bool Value;
    }

    public struct BrokenEffectComponent : IComponentData
    {
        public bool Value;
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class BreakableCollisionSystem : SystemBase
    {
        EndFixedStepSimulationEntityCommandBufferSystem m_ecbSystem;

        protected override void OnCreate()
        {
            m_ecbSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        }

        protected override void OnDestroy()
        {
        }


        protected override void OnUpdate()
        {
            var collisionJob = new BreakableCollisionJob
            {
                Ecb = m_ecbSystem.CreateCommandBuffer(),
                breakableGroup = GetComponentLookup<BreakableComponent>(false),
                velocityGroup = GetComponentLookup<TriggerComponent>(false),
                ammoGroup = GetComponentLookup<AmmoDataComponent>(true),
                gravityGroup = GetComponentLookup<PhysicsGravityFactor>(false),
            };

            Dependency = collisionJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        }

        [BurstCompile]
        struct BreakableCollisionJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<AmmoDataComponent> ammoGroup;
            [ReadOnly] public ComponentLookup<TriggerComponent> velocityGroup;


            public ComponentLookup<BreakableComponent> breakableGroup;
            public ComponentLookup<PhysicsGravityFactor> gravityGroup;

            public EntityCommandBuffer Ecb;

            public void Execute(CollisionEvent ev)
            {
                var a = ev.EntityA;
                var b = ev.EntityB;
                //bool canBreak = ammoGroup.HasComponent(b) == true || velocityGroup.HasComponent(b);
                bool canBreak1 = velocityGroup.HasComponent(b);
                bool canBeBroken1 = gravityGroup.HasComponent(a) && breakableGroup.HasComponent(a);
                bool canBreak2 = velocityGroup.HasComponent(a);
                bool canBeBroken2 = gravityGroup.HasComponent(b) && breakableGroup.HasComponent(b);


                if (canBreak1 && canBeBroken1)
                {
                    var breakable = breakableGroup[a];
                    var playAndDestroyEffectComponent = new PlayAndDestroyEffectComponent();
                    if (breakable.broken == false)
                    {
                        //Debug.Log("BREAKABLE A");
                        var breakableParentEntity = breakable.parentEntity;
                        playAndDestroyEffectComponent.effectIndex = breakable.effectIndex;
                        playAndDestroyEffectComponent.play = true; //not needed yet since have index (now reverse is true)


                        var gravity = gravityGroup[a];
                        gravity.Value = breakable.gravityFactorAfterBreaking;
                        Ecb.SetComponent(a, gravity);
                        breakable.broken = true;
                        breakable.playEffect = true;
                        if (ammoGroup.HasComponent(b))
                        {
                            var shooter = ammoGroup[b].Shooter; //n/a
                            breakable.breakerEntity = shooter;
                        }

                        Ecb.SetComponent(a, breakable);
                        Ecb.AddComponent<BrokenComponent>(a);
                        Ecb.AddComponent(breakableParentEntity, playAndDestroyEffectComponent);
                        //adds to parent transform node so then it will play effects and everything from there (it is a game object entity in scene)
                    }
                }
                else if (canBreak2 && canBeBroken2)
                {
                    var breakable = breakableGroup[b];
                    var playAndDestroyEffectComponent = new PlayAndDestroyEffectComponent();
                    if (breakable.broken == false)
                    {
                        //Debug.Log("BREAKABLE B");
                        var breakableParentEntity = breakable.parentEntity;
                        playAndDestroyEffectComponent.effectIndex = breakable.effectIndex;
                        playAndDestroyEffectComponent.play = true; //not needed yet since have index (now reverse is true)


                        var gravity = gravityGroup[b];
                        gravity.Value = breakable.gravityFactorAfterBreaking;
                        Ecb.SetComponent(b, gravity);
                        breakable.broken = true;
                        breakable.playEffect = true;
                        if (ammoGroup.HasComponent(a))
                        {
                            var shooter = ammoGroup[a].Shooter; //n/a
                            breakable.breakerEntity = shooter;
                        }

                        Ecb.SetComponent(b, breakable);
                        Ecb.AddComponent<BrokenComponent>(b);
                        Ecb.AddComponent(breakableParentEntity,
                            playAndDestroyEffectComponent);
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BreakableCollisionSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct BreakableCollisionHandlerSystem : ISystem
    {
        private EntityQuery _breakablesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);
            builder.WithAll<BreakableComponent>();
            _breakablesQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton =
                SystemAPI.GetSingleton<
                    EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var breakableEntities = _breakablesQuery.ToEntityArray(Allocator.TempJob);
            var brokenGroup = SystemAPI.GetComponentLookup<BrokenComponent>();
            var breakableGroup = SystemAPI.GetComponentLookup<BreakableComponent>();
            var gravityGroup = SystemAPI.GetComponentLookup<PhysicsGravityFactor>();


            var job = new BreakableJob()
            {
                breakableEntities = breakableEntities,
                brokenGroup = brokenGroup,
                breakableGroup = breakableGroup,
                gravityGroup = gravityGroup,
                Ecb = ecb
            };
            job.Schedule();
        }

        [WithAll(typeof(BrokenComponent))]
        [BurstCompile]
        partial struct BreakableJob : IJobEntity
        {
            [ReadOnly][DeallocateOnJobCompletion] public NativeArray<Entity> breakableEntities;
            [ReadOnly] public ComponentLookup<BrokenComponent> brokenGroup;
            [ReadOnly] public ComponentLookup<BreakableComponent> breakableGroup;
            [ReadOnly] public ComponentLookup<PhysicsGravityFactor> gravityGroup;
            public EntityCommandBuffer Ecb;


            void Execute(Entity e)
            {
                if (brokenGroup.HasComponent(e))
                {
                    //Debug.Log("BROKEN");
                    var brokenGroupIndex = breakableGroup[e].groupIndex;
                    for (var i = 0; i < breakableEntities.Length; i++)
                    {
                        var breakableEntity = breakableEntities[i];
                        var breakableComponent = breakableGroup[breakableEntity];
                        if (breakableComponent.groupIndex == brokenGroupIndex &&
                            gravityGroup.HasComponent(breakableEntity))
                        {
                            var gravity = gravityGroup[breakableEntity];
                            gravity.Value = breakableComponent.gravityFactorAfterBreaking;
                            Ecb.SetComponent(breakableEntity, gravity);
                            breakableComponent.broken = true;
                            Ecb.SetComponent(breakableEntity, breakableComponent);
                        }
                    }

                    Ecb.RemoveComponent<BrokenComponent>(e);
                }
            }
        }
    }


}