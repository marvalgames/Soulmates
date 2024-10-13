using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.DebugDrawer
{
public class DrawBuffer<T>: IDisposable where T: unmanaged
{
    internal GraphicsBuffer gpuBuffer;
    internal int counter;
    internal UnsafeAtomicCounter32 counterAtomic;
    NativeList<T> bufferData;
    
/////////////////////////////////////////////////////////////////////////////////

    public DrawBuffer()
    {
        unsafe
        {
            fixed (void* counterPtr = &counter)
            {
                counterAtomic = new UnsafeAtomicCounter32(counterPtr);
            }
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    internal NativeArray<T> BeginFrame()
    {
        ResizeBuffer();
        counterAtomic.Reset();

        var rv = bufferData.AsArray();
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////

    internal int EndFrame()
    {
        if (counter == 0)
            return 0;
        
        ResizeGPUBuffer();
        var cnt = math.min(bufferData.Length, counter);
        gpuBuffer.SetData(bufferData.AsArray(), 0, 0, cnt);
        return cnt;
    }

/////////////////////////////////////////////////////////////////////////////////

    void ResizeGPUBuffer()
    {
        if (gpuBuffer == null || gpuBuffer.count < counter)
        {
            if (gpuBuffer != null)
                gpuBuffer.Dispose();
            var cnt = math.max(counter, 0xffff);
            gpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, cnt, UnsafeUtility.SizeOf<T>());
        }
    }
        
/////////////////////////////////////////////////////////////////////////////////

    void ResizeBuffer()
    {
        if (!bufferData.IsCreated)
        {
            bufferData = new (counter, Allocator.Persistent);
        }
        var cnt = math.max(counter, 0xffff);
        bufferData.Resize(cnt, NativeArrayOptions.ClearMemory);
    }

/////////////////////////////////////////////////////////////////////////////////

    public void Dispose()
    {
        if (gpuBuffer != null)
            gpuBuffer.Dispose();
        gpuBuffer = null;
        
        bufferData.Dispose();
    }
}
}
