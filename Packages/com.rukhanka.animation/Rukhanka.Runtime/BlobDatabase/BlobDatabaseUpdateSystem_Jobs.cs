using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[assembly: RegisterGenericJobType(typeof(Rukhanka.BlobDatabaseUpdateSystem.ProcessNewBlobsJob<Rukhanka.AvatarMaskBlob>))]
[assembly: RegisterGenericJobType(typeof(Rukhanka.BlobDatabaseUpdateSystem.ProcessNewBlobsJob<Rukhanka.AnimationClipBlob>))]

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public partial struct BlobDatabaseUpdateSystem
{
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

internal struct ProcessNewBlobsJob<T>: IJobChunk where T: unmanaged
{
    public BufferTypeHandle<NewBlobAssetDatabaseRecord<T>> componentTypeHandle;
    public NativeHashMap<Hash128, BlobAssetReference<T>> db;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
        var ba = chunk.GetBufferAccessor(ref componentTypeHandle);
        for (var i = 0; i < chunk.Count && ba.Length > 0; ++i)
        {
            var newBlobsArr = ba[i];
            for (int j = 0; j < newBlobsArr.Length; ++j)
            {
                var a = newBlobsArr[j];
                db[a.hash] = a.value;
            }
        }
    }
}
}
}

