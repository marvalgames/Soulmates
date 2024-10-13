using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    
[UpdateInGroup(typeof(RukhankaAnimationInjectionSystemGroup))]
[UpdateAfter(typeof(FABRIKSystem))]
public partial struct OverrideTransformIKSystem: ISystem
{
    [BurstCompile]
    partial struct OverrideTransformIKJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<RigDefinitionComponent> rigDefLookup;
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        [ReadOnly]
        public ComponentLookup<Parent> parentLookup;
        [ReadOnly]
        public ComponentLookup<AnimatorEntityRefComponent> animatorEntityRefLookup;
        
/////////////////////////////////////////////////////////////////////////////////

        [NativeDisableContainerSafetyRestriction]
        public RuntimeAnimationData runtimeData;
    
        void Execute(OverrideTransformIKComponent ikc, in AnimatorEntityRefComponent aer)
        {
            var rigDef = rigDefLookup[aer.animatorEntity];
            using var animStream = AnimationStream.Create(runtimeData, aer.animatorEntity, rigDef);

            var targetEntityRigRootRelativePose = IKCommon.GetRigRelativeEntityPose(ikc.target, aer.animatorEntity, animStream.GetWorldPose(0), runtimeData, localTransformLookup, parentLookup, animatorEntityRefLookup);
            var bonePose = animStream.GetWorldPose(aer.boneIndexInAnimationRig);

            targetEntityRigRootRelativePose.pos = math.lerp(bonePose.pos, targetEntityRigRootRelativePose.pos, ikc.positionWeight);
            targetEntityRigRootRelativePose.rot = math.slerp(bonePose.rot, targetEntityRigRootRelativePose.rot, ikc.rotationWeight);
            targetEntityRigRootRelativePose.scale = 1;
            
            animStream.SetWorldPose(aer.boneIndexInAnimationRig, targetEntityRigRootRelativePose);
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnCreate(ref SystemState ss)
    {
        var q = SystemAPI.QueryBuilder()
            .WithAll<OverrideTransformIKComponent, AnimatorEntityRefComponent>()
            .Build();
        
        ss.RequireForUpdate(q);
    }

/////////////////////////////////////////////////////////////////////////////////
    
    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
        var rigDefLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var animatorEntityRefLookup = SystemAPI.GetComponentLookup<AnimatorEntityRefComponent>(true);
        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;
        
        var ikJob = new OverrideTransformIKJob()
        {
            rigDefLookup = rigDefLookup,
            runtimeData = runtimeData,
            localTransformLookup = localTransformLookup,
            parentLookup = parentLookup,
            animatorEntityRefLookup = animatorEntityRefLookup
        };

        ikJob.ScheduleParallel();
    }
}
}
