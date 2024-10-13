using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    
[UpdateInGroup(typeof(RukhankaAnimationInjectionSystemGroup))]
public partial struct FABRIKSystem: ISystem
{
    struct FABRIKData
    {
        public NativeArray<float3> chainWorldPositions;
        public NativeArray<float3> chainInitialDirections;
        public NativeArray<float> chainLengths;
        public NativeArray<int> chainBoneIndices;
    }

/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnCreate(ref SystemState ss)
    {
        var q = SystemAPI.QueryBuilder()
            .WithAll<FABRIKComponent, AnimatorEntityRefComponent>()
            .Build();
        
        ss.RequireForUpdate(q);
    }

/////////////////////////////////////////////////////////////////////////////////
    
    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
        var rigDefLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        var animatorEntityRefLookup = SystemAPI.GetComponentLookup<AnimatorEntityRefComponent>(true);
        
        ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;

        var ikJob = new FABRIKJob()
        {
            parentLookup = parentLookup,
            localTransformLookup = localTransformLookup,
            rigDefLookup = rigDefLookup,
            animatorEntityRefLookup = animatorEntityRefLookup,
            runtimeData = runtimeData
        };

        ikJob.ScheduleParallel();
    }
}
}
