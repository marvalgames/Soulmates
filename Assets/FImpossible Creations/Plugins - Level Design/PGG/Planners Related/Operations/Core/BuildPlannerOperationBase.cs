using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    public abstract class BuildPlannerOperationBase : ScriptableObject
    {
        public virtual string Description => "";

        /// <summary> Called before running field planners. </summary>
        public virtual void OnPrepareBuildPlan(BuildPlannerPreset planner, BuildPlannerOperationHelper helper) { }

        /// <summary> Called after running all field planners procedures </summary>
        public virtual void OnBuildPlanComplete(BuildPlannerPreset planner, BuildPlannerOperationHelper helper) { }

        /// <summary> Called after generating all field planners grids </summary>
        public virtual void OnBuildPlannerExecutorComplete(BuildPlannerPreset planner, BuildPlannerExecutor executor, BuildPlannerOperationHelper helper) { }

        /// <summary> Editor only - operation foldout display </summary>
        public virtual bool Foldable => true;

        /// <summary> Editor only - body for GUI code. Use Helper.RequestVariable for unique values </summary>
        public virtual void Editor_DisplayGUI(BuildPlannerOperationHelper helper, BuildPlannerPreset planner) { }
    }

    [System.Serializable]
    public class BuildPlannerOperationHelper
    {
        public bool Enabled = true;
        public bool Foldout = true;
        public BuildPlannerOperationBase Operation;

        [SerializeField] private List<FieldVariable> variables = new List<FieldVariable>();

        /// <summary> Can be used for containing any parasable value or just strings </summary>
        [SerializeField, HideInInspector] public List<string> customStringList = null;
        /// <summary> Support for list of unity objects </summary>
        [SerializeField, HideInInspector] public List<UnityEngine.Object> customObjectList = null;

        public FieldVariable RequestVariable(string name, object defaultValue)
        {
            int hash = name.GetHashCode();
            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i].GetNameHash == hash) return variables[i];
            }

            FieldVariable nVar = new FieldVariable(name, defaultValue);
            variables.Add(nVar);
            return nVar;
        }
    }

}