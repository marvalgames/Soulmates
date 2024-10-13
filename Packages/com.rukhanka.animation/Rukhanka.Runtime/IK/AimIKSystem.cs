using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    
[UpdateInGroup(typeof(RukhankaAnimationInjectionSystemGroup))]
public partial struct AimIKSystem: ISystem
{
    [BurstCompile]
    partial struct AimIKJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<RigDefinitionComponent> rigDefLookup;
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        [ReadOnly]
        public ComponentLookup<Parent> parentLookup;
        [ReadOnly]
        public ComponentLookup<AnimatorEntityRefComponent> boneEntityRefLookup;
        
        [NativeDisableContainerSafetyRestriction]
        public RuntimeAnimationData runtimeData;
    
        void Execute(AimIKComponent aik, in AnimatorEntityRefComponent aer, in DynamicBuffer<AimIKAffectedBoneComponent> aimedBones)
        {
            if (aik.weight < math.EPSILON)
                return;
            
            var rigDef = rigDefLookup[aer.animatorEntity];
            using var animStream = AnimationStream.Create(runtimeData, aer.animatorEntity, rigDef);
            
            var targetEntityRigRelativePose = IKCommon.GetRigRelativeEntityPose(aik.target, aer.animatorEntity, animStream.GetWorldPose(0), runtimeData, localTransformLookup, parentLookup, boneEntityRefLookup);

            for (var i = 0; i < aimedBones.Length; ++i)
            {
                var aimedBone = aimedBones[i];
                if (!boneEntityRefLookup.TryGetComponent(aimedBone.boneEntity, out var aimedBoneEntity))
                {
                #if RUKHANKA_DEBUG_INFO
                    Debug.LogWarning($"Aimed entity '{aimedBone.boneEntity}' does not have AnimatorEntityRefComponent.");
                #endif
                    continue;
                }
                
                var ikBoneWorldPose = animStream.GetWorldPose(aimedBoneEntity.boneIndexInAnimationRig);
                var toTargetDir = math.normalize(targetEntityRigRelativePose.pos - ikBoneWorldPose.pos);
                var originalForward = math.rotate(ikBoneWorldPose.rot, aik.forwardVector);
                var acos = math.dot(toTargetDir, originalForward);
                if (math.abs(acos) < 0.99999f)
                {
                    var crossDir = math.normalize(math.cross(originalForward, toTargetDir));
                    var angle = math.acos(acos);
                    angle = math.clamp(angle, aik.angleLimits.x, aik.angleLimits.y);
                    var correctedRot = quaternion.AxisAngle(crossDir, angle);
                    var compositeWeight = aik.weight * aimedBone.weight;
                    correctedRot = math.slerp(quaternion.identity, correctedRot, compositeWeight);
                    
                    ikBoneWorldPose.rot = math.mul(correctedRot, ikBoneWorldPose.rot);
                    animStream.SetWorldPose(aimedBoneEntity.boneIndexInAnimationRig, ikBoneWorldPose);
                }
            }
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnCreate(ref SystemState ss)
    {
        var q = SystemAPI.QueryBuilder()
            .WithAll<AimIKComponent, AnimatorEntityRefComponent, AimIKAffectedBoneComponent>()
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
        var aerLookup = SystemAPI.GetComponentLookup<AnimatorEntityRefComponent>(true);
        ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;
        
        var ikJob = new AimIKJob()
        {
            rigDefLookup = rigDefLookup,
            runtimeData = runtimeData,
            localTransformLookup = localTransformLookup,
            parentLookup = parentLookup,
            boneEntityRefLookup = aerLookup
        };

        ikJob.ScheduleParallel();
    }
}
}
