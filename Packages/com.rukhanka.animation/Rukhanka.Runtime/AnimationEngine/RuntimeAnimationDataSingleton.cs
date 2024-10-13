
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct RuntimeAnimationData: IComponentData
{
	public struct AnimatedEntityBoneDataProps
	{
		public int bonePoseOffset;
		public int boneFlagsOffset;
		public int rigBoneCount;
		public int genericAnimationDataOffset;
		public int genericAnimationDataSize;
		
		public static AnimatedEntityBoneDataProps MakeInvalid()
		{
			return new AnimatedEntityBoneDataProps()
			{
				boneFlagsOffset = -1,
				bonePoseOffset = -1,
				rigBoneCount = -1,
				genericAnimationDataOffset = -1,
				genericAnimationDataSize = -1
			};
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////

	public struct GenericFloatAnimatedValue
	{
		public Hash128 hash;
		public float value;
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
    internal NativeList<BoneTransform> animatedBonesBuffer;
    internal NativeList<BoneTransform> worldSpaceBonesBuffer;
    internal NativeList<GenericFloatAnimatedValue> genericCurveAnimatedValuesBuffer;
    internal NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap;
    internal NativeList<int3> boneToEntityArr;
	internal NativeList<ulong> boneTransformFlagsHolderArr;

/////////////////////////////////////////////////////////////////////////////////

	public static RuntimeAnimationData MakeDefault()
	{
		var rv = new RuntimeAnimationData()
		{
			animatedBonesBuffer = new (Allocator.Persistent),
			worldSpaceBonesBuffer = new (Allocator.Persistent),
			entityToDataOffsetMap = new (128, Allocator.Persistent),
			boneToEntityArr = new (Allocator.Persistent),
			boneTransformFlagsHolderArr = new (Allocator.Persistent),
			genericCurveAnimatedValuesBuffer = new (Allocator.Persistent)
		};
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

	public void Dispose()
	{
		animatedBonesBuffer.Dispose();
		worldSpaceBonesBuffer.Dispose();
		entityToDataOffsetMap.Dispose();
		boneToEntityArr.Dispose();
		boneTransformFlagsHolderArr.Dispose();
		genericCurveAnimatedValuesBuffer.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AnimatedEntityBoneDataProps CalculateBufferOffset(in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap, Entity animatedRigEntity)
	{
		if (!entityToDataOffsetMap.TryGetValue(animatedRigEntity, out var offset))
			return AnimatedEntityBoneDataProps.MakeInvalid();

		return offset;
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<BoneTransform> GetAnimationDataForRigRO(in NativeList<BoneTransform> animatedBonesBuffer, int offset, int length)
	{
		var rv = animatedBonesBuffer.GetReadOnlySpan(offset, length);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<BoneTransform> GetAnimationDataForRigRW(in NativeList<BoneTransform> animatedBonesBuffer, int offset, int length)
	{
		var rv = animatedBonesBuffer.GetSpan(offset, length);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<BoneTransform> GetAnimationDataForRigRO
	(
		in NativeList<BoneTransform> animatedBonesBuffer,
		in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap,
		Entity animatedRigEntity
	)
	{
		var dp = CalculateBufferOffset(entityToDataOffsetMap, animatedRigEntity);
		if (dp.bonePoseOffset < 0)
			return default;
			
		return GetAnimationDataForRigRO(animatedBonesBuffer, dp.bonePoseOffset, dp.rigBoneCount);
	}

///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<BoneTransform> GetAnimationDataForRigRW
	(
		in NativeList<BoneTransform> animatedBonesBuffer,
		in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap,
		Entity animatedRigEntity
	)
	{
		var dp = CalculateBufferOffset(entityToDataOffsetMap, animatedRigEntity);
		if (dp.bonePoseOffset < 0)
			return default;
			
		return GetAnimationDataForRigRW(animatedBonesBuffer, dp.bonePoseOffset, dp.rigBoneCount);
	}

///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AnimationTransformFlags GetAnimationTransformFlagsRO(in NativeList<int3> boneToEntityArr, in NativeList<ulong> boneTransformFlagsArr, int globalBoneIndex, int boneCount)
	{
		var boneInfo = boneToEntityArr[globalBoneIndex];
		var rv = AnimationTransformFlags.CreateFromBufferRO(boneTransformFlagsArr, boneInfo.z, boneCount);
		return rv;
	}

///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AnimationTransformFlags GetAnimationTransformFlagsRW(in NativeList<int3> boneToEntityArr, in NativeList<ulong> boneTransformFlagsArr, int globalBoneIndex, int boneCount)
	{
		var boneInfo = boneToEntityArr[globalBoneIndex];
		var rv = AnimationTransformFlags.CreateFromBufferRW(boneTransformFlagsArr, boneInfo.z, boneCount);
		return rv;
	}
	
///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe int FindGenericAnimatedDataIndexByHash
	(
		Entity animatedEntity,
		in NativeParallelHashMap<Entity, AnimatedEntityBoneDataProps> entityToDataOffsetMap,
		in NativeList<GenericFloatAnimatedValue> genericAnimData,
		in Hash128 hash
	)
	{
		if (!entityToDataOffsetMap.TryGetValue(animatedEntity, out var entityOffsets))
			return -1;
		
		var spanStart = genericAnimData.GetUnsafeReadOnlyPtr() + entityOffsets.genericAnimationDataOffset;
		var entityDataSpan = new ReadOnlySpan<GenericFloatAnimatedValue>(spanStart, entityOffsets.genericAnimationDataSize);
		var h32 = Toolbox.HashUtils.Hash128To32(hash);
		for (var i = 0; i < entityDataSpan.Length; ++i)
		{
			var index = (int)((h32 + i) % entityOffsets.genericAnimationDataSize);
			var hv = entityDataSpan[index].hash;
			if (hash == hv)
				return index + entityOffsets.genericAnimationDataOffset;
		}
		return -1;
	}
}
}
