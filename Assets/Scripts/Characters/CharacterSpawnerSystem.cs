using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
public partial struct CharacterSpawnerSystem : ISystem
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

        foreach (var (pgo, characterSpawnComponent, entity)
                 in SystemAPI.Query<CharacterSpawnGameObject, CharacterSpawnComponent>().WithEntityAccess())
        {
            if (!pgo.instantiated)
            {
                // Already instantiated
                //return;
                var characterPositionListBuffer = SystemAPI.GetBufferLookup<CharacterPositionListBuffer>(true);
                var instances = characterPositionListBuffer[entity].Length;
                for (int i = 0; i < instances; i++)
                {
                    Entity instance = commandBuffer.Instantiate(characterSpawnComponent.entityPrefab);
                    var positionList = characterPositionListBuffer[entity];
                    var position = positionList[i].localTransform.Position;
                    var rotation = positionList[i].localTransform.Rotation;
                    commandBuffer.SetComponent(instance,
                        LocalTransform.FromPositionRotation(position, rotation));

                    var go = GameObject.Instantiate(pgo.Prefab);
                    commandBuffer.AddComponent(instance, new TransformGO() { Transform = go.transform });
                }

                pgo.instantiated = true;
            }
        }

        foreach (var (goTransform, entityTransform, entity) in SystemAPI.Query<TransformGO, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            goTransform.Transform.position = entityTransform.ValueRO.Position;
            goTransform.Transform.rotation = entityTransform.ValueRO.Rotation;
        }
    }
}