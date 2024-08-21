using FIMSpace;
using FIMSpace.Generating;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "PE Color Vertices", menuName = "Vertices Instance", order = 1)]
    public class FSPostEvent_VertexRecoloring : FieldSpawnerPostEvent_Base
    {
        FieldVariable ColVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Color", Color.red); }
        Color PaintColor(FieldSetup.CustomPostEventHelper helper) { return ColVar(helper).GetColor(); }
        FieldVariable NoiseVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Noise Scale", 0.1f); }
        float GetNoiseScale(FieldSetup.CustomPostEventHelper helper) { return NoiseVar(helper).GetFloatValue(); }

        FieldVariable FalloffVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Falloff", 0.2f); }
        float GetFalloff(FieldSetup.CustomPostEventHelper helper) { return FalloffVar(helper).GetFloatValue(); }

        FieldVariable FalloffCurveVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Intensity Curve", AnimationCurve.Linear(0f, 1f, 1f, 0f)); }


        FieldVariable ModeVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Mode", 0); }
        FieldVariable ApplyVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Apply", 1); }
        FieldVariable AxisVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Axis", 0); }

        public enum EPaintOn
        {
            WholeMesh, UpperEdges, LowerEdges, ExcludeUpDownEdges//, SideEdges, ExcludeAllEdges
        }

        public enum EApply { Override, Blend }
        public enum EAxis { Wall, Flat }


        private EAxis executionAxis = EAxis.Wall;

        FGenerators.DefinedRandom rand;
        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            //FDebug.StartMeasure();
            Color col = PaintColor(helper);
            float scale = GetNoiseScale(helper);

            rand = new FGenerators.DefinedRandom(FGenerators.LatestSeed + 1000);
            Vector3 randomOffset = new Vector3(rand.GetRandom(-10000, 10000), rand.GetRandom(-10000, 10000), rand.GetRandom(-10000, 10000));
            EPaintOn paintMode = (EPaintOn)ModeVar(helper).IntV;

            float fallff = GetFalloff(helper);

            AnimationCurve curve = FalloffCurveVar(helper).GetCurve();
            if (curve == null) curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");
            string useTag = tagg.GetStringValue();
            if (useTag == "Untagged") useTag = "";

            EApply apply = (EApply)ApplyVar(helper).IntV;

            executionAxis = (EAxis)AxisVar(helper).IntV;

            ProceedPainting(generatedRef.CombinedNonStaticContainer, useTag, col, scale, fallff, randomOffset, paintMode, apply, curve, generatedRef.InternalField.GetCellUnitSize().x);
            ProceedPainting(generatedRef.CombinedStaticContainer, useTag, col, scale, fallff, randomOffset, paintMode, apply, curve, generatedRef.InternalField.GetCellUnitSize().x);
            //FDebug.EndMeasureAndLog("VertXs");
        }

        void ProceedPainting(GameObject parent, string useTag, Color col, float scale, float falloff, Vector3 randomOffset, EPaintOn paintMode, EApply apply, AnimationCurve curve, float cellSize)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                MeshFilter filter = parent.transform.GetChild(i).GetComponent<MeshFilter>();
                if (filter == null) continue;
                if (useTag.Length > 0) if (filter.gameObject.CompareTag(useTag) == false) continue;
                ProceedMeshPainting(filter.transform, filter.sharedMesh, col, scale, falloff, randomOffset, paintMode, apply, curve, cellSize);
            }
        }



        // Main procedure here

        void ProceedMeshPainting(Transform parent, Mesh mesh, Color col, float scale, float falloff, Vector3 randomOffset, EPaintOn paintMode, EApply apply, AnimationCurve curve, float cellSize)
        {
            if (parent == null) return; if (mesh == null) return;

            MeshPaintHelper paintHelper = new MeshPaintHelper(parent, mesh);
            if (!paintHelper.IsValid) return;

            if (paintMode != EPaintOn.WholeMesh) paintHelper.GeneratePaintingGrid();
            
            for (int i = 0; i < paintHelper.verts.Count; i++)
            {
                Vector3 vertWPos = parent.TransformPoint(paintHelper.verts[i]);
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
                float z = vertWPos.z * scale + randomOffset.z;
                //float y = 0f;
                //float z = 0f;

                //if (executionAxis == EAxis.Wall)
                //{
                //    y = vertWPos.y * scale + randomOffset.y;
                //    z = vertWPos.z * scale + randomOffset.x;
                //}
                //else
                //{
                //    y = vertWPos.z * scale + randomOffset.y;
                //}



                float blend = FEngineering.PerlinNoise3D(x, y, z) * blendMul;
                //float blend = Mathf.PerlinNoise(x, z) * blendMul;
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
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "Painting vertices of the combined meshes. Really useful if you use vertex color in your shaders.";

            FieldVariable.Editor_DrawTweakableVariable(ColVar(helper));
            FieldVariable.Editor_DrawTweakableVariable(NoiseVar(helper));

            GUILayout.Space(4);
            var falloff = FalloffVar(helper);
            FieldVariable.Editor_DrawTweakableVariable(falloff);
            if (falloff.Float < 0f) falloff.Float = 0f;

            GUILayout.Space(2);
            var curveVar = FalloffCurveVar(helper);
            curveVar.helper = new Vector3(0f, 1f, 0f);
            FieldVariable.Editor_DrawTweakableVariable(curveVar);

            GUILayout.Space(4);

            int mode = ModeVar(helper).IntV;
            EPaintOn eMode = (EPaintOn)mode;
            eMode = (EPaintOn)EditorGUILayout.EnumPopup("Paint Mode:", eMode);
            ModeVar(helper).IntV = (int)eMode;

            GUILayout.Space(2);

            int app = ApplyVar(helper).IntV;
            EApply eAppl = (EApply)app;
            eAppl = (EApply)EditorGUILayout.EnumPopup(new GUIContent("Apply Mode:", "If combined mesh already contains vertex color data, you might want to blend it instead of overriding."), eAppl);
            ApplyVar(helper).IntV = (int)eAppl;

            //int ax = AxisVar(helper).IntV;
            //EAxis eAx = (EAxis)ax;
            //eAx = (EAxis)EditorGUILayout.EnumPopup(new GUIContent("Noise Axis:"), eAx);
            //AxisVar(helper).IntV = (int)eAx;


            GUILayout.Space(4);
            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");

            string info = "";
            if (tagg.GetStringValue() == "Untagged") info = "Untagged = Apply To All";
            else info = "Apply To Tagged:";
            tagg.SetValue(UnityEditor.EditorGUILayout.TagField(info, tagg.GetStringValue()));

            GUILayout.Space(2);

            base.Editor_DisplayGUI(helper);
        }
#endif

    }
}
