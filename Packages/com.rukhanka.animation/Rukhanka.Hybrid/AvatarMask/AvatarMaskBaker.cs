#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using FixedStringName = Unity.Collections.FixedString512Bytes;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{ 
public class AvatarMaskBaker
{
	public BlobAssetReference<AvatarMaskBlob> CreateAvatarMaskBlob(IBaker baker, AvatarMask am)
	{
		if (am == null)
			return default;
		
		var blobHash = BakingUtils.ComputeAvatarMaskHash(am);
		var blobExists = baker.TryGetBlobAssetReference<AvatarMaskBlob>(blobHash, out var avatarMaskBlob);
		if (blobExists)
			return avatarMaskBlob;
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var amb = ref bb.ConstructRoot<AvatarMaskBlob>();
		amb.hash = blobHash;	
	#if RUKHANKA_DEBUG_INFO
		if (am.name.Length > 0)
			bb.AllocateString(ref amb.name, am.name);
		var startTimeMarker = Time.realtimeSinceStartup;
	#endif
		
		//	Generic avatar mask
		var avatarMaskIncludedBones = new List<string>();
		for (int i = 0; i < am.transformCount; ++i)
		{
			var bonePath = am.GetTransformPath(i);
			var boneActive = am.GetTransformActive(i);
			if (bonePath.Length == 0 || !boneActive) continue;
			var boneNames = bonePath.Split('/');
			var leafBoneName = boneNames[^1];
			avatarMaskIncludedBones.Add(leafBoneName);
		}
		
		var includedBoneHashes = bb.Allocate(ref amb.includedBoneHashes, avatarMaskIncludedBones.Count);
	#if RUKHANKA_DEBUG_INFO
		var includedBonePaths = bb.Allocate(ref amb.includedBoneNames, avatarMaskIncludedBones.Count);
	#endif
		
		for (var i = 0; i < includedBoneHashes.Length; ++i)
		{
			var leafBoneName = avatarMaskIncludedBones[i];
		#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref includedBonePaths[i], leafBoneName);
		#endif
			includedBoneHashes[i] = new FixedStringName(leafBoneName).CalculateHash128();
		}

		//	Humanoid avatar mask
		var humanBodyPartsCount = (int)AvatarMaskBodyPart.LastBodyPart;
		amb.humanBodyPartsAvatarMask = 0;
		for (int i = 0; i < humanBodyPartsCount; ++i)
		{
			var ambp = (AvatarMaskBodyPart)i;
			if (am.GetHumanoidBodyPartActive(ambp))
				amb.humanBodyPartsAvatarMask |= 1u << i;
		}

	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		amb.bakingTime = (float)dt;
	#endif
		
		var rv = bb.CreateBlobAssetReference<AvatarMaskBlob>(Allocator.Persistent);
		baker.AddBlobAssetWithCustomHash(ref rv, blobHash);
		
		return rv;
	}
}
}

#endif