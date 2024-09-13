using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    
public partial struct FABRIKSystem
{
    
[BurstCompile]
partial struct FABRIKJob : IJobEntity
{
    [ReadOnly]
    public ComponentLookup<RigDefinitionComponent> rigDefLookup;
    [ReadOnly]
    public ComponentLookup<LocalTransform> localTransformLookup;
    [ReadOnly]
    public ComponentLookup<Parent> parentLookup;
    [ReadOnly]
    public ComponentLookup<AnimatorEntityRefComponent> animatorEntityRefLookup;
    
    [NativeDisableContainerSafetyRestriction]
    public RuntimeAnimationData runtimeData;
    
/////////////////////////////////////////////////////////////////////////////////

    void Execute(FABRIKComponent ikc, in AnimatorEntityRefComponent aer)
    {
        if (ikc.weight <= math.EPSILON)
            return;
        
        var rigDef = rigDefLookup[aer.animatorEntity];
        using var animStream = AnimationStream.Create(runtimeData, aer.animatorEntity, rigDef);

        var targetEntityRigRootRelativePose = IKCommon.GetRigRelativeEntityPose(ikc.target, aer.animatorEntity, animStream.GetWorldPose(0), localTransformLookup, parentLookup);
        var targetEntityRigRelativePosition = targetEntityRigRootRelativePose.pos;

        var ikData = PrepareIKData(ikc, rigDef, aer);

        IKChainToWorld(ikData, animStream);
        if (Solve(ikData, ikc.numIterations, ikc.threshold, targetEntityRigRelativePosition))
            IKDataToBonePoses(ikData, animStream, ikc.weight);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    FABRIKData PrepareIKData(FABRIKComponent ikComponent, RigDefinitionComponent rb, AnimatorEntityRefComponent rootBoneAER)
    {
        var rv = new FABRIKData();

        var chainEndBoneIndex = animatorEntityRefLookup[ikComponent.tip].boneIndexInAnimationRig;
        var chainRootBoneIndex = rootBoneAER.boneIndexInAnimationRig;
        var curBoneIndex = chainEndBoneIndex;

        var chainBoneIndices = new NativeList<int>(Allocator.Temp);
        while (chainRootBoneIndex != curBoneIndex && curBoneIndex >= 0)
        {
            chainBoneIndices.Add(curBoneIndex);
            curBoneIndex = rb.rigBlob.Value.bones[curBoneIndex].parentBoneIndex;
        }
        chainBoneIndices.Add(chainRootBoneIndex);
        var numBonesInChain = chainBoneIndices.Length;

        rv.chainLengths = new NativeArray<float>(numBonesInChain, Allocator.Temp);
        rv.chainBoneIndices = chainBoneIndices.AsArray();
        rv.chainWorldPositions = new NativeArray<float3>(numBonesInChain, Allocator.Temp);
        rv.chainInitialDirections = new NativeArray<float3>(numBonesInChain, Allocator.Temp);

        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    void IKChainToWorld(FABRIKData ikData, AnimationStream animStream)
    {
        ikData.chainLengths[^1] = 0;

        //  Fill world positions
        for (var i = 0; i < ikData.chainBoneIndices.Length; ++i)
        {
            var boneIndex = ikData.chainBoneIndices[i];
            var boneWorldPos = animStream.GetWorldPosition(boneIndex);
            ikData.chainWorldPositions[i] = boneWorldPos;
        }
        
        //  Fill chain lengths and directions
        for (var i = 0; i < ikData.chainBoneIndices.Length - 1; ++i)
        {
            var curBonePos = ikData.chainWorldPositions[i];
            var nextBonePos = ikData.chainWorldPositions[i + 1];
            var lv = nextBonePos - curBonePos;
            var l = math.length(lv);
            ikData.chainInitialDirections[i] = lv / l;
            ikData.chainLengths[i] = l;
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void IKDataToBonePoses(FABRIKData ikData, AnimationStream animationStream, float weight)
    {
        var initialIndex = ikData.chainWorldPositions.Length - 1;
        //var initialIndex = 0;
        var prevBoneRot = quaternion.identity;
        for (var i = initialIndex; i > 0; --i)
        {
            var pos = ikData.chainWorldPositions[i];
            var boneIndex = ikData.chainBoneIndices[i];

            var bonePoseOrig = animationStream.GetWorldPose(boneIndex);
            var bonePose = bonePoseOrig;
            var boneDir = ikData.chainInitialDirections[i - 1];
            var nextBonePos = ikData.chainWorldPositions[i - 1];
            var boneVec = math.normalize(pos - nextBonePos);
            var rot = MathUtils.FromToRotationForNormalizedVectors(boneDir, boneVec);
            bonePose.rot = math.mul(math.inverse(prevBoneRot), bonePose.rot);
            bonePose.rot = math.mul(rot, bonePose.rot);
            prevBoneRot = rot;
            bonePose.rot = math.slerp(bonePoseOrig.rot, bonePose.rot, weight);
            var resultTransform = BoneTransform.Lerp(bonePoseOrig, bonePose, weight);
            animationStream.SetWorldPose(boneIndex, resultTransform);
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void ForwardPass(FABRIKData ikData, float3 origin)
    {
        ikData.chainWorldPositions[^1] = origin;

        for (var i = ikData.chainWorldPositions.Length - 2; i >= 0; --i)
        {
            var p0 = ikData.chainWorldPositions[i];
            var p1 = ikData.chainWorldPositions[i + 1];
            var dir = math.normalize(p0 - p1);
            var offset = dir * ikData.chainLengths[i];
            ikData.chainWorldPositions[i] = ikData.chainWorldPositions[i + 1] + offset;
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void BackwardPass(FABRIKData ikData, float3 goalInWorldSpace)
    {
        ikData.chainWorldPositions[0] = goalInWorldSpace;

        for (var i = 1; i < ikData.chainWorldPositions.Length; ++i)
        {
            var p0 = ikData.chainWorldPositions[i - 1];
            var p1 = ikData.chainWorldPositions[i];
            var dir = math.normalize(p1 - p0);
            var offset = dir * ikData.chainLengths[i - 1];
            ikData.chainWorldPositions[i] = ikData.chainWorldPositions[i - 1] + offset;
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void ValidateIKData(FABRIKData ikData)
    {
        for (var i = 0; i < ikData.chainWorldPositions.Length; ++i)
        {
            if (math.any(math.isnan(ikData.chainWorldPositions[i])))
                Debug.Log("NAN!");
            
            if (math.any(math.isnan(ikData.chainInitialDirections[i])))
                Debug.Log("NAN!");
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    bool Solve(FABRIKData ikData, int numIterations, float threshold, float3 goalPos)
    {
        var chainOrigin = ikData.chainWorldPositions[^1];
        for (var i = 0; i < numIterations; ++i)
        {
            var tipPos = ikData.chainWorldPositions[0];
            if (math.lengthsq(goalPos - tipPos) <= threshold)
                return i > math.EPSILON;
            
            BackwardPass(ikData, goalPos);
            ForwardPass(ikData, chainOrigin);
        }

        return true;
    }
}
}
}
