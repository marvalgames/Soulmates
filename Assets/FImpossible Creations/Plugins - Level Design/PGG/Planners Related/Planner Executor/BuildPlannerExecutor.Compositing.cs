using System.Collections.Generic;
using UnityEngine;
using FIMSpace.Generating.Planning.GeneratingLogics;

namespace FIMSpace.Generating
{
    public partial class BuildPlannerExecutor : MonoBehaviour
    {

        public void ValidateSetups()
        {
            if (BuildPlannerPreset)
            {
                if (BuildPlannerPreset.BasePlanners.Count == _plannerPrepare.FieldSetupCompositions.Count)
                    for (int i = 0; i < BuildPlannerPreset.BasePlanners.Count; i++)
                    {
                        if (_plannerPrepare.FieldSetupCompositions[i] != null)
                        {
                            if (_plannerPrepare.FieldSetupCompositions[i].Prepared)
                                _plannerPrepare.FieldSetupCompositions[i].RefreshWith(this, BuildPlannerPreset.BasePlanners[i]);
                        }
                    }

                AdjustDuplicatesCounts();

                for (int i = 0; i < BuildPlannerPreset.BasePlanners.Count; i++)
                {
                    if (_plannerPrepare.FieldSetupCompositions[i] != null)
                    {
                        _plannerPrepare.FieldSetupCompositions[i].ValidateComposition(-1);
                    }
                }
            }
        }


        public void AdjustDuplicatesCounts()
        {
            if (_plannerPrepare.UseDuplicatesOverrides.Count != BuildPlannerPreset.BasePlanners.Count)
                PGGUtils.AdjustStructsListCount(_plannerPrepare.UseDuplicatesOverrides, BuildPlannerPreset.BasePlanners.Count, false);

            if (_plannerPrepare.DuplicatesOverrides.Count != BuildPlannerPreset.BasePlanners.Count)
                PGGUtils.AdjustCount(_plannerPrepare.DuplicatesOverrides, BuildPlannerPreset.BasePlanners.Count, false);

            if (_plannerPrepare.DuplicatesOverrides.Count != BuildPlannerPreset.BasePlanners.Count)
                PGGUtils.AdjustCount(_plannerPrepare.DuplicatesOverrides, BuildPlannerPreset.BasePlanners.Count, true);
        }

        public void AdjustTargetDuplicatesCount(int i)
        {
            FieldSetupComposition selected = _plannerPrepare.FieldSetupCompositions[i];

            if (_plannerPrepare.DuplicatesOverrides[i] == null)
                _plannerPrepare.DuplicatesOverrides[i] = new BuildPlannerExecutor.PlannerDuplicatesSupport();

            if (_plannerPrepare.DuplicatesOverrides[i].DuplicatesCompositions == null)
                _plannerPrepare.DuplicatesOverrides[i].DuplicatesCompositions = new List<FieldSetupComposition>();

            if (_plannerPrepare.DuplicatesOverrides[i].DuplicatesCompositions.Count != selected.Duplicates)
                PGGUtils.AdjustCount(_plannerPrepare.DuplicatesOverrides[i].DuplicatesCompositions, selected.Duplicates, false);

            if (_plannerPrepare.DuplicatesOverrides[i].DuplicatesShapes.Count != selected.Duplicates)
                PGGUtils.AdjustUnityObjCount(_plannerPrepare.DuplicatesOverrides[i].DuplicatesShapes, selected.Duplicates, null);
        }





        #region Preset Settings Related

        public void OnPlannerChange()
        {
            ResetPlannerComposition();
        }

        public bool ResetPlannerComposition()
        {
            bool changed = false;
            if (BuildPlannerPreset == null) return changed;

            if (_plannerPrepare == null) _plannerPrepare = new PlannerPreparation();

            if (_plannerPrepare.FieldSetupCompositions.Count != BuildPlannerPreset.BasePlanners.Count)
            {
                PGGUtils.AdjustCount(_plannerPrepare.FieldSetupCompositions, BuildPlannerPreset.BasePlanners.Count);

                #region Define types inheriting planner setup

                for (int i = 0; i < _plannerPrepare.FieldSetupCompositions.Count; i++)
                {
                    var compos = _plannerPrepare.FieldSetupCompositions[i];

                    if (BuildPlannerPreset.BasePlanners[i])
                    {
                        if (BuildPlannerPreset.BasePlanners[i].FieldType == Planning.FieldPlanner.EFieldType.Prefab)
                        {
                            compos.GenType = EPGGGenType.Prefab;
                            //compos.RefreshPrefabsSpawnSetup();
                            //compos.CustomSpawn.SetSpawn(BuildPlannerPreset.BasePlanners[i].DefaultPrefab);
                        }
                    }

                }

                #endregion

                changed = true;
            }

            if (_plannerPrepare.FieldSetupCompositions.Count == BuildPlannerPreset.BasePlanners.Count)
                for (int i = 0; i < _plannerPrepare.FieldSetupCompositions.Count; i++)
                {
                    var compos = _plannerPrepare.FieldSetupCompositions[i];
                    if (compos.ParentFieldPlanner != null) if ( compos.ParentFieldPlanner.FieldType == Planning.FieldPlanner.EFieldType.Prefab) compos.GenType = EPGGGenType.Prefab;


                    if (compos.GenType == EPGGGenType.Prefab)
                    {
                        compos.RefreshWith(this, BuildPlannerPreset.BasePlanners[i]);
                        //compos.RefreshPrefabsSpawnSetup();
                        //if (compos.CustomSpawn.GetSpawn() == null)
                        //{
                            if (BuildPlannerPreset.BasePlanners[i].DefaultFieldSetup != null)
                            {
                                compos.PrepareWithPrefabList(this, BuildPlannerPreset.BasePlanners[i]);
                                changed = true;
                            }
                        //}
                    }
                    else
                    {
                        if (compos.Setup == null)
                        {
                            if (BuildPlannerPreset.BasePlanners[i].DefaultFieldSetup != null)
                            {
                                compos.RefreshWith(this, BuildPlannerPreset.BasePlanners[i]);
                                changed = true;
                            }
                            else if (BuildPlannerPreset.BasePlanners[i].FieldType == Planning.FieldPlanner.EFieldType.Prefab)
                            {
                                compos.RefreshWith(this, BuildPlannerPreset.BasePlanners[i]);
                                changed = true;
                            }
                        }
                    }

                    //else
                    {
                        if (BuildPlannerPreset.BasePlanners[i].ParentBuildPlanner != null)
                        {
                            if (compos.PlannerVariablesOverrides == null) compos.PlannerVariablesOverrides = new List<FieldVariable>();
                            FieldVariable.UpdateVariablesWith(compos.PlannerVariablesOverrides, BuildPlannerPreset.BasePlanners[i].FVariables);
                        }
                    }
                }


            if (_plannerPrepare.PlannerVariablesOverrides == null) _plannerPrepare.PlannerVariablesOverrides = new List<FieldVariable>();
            FieldVariable.UpdateVariablesWith(_plannerPrepare.PlannerVariablesOverrides, BuildPlannerPreset.BuildVariables);

            return changed;
        }

        #endregion



        /// <summary>
        /// Class for setting up target FieldSetups to generate on planner preset
        /// </summary>
        [System.Serializable]
        public class PlannerPreparation
        {
            public List<FieldVariable> PlannerVariablesOverrides = new List<FieldVariable>();
            public List<FieldSetupComposition> FieldSetupCompositions = new List<FieldSetupComposition>();
            public List<bool> UseDuplicatesOverrides = new List<bool>();
            public List<PlannerDuplicatesSupport> DuplicatesOverrides = new List<PlannerDuplicatesSupport>();
        }

        [System.Serializable]
        public class PlannerDuplicatesSupport
        {
            public List<FieldSetupComposition> DuplicatesCompositions = new List<FieldSetupComposition>();
            public List<ShapeGeneratorBase> DuplicatesShapes = new List<ShapeGeneratorBase>();
        }


    }

}
