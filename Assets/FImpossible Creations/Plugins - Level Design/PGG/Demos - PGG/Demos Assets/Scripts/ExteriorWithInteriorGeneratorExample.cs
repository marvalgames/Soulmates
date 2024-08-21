using FIMSpace.Generating.Planning;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class ExteriorWithInteriorGeneratorExample : PGGGeneratorBase
    {
        public SimpleFieldGenerator_GenImplemented Exterior;
        public RectangleOfFieldsGenerator Interior;

        public override FGenGraph<FieldCell, FGenPoint> PGG_Grid { get { return null; } }
        public override FieldSetup PGG_Setup { get { return null; } }


        public override void GenerateObjects()
        {
            Prepare();
            ClearGenerated();

            #region Prepare generators

            Exterior.RandomSeed = false;
            Exterior.GenerateOnStart = false;
            Exterior.AutoRefresh = false;
            Exterior.Seed = Seed;

            Interior.RandomSeed = false;
            Interior.GenerateOnStart = false;
            Interior.AutoRefresh = false;
            Interior.Seed = Seed;

            #endregion


            // First generate exterior setup, then generate interior rooms
            // with windows and doors in correct placement thanks to already generated exterior grid's info

            Exterior.ClearGenerated();
            Exterior.GenerateObjects();
            Generated.Add(Exterior.Generated[0]);

            // Find grid positions of generated doors and windows by exterior generator
            var exteriorGrid = Exterior.Generated[0].Grid;

            // Prepare doors and window guides for interior generator
            List<SpawnInstruction> instructions = new List<SpawnInstruction>();

            // Getting door/window cell from exterior grid and preparing guide for interior to have inside door/window placed in right position
            for (int i = 0; i < exteriorGrid.AllApprovedCells.Count; i++)
            {
                var cell = exteriorGrid.AllApprovedCells[i];
                var doorSpawn = SpawnRuleBase.GetSpawnDataWithTag(cell, "Door");
                if (FGenerators.CheckIfExist_NOTNULL(doorSpawn ))
                {
                    // Guide for doorway
                    SpawnInstruction instr = new SpawnInstruction();
                    instr.gridPosition = cell.Pos + Vector3Int.right;
                    instr.desiredDirection = doorSpawn.GetFullOffset().normalized.V3toV3Int();
                    instr.useDirection = true;
                    instr.helperType = EHelperGuideType.Doors;
                    instructions.Add(instr);
                }
                else
                {
                    // Guide for window hole
                    var windowSpawn = SpawnRuleBase.GetSpawnDataWithTag(cell, "Window");
                    if (FGenerators.CheckIfExist_NOTNULL( windowSpawn ))
                    {
                        SpawnInstruction instr = new SpawnInstruction();
                        instr.gridPosition = cell.Pos + Vector3Int.right;
                        instr.desiredDirection = windowSpawn.GetFullOffset().normalized.V3toV3Int();
                        instr.useDirection = true;
                        instr.helperType = EHelperGuideType.Spawn;
                        instructions.Add(instr);
                    }
                }
            }

            Interior.Prepare();

            // Guides to grid data of interior generator for putting doors/windows in right positions
            Interior.HelpGuides = instructions;

            // Move generated data from interior generator to this component's list
            Interior.GenerateObjects();
            PGGUtils.TransferFromListToList<InstantiatedFieldInfo>(Interior.Generated, Generated);

            base.GenerateObjects(); // Triggering event if assigned
        }


    }


    #region Editor Inspector Window

#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(ExteriorWithInteriorGeneratorExample))]
    public class ExteriorWithInteriorGeneratorExampleEditor : PGGGeneratorBaseEditor
    {
        protected override void DrawGUIHeader()
        {
            UnityEditor.EditorGUILayout.HelpBox("Interior and exterior setup must have same cell size", UnityEditor.MessageType.None);
            UnityEditor.EditorGUILayout.HelpBox("Interior is generated normally, then interior creates inside modules for doors/windows in correct placement in order to exterior modules", UnityEditor.MessageType.None);
            base.DrawGUIHeader();
        }

        protected override void DrawGUIBeforeDefaultInspector()
        {
            GUILayout.Space(3);
            base.DrawGUIBeforeDefaultInspector();
        }

        protected override void DrawGUIFooter()
        {
            GUILayout.Space(7);
            DrawGeneratingButtons(false);
            base.DrawGUIFooter();
        }
    }
#endif

    #endregion

}