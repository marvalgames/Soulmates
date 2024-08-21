using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    public abstract class FieldPlannerOperationBase : ScriptableObject
    {
        public virtual string Description => "";

        /// <summary> Called before running field planner - just the main instance! </summary>
        public virtual void OnStartPrepareFieldPlannerMainInstance(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper) { }

        /// <summary> Called before running field planner - called for each instance! </summary>
        public virtual void OnStartPrepareFieldPlanner(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper) { }

        /// <summary> Called after running all field planner procedures (all pre and post procedures) - called for each instance </summary>
        public virtual void OnFieldPlannerComplete(BuildPlannerPreset build, FieldPlanner planner, FieldPlannerOperationHelper helper) { }

        /// <summary> Called after generating field planners grid objects - called for each instance </summary>
        /// <param name="helper"> Use helper.RequestVariable for unique value per planner </param>
        /// <param name="generator"> (Null if spawning prefabs instead of grids) You can cast it to GridPainter or FlexibleGridPainter depending what was used by executor </param>
        public virtual void OnPlannerGeneringComplete(FieldPlannerOperationHelper helper, BuildPlannerPreset build, BuildPlannerExecutor executor, FieldPlanner planner, PGGGeneratorRoot generator) { }

        /// <summary> Editor only - operation foldout display </summary>
        public virtual bool Foldable => true;

        /// <summary> Editor only - body for GUI code. Use Helper.RequestVariable for unique values </summary>
        public virtual void Editor_DisplayGUI(FieldPlannerOperationHelper helper, BuildPlannerPreset build, FieldPlanner planner) { }
    }

    [System.Serializable]
    public class FieldPlannerOperationHelper
    {
        public bool Enabled = true;
        public bool Foldout = true;
        public FieldPlannerOperationBase Operation;

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