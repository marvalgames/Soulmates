using System;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Rendering;
using Unity.Assertions;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    
//  This class purpose to wrap compute buffer which updated every frame
public class FrameFencedGPUBufferPool<T>: IDisposable where T: unmanaged
{
    BufferPool bufferPool;
    NativeQueue<int> busyBuffers;
    int currentFrameBufferIndex = -1;
    int elementCount;
    readonly GraphicsBuffer.Target bufferTargetFlags;
    readonly GraphicsBuffer.UsageFlags bufferUsageFlags;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public FrameFencedGPUBufferPool(int count, GraphicsBuffer.Target target, GraphicsBuffer.UsageFlags usageFlags)
    {
        elementCount = count;
        bufferTargetFlags = target;
        bufferUsageFlags = usageFlags;
        bufferPool = new BufferPool(count, UnsafeUtility.SizeOf<T>(), target, usageFlags);
        busyBuffers = new (Allocator.Persistent);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void Dispose()
    {
        busyBuffers.Dispose();
        bufferPool?.Dispose();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void BeginFrame()
    {
        Assert.IsTrue(currentFrameBufferIndex == -1);
        RecoverBuffers();
        currentFrameBufferIndex = bufferPool.GetBufferId();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void EndFrame()
    {
        Assert.IsFalse(currentFrameBufferIndex == -1);
        busyBuffers.Enqueue(currentFrameBufferIndex);
        currentFrameBufferIndex = -1;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void RecoverBuffers()
    {
        var totalLiveFrames = SparseUploader.NumFramesInFlight + 1;
        for (var i = totalLiveFrames; i < busyBuffers.Count; ++i)
        {
            var bufID = busyBuffers.Dequeue();
            bufferPool.PutBufferId(bufID);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool Grow(int requiredElementsCount)
    {
        var chunkSizeInBytes = 0x1000 * 16;
        var elementSizeInBytes = UnsafeUtility.SizeOf<T>();
        var chunkSizeInElements = chunkSizeInBytes / elementSizeInBytes;
        var newSizeInElements = requiredElementsCount + chunkSizeInElements;
        if (elementCount < newSizeInElements)
        {
            var newSizeInBytes = newSizeInElements * elementSizeInBytes;
            
            if (newSizeInBytes > SystemInfo.maxGraphicsBufferSize)
            {
                var formattedRequiredBytes = CommonTools.FormatMemory(newSizeInBytes);
                var formattedMaxComputeBufferBytes = CommonTools.FormatMemory(SystemInfo.maxGraphicsBufferSize);
                throw new InvalidOperationException($"Requested buffer size ({requiredElementsCount} elements, {formattedRequiredBytes}) exceeded maximum compute buffer capacity of '{formattedMaxComputeBufferBytes}'");
            }
            
            bufferPool?.Dispose();
            busyBuffers.Clear();
            bufferPool = new BufferPool(newSizeInElements, UnsafeUtility.SizeOf<T>(), bufferTargetFlags, bufferUsageFlags);
            elementCount = newSizeInElements;
            currentFrameBufferIndex = -1;
            return true;
        }
        return false;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public NativeArray<T> LockBufferForWrite(int startIndex, int count)
    {
        var b = bufferPool.GetBufferFromId(currentFrameBufferIndex);
        var rv = b.LockBufferForWrite<T>(startIndex, count);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void UnlockBufferAfterWrite(int count)
    {
        var b = bufferPool.GetBufferFromId(currentFrameBufferIndex);
        b.UnlockBufferAfterWrite<T>(count);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public static implicit operator GraphicsBuffer(FrameFencedGPUBufferPool<T> b) => b.bufferPool.GetBufferFromId(b.currentFrameBufferIndex);
}
}
