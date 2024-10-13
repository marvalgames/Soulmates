using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class IKCommon
{
    public static void GetEntityWorldTransform
    (
        Entity e,
        ref BoneTransform t,
		in RuntimeAnimationData runtimeAnimationData,
        ComponentLookup<LocalTransform> ltl,
        ComponentLookup<Parent> pl,
        ComponentLookup<AnimatorEntityRefComponent> aerc
    )
    {
        if (!ltl.TryGetComponent(e, out var lt)) return;
        
        //  If current entity is a part of the rig (bone entity) use its animated pose
        if (aerc.TryGetComponent(e, out var aer))
        {
            var boneWorldPoses = RuntimeAnimationData.GetAnimationDataForRigRO(runtimeAnimationData.worldSpaceBonesBuffer, runtimeAnimationData.entityToDataOffsetMap, aer.animatorEntity);
            var bt = boneWorldPoses[aer.boneIndexInAnimationRig];
            t = BoneTransform.Multiply(bt, t);
            return;
        }

        t = BoneTransform.Multiply(new BoneTransform(lt), t);

        if (pl.TryGetComponent(e, out var p))
        {
            GetEntityWorldTransform(p.Value, ref t, runtimeAnimationData, ltl, pl, aerc);
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public static BoneTransform GetRigRelativeEntityPose
    (
        Entity target,
        Entity animatorEntity,
        BoneTransform rigRootWorldPose,
		in RuntimeAnimationData runtimeAnimationData,
        ComponentLookup<LocalTransform> ltl,
        ComponentLookup<Parent> pl,
        ComponentLookup<AnimatorEntityRefComponent> aerc
    )
    {
        var targetEntityWorldPose = BoneTransform.Identity();
        GetEntityWorldTransform(target, ref targetEntityWorldPose, runtimeAnimationData, ltl, pl, aerc);
        var animatedEntityWorldPose = BoneTransform.Inverse(rigRootWorldPose);
        GetEntityWorldTransform(animatorEntity, ref animatedEntityWorldPose, runtimeAnimationData, ltl, pl, aerc);
        var rv = BoneTransform.Multiply(BoneTransform.Inverse(animatedEntityWorldPose), targetEntityWorldPose);
        return rv;
    }
}
}
