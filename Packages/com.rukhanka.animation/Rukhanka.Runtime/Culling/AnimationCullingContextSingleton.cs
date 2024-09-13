using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct AnimationCullingContext: IComponentData, IDisposable
{
    public NativeList<float4> cullingPlanes;
    public NativeList<int2> cullingVolumePlaneRanges;
    public NativeList<LODGroupExtensions.LODParams> lodAffectors;
    
#if RUKHANKA_DEBUG_INFO
    public bool drawCullingVolumes;
    public uint cullingVolumeColor;
    
    public bool drawSceneBoundingBoxes;
    public uint visibleChunkColor;
    public uint invisibleChunkColor;
    public uint visibleRendererColor;
    public uint invisibleRendererColor;
#endif
    
    public void Dispose()
    {
        cullingPlanes.Dispose();
        cullingVolumePlaneRanges.Dispose();
        lodAffectors.Dispose();
    }
    
/////////////////////////////////////////////////////////////////////////////////

    internal void AddCullingPlanes(NativeArray<float4> planes)
    {
        if (planes.Length == 0)
            return;
        
        var volumePlaneRanges = new int2(cullingPlanes.Length, planes.Length);
        cullingVolumePlaneRanges.Add(volumePlaneRanges);
        cullingPlanes.AddRange(planes);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    internal void AddLODAffector(AnimationCullingContextUpdateSystem.CullingCameraData ccd, float lodBias)
    {
        var lp = new LODGroupExtensions.LODParams()
        {
            cameraPos = ccd.pos,
            orthosize = ccd.orthographicSize,
            distanceScale = CalculateDistanceScale(ccd.fieldOfView, lodBias, ccd.isOrthographic, ccd.orthographicSize),
            isOrtho = ccd.isOrthographic
        };
        lodAffectors.Add(lp);
    }
   
/////////////////////////////////////////////////////////////////////////////////

    float CalculateDistanceScale(float fov, float globalLodBias, bool isOrtho, float orthoSize)
    {
        float rv;
        if (isOrtho)
        {
            rv = 2.0f * orthoSize / globalLodBias;
        }
        else
        {
            var halfAngle = math.tan(math.radians(fov * 0.5f));
            rv = 2.0f * halfAngle / globalLodBias;
        }
        return rv;
    }

}
}
