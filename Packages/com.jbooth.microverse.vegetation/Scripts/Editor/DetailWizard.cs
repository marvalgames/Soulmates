using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;


namespace JBooth.MicroVerseCore
{

    internal class DetailWizardSharedStyles
    {
        public readonly GUIStyle helpBoxBig;
        public readonly GUIContent noiseSeed = EditorGUIUtility.TrTextContent("Noise Seed", "Specifies the random seed value for detail object placement.");
        public readonly GUIContent noiseSpread = EditorGUIUtility.TrTextContent("Noise Spread", "Controls the spatial frequency of the noise pattern used to vary the scale and color of the detail objects.");
        public readonly GUIContent detailDensity = EditorGUIUtility.TrTextContent("Detail density", "Controls detail density for this detail prototype, relative to it's size. Only enabled in \"Coverage\" detail scatter mode.");
        public readonly GUIContent holeEdgePadding = EditorGUIUtility.TrTextContent("Hole Edge Padding (%)", "Controls how far away detail objects are from the edge of the hole area.\n\nSpecify this value as a percentage of the detail width, which determines the radius of the circular area around the detail object used for hole testing.");
        public readonly GUIContent useDensityScaling = EditorGUIUtility.TrTextContent("Affected by Density Scale", "Toggles whether or not this detail prototype should be affected by the global density scaling setting in the Terrain settings.");
        public readonly GUIContent alignToGround = EditorGUIUtility.TrTextContent("Align To Ground (%)", "Rotate detail axis to ground normal direction.");
        public readonly GUIContent positionJitter = EditorGUIUtility.TrTextContent("Position Jitter (%)", "Controls the randomness of the detail distribution, from ordered to random. Only available when legacy distribution in Quality Settings is turned off.");

        public DetailWizardSharedStyles()
        {
            helpBoxBig = new GUIStyle("HelpBox")
            {
                fontSize = EditorStyles.label.fontSize
            };
        }

        private static DetailWizardSharedStyles s_Styles = null;

        public static DetailWizardSharedStyles Instance
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new DetailWizardSharedStyles();
                return s_Styles;
            }
        }
    };

    public enum TerrainDetailMeshRenderMode
    {
        Mesh,
        Texture,
        BillboardTexture
    }

    public class TerrainDetailMeshWizard : ScriptableWizard
    {
        DetailPrototypeSerializable prototype;


        public static TerrainDetailMeshRenderMode GetRenderMode(DetailPrototypeSerializable prototype)
        {
            switch (prototype.renderMode)
            {
                case DetailRenderMode.GrassBillboard:
                    return TerrainDetailMeshRenderMode.BillboardTexture;
                case DetailRenderMode.Grass:
                    return TerrainDetailMeshRenderMode.Texture;
                default:
                    return TerrainDetailMeshRenderMode.Mesh;

            }
        }


        void DoApply()
        {
            if (MicroVerse.instance != null)
                MicroVerse.instance.Invalidate(stamp.GetBounds(), MicroVerse.InvalidateType.Tree);
        }

        void OnWizardCreate()
        {
            DoApply();
        }

        void OnWizardOtherButton()
        {
            DoApply();
        }

        void OnWizardUpdate()
        {

        }
        static DetailStamp stamp;
        public static bool DrawInspector(DetailStamp ds, DetailPrototypeSerializable prototype)
        {
            stamp = ds;
            return DrawInspector(prototype);
        }

        public static bool DrawInspector(DetailPrototypeSerializable prototype)
        { 
            EditorGUI.BeginChangeCheck();

            var renderMode = GetRenderMode(prototype);
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.terrainDetailGrassBillboardShader != null
                || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.terrainDetailGrassShader != null)
            {
                var orm = renderMode;
                renderMode = (TerrainDetailMeshRenderMode)EditorGUILayout.EnumPopup("Render Mode", renderMode);
                if (orm != renderMode)
                {
                    if (renderMode == TerrainDetailMeshRenderMode.Texture)
                    {
                        prototype.renderMode = DetailRenderMode.Grass;
                    }
                    else if (renderMode == TerrainDetailMeshRenderMode.BillboardTexture)
                    {
                        prototype.renderMode = DetailRenderMode.GrassBillboard;
                    }
                    else
                    {
                        prototype.renderMode = DetailRenderMode.VertexLit;
                    }
                }
            }

            if (prototype.renderMode == DetailRenderMode.VertexLit)
            {
                prototype.prototype = EditorGUILayout.ObjectField("Detail Prefab", prototype.prototype, typeof(GameObject), false) as GameObject;
                prototype.prototypeTexture = null;
                prototype.usePrototypeMesh = true;
                if (prototype.prototype != null)
                {
                    var lod = prototype.prototype.GetComponent<LODGroup>();
                    if (lod != null)
                    {
                        EditorGUILayout.HelpBox("LOD Ground Found\nThis is not supported by Unity's terrain system, however some 3rd party renderer's allow it. The first LOD will be set to be used instead.", MessageType.Info);
                    }
                }
            }
            else
            {
                prototype.usePrototypeMesh = false;
                prototype.prototypeTexture = EditorGUILayout.ObjectField("Grass Texture", prototype.prototypeTexture, typeof(Texture2D), false) as Texture2D;
                prototype.prototype = null;
            }

            if (prototype.usePrototypeMesh && prototype.prototype != null)
            {

#if UNITY_2022_2_OR_NEWER
                prototype.alignToGround = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.alignToGround, prototype.alignToGround * 100, 0, 100) / 100.0f;
                GUI.enabled = !QualitySettings.useLegacyDetailDistribution;
                prototype.positionJitter = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.positionJitter, prototype.positionJitter * 100, 0, 100) / 100.0f;
                GUI.enabled = true;
#endif
                prototype.minWidth = EditorGUILayout.FloatField("Min Width", prototype.minWidth);
                prototype.maxWidth = EditorGUILayout.FloatField("Max Width", prototype.maxWidth);
                prototype.minHeight = EditorGUILayout.FloatField("Min Height", prototype.minHeight);
                prototype.maxHeight = EditorGUILayout.FloatField("Max Height", prototype.maxHeight);
                prototype.noiseSeed = EditorGUILayout.IntField(DetailWizardSharedStyles.Instance.noiseSeed, prototype.noiseSeed);
                prototype.noiseSpread = EditorGUILayout.FloatField(DetailWizardSharedStyles.Instance.noiseSpread, prototype.noiseSpread);
                prototype.holeEdgePadding = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.holeEdgePadding, prototype.holeEdgePadding * 100, 0, 100) / 100.0f;

                prototype.density = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.detailDensity, prototype.density, 0, 8);


                if (!prototype.useInstancing)
                {
                    prototype.healthyColor = EditorGUILayout.ColorField("Healthy Color", prototype.healthyColor);
                    prototype.dryColor = EditorGUILayout.ColorField("Dry Color", prototype.dryColor);
                }


                GUI.enabled = true;
                prototype.useInstancing = EditorGUILayout.Toggle("Use GPU Instancing", prototype.useInstancing);
#if UNITY_2022_2_OR_NEWER
                prototype.useDensityScaling = EditorGUILayout.Toggle(DetailWizardSharedStyles.Instance.useDensityScaling, prototype.useDensityScaling);
#endif
            }
            else if (prototype.usePrototypeMesh == false)
            {
#if UNITY_2022_2_OR_NEWER
                prototype.alignToGround = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.alignToGround, prototype.alignToGround * 100, 0, 100) / 100.0f;

                prototype.positionJitter = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.positionJitter, prototype.positionJitter * 100, 0, 100) / 100.0f;
                prototype.targetCoverage = EditorGUILayout.Slider(new GUIContent("Target Coverage"), prototype.targetCoverage * 100, 0, 100) / 100.0f;
#endif
                prototype.minWidth = EditorGUILayout.FloatField("Min Width", prototype.minWidth);
                prototype.maxWidth = EditorGUILayout.FloatField("Max Width", prototype.maxWidth);
                prototype.minHeight = EditorGUILayout.FloatField("Min Height", prototype.minHeight);
                prototype.maxHeight = EditorGUILayout.FloatField("Max Height", prototype.maxHeight);
                prototype.noiseSeed = EditorGUILayout.IntField(DetailWizardSharedStyles.Instance.noiseSeed, prototype.noiseSeed);
                prototype.noiseSpread = EditorGUILayout.FloatField(DetailWizardSharedStyles.Instance.noiseSpread, prototype.noiseSpread);
                prototype.density = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.detailDensity, prototype.density, 0, 5);

                prototype.holeEdgePadding = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.holeEdgePadding, prototype.holeEdgePadding * 100, 0, 100) / 100.0f;

                prototype.healthyColor = EditorGUILayout.ColorField("Healthy Color", prototype.healthyColor);
                prototype.dryColor = EditorGUILayout.ColorField("Dry Color", prototype.dryColor);

                prototype.useDensityScaling = EditorGUILayout.Toggle(DetailWizardSharedStyles.Instance.useDensityScaling, prototype.useDensityScaling);
                prototype.minHeight = Mathf.Max(0f, prototype.minHeight);
                prototype.minWidth = Mathf.Max(0f, prototype.minWidth);
                prototype.maxHeight = Mathf.Max(prototype.minHeight, prototype.maxHeight);
                prototype.maxWidth = Mathf.Max(prototype.minWidth, prototype.maxWidth);

            }
            if (EditorGUI.EndChangeCheck())
            {
                if (MicroVerse.instance != null)
                {
                    if (stamp != null)
                    {
                        MicroVerse.instance.Invalidate(stamp.GetBounds(), MicroVerse.InvalidateType.Tree);
                    }
                    else
                    {
                        MicroVerse.instance.Invalidate(null, MicroVerse.InvalidateType.Tree);
                    }
                }
                return true;
            }
            return false;
        }
        protected override bool DrawWizardGUI()
        {
            return DrawInspector(stamp, prototype);
        }
    }

}
