using Unity.Assertions;
using Unity.Mathematics;
using UnityEngine;
using SystemInfo = UnityEngine.Device.SystemInfo;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{
public class ComputeKernel
{
    public readonly uint3 numThreadGroups;
    public readonly int kernelIndex;
    public readonly string kernelName;
    public readonly ComputeShader computeShader;
        
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public ComputeKernel(ComputeShader cs, string kernelName)
    {
        kernelIndex = cs.FindKernel(kernelName);
        this.kernelName = kernelName;
        cs.GetKernelThreadGroupSizes(kernelIndex, out numThreadGroups.x, out numThreadGroups.y, out numThreadGroups.z);
        Assert.IsTrue(numThreadGroups.x <= SystemInfo.maxComputeWorkGroupSizeX, $"Kernel '{kernelName}({kernelIndex})' of shader '{cs.name}' work group size X '{numThreadGroups.x}' exceeds hardware limit of '{SystemInfo.maxComputeWorkGroupSizeX}'");
        Assert.IsTrue(numThreadGroups.y <= SystemInfo.maxComputeWorkGroupSizeY, $"Kernel '{kernelName}({kernelIndex})' of shader '{cs.name}' work group size Y '{numThreadGroups.y}' exceeds hardware limit of '{SystemInfo.maxComputeWorkGroupSizeY}'");
        Assert.IsTrue(numThreadGroups.z <= SystemInfo.maxComputeWorkGroupSizeZ, $"Kernel '{kernelName}({kernelIndex})' of shader '{cs.name}' work group size Z '{numThreadGroups.z}' exceeds hardware limit of '{SystemInfo.maxComputeWorkGroupSizeZ}'");
        computeShader = cs; 
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispatch(int workGroupSizeX, int workgroupSizeY, int workgroupSizeZ)
    {
        var workGroupSize = new int3(workGroupSizeX, workgroupSizeY, workgroupSizeZ);
        var numDispatches = (int3)math.ceil(workGroupSize / (float3)numThreadGroups);
    #if UNITY_ASSERTIONS
        if (ValidateDispatch(workGroupSizeX, workgroupSizeY, workgroupSizeZ))
    #endif
        computeShader.Dispatch(kernelIndex, numDispatches.x, numDispatches.y, numDispatches.z);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool ValidateDispatch(int workGroupSizeX, int workgroupSizeY, int workgroupSizeZ)
    {
        var workGroupSize = new int3(workGroupSizeX, workgroupSizeY, workgroupSizeZ);
        var numDispatches = (int3)math.ceil(workGroupSize / (float3)numThreadGroups);
        int maxDispatchesCount = 0xffff;
        if (numDispatches.x > maxDispatchesCount)
            Debug.LogError($"Kernel '{kernelName}({kernelIndex})' of shader '{computeShader.name}' dispatch thread group count X '{numDispatches.x}' exceeds hardware limit of '{maxDispatchesCount}'. Try to increase kernel work group size.");
        if (numDispatches.y > maxDispatchesCount)
            Debug.LogError($"Kernel '{kernelName}({kernelIndex})' of shader '{computeShader.name}' dispatch thread group count Y '{numDispatches.y}' exceeds hardware limit of '{maxDispatchesCount}'. Try to increase kernel work group size.");
        if (numDispatches.z > maxDispatchesCount)
            Debug.LogError($"Kernel '{kernelName}({kernelIndex})' of shader '{computeShader.name}' dispatch thread group count Z '{numDispatches.z}' exceeds hardware limit of '{maxDispatchesCount}'. Try to increase kernel work group size.");
        return math.all(numDispatches <= maxDispatchesCount);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static implicit operator int(ComputeKernel c) => c.kernelIndex;
}
}
