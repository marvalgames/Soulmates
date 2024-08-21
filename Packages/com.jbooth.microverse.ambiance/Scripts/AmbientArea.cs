using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Splines;
//using Unity.Burst;
//using Unity.Jobs;

namespace JBooth.MicroVerseCore
{
    [ExecuteAlways]
    public partial class AmbientArea : MonoBehaviour
    {
        public enum AmbianceFalloff
        {
            Global,
            Box,
            Range,
            Spline,
            SplineArea,
#if __MICROVERSE_MASKS__
            SDFMask
#endif
        }

        [Tooltip("Ambient sound configuration for area")]
        public Ambient ambient;
        [Range(0,1)]
        public float volume = 1;
        public AmbianceFalloff falloff = AmbianceFalloff.Range;
        public Vector2 falloffParams = new Vector2(0.8f, 1.0f);
        public SplineContainer spline = null;
        public Vector2 worldHeightRange;
        public Vector2 worldHeightFalloff;
        List<NativeSpline> nativeSplines;
        List<NativeArray<float3>> nativeSplineLut;
        List<Bounds> nativeSplineBounds;
        bool nativeSplineAlloc = false;
        bool nativeSplineLutAlloc = false;
        bool nativeSplineBoundsAlloc = false;
        AmbientState ambientState;
        ClipPlayer clipPlayer;

#if __MICROVERSE_MASKS__
        public MaskTarget maskTarget;
        Terrain[] terrains;
#endif

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                AmbianceMgr.EnsureExists();
                AmbianceMgr.RegisterArea(this);
                if (ambient != null && ambient.randomSounds.Length > 0)
                {
                    ambientState = new AmbientState(ambient);
                }
                if (ambient != null && ambient.backgroundLoops.Length > 0)
                {
                    clipPlayer = new ClipPlayer(ambient.backgroundLoops, ambient.outputGroup, ClipPlayer.PlayOrder.Random);
                }
                if (falloff == AmbianceFalloff.Spline && spline != null)
                {
                    nativeSplines = new List<NativeSpline>(spline.Splines.Count);
                    nativeSplineBounds = new List<Bounds>(spline.Splines.Count);

                    foreach (var sourceSpline in spline.Splines)
                    {
                        var nativeSpline = new NativeSpline(sourceSpline, spline.transform.localToWorldMatrix, Unity.Collections.Allocator.Persistent);
                        nativeSplines.Add(nativeSpline);
                        var bounds = sourceSpline.GetBounds();
                        bounds.center = spline.transform.localToWorldMatrix.MultiplyPoint(bounds.center);
                        bounds.Expand(falloffParams.x + falloffParams.y);
                        nativeSplineBounds.Add(bounds);
                    }
                    nativeSplineAlloc = true;
                    nativeSplineBoundsAlloc = true;
                }
                else if (falloff == AmbianceFalloff.SplineArea && spline != null)
                {
                    // spline areas are treated as 2d, since there's not a great way
                    // to ramp volume to them smoothly as 3d, since the closest point
                    // can swap based on the position of the listener.
                    // So, we reposition the spline points such that they end up at
                    // 0 on Y in world space, then do the same for the listener.
                    //float wpy = -spline.transform.position.y;
                    nativeSplineLut = new List<NativeArray<float3>>(spline.Splines.Count);
                    nativeSplineBounds = new List<Bounds>(spline.Splines.Count);
                    foreach (var sourceSpline in spline.Splines)
                    {
                        var sp = new Spline(sourceSpline);
                        int length = sourceSpline.Count * 15;
                        NativeArray<float3> lut = new NativeArray<float3>(length, Allocator.Persistent);
                        for (int i = 0; i < length; ++i)
                        {
                            var pos = sourceSpline.EvaluatePosition((float)i / length);
                            pos = spline.transform.localToWorldMatrix.MultiplyPoint(pos);
                            pos.y = 0;
                            lut[i] = pos;
                        }
                        nativeSplineLut.Add(lut);
                        var bounds = sourceSpline.GetBounds();
                        bounds.center = spline.transform.localToWorldMatrix.MultiplyPoint(bounds.center);
                        var center = bounds.center;
                        center.y = 0;
                        bounds.center = center;
                        var size = bounds.size;
                        size.y = 99999;
                        bounds.size = size;
                        bounds.Expand(falloffParams.x + falloffParams.y);
                        nativeSplineBounds.Add(bounds);
                    }
                    nativeSplineLutAlloc = true;
                    nativeSplineBoundsAlloc = true;
                }
#if __MICROVERSE_MASKS__
                else if (falloff == AmbianceFalloff.SDFMask && maskTarget != null)
                {
                    terrains = FindObjectsOfType<Terrain>();
                }
#endif
            }
#if UNITY_EDITOR
            Camera.onPreCull -= DrawWithCamera;
            Camera.onPreCull += DrawWithCamera;
#endif
        }


        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                if (clipPlayer != null)
                {
                    clipPlayer.UpdatePlayer(0);
                }

                AmbianceMgr.UnregisterArea(this);
                ambientState = null;
                clipPlayer = null;

                if (nativeSplineAlloc)
                {
                    nativeSplineAlloc = false;
                    foreach (var nativeSpline in nativeSplines)
                    {
                        nativeSpline.Dispose();
                    }
                    nativeSplines.Clear();
                }
                if (nativeSplineLutAlloc)
                {
                    nativeSplineLutAlloc = false;
                    foreach (var nl in nativeSplineLut)
                    {
                        nl.Dispose();
                    }
                    nativeSplineLut.Clear();
                }
                if (nativeSplineBoundsAlloc)
                {
                    nativeSplineBounds.Clear();
                }
            }
#if UNITY_EDITOR
            Camera.onPreCull -= DrawWithCamera;
#endif
        }


        float IsPointInsideSpline(Vector3 point, NativeArray<float3> points)
        {
            int vertexCount = points.Length;
            bool inside = false;
            Vector3 sp0 = points[0];
            float minDistance = math.distance(point, sp0);

            for (int i = 1; i < vertexCount; ++i)
            {
                Vector3 sp1 = points[i];
                float d = math.distance(point, sp1);
                if (d < minDistance)
                    minDistance = d;

                if (((sp0.z > point.z) != (sp1.z > point.z)) &&
                    (point.x < (sp1.x - sp0.x) * (point.z - sp0.z) /
                    (sp1.z - sp0.z) + sp0.x))
                {
                    inside = !inside;
                }
                sp0 = sp1;
            }

            return minDistance * ((inside == true) ? -1 : 1);
        }



#if UNITY_EDITOR
        static Mesh cube;
        static Mesh sphere;
        static Material mat;
        static int _Color = Shader.PropertyToID("_Color");
        private void DrawWithCamera(Camera camera)
        {
            if (UnityEditor.Selection.activeGameObject != gameObject)
                return;
            if (camera && Application.isPlaying == false)
            {
                if (cube == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube = go.GetComponent<MeshFilter>().sharedMesh;
                    DestroyImmediate(go);
                }
                if (sphere == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere = go.GetComponent<MeshFilter>().sharedMesh;
                    DestroyImmediate(go);
                }
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Hidden/AreaPreview"));
                }
                if (falloff == AmbianceFalloff.Box)
                {
                    var mtx = transform.localToWorldMatrix;
                    mat.SetColor(_Color, MicroVerse.instance.options.colors.ambientAreaColor * new Color(1, 1, 1, 0.5f));
                    Graphics.DrawMesh(cube, mtx, mat, gameObject.layer, camera);
                    mtx *= Matrix4x4.Scale(new Vector3(falloffParams.x, falloffParams.x, falloffParams.x));
                    Graphics.DrawMesh(cube, mtx, mat, gameObject.layer, camera);
                }
                else if (falloff == AmbianceFalloff.Range)
                {
                    var mtx = transform.localToWorldMatrix;
                    mat.SetColor(_Color, MicroVerse.instance.options.colors.ambientAreaColor * new Color(1, 1, 1, 0.5f));
                    Graphics.DrawMesh(sphere, mtx, mat, gameObject.layer, camera);
                    mtx *= Matrix4x4.Scale(new Vector3(falloffParams.x, falloffParams.x, falloffParams.x));
                    Graphics.DrawMesh(sphere, mtx, mat, gameObject.layer, camera);
                }
            }
        }

#endif

        float GetFalloff(Vector3 worldPos)
        {
            switch (falloff)
            {
                case AmbianceFalloff.Global:
                    return volume;
                case AmbianceFalloff.Box:
                    {
                        float3 objPos = transform.worldToLocalMatrix.MultiplyPoint(worldPos);
                        objPos = math.saturate(math.abs(objPos));
                        float dist = math.length(objPos + math.max(objPos.x, math.max(objPos.y, objPos.z)));
                        float v = 1.0f - math.saturate((dist - falloffParams.x) / math.max(0.01f, 1.0f - falloffParams.x));
                        v = math.smoothstep(0, 1, v);
                        return v * volume;
                    }
                case AmbianceFalloff.Range:
                    {
                        Vector3 objPos = transform.worldToLocalMatrix.MultiplyPoint(worldPos);
                        float dist = math.distance(objPos, Vector3.zero) * 2;
                        float v = 1.0f - math.saturate((dist - falloffParams.x) / math.max(0.001f, 1.0f - falloffParams.x));
                        v = math.smoothstep(0, 1, v);
                        return v * volume;
                    }
                case AmbianceFalloff.Spline:
                    {
                        if (nativeSplineAlloc)
                        {
                            float localVolume = 0;
                            for (int x = 0; x < nativeSplines.Count; ++x)
                            {
                                Bounds b = nativeSplineBounds[x];

                                if (b.Contains(worldPos))
                                {
                                    var nativeSpline = nativeSplines[x];
                                    float d = SplineUtility.GetNearestPoint(nativeSpline, worldPos, out float3 p, out float t);

                                    if (d <= falloffParams.x)
                                    {
                                        return volume;
                                    }
                                    else if (falloffParams.y > 0 && d <= falloffParams.x + falloffParams.y)
                                    {
                                        float nv = 1.0f - math.saturate((d - falloffParams.x) / math.max(0.001f, falloffParams.y - falloffParams.x));
                                        if (nv > localVolume)
                                            localVolume = nv;
                                    }
                                }
                            }
                            return localVolume * volume;
                        }
                        return 0;
                    }
                case AmbianceFalloff.SplineArea:
                    {
                        if (nativeSplineLutAlloc)
                        {
                            // make our position 2d.
                            Vector3 worldPos2D = worldPos;
                            worldPos2D.y = 0;
                            float localVolume = 0;

                            for (int x = 0; x < nativeSplineLut.Count; ++x)
                            {
                                Bounds b = nativeSplineBounds[x];

                                if (b.Contains(worldPos2D))
                                {
                                    
                                    float hweight = 0;
                                    if (worldPos.y < worldHeightRange.x)
                                    {
                                        hweight = math.saturate((worldPos.y - math.abs(worldHeightRange.x - worldHeightFalloff.x)) / math.max(0.001f, worldHeightFalloff.x));
                                    }
                                    else if (worldPos.y > worldHeightRange.y)
                                    {
                                        hweight = 1.0f - math.saturate((worldPos.y - math.abs(worldHeightRange.y + worldHeightFalloff.y)) / math.max(0.001f, worldHeightFalloff.y));
                                    }
                                    else
                                    {
                                        hweight = 1;
                                    }
                                    if (worldHeightRange.x == 0 && worldHeightRange.y == 0)
                                    {
                                        hweight = 1;
                                    }
                                    

                                    if (hweight <= 0)
                                        return 0;

                                    float d = IsPointInsideSpline(worldPos2D, nativeSplineLut[x]);
                                    if (d < 0)
                                    {
                                        return volume;
                                    }

                                    float nv = hweight * (1.0f - math.saturate((d - falloffParams.x) / math.max(0.001f, falloffParams.y - falloffParams.x)));
                                    if (nv > localVolume)
                                        localVolume = nv;
                                }
                            }

                            return localVolume * volume;
                        }
                        return 0;
                    }
#if __MICROVERSE_MASKS__
                case AmbianceFalloff.SDFMask:
                {
                    if (maskTarget != null)
                    {
                        foreach (var t in terrains)
                        {
                            Bounds b = TerrainUtil.ComputeTerrainBounds(t);
                            if (b.Contains(worldPos))
                            {
                                float hweight = 0;
                                if (worldPos.y < worldHeightRange.x)
                                {
                                    hweight = math.saturate((worldPos.y - math.abs(worldHeightRange.x - worldHeightFalloff.x)) / math.max(0.001f, worldHeightFalloff.x));
                                }
                                else if (worldPos.y > worldHeightRange.y)
                                {
                                    hweight = 1.0f - math.saturate((worldPos.y - math.abs(worldHeightRange.y + worldHeightFalloff.y)) / math.max(0.001f, worldHeightFalloff.y));
                                }
                                else
                                {
                                    hweight = 1;
                                }

                                if (hweight <= 0)
                                    return 0;

                                foreach (var tex in maskTarget.textures)
                                {
                                    if (tex.terrainData == t.terrainData)
                                    {
                                        if (tex.texture == null)
                                            return 0;
                                        worldPos -= t.transform.position;
                                        worldPos.x /= t.terrainData.size.x;
                                        worldPos.z /= t.terrainData.size.z;
                                        var d = tex.texture.GetPixelBilinear(worldPos.x, worldPos.z).r;
                                        d *= t.terrainData.size.x / tex.texture.width * 255;
                                        return hweight * (1.0f - math.saturate((d - falloffParams.x) / math.max(0.001f, falloffParams.y - falloffParams.x))) * volume;
                                    }
                                }
                            }
                        }
                    }
                    return 0;
                }
#endif
            }

            return 0;
        }

        internal float audioChance;

        internal void UpdateArea(Vector3 listenerPos)
        {
            audioChance = GetFalloff(listenerPos);
            if (ambientState != null)
            {
                ambientState.UpdateState(this, listenerPos);
            }
            if (clipPlayer != null)
            {
                clipPlayer.UpdatePlayer(audioChance * ambient.backgroundVolume * AmbianceMgr.ambientLevel);
            }
        }
    }
}