using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Assertions;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct AnimationStream: IDisposable
{
    public RuntimeAnimationData.AnimatedEntityBoneDataProps rigBoneProps;
    public RuntimeAnimationData runtimeData;
    public BlobAssetReference<RigDefinitionBlob> rigBlob;
    public NativeBitArray worldPoseDirtyFlags;
    
/////////////////////////////////////////////////////////////////////////////////

    public static AnimationStream Create(RuntimeAnimationData rd, Entity rigEntity, in RigDefinitionComponent rdc)
    {
        var offsets = RuntimeAnimationData.CalculateBufferOffset(rd.entityToDataOffsetMap, rigEntity);
        var rv = new AnimationStream()
        {
            rigBoneProps = offsets,
            runtimeData = rd,
            rigBlob = rdc.rigBlob,
            worldPoseDirtyFlags = new NativeBitArray(offsets.rigBoneCount, Allocator.Temp)
        };
        
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public void Dispose()
    {
        RebuildOutdatedBonePoses(-1);
    }

/////////////////////////////////////////////////////////////////////////////////

    public BoneTransform GetLocalPose(int boneIndex)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return BoneTransform.Identity();
        
        return runtimeData.animatedBonesBuffer[rigBoneProps.bonePoseOffset + boneIndex];
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public float3 GetLocalPosition(int boneIndex) => GetLocalPose(boneIndex).pos;
    public quaternion GetLocalRotation(int boneIndex) => GetLocalPose(boneIndex).rot;

/////////////////////////////////////////////////////////////////////////////////

    public BoneTransform GetWorldPose(int boneIndex)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return BoneTransform.Identity();
        
        var isWorldPoseDirty = worldPoseDirtyFlags.IsSet(boneIndex);
        if (isWorldPoseDirty)
            RebuildOutdatedBonePoses(boneIndex);
        
        return runtimeData.worldSpaceBonesBuffer[rigBoneProps.bonePoseOffset + boneIndex];   
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public float3 GetWorldPosition(int boneIndex) => GetWorldPose(boneIndex).pos;
    public quaternion GetWorldRotation(int boneIndex) => GetWorldPose(boneIndex).rot;
    
/////////////////////////////////////////////////////////////////////////////////

    BoneTransform GetParentBoneWorldPose(int boneIndex)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return BoneTransform.Identity();
        
        var parentBoneIndex = rigBlob.Value.bones[boneIndex].parentBoneIndex;
        var parentWorldPose = BoneTransform.Identity();
        if (parentBoneIndex >= 0)
        {
            if (worldPoseDirtyFlags.IsSet(parentBoneIndex))
                RebuildOutdatedBonePoses(parentBoneIndex);
            parentWorldPose = runtimeData.worldSpaceBonesBuffer[parentBoneIndex + rigBoneProps.bonePoseOffset];
        }

        return parentWorldPose;
    }

/////////////////////////////////////////////////////////////////////////////////

    public void SetWorldPose(int boneIndex, in BoneTransform bt)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return;
        
        var absBoneIndex = rigBoneProps.bonePoseOffset + boneIndex;
        runtimeData.worldSpaceBonesBuffer[absBoneIndex] = bt;
        
        var parentWorldPose = GetParentBoneWorldPose(boneIndex);
        
        ref var boneLocalPose = ref runtimeData.animatedBonesBuffer.ElementAt(absBoneIndex);
        boneLocalPose = BoneTransform.Multiply(BoneTransform.Inverse(parentWorldPose), bt);
        
        MarkChildrenWorldPosesAsDirty(boneIndex);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public void SetWorldPosition(int boneIndex, float3 pos)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return;
        
        var absBoneIndex = rigBoneProps.bonePoseOffset + boneIndex;
        ref var curPose = ref runtimeData.worldSpaceBonesBuffer.ElementAt(absBoneIndex);
        curPose.pos = pos;
        
        var parentWorldPosition = GetParentBoneWorldPose(boneIndex).pos;
        
        ref var boneLocalPose = ref runtimeData.animatedBonesBuffer.ElementAt(absBoneIndex);
        boneLocalPose.pos = pos - parentWorldPosition;
        
        MarkChildrenWorldPosesAsDirty(boneIndex);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public void SetWorldRotation(int boneIndex, quaternion rot)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return;
        
        var absBoneIndex = rigBoneProps.bonePoseOffset + boneIndex;
        ref var boneWorldPose = ref runtimeData.worldSpaceBonesBuffer.ElementAt(absBoneIndex);
        boneWorldPose.rot = rot;
        
        var parentWorldRot = GetParentBoneWorldPose(boneIndex).rot;

        ref var boneLocalPose = ref runtimeData.animatedBonesBuffer.ElementAt(absBoneIndex);
        boneLocalPose.rot = math.mul(math.conjugate(parentWorldRot), boneWorldPose.rot);
        
        MarkChildrenWorldPosesAsDirty(boneIndex);
    }

/////////////////////////////////////////////////////////////////////////////////

    public void SetLocalPose(int boneIndex, in BoneTransform bt)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return;
        
        var absBoneIndex = rigBoneProps.bonePoseOffset + boneIndex;
        runtimeData.animatedBonesBuffer[absBoneIndex] = bt;
        MarkChildrenWorldPosesAsDirty(boneIndex);
        worldPoseDirtyFlags.Set(boneIndex, true);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public void SetLocalPosition(int boneIndex, float3 pos)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return;
        
        var absBoneIndex = rigBoneProps.bonePoseOffset + boneIndex;
        ref var curPose = ref runtimeData.animatedBonesBuffer.ElementAt(absBoneIndex);
        curPose.pos = pos;
        MarkChildrenWorldPosesAsDirty(boneIndex);
        worldPoseDirtyFlags.Set(boneIndex, true);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public void SetLocalRotation(int boneIndex, quaternion rot)
    {
        if (boneIndex >= rigBoneProps.rigBoneCount)
            return;
        
        var absBoneIndex = rigBoneProps.bonePoseOffset + boneIndex;
        ref var curPose = ref runtimeData.animatedBonesBuffer.ElementAt(absBoneIndex);
        curPose.rot = rot;
        MarkChildrenWorldPosesAsDirty(boneIndex);
        worldPoseDirtyFlags.Set(boneIndex, true);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void MarkChildrenWorldPosesAsDirty(int rootBoneIndex)
    {
        for (var i = rootBoneIndex + 1; i < rigBoneProps.rigBoneCount; ++i)
        {
            ref var bone = ref rigBlob.Value.bones[i];
            if (bone.parentBoneIndex == rootBoneIndex)
            {
                worldPoseDirtyFlags.Set(i, true);
                MarkChildrenWorldPosesAsDirty(i);
            }
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void RebuildOutdatedBonePoses(int interestedBoneIndex)
    {
        if (rigBoneProps.rigBoneCount < 0)
            return;
        
        var endBoneIndex = math.select(rigBoneProps.rigBoneCount - 1, interestedBoneIndex, interestedBoneIndex >= 0);
        endBoneIndex = math.min(endBoneIndex, rigBoneProps.rigBoneCount);
        for (var i = 0; i <= endBoneIndex; ++i)
        {
            var isWorldPoseDirty = worldPoseDirtyFlags.IsSet(i);
            if (!isWorldPoseDirty)
                continue;
            
            var absBoneIndex = rigBoneProps.bonePoseOffset + i;
            ref var rigBone = ref rigBlob.Value.bones[i];
            var boneLocalPose = runtimeData.animatedBonesBuffer[absBoneIndex];

            var parentBoneWorldPose = BoneTransform.Identity();
            if (rigBone.parentBoneIndex >= 0)
            {
                parentBoneWorldPose = runtimeData.worldSpaceBonesBuffer[rigBoneProps.bonePoseOffset + rigBone.parentBoneIndex];
            }

            var worldPose = BoneTransform.Multiply(parentBoneWorldPose, boneLocalPose);
            runtimeData.worldSpaceBonesBuffer[absBoneIndex] = worldPose;
        }
        worldPoseDirtyFlags.SetBits(0, false, endBoneIndex + 1);
    }

/////////////////////////////////////////////////////////////////////////////////

    public AnimationTransformFlags GetAnimationTransformFlagsRO()
    {
        return AnimationTransformFlags.CreateFromBufferRO(runtimeData.boneTransformFlagsHolderArr, rigBoneProps.boneFlagsOffset, rigBoneProps.rigBoneCount);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public AnimationTransformFlags GetAnimationTransformFlagsRW()
    {
        return AnimationTransformFlags.CreateFromBufferRW(runtimeData.boneTransformFlagsHolderArr, rigBoneProps.boneFlagsOffset, rigBoneProps.rigBoneCount);
    }
}
}
