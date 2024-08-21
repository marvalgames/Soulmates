using UnityEngine;
using System.Collections.Generic;

namespace JBooth.MicroVerseCore
{
    [ExecuteAlways]
    public class HeightAreaEffectStamp : Stamp, IHeightModifier, IModifier
    {
        public enum EffectType
        {
            Terrace,
            Beach,
            RemapCurve,
            Noise,
        }

        [Tooltip("Effect to apply")]
        public EffectType effectType = EffectType.Terrace;
        public FalloffFilter falloff = new FalloffFilter();
        public Noise noise = new Noise();
        [Tooltip("How the noise should be combined with the existing height data")]
        public HeightStamp.CombineMode combineMode = HeightStamp.CombineMode.Add;
        [Range(0,1)] public float combineBlend = 0;

        [Tooltip("How high each terrace should be")]
        [Range(0.05f, 20)] public float terraceSize = 1;
        [Tooltip("How sharp the terrace should be")]
        [Range(0.0f, 1.0f)]public float terraceStrength = 1;
        [Tooltip("The effect will only be present around the stamps world Y value by this many meters")]
        [Range(0.05f, 100)] public float beachDistance = 5;
        [Tooltip("Allows you to control the curve of adjustment")]
        [Range(0.25f, 4)] public float beachPower = 1;
        public AnimationCurve remapCurve = AnimationCurve.Linear(0, 0, 1, 1);


        Material material;

        public Texture2D remapCurveTex;


        public void Dispose()
        {

        }

        protected override void OnDestroy()
        {
            DestroyImmediate(material);
            base.OnDestroy();
        }

        static Shader heightmapShader = null;

        public void Initialize()
        {
            if (heightmapShader == null)
            {
                heightmapShader = Shader.Find("Hidden/MicroVerse/HeightAreaEffectStamp");
            }
            if (material == null)
            {
                material = new Material(heightmapShader);
            }
            if (effectType == EffectType.RemapCurve)
            {
                if (remapCurveTex == null)
                {
                    remapCurveTex = new Texture2D(256, 1, TextureFormat.R16, false);
                    remapCurveTex.wrapMode = TextureWrapMode.Clamp;
                    remapCurveTex.filterMode = FilterMode.Bilinear;
                    remapCurveTex.hideFlags = HideFlags.HideAndDontSave;
                    for (int i = 0; i < 256; ++i)
                    {
                        remapCurveTex.SetPixel(i, 0, new Color(remapCurve.Evaluate((float)i / 256), 0, 0, 1));
                    }
                    remapCurveTex.Apply(false, false);
                }
            }
            
        }

        public override Bounds GetBounds()
        {
            FalloffOverride fo = GetComponentInParent<FalloffOverride>();
            var foType = falloff.filterType;
            var foFilter = falloff;
            if (fo != null && fo.enabled)
            {
                foType = fo.filter.filterType;
                foFilter = fo.filter;
            }
#if __MICROVERSE_SPLINES__
            if (foType == FalloffFilter.FilterType.SplineArea && foFilter.splineArea != null)
            {
                return foFilter.splineArea.GetBounds();
            }
#endif
            if (foType == FalloffFilter.FilterType.Global && foFilter.paintArea != null && foFilter.paintArea.clampOutsideOfBounds)
            {
                return foFilter.paintArea.GetBounds();
            }
            return TerrainUtil.GetBounds(transform);
        }

        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od)
        {
            keywordBuilder.Clear();
            falloff.PrepareMaterial(material, transform, keywordBuilder.keywords);
            falloff.PrepareTerrain(material, heightmapData.terrain, transform, keywordBuilder.keywords);
            switch (effectType)
            {
                case EffectType.Terrace:
                    keywordBuilder.Add("_TERRACE");
                    material.SetFloat(_TerraceSize, terraceSize);
                    material.SetFloat(_TerraceStrength, terraceStrength);
                    break;
                case EffectType.Beach:
                    keywordBuilder.Add("_BEACH");
                    material.SetFloat(_BeachDistance, beachDistance);
                    material.SetFloat(_BeachPower, beachPower);
                    break;
                case EffectType.RemapCurve:
                    keywordBuilder.Add("_REMAP");
                    material.SetTexture(_RemapCurve, remapCurveTex);
                    break;
                case EffectType.Noise:
                    noise.PrepareMaterial(material, "_NOISE", "_Noise", keywordBuilder.keywords);
                    material.SetFloat(_CombineMode, (int)combineMode);
                    material.SetFloat(_CombineBlend, combineBlend);
                    break;
            }

            material.SetFloat(_WorldPosY, transform.position.y);

            material.SetMatrix(_Transform, TerrainUtil.ComputeStampMatrix(heightmapData.terrain, transform, true));
            material.SetVector(_RealSize, TerrainUtil.ComputeTerrainSize(heightmapData.terrain));

            var noisePos = heightmapData.terrain.transform.position;
            noisePos.x /= heightmapData.terrain.terrainData.size.x;
            noisePos.z /= heightmapData.terrain.terrainData.size.z;
            material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, GetTerrainScalingFactor(heightmapData.terrain)));
            
            keywordBuilder.Assign(material);
            Graphics.Blit(source, dest, material);
            return true;
        }

        static int _Transform = Shader.PropertyToID("_Transform");
        static int _RealSize = Shader.PropertyToID("_RealSize");
        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");
        static int _TerraceSize = Shader.PropertyToID("_TerraceSize");
        static int _BeachDistance = Shader.PropertyToID("_BeachDistance");
        static int _WorldPosY = Shader.PropertyToID("_WorldPosY");
        static int _BeachPower = Shader.PropertyToID("_BeachPower");
        static int _RemapCurve = Shader.PropertyToID("_RemapCurve");
        static int _CombineMode = Shader.PropertyToID("_CombineMode");
        static int _CombineBlend = Shader.PropertyToID("_CombineBlend");
        static int _TerraceStrength = Shader.PropertyToID("_TerraceStrength");

        void OnDrawGizmosSelected()
        {
            if (MicroVerse.instance != null)
            {
                Gizmos.color = MicroVerse.instance.options.colors.heightStampColor;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
            }
        }
    }
}