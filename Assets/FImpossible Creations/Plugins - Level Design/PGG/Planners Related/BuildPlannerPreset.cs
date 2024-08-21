using System.Collections.Generic;
using UnityEngine;
using FIMSpace.Generating.Checker;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning
{
    [CreateAssetMenu(fileName = "BuildPlanner_", menuName = "FImpossible Creations/Procedural Generation/Build Planner Preset (Layout)", order = 0)]
    public partial class BuildPlannerPreset : ScriptableObject
    {
        [Tooltip("Custom info to display for build planner preset users")]
        public string CustomInfo = "";

        public List<FieldVariable> BuildVariables = new List<FieldVariable>();

        //private int _DisplayBuildLayer = 0;
        /// <summary> Getting base layer planners list (without duplicates! duplicates are stored in planners lsit variables) </summary>
        public List<FieldPlanner> BasePlanners
        {
            get
            {
                if (BuildLayers.Count == 0) BuildLayers.Add(new BuildPlannerLayer());
                return BuildLayers[0].FieldPlanners;
            }
        }

        private static List<FieldPlanner> _CollectAllAvailablePlannersCache = new List<FieldPlanner>();

        /// <summary>
        /// Generating new list of planners in current stage of the build plan.
        /// It will exclude discarded and disabled planners.
        /// You can choose to gather duplicates.
        /// </summary>
        public List<FieldPlanner> CollectAllAvailablePlanners(bool withDuplicates = true, bool newListInstance = true, bool withSubFields = true, bool onlyAlreadyExecuted = false)
        {
            List<FieldPlanner> allPlanners;

            #region GC Cache for planners list if used
            if (newListInstance == false)
            {
                _CollectAllAvailablePlannersCache.Clear();
                allPlanners = _CollectAllAvailablePlannersCache;
            }
            else
            {
                allPlanners = new List<FieldPlanner>();
            }
            #endregion

            var basePlanners = BasePlanners;

            for (int i = 0; i < basePlanners.Count; i++)
            {
                FieldPlanner plan = basePlanners[i];

                if (plan.DisableWholePlanner) continue;
                if (plan.Discarded) continue;
                if (onlyAlreadyExecuted) if (!plan.WasCalled) continue;

                allPlanners.Add(plan);

                if (withSubFields)
                {
                    for (int s = 0; s < plan.GetSubFieldsCount; s++)
                    {
                        if (FieldPlanner.CantExecute(plan.GetSubField(s))) { continue; }
                        if (onlyAlreadyExecuted) if (!plan.GetSubField(s).WasCalled) continue;
                        allPlanners.Add(plan.GetSubField(s));
                    }
                }

                if (withDuplicates)
                {
                    var duplicatesList = plan.GetDuplicatesPlannersList();
                    if (duplicatesList != null)
                        for (int d = 0; d < duplicatesList.Count; d++)
                        {
                            plan = duplicatesList[d];
                            if (plan.Discarded) continue;
                            if (onlyAlreadyExecuted) if (!plan.WasCalled) continue;
                            allPlanners.Add(plan);

                            if (withSubFields)
                                for (int s = 0; s < plan.GetSubFieldsCount; s++)
                                {
                                    if (FieldPlanner.CantExecute(plan.GetSubField(s))) continue;
                                    if (onlyAlreadyExecuted) if (!plan.GetSubField(s).WasCalled) continue;
                                    allPlanners.Add(plan.GetSubField(s));
                                }
                        }
                }
            }

            return allPlanners;
        }

        private static List<CheckerField3D> _CollectAllAvailablePlannersCheckersCache = new List<CheckerField3D>();
        private static List<ICheckerReference> _CollectAllAvailableCheckersRefCache = new List<ICheckerReference>();

        public List<CheckerField3D> CollectAllAvailablePlannersCheckers(bool withDuplicates = true, bool newListInstance = true, bool withSubFields = true, bool onlyAlreadyExecuted = false)
        {
            var planners = CollectAllAvailablePlanners(withDuplicates, newListInstance, withSubFields, onlyAlreadyExecuted);

            List<CheckerField3D> allCheckers;
            #region GC Cache for planners list if used
            if (newListInstance == false)
            {
                _CollectAllAvailablePlannersCheckersCache.Clear();
                allCheckers = _CollectAllAvailablePlannersCheckersCache;
            }
            else
            {
                allCheckers = new List<CheckerField3D>();
            }
            #endregion

            for (int p = 0; p < planners.Count; p++)
            {
                allCheckers.Add(planners[p].LatestChecker);
            }

            return allCheckers;
        }

        public List<ICheckerReference> CollectAllAvailablePlannersCheckerRefs(bool withDuplicates = true, bool newListInstance = true, bool withSubFields = true, bool onlyAlreadyExecuted = false)
        {
            var planners = CollectAllAvailablePlanners(withDuplicates, newListInstance, withSubFields, onlyAlreadyExecuted);

            List<ICheckerReference> allCheckers;
            #region GC Cache for planners list if used
            if (newListInstance == false)
            {
                _CollectAllAvailableCheckersRefCache.Clear();
                allCheckers = _CollectAllAvailableCheckersRefCache;
            }
            else
            {
                allCheckers = new List<ICheckerReference>();
            }
            #endregion

            for (int p = 0; p < planners.Count; p++)
            {
                allCheckers.Add(planners[p].LatestChecker);
            }

            return allCheckers;
        }


        public int CountAllAvailablePlanners(bool ignoreDisabledPlanners = true, bool ignoreDiscarded = false)
        {
            int sum = 0;
            var basePlanners = BasePlanners;

            for (int i = 0; i < basePlanners.Count; i++)
            {
                FieldPlanner plan = basePlanners[i];
                if (ignoreDisabledPlanners) if (plan.DisableWholePlanner) continue;
                if (ignoreDiscarded) if (plan.Discarded) continue;

                sum += 1;

                var duplicatesList = plan.GetDuplicatesPlannersList();
                if (duplicatesList != null)
                    for (int d = 0; d < duplicatesList.Count; d++)
                    {
                        plan = duplicatesList[d];
                        if (ignoreDiscarded) if (plan.Discarded) continue;
                        sum += 1;
                    }
            }

            return sum;
        }

        public int CountIndexOfPlannerInstance(FieldPlanner instance, bool ignoreDisabledPlanners = true, bool ignoreDiscarded = false)
        {
            int sum = 0;
            var basePlanners = BasePlanners;

            for (int i = 0; i < basePlanners.Count; i++)
            {
                FieldPlanner plan = basePlanners[i];
                if (ignoreDisabledPlanners) if (plan.DisableWholePlanner) continue;
                if (ignoreDiscarded) if (plan.Discarded) continue;

                if (plan == instance) return sum;
                sum += 1;

                var duplicatesList = plan.GetDuplicatesPlannersList();
                if (duplicatesList != null)
                    for (int d = 0; d < duplicatesList.Count; d++)
                    {
                        plan = duplicatesList[d];
                        if (ignoreDiscarded) if (plan.Discarded) continue;

                        if (plan == instance) return sum;
                        sum += 1;
                    }
            }

            return sum;
        }


        public List<BuildPlannerLayer> BuildLayers = new List<BuildPlannerLayer>();
        private void ValidateBuildLayers()
        {
            if (BuildLayers.Count == 0) BuildLayers.Add(new BuildPlannerLayer());
        }

        public List<FieldPlanner> GetPlanners(int layer = 0)
        {
            ValidateBuildLayers();
            if (layer <= 0 || layer >= BuildLayers.Count) return BuildLayers[0].FieldPlanners;
            return BuildLayers[layer].FieldPlanners;
        }

        public PlanGenerationPrint LatestGenerated { get; private set; }

        //public bool UseBoundsWorkflow = false;
        [Tooltip("Async generating is work in progress feature.\nAsync means multi-threaded generating without fps lags.")]
        [HideInInspector] public bool AsyncGenerating = false;

        //public static bool IsAsyncGenerationInProgress { get; private set; }
        //public static void UpdateGeneratingFlagsWith(BuildPlannerPreset pres, bool isGenerating)
        //{
        //    if (isGenerating) IsAsyncGenerationInProgress = pres.AsyncGenerating;
        //    else IsAsyncGenerationInProgress = false; 
        //}

        #region Generating Properies

        [System.NonSerialized] public bool _Editor_GraphNodesChanged = false;

        /// <summary> Used for editor auto-refresh preview </summary>
        [System.NonSerialized] public bool _Editor_GraphNodesChangedForced = false;

        public bool IsGeneratingDone
        {
            get
            {
                if (generateProgressManager == null) return false;
                return generateProgressManager.IsGeneratingDone;
            }

        }

        public bool IsGenerating
        {
            get
            {
                if (generateProgressManager == null) return false;
                return generateProgressManager.IsGenerating;
            }
        }

        public void OverrideProgressDisplay(float progr)
        {
            generateProgressManager.OverrideProgressDisplay(progr);
        }

        public float GeneratingProgress
        {
            get
            {
                if (generateProgressManager == null) return 0f;
                return generateProgressManager.GeneratingProgress;
            }
        }

        public float GeneratingProgressSmooth
        {
            get
            {
                if (generateProgressManager == null) return 0f;
                return generateProgressManager.SmoothGeneratingProgress;
            }
        }

        #endregion


        PlannerAsyncManager generateProgressManager = null;

        /// <summary> Latest seed set with this planner preset </summary>
        public int LatestSeed { get; private set; }
        /// <summary> When generation starts is's zero, it's going through all Field Planners and it's instances iterating this value </summary>
        public int GenerationIteration { get; set; }

        public PlannerAsyncManager RunProceduresAndGeneratePrint(int seed)
        {
            LatestSeed = seed;
            GenerationIteration = 0;

            LatestGenerated = new PlanGenerationPrint();
            generateProgressManager = new PlannerAsyncManager(this, seed);
            RefreshPlannersGraphs();

            return generateProgressManager;
        }

        public void ClearGeneration()
        {
            LatestGenerated = null;
            if (generateProgressManager != null) generateProgressManager.Remove();
            generateProgressManager = null;
        }

        public void UpdateGenerating(float dt = 0f)
        {
            if (generateProgressManager != null) generateProgressManager.UpdateGenerating(dt);
        }

        public void RefreshPlannersGraphs()
        {
            for (int i = 0; i < BasePlanners.Count; i++)
            {
                if (BasePlanners[i].DisableWholePlanner) continue;
                BasePlanners[i].RefreshGraphs();
            }

            CallOperations_PreGenerating();
        }

        [HideInInspector] public List<BuildPlannerOperationHelper> Operations = new List<BuildPlannerOperationHelper>();

        public void CallOperations_PreGenerating()
        {
            foreach (BuildPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnPrepareBuildPlan(this, op); }
        }

        public void CallOperations_OnPlanComplete()
        {
            foreach (BuildPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnBuildPlanComplete(this, op); }
        }

        public void CallOperations_OnExecutorComplete(BuildPlannerExecutor executor)
        {
            foreach (BuildPlannerOperationHelper op in Operations) { if (op == null || op.Operation == null) continue; op.Operation.OnBuildPlannerExecutorComplete(this, executor, op); }
        }

        #region Editor Utils

#if UNITY_EDITOR

        public bool _Editor_OperationsFoldout = false;

        public void SwitchSubAssetsVisibility(HideFlags? flag = null)
        {
            if (AssetDatabase.Contains(this) == false) return;
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
            bool visible = false;

            HideFlags targetFlag;

            if (flag == null)
            {
                for (int s = 0; s < assets.Length; s++) { if (assets[s] != this) { if (assets[s].hideFlags == HideFlags.HideInHierarchy) visible = false; else visible = true; break; } }
                targetFlag = visible ? HideFlags.HideInHierarchy : HideFlags.None;
            }
            else
                targetFlag = flag.Value;

            for (int s = 0; s < assets.Length; s++)
            {
                if (assets[s] == this) continue;
                assets[s].hideFlags = targetFlag;
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        public void ClearUnusedAssetsInside(bool showDialog = true, bool refreshDatabase = true)
        {
            if (AssetDatabase.Contains(this) == false) return;

            List<Object> used = new List<Object>();

            for (int l = 0; l < BuildLayers.Count; l++)
            {
                for (int p = 0; p < BuildLayers[l].FieldPlanners.Count; p++)
                {
                    var plan = BuildLayers[l].FieldPlanners[p];
                    used.Add(plan);

                    for (int pr = 0; pr < plan.Procedures.Count; pr++)
                    {
                        used.Add(plan.Procedures[pr]);
                    }

                    for (int pr = 0; pr < plan.PostProcedures.Count; pr++)
                    {
                        used.Add(plan.PostProcedures[pr]);
                    }

                    if (plan.FSubGraphs != null)
                        for (int pr = 0; pr < plan.FSubGraphs.Count; pr++)
                        {
                            for (int sb = 0; sb < plan.FSubGraphs[pr].FProcedures.Count; sb++)
                            {
                                used.Add(plan.FSubGraphs[pr].FProcedures[sb]);
                            }
                        }


                    used.Add(plan.ShapeGenerator);
                }
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this));
            List<Object> toRemove = new List<Object>();

            if (showDialog)
            {
                UnityEngine.Debug.Log("[PGG Sub-Assets Clearing] Files: " + assets.Length + "   Used by Build Planner: " + used.Count);
            }

            for (int s = 0; s < assets.Length; s++)
            {
                if (assets[s] == this) continue;

                if (used.Contains(assets[s]) == false)
                {
                    toRemove.Add(assets[s]);
                }
            }

            if (toRemove.Count == 0)
            {
                if (showDialog) EditorUtility.DisplayDialog("Clearing Unused Sub-Assets", "No Sub-Assets to clear!", "Ok");
                return;
            }

            if (showDialog)
            {
                if (EditorUtility.DisplayDialog("Clearing Unused Sub-Assets", "Detected " + toRemove.Count + " sub-assets to remove from " + name + " build planner preset file, proceed?", "Yes Remove Them", "No"))
                {
                    for (int r = 0; r < toRemove.Count; r++)
                    {
                        DestroyImmediate(toRemove[r], true);
                    }
                }
                else
                {
                    string log = "[PGG Sub - Assets Clearing]\n";

                    for (int r = 0; r < toRemove.Count; r++)
                    {
                        log += "[" + r + "] " + toRemove[r].name + " : " + toRemove[r].GetType().Name + "   |   ";
                    }

                    UnityEngine.Debug.Log(log);
                }
            }
            else
            {
                for (int r = 0; r < toRemove.Count; r++)
                {
                    DestroyImmediate(toRemove[r], true);
                }
            }

            EditorUtility.SetDirty(this);

            if (refreshDatabase)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

#endif

        #endregion


    }


    #region Editor Window

#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(BuildPlannerPreset))]
    public class BuildPlannerPresetEditor : UnityEditor.Editor
    {
        public BuildPlannerPreset Get { get { if (_get == null) _get = (BuildPlannerPreset)target; return _get; } }
        private BuildPlannerPreset _get;

        public override void OnInspectorGUI()
        {
            Color preBG = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.125f, .9f, 0.125f);
            if (GUILayout.Button("Open Build Planner in designer window", GUILayout.Height(38))) AssetDatabase.OpenAsset(Get);
            UnityEditor.EditorGUILayout.HelpBox("Build Planner Preset should be displayed via Build Planner Designer Window!", UnityEditor.MessageType.Info);
            GUI.backgroundColor = preBG;

            GUILayout.Space(5f);
            DrawDefaultInspector();

            GUILayout.Space(18f);

            GUILayout.Label("--- Dev Debugging Tools ---", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(4f);

            if (GUILayout.Button("Clear unused stored inside build planner preset")) Get.ClearUnusedAssetsInside();
            if (GUILayout.Button("Switch build planner sub-assets visibility")) Get.SwitchSubAssetsVisibility();

        }
    }
#endif

    #endregion

}