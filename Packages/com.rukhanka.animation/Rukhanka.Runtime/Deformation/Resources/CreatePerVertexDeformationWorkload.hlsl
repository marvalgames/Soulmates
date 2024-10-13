#ifndef CREATE_PER_VERTEX_DEFORMATION_WORKLOAD_HLSL_
#define CREATE_PER_VERTEX_DEFORMATION_WORKLOAD_HLSL_

/////////////////////////////////////////////////////////////////////////////////

RWByteAddressBuffer outFramePerVertexWorkload;
uint totalDeformedMeshesCount;

/////////////////////////////////////////////////////////////////////////////////

[numthreads(128, 1, 1)]
void CreatePerVertexDeformationWorkload(uint tid: SV_DispatchThreadID)
{
    if (tid >= totalDeformedMeshesCount)
        return;

    MeshFrameDeformationDescription md = frameDeformedMeshes[tid];

    for (int i = 0; i < md.meshVerticesCount; ++i)
    {
        int outVertexIndex = i + md.baseOutVertexIndex;
        outFramePerVertexWorkload.Store(outVertexIndex * 4, tid);
    }
}

#endif // CREATE_PER_VERTEX_DEFORMATION_WORKLOAD_HLSL_
