#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

namespace FIMSpace.Generating.Rules.Modelling
{
    public class SR_RoofSideGenerator : SR_RoofGenerator_TileDes_Base
    {
        public override string TitleName() { return "Roof Generator/Roof Side Wall Generator"; }

        public Vector3 ExtraOffset = Vector3.zero;
        public Vector2 UVTiling = new Vector2(4, 4);
        public bool GenerateFirstSide = true;
        public bool GenerateSecondSide = true;


        #region Editor GUI

#if UNITY_EDITOR


        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("Generating procedural mesh for the roof side wall.", MessageType.None);
            base.NodeBody(so);
        }

#endif

        #endregion


        protected override void PrepareMesh(FieldSetup preset, ref SpawnData thisSpawn, FieldCell cell)
        {
            secondSideSpawn = null;

            Bounds bounds = indicatorsBounds;
            bounds.Encapsulate(bounds.min + new Vector3(-ExtraOffset.x, -ExtraOffset.y, -ExtraOffset.z));
            bounds.Encapsulate(bounds.max + new Vector3(ExtraOffset.x, ExtraOffset.y, ExtraOffset.z));


            if (Rotate90)
            {
                bounds = FEngineering.RotateLocalBounds(bounds, Quaternion.Euler(0f, 90f, 0f));
            }

            Vector3 midPoint = bounds.center;
            midPoint.z = Mathf.Lerp(bounds.min.z, bounds.max.z, 0.5f);

            #region Prepare Tile Design

            TileDesign des = new TileDesign();
            TileMeshSetup tileMesh = null;

            TileDesignPreset tilePres = OptionalSetup;
            if (tilePres != null)
            {
                des.PasteEverythingFrom(tilePres.BaseDesign);
                if (des.TileMeshes.Count > 0) tileMesh = des.TileMeshes[0];
            }

            if (tileMesh == null) tileMesh = new TileMeshSetup("Roof Side Wall");
            des.DesignName = "Roof Side Wall";

            if (TargetMaterial) des.DefaultMaterial = TargetMaterial;
            des.TileMeshes.Add(tileMesh);

            tileMesh.GenTechnique = TileMeshSetup.EMeshGenerator.Extrude;
            tileMesh.Origin = EOrigin.BottomCenter;
            tileMesh._extrudeMirror = false;
            if (!OptionalSetup) tileMesh._extrudeBackCap = false;
            tileMesh._extrudeFrontCap = true;

            List<TileMeshSetup.CurvePoint> curve = tileMesh.GetCurve1();
            Color firstColor = Color.white;
            if (curve.Count > 0) firstColor = curve[0].VertexColor;

            if (curve.Count < 3)
            {
                curve.Clear();
                curve.Add(new TileMeshSetup.CurvePoint(new Vector2(0, 1f), true));
                curve.Add(new TileMeshSetup.CurvePoint(new Vector2(0.5f, 0f), true));
                curve.Add(new TileMeshSetup.CurvePoint(new Vector2(1, 1f), true));
                curve.Add(new TileMeshSetup.CurvePoint(new Vector2(0.5f, 1f), true));
                for (int c = 0; c < curve.Count; c++) curve[c].VertexColor = firstColor;
            }

            #endregion

            float roofCenterShift = RoofCenterVariable.GetValue(0f) == 0f ? RoofCenterOffset : RoofCenterVariable.GetValue(0f);
            if (roofCenterShift != 0f)
            {
                float roofProgr = Mathf.InverseLerp(-1f, 1f, roofCenterShift);
                float progr = Mathf.Abs(roofCenterShift);

                for (int c = 0; c < curve.Count; c++)
                {
                    var p = curve[c];
                    if (p.localPos.x < 0.01f) continue;
                    if (p.localPos.x > 0.99f) continue;

                    if (roofCenterShift > 0f)
                    {
                        p.localPos.x = Mathf.Lerp(p.localPos.x, 0f, progr);
                    }
                    else
                    {
                        p.localPos.x = Mathf.Lerp(p.localPos.x, 1f, progr);
                    }
                }
            }

            float roofHeight = RoofHeightVariable.GetValue(0f) == 0f ? RoofHeight : RoofHeightVariable.GetValue(0f);

            Vector3 backPoint = bounds.center; backPoint.z = bounds.min.z;
            Vector3 frontPoint = bounds.center; frontPoint.z = bounds.max.z;

            tileMesh.width = Mathf.Abs(backPoint.z - frontPoint.z);
            tileMesh.height = roofHeight;
            if (OptionalSetup == null) tileMesh.depth = 0;
            else tileMesh._extrudeFlip = true;

            tileMesh.UVFit = EUVFit.FitX;
            tileMesh.UVMul = UVTiling;

            float xYUVRatio = tileMesh.width / tileMesh.height;
            tileMesh.UVMul.x *= xYUVRatio;
            tileMesh.UVMul.y /= xYUVRatio;
            tileMesh.UVMul /= xYUVRatio;

            des.FullGenerateStack();

            GameObject tilePrefab = des.GeneratePrefab();

            Vector3 sideLPos = midPoint;
            sideLPos.x = bounds.max.x;

            Quaternion extraOffRot = Quaternion.identity;
            if (Rotate90) extraOffRot = Quaternion.Euler(0f, 90f, 0f);
            if (Rotate90) sideLPos = RotateAround(sideLPos, indicatorsBounds.center, extraOffRot);

            tilePrefab.transform.position = sideLPos;
            tilePrefab.transform.rotation = extraOffRot * Quaternion.LookRotation(Vector3.right);

            thisSpawn.SetGeneratorSpaceCoords(tilePrefab.transform.position, tilePrefab.transform.rotation);
            tilePrefab.transform.rotation = Quaternion.identity;

            //GameObject secondSide = Instantiate(tilePrefab);
            //Vector3 sideRPos = midPoint;
            //sideRPos.x = bounds.min.x;
            //if (Rotate90) sideRPos = RotateAround(sideRPos, indicatorsBounds.center, extraOffRot);
            //secondSide.transform.position = sideRPos;
            //secondSide.transform.rotation = extraOffRot * Quaternion.LookRotation(Vector3.left);
            //secondSide.transform.SetParent(tilePrefab.transform, true);

            if (GenerateSecondSide)
            {
                secondSideSpawn = thisSpawn.Copy();
                thisSpawn.GeneratorSpaceToCellSpace(preset, cell, thisSpawn.Offset);

                Vector3 sideRPos = midPoint;
                sideRPos.x = bounds.min.x;
                if (Rotate90) sideRPos = RotateAround(sideRPos, indicatorsBounds.center, extraOffRot);
                secondSideSpawn.SetGeneratorSpaceCoords(sideRPos, extraOffRot * Quaternion.LookRotation(Vector3.left));
            }

            if (!GenerateFirstSide) thisSpawn.Enabled = false;

            //////////////////////////////
            generatedPrefab = tilePrefab;
            tilePrefab.transform.position = new Vector3(10000, 10000, 10000);
            tilePrefab.hideFlags = HideFlags.HideAndDontSave;
            tilePrefab.SetActive(false);
            //////////////////////////////

        }

        SpawnData secondSideSpawn = null;

        protected override void OnGenerateRoof(List<SpawnData> indicators, FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            base.OnGenerateRoof(indicators, mod, ref thisSpawn, preset, cell, grid);

            if (FGenerators.NotNull(secondSideSpawn))
            {
                secondSideSpawn.Prefab = thisSpawn.Prefab;
                var nCell = GetCellInPosition(secondSideSpawn.Offset, 3);
                nCell.AddSpawnToCell(secondSideSpawn);
                secondSideSpawn.LocalRotationOffset = Vector3.zero;
                secondSideSpawn.GeneratorSpaceToCellSpace(preset, nCell, secondSideSpawn.Offset);
            }
        }
    }
}