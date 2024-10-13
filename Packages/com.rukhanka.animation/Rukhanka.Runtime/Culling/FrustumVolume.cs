#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif

using Unity.Collections;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
internal struct FrustumVolume
{
    public NativeArray<float4> planes;
    public NativeArray<float3> points;
    public float3 middlePoint;
    
/////////////////////////////////////////////////////////////////////////////////

    //  Frustum corners in NDC
    static readonly float4[] frustumCorners = 
    {
        // Near plane
        new float4(-1, -1, -1, 1),
        new float4(-1, +1, -1, 1),
        new float4(+1, +1, -1, 1),
        new float4(+1, -1, -1, 1),
        // Far plane
        new float4(-1, -1, +1, 1),
        new float4(-1, +1, +1, 1),
        new float4(+1, +1, +1, 1),
        new float4(+1, -1, +1, 1),
    };
    
    // Point (xy) and plane (zw) indices used to form edges
    public static readonly int4[] frustumEdgeIndices =
    {
        // Sides
        new int4(2, 6, 0, 2),
        new int4(1, 5, 1, 2),
        new int4(0, 4, 1, 3),
        new int4(3, 7, 0, 3),
        
        // Near plane edges
        new int4(1, 2, 5, 2),
        new int4(2, 3, 5, 0),
        new int4(0, 3, 5, 3),
        new int4(0, 1, 5, 1),
        
        //  Far plane edges
        new int4(5, 6, 4, 2),
        new int4(6, 7, 4, 0),
        new int4(4, 7, 4, 3),
        new int4(4, 5, 4, 1),
    };
    
/////////////////////////////////////////////////////////////////////////////////

    public static FrustumVolume Allocate()
    {
        var rv = new FrustumVolume()
        {
            points = new (frustumCorners.Length, Allocator.Temp),
            planes = new (6, Allocator.Temp),
        };
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////
    
#if !RUKHANKA_NO_DEBUG_DRAWER
    public void DrawWireOutline(Drawer dd, uint color)
    {
        for (var j = 0; j < frustumEdgeIndices.Length; ++j)
        {
            var e = frustumEdgeIndices[j].xy;
            var p0 = points[e.x];
            var p1 = points[e.y];
            dd.DrawLine(p0, p1, color);
        }
    }
#endif
    
/////////////////////////////////////////////////////////////////////////////////

    public float GetMinimumDistanceToPoint(float3 pt)
    {
        var distance = float.MaxValue;
        var pt4 = new float4(pt, 1);
        foreach (var pln in planes)
        {
            var d = math.dot(pln, pt4);
            distance = math.min(distance, d);
        }
        return distance;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public static FrustumVolume MakeCullingCameraFrustum(AnimationCullingContextUpdateSystem.CullingCameraData ccd)
    {
        var rv = Allocate();
        
        var cullingMatrix = math.inverse(ccd.cullingMatrix);
        
        //  Build point list
        for (var i = 0; i < frustumCorners.Length; ++i)
        {
            var vtx = math.mul(cullingMatrix, frustumCorners[i]);
            vtx /= vtx.w;
            rv.points[i] = vtx.xyz;
        }
        
        var mp = math.mul(cullingMatrix, new float4(0, 0, 0.1f, 1));
        rv.middlePoint = mp.xyz / mp.w;
        
        //  Build planes
        // +X
        rv.planes[0] = MathUtils.BuildPlaneFromThreePoints(ccd.pos, rv.points[7], rv.points[6]);
        // -X
        rv.planes[1] = MathUtils.BuildPlaneFromThreePoints(ccd.pos, rv.points[5], rv.points[4]);
        // +Y
        rv.planes[2] = MathUtils.BuildPlaneFromThreePoints(ccd.pos, rv.points[6], rv.points[5]);
        // -Y
        rv.planes[3] = MathUtils.BuildPlaneFromThreePoints(ccd.pos, rv.points[4], rv.points[7]);
        // +Z
        rv.planes[4] = MathUtils.BuildPlaneFromNormalAndPoint(-ccd.viewDir, rv.points[5]);
        // -Z
        rv.planes[5] = MathUtils.BuildPlaneFromNormalAndPoint(ccd.viewDir, rv.points[0]);
        
        return rv;
    }
}
}
