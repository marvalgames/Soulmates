using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static partial class ScriptedAnimator
{
    public static void ResetAnimationState(ref DynamicBuffer<AnimationToProcessComponent> atps)
    {
        atps.Clear();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void PlayAnimation
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        BlobAssetReference<AnimationClipBlob> clip,
        float normalizedTime,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        var atp = new AnimationToProcessComponent()
        {
            animation = clip,
            time = normalizedTime,
            weight = weight,
            avatarMask = avatarMask,
            blendMode = AnimationBlendingMode.Override,
            layerIndex = 0,
            layerWeight = 1,
            motionId = (uint)atps.Length
        };
        atps.Add(atp);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void BlendTwoAnimations
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        BlobAssetReference<AnimationClipBlob> clip0,
        BlobAssetReference<AnimationClipBlob> clip1,
        float normalizedTime,
        float blendFactor,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        var atp = new AnimationToProcessComponent()
        {
            animation = clip0,
            time = normalizedTime,
            weight = (1 - blendFactor) * weight,
            avatarMask = avatarMask,
            blendMode = AnimationBlendingMode.Override,
            layerIndex = 0,
            layerWeight = 1,
            motionId = (uint)atps.Length
        };
        atps.Add(atp);
        
        atp.animation = clip1;
        atp.weight = blendFactor * weight;
        atp.motionId = (uint)atps.Length;
        atps.Add(atp);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static unsafe void PlayBlendTree1D
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        in NativeArray<BlobAssetReference<AnimationClipBlob>> blendTreeClips,
        in NativeArray<float> blendTreeThresholds,
        float blendTreeParameterValue,
        float normalizedTime,
        float blendTreeWeight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        BurstAssert.IsTrue(blendTreeClips.Length == blendTreeThresholds.Length, "Blend tree clips count must match thresholds array length");
        var bttSpan = new ReadOnlySpan<float>(blendTreeThresholds.GetUnsafeReadOnlyPtr(), blendTreeThresholds.Length);
        
        var motions = ComputeBlendTree1D(bttSpan, blendTreeParameterValue);
        
        for (var i = 0; i < motions.Length; ++i)
        {
            var m = motions[i];
            var atp = new AnimationToProcessComponent()
            {
                animation = blendTreeClips[m.motionIndex],
                time = normalizedTime,
                weight = m.weight * blendTreeWeight,
                avatarMask = avatarMask,
                blendMode = AnimationBlendingMode.Override,
                layerIndex = 0,
                layerWeight = 1,
                motionId = (uint)atps.Length
            };
            
            atps.Add(atp);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static unsafe void PlayBlendTree2D
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        in NativeArray<BlobAssetReference<AnimationClipBlob>> blendTreeClips,
        in NativeArray<float2> blendTreePositions,
        float2 blendTreeParameterValue,
        float normalizedTime,
        MotionBlob.Type blendTreeType,
        float blendTreeWeight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        BurstAssert.IsTrue(blendTreeClips.Length == blendTreePositions.Length, "Blend tree clips and positions array lengths must match.");
        
        var bttSpan = new ReadOnlySpan<float2>(blendTreePositions.GetUnsafeReadOnlyPtr(), blendTreePositions.Length);
        
        var motions = blendTreeType switch
        {
	       MotionBlob.Type.BlendTree2DSimpleDirectional   => ComputeBlendTree2DSimpleDirectional(bttSpan, blendTreeParameterValue),
	       MotionBlob.Type.BlendTree2DFreeformCartesian   => ComputeBlendTree2DFreeformCartesian(bttSpan, blendTreeParameterValue),
	       MotionBlob.Type.BlendTree2DFreeformDirectional => ComputeBlendTree2DFreeformDirectional(bttSpan, blendTreeParameterValue),
	       _ => default
        };
        
        for (var i = 0; i < motions.Length; ++i)
        {
            var m = motions[i];
            var atp = new AnimationToProcessComponent()
            {
                animation = blendTreeClips[m.motionIndex],
                time = normalizedTime,
                weight = m.weight * blendTreeWeight,
                avatarMask = avatarMask,
                blendMode = AnimationBlendingMode.Override,
                layerIndex = 0,
                layerWeight = 1,
                motionId = (uint)atps.Length
            };
            
            atps.Add(atp);
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static int GetStateIndexInControllerLayer(BlobAssetReference<ControllerBlob> cb, int layerIndex, uint stateHash)
    {
        ref var layerBlob = ref cb.Value.layers[layerIndex];
        for (var i = 0; i < layerBlob.states.Length; ++i)
        {
            ref var stateBlob = ref layerBlob.states[i];
            if (stateBlob.hash == stateHash)
                return i;
        }
        return -1;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void PlayAnimatorState
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        in NativeArray<AnimatorControllerParameterComponent> animatorControllerParameters,
        in BlobAssetReference<ControllerBlob> controllerBlob,
        in BlobAssetReference<ControllerAnimationsBlob> animationsBlob,
        in BlobDatabaseSingleton blobDatabase,
        int layerIndex,
        int stateIndex,
        float normalizedTime,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        BurstAssert.IsTrue(controllerBlob.IsCreated, "Controller blob is not valid");
        BurstAssert.IsTrue(animationsBlob.IsCreated, "Controller animations blob is not valid");
        
        BurstAssert.IsTrue(controllerBlob.Value.layers.Length > layerIndex, "Layer index is out of range of controller layers array");
        if (controllerBlob.Value.layers.Length <= layerIndex || layerIndex < 0)
            return;
        
        ref var lb = ref controllerBlob.Value.layers[layerIndex];
        
        BurstAssert.IsTrue(lb.states.Length > stateIndex, "State index is out of range of controller layer states array");
        if (lb.states.Length <= stateIndex || stateIndex < 0)
            return;
        
        ref var sb = ref lb.states[stateIndex];
        
        PlayMotion
        (
            ref atps,
            ref sb.motion,
            animatorControllerParameters,
            animationsBlob,
            blobDatabase,
            normalizedTime,
            weight,
            avatarMask
        );
    }
}
}
