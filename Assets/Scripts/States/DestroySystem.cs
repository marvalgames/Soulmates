using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct DestroyComponent : IComponentData
{
    public int frames;
}

[UpdateInGroup((typeof(PresentationSystemGroup)))]
[RequireMatchingQueriesForUpdate]
public partial struct DestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var useItem1Group = SystemAPI.GetComponentLookup<UseItem1>();
        var useItem2Group = SystemAPI.GetComponentLookup<UseItem2>();
        var job = new DestroySystemJob()
        {
            ecb = ecb,
            useItemGroup1 = useItem1Group,
            useItemGroup2 = useItem2Group
        };

        job.Schedule();
    }

    [BurstCompile]
    partial struct DestroySystemJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        [ReadOnly] public ComponentLookup<UseItem1> useItemGroup1;
        [ReadOnly] public ComponentLookup<UseItem2> useItemGroup2;

        void Execute(ref DestroyComponent destroyComponent, in Entity e)
        {
            if (useItemGroup1.HasComponent(e))
            {
                ecb.RemoveComponent<UseItem1>(e);
            }
            else if (useItemGroup2.HasComponent(e))
            {
                ecb.RemoveComponent<UseItem2>(e);
            }

            ecb.DestroyEntity(e);
        }
    }
}