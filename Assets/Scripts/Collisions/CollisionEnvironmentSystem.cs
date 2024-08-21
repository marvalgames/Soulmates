using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using PlayerMoveSystem = Sandbox.Player.PlayerMoveSystem;


namespace Collisions
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PlayerMoveSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class CollisionEnvironmentSystem : SystemBase
    {
        private EndFixedStepSimulationEntityCommandBufferSystem _mEcbSystem;

        protected override void OnCreate()
        {
            _mEcbSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var collisionEnvJob = new CollisionEnvJob
            {
                Ecb = _mEcbSystem.CreateCommandBuffer(),
                TriggerGroup = GetComponentLookup<TriggerComponent>(true),
                ApplyGroup = GetComponentLookup<ApplyImpulseComponent>(false)
            };

            Dependency = collisionEnvJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
            Dependency.Complete();
        } // OnUpdate

        [BurstCompile]
        struct CollisionEnvJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<TriggerComponent> TriggerGroup;
            public ComponentLookup<ApplyImpulseComponent> ApplyGroup;
            public EntityCommandBuffer Ecb;

            public void Execute(CollisionEvent ev) 
            {
                var a = ev.EntityA;
                var b = ev.EntityB;
                if (TriggerGroup.HasComponent(a) == false || TriggerGroup.HasComponent(b) == false) return;
                var triggerComponentA = TriggerGroup[a];
                var triggerComponentB = TriggerGroup[b];
                bool groundCollision = triggerComponentA.Type == (int)TriggerType.Player
                                       && triggerComponentB.Type == (int)TriggerType.Ground;

                if (ApplyGroup.HasComponent(a) && groundCollision)
                {
                    var apply = ApplyGroup[a];
                    apply.GroundCollision = true;
                    ApplyGroup[a] = apply;
                }
            
            }
        }
    }
}