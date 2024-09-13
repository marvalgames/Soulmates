using Unity.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{
public static class ComputeBufferTools
{
    static ComputeShader copyCS;
    static ComputeKernel copyKernel;
    static ComputeKernel clearKernel;
    static readonly int ShaderID_copyBufferElementsCount = Shader.PropertyToID("copyBufferElementsCount");
    static readonly int ShaderID_srcBuf = Shader.PropertyToID("srcBuf");
    static readonly int ShaderID_dstBuf = Shader.PropertyToID("dstBuf");
    static readonly int ShaderID_srcOffset = Shader.PropertyToID("srcOffset");
    static readonly int ShaderID_dstOffset = Shader.PropertyToID("dstOffset");
    static readonly int ShaderID_clearValue = Shader.PropertyToID("clearValue");
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static GraphicsBuffer GrowNoCopy(GraphicsBuffer gb, int newElementCount)
    {
        Assert.IsNotNull(gb);
        if (gb == null)
            return null;
        
        if (newElementCount <= gb.count)
            return gb;
        
        var newBuf = new GraphicsBuffer(gb.target, gb.usageFlags, newElementCount, gb.stride);
        gb.Dispose();
        return newBuf;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void Copy<T>(GraphicsBuffer src, GraphicsBuffer dst, uint srcOffsetInElements, uint dstOffsetInElements, uint copyCount) where T: unmanaged
    {
        var elementSizeInBytes = UnsafeUtility.SizeOf<T>();
        Copy(src, dst, (uint)(srcOffsetInElements * elementSizeInBytes), (uint)(dstOffsetInElements * elementSizeInBytes), (uint)(copyCount * elementSizeInBytes));
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void Copy(GraphicsBuffer src, GraphicsBuffer dst, uint srcOffsetInBytes, uint dstOffsetInBytes, uint numBytesToCopy) 
    {
        Assert.IsTrue(numBytesToCopy % 4 == 0);
        Assert.IsTrue(srcOffsetInBytes % 4 == 0);
        Assert.IsTrue(dstOffsetInBytes % 4 == 0);
        
        var numIntsToCopy = numBytesToCopy / 4;
        if (numIntsToCopy > 0)
        {
            if (copyCS == null)
                copyCS = Resources.Load<ComputeShader>("RukhankaGPUBufferManipulation");
            if (copyKernel == null)
                copyKernel = new ComputeKernel(copyCS, "CopyBuffer");
            
            copyCS.SetBuffer(copyKernel, ShaderID_srcBuf, src);
            copyCS.SetBuffer(copyKernel, ShaderID_dstBuf, dst);
            copyCS.SetInt(ShaderID_copyBufferElementsCount, (int)numIntsToCopy);
            copyCS.SetInt(ShaderID_dstOffset, (int)dstOffsetInBytes);
            copyCS.SetInt(ShaderID_srcOffset, (int)srcOffsetInBytes);
            copyKernel.Dispatch((int)numIntsToCopy, 1, 1);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //  We cannot resize compute buffer. The only way is to create new, copy data, destroy old and return created one.
    public static GraphicsBuffer Resize(GraphicsBuffer gb, int newElementCount)
    {
        Assert.IsNotNull(gb);
        if (gb == null)
            return null;
        
        //  In case of new and old sizes match simply return
        if (newElementCount == gb.count)
            return gb;
        
        Assert.IsTrue((gb.target & GraphicsBuffer.Target.Raw) != 0, "Graphics buffer must be created with 'GraphicsBuffer.Target.Raw' flag");
        
        var newBuf = new GraphicsBuffer(gb.target, gb.usageFlags, newElementCount, gb.stride);
        var elementsToCopy = math.min(newBuf.count, gb.count);
        Copy(gb, newBuf, 0, 0, (uint)(elementsToCopy * gb.stride));
        gb.Dispose();
        return newBuf;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void Clear(GraphicsBuffer gb, int startByteOffset, int clearBytesCount, int clearValue = 0)
    {
        Assert.IsNotNull(gb);
        Assert.IsTrue(clearBytesCount % 4 == 0);
        if (gb == null)
            return;
        
        Assert.IsTrue((gb.target & GraphicsBuffer.Target.Raw) != 0, "Partial clear only for raw buffers");
         
        if (clearBytesCount > 0)
        {
            if (copyCS == null)
                copyCS = Resources.Load<ComputeShader>("RukhankaGPUBufferManipulation");
             if (clearKernel == null)
                 clearKernel = new ComputeKernel(copyCS, "ClearBuffer");
            
            var numIntsToClear = clearBytesCount / 4;
            copyCS.SetBuffer(clearKernel, ShaderID_dstBuf, gb);
            copyCS.SetInt(ShaderID_copyBufferElementsCount, numIntsToClear);
            copyCS.SetInt(ShaderID_dstOffset, startByteOffset);
            copyCS.SetInt(ShaderID_clearValue, clearValue);
            clearKernel.Dispatch(numIntsToClear, 1, 1);
        }
    }
}
}
