#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Rules.Modelling
{
    public class SR_RoofGeneratorPlacer : SR_RoofGenerator_Base
    {
        public override string TitleName() { return "Roof Generator/Roof Generator - Placer"; }

        [Space(4)]
        [PGG_SingleLineSwitch("PaddingScale", 58, "Select if you want to align padding spacing with cell size or world units", 140)]
        public Vector3 ModelTilePadding = new Vector3(1f, 1f, 1f);
        [HideInInspector] public ESR_Measuring PaddingScale = ESR_Measuring.Cells;
        //public Vector3 ScaleModel = new Vector3(1f, 1f, 1f);
        public Vector3 RotateTile = new Vector3(0, 0, 0);

        [Space(4)]
        public Vector3 ExtraOffset = new Vector3(0, 0, 0);

        [Tooltip("X is Position  Y is Rotation  Z is Scale")]
        public Vector3 RandomPRS = new Vector3(0, 0, 0);


        private FGenerators.DefinedRandom _rand = null;

        [HideInInspector] public bool ResizeTiles = true;
        [HideInInspector] public ESR_TileScaleAxis SwizzleScaleX = ESR_TileScaleAxis.X;
        [HideInInspector] public ESR_TileScaleAxis SwizzleScaleY = ESR_TileScaleAxis.Y;
        [HideInInspector] public ESR_TileScaleAxis SwizzleScaleZ = ESR_TileScaleAxis.Z;
        [HideInInspector][Range(0f,1f)] public float CenterSizeOffset = 0f;

        [HideInInspector] public bool FrontSide = true;
        [HideInInspector] public bool BackSide = true;

        public static Vector3 GetSwizzled(Vector3 input, ESR_TileScaleAxis x, ESR_TileScaleAxis y, ESR_TileScaleAxis z)
        {
            Vector3 output = input;
            if (x == ESR_TileScaleAxis.Y) output.x = input.y; else if (x == ESR_TileScaleAxis.Z) output.x = input.z;
            if (y == ESR_TileScaleAxis.X) output.y = input.x; else if (y == ESR_TileScaleAxis.Z) output.y = input.z;
            if (z == ESR_TileScaleAxis.X) output.z = input.x; else if (z == ESR_TileScaleAxis.Y) output.z = input.y;
            return output;
        }


        #region Editor GUI

#if UNITY_EDITOR

        SerializedProperty sp_Swizzle = null;
        SerializedProperty sp_FrontSide = null;
        public override void NodeBody(SerializedObject so)
        {
            EditorGUILayout.HelpBox("Generating adjusted tiles with use of prefabs, to align roof shape.", MessageType.None);
            EditorGUILayout.HelpBox("Tiles generated with this node are not readable by other spawners and are ignoring cells tag rules!", MessageType.None);
            base.NodeBody(so);
        }

        public override void NodeFooter(SerializedObject so, FieldModification mod)
        {
            if (sp_Swizzle == null) sp_Swizzle = so.FindProperty("ResizeTiles");
            SerializedProperty spc = sp_Swizzle.Copy();

            EditorGUILayout.PropertyField(sp_Swizzle);

            if (sp_Swizzle.boolValue)
            {
                spc.Next(false);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Swizzle Tile Scale Axis:", "If you want to scale width of the tile, but your model orientation scales different axis instead, you can adjust it with swizzling."), GUILayout.Width(160));
                EditorGUILayout.PropertyField(spc, GUIContent.none, GUILayout.MinWidth(60)); spc.Next(false);
                EditorGUILayout.PropertyField(spc, GUIContent.none, GUILayout.MinWidth(60)); spc.Next(false);
                EditorGUILayout.PropertyField(spc, GUIContent.none, GUILayout.MinWidth(60)); spc.Next(false);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(spc); 
            }
            else
            {
                spc.Next(false);
                spc.Next(false);
                spc.Next(false);
                spc.Next(false);
                EditorGUILayout.PropertyField(spc);
            }

            if (sp_FrontSide == null) sp_FrontSide = so.FindProperty("FrontSide");
            spc = sp_FrontSide.Copy();
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(spc); spc.Next(false);
            GUILayout.Space(14);
            EditorGUILayout.PropertyField(spc);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;

            base.NodeFooter(so, mod);
        }

#endif

        #endregion


        Quaternion rot90 = Quaternion.identity;
        Quaternion rot90Revert = Quaternion.identity;
        Bounds boundsNotRot;
        Vector3 sourceSpawnScale;
        protected override void PrepareMesh(FieldSetup preset, ref SpawnData thisSpawn, FieldCell cell)
        {
            _rand = new FGenerators.DefinedRandom(FGenerators.LatestSeed + 500);
            sourceSpawnScale = thisSpawn.LocalScaleMul;

            thisSpawn.Enabled = false;

            // todo Get rotated bounds, indicator matrix
            Bounds bounds = indicatorsBounds;
            Bounds boundsBackup = bounds;

            bounds = new Bounds(indicatorsBounds.center, Vector3.zero);
            bounds.Encapsulate(boundsBackup.min + new Vector3(-ExtraOffset.x, 0, -ExtraOffset.z));
            bounds.Encapsulate(boundsBackup.max + new Vector3(ExtraOffset.x, 0, ExtraOffset.z));


            boundsNotRot = bounds;

            if (Rotate90)
            {
                rot90 = Quaternion.Euler(0f, 90f, 0f);
                rot90Revert = Quaternion.Inverse(rot90);
                bounds = FEngineering.RotateLocalBounds(bounds, rot90);
            }
            else
            {
                rot90 = Quaternion.identity;
                rot90Revert = Quaternion.identity;
            }

            float roofCenterShift = RoofCenterVariable.GetValue(0f) == 0f ? RoofCenterOffset : RoofCenterVariable.GetValue(0f);
            if (FrontSide) PrepareAndDoSpawning(cell, preset, bounds, thisSpawn, roofCenterShift, true);
            if (BackSide) PrepareAndDoSpawning(cell,preset, bounds, thisSpawn, roofCenterShift, false);

        }

        void PrepareAndDoSpawning(FieldCell cell, FieldSetup preset, Bounds bounds, SpawnData thisSpawn, float roofOffset, bool frontSide)
        {
            if (roofOffset < -0.99999f) roofOffset = -0.99999f; else if (roofOffset > 0.99999f) roofOffset = 0.99999f;
            if (!frontSide) roofOffset = -roofOffset;

            Vector3 cellSize = preset.GetCellUnitSize();
            Vector3 tilePadding = ModelTilePadding;
            if (PaddingScale == ESR_Measuring.Cells) tilePadding = Vector3.Scale(ModelTilePadding, cellSize);

            Vector3 minRef = bounds.min;
            Vector3 maxRef = bounds.max;

            if (!frontSide)
            {
                minRef.z = -bounds.max.z;
                maxRef.z = -bounds.min.z;
            }

            Vector3 spawnOrigin = minRef;

            float roofProgr = Mathf.InverseLerp(-1f, 1f, roofOffset);

            if (frontSide) roofProgr = Mathf.Lerp(roofProgr, -1f, CenterSizeOffset);
            else roofProgr = Mathf.Lerp(roofProgr, -1f, CenterSizeOffset);



            float finalSizeZ = bounds.size.z;
            finalSizeZ = Mathf.Lerp(0f, finalSizeZ, roofProgr);

            int xTiles = Mathf.RoundToInt(bounds.size.x / tilePadding.x);
            if (xTiles < 1) xTiles = 1;

            int zTiles = Mathf.RoundToInt(finalSizeZ / tilePadding.z);
            if (zTiles < 1) zTiles = 1;

            float zScaleFactor = finalSizeZ / (zTiles);
            float xScaleFactor = bounds.size.x / (xTiles);

            Vector3 originOffset = Vector3.zero;
            originOffset.x = xScaleFactor / 2f;
            originOffset.z = (finalSizeZ / zTiles) / 2f;


            // Roof rotation
            Vector3 roofMinStart = minRef; roofMinStart.x = bounds.center.x;

            Vector3 roofCenterTop = bounds.center;
            roofCenterTop.y = bounds.center.y + RoofHeight;

            roofCenterTop.z = Mathf.Lerp(minRef.z, maxRef.z, roofProgr);

            Vector3 roofCenterOffsetted = bounds.center;
            roofCenterOffsetted.z = roofCenterTop.z;

            float roofSideLength = Vector3.Distance(roofMinStart, roofCenterTop);
            float centerToSideLength = Vector3.Distance(roofMinStart, roofCenterOffsetted);
            float lengthRatio = roofSideLength / centerToSideLength;

            Quaternion toRoofTop = Quaternion.LookRotation(roofCenterTop - roofMinStart);
            float roofAngle = toRoofTop.eulerAngles.x;
            originOffset.y += (RoofHeight / 2f) * (1f / (zTiles));
            spawnOrigin += originOffset;

            Quaternion rotationOffset = Quaternion.identity;
            if (RotateTile != Vector3.one) rotationOffset *= Quaternion.Euler(RotateTile);

            Vector3 scaleVector = new Vector3(xScaleFactor, 1f, zScaleFactor * lengthRatio);
            scaleVector = Vector3.Scale(thisSpawn.LocalScaleMul, scaleVector);

            Vector3 swizzledScale = Vector3.one;
            if ( ResizeTiles) swizzledScale = GetSwizzled(scaleVector, SwizzleScaleX, SwizzleScaleY, SwizzleScaleZ);
            Vector3 mxMainOff = Vector3.zero;

            Matrix4x4 mx = Matrix4x4.TRS(mxMainOff, Quaternion.Euler(0f, 0f, 0f), Vector3.one);

            if (!frontSide)
            {
                Vector3 mxOffset = Vector3.zero;
                mxOffset.x += bounds.max.x;
                mxOffset.x += bounds.min.x;
                mx = Matrix4x4.TRS(mxMainOff + mxOffset, Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            }

            DoSpawning(cell, mx, preset, spawnOrigin, xScaleFactor, zScaleFactor, xTiles, zTiles, swizzledScale, roofAngle, rotationOffset);
        }

        void DoSpawning(FieldCell cell, Matrix4x4 mx, FieldSetup preset, Vector3 spawnOrigin, float xScaleFactor, float zScaleFactor, int xTiles, int zTiles, Vector3 swizzledScale, float roofAngle, Quaternion rotationOffset)
        {
            Quaternion roofAngleRot = mx.rotation * Quaternion.Euler(roofAngle, 0, 0);
            Quaternion rot = roofAngleRot;// FEngineering.QToWorld(rotationOffset, roofAngleRot);

            Quaternion extraOffRot = Quaternion.identity;
            if (Rotate90) extraOffRot = Quaternion.Euler(0f, 90f, 0f);

            for (int x = 0; x < xTiles; x++)
            {
                for (int z = 0; z < zTiles; z++)
                {
                    float toCenterProgress = (float)z / (float)(zTiles);

                    SpawnData tileSpawn = OwnerSpawner.GenerateSpawnerSpawnData(_roof_Mod, preset, _roof_Cell);

                    Vector3 pos = spawnOrigin;
                    pos.x += xScaleFactor * x; // Move by single tile unit size
                    pos.z += zScaleFactor * z;

                    pos.y += RoofHeight * toCenterProgress + ExtraOffset.y;

                    float range = RandomPRS.x * 10f;
                    pos += new Vector3(_rand.GetRandomPlusMinus(range), _rand.GetRandomPlusMinus(range), _rand.GetRandomPlusMinus(range));
                    range = RandomPRS.y * 10f;
                    Quaternion newRot = extraOffRot * rot * Quaternion.Euler(_rand.GetRandomPlusMinus(range), _rand.GetRandomPlusMinus(range), _rand.GetRandomPlusMinus(range));

                    if (ResizeTiles)
                    {
                        range = 1f + RandomPRS.z;
                        swizzledScale = Vector3.Scale(swizzledScale, new Vector3(_rand.GetRandom(1f, range), _rand.GetRandom(1f, range), _rand.GetRandom(1f, range)));
                        tileSpawn.LocalScaleMul = swizzledScale; // Apply scale adjustement
                    }
                    else
                    {
                        tileSpawn.LocalScaleMul = sourceSpawnScale;
                    }

                    pos = mx.MultiplyPoint3x4(pos);
                    if (Rotate90) pos = RotateAround(pos, boundsNotRot.center, extraOffRot);
                    //pos = Matrix4x4.TRS(boundsNotRot.center, Quaternion.Euler(0f, 90f, 0f), Vector3.one).MultiplyPoint3x4(pos);
                    tileSpawn.SetGeneratorSpaceCoords(pos, newRot);
                    //todo add stigma for roof generator random rotation etc.
                    FieldCell nCell = GetCellInPosition(tileSpawn.Offset, 1);
                    nCell.AddSpawnToCell(tileSpawn);
                    tileSpawn.GeneratorSpaceToCellSpace(preset, nCell, pos);
                    tileSpawn.LocalRotationOffset = rotationOffset.eulerAngles;
                }
            }
        }


    }
}