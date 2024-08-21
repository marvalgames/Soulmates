using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Planning.PlannerNodes;
using System;
using FIMSpace.Graph;
using FIMSpace.Generating.Checker;

namespace FIMSpace.Generating.Planning
{
    public partial class FieldPlanner
    {
        /// <summary> If this planner is an duplicate, then parent will be the source planner </summary>
        [System.NonSerialized] public FieldPlanner DuplicateParent = null;
        [System.NonSerialized] private List<FieldPlanner> duplicatePlanners = null;
        /// <summary> True on procedures execution start </summary>
        [System.NonSerialized] public bool WasPreExecuted = false;
        /// <summary> True after first executions </summary>
        [System.NonSerialized] public bool WasCalled = false;
        [System.NonSerialized] public bool Discarded = false;

        /// <summary> If preapred, already executed and not discarded </summary>
        public bool Available { get { return !DisableWholePlanner && WasPreExecuted && !Discarded; } }

        /// <summary> If preapred and not discarded </summary>
        public bool AvailableBypassWasExecuted { get { return !DisableWholePlanner && !Discarded; } }
        public bool IsDuplicate { get { return DuplicateParent != null; } }

        /// <summary> Checks if is null or disabled or discarded </summary>
        public static bool CantExecute(FieldPlanner f)
        {
            if (f == null) return true;
            if (f.Discarded) return true;
            if (f.DisableWholePlanner) return true;
            return false;
        }


        /// <summary> Called when generating scene objects with grid painter, the object is generator component (grid painter) </summary>
        public List<Action<object>> OnGeneratingEvents { get; private set; }
        /// <summary> Action which will be called when generating scene objects with grid painter, the 'object' is generator component (grid painter) </summary>
        public void AddOnGeneratingEvent(Action<object> action)
        {
            if (OnGeneratingEvents == null) OnGeneratingEvents = new List<Action<object>>();
            OnGeneratingEvents.Add(action);
        }


        public FieldPlanner GetPlannerByUniqueID(int uniqueId)
        {
            if (uniqueId == -1) return null;
            if (ParentBuildPlanner == null) return null;

            for (int i = 0; i < ParentBuildPlanner.BasePlanners.Count; i++)
            {
                var pl = ParentBuildPlanner.BasePlanners[i];
                if (i == uniqueId) return pl;
            }

            return null;
        }

        string[] executionTimeTags = null;
        public bool IsTagged(string checkTag)
        {
            if (executionTimeTags == null)
            {
                executionTimeTags = this.tag.Split(',');
            }

            if (executionTimeTags.Length == 0) return checkTag == "";

            //bool wasNegCheck = false;
            //bool allNegNotDetected = true;
            bool neg = false;
            if (checkTag.Length > 0)
            {
                neg = checkTag[0] == '!';
                if (neg) checkTag = checkTag.Remove(0, 1);
            }

            for (int i = 0; i < executionTimeTags.Length; i++)
            {
                if (executionTimeTags[i] == checkTag) return !neg;
            }

            return neg;
        }

        public List<FieldPlanner> GetPlannersList()
        {
            if (ParentBuildPlanner == null) return null;
            return ParentBuildPlanner.BasePlanners;
        }


        public int GetNodeHelperIterationIndex()
        {
            if (ParentBuildPlanner) return ParentBuildPlanner.GenerationIteration;
            return 0;
        }

        public void CallFromParentLayer(BuildPlannerPreset.BuildPlannerLayer buildPlannerLayer)
        {
            parentLayer = buildPlannerLayer;
        }

        public void Discard(PlanGenerationPrint print)
        {
            Discarded = true;
        }

        /// <summary> Setting planner's checker root position to provided world position and rounding it if rounding is enabled in this planner </summary>
        public void SetCheckerWorldPosition(Vector3 newPosition)
        {
            if (LatestResult == null) return;
            if (LatestResult.Checker == null) return;

            LatestResult.Checker.RootPosition = newPosition;
            if (RoundToScale) LatestResult.Checker.RoundRootPositionToScale();
        }

        public void SetCheckerWorldPosition(CheckerField3D checker, Vector3 newPosition)
        {
            if (LatestResult == null) return;
            if (checker == null) return;

            checker.RootPosition = newPosition;
            if (RoundToScale) checker.RoundRootPositionToScale();
        }

        public List<FieldPlanner> GetDuplicatesPlannersList()
        {
            if (DuplicateParent) return DuplicateParent.duplicatePlanners;
            return duplicatePlanners;
        }

        /// <summary>
        /// Checking if planner if enabled, executed and not discarded
        /// </summary>
        public bool IsValid()
        {
            if (DisableWholePlanner) return false;
            if (WasPreExecuted == false) return false;
            if (Discarded) return false;
            return true;
        }

        public void ResetForGenerating()
        {
            PreparationDone = false;
            PreparationWasDoneFlag = false;
            PreparationWasStarted = false;

            ExecutionDone = false;
            ExecutionWasDoneFlag = false;
            ExecutionWasStarted = false;

            MidExecutionDone = false;
            MidExecutionDoneFlag = false;
            MidExecutionWasStarted = false;

            PostExecutionDone = false;
            PostExecutionDoneFlag = false;
            PostExecutionWasStarted = false;

            WasPreExecuted = false;
            WasCalled = false;
            Discarded = false;

            previewChecker = null;
            ClearSubFields();
        }


        public void PrepareForGenerating(int indexOnPreset, int preparationIndex, PlanGenerationPrint print)
        {
            int i = indexOnPreset;
            executionTimeTags = null;

            if (OnGeneratingEvents == null) OnGeneratingEvents = new List<Action<object>>();
            OnGeneratingEvents.Clear();

            PlannersInBuild[i].PrepareOnPrint(print, -1);

            for (int d = 0; d < PlannersInBuild[i].Duplicates; d++)
            {
                PlannersInBuild[i].GetDuplicatesPlannersList()[d].PrepareOnPrint(print, d);
            }


            //PlannersInBuild[i].PreparePlannerInstance();


            PreparationDone = true;


            //PlannersInBuild[i].PreparePlannerInstance();
            //for (int d = 0; d < PlannersInBuild[i].Duplicates; d++)
            //{
            //    var pln = PlannersInBuild[i].GetDuplicatesPlannersList()[d];
            //    pln.PreparePlannerInstance();
            //}
        }

        // Planners

        private int[] _plannerIds = null;
        public int[] GetPlannersIDList(bool forceRefresh = false)
        {
            if (Event.current != null) if (Event.current.type == EventType.MouseDown) forceRefresh = true;

            if (forceRefresh || _plannerIds == null || _plannerIds.Length != PlannersInBuild.Count)
            {
                _plannerIds = new int[ParentBuildPlanner.BasePlanners.Count];
                for (int i = 0; i < PlannersInBuild.Count; i++)
                {
                    _plannerIds[i] = i;
                }
            }

            return _plannerIds;
        }

        public void AddDuplicateReference(FieldPlanner fieldPlanner)
        {
            if (duplicatePlanners == null) duplicatePlanners = new List<FieldPlanner>();
            fieldPlanner.DuplicateParent = this;
            duplicatePlanners.Add(fieldPlanner);
        }

        private GUIContent[] _plannerNames = null;
        public GUIContent[] GetPlannersNameList(bool forceRefresh = false)
        {
            if (Event.current != null) if (Event.current.type == EventType.MouseDown) forceRefresh = true;

            if (forceRefresh || _plannerNames == null || _plannerNames.Length != PlannersInBuild.Count)
            {
                _plannerNames = new GUIContent[ParentBuildPlanner.BasePlanners.Count];
                for (int i = 0; i < PlannersInBuild.Count; i++)
                {
                    _plannerNames[i] = new GUIContent("[" + i + "] " + PlannersInBuild[i].name, "Parent build plan : " + ParentBuildPlanner.name);
                }
            }
            return _plannerNames;
        }


        // Planner Variables


        private int[] _VariablesIds = null;

        public int[] GetVariablesIDList(bool forceRefresh = false)
        {
            if (Event.current != null) if (Event.current.type == EventType.MouseDown) forceRefresh = true;

            if (forceRefresh || _VariablesIds == null || _VariablesIds.Length != Variables.Count)
            {
                _VariablesIds = new int[Variables.Count];
                for (int i = 0; i < Variables.Count; i++)
                {
                    _VariablesIds[i] = i;
                }
            }

            return _VariablesIds;
        }

        public int[] GetBuildVariablesIDList(bool forceRefresh = false)
        {
            return ParentBuildPlanner.GetVariablesIDList(forceRefresh);
        }

        public GUIContent[] GetBuildVariablesNameList(bool forceRefresh = false)
        {
            return ParentBuildPlanner.GetVariablesNameList(forceRefresh);
        }

        private GUIContent[] _VariablesNames = null;
        public GUIContent[] GetVariablesNameList(bool forceRefresh = false)
        {
            if (Event.current != null) if (Event.current.type == EventType.MouseDown) forceRefresh = true;

            if (forceRefresh || _VariablesNames == null || _VariablesNames.Length != Variables.Count)
            {
                _VariablesNames = new GUIContent[Variables.Count];
                for (int i = 0; i < Variables.Count; i++)
                {
                    _VariablesNames[i] = new GUIContent(Variables[i].Name);
                }
            }
            return _VariablesNames;
        }



        // Extras

        public static IPlanNodesContainer GetNodesContainer(PlannerRuleBase nd)
        {
            if (nd)
            {
                if (nd.LiteParentContainer != null) return nd.LiteParentContainer;
                if (nd.ParentNodesContainer != null) if (nd.ParentNodesContainer is IPlanNodesContainer) return nd.ParentNodesContainer as IPlanNodesContainer;
                return nd.ParentPlanner;
            }

            return null;
        }

    }
}