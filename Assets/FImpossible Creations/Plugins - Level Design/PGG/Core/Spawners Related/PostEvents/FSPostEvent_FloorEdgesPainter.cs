using FIMSpace;
using FIMSpace.Generating;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "PE Floor Edges Painter", menuName = "Floor Edges Painter Instance", order = 1)]
    public class FSPostEvent_FloorEdgesPainter : FieldSpawnerPostEvent_Base
    {
        FieldVariable FalloffVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Check Distance", 1f); }
        float GetFalloff(FieldSetup.CustomPostEventHelper helper) { return FalloffVar(helper).GetFloatValue(); }
        FieldVariable ColVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Color", Color.red); }
        Color PaintColor(FieldSetup.CustomPostEventHelper helper) { return ColVar(helper).GetColor(); }

        FieldVariable FalloffCurveVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Intensity Curve", AnimationCurve.Linear(0f, 1f, 1f, 0f)); }

        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            //FDebug.StartMeasure();

            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");
            string useTag = tagg.GetStringValue();
            if (useTag == "Untagged") useTag = "";

            Color col = PaintColor(helper);
            AnimationCurve curve = FalloffCurveVar(helper).GetCurve();
            if (curve == null) curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            float dist = GetFalloff(helper);

            ProceedPainting(generatedRef.CombinedNonStaticContainer, useTag, col, curve, dist, generatedRef.InternalField.GetCellUnitSize().x);
            ProceedPainting(generatedRef.CombinedStaticContainer, useTag, col, curve, dist, generatedRef.InternalField.GetCellUnitSize().x);

            //FDebug.EndMeasureAndLog("Vert");
        }

        void ProceedPainting(GameObject parent, string useTag, Color col, AnimationCurve curve, float checkDist, float cellSize)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                MeshFilter filter = parent.transform.GetChild(i).GetComponent<MeshFilter>();
                if (filter == null) continue;
                if (useTag.Length > 0) if (filter.gameObject.CompareTag(useTag) == false) continue;
                ProceedMeshPainting(filter.transform, filter.sharedMesh, col, curve, checkDist, cellSize);
            }
        }

        void ProceedMeshPainting(Transform parent, Mesh mesh, Color col, AnimationCurve curve, float checkDist, float cellSize)
        {
            if (parent == null) return; if (mesh == null) return;

            MeshPaintHelper paintHelper = new MeshPaintHelper(parent, mesh);
            if (!paintHelper.IsValid) return;

            paintHelper.GeneratePaintingGrid();

            for (int i = 0; i < paintHelper.verts.Count; i++)
            {
                Vector3 vertWPos = parent.TransformPoint(paintHelper.verts[i]);
                float blendMul = 1f;

                float blend = paintHelper.IsSideVertex(vertWPos, checkDist) ? 1f : 0f;

                if (blendMul < 0.0001f) continue;

                blend = curve.Evaluate(1f - blend);

                Color nCol = paintHelper.colors[i];
                nCol = Color.LerpUnclamped(nCol, col, blend);

                paintHelper.colors[i] = nCol;
            }

            mesh.SetColors(paintHelper.colors);
        }


#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "Painting vertices on the combined mesh edges";

            var falloff = FalloffVar(helper);
            FieldVariable.Editor_DrawTweakableVariable(falloff);
            if (falloff.Float < 0f) falloff.Float = 0f;

            FieldVariable.Editor_DrawTweakableVariable(ColVar(helper));

            //GUILayout.Space(2);
            //var curveVar = FalloffCurveVar(helper);
            //curveVar.helper = new Vector3(0f, 1f, 0f);
            //FieldVariable.Editor_DrawTweakableVariable(curveVar);

            GUILayout.Space(4);
            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");

            string info = "";
            if (tagg.GetStringValue() == "Untagged") info = "Untagged = Apply To All";
            else info = "Apply To Tagged:";
            tagg.SetValue(UnityEditor.EditorGUILayout.TagField(info, tagg.GetStringValue()));

            GUILayout.Space(2);
        }
#endif


    }
}
