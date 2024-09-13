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
                    Debug.Log("SPAWN");


                    Entity instance = commandBuffer.Instantiate(characterSpawnComponent.entityPrefab);
                    var positionList = characterPositionListBuffer[entity];
                    var position = positionList[i].localTransform.Position;
                    var rotation = positionList[i].localTransform.Rotation;
                    commandBuffer.SetComponent(instance,
                        LocalTransform.FromPositionRotation(position, rotation));

                    var go = GameObject.Instantiate(pgo.Prefab);
                    commandBuffer.AddComponent(instance, new TransformGO() { Transform = go.transform });
                }


                // GameObject go = GameObject.Instantiate(pgo.Prefab);
                //Debug.Log(go.name);
                //go.AddComponent<EntityGameObject>().AssignEntity(entity, state.World);
                //go.transform.position = transform.WorldPosition;
                //ecbBOS.AddComponent(entity, new TransformGO() { Transform = go.transform });
                //ecbBOS.AddComponent(entity, new AnimatorGO() { Animator = go.GetComponent<Animator>() });
                //ecbBOS.RemoveComponent<PresentationGO>(entity);
                pgo.instantiated = true;
            }
        }

        var ecbEOS = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (goTransform, entityTransform, entity) in SystemAPI.Query<TransformGO, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            Debug.Log("goTransform " + goTransform.Transform.position);
            goTransform.Transform.position = entityTransform.ValueRO.Position;
            goTransform.Transform.rotation = entityTransform.ValueRO.Rotation;
        }


        // foreach (var (goTransform,goAnimator, transform,speed) in SystemAPI.Query<TransformGO, AnimatorGO, TransformAspect, RefRO<Speed>>())
        // {
        //     goTransform.Transform.position =  transform.WorldPosition;
        //     goTransform.Transform.rotation =  transform.WorldRotation;
        //     goAnimator.Animator.SetFloat("speed", speed.ValueRO.value);
        // }
        // foreach(var (goTransform,entity) in SystemAPI.Query<TransformGO>().WithNone<LocalTransform>().WithEntityAccess())
        // {
        //     if (goTransform.Transform != null)
        //     {
        //         GameObject.Destroy(goTransform.Transform.gameObject);
        //     }
        //     ecbEOS.RemoveComponent<TransformGO>(entity);
        // }

        // We set this to true so it will no longer be processed on the next frame
    }
}