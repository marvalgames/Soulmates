#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public partial class AnimationCullingContextUpdateSystem
{

[BurstCompile]
struct BuildCullingContextJob: IJob
{
    [ReadOnly]
    public NativeArray<CullingCameraData> cullingCameras;
    [ReadOnly]
    public NativeArray<LightData> shadowCastingLights;
    
    public float lodBias;
    public AnimationCullingContext actx;
    
#if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
    public Drawer dd;
#endif
    
/////////////////////////////////////////////////////////////////////////////////

    public void Execute()
    {
        actx.cullingPlanes.Clear();
        actx.cullingVolumePlaneRanges.Clear();
        actx.lodAffectors.Clear();
        
        foreach (var cm in cullingCameras)
        {
            var frustum = FrustumVolume.MakeCullingCameraFrustum(cm);
            actx.AddLODAffector(cm, lodBias);
            
            var i = 0;
            for (; i < shadowCastingLights.Length; ++i)
            {
                var l = shadowCastingLights[i];
                var cullingPlanes = MakeShadowVolume(frustum, l, cm.pos);
                actx.AddCullingPlanes(cullingPlanes);
            }
            
            //  In case of no lights add input frustum as culling planes
            if (i == 0)
            {
                actx.AddCullingPlanes(frustum.planes);
                
            #if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
                if (actx.drawCullingVolumes)
                    frustum.DrawWireOutline(dd, actx.cullingVolumeColor); 
            #endif
            }
        } 
        
    }
    
/////////////////////////////////////////////////////////////////////////////////

    NativeArray<float4> MakeShadowVolume(FrustumVolume frustum, LightData l, float3 cameraPos)
    {
        var rv = new NativeList<float4>(16, Allocator.Temp);
        var lightDir = l.posOrDir.xyz;
        var nearShadowPlane = float4.zero;
        
        //  For infinite (directional) lights
        if (l.posOrDir.w == 0)
        {
            //  Project hull points onto light vector and pick maximum distance
            var shadowDist = l.range;
            for (var i = 0; i < frustum.points.Length; ++i)
            {
                var fp = frustum.points[i];
                var cameraRelativePos = fp - cameraPos;
                var projPos = math.dot(cameraRelativePos, lightDir) * lightDir;
                //  Ignore points that lie behind camera
                var d = math.dot(lightDir, cameraRelativePos);
                if (d > 0)
                    shadowDist = math.max(math.length(projPos), shadowDist);
            }
            
            //  Make "near" shadow plane
            var nearShadowPlanePoint = cameraPos + lightDir * shadowDist;
            nearShadowPlane = MathUtils.BuildPlaneFromNormalAndPoint(-lightDir, nearShadowPlanePoint);
            rv.Add(nearShadowPlane);
        }
        else
        {
            //  Skip entire calculation if light sphere is not visible or light inside view frustum
            var distanceToLight = frustum.GetMinimumDistanceToPoint(l.posOrDir.xyz);
            if (distanceToLight < -l.range || distanceToLight >= 0)
            {
            #if (RUKHANKA_DEBUG_INFO && ! RUKHANKA_NO_DEBUG_DRAWER)
                if (actx.drawCullingVolumes)
                    frustum.DrawWireOutline(dd, actx.cullingVolumeColor);
            #endif
                
                return frustum.planes;
            }
        }
        
        for (var i = 0; i < FrustumVolume.frustumEdgeIndices.Length; ++i)
        {
            var fep = FrustumVolume.frustumEdgeIndices[i]; 
            var ep = fep.zw;
            var pts = fep.xy;
            
            var p0 = frustum.planes[ep.x];
            var p1 = frustum.planes[ep.y];
            
            var d0 = math.dot(p0, l.posOrDir);
            var d1 = math.dot(p1, l.posOrDir);
            
            //  This is a silhouette edge
            if (d0 * d1 < 0)
            {
                var pt0 = frustum.points[pts.x];
                var pt1 = frustum.points[pts.y];
                
                var planeNormal = math.cross(l.posOrDir.xyz - pt0 * l.posOrDir.w, pt1 - pt0);
                
                var m = math.lengthsq(planeNormal);
                if (m > math.EPSILON)
                {
                    planeNormal *= math.rcp(math.sqrt(m));
                    if (math.dot(frustum.middlePoint - pt0, planeNormal) < 0)
                        planeNormal *= -1;
                    var d = -math.dot(planeNormal, pt0);
                    var plane = new float4(planeNormal, d);
                    rv.Add(plane);
                }
                
            #if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
                if (actx.drawCullingVolumes)
                {
                    if (l.posOrDir.w == 0)
                    {
                        float3 ip0 = 0;
                        MathUtils.PlaneLineIntersection(pt0, lightDir, nearShadowPlane, ref ip0);
                        
                        float3 ip1 = 0;
                        MathUtils.PlaneLineIntersection(pt1, lightDir, nearShadowPlane, ref ip1);
                        
                        dd.DrawLine(ip0, pt0, actx.cullingVolumeColor);
                        dd.DrawLine(pt0, pt1, actx.cullingVolumeColor);
                        dd.DrawLine(pt1, ip1, actx.cullingVolumeColor);
                        dd.DrawLine(ip1, ip0, actx.cullingVolumeColor);
                    }
                    else
                    {
                        dd.DrawLine(pt0, pt1, actx.cullingVolumeColor);
                        dd.DrawLine(l.posOrDir.xyz, pt1, actx.cullingVolumeColor);
                        dd.DrawLine(l.posOrDir.xyz, pt0, actx.cullingVolumeColor);
                    }
                }
           #endif 
            }
        #if (RUKHANKA_DEBUG_INFO && ! RUKHANKA_NO_DEBUG_DRAWER)
            else if (d0 > 0 && actx.drawCullingVolumes)
            {
                //  Draw edge with light facing planes
                var pt0 = frustum.points[pts.x];
                var pt1 = frustum.points[pts.y];
                dd.DrawLine(pt0, pt1, actx.cullingVolumeColor);
            }
        #endif
        }
        
        AddExistingLightFacingEdges(l.posOrDir, frustum.planes, ref rv);
        
        return rv.AsArray();
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void AddExistingLightFacingEdges(float4 lightPos, NativeArray<float4> planes, ref NativeList<float4> outPlanes)
    {
        for (var i = 0; i < planes.Length; ++i)
        {
            var pln = planes[i];
            if (math.dot(pln, lightPos) > 0)
            {
                outPlanes.Add(pln);
            }
        }
    }
}
}
}
