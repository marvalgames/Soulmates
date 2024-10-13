using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Unity.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
public struct SkinnedMeshRendererRootBoneEntity: IComponentData
{
	public Entity value;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public partial class SkinnedMeshBaker: Baker<SkinnedMeshRenderer>
{
	public override void Bake(SkinnedMeshRenderer a)
	{
		if (a.sharedMesh == null)
			return;
		
		var smrHash = new Hash128((uint)a.sharedMesh.GetInstanceID(), 0, 0, 0);
		var e = GetEntity(a, TransformUsageFlags.Renderable);
		
		var isSMSBlobExists = TryGetBlobAssetReference<SkinnedMeshInfoBlob>(smrHash, out var smrBlobAsset);
		if (!isSMSBlobExists)
		{
			smrBlobAsset = CreateSkinnedMeshBlob(a, smrHash);
			AddBlobAssetWithCustomHash(ref smrBlobAsset, smrHash);
		}
		
		var asmc = new AnimatedSkinnedMeshComponent()
		{
			smrInfoBlob = smrBlobAsset,
			animatedRigEntity = GetEntity(a.gameObject.GetComponentInParent<RigDefinitionAuthoring>(true), TransformUsageFlags.Dynamic),
			rootBoneIndexInRig = -1,
			nameHash = FixedStringExtensions.CalculateHash32(new FixedStringName(a.name))
		};
		AddComponent(e, asmc);
		
		var rbe = new SkinnedMeshRendererRootBoneEntity()
		{
			value = GetEntity(a.rootBone, TransformUsageFlags.None)
		};
		AddComponent(e, rbe);
		
		//	Skinned mesh renderer is split into multiple render entities in runtime. We need to track renderer<->skinned mesh relationships
		var c = new AnimatedRendererBakingComponent()
		{
			needUpdateRenderBounds = a.updateWhenOffscreen,
			animatorEntity = GetEntity(GetComponentInParent<RigDefinitionAuthoring>(), TransformUsageFlags.None)
		};
		AddComponent(e, c);

	#if RUKHANKA_DEBUG_INFO
		if (a.rootBone == null && a.updateWhenOffscreen)
			Debug.LogError($"Skinned mesh '{a.name}' root bone is null. This will prevent renderer bounding box recalculation! Disable 'Update When Offscreen' or assign valid root bone.");
	#endif
			
		if (a.updateWhenOffscreen && a.rootBone != null)
		{
			var lb = a.localBounds;
			var aabb = new AABB() { Center = lb.center, Extents = lb.extents };
			var smb = new SkinnedMeshBounds() { value = aabb };
			AddComponent(e, smb);
		}
		
	#if !RUKHANKA_NO_DEFORMATION_SYSTEM
		CheckMaterialCompatibility(a);
        CreateRenderComponents(a);
        CreateSkinMatricesBuffer(a);
        CreateBlendShapeWeightsBuffer(a);
	#endif
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateSkinnedMeshBonesBlob(ref BlobBuilder bb, ref SkinnedMeshInfoBlob smrBlob, SkinnedMeshRenderer r)
	{
		var bonesArr = bb.Allocate(ref smrBlob.bones, r.bones.Length);
		for (int j = 0; j < bonesArr.Length; ++j)
		{
			var b = r.bones[j];
			ref var boneBlob = ref bonesArr[j];
			
			if (b != null)
			{
	#if RUKHANKA_DEBUG_INFO
				bb.AllocateString(ref boneBlob.name, b.name);
	#endif
				var bn = new FixedStringName(b.name);
				boneBlob.hash = bn.CalculateHash128();
				boneBlob.bindPose = r.sharedMesh.bindposes[j];
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBlendShapesBlob(ref BlobBuilder bb, ref SkinnedMeshInfoBlob smrBlob, SkinnedMeshRenderer r)
	{
		var blendShapeHashesArr = bb.Allocate(ref smrBlob.blendShapes, r.sharedMesh.blendShapeCount);
		for (var j = 0; j < blendShapeHashesArr.Length; ++j)
		{
			ref var bs = ref blendShapeHashesArr[j];
			var bsName = "blendShape." + r.sharedMesh.GetBlendShapeName(j);
			bs.hash = bsName.CalculateHash32();
		#if RUKHANKA_DEBUG_INFO
			if (bsName.Length > 0)
				bb.AllocateString(ref bs.name, bsName);
		#endif
		}
		smrBlob.meshBlendShapesCount = r.sharedMesh.blendShapeCount;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBoneWeightsDataBlob(ref BlobBuilder bb, ref SkinnedMeshInfoBlob smrBlob, SkinnedMeshRenderer r)
	{
		CreateBoneWeightsIndicesBlob(ref bb, ref smrBlob, r);
		smrBlob.meshBoneWeightsCount = r.sharedMesh.GetAllBoneWeights().Length;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBoneWeightsIndicesBlob(ref BlobBuilder bb, ref SkinnedMeshInfoBlob smrBlob, SkinnedMeshRenderer r)
	{
		var mesh = r.sharedMesh;
		var allBoneWeights = mesh.GetBonesPerVertex();
		
		using var outArr = new NativeArray<uint>(allBoneWeights.Length, Allocator.TempJob);
		var computeAbsoluteOffsetsJob = new ComputeAbsoluteBoneWeightsIndicesOffsetsJob()
		{
			bonesPerVertex = allBoneWeights,
			outIndicesArr = outArr
		};
		computeAbsoluteOffsetsJob.Run();
		
		var ba = bb.Allocate(ref smrBlob.boneWeightsIndices, allBoneWeights.Length);
		//UnsafeUtility.MemCpy(ba.GetUnsafePtr(), allBoneWeights.GetUnsafeReadOnlyPtr(), allBoneWeights.Length * UnsafeUtility.SizeOf<uint>());
		for (var i = 0; i < ba.Length; ++i)
		{
			ba[i] = outArr[i];		
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BlobAssetReference<SkinnedMeshInfoBlob> CreateSkinnedMeshBlob(SkinnedMeshRenderer r, Hash128 smrHash)
	{ 
		var bb = new BlobBuilder(Allocator.Temp);
		ref var smrBlob = ref bb.ConstructRoot<SkinnedMeshInfoBlob>();
		smrBlob.hash = smrHash;
	#if RUKHANKA_DEBUG_INFO
		if (r.name.Length > 0)
			bb.AllocateString(ref smrBlob.skeletonName, r.name);
		var startTimeMarker = Time.realtimeSinceStartup;
	#endif
		
		CreateSkinnedMeshBonesBlob(ref bb, ref smrBlob, r);
		CreateBlendShapesBlob(ref bb, ref smrBlob, r);
		CreateBoneWeightsDataBlob(ref bb, ref smrBlob, r);
		
		smrBlob.meshVerticesCount = r.sharedMesh.vertexCount;
		
	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		smrBlob.bakingTime = (float)dt;
	#endif

		var rv = bb.CreateBlobAssetReference<SkinnedMeshInfoBlob>(Allocator.Persistent);
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	Transform GetRootBone(SkinnedMeshRenderer smr)
	{
		return smr.rootBone ?? smr.transform;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if !RUKHANKA_NO_DEFORMATION_SYSTEM

	unsafe void CreateRenderComponents(SkinnedMeshRenderer a)
	{
		//	To bake skinned mesh using builtin Entities.Graphics methods, I need to do many reflection magic because all baking stuff
		//	og EG is private.
		//	All this equivalent to the following:
		//
		//	{
		//		Unity.Rendering.MeshRendererBakingUtility.ConvertConvertToMultipleEntities(...);
		//		var entity = GetEntity(a, TransformUsageFlags.Renderable);
		//		AddComponent(entity, new Unity.Rendering.MeshRendererBakingData() { MeshRenderer = authoring });
		//	}
		
		var skinnedMeshRootBone = GetRootBone(a);
        var entitiesGraphicsSystemType = typeof(EntitiesGraphicsSystem);
        
        var meshRendererBakingUtilityTypeName = "Unity.Rendering.MeshRendererBakingUtility";
        var meshRendererBakingUtilityType = entitiesGraphicsSystemType.Assembly.GetType(meshRendererBakingUtilityTypeName);
		if (meshRendererBakingUtilityType == null)
			throw new NullReferenceException($"Cannot find '{meshRendererBakingUtilityTypeName}'");
		
#if ENTITIES_GRAPHICS_V120_OR_NEWER 
        var cvtMethodName = "ConvertToMultipleEntities";
        var convertNumParameters = 5;
#elif ENTITIES_GRAPHICS_V110_OR_NEWER
        var cvtMethodName = "ConvertToMultipleEntities";
        var convertNumParameters = 6;
#else
        var cvtMethodName = "Convert";
        var convertNumParameters = 7;
#endif
        MethodInfo cvtMethod = null;
		var cvtMethods = meshRendererBakingUtilityType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
		for (var i = 0; i < cvtMethods.Length; ++i)
		{
			var m = cvtMethods[i];
			var methodParams = m.GetParameters();
			if (m.Name == cvtMethodName && methodParams.Length == convertNumParameters)
			{
				cvtMethod = m;
				break;
			}
		}
		if (cvtMethod == null)
			throw new NullReferenceException($"Cannot find method '{cvtMethodName}' in '{meshRendererBakingUtilityTypeName}'");
		
		var cvtMethodGenericInstantiation = cvtMethod.MakeGenericMethod(typeof(SkinnedMeshRenderer));
		
#if ENTITIES_GRAPHICS_V120_OR_NEWER 
		var callParameters = new object[] { this, a, a.sharedMesh, skinnedMeshRootBone, null };
		var additionalEntitiesIndex = 4;
#elif ENTITIES_GRAPHICS_V110_OR_NEWER
		var materials = new List<Material>();
		a.GetSharedMaterials(materials);
		var callParameters = new object[] { this, a, a.sharedMesh, materials, skinnedMeshRootBone, null };
		var additionalEntitiesIndex = 5;
#else
		var materials = new List<Material>();
		a.GetSharedMaterials(materials);
		var callParameters = new object[] { this, a, a.sharedMesh, materials, false, null, skinnedMeshRootBone };
		var additionalEntitiesIndex = 5;
#endif
		cvtMethodGenericInstantiation.Invoke(null, callParameters);
#if ENTITIES_GRAPHICS_V120_OR_NEWER 
		var additionalEntities = (NativeArray<Entity>)callParameters[additionalEntitiesIndex];
		Assert.IsTrue(additionalEntities.IsCreated);
#else
		var additionalEntities = (List<Entity>)callParameters[additionalEntitiesIndex];
#endif
		var aabb = a.localBounds.ToAABB();
		foreach (var ae in additionalEntities)
		{
			AddComponent<DeformedMeshIndex>(ae);
			SetComponent(ae, new RenderBounds { Value = aabb });
		}
		
#if ENTITIES_GRAPHICS_V120_OR_NEWER 
		additionalEntities.Dispose();
#endif
        
		var meshRendererBakingDataTypeName = "Unity.Rendering.MeshRendererBakingData";
        var meshRendererBakingDataType = entitiesGraphicsSystemType.Assembly.GetType(meshRendererBakingDataTypeName);
		var mrbdTypeIndex = TypeManager.GetTypeIndex(meshRendererBakingDataType);
		var typeInfo = TypeManager.GetTypeInfo(mrbdTypeIndex);
        var untypedComponentData = UnsafeUtility.Malloc(typeInfo.TypeSize, typeInfo.AlignmentInBytes, Allocator.Temp);
        UnityObjectRef<Renderer> meshRenderer = a;
        UnsafeUtility.MemCpy(untypedComponentData, &meshRenderer, typeInfo.TypeSize);
        
		var e = GetEntity(a, TransformUsageFlags.Renderable);
		UnsafeAddComponent(e, mrbdTypeIndex, typeInfo.TypeSize, untypedComponentData);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateSkinMatricesBuffer(SkinnedMeshRenderer a)
	{
		var e = GetEntity(a, TransformUsageFlags.None);
		var mesh = a.sharedMesh;
		var skinnedMeshRootBone = GetRootBone(a);
		var rootBoneMatrixInverse = math.inverse(skinnedMeshRootBone.localToWorldMatrix);
		
		var boneWeights = mesh.GetAllBoneWeights();
		var bindPoses = mesh.GetBindposes();
		var bones = a.bones;
		
		Assert.IsTrue(bones.Length == bindPoses.Length);
		
		var hasSkinning = boneWeights.Length > 0 && bindPoses.Length > 0;
		if (!hasSkinning)
			return;
		
		var skinMatrixBuf = AddBuffer<Rukhanka.SkinMatrix>(e);
		skinMatrixBuf.Resize(bindPoses.Length, NativeArrayOptions.UninitializedMemory);
		
		for (var i = 0; i < bones.Length; ++i)
		{
			var b = bones[i];
			if (b == null)
				continue;

			DependsOn(bones[i]);

			var bp = bindPoses[i];
			var boneMatRootSpace = math.mul(rootBoneMatrixInverse, b.localToWorldMatrix);
			var skinMatRootSpace = math.mul(boneMatRootSpace, bp);
			var sm = new Rukhanka.SkinMatrix()
			{
				Value = new float3x4(skinMatRootSpace.c0.xyz, skinMatRootSpace.c1.xyz, skinMatRootSpace.c2.xyz, skinMatRootSpace.c3.xyz)
			};
			skinMatrixBuf[i] = sm;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBlendShapeWeightsBuffer(SkinnedMeshRenderer a)
	{
		var mesh = a.sharedMesh;
		if (mesh.blendShapeCount == 0)
			return;
		
		var e = GetEntity(a, TransformUsageFlags.None);
		var bswb = AddBuffer<Rukhanka.BlendShapeWeight>(e);
		for (var i = 0; i < mesh.blendShapeCount; ++i)
		{
			var srcBlendShapeWeight = a.GetBlendShapeWeight(i);
			var bsw = new Rukhanka.BlendShapeWeight()
			{
				Value = srcBlendShapeWeight
			};
			bswb.Add(bsw);
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CheckMaterialCompatibility(SkinnedMeshRenderer a)
	{
		var materials = new List<Material>();
		a.GetSharedMaterials(materials);

		for (var i = 0; i < materials.Count; ++i)
		{
			var m = materials[i];
			if (m == null)
				continue;

		#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
			var deformationCompatibleShader = m.HasProperty("_DotsDeformationParams");
		#else
			var deformationCompatibleShader = m.HasProperty("_ComputeMeshIndex");
		#endif
			if (!deformationCompatibleShader)
			{
				var s = $"Shader [{m.shader.name}] on [{a.name}] does not support skinning. This can result in incorrect rendering."
						+ "Please see <a href=\"https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.2/manual/mesh_deformations.html#material-setup\">documentation</a>"
						+ " for Linear Blend Skinning Node and Compute Deformation Node in Shader Graph";
				Debug.LogWarning(s, a);
			}
		}
	}
#endif
}
}
