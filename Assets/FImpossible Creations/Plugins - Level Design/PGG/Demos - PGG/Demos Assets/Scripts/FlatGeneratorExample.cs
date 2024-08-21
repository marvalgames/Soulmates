using FIMSpace.Generating.RectOfFields;
using FIMSpace.Hidden;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class FlatGeneratorExample : PGGGeneratorBase
    {
        public override FGenGraph<FieldCell, FGenPoint> PGG_Grid { get { return null; } }
        public override FieldSetup PGG_Setup { get { return null; } }

        public RectangleOfFieldsGenerator InteriorSetupZeroLevel;
        public RectangleOfFieldsGenerator InteriorSetupUpLevels;
        public RectangleOfFieldsGenerator InteriorSetupRoofLevel;
        public SimpleFieldGenerator ExteriorGenerator;
        [Range(4, 10)] public int LevelsToGenerate = 5;

        public override void GenerateObjects()
        {
            Prepare();
            ClearGenerated();

            int seed = Seed;

            // Getting variables helpful for generating exterior
            Vector3Int size = ExteriorGenerator.FieldSizeInCells;
            size.y = LevelsToGenerate;

            // That many cells in Y axis as levels to generate we choosed
            ExteriorGenerator.FieldSizeInCells = size;

            #region Exterior Specific

            // Temporary turn off door generation for custom placement (had to be implemented inside FieldPreset with custom variable)
            int varId = ExteriorGenerator.FieldPreset.GetVariableIndex("Custom Gates");
            float pre = 1f; if (varId != -1) pre = ExteriorGenerator.FieldPreset.Variables[varId].GetFloatValue();
            if (varId != -1) ExteriorGenerator.FieldPreset.Variables[varId].SetValue(1f);

            // Generating instructions for generating entrances
            List<SpawnInstruction> guides = new List<SpawnInstruction>();

            SpawnInstruction front = new SpawnInstruction();
            front.gridPosition = (InteriorSetupZeroLevel.CorridorGuide.Start.V2toV3Int().Divide(2)) + new Vector3Int(-1, 0, 0); // Div by 2 because exterior cells are 4 size and interiors are 2 size
            front.desiredDirection = InteriorSetupZeroLevel.CorridorGuide.StartDir.GetDirection().V3toV3Int();
            front.useDirection = true;
            front.definition = ExteriorGenerator.FieldPreset.CellsCommands[0];
            guides.Add(front);

            SpawnInstruction back = front;
            back.gridPosition = InteriorSetupZeroLevel.CorridorGuide.End.V2toV3Int().Divide(2)+ new Vector3Int(-1, 0, 0);
            back.desiredDirection = InteriorSetupZeroLevel.CorridorGuide.EndDir.GetDirection().V3toV3Int();
            guides.Add(back);

            ExteriorGenerator.Seed = seed;
            ExteriorGenerator.Generate(guides);
            Generated.Add(ExteriorGenerator.Generated);
            ExteriorGenerator.Generated = null;

            // Restoring variable
            if (varId != -1) ExteriorGenerator.FieldPreset.Variables[varId].SetValue(pre);

            #endregion

            // Now generating first level, then using level 1 generator for all floors and finishing top with top level generator
            InteriorSetupZeroLevel.GenerateObjects();
            CopyToGenerated(InteriorSetupZeroLevel.Generated, "Zero");
            InteriorSetupZeroLevel.ClearGenerated(false);
            seed += 1;

            for (int i = 1; i < LevelsToGenerate + 1; i++)
            {
                InteriorSetupUpLevels.Seed = seed;
                InteriorSetupUpLevels.CorridorGuide.Start = InteriorSetupZeroLevel.CorridorGuide.Start + new Vector2Int(0, FGenerators.GetRandom(0,1) );
                InteriorSetupUpLevels.CorridorGuide.End = InteriorSetupZeroLevel.CorridorGuide.End + new Vector2Int(FGenerators.GetRandom(-6,4), FGenerators.GetRandom(-2,0) );
                InteriorSetupUpLevels.transform.position = InteriorSetupZeroLevel.transform.position + Vector3.up * (i * 6); // 6 is Height of one exterior field setup level
                InteriorSetupUpLevels.GenerateObjects();
                CopyToGenerated(InteriorSetupUpLevels.Generated, i.ToString() );
                InteriorSetupUpLevels.ClearGenerated(false);
                seed += 1;
            }

            // Finishing with roof level
            InteriorSetupRoofLevel.Seed = seed;
            InteriorSetupRoofLevel.transform.position  = InteriorSetupZeroLevel.transform.position + Vector3.up * ((LevelsToGenerate + 1) * 6 /*2 is to fit roof offset*/);
            InteriorSetupRoofLevel.GenerateObjects();
            CopyToGenerated(InteriorSetupRoofLevel.Generated, "-Roof");
            InteriorSetupRoofLevel.ClearGenerated(false);

            base.GenerateObjects();
        }

        public void CopyToGenerated(List<InstantiatedFieldInfo> gen, string id)
        {
            Transform newContainer = new GameObject("Level " + id + " - Container").transform;
            newContainer.SetParent(transform);
            newContainer.ResetCoords();
            Generated[0].Instantiated.Add(newContainer.gameObject);

            for (int i = 0; i < gen.Count; i++)
            {
                gen[i].MainContainer.transform.SetParent(newContainer, true);
                Generated.Add(gen[i]);
            }
        }


    }


#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(FlatGeneratorExample))]
    public class FlatGeneratorExampleEditor : PGGGeneratorBaseEditor
    {
        // Easy access to parent script if custom coding inspector window
        public FlatGeneratorExample Get { get { if (_get == null) _get = (FlatGeneratorExample)target; return _get; } }
        private FlatGeneratorExample _get;

        protected override void DrawGUIFooter()
        {
            base.DrawGUIFooter();
            DrawGeneratingButtons(false);
        }
    }
#endif

}