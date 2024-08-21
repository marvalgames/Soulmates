
using UnityEngine;
using System.Collections.Generic;


#if UNITY_2021_3_OR_NEWER

using UnityEngine.Splines;

namespace JBooth.MicroVerseCore
{
    public class SplineArea : Stamp, IModifier
    {
        public SplineContainer spline;
        [Tooltip("This is the resolution of the signed distance field used to represent this spline")]
        public SplinePath.SDFRes sdfRes = SplinePath.SDFRes.k512;
        [Tooltip("This is the max distance for effects which use the spline to fall off. Because a spline area can be used by many different things, you have to set this on the area")]
        public float maxSDF = 128;

        public Noise positionNoise = new Noise();


        public bool NeedCurvatureMap() { return false; }

        Dictionary<Terrain, SplineRenderer> splineRenderers = new Dictionary<Terrain, SplineRenderer>();


        public override void OnEnable()
        {
            if (spline == null)
            {
                spline = GetComponent<SplineContainer>();
            }
        }

        void ClearSplineRenders()
        {
            foreach (var sr in splineRenderers.Values)
            {
                sr.Dispose();
            }
            splineRenderers.Clear();
        }

        SplineRenderer GetSplineRenderer(Terrain terrain)
        {
            var mode = spline.Spline.Closed ? SplineRenderer.RenderDesc.Mode.Area : SplineRenderer.RenderDesc.Mode.Path;
            if (splineRenderers.ContainsKey(terrain))
            {
                var sr = splineRenderers[terrain];
                if (sr.lastMaxSDF < maxSDF)
                {
                    sr.Render(spline, terrain, positionNoise, null, (int)sdfRes, maxSDF, mode);
                }
                else
                {
                    return sr;
                }
            }
            else
            {
                var terrainBounds = TerrainUtil.ComputeTerrainBounds(terrain);
                if (terrainBounds.Intersects(GetBounds()))
                {
                    SplineRenderer sr = new SplineRenderer();
                    bounds = new Bounds(Vector3.zero, Vector3.zero);
                    sr.Render(spline, terrain, positionNoise, null, (int)sdfRes, maxSDF, mode);
                    splineRenderers.Add(terrain, sr);
                    return sr;
                }
            }
            return null;
        }

        public void UpdateSplineSDFs()
        {
            ClearSplineRenders();
            if (spline == null)
                return;

            if (MicroVerse.instance == null)
                return;
            MicroVerse.instance.SyncTerrainList();
            foreach (var terrain in MicroVerse.instance.terrains)
            {
                GetSplineRenderer(terrain);
            }
        }

        public void Initialize()
        {
            
        }

        public RenderTexture GetSDF(Terrain t)
        {
            var sr = GetSplineRenderer(t);
            if (sr != null)
                return sr.splineSDF;
            return null;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ClearSplineRenders();
        }

        protected override void OnDestroy()
        {
            ClearSplineRenders();
            base.OnDestroy();
        }

        public void Dispose()
        {
        }

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        public override Bounds GetBounds()
        {
            if (bounds.center == Vector3.zero && bounds.size == Vector3.zero)
            {
                bounds = SplinePath.ComputeBounds(spline, Mathf.Max(maxSDF, positionNoise.amplitude * 0.5f));
            }
            return bounds;
        }

#if UNITY_EDITOR
        public override void OnMoved()
        {
            bounds = new Bounds();
            UpdateSplineSDFs();
            base.OnMoved();
        }
#endif

    }

}

#endif // 2022+