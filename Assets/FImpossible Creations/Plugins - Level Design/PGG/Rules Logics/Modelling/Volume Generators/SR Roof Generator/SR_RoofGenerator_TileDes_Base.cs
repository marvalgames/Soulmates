using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Rules.Modelling
{
    public abstract class SR_RoofGenerator_TileDes_Base : SR_RoofGenerator_Base
    {
        public TileDesignPreset OptionalSetup;

        [HideInInspector] public Material TargetMaterial;
        protected GameObject generatedPrefab = null;

        public override void PreGenerateResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset, FieldSpawner callFrom)
        {
            base.PreGenerateResetRule(grid, preset, callFrom);
            if (generatedPrefab) FGenerators.DestroyObject(generatedPrefab);
        }

        protected override void OnGenerateRoof(List<SpawnData> indicators, FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            base.OnGenerateRoof(indicators, mod, ref thisSpawn, preset, cell, grid);
            thisSpawn.Prefab = generatedPrefab;
        }

    }
}