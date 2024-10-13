#if UNITY_EDITOR

using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public static class BakingUtils
{
    public static Hash128 ComputeAnimationHash(uint2 animationAssetID, uint2 avatarAssetID)
    {
        var rv = new Hash128(avatarAssetID.x, avatarAssetID.y, animationAssetID.x, animationAssetID.y);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static uint2 GetAssetID(Object obj)
    {
        if (obj == null)
            return 0;
        
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guidString, out long fileID))
            return 0;
        
        var guid = new GUID(guidString);
        
        var hashBuilder = new xxHash3.StreamingState(true, 121212);
        hashBuilder.Update(guid);
        hashBuilder.Update(fileID);
        var rv = hashBuilder.DigestHash64();
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static Hash128 ComputeAnimationHash(AnimationClip animation, Avatar avatar)
    {
        var animationAssetID = GetAssetID(animation);
        var avatarAssetID = GetAssetID(avatar);
        var rv = ComputeAnimationHash(animationAssetID, avatarAssetID);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static Hash128 ComputeControllerHash(AnimatorController controller)
    {
        var assetID = GetAssetID(controller);
        var rv = new Hash128(assetID.x, assetID.y, 0, 0);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static Hash128 ComputeAvatarMaskHash(AvatarMask avatarMask)
    {
		var assetID = GetAssetID(avatarMask);
        var rv = new Hash128(assetID.x, assetID.y, 0, 0);
        return rv;
    }
}
}

#endif
