using FIMSpace;
using FIMSpace.FGenerating;
using FIMSpace.Generating;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "TD Color Vertices", menuName = "Tile Designer - Vertices Instance", order = 1)]
    public class TDPostFilter_VertexColoring : TilePostFilterBase
    {
        FUniversalVariable ColVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Color", Color.red); }
        Color PaintColor(TileDesign.PostFilterHelper helper) { return ColVar(helper).GetColor(); }
        FUniversalVariable NoiseVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Noise Scale", 0.1f); }
        float GetNoiseScale(TileDesign.PostFilterHelper helper) { return NoiseVar(helper).GetFloat(); }

        FUniversalVariable FalloffVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Falloff", 0.2f); }
        float GetFalloff(TileDesign.PostFilterHelper helper) { return FalloffVar(helper).GetFloat(); }

        FUniversalVariable FalloffCurveVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Intensity Curve", AnimationCurve.Linear(0f, 1f, 1f, 0f)); }

        FUniversalVariable ModeVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Mode", 0); }
        FUniversalVariable ApplyVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Apply", 1); }
        FUniversalVariable AxisVar(TileDesign.PostFilterHelper helper) { return helper.RequestVariable("Axis", 0); }

        public enum EPaintOn { WholeMesh, UpperEdges, LowerEdges, ExcludeUpDownEdges }
        public enum EApply { Override, Blend }

        FGenerators.DefinedRandom rand;

        public override void OnTilesCombined(TileDesign design, Material combinationMaterial, ref Mesh mesh, TileDesign.PostFilterHelper helper)
        {
            Color col = PaintColor(helper);
            float scale = GetNoiseScale(helper);

            rand = new FGenerators.DefinedRandom(FGenerators.LatestSeed);

            Vector3 randomOffset = new Vector3(rand.GetRandom(-1000, 1000), rand.GetRandom(-1000, 1000), rand.GetRandom(-1000, 1000));
            EPaintOn paintMode = (EPaintOn)ModeVar(helper).GetInt();

            float fallff = GetFalloff(helper);

            AnimationCurve curve = FalloffCurveVar(helper).GetCurve();
            if (curve == null) curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            EApply apply = (EApply)ApplyVar(helper).GetInt();
            ProceedMeshPainting(mesh, col, scale, fallff, randomOffset, paintMode, apply, curve);
        }


        // Main procedure here

        void ProceedMeshPainting(Mesh mesh, Color col, float scale, float falloff, Vector3 randomOffset, EPaintOn paintMode, EApply apply, AnimationCurve curve)
        {
            if (mesh == null) return;

            MeshPaintHelper paintHelper = new MeshPaintHelper(null, mesh);
            if (!paintHelper.IsValid) return;

            if (paintMode != EPaintOn.WholeMesh) paintHelper.GeneratePaintingGrid();


            for (int i = 0; i < paintHelper.verts.Count; i++)
            {
                Vector3 vertWPos = (paintHelper.verts[i]);
                float blendMul = 1f;

                #region Paint Mode Switch


                if (paintMode == EPaintOn.WholeMesh)
                {
                    // No restrictions
                }
                else if (paintMode == EPaintOn.UpperEdges)
                {
                    blendMul = paintHelper.GetEdgeFactor(i, vertWPos, true, falloff);
                }
                else if (paintMode == EPaintOn.LowerEdges)
                {
                    blendMul = paintHelper.GetEdgeFactor(i, vertWPos, false, falloff);
                }
                else if (paintMode == EPaintOn.ExcludeUpDownEdges)
                {
                    float upMul = paintHelper.GetEdgeFactor(i, vertWPos, true, falloff);
                    float lowMul = paintHelper.GetEdgeFactor(i, vertWPos, false, falloff);

                    blendMul = 1f - Mathf.Max(upMul, lowMul);
                }

                #endregion

                if (blendMul < 0.0001f) continue;

                float x = vertWPos.x * scale + randomOffset.x;
                float y = vertWPos.y * scale + randomOffset.y;
                float z = vertWPos.z * scale + randomOffset.x;

                float blend = FEngineering.PerlinNoise3D(x, y, z) * blendMul;
                //float blend = Mathf.PerlinNoise(x + z, y) * blendMul;
                blend = curve.Evaluate(1f - blend);

                #region Color Apply

                Color nCol = paintHelper.colors[i];

                if (apply == EApply.Blend)
                    nCol = Color.LerpUnclamped(nCol, col, blend);
                else
                    nCol = Color.LerpUnclamped(Color.clear, col, blend);

                #endregion


                paintHelper.colors[i] = nCol;
            }

            mesh.SetColors(paintHelper.colors);
        }


        // Editor inspector display code below

#if UNITY_EDITOR
        public override void Editor_DisplayGUI(TileDesign.PostFilterHelper helper)
        {
            PostEventInfo = "Painting vertices of the combined meshes. Really useful if you use vertex color in your shaders.";

            ColVar(helper).Editor_DisplayVariableGUI();
            NoiseVar(helper).Editor_DisplayVariableGUI();

            GUILayout.Space(4);
            var falloff = FalloffVar(helper);
            falloff.Editor_DisplayVariableGUI();
            if (falloff.GetFloat() < 0f) falloff.SetValue(0f);

            GUILayout.Space(2);
            var curveVar = FalloffCurveVar(helper);
            curveVar.SetCurveFixedRange(0f, 0f, 1f, 1f);
            curveVar.Editor_DisplayVariableGUI();

            GUILayout.Space(4);

            int mode = ModeVar(helper).GetInt();
            EPaintOn eMode = (EPaintOn)mode;
            eMode = (EPaintOn)EditorGUILayout.EnumPopup("Paint Mode:", eMode);
            ModeVar(helper).SetValue((int)eMode);

            GUILayout.Space(2);

            int app = ApplyVar(helper).GetInt();
            EApply eAppl = (EApply)app;
            eAppl = (EApply)EditorGUILayout.EnumPopup(new GUIContent("Apply Mode:", "If combined mesh already contains vertex color data, you might want to blend it instead of overriding."), eAppl);
            ApplyVar(helper).SetValue((int)eAppl);

            GUILayout.Space(2);

            base.Editor_DisplayGUI(helper);
        }
#endif

    }
}
