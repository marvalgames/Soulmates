using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial struct BlobDatabaseUpdateSystem: ISystem
{
    EntityQuery newBlobAssetsQuery;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnCreate(ref SystemState ss)
    {
        CreateDBSingleton(ref ss);
        
        newBlobAssetsQuery = SystemAPI.QueryBuilder()
            .WithAny
                <
                    NewBlobAssetDatabaseRecord<AnimationClipBlob>,
                    NewBlobAssetDatabaseRecord<AvatarMaskBlob>
                >()
            .WithOptions(EntityQueryOptions.IncludePrefab)
            .Build();
        
        ss.RequireForUpdate(newBlobAssetsQuery);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void CreateDBSingleton(ref SystemState ss)
    {
        var blobDB = new BlobDatabaseSingleton()
        {
            animations = new (128, Allocator.Persistent),
            avatarMasks = new (128, Allocator.Persistent),
        };
        ss.EntityManager.CreateSingleton(blobDB, "Rukhanka.BlobDatabaseSingleton");
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
        if (!SystemAPI.TryGetSingletonRW<BlobDatabaseSingleton>(out var db))
            return;
        
        var ecbs = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbs.CreateCommandBuffer(ss.WorldUnmanaged);
        
        ProcessNewBlobs(ref ss, ref db.ValueRW.avatarMasks, ecb);
        ProcessNewBlobs(ref ss, ref db.ValueRW.animations, ecb);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void ProcessNewBlobs<T>(ref SystemState ss, ref NativeHashMap<Hash128, BlobAssetReference<T>> db, EntityCommandBuffer ecb) where T: unmanaged
    {
        var componentTypeHandle = ss.EntityManager.GetBufferTypeHandle<NewBlobAssetDatabaseRecord<T>>(true);
        var processNewBlobsJob = new ProcessNewBlobsJob<T>()
        {
            db = db,
            componentTypeHandle = componentTypeHandle
        };
        ss.Dependency = processNewBlobsJob.Schedule(newBlobAssetsQuery, ss.Dependency);
        ecb.RemoveComponent<NewBlobAssetDatabaseRecord<T>>(newBlobAssetsQuery, EntityQueryCaptureMode.AtPlayback);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnDestroy(ref SystemState ss)
    {
        if (SystemAPI.TryGetSingletonRW<BlobDatabaseSingleton>(out var animDB))
        {
            animDB.ValueRW.animations.Dispose();
            animDB.ValueRW.avatarMasks.Dispose();
        }
    }
}
}

