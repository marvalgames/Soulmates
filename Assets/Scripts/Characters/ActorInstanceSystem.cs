using Sandbox.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Player
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct ActorInstanceSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (actorClass, actorComponent, entity)
                     in SystemAPI.Query<ActorClass, RefRW<ActorComponent>>().WithEntityAccess())
            {
                if (!actorComponent.ValueRW.instantiated)
                {
                    var go = GameObject.Instantiate(actorClass.actorPrefab);
                    go.SetActive(true);
                    commandBuffer.AddComponent(entity, new ActorInstance { actorPrefabInstance = go, linkedEntity = entity });
                    actorComponent.ValueRW.instantiated = true;
                    
                }
            }

            foreach (var (actor, entityTransform, entity) in SystemAPI.Query<ActorInstance, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                //actor.actorPrefabInstance.SetActive(true);
                actor.actorPrefabInstance.transform.position = entityTransform.ValueRO.Position;
                actor.actorPrefabInstance.transform.rotation = entityTransform.ValueRO.Rotation;
            }


        }
    }
}