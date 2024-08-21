using System.Collections.Generic;
using UnityEngine;
using FIMSpace.Generating.Planning.GeneratingLogics;
using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System;
using FIMSpace.Generating.Planner.Nodes;

namespace FIMSpace.Generating.Planning
{
    /// <summary>
    /// It's never sub-asset -> it's always project file asset
    /// </summary>
    public partial class FieldPlanner : ScriptableObject, IPlanNodesContainer, ICheckerReference
    {
        public static FieldPlanner CurrentGraphExecutingPlanner = null;

        public static BuildPlannerPreset CurrentGraphExecutingBuild
        {
            get
            {
                if (CurrentGraphExecutingPlanner == null) return null;
                return CurrentGraphExecutingPlanner.ParentBuildPlanner;
            }
        }

        public static FieldPlanner CurrentGraphPreparingPlanner = null;

        /// <summary> Assigned automatically at start if null </summary>
        private BuildPlannerPreset.BuildPlannerLayer parentLayer = null;

        /// <summary> For future implementation of build layers </summary>
        public List<FieldPlanner> GetPlanners()
        {
            if (parentLayer == null) ParentBuildPlanner.RefreshBuildLayers();
            return parentLayer.FieldPlanners;
        }

        [HideInInspector] public List<FieldPlannerOperationHelper> Operations = new List<FieldPlannerOperationHelper>();

        public void CallOperations_OnStartPrepareMainInstance()
        {
            foreach (FieldPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnStartPrepareFieldPlannerMainInstance(ParentBuildPlanner, this, op); }
        }

        public void CallOperations_OnStartPrepare()
        {
            foreach (FieldPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnStartPrepareFieldPlanner(ParentBuildPlanner, this, op); }
        }

        public void CallOperations_OnPlannerComplete()
        {
            foreach (FieldPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnFieldPlannerComplete(ParentBuildPlanner, this, op); }
        }

        public void CallOperations_OnSpawningComplete(BuildPlannerExecutor executor, PGGGeneratorRoot generator)
        {
            foreach (FieldPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnPlannerGeneringComplete(op, ParentBuildPlanner, executor, this, generator); }
        }

        public enum EFieldType
        {
            FieldPlanner,
            BuildField,
            InternalField,
            Prefab
        }

        [HideInInspector] public BuildPlannerPreset ParentBuildPlanner;
        [NonSerialized] public ShapeGeneratorBase _tempOverrideShape = null;
        [HideInInspector] public ShapeGeneratorBase ShapeGenerator = null;
        public bool DisableWholePlanner = false;
        public bool DontGenerateIt { get { return DisableWholePlanner || Discarded; } }

        public string tag = "";
        public int InstanceID { get; private set; }
        public PE_Start ProceduresBegin { get { return proceduresBegin; } }

        [SerializeField, HideInInspector] private PE_Start proceduresBegin;
        public PE_Start PostProceduresBegin { get { return postProceduresBegin; } }
        [SerializeField, HideInInspector] private PE_Start postProceduresBegin;

        public List<PGGPlanner_NodeBase> FProcedures = new List<PGGPlanner_NodeBase>();
        public List<PGGPlanner_NodeBase> FPostProcedures = new List<PGGPlanner_NodeBase>();

        public List<SubGraph> FSubGraphs = new List<SubGraph>();

        public List<FieldVariable> FVariables = new List<FieldVariable>();

        [HideInInspector] public bool ExposeShape = true;

        [HideInInspector] public EFieldType FieldType = EFieldType.FieldPlanner;

        [Tooltip("You can assign other FieldSetups later, in the BuildPlannerExecutor component")]
        [HideInInspector] public FieldSetup DefaultFieldSetup = null;
        [Tooltip("If using 'Prefab' field type - prefab to be used for grid field preview")]
        [HideInInspector] public GameObject DefaultPrefab = null;
        //[Tooltip("If 'prefab to grid' should calcaulate Y Level cells")]
        //[HideInInspector] public bool PrefabField_FlatGrid = false;

        [Space(4)]
        [PGG_SingleLineTwoProperties("ExposeInstanceCount", 86, 140, 18)] public int Instances = 1;
        /// <summary> Returning Instances-1 </summary>
        public int Duplicates { get { return Instances - 1; } }
        /// <summary> Returning duplicateIndex + 1 </summary>
        public int InstanceIndex { get { return IndexOfDuplicate + 1; } }
        [HideInInspector][Tooltip("Toggle if you want to allow changing duplicates count in executor")] public bool ExposeInstanceCount = false;

        [Space(6)]

        [PGG_SingleLineTwoProperties("AlwaysPushOut", 106, 124)] public bool DisableCollision = false;
        [HideInInspector] public bool AlwaysPushOut = true;


        //[PGG_SingleLineTwoProperties("AllowRotateBy90", 96, 124)] public bool RoundPosition = true;
        /*[HideInInspector] */
        public bool AllowRotateBy90 = true;

        [Space(7)]
        //[PGG_SingleLineTwoProperties("RoundToScale", 120, 124, 18)]
        [Tooltip("Size of single cell used for this field planner preview (in the executor component size will adapt to the FieldSetup cell size)")]
        public Vector3 PreviewCellSize = new Vector3(1f, 1f, 1f);

        //[Space(6)]
        //[Tooltip("Making position be only full numbers like  1 , 2 , 3  rounding fractions like  1.4  2.7  etc.")]
        [Tooltip("Rounding field position accordingly to the scale, so there is no x = 0.5 but x = 0  x = 1 etc.")]
        public bool RoundToScale = false;

        //[HideInInspector] public bool UseCheckerScale = false;
        //[HideInInspector] public Vector3 CheckerScale = Vector3.one;

        public Vector3 GetScale { get { return PreviewCellSize; } }
        //public Vector3 GetScale { get { if (UseCheckerScale) return PreviewCellSize; else return PreviewCellSize; } }
        public float GetScaleF { get { return PreviewCellSize.x; } }
        //public float GetScaleF { get { if (UseCheckerScale) return PreviewCellSize.x; else return PreviewCellSize.x; } }


        public int MaxRetries = 64;

        private CheckerField3D previewChecker;
        public PlannerResult LatestResult;

        public CheckerField3D LatestChecker
        {
            get
            {
                if (LatestResult == null) return GetInitialChecker();
                /*if (LatestResult.Checker != null) LatestResult.Checker.CellsFieldParent = this; */
                return LatestResult.Checker;
            }
        }

        public CheckerField3D CheckerReference { get { return LatestChecker; } }


        public int IndexOnPrint = -1;
        public int IndexOfDuplicate = -1;
        public int IndexOnPreset = -1;
        private string printName;

        public enum EViewGraph { Procedures_Placement, PostProcedures_Cells, Procedures_CustomGraphs }
        [HideInInspector] public EViewGraph GraphView = EViewGraph.Procedures_Placement;
        public EViewGraph Graph_DisplayMode { get { return GraphView; } }

        public string ArrayNameString
        {
            get
            {
                if (IsSubField)
                {
                    if (IsDuplicate) return "Sub[" + GetSubFieldID() + "] [" + IndexOnPreset + "][" + IndexOfDuplicate + "]";
                    else
                        return "Sub[" + GetSubFieldID() + "] [" + IndexOnPreset + "]";
                }

                if (IsDuplicate) return "[" + IndexOnPreset + "][" + IndexOfDuplicate + "]";
                else return "[" + IndexOnPreset + "]";
            }
        }

        public Vector3 ArrayNameIDVector
        {
            get
            {
                if (IsSubField)
                {
                    if (IsDuplicate) return new Vector3(IndexOnPreset, IndexOfDuplicate, GetSubFieldID());
                    else
                        return new Vector3(IndexOnPreset, -1, GetSubFieldID());
                }

                if (IsDuplicate) return new Vector3(IndexOnPreset, IndexOfDuplicate, -1);
                else return new Vector3(IndexOnPreset, -1, -1);
            }
        }


        #region Async Support Variables

        [System.NonSerialized] public bool ExecutionWasStarted = false;
        [System.NonSerialized] public bool ExecutionDone = false;
        [System.NonSerialized] public bool ExecutionWasDoneFlag = false;

        [System.NonSerialized] public bool MidExecutionWasStarted = false;
        [System.NonSerialized] public bool MidExecutionDone = false;
        [System.NonSerialized] public bool MidExecutionDoneFlag = false;

        [System.NonSerialized] public bool PostExecutionWasStarted = false;
        [System.NonSerialized] public bool PostExecutionDone = false;
        [System.NonSerialized] public bool PostExecutionDoneFlag = false;

        [System.NonSerialized] public bool PreparationWasStarted = false;
        [System.NonSerialized] public bool PreparationDone = false;
        [System.NonSerialized] public bool PreparationWasDoneFlag = false;

        #endregion


        public List<PGGPlanner_NodeBase> Procedures { get { return FProcedures; } }

        public List<PGGPlanner_NodeBase> PostProcedures { get { return FPostProcedures; } }

        public List<FieldVariable> Variables { get { return FVariables; } }

        public ScriptableObject ScrObj { get { return this; } }

        public FieldPlanner.LocalVariables GraphLocalVariables
        {
            get
            {
                if (localVars == null) RefreshLocalVariables();
                return localVars;
            }
        }

        private FieldPlanner.LocalVariables localVars;



        private void Awake()
        {
            RefreshStartGraphNodes();
            InstanceID = GetInstanceID();
            //if (Variables == null || Variables.Count == 0)
            //{
            //    Variables = new List<FieldVariable>();
            //    FieldVariable def = new FieldVariable("Spawn Propability Multiplier", 1f);
            //    def.helper.x = 0; def.helper.y = 5;
            //    Variables.Add(def);

            //    def = new FieldVariable("Spawn Count Multiplier", 1f);
            //    def.helper.x = 0; def.helper.y = 5;
            //    Variables.Add(def);
            //}

        }


        void OnValidate()
        {
            RefreshStartGraphNodes();
        }

        public void RefreshStartGraphNodes()
        {
            //proceduresBegin = null;

            for (int p = 0; p < Procedures.Count; p++)
            {
                if (Procedures[p] is PE_Start)
                {
                    proceduresBegin = Procedures[p] as PE_Start;
                    break;
                }
            }

            for (int p = 0; p < PostProcedures.Count; p++)
            {
                if (PostProcedures[p] is PE_Start)
                {
                    postProceduresBegin = PostProcedures[p] as PE_Start;
                    break;
                }
            }

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == null) continue;
                FSubGraphs[s].RefreshStartGraphNodes();
            }
        }

        public void RefreshGraphs()
        {
            FGraph_RunHandler.RefreshConnections(Procedures);
            FGraph_RunHandler.RefreshConnections(PostProcedures);

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == null) continue;
                FGraph_RunHandler.RefreshConnections(FSubGraphs[s].Procedures);
                PlannerRuleBase.EnsurePlannerRulesOwner(FSubGraphs[s].Procedures, FSubGraphs[s]);
            }
        }

        public void ArrangeForGeneration(int i)
        {
            for (int p = 0; p < Procedures.Count; p++)
                Procedures[p].ToRB().ParentPlanner = this;

            for (int p = 0; p < PostProcedures.Count; p++)
                PostProcedures[p].ToRB().ParentPlanner = this;

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == null) continue;
                for (int p = 0; p < FSubGraphs[s].Procedures.Count; p++)
                    FSubGraphs[s].Procedures[p].ToRB().ParentPlanner = this;
            }

            for (int d = 0; d < PlannersInBuild[i].Duplicates; d++)
            {
                var dupl = Instantiate(PlannersInBuild[i]);
                dupl.IndexOfDuplicate = d;
                PlannersInBuild[i].AddDuplicateReference(dupl);
                dupl.PrepareProcedures();
            }
        }

        public void RefreshPreviewWith(CheckerField3D checker)
        {
            if (ParentBuildPlanner == null) return;

            //UnityEngine.Debug.Log("checker[ " + IndexOnPrint + "  /  " + ParentBuildPlanner.LatestGenerated.PlannerResults.Count + "]");
            if (DuplicateParent == null)
            {
                ParentBuildPlanner.LatestGenerated.PlannerResults[IndexOnPrint].Checker = checker;
            }
            else
            {
                ParentBuildPlanner.LatestGenerated.PlannerResults[IndexOnPrint].DuplicateResults[IndexOfDuplicate].Checker = checker;
            }

        }

        /// <summary>
        /// Called on root instance and duplicates
        /// </summary>
        public void PrepareOnPrint(PlanGenerationPrint gen, int duplicateId)
        {
            CurrentGraphPreparingPlanner = this;
            IndexOfDuplicate = duplicateId;
            LatestResult = PlannerResult.GenerateInstance(ParentBuildPlanner, this);

            if (duplicateId >= 0) // If it's duplicate -> include it inside main planner duplicates list
            {
                gen.PlannerResults[IndexOnPrint].AddDuplicateResultSlot(PlannerResult.GenerateInstance(ParentBuildPlanner, this));
            }
            else // Add base result and prepare support for duplicates
            {
                LatestResult.PrepareDuplicateSupport();
                gen.PlannerResults.Add(LatestResult);
            }

            PrepareInitialChecker();
            LatestResult.Checker = GetInitialChecker();

            if (duplicateId >= 0) // If it's duplicate -> include it inside main planner duplicates list
            {
                gen.PlannerResults[IndexOnPrint].DuplicateResults[duplicateId] = LatestResult.Copy();
            }
            else // Add base result and prepare support for duplicates
            {
                gen.PlannerResults[IndexOnPrint] = LatestResult;
            }

            if (ParentBuildPlanner.OnIteractionCallback != null)
            {

                if (PlannerRuleBase.Debugging)
                {
                    gen.DebugInfo = "Initializing  " + printName;
                    gen._debugLatestExecuted = LatestResult.Checker;
                }

                ParentBuildPlanner.OnIteractionCallback.Invoke(gen);
            }




        }


        // Prepare procedures for each instance call
        //void PreparePlannerInstance()
        //{
        //    UnityEngine.Debug.Log("pl id = " + GetInstanceID());

        //    for (int i = 0; i < Procedures.Count; i++)
        //        InstancePrepareCall(Procedures);

        //    for (int i = 0; i < PostProcedures.Count; i++)
        //        InstancePrepareCall(PostProcedures);

        //    for (int s = 0; s < FSubGraphs.Count; s++)
        //        for (int i = 0; i < FSubGraphs[s].Procedures.Count; i++)
        //            InstancePrepareCall(FSubGraphs[s].Procedures);
        //}

        //void InstancePrepareCall(List<PGGPlanner_NodeBase> nodes)
        //{
        //    if (nodes == null) return;

        //    for (int i = 0; i < nodes.Count; i++)
        //    {
        //        if (nodes[i] == null) continue;
        //        nodes[i].OnInstancePrepare(this);
        //    }
        //}


        /// <summary> Can't be async </summary>
        public void PrepareProcedures()
        {
            CallOperations_OnStartPrepare();

            for (int i = 0; i < FProcedures.Count; i++)
            {
                if (FProcedures[i] == null) continue;
                if (FProcedures[i].Enabled == false) continue;
                FProcedures[i].ToRB().PreGeneratePrepare();
            }

            for (int i = 0; i < FPostProcedures.Count; i++)
            {
                if (FPostProcedures[i] == null) continue;
                if (FPostProcedures[i].Enabled == false) continue;
                FPostProcedures[i].ToRB().PreGeneratePrepare();
            }

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                var subGr = FSubGraphs[s];
                if (subGr == null) continue;

                for (int i = 0; i < subGr.FProcedures.Count; i++)
                {
                    if (subGr.FProcedures[i] == null) continue;
                    if (subGr.FProcedures[i].Enabled == false) continue;
                    subGr.FProcedures[i].ToRB().PreGeneratePrepare();
                }
            }
        }

        public void RefreshOnReload()
        {
            RefreshLocalVariables();
        }

        void RefreshLocalVariables()
        {
            if (localVars == null) localVars = new FieldPlanner.LocalVariables(this);
            localVars.RefreshList();

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == null) continue;
                FSubGraphs[s].RefreshLocalVariables();
            }
        }


        /// <summary> Can't be async </summary>
        public void PrePrepareForGenerating(int indexOnPreset, int preparationIndex)
        {
            InstanceID = GetInstanceID();
            if (FSubGraphs == null) FSubGraphs = new List<SubGraph>();

            RefreshStartGraphNodes();
            RefreshLocalVariables();
            //RefreshGraphs();

            printName = name;

            PreparationWasStarted = true;

            if (duplicatePlanners != null) duplicatePlanners.Clear();

            IndexOnPreset = indexOnPreset;
            IndexOnPrint = preparationIndex;

            PGGUtils.CheckForNulls(FProcedures);
            PGGUtils.CheckForNulls(FPostProcedures);

            PrepareInternalValueVariables();

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == null) continue;
                PGGUtils.CheckForNulls(FSubGraphs[s].Procedures);
            }

            PrepareProcedures();
            RefreshGraphs();

            ArrangeForGeneration(indexOnPreset);
        }

        /// <summary>
        /// Call before RunStartProcedures. Can't be async
        /// </summary>
        public void PreRunProcedures(PlanGenerationPrint gen)
        {
            CurrentGraphExecutingPlanner = this;

            for (int i = 0; i < FProcedures.Count; i++)
            {
                if (FProcedures[i].Enabled == false) continue;
                FProcedures[i].ToRB().Prepare(gen);
            }

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == null) continue;

                for (int i = 0; i < FSubGraphs[s].FProcedures.Count; i++)
                {
                    if (FSubGraphs[s].FProcedures[i].Enabled == false) continue;
                    FSubGraphs[s].FProcedures[i].ToRB().Prepare(gen);
                }
            }
        }


        /// <summary>
        /// Call before RunStartProcedures. Can't be async
        /// </summary>
        public void PreRunPostProcedures(PlanGenerationPrint gen)
        {
            CurrentGraphExecutingPlanner = this;

            for (int i = 0; i < FPostProcedures.Count; i++)
            {
                if (FPostProcedures[i].Enabled == false) continue;
                FPostProcedures[i].ToRB().Prepare(gen);
            }
        }

        public void RunStartProcedures(PlanGenerationPrint gen)
        {
            ExecutionWasStarted = true;
            WasPreExecuted = true;

            if (proceduresBegin == null)
            {
                CompleteGenerating();
                WasCalled = true;
                return;
            }

            if (proceduresBegin.OutputConnections.Count == 0)
            {
                SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.AfterEachInstance);
                CompleteGenerating();
                WasCalled = true;
                return;
            }

            PlannerRuleBase operation = proceduresBegin.FirstOutputConnection as PlannerRuleBase;
            if (operation == null)
            {
                SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.AfterEachInstance);
                CompleteGenerating();
                WasCalled = true;
                return;
            }

            CurrentGraphExecutingPlanner = this;

            CallExecution(operation, gen);
            SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.AfterEachInstance);

            CompleteGenerating();
            WasCalled = true;
        }



        public void RunMidProcedures(PlanGenerationPrint gen)
        {
            MidExecutionWasStarted = true;
            WasPreExecuted = true;
            WasCalled = true;
            SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.Default);
            CompleteMidGenerating();
        }


        /// <summary> Returns true if something was executed </summary>
        bool SubGraphsInstanceExecution(PlanGenerationPrint gen, SubGraph.EExecutionOrder targetOrder)
        {
            if (FSubGraphs == null) return false;

            bool executed = false;
            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                var sub = FSubGraphs[s];
                if (sub == null) continue;
                if (sub.ExecutionOrder != targetOrder) continue;
                if (sub.proceduresBegin == null) continue;

                PlannerRuleBase operation = sub.proceduresBegin.FirstOutputConnection as PlannerRuleBase;
                if (operation != null)
                {
                    CurrentGraphExecutingPlanner = this;
                    CallExecution(operation, gen);
                    executed = true;
                }
            }

            return executed;
        }


        public void RunPostProcedures(PlanGenerationPrint gen)
        {
            PostExecutionWasStarted = true;

            if (postProceduresBegin == null)
            {
                CompletePostGenerating();
                return;
            }

            if (postProceduresBegin.OutputConnections.Count == 0)
            {
                SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.PostProcedure);
                CompletePostGenerating();
                return;
            }

            PlannerRuleBase operation = postProceduresBegin.FirstOutputConnection as PlannerRuleBase;
            if (operation == null)
            {
                SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.PostProcedure);
                CompletePostGenerating();
                return;
            }

            CurrentGraphExecutingPlanner = this;

            SubGraphsInstanceExecution(gen, SubGraph.EExecutionOrder.PostProcedure);
            CallExecution(postProceduresBegin, gen);

            CompletePostGenerating();
        }


        public void CallExecution(PlannerRuleBase rule, PlanGenerationPrint newResult)
        {
            rule.Execute(newResult, LatestResult);

            if (IndexOnPrint != -1)
            {
                if (newResult != null)
                    if (DuplicateParent == null)
                    {
                        newResult.PlannerResults[IndexOnPrint] = LatestResult;
                    }
                    else
                    {
                        if (newResult.PlannerResults[IndexOnPrint].DuplicateResults != null)
                        {
                            newResult.PlannerResults[IndexOnPrint].DuplicateResults[IndexOfDuplicate] = LatestResult;
                        }
                    }
            }

            if (ParentBuildPlanner.OnIteractionCallback != null)
            {
                if (newResult != null)
                    if (PlannerRuleBase.Debugging)
                    {
                        newResult.DebugInfo = "Field Planner '" + name + "'\n\nProcedure: " + rule.GetDisplayName() + "\n\n" + rule.DebuggingInfo;
                        newResult.DebugGizmosAction = rule.DebuggingGizmoEvent;
                    }

                ParentBuildPlanner.OnIteractionCallback.Invoke(newResult);
            }

            //UnityEngine.Debug.Log(start._E_GetDisplayName() + " connections = " + start.OutputConnections.Count);
            if (rule.FirstOutputConnection == null)
            {
                return;
            }

            if (rule.AllowedOutputConnectionIndex > -1)
            {
                for (int c = 0; c < rule.OutputConnections.Count; c++)
                {
                    if (rule.OutputConnections[c].ConnectionFrom_AlternativeID != rule.AllowedOutputConnectionIndex) continue;
                    //UnityEngine.Debug.Log("out index = " + start.AllowedOutputConnectionIndex + " alt ind = " + start.OutputConnections[c].ConnectionFrom_AlternativeID);

                    CallExecution(
                    rule.OutputConnections[c].GetOther(rule) as PlannerRuleBase,
                     newResult);
                }
            }
            else
            {
                for (int c = 0; c < rule.OutputConnections.Count; c++)
                {
                    CallExecution(
                    rule.OutputConnections[c].GetOther(rule) as PlannerRuleBase,
                     newResult);
                }
            }

        }

        void CompleteGenerating()
        {
            ExecutionDone = true;
        }
        void CompleteMidGenerating()
        {
            MidExecutionDone = true;
        }
        void CompletePostGenerating()
        {
            PostExecutionDone = true;
            CallOperations_OnPlannerComplete();
        }

        public void OnCompleateAllGenerating()
        {
            if (!Discarded)
                if (LatestResult != null)
                {
                    for (int c = 0; c < LatestResult.CellsInstructions.Count; c++)
                    {
                        if (FGenerators.NotNull(LatestResult.CellsInstructions[c].HelperCellRef)) continue;

                        FieldCell setCellRef;

                        if (LatestResult.CellsInstructions[c].HelperCellRef != null)
                            setCellRef = LatestResult.Checker.GetCell(LatestResult.CellsInstructions[c].HelperCellRef.Pos);
                        else
                            setCellRef = LatestResult.Checker.GetCell(LatestResult.CellsInstructions[c].pos);

                        LatestResult.CellsInstructions[c].HelperCellRef = setCellRef;
                    }
                }

            if (IsDuplicate == false)
                if (duplicatePlanners != null)
                    for (int d = 0; d < Duplicates; d++)
                    {
                        duplicatePlanners[d].OnCompleateAllGenerating();
                    }
        }


        public static List<CheckerField3D> PlannerListToCheckerList(List<FieldPlanner> planners)
        {
            List<CheckerField3D> checkers = new List<CheckerField3D>();
            for (int p = 0; p < planners.Count; p++) checkers.Add(planners[p].LatestChecker);
            return checkers;
        }

        public static List<ICheckerReference> PlannerListToCheckerRefList(List<FieldPlanner> planners)
        {
            List<ICheckerReference> checkers = new List<ICheckerReference>();
            for (int p = 0; p < planners.Count; p++) checkers.Add(planners[p]);
            return checkers;
        }

        public static List<FieldPlanner> CheckerRefListToPlannerList(List<ICheckerReference> planners)
        {
            List<FieldPlanner> fields = new List<FieldPlanner>();
            for (int p = 0; p < planners.Count; p++) { FieldPlanner f = planners[p] as FieldPlanner; if (f != null) fields.Add(f); }
            return fields;
        }


#if UNITY_EDITOR
#endif

    }

}