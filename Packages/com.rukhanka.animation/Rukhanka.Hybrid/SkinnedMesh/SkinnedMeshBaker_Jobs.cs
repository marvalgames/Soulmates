using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public partial class SkinnedMeshBaker
{
[BurstCompile]
struct ComputeAbsoluteBoneWeightsIndicesOffsetsJob: IJob
{
    [ReadOnly]
    public NativeArray<byte> bonesPerVertex;
    public NativeArray<uint> outIndicesArr;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Execute()
    {
        var absOffset = 0u;
        for (var i = 0; i < bonesPerVertex.Length; ++i)
        {
            var bpv = bonesPerVertex[i];
            var v = SkinnedMeshInfoBlob.PackBoneCountAndOffset(bpv, absOffset);
            outIndicesArr[i] = v;
            absOffset += bpv;
        }
    }
}
}
}
