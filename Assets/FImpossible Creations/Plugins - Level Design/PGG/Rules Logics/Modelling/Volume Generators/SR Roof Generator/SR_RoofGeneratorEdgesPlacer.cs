#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Rules.Modelling
{
    public class SR_RoofGeneratorEdgesPlacer : SR_RoofGenerator_Base
    {
        public override string TitleName() { return "Roof Generator/Roof Generator - Edges Placer"; }

        // Place scaled model on left/right/front/back/middle edge

        public enum ESpawnArea
        {
            None = 0,
            LeftEdge = 2,
            RightEdge = 4,
            FrontEdge = 8,
            BackEdge = 16,
            Top = 32
        }

        [HideInInspector] public ESpawnArea SpawnArea = ESpawnArea.Top;

        [Space(4)]
        [PGG_SingleLineSwitch("PaddingScale", 58, "Select if you want to align padding spacing with cell size or world units", 140)]
        public Vector3 ModelTilePadding = new Vector3(1f, 1f, 1f);
        [HideInInspector] public ESR_Measuring PaddingScale = ESR_Measuring.Cells;
        public Vector3 RotateTile = new Vector3(0, 0, 0);

        [Space(4)]
        //public Vector3 Rescale = Vector3.one;
        public Vector3 ExtraOffset = new Vector3(0, 0, 0);
        public Vector3 RandomPRS = new Vector3(0, 0, 0);

        private FGenerators.DefinedRandom _rand = null;


        #region Editor GUI

#if UNITY_EDITOR

        public override void NodeBody(SerializedObject so)
        {
            SpawnArea = (ESpawnArea)EditorGUILayout.EnumFlagsField("Spawn Area:", SpawnArea);
            GUILayout.Space(4);

            GUIIgnore.Clear();
            if ((SpawnArea & ESpawnArea.Top) == 0)
            {
                GUIIgnore.Add("RoofHeight");
                GUIIgnore.Add("RoofHeightVariable");
                GUIIgnore.Add("RoofCenterOffset");
                GUIIgnore.Add("RoofCenterVariable");
            }

            base.NodeBody(so);
        }

#endif

        #endregion


        protected override void PrepareMesh(FieldSetup preset, ref SpawnData thisSpawn, FieldCell cell)
        {
            _rand = new FGenerators.DefinedRandom(FGenerators.LatestSeed + 500);
            float roofHeight = RoofHeightVariable.GetValue(0f) == 0f ? RoofHeight : RoofHeightVariable.GetValue(0f);

            thisSpawn.Enabled = false;

            Vector3 cellSize = preset.GetCellUnitSize();
            // todo Get rotated bounds, indicator matrix
            Bounds bounds = indicatorsBounds;

            bounds.Encapsulate(bounds.min + new Vector3(-ExtraOffset.x, 0, -ExtraOffset.z));
            bounds.Encapsulate(bounds.max + new Vector3(ExtraOffset.x, 0, ExtraOffset.z));

            Vector3 tilePadding = ModelTilePadding;
            if (PaddingScale == ESR_Measuring.Cells) tilePadding = Vector3.Scale(ModelTilePadding, cellSize);

            // todo Z to center offset front / back
            int xTiles = Mathf.RoundToInt(bounds.size.x / tilePadding.x);
            int zTiles = Mathf.RoundToInt(bounds.size.z / tilePadding.z);

            if (xTiles < 1) xTiles = 1;
            if (zTiles < 2) zTiles = 2;
            if (zTiles % 2 != 0) zTiles -= 1;

            float xScaleFactor = bounds.size.x / (xTiles);
            float zScaleFactor = bounds.size.z / (zTiles);

            Vector3 originOffset = Vector3.zero;
            Vector3 spawnOrigin = bounds.min + originOffset;

            originOffset.x = xScaleFactor / 2f;
            originOffset.z = zScaleFactor / 2f;


            // Roof rotation
            Vector3 roofMinStart = bounds.min; roofMinStart.x = bounds.center.x;
            Vector3 roofCenterTop = bounds.center; roofCenterTop.y = bounds.center.y + roofHeight;
            //originOffset.y += (roofHeight / 2f) * (1f / (zTiles / 2f));
            spawnOrigin += originOffset;

            Quaternion rotationOffset = Quaternion.identity;
            if (RotateTile != Vector3.zero) rotationOffset *= Quaternion.Euler(RotateTile);

            
            if ((SpawnArea & ESpawnArea.LeftEdge) != 0)
            {
                for (int z = 0; z < zTiles; z++)
                {
                    SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);
                    Vector3 pos = spawnOrigin;

                    pos.x = bounds.min.x;
                    pos.z += zScaleFactor * z;
                    pos.y = bounds.center.y;

                    Quaternion roofAngleRot = Quaternion.Euler(0, -90, 0);
                    Quaternion rot = FEngineering.QToWorld(roofAngleRot, rotationOffset);

                    ApplyRandomOffsetsToSpawn(tileSpawn, pos, rot, thisSpawn.LocalScaleMul);

                    //todo add stigma for roof generator random rotation etc.
                    SpawnForCell(tileSpawn, preset, pos);
                    //var nCell = GetCellInPosition(tileSpawn.Offset).AddSpawnToCell(tileSpawn);
                }
            }

            if ((SpawnArea & ESpawnArea.RightEdge) != 0)
            {
                for (int z = 0; z < zTiles; z++)
                {
                    SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);
                    Vector3 pos = spawnOrigin;

                    pos.x = bounds.max.x;
                    pos.z += zScaleFactor * z;
                    pos.y = bounds.center.y;

                    Quaternion roofAngleRot = Quaternion.Euler(0, 90, 0);
                    Quaternion rot = FEngineering.QToWorld(roofAngleRot, rotationOffset);

                    ApplyRandomOffsetsToSpawn(tileSpawn, pos, rot, thisSpawn.LocalScaleMul);

                    //todo add stigma for roof generator random rotation etc.
                    SpawnForCell(tileSpawn, preset, pos);
                    //var nCell = GetCellInPosition(tileSpawn.Offset).AddSpawnToCell(tileSpawn);
                }
            }

            if ((SpawnArea & ESpawnArea.FrontEdge) != 0)
            {
                for (int x = 0; x < xTiles; x++)
                {
                    SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);
                    Vector3 pos = spawnOrigin;

                    pos.z = bounds.min.z;
                    pos.x += xScaleFactor * x;
                    pos.y = bounds.center.y;

                    Quaternion roofAngleRot = Quaternion.Euler(0, 0, 0);
                    Quaternion rot = FEngineering.QToWorld(rotationOffset, roofAngleRot);

                    ApplyRandomOffsetsToSpawn(tileSpawn, pos, rot, thisSpawn.LocalScaleMul);

                    SpawnForCell(tileSpawn, preset, pos);
                    //var nCell = GetCellInPosition(tileSpawn.Offset).AddSpawnToCell(tileSpawn);
                }
            }

            if ((SpawnArea & ESpawnArea.BackEdge) != 0)
            {
                for (int x = 0; x < xTiles; x++)
                {
                    SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);
                    Vector3 pos = spawnOrigin;

                    pos.z = bounds.max.z;
                    pos.x += xScaleFactor * x;
                    pos.y = bounds.center.y;

                    Quaternion roofAngleRot = Quaternion.Euler(0, 180, 0);
                    Quaternion rot = FEngineering.QToWorld(rotationOffset, roofAngleRot);

                    ApplyRandomOffsetsToSpawn(tileSpawn, pos, rot, thisSpawn.LocalScaleMul);

                    SpawnForCell(tileSpawn, preset, pos);
                    //var nCell = GetCellInPosition(tileSpawn.Offset).AddSpawnToCell(tileSpawn);
                }
            }

            if ((SpawnArea & ESpawnArea.Top) != 0)
            {
                if (Rotate90)
                {
                    for (int z = 0; z < zTiles; z++)
                    {
                        SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);
                        Vector3 pos = spawnOrigin;

                        pos.z += zScaleFactor * z;
                        pos.x = Mathf.Lerp(bounds.min.x, bounds.max.x, Mathf.InverseLerp(-1f, 1f, RoofCenterOffset));
                        pos.y = bounds.center.y;
                        pos.y += roofHeight;

                        Quaternion roofAngleRot = Quaternion.Euler(0, 90, 0);
                        Quaternion rot = FEngineering.QToWorld(roofAngleRot, rotationOffset);

                        ApplyRandomOffsetsToSpawn(tileSpawn, pos, rot, thisSpawn.LocalScaleMul);

                        SpawnForCell(tileSpawn, preset, pos);
                        //var nCell = GetCellInPosition(tileSpawn.Offset).AddSpawnToCell(tileSpawn);
                    }
                }
                else
                {
                    for (int x = 0; x < xTiles; x++)
                    {
                        SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);
                        Vector3 pos = spawnOrigin;

                        pos.z = Mathf.Lerp(bounds.min.z, bounds.max.z, Mathf.InverseLerp(-1f, 1f, RoofCenterOffset));
                        pos.x += xScaleFactor * x;
                        pos.y = bounds.center.y;
                        pos.y += roofHeight;

                        Quaternion roofAngleRot = Quaternion.Euler(0, 180, 0);
                        Quaternion rot = FEngineering.QToWorld(rotationOffset, roofAngleRot);

                        ApplyRandomOffsetsToSpawn(tileSpawn, pos, rot, thisSpawn.LocalScaleMul);

                        SpawnForCell(tileSpawn, preset, pos);
                        //var nCell = GetCellInPosition(tileSpawn.Offset, 1);
                        //nCell.AddSpawnToCell(tileSpawn);
                        //tileSpawn.GeneratorSpaceToCellSpace(preset, nCell, pos);
                    }
                }
            }
        }

        void SpawnForCell(SpawnData tileSpawn, FieldSetup preset, Vector3 pos)
        {
            var nCell = GetCellInPosition(tileSpawn.Offset, 1);
            nCell.AddSpawnToCell(tileSpawn);
            tileSpawn.GeneratorSpaceToCellSpace(preset, nCell, pos);
        }

        protected void ApplyRandomOffsetsToSpawn(SpawnData tileSpawn, Vector3 refPos, Quaternion refRot, Vector3 scaleRef)
        {
            float range = RandomPRS.x * 10f; refPos += GetRandomOffset(range);
            range = RandomPRS.y * 10f; refRot *= Quaternion.Euler(GetRandomOffset(range));

            range = 1f + RandomPRS.z;
            tileSpawn.LocalScaleMul = Vector3.Scale(scaleRef, GetRandomScaleOffset(range));

            tileSpawn.SetGeneratorSpaceCoords(refPos, refRot);
        }

        protected Vector3 GetRandomOffset(float range)
        {
            return new Vector3(_rand.GetRandomPlusMinus(range), _rand.GetRandomPlusMinus(range), _rand.GetRandomPlusMinus(range));
        }

        protected Vector3 GetRandomScaleOffset(float range)
        {
            return new Vector3(_rand.GetRandom(1f, range), _rand.GetRandom(1f, range), _rand.GetRandom(1f, range));
        }

    }
}