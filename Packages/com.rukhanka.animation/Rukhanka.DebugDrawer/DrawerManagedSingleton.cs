
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.DebugDrawer
{
public class DrawerManagedSingleton: IComponentData
{
    internal DrawBuffer<LineData> linesBuf;
    internal DrawBuffer<ThickLineData> thickLinesBuf;
    internal DrawBuffer<TriangleData> trianglesBuf;
    internal DrawBuffer<BoneData> bonesBuf;

    internal NativeArray<LineData> lineData;
    internal NativeArray<TriangleData> triData;
    internal NativeArray<ThickLineData> thickLineData;
    internal NativeArray<BoneData> boneData;
    
    Material lineDrawMat;
    Material thickLinesDrawMat;
    Material trianglesDrawMat;
    Material boneTriDrawMat;
    Material boneOutlineDrawMat;

    Mesh boneMesh;

    internal struct LineData
    {
        public float3 p0, p1;
        public uint color;
    }

    internal struct ThickLineData
    {
        public float3 p0, p1;
        public float thickness;
        public uint color;
    }
    
    internal struct TriangleData
    {
        public float3 p0, p1, p2;
        public uint color;
    }
    
    internal struct BoneData
    {
		public float3 pos0, pos1;
		public uint colorTri, colorLines;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose()
    {
        linesBuf.Dispose();
        thickLinesBuf.Dispose();
        trianglesBuf.Dispose();
        bonesBuf.Dispose();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DrawerManagedSingleton()
    {
	    if (!SystemInfo.supportsComputeShaders)
	    {
		    Debug.LogError("System does not support compute shaders and/or graphics buffers. DebugDrawer will be disabled.");
		    return;
	    }
	    
        linesBuf = new ();
        thickLinesBuf = new ();
        trianglesBuf = new ();
        bonesBuf = new ();

        CreateMaterials();
        CreateBoneMesh();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public bool IsValid()
	{
		var rv =
			linesBuf != null &&
			thickLinesBuf != null &&
			trianglesBuf != null &&
			bonesBuf != null &&

			lineDrawMat != null &&
			thickLinesDrawMat != null &&
			trianglesDrawMat != null &&
			boneOutlineDrawMat != null &&
			boneTriDrawMat != null;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateMaterials()
	{
	#if HDRP_10_0_0_OR_NEWER
		var srpName = "HDRP";
	#elif URP_10_0_0_OR_NEWER
		var srpName = "URP";
	#else
		Debug.LogError("No scriptable renderer pipeline found. Please install HDRP or URP.");
		return;
	#endif
		
        lineDrawMat = new Material(Shader.Find($"RukhankaDebugLineDrawer {srpName}"));
        thickLinesDrawMat = new Material(Shader.Find($"RukhankaDebugThickLineDrawer {srpName}"));
        trianglesDrawMat = new Material(Shader.Find($"RukhankaDebugTriangleDrawer {srpName}"));
        boneTriDrawMat = new Material(Shader.Find($"RukhankaBoneTriangleRenderer {srpName}"));
        boneOutlineDrawMat = new Material(Shader.Find($"RukhankaBoneOutlineRenderer {srpName}"));
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBoneMesh()
	{
		boneMesh = new Mesh();
		boneMesh.subMeshCount = 2;

		var vtx = new Vector3[6];
		vtx[0] = new Vector3(0, 1, 0);
		vtx[5] = new Vector3(0, -1, 0);
		vtx[1] = new Vector3(-1, 0, 0);
		vtx[2] = new Vector3(1, 0, 0);
		vtx[3] = new Vector3(0, 0, -1);
		vtx[4] = new Vector3(0, 0, 1);

		for (int i = 0; i < vtx.Length; ++i)
			vtx[i] *= 0.1f;

		var triIdx = new int[]
		{
			0, 1, 4,
			0, 4, 2,
			0, 2, 3,
			0, 3, 1,

			5, 4, 1,
			5, 2, 4,
			5, 3, 2,
			5, 1, 3,
		};

		var lineIdx = new int[]
		{
			0, 1,
			0, 2, 
			0, 3,
			0, 4,
			5, 1,
			5, 2, 
			5, 3,
			5, 4,
			2, 4,
			1, 4,
			1, 3,
			2, 3,
		};

		boneMesh.SetVertices(vtx);
		boneMesh.SetIndices(triIdx, MeshTopology.Triangles, 0);
		boneMesh.SetIndices(lineIdx, MeshTopology.Lines, 1);
	}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void BeginFrame()
    {
        lineData = linesBuf.BeginFrame();
        thickLineData = thickLinesBuf.BeginFrame();
        triData = trianglesBuf.BeginFrame();
        boneData = bonesBuf.BeginFrame();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //  Render primitives
    public void EndFrame()
    {
	    var numLines = linesBuf.EndFrame();
        lineData = default;
        var numThickLines = thickLinesBuf.EndFrame();
        thickLineData = default;
        var numTriangles = trianglesBuf.EndFrame();
        triData = default;
        var numBones = bonesBuf.EndFrame();
        boneData = default;
        
        var rp = new RenderParams();
        rp.camera = null;
        rp.layer = 0;
        rp.lightProbeProxyVolume = null;
        rp.lightProbeUsage = LightProbeUsage.Off;
        rp.matProps = null;
        rp.motionVectorMode = MotionVectorGenerationMode.ForceNoMotion;
        rp.receiveShadows = false;
        rp.reflectionProbeUsage = ReflectionProbeUsage.Off;
        rp.rendererPriority = 0;
        rp.renderingLayerMask = 0xffffffff;
        rp.shadowCastingMode = ShadowCastingMode.Off;
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000);

        if (numLines > 0)
        {
			rp.material = lineDrawMat;
			rp.material.SetBuffer("lineDataBuf", linesBuf.gpuBuffer);
			Graphics.RenderPrimitives(rp, MeshTopology.Lines, linesBuf.counter * 2);
        }

        if (numThickLines > 0)
        {
	        rp.material = thickLinesDrawMat;
	        rp.material.SetBuffer("thickLineDataBuf", thickLinesBuf.gpuBuffer);
	        Graphics.RenderPrimitives(rp, MeshTopology.Triangles, thickLinesBuf.counter * 6);
        }

        if (numTriangles > 0)
        {
	        rp.material = trianglesDrawMat;
	        rp.material.SetBuffer("triDataBuf", trianglesBuf.gpuBuffer);
	        Graphics.RenderPrimitives(rp, MeshTopology.Triangles, trianglesBuf.counter * 3);
        }

        if (numBones > 0)
        {
	        rp.material = boneTriDrawMat;
	        rp.material.SetBuffer("boneDataBuf", bonesBuf.gpuBuffer);
	        Graphics.RenderMeshPrimitives(rp, boneMesh, 0, bonesBuf.counter);
	        
	        rp.material = boneOutlineDrawMat;
	        rp.material.SetBuffer("boneDataBuf", bonesBuf.gpuBuffer);
	        Graphics.RenderMeshPrimitives(rp, boneMesh, 1, bonesBuf.counter);
        }
    }
}
}
