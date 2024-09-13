using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
	internal struct SkinnedMeshRendererFrameDeformationData
	{
		public int skinMatrixIndex;
		public int deformedVertexIndex;
		public int blendShapeWeightIndex;
		
		public static SkinnedMeshRendererFrameDeformationData MakeDefault()
		{
			var rv = new SkinnedMeshRendererFrameDeformationData()
			{
				deformedVertexIndex = -1,
				skinMatrixIndex = -1,
				blendShapeWeightIndex = -1
			};
			return rv;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal struct SourceMeshVertex
	{
		public float3 position;
		public float3 normal;
		public float3 tangent;
		public uint boneWeightsOffsetAndCount;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal struct DeformedVertex
	{
		public float3 position;
		public float3 normal;
		public float3 tangent;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal struct BlendShapeVertexDelta
	{
		public int originalMeshVertexIndex;
		public float3 positionDelta;
		public float3 normalDelta;
		public float3 tangentDelta;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal struct SkinnedMeshDescription
	{
		public int baseVertex;
		public int vertexCount;
		public int baseBoneWeightIndex;
		public int baseBlendShapeIndex;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal struct MeshFrameDeformationDescription
	{
		public int baseSkinMatrixIndex;
		public int baseBlendShapeWeightIndex;
		public int baseOutVertexIndex;
		public int baseInputMeshVertexIndex;
		public int baseInputMeshBlendShapeIndex;
		public int meshVerticesCount;
		public int meshBlendShapesCount;
	}
	
//-----------------------------------------------------------------------------------------------------------------//

	internal struct DeformationRuntimeData: IComponentData, IDisposable
	{
		public NativeParallelHashMap<int, BRGRenderMeshArray> renderMeshArrays;
		
		public NativeParallelHashMap<Hash128, SkinnedMeshDescription> registeredSkinnedMeshesMap;
		public NativeParallelHashMap<BatchMeshID, BlobAssetReference<SkinnedMeshInfoBlob>> newSkinnedMeshesToRegister;
		public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap;
		
		public int totalSkinnedVerticesCount;
		public int totalBoneWeightsCount;
		public int totalBlendShapeVerticesCount;
		public int frameSkinMatrixCount;
		public int frameBlendShapeWeightsCount;
		public int frameDeformedVerticesCount;
		public int frameDeformedMeshesCount;
		public int frameActiveDeformedMeshesCount;
		public int maximumVerticesAcrossAllRegisteredMeshes;
		
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public void Dispose()
		{
			registeredSkinnedMeshesMap.Dispose();
			newSkinnedMeshesToRegister.Dispose();
			entityToSMRFrameDataMap.Dispose();
		}
		
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
		public static DeformationRuntimeData Construct(ref SystemState ss)
		{
			var registerMeshAndMaterialSystem = ss.World.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>();
			BurstAssert.IsTrue(registerMeshAndMaterialSystem != null, $"{nameof(RegisterMaterialsAndMeshesSystem)} was not found!");
			if (registerMeshAndMaterialSystem == null)
				return default;
			
			var rv = new DeformationRuntimeData();
			rv.renderMeshArrays = registerMeshAndMaterialSystem.BRGRenderMeshArrays;
			BurstAssert.IsTrue(rv.renderMeshArrays.IsCreated, "Render mesh arrays is not valid. Probably wrong system creation order.");
			
			rv.registeredSkinnedMeshesMap = new (0xff, Allocator.Persistent);	
			rv.newSkinnedMeshesToRegister = new (0xff, Allocator.Persistent);	
			rv.entityToSMRFrameDataMap = new (0xff, Allocator.Persistent);
			rv.totalSkinnedVerticesCount = 0;
			rv.totalBoneWeightsCount = 0;
			rv.totalBlendShapeVerticesCount = 0;
			rv.frameSkinMatrixCount = 0;
			rv.frameBlendShapeWeightsCount = 0;
			rv.frameDeformedVerticesCount = 0;
			rv.frameDeformedMeshesCount = 0;
			rv.frameActiveDeformedMeshesCount = 0;
			rv.maximumVerticesAcrossAllRegisteredMeshes = 0;
			
			return rv;
		}
	}
}

