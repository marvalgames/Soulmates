#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Rukhanka.Toolbox;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(RukhankaDeformationSystemGroup))]
[UpdateAfter(typeof(SkinnedMeshPreparationSystem))]
public partial class MeshDeformationSystem: SystemBase
{
	GraphicsBuffer meshVertexDataCB;
	GraphicsBuffer meshBoneWeightDataCB;
	GraphicsBuffer meshBlendShapesDataCB;
	GraphicsBuffer newMeshBonesPerVertexCB;
	GraphicsBuffer frameVertexSkinningWorkloadCB;
	
	GraphicsBuffer finalDeformedVerticesCB;
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
	//	Double buffer scheme. Current frame will read previous frame data to calculate motion delta
	GraphicsBuffer finalDeformedVerticesCB1;
	bool buffer0IsFront = true;
#endif
	
	FrameFencedGPUBufferPool<SkinMatrix> frameSkinMatricesBuffer;
	FrameFencedGPUBufferPool<float> frameBlendShapeWeightsBuffer;
	FrameFencedGPUBufferPool<MeshFrameDeformationDescription> frameMeshDeformationDescriptionBuffer;
	
	ComputeShader meshDeformationSystemCS;
	ComputeKernel fillInitialMeshDataKernel;
	ComputeKernel fillInitialMeshBlendShapesKernel;
	ComputeKernel createPerVertexDeformationWorkloadKernel;
	ComputeKernel skinningKernel;
	EntitiesGraphicsSystem entitiesGraphicsSystem;
	
	SharedComponentTypeHandle<RenderMeshArray> renderMeshArrayTypeHandle;

	struct InputMeshVertexDesc
	{
		public VertexAttribute vertexAttribute;
		public VertexAttributeFormat vertexAttributeFormat;
		public int streamIndex;
		public int dimension;
	}
	
	static readonly InputMeshVertexDesc[] inputMeshVertexDesc =
	{
		new () {vertexAttribute = VertexAttribute.Position, vertexAttributeFormat = VertexAttributeFormat.Float32, streamIndex = 0, dimension = 3 },
		new () {vertexAttribute = VertexAttribute.Normal, vertexAttributeFormat = VertexAttributeFormat.Float32, streamIndex = 0, dimension = 3 },
		new () {vertexAttribute = VertexAttribute.Tangent, vertexAttributeFormat = VertexAttributeFormat.Float32, streamIndex = 0, dimension = 4 },
	};
	
	readonly int ShaderID_inputVertexSizeInBytes = Shader.PropertyToID("inputVertexSizeInBytes");
	readonly int ShaderID_outDataVertexOffset = Shader.PropertyToID("outDataVertexOffset");
	readonly int ShaderID_totalMeshVertices = Shader.PropertyToID("totalMeshVertices");
	readonly int ShaderID_meshVertexData = Shader.PropertyToID("meshVertexData");
	readonly int ShaderID_outInitialDeformedMeshData = Shader.PropertyToID("outInitialDeformedMeshData");
	readonly int ShaderID_meshBonesPerVertexData = Shader.PropertyToID("meshBonesPerVertexData");
	readonly int ShaderID_inputBonesWeightsDataOffset = Shader.PropertyToID("inputBonesWeightsDataOffset");
	readonly int ShaderID_outBonesWeightsDataOffset = Shader.PropertyToID("outBonesWeightsDataOffset");
	readonly int ShaderID_frameDeformedMeshes = Shader.PropertyToID("frameDeformedMeshes");
	readonly int ShaderID_outFramePerVertexWorkload = Shader.PropertyToID("outFramePerVertexWorkload");
	readonly int ShaderID_framePerVertexWorkload = Shader.PropertyToID("framePerVertexWorkload");
	readonly int ShaderID_inputMeshVertexData = Shader.PropertyToID("inputMeshVertexData");
	readonly int ShaderID_inputBoneInfluences = Shader.PropertyToID("inputBoneInfluences");
	readonly int ShaderID_inputBlendShapes = Shader.PropertyToID("inputBlendShapes");
	readonly int ShaderID_frameSkinMatrices = Shader.PropertyToID("frameSkinMatrices");
	readonly int ShaderID_frameBlendShapeWeights = Shader.PropertyToID("frameBlendShapeWeights");
	readonly int ShaderID_outDeformedVertices = Shader.PropertyToID("outDeformedVertices");
	readonly int ShaderID_totalDeformedMeshesCount = Shader.PropertyToID("totalDeformedMeshesCount");
	readonly int ShaderID_totalSkinnedVerticesCount = Shader.PropertyToID("totalSkinnedVerticesCount");
	readonly int ShaderID_voidMeshVertexCount = Shader.PropertyToID("voidMeshVertexCount");
	readonly int ShaderID_DeformedMeshData = Shader.PropertyToID("_DeformedMeshData");
	readonly int ShaderID_PreviousFrameDeformedMeshData = Shader.PropertyToID("_PreviousFrameDeformedMeshData");
	readonly int ShaderID_meshBlendShapesBuffer = Shader.PropertyToID("meshBlendShapesBuffer");
	readonly int ShaderID_outInitialMeshBlendShapesData = Shader.PropertyToID("outInitialMeshBlendShapesData");
	readonly int ShaderID_inputBlendShapeVerticesCount = Shader.PropertyToID("inputBlendShapeVerticesCount");
	readonly int ShaderID_inputBlendShapeVertexOffset = Shader.PropertyToID("inputBlendShapeVertexOffset");
	readonly int ShaderID_outBlendShapeVertexOffset = Shader.PropertyToID("outBlendShapeVertexOffset");
	
	EntityQuery activeDeformedEntitiesQuery;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		renderMeshArrayTypeHandle = GetSharedComponentTypeHandle<RenderMeshArray>();
		entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
		frameSkinMatricesBuffer = new (0xffff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		frameBlendShapeWeightsBuffer = new (0xffff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		frameMeshDeformationDescriptionBuffer = new (0xffff, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite);
		
		activeDeformedEntitiesQuery = SystemAPI.QueryBuilder()
			.WithAll<DeformedMeshIndex, AnimatedRendererComponent, MaterialMeshInfo>()
			.Build();
		
		RequireForUpdate(activeDeformedEntitiesQuery);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnDestroy()
	{
		meshVertexDataCB?.Dispose();
		meshBoneWeightDataCB?.Dispose();
		meshBlendShapesDataCB?.Dispose();
		newMeshBonesPerVertexCB?.Dispose();
		frameVertexSkinningWorkloadCB?.Dispose();
		
		finalDeformedVerticesCB?.Dispose();
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		finalDeformedVerticesCB1?.Dispose();
	#endif
		
		frameMeshDeformationDescriptionBuffer.Dispose();
		frameSkinMatricesBuffer.Dispose();
		frameBlendShapeWeightsBuffer.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		renderMeshArrayTypeHandle.Update(this);
		
		ref var deformationRuntimeData = ref SystemAPI.GetSingletonRW<DeformationRuntimeData>().ValueRW;
		deformationRuntimeData.frameDeformedMeshesCount = activeDeformedEntitiesQuery.CalculateEntityCount();
		
		var resetFrameCountersJH = ResetFrameCounters(ref deformationRuntimeData, Dependency);
		var prepareSkinningDataJH = PrepareMeshGPUSkinningData(ref deformationRuntimeData, resetFrameCountersJH);
		
		//	Complete previous jobs here, because we need to know skin matrix buffer and blend shape weights sizes here to resize GPU buffers
		prepareSkinningDataJH.Complete();
		
		var copySkinMatricesToGPUBufferJH = CopySkinMatricesToGPUBuffer(deformationRuntimeData, default);
		var copyBlendShapeWeightsToGPUBufferJH = CopyBlendShapeWeightsToGPUBuffer(deformationRuntimeData, default);
		var copyFrameDeformationDataToGPUBuffersJH = JobHandle.CombineDependencies(copySkinMatricesToGPUBufferJH, copyBlendShapeWeightsToGPUBufferJH);
		
		//	Complete dependency second time before compute shader execution. Need to make sure that SkinMatrix GPU buffer data writes
		//	is complete.
		copyFrameDeformationDataToGPUBuffersJH.Complete();
		
		CopyNewMeshesToInitialMeshDataBuffer(deformationRuntimeData);
		
		frameSkinMatricesBuffer.UnlockBufferAfterWrite(deformationRuntimeData.frameSkinMatrixCount);
		frameBlendShapeWeightsBuffer.UnlockBufferAfterWrite(deformationRuntimeData.frameBlendShapeWeightsCount);
		frameMeshDeformationDescriptionBuffer.UnlockBufferAfterWrite(deformationRuntimeData.frameActiveDeformedMeshesCount);
		
		ScheduleSkinningDispatch(deformationRuntimeData);
		
		frameSkinMatricesBuffer.EndFrame();
		frameBlendShapeWeightsBuffer.EndFrame();
		frameMeshDeformationDescriptionBuffer.EndFrame();
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    static GraphicsBuffer CreateOrGrowGraphicsBuffer<T>(GraphicsBuffer gb, GraphicsBuffer.Target target, GraphicsBuffer.UsageFlags usage, int elementCount, bool preserveContents) where T: unmanaged
    {
		if (gb == null)
        {
	        //	In case of zero input size, increase it to make a buffer in any case
	        elementCount = math.max(0xff, elementCount);
			gb = new GraphicsBuffer(target, usage, elementCount, UnsafeUtility.SizeOf<T>());
            return gb;
        }
		
        if (elementCount <= gb.count)
            return gb;
        
        //	To prevent frequent buffer recreations, resize buffer with some additional capacity
        elementCount += 0x1000;
        
		gb = preserveContents ? ComputeBufferTools.Resize(gb, elementCount) : ComputeBufferTools.GrowNoCopy(gb, elementCount);
        return gb;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle ResetFrameCounters(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var resetFrameCountersJob = new ResetFrameCountersJob()
		{
			frameActiveDeformedMeshesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameActiveDeformedMeshesCount))
		};
		
		var rv = resetFrameCountersJob.Schedule(dependsOn);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ScheduleSkinningDispatch(in DeformationRuntimeData drd)
	{
		var frameDeformedVerticesCount = drd.frameDeformedVerticesCount;
		frameVertexSkinningWorkloadCB = CreateOrGrowGraphicsBuffer<uint>(frameVertexSkinningWorkloadCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, frameDeformedVerticesCount, false);
		
		//	Schedule workload generation dispatch if we have visible/existing skinned mesh renderers
		if (frameDeformedVerticesCount > 0)
		{
			var cs0 = createPerVertexDeformationWorkloadKernel.computeShader;
			cs0.SetBuffer(createPerVertexDeformationWorkloadKernel, ShaderID_outFramePerVertexWorkload, frameVertexSkinningWorkloadCB);
			cs0.SetBuffer(createPerVertexDeformationWorkloadKernel, ShaderID_frameDeformedMeshes, frameMeshDeformationDescriptionBuffer);
			cs0.SetInt(ShaderID_totalDeformedMeshesCount, drd.frameActiveDeformedMeshesCount);
			createPerVertexDeformationWorkloadKernel.Dispatch(drd.frameActiveDeformedMeshesCount, 1, 1);
		}
		
		var deformedVerticesBufferSize = frameDeformedVerticesCount + drd.maximumVerticesAcrossAllRegisteredMeshes;
		finalDeformedVerticesCB = CreateOrGrowGraphicsBuffer<DeformedVertex>(finalDeformedVerticesCB, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, deformedVerticesBufferSize, false);
		
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		finalDeformedVerticesCB1 = CreateOrGrowGraphicsBuffer<DeformedVertex>(finalDeformedVerticesCB1, GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.None, deformedVerticesBufferSize, false);
		var outDeformedVerticesCB = buffer0IsFront ? finalDeformedVerticesCB : finalDeformedVerticesCB1;
	#else
		var outDeformedVerticesCB = finalDeformedVerticesCB;
	#endif
		
		//	Schedule skinning even for zero visible meshes, because we need to actualize void mesh zone to properly cull invisible meshes
		if (deformedVerticesBufferSize > 0)
		{
			var cs1 = skinningKernel.computeShader;
			cs1.SetBuffer(skinningKernel, ShaderID_frameDeformedMeshes, frameMeshDeformationDescriptionBuffer);
			cs1.SetBuffer(skinningKernel, ShaderID_framePerVertexWorkload, frameVertexSkinningWorkloadCB);
			cs1.SetBuffer(skinningKernel, ShaderID_inputMeshVertexData, meshVertexDataCB);
			cs1.SetBuffer(skinningKernel, ShaderID_inputBoneInfluences, meshBoneWeightDataCB);
			cs1.SetBuffer(skinningKernel, ShaderID_inputBlendShapes, meshBlendShapesDataCB);
			cs1.SetBuffer(skinningKernel, ShaderID_frameSkinMatrices, frameSkinMatricesBuffer);
			cs1.SetBuffer(skinningKernel, ShaderID_frameBlendShapeWeights, frameBlendShapeWeightsBuffer);
			cs1.SetBuffer(skinningKernel, ShaderID_outDeformedVertices, outDeformedVerticesCB);
			cs1.SetInt(ShaderID_totalSkinnedVerticesCount, frameDeformedVerticesCount);
			cs1.SetInt(ShaderID_voidMeshVertexCount, drd.maximumVerticesAcrossAllRegisteredMeshes);
			skinningKernel.Dispatch(deformedVerticesBufferSize, 1, 1);
		}
		
		Shader.SetGlobalBuffer(ShaderID_DeformedMeshData, outDeformedVerticesCB);
		
	#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
		var prevDeformedVerticesDataCB = buffer0IsFront ? finalDeformedVerticesCB1 : finalDeformedVerticesCB;
		Shader.SetGlobalBuffer(ShaderID_PreviousFrameDeformedMeshData, prevDeformedVerticesDataCB);
		buffer0IsFront = !buffer0IsFront;
	#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle CopySkinMatricesToGPUBuffer(in DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var skinMatrixCount = drd.frameSkinMatrixCount;
		frameSkinMatricesBuffer.Grow(skinMatrixCount);
		frameSkinMatricesBuffer.BeginFrame();
		
		var gpuBufferSkinMatrixOutArr = frameSkinMatricesBuffer.LockBufferForWrite(0, skinMatrixCount);
		
		var copySkinMatricesToGPUJob = new CopySkinMatricesToGPUJob()
		{
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			mappedGPUSkinMatrixBuffer = gpuBufferSkinMatrixOutArr
		};
		
		var jh = copySkinMatricesToGPUJob.ScheduleParallel(dependsOn);
		return jh;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle CopyBlendShapeWeightsToGPUBuffer(in DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var blendShapeWeightsCount = drd.frameBlendShapeWeightsCount;
		frameBlendShapeWeightsBuffer.Grow(blendShapeWeightsCount);
		frameBlendShapeWeightsBuffer.BeginFrame();
		
		var gpuBufferOutArr = frameBlendShapeWeightsBuffer.LockBufferForWrite(0, blendShapeWeightsCount);
		
		var copyBlendShapeWeightsToGPUJob = new CopyBlendShapeWeightToGPUJob()
		{
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			mappedGPUBlendShapeWeightsBuffer = gpuBufferOutArr
		};
		
		var jh = copyBlendShapeWeightsToGPUJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle PrepareMeshGPUSkinningData(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var deformedMeshCount = drd.frameDeformedMeshesCount;
		frameMeshDeformationDescriptionBuffer.Grow(deformedMeshCount);
		frameMeshDeformationDescriptionBuffer.BeginFrame();
		var gpuBufferMeshDeformationOutArr = frameMeshDeformationDescriptionBuffer.LockBufferForWrite(0, deformedMeshCount);
		
		var setDeformedMeshIndicesJH = SetDeformedMeshIndicesForRenderEntities(ref drd, dependsOn);
		var prepareSkinningDataJH = PrepareSkinningCommands(ref drd, gpuBufferMeshDeformationOutArr, dependsOn);
		
		var rv = JobHandle.CombineDependencies(setDeformedMeshIndicesJH, prepareSkinningDataJH);
		
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle PrepareSkinningCommands(ref DeformationRuntimeData drd, NativeArray<MeshFrameDeformationDescription> gpuBufferMeshDeformationOutArr, JobHandle dependsOn)
	{
		var prepareSkinningDataJob = new PrepareSkinningCommandsJob()
		{
			meshFrameDeformationData = gpuBufferMeshDeformationOutArr,
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			registeredSkinnedMeshes = drd.registeredSkinnedMeshesMap,
			frameActiveDeformedMeshesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameActiveDeformedMeshesCount))
		};
		var jh = prepareSkinningDataJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle SetDeformedMeshIndicesForRenderEntities(ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var setDeformedMeshIndexJob = new SetDeformedMeshIndicesJob()
		{
			frameDeformedVerticesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameDeformedVerticesCount)),
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
		#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
			currentFrameDeformedBufferIndex = buffer0IsFront ? 0 : 1
		#endif
		};
		
		var jh = setDeformedMeshIndexJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void InitComputeShaders()
	{
		if (meshDeformationSystemCS != null)
			return;
		
		meshDeformationSystemCS = Resources.Load<ComputeShader>("RukhankaMeshDeformation");
		fillInitialMeshDataKernel = new ComputeKernel(meshDeformationSystemCS, "CopyInitialMeshData");
		fillInitialMeshBlendShapesKernel = new ComputeKernel(meshDeformationSystemCS, "CopyInitialMeshBlendShapes");
		createPerVertexDeformationWorkloadKernel = new ComputeKernel(meshDeformationSystemCS, "CreatePerVertexDeformationWorkload");
		skinningKernel = new ComputeKernel(meshDeformationSystemCS, "Skinning");
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyNewMeshesToInitialMeshDataBuffer(in DeformationRuntimeData drd)
	{
		if (drd.newSkinnedMeshesToRegister.IsEmpty)
			return;
		
		meshVertexDataCB = CreateOrGrowGraphicsBuffer<SourceMeshVertex>(meshVertexDataCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, drd.totalSkinnedVerticesCount, true);
		meshBoneWeightDataCB = CreateOrGrowGraphicsBuffer<BoneWeight1>(meshBoneWeightDataCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, drd.totalBoneWeightsCount, true);
		meshBlendShapesDataCB = CreateOrGrowGraphicsBuffer<DeformedVertex>(meshBlendShapesDataCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, drd.totalBlendShapeVerticesCount, true);
		
		InitComputeShaders();
		var meshToBoneWeightsOffsetMap = CreateNewMeshesBoneIndicesComputeBuffer(drd);

		foreach (var sm in drd.newSkinnedMeshesToRegister)
		{
			var batchMeshID = sm.Key;
			var skinnedMeshHash = sm.Value.Value.hash;
			var smd = drd.registeredSkinnedMeshesMap[skinnedMeshHash];
			var mesh = entitiesGraphicsSystem.GetMesh(batchMeshID);
			var boneWeightsOffsetForMesh = meshToBoneWeightsOffsetMap[batchMeshID];
			
			CopyMeshVertexData(smd, boneWeightsOffsetForMesh, mesh);
			CopyMeshBoneWeightsData(smd, mesh);
			CopyMeshBlendShapes(smd, mesh);
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe NativeHashMap<BatchMeshID, int> CreateNewMeshesBoneIndicesComputeBuffer(in DeformationRuntimeData drd)
	{
		var meshToBoneWeightsOffsetMap = new NativeHashMap<BatchMeshID, int>(0xff, Allocator.Temp);
		var newMeshesBonesPerVertexData = new NativeList<uint>(0xff, Allocator.Temp);
		
		foreach (var newMesh in drd.newSkinnedMeshesToRegister)
		{
			var baseVertexIndex = newMeshesBonesPerVertexData.Length;
			var mesh = entitiesGraphicsSystem.GetMesh(newMesh.Key);
			
			if (!HasSupportedVertexLayout(mesh))
				continue;
			
			meshToBoneWeightsOffsetMap[newMesh.Key] = baseVertexIndex;
			newMeshesBonesPerVertexData.Resize(baseVertexIndex + mesh.vertexCount, NativeArrayOptions.UninitializedMemory);
			
			ref var bwi = ref newMesh.Value.Value.boneWeightsIndices;
			//BurstAssert.IsTrue(bwi.Length == mesh.vertexCount, "Bone weights offsets array does not match vertex count.");
			UnsafeUtility.MemCpy(newMeshesBonesPerVertexData.GetUnsafePtr() + baseVertexIndex, bwi.GetUnsafePtr(), bwi.Length * 4);
		}
		
		newMeshBonesPerVertexCB = CreateOrGrowGraphicsBuffer<uint>(newMeshBonesPerVertexCB, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.None, newMeshesBonesPerVertexData.Length, false);
		newMeshBonesPerVertexCB.SetData(newMeshesBonesPerVertexData.AsArray());
		return meshToBoneWeightsOffsetMap;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyMeshBlendShapes(SkinnedMeshDescription smd, Mesh mesh)
	{
		if (mesh.blendShapeCount == 0)
			return;
		
		var meshAllBlendShapes = mesh.GetBlendShapeBuffer(BlendShapeBufferLayout.PerShape);
		var blendShapeVertexDeltaSize = UnsafeUtility.SizeOf<BlendShapeVertexDelta>();
		Assert.IsTrue(blendShapeVertexDeltaSize == meshAllBlendShapes.stride);
		
		var cs = fillInitialMeshBlendShapesKernel.computeShader;
		cs.SetBuffer(fillInitialMeshBlendShapesKernel, ShaderID_meshBlendShapesBuffer, meshAllBlendShapes);
		cs.SetBuffer(fillInitialMeshBlendShapesKernel, ShaderID_outInitialMeshBlendShapesData, meshBlendShapesDataCB);
		
		var deformedVertexSize = UnsafeUtility.SizeOf<DeformedVertex>();
		ComputeBufferTools.Clear(meshBlendShapesDataCB, smd.baseBlendShapeIndex * deformedVertexSize, smd.vertexCount * deformedVertexSize * mesh.blendShapeCount);
		
		for (var i = 0; i < mesh.blendShapeCount; ++i)
		{
			var bsr = mesh.GetBlendShapeBufferRange(i);
			cs.SetInt(ShaderID_inputBlendShapeVerticesCount, (int)(bsr.endIndex - bsr.startIndex));
			cs.SetInt(ShaderID_inputBlendShapeVertexOffset, (int)bsr.startIndex);
			cs.SetInt(ShaderID_outBlendShapeVertexOffset, smd.baseBlendShapeIndex + i * smd.vertexCount );
			fillInitialMeshBlendShapesKernel.Dispatch(smd.vertexCount, 1, 1);
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyMeshBoneWeightsData(SkinnedMeshDescription smd, Mesh mesh)
	{
		var meshAllBoneWeights = mesh.GetAllBoneWeights();
		meshBoneWeightDataCB.SetData(meshAllBoneWeights, 0, smd.baseBoneWeightIndex, meshAllBoneWeights.Length);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyMeshVertexData(SkinnedMeshDescription smd, int meshBoneWeightsOffset, Mesh mesh)
	{
		//	Copy initial vertex data
		mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
		var meshVertexBuffer = mesh.GetVertexBuffer(0);
		
		meshDeformationSystemCS.SetInt(ShaderID_totalMeshVertices, smd.vertexCount);
		meshDeformationSystemCS.SetInt(ShaderID_outDataVertexOffset, smd.baseVertex);
		var vertexBufferStride = mesh.GetVertexBufferStride(0);
		meshDeformationSystemCS.SetInt(ShaderID_inputVertexSizeInBytes, vertexBufferStride);
		meshDeformationSystemCS.SetInt(ShaderID_inputBonesWeightsDataOffset, meshBoneWeightsOffset);
		meshDeformationSystemCS.SetInt(ShaderID_outBonesWeightsDataOffset, smd.baseBoneWeightIndex);
		meshDeformationSystemCS.SetBuffer(fillInitialMeshDataKernel, ShaderID_meshVertexData, meshVertexBuffer);
		meshDeformationSystemCS.SetBuffer(fillInitialMeshDataKernel, ShaderID_outInitialDeformedMeshData, meshVertexDataCB);
		meshDeformationSystemCS.SetBuffer(fillInitialMeshDataKernel, ShaderID_meshBonesPerVertexData, newMeshBonesPerVertexCB);
		
		fillInitialMeshDataKernel.Dispatch(smd.vertexCount, 1, 1);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool HasSupportedVertexLayout(Mesh mesh)
	{
		if (mesh.vertexAttributeCount < inputMeshVertexDesc.Length)
		{
			Debug.LogError($"Unsupported vertex layout for deformations in mesh ({mesh.name}). Expecting {inputMeshVertexDesc.Length} attributes but mash has only {mesh.vertexAttributeCount}.");
			return false;
		}
		
		//	Check each attribute
		for (var i = 0; i < inputMeshVertexDesc.Length; ++i)
		{
			var attrib = mesh.GetVertexAttribute(i);
			var vd = inputMeshVertexDesc[i];
			
			if (attrib.attribute != vd.vertexAttribute)
			{
				Debug.LogError($"Attribute mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.vertexAttribute}', got '{attrib.attribute}'.");
				return false;
			}
			
			if (attrib.format != vd.vertexAttributeFormat)
			{
				Debug.LogError($"Format mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.vertexAttributeFormat}', got '{attrib.format}'.");
				return false;
			}
			
			if (attrib.dimension != vd.dimension)
			{
				Debug.LogError($"Attribute dimension mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.dimension}', got '{attrib.dimension}'.");
				return false;
			}
			
			if (attrib.stream != vd.streamIndex)
			{
				Debug.LogError($"Attribute stream index mismatch for deformations in mesh ({mesh.name}). Expecting '{vd.streamIndex}', got '{attrib.stream}'.");
				return false;
			}
		}
		
		return true;
	}
}
}

#endif