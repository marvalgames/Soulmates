using Sandbox.Player;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(TransformSystemGroup))]
[BurstCompile]
public partial struct LockAxisSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // The fixed Y value you want to lock entities to

        
        //foreach (var (prefab, entity) in
           //      SystemAPI.Query<PlayerMoveGameObjectClass>().WithEntityAccess())
            
        // Process all entities with a Position component
        foreach (var (lockComponent, transform, entity) 
                 in SystemAPI.Query<RefRO<CharacterLockAxisComponent>, RefRW<LocalTransform>>().WithAny<EnemyComponent>().WithEntityAccess())
        {
            float3 position = transform.ValueRO.Position;

            // Lock the Y-axis
            position.y = lockComponent.ValueRO.FixedY;
            //Debug.Log("FIXED  " + fixedY);

            // Update the entity's position
            transform.ValueRW.Position = position;
        }
    }
}


