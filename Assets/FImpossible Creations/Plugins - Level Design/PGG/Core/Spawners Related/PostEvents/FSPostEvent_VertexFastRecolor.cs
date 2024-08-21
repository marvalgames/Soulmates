using FIMSpace;
using FIMSpace.Generating;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "PE Fast Color Vertices", menuName = "Vertices Instance", order = 1)]
    public class FSPostEvent_VertexFastRecolor : FieldSpawnerPostEvent_Base
    {
        FieldVariable ColVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Color", Color.red); }
        Color PaintColor(FieldSetup.CustomPostEventHelper helper) { return ColVar(helper).GetColor(); }
        FieldVariable NoiseVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Noise Scale", 0.1f); }
        float GetNoiseScale(FieldSetup.CustomPostEventHelper helper) { return NoiseVar(helper).GetFloatValue(); }

        FieldVariable PowerVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Power",1f); }
        float GetPower(FieldSetup.CustomPostEventHelper helper) { return PowerVar(helper).GetFloatValue(); }

        FieldVariable FalloffCurveVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Intensity Curve", AnimationCurve.Linear(0f, 0f, 1f, 1f)); }
        FieldVariable ModeVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Mode", 0); }
        FieldVariable DriveValue(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Drive", new Vector3(0.5f, 2f, 0f)); }

        enum EPositionDriven
        {
            YBelow, YAbove, YBetween
        }

        EPositionDriven driven = EPositionDriven.YBetween;
        Vector3 driveValue = Vector3.one;
        float midPos;

        FGenerators.DefinedRandom rand;
        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            //FDebug.StartMeasure();
            Color col = PaintColor(helper);
            float scale = GetNoiseScale(helper);

            rand = new FGenerators.DefinedRandom(FGenerators.LatestSeed + 1000);
            Vector3 randomOffset = new Vector3(rand.GetRandom(-10000, 10000), rand.GetRandom(-10000, 10000), rand.GetRandom(-10000, 10000));

            float powr = GetPower(helper);

            AnimationCurve curve = FalloffCurveVar(helper).GetCurve();
            if (curve == null) curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");
            string useTag = tagg.GetStringValue();
            if (useTag == "Untagged") useTag = "";

            driven = (EPositionDriven)ModeVar(helper).IntV;
            driveValue = DriveValue(helper).GetVector3Value();
            midPos = Mathf.LerpUnclamped(driveValue.x, driveValue.y, 0.5f);

            ProceedPainting(generatedRef.CombinedNonStaticContainer, useTag, col, scale, powr, randomOffset, curve, generatedRef.InternalField.GetCellUnitSize().x);
            ProceedPainting(generatedRef.CombinedStaticContainer, useTag, col, scale, powr, randomOffset, curve, generatedRef.InternalField.GetCellUnitSize().x);
            //FDebug.EndMeasureAndLog("VertXs");
        }

        void ProceedPainting(GameObject parent, string useTag, Color col, float scale, float power, Vector3 randomOffset, AnimationCurve curve, float cellSize)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                MeshFilter filter = parent.transform.GetChild(i).GetComponent<MeshFilter>();
                if (filter == null) continue;
                if (useTag.Length > 0) if (filter.gameObject.CompareTag(useTag) == false) continue;
                ProceedMeshPainting(filter.transform, filter.sharedMesh, col, scale, power, randomOffset, curve, cellSize);
            }
        }



        // Main procedure here

        void ProceedMeshPainting(Transform parent, Mesh mesh, Color col, float scale, float power, Vector3 randomOffset, AnimationCurve curve, float cellSize)
        {
            if (parent == null) return; if (mesh == null) return;

            List<Vector3> verts = new List<Vector3>();
            mesh.GetVertices(verts);

            List<Color> cols = new List<Color>();
            mesh.GetColors(cols);
            if (cols.Count == 0) for (int i = 0; i < verts.Count; i++) cols.Add(Color.black);

            Vector3 dVal = driveValue;
            float yOff = mesh.bounds.min.y;

            for (int i = 0; i < verts.Count; i++)
            {
                //Vector3 vertWPos = parent.TransformPoint(verts[i]);
                Vector3 vertWPos = verts[i];
                vertWPos.y -= yOff;

                float blendMul = 0f;

                if (driven == EPositionDriven.YAbove)
                {
                    if (vertWPos.y > dVal.x)
                    {
                        blendMul = Mathf.InverseLerp(dVal.x, mesh.bounds.max.y, vertWPos.y);
                    }
                }
                else if (driven == EPositionDriven.YBelow)
                {
                    if (vertWPos.y < dVal.x)
                    {
                        blendMul = Mathf.InverseLerp(dVal.x, mesh.bounds.min.y, vertWPos.y);
                    }
                }
                else if (driven == EPositionDriven.YBetween)
                {
                    if (vertWPos.y > dVal.x && vertWPos.y < dVal.y)
                    {
                        if (vertWPos.y < midPos)
                            blendMul = Mathf.InverseLerp(dVal.x, midPos, vertWPos.y);
                        else
                            blendMul = Mathf.InverseLerp(dVal.y, midPos, vertWPos.y);
                    }
                }

                if (blendMul < 0.0001f) continue;

                float x = vertWPos.x * scale + randomOffset.x;
                float y = vertWPos.y * scale + randomOffset.y;
                float z = vertWPos.z * scale + randomOffset.z;

                float noise = FEngineering.PerlinNoise3D(x, y, z);
                noise = 1f - noise; noise *= noise; noise = 1f - noise;
                float blend = noise * curve.Evaluate(blendMul) * power;

                #region Color Apply

                Color nCol = cols[i];
                nCol = Color.LerpUnclamped(nCol, col, blend);

                #endregion

                cols[i] = nCol;
            }

            mesh.SetColors(cols);
        }


        // Editor inspector display code below

#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "Painting vertices of the combined meshes. Really useful if you use vertex color in your shaders.";

            FieldVariable.Editor_DrawTweakableVariable(ColVar(helper));
            FieldVariable.Editor_DrawTweakableVariable(NoiseVar(helper));

            GUILayout.Space(4);
            var falloff = PowerVar(helper);
            falloff.helper.x = 0f;
            falloff.helper.y = 1f;
            FieldVariable.Editor_DrawTweakableVariable(falloff);
            if (falloff.Float < 0f) falloff.Float = 0f;

            GUILayout.Space(2);
            var curveVar = FalloffCurveVar(helper);
            curveVar.helper = new Vector3(0f, 1f, 0f);
            FieldVariable.Editor_DrawTweakableVariable(curveVar);

            GUILayout.Space(4);

            int mode = ModeVar(helper).IntV;
            EPositionDriven eMode = (EPositionDriven)mode;
            eMode = (EPositionDriven)EditorGUILayout.EnumPopup("Paint Mode:", eMode);
            ModeVar(helper).IntV = (int)eMode;

            GUILayout.Space(2);

            var driveV = DriveValue(helper);
            Vector3 dVal = driveV.GetVector3Value();

            if (eMode == EPositionDriven.YAbove)
                dVal.x = EditorGUILayout.FloatField("Y Above:", dVal.x);
            else if (eMode == EPositionDriven.YBelow)
                dVal.x = EditorGUILayout.FloatField("Y Below:", dVal.x);
            else if (eMode == EPositionDriven.YBetween)
                dVal = EditorGUILayout.Vector2Field("Y Between:", dVal);

            driveV.SetValue(dVal);

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
