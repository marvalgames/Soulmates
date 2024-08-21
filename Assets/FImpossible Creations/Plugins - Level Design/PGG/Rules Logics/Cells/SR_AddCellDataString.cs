using UnityEngine;

namespace FIMSpace.Generating.Rules.Cells
{
    public class SR_AddCellDataString : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "Add Cell Data String"; }
        public override string Tooltip() { return "Injecting cell data for current grid cell if all other nodes conditions are met"; }

        public string CellDataString = "";

        public EProcedureType Type { get { return EProcedureType.OnConditionsMet; } }

        [HideInInspector] public CheckCellsSelectorSetup checkSetup = new CheckCellsSelectorSetup(true, false);


        #region Editor stuff
#if UNITY_EDITOR

        public override void NodeHeader()
        {
            base.NodeHeader();
            checkSetup.UseCondition = false;
            DrawMultiCellSelector(checkSetup, OwnerSpawner);
        }
#endif
        #endregion


        public override void OnConditionsMetAction(FieldModification mod, ref SpawnData thisSpawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid)
        {
            CellSelector_Execute(checkSetup, grid, cell, cell, thisSpawn, (FieldCell c, SpawnData s) => AddData(c) );
        }

        public void AddData(FieldCell cell)
        {
            cell.AddCustomData(CellDataString);
        }

    }
}