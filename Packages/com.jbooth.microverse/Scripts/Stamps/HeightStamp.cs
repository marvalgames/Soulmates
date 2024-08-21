using UnityEngine;
using System.Collections.Generic;

namespace JBooth.MicroVerseCore
{
    [ExecuteAlways]
    public class HeightStamp : Stamp, IHeightModifier, IModifier
    {
        public enum CombineMode
        {
            Override = 0,
            Max = 1,
            Min = 2,
            Add = 3,
            Subtract = 4,
            Multiply = 5,
            Average = 6,
            Difference = 7,
            SqrtMultiply = 8,
            Blend = 9,
        }

        public Texture2D stamp;
        public CombineMode mode = CombineMode.Max;

        public FalloffFilter falloff = new FalloffFilter();

        [Tooltip("Twists the stamp around the Y axis")]
        [Range(-90, 90)] public float twist = 0;
        [Tooltip("Erodes the slopes of the terrain")]
        [Range(0, 600)] public float erosion = 0;
        [Tooltip("Controls the scale of the erosion effect")]
        [Range(1, 90)] public float erosionSize = 4;

        [Tooltip("Bends the heights towards the top or bottom")]
        [Range(0.1f, 8.0f)] public float power = 1;
        [Tooltip("Invert the height map")]
        public bool invert;

        public bool useHeightRemap;
        public AnimationCurve remapCurve = AnimationCurve.Linear(0, 0, 1, 1);
        Texture2D remapCurveTex;
        public void ClearRemapCurve() { if (remapCurveTex != null) DestroyImmediate(remapCurveTex);  }

        [Tooltip("Blend between existing height map and new one")]
        [Range(0, 1)] public float blend = 1;

        public Vector2 remapRange = new Vector2(0, 1);
        public Vector4 scaleOffset = new Vector4(1, 1, 0, 0);
        [Range(-1, 1)] public float tiltX = 0;
        [Range(-1, 1)] public float tiltZ = 0;
        public bool tiltScaleX = false;
        public bool tiltScaleZ = false;
        [Range(0, 6)] public float mipBias = 0;

        Material material;

        public void Dispose()
        {
            
        }

        protected override void OnDestroy()
        {
            DestroyImmediate(material);
            base.OnDestroy();
        }

        [SerializeField] int version = 0;
        public override void OnEnable()
        {
            if (version == 0 && mode == CombineMode.Max)
            {
                var pos = transform.position;
                pos.y = 0;
                transform.position = pos;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
            }
            else if (version == 1 && mode != HeightStamp.CombineMode.Override && mode != HeightStamp.CombineMode.Max)
            {
                var pos = transform.position;
                pos.y = 0;
                transform.position = pos;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
            }
            base.OnEnable();
            version = 2;
        }

        static Shader heightmapShader = null;

        public void Initialize()
        {
            if (stamp != null)
            {
                stamp.wrapMode = TextureWrapMode.Clamp;
            }
            if (heightmapShader == null)
            {
                heightmapShader = Shader.Find("Hidden/MicroVerse/HeightmapStamp");
            }
            if (material == null)
            {
                material = new Material(heightmapShader);
            }
            if (useHeightRemap && remapCurveTex == null)
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

        static int _AlphaMapSize = Shader.PropertyToID("_AlphaMapSize");
        static int _PlacementMask = Shader.PropertyToID("_PlacementMask");
        static int _NoiseUV = Shader.PropertyToID("_NoiseUV");
        static int _Invert = Shader.PropertyToID("_Invert");
        static int _Blend = Shader.PropertyToID("_Blend");
        static int _Power = Shader.PropertyToID("_Power");
        static int _Tilt = Shader.PropertyToID("_Tilt");
        static int _TiltScale = Shader.PropertyToID("_TiltScale");

        // used by copy paste stamp
        public bool ApplyHeightStampAbsolute(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od, Vector2 heightRenorm)
        {
            material.SetVector("_HeightRenorm", heightRenorm);
            keywordBuilder.Clear();
            keywordBuilder.Add("_PASTESTAMP");
            keywordBuilder.Add("_ABSOLUTEHEIGHT");
            PrepareMaterial(material, heightmapData, keywordBuilder.keywords);
            material.SetFloat(_AlphaMapSize, source.width);
            material.SetTexture(_PlacementMask, od.terrainMask);
            var noisePos = heightmapData.terrain.transform.position;
            noisePos.x /= heightmapData.terrain.terrainData.size.x;
            noisePos.z /= heightmapData.terrain.terrainData.size.z;
            material.SetFloat(_Power, 1);
            material.SetFloat(_Blend, 1);
            material.SetFloat(_Invert, 0);

            material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, GetTerrainScalingFactor(heightmapData.terrain)));

            keywordBuilder.Assign(material);
            
            // this is only called via the copy paste stamp, and when passing true we get an error with
            // a small offset, so we pretend it's not a height stamp and pass false.
            material.SetMatrix(_Transform, TerrainUtil.ComputeStampMatrix(heightmapData.terrain, transform, false));
            Graphics.Blit(source, dest, material);
            return true;
        }

        public bool ApplyHeightStamp(RenderTexture source, RenderTexture dest, HeightmapData heightmapData, OcclusionData od)
        {
            keywordBuilder.Clear();
            PrepareMaterial(material, heightmapData, keywordBuilder.keywords);
            material.SetFloat(_AlphaMapSize, source.width);
            material.SetTexture(_PlacementMask, od.terrainMask);
            var noisePos = heightmapData.terrain.transform.position;
            noisePos.x /= heightmapData.terrain.terrainData.size.x;
            noisePos.z /= heightmapData.terrain.terrainData.size.z;

            material.SetVector(_NoiseUV, new Vector3(noisePos.x, noisePos.z, GetTerrainScalingFactor(heightmapData.terrain)));
            material.SetFloat(_Power, power);
            material.SetFloat(_Blend, blend);
            material.SetFloat(_Invert, invert ? 1.0f : 0.0f);

            material.SetVector(_TiltScale, new Vector2(tiltScaleX ? 1 : 0, tiltScaleZ ? 1 : 0));
            material.SetVector(_Tilt, new Vector3(tiltX, 0, tiltZ));

            if (power != 1.0f || tiltX != 0 || tiltZ != 0)
            {
                keywordBuilder.Add("_USEPOWORTILT");
            }
            
            keywordBuilder.Assign(material);
            Graphics.Blit(source, dest, material);
            return true;
        }

        static int _Transform = Shader.PropertyToID("_Transform");
        static int _RealSize = Shader.PropertyToID("_RealSize");
        static int _StampTex = Shader.PropertyToID("_StampTex");
        static int _MipBias = Shader.PropertyToID("_MipBias");
        static int _RemapRange = Shader.PropertyToID("_RemapRange");
        static int _ScaleOffset = Shader.PropertyToID("_ScaleOffset");
        static int _HeightRemap = Shader.PropertyToID("_HeightRemap");
        static int _CombineMode = Shader.PropertyToID("_CombineMode");
        static int _Twist = Shader.PropertyToID("_Twist");
        static int _Erosion = Shader.PropertyToID("_Erosion");
        static int _ErosionSize = Shader.PropertyToID("_ErosionSize");
        static int _HeightRemapCurve = Shader.PropertyToID("_HeightRemapCurve");
        static int _CombineBlend = Shader.PropertyToID("_CombineBlend");

        void PrepareMaterial(Material material, HeightmapData heightmapData, List<string> keywords)
        {
            var localPosition = heightmapData.WorldToTerrainMatrix.MultiplyPoint3x4(transform.position);
            var size = transform.lossyScale;

            material.SetMatrix(_Transform, TerrainUtil.ComputeStampMatrix(heightmapData.terrain, transform, true));
            material.SetVector(_RealSize, TerrainUtil.ComputeTerrainSize(heightmapData.terrain));
            
            if (stamp != null)
            {
                stamp.wrapMode = (scaleOffset == new Vector4(1,1,0,0)) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            }
            material.SetTexture(_StampTex, stamp);
            material.SetFloat(_MipBias, mipBias);
            material.SetVector(_RemapRange, remapRange);
            material.SetVector(_ScaleOffset, scaleOffset);
            material.SetFloat(_CombineBlend, blend);
            falloff.PrepareTerrain(material, heightmapData.terrain, transform, keywords);
            falloff.PrepareMaterial(material, transform, keywords);

            

            var y = localPosition.y;

            material.SetVector(_HeightRemap, new Vector2(y, y + size.y) / heightmapData.RealHeight);
            material.SetInt(_CombineMode, (int)mode);

            if (twist != 0)
            {
                keywords.Add("_TWIST");
                material.SetFloat(_Twist, twist);
            }
            if (erosion != 0)
            {
                keywords.Add("_EROSION");
                material.SetFloat(_Erosion, erosion);
                material.SetFloat(_ErosionSize, erosionSize);
            }

            if (useHeightRemap)
            {
                keywords.Add("_USEHEIGHTREMAPCUVE");
                material.SetTexture(_HeightRemapCurve, remapCurveTex);
            }


        }

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