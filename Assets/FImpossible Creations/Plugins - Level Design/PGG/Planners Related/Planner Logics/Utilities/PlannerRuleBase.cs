#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using FIMSpace.Generating.Planner.Nodes;
using FIMSpace.Graph;
using System;
using System.Collections.Generic;
using UnityEngine;
using FIMSpace.Generating.Rules.QuickSolutions;
using FIMSpace.Generating.Checker;

namespace FIMSpace.Generating.Planning.PlannerNodes
{

    /// <summary>
    /// It's always sub-asset -> it's never project file asset
    /// </summary>
    public abstract partial class PlannerRuleBase : PGGPlanner_NodeBase
    {
        public static bool Debugging = false;

        /// <summary> Warning! Duplicate is refering to root project planner, In the nodes logics you can use CurrentExecutingPlanner for instance planner reference </summary>
        [HideInInspector] public FieldPlanner ParentPlanner;
        public FieldPlanner CurrentExecutingPlanner { get { return FieldPlanner.CurrentGraphExecutingPlanner; } }
        public bool IsGeneratingAsync { get { if (CurrentExecutingPlanner == null) return false; if (CurrentExecutingPlanner.ParentBuildPlanner == null) return false; return CurrentExecutingPlanner.ParentBuildPlanner.AsyncGenerating; } }

        [HideInInspector] public ScriptableObject ParentNodesContainer;
        /// <summary> By default the ParentNodesContainer is treated as LiteParentContainer, but other implementations like SubGraphs, which are not scriptable objects, are also LiteParentContainer and requires this variable for being detected </summary>
        [NonSerialized] public IPlanNodesContainer LiteParentContainer = null;

        public string DebuggingInfo { get; protected set; }
        public Action DebuggingGizmoEvent { get; protected set; }

        //public virtual string TitleName() { return GetType().Name; }
        public virtual string Tooltip() { string tooltipHelp = "(" + GetType().Name; return tooltipHelp + ")"; }

        public override Vector2 NodeSize { get { return new Vector2(232, 90); } }
        /// <summary> PlannerRuleBase by default is true </summary>
        public override bool DrawInputConnector { get { return true; } }


        public List<FieldPlanner> GetPlannersFromPort(PGGPlannerPort port, bool newListInstance = false, bool callRead = true)
        {
            return PGGPlannerPort.GetPlannersFromPort(port, newListInstance, callRead);
        }

        /// <summary> Getting connected planner, if there is no planner, it will return self planner </summary>
        public FieldPlanner GetPlannerFromPortAlways(PGGPlannerPort port, bool callRead = true)
        {
            FieldPlanner planner = PGGPlannerPort.GetPlannerFromPort(port, callRead);
            if (planner == null) planner = CurrentExecutingPlanner;
            return planner;
        }

        public FieldPlanner GetPlannerFromPort(PGGPlannerPort port, bool callRead = true)
        {
            return PGGPlannerPort.GetPlannerFromPort(port, callRead);
        }


        public CheckerField3D GetCheckerFromPort(PGGPlannerPort source, bool callRead = true)
        {
            return PGGPlannerPort.GetCheckerFromPort(source, callRead);
        }

        public FieldCell GetCellFromInputPort(IFGraphPort port, bool callRead = false)
        {
            NodePortBase nodePort = port as NodePortBase;
            if (nodePort == null) return null;

            if (callRead) nodePort.TriggerReadPort(true);

            if (nodePort.IsConnected)
            {
                if (nodePort.BaseConnection.PortReference is PGGCellPort)
                {
                    PGGCellPort cellPrt = nodePort.BaseConnection.PortReference as PGGCellPort;
                    if (FGenerators.NotNull(cellPrt.GetInputCellValue)) return cellPrt.GetInputCellValue;
                }
            }

            return null;
        }




        public FieldPlanner GetPlannerByID(int plannerId, int duplicateId = -1, int subId = -1)
        {
            return PGGPlannerPort.GetPlannerByID(plannerId, duplicateId, subId);
        }

        public static FieldPlanner GetFieldPlannerByID(int plannerId, int duplicateId = -1, int subFieldID = -1, bool selfOnUndefined = true)
        {
            return PGGPlannerPort.GetFieldPlannerByID(plannerId, duplicateId, subFieldID, selfOnUndefined);
        }

        /// <summary> [Base Calls OnCustomPrepare] This method can contain preparation code which not need to be async </summary>
        public virtual void PreGeneratePrepare() { OnCustomPrepare(); }
        /// <summary> [Base is empty] .Called by PreGeneratePrepare. For function nodes to refresh some variables when re-executing node. Should be Async friendly. </summary>
        public virtual void OnCustomPrepare() { }


        /// <summary> [Base is not empty] Preparing initial debug message </summary>
        public virtual void Prepare(PlanGenerationPrint print)
        {
#if UNITY_EDITOR
            DebuggingInfo = "Debug Info not Assigned";
#endif
        }

        /// <summary> [Base is empty] </summary>
        public virtual void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            // Node Procedures Code
        }


        protected void CallOtherExecution(FGraph_NodeBase otherNode, PlanGenerationPrint print)
        {
            if (otherNode == null) return;

            FieldPlanner callerParent = ParentPlanner;

            if (callerParent == null)
                if (ParentNodesContainer is PlannerFunctionNode)
                    callerParent = CurrentExecutingPlanner;

            if (callerParent == null)
            {
                if (print == null)
                {
                    if (otherNode is PlannerRuleBase)
                    {
                        PlannerRuleBase oPlannerRule = otherNode as PlannerRuleBase;

                        if (oPlannerRule.ParentPlanner == null)
                        {
                            if (MG_ModGraph != null)
                                MG_ModGraph.CallExecution(otherNode as PlannerRuleBase);
                        }
                        else
                        {
                            callerParent.CallExecution(otherNode as PlannerRuleBase, print);
                        }
                    }
                }

                return;
            }

            if (otherNode is PlannerRuleBase)
            {
                callerParent.CallExecution(otherNode as PlannerRuleBase, print);
            }
        }

        protected void CallOtherExecutionWithConnector(int altId, PlanGenerationPrint print)
        {
            for (int c = 0; c < OutputConnections.Count; c++)
            {
                if (OutputConnections[c].ConnectionFrom_AlternativeID == altId)
                {
                    CallOtherExecution(OutputConnections[c].GetOther(this), print);
                }
            }
        }


        public static void EnsurePlannerRulesOwner(List<PGGPlanner_NodeBase> nodes, object owner)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                PlannerRuleBase rule = nodes[i] as PlannerRuleBase;
                if (rule == null) continue;

                if (owner is ScriptableObject) rule.ParentNodesContainer = owner as ScriptableObject;
                else if (owner is IPlanNodesContainer) rule.LiteParentContainer = owner as IPlanNodesContainer;
            }
        }


        #region Editor related


#if UNITY_EDITOR

        public virtual void OnGUIModify()
        {

        }

        [HideInInspector]
        public bool _editor_drawRule = true;
        protected UnityEditor.SerializedObject inspectorViewSO = null;

        protected virtual void DrawGUIHeader(int i)
        {
            if (inspectorViewSO == null) inspectorViewSO = new UnityEditor.SerializedObject(this);
            EditorGUILayout.BeginHorizontal(FGUI_Resources.BGInBoxLightStyle, GUILayout.Height(20)); // 1

            Enabled = EditorGUILayout.Toggle(Enabled, GUILayout.Width(24));


            string foldout = FGUI_Resources.GetFoldSimbol(_editor_drawRule);
            string tip = Tooltip();


            if (GUILayout.Button(new GUIContent(foldout + "  " + GetDisplayName() + "  " + foldout, tip), FGUI_Resources.HeaderStyle))
            {
                bool rmb = false;
                if (rmb == false) _editor_drawRule = !_editor_drawRule;
            }

            int hh = 18;

            if (i > 0) if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_ArrowUp), FGUI_Resources.ButtonStyle, GUILayout.Width(18), GUILayout.Height(hh))) { FGenerators.SwapElements(ParentPlanner.FProcedures, i, i - 1); return; }
            if (i < ParentPlanner.FProcedures.Count - 1) if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_ArrowDown), FGUI_Resources.ButtonStyle, GUILayout.Width(18), GUILayout.Height(hh))) { FGenerators.SwapElements(ParentPlanner.FProcedures, i, i + 1); return; }

            if (GUILayout.Button("X", FGUI_Resources.ButtonStyle, GUILayout.Width(24), GUILayout.Height(hh)))
            {
                ParentPlanner.RemoveRuleFromPlanner(this);
                return;
            }

            EditorGUILayout.EndHorizontal(); // 1
        }

        protected virtual void DrawGUIFooter()
        {
            EditorGUILayout.EndVertical();

            if (inspectorViewSO.ApplyModifiedProperties())
            {
                OnStartReadingNode();
            }
        }


        //public void DrawGUIStack(int i)
        //{
        //    DrawGUIHeader(i);

        //    Color preColor = GUI.color;

        //    if (inspectorViewSO != null)
        //        if (inspectorViewSO.targetObject != null)
        //            if (_editor_drawRule)
        //            {
        //                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
        //                if (Enabled == false) GUI.color = new Color(0.9f, 0.9f, 0.9f, 0.7f);
        //                inspectorViewSO.Update();
        //                DrawGUIBody();
        //                DrawGUIFooter();
        //            }

        //    GUI.color = preColor;
        //}

        /// <summary>
        /// Returns true if something changed in GUI - using EditorGUI.BeginChangeCheck();
        /// </summary>
        //protected virtual void DrawGUIBody(/*int i*/)
        //{
        //    UnityEditor.SerializedProperty sp = inspectorViewSO.GetIterator();
        //    sp.Next(true);
        //    sp.NextVisible(false);
        //    bool can = sp.NextVisible(false);

        //    if (can)
        //    {
        //        do
        //        {
        //            bool cont = false;
        //            if (cont) continue;

        //            UnityEditor.EditorGUILayout.PropertyField(sp);
        //        }
        //        while (sp.NextVisible(false) == true);
        //    }

        //    //    EditorGUILayout.EndVertical();

        //    //    so.ApplyModifiedProperties();
        //    //}


        //}

#endif

        #endregion


        #region Mod Graph Related

        /// <summary>  Current executing mod graph (for field modification graph) </summary>
        public SR_ModGraph MG_ModGraph { get { return SR_ModGraph.Graph_ModGraph; } }
        /// <summary> Current executing field mod (for field modification graph) </summary>
        public FieldSpawner MG_Spawner { get { return SR_ModGraph.Graph_Spawner; } }
        /// <summary> Current executing field modificator's spawner </summary>
        public FieldModification MG_Mod { get { return SR_ModGraph.Graph_Mod; } }
        /// <summary> Current executing mod spawner's spawn (for field modification graph) </summary>
        public SpawnData MG_Spawn { get { return SR_ModGraph.Graph_SpawnData; } }
        /// <summary> Current executing field setup preset (for field modification graph) </summary>
        public FieldSetup MG_Preset { get { return SR_ModGraph.Graph_Preset; } }
        /// <summary> Current executing field grid cell (for field modification graph) </summary>
        public FieldCell MG_Cell { get { return SR_ModGraph.Graph_Cell; } }
        /// <summary> Current executing field gridd (for field modification graph) </summary>
        public FGenGraph<FieldCell, FGenPoint> MG_Grid { get { return SR_ModGraph.Graph_Grid; } }
        public List<SpawnInstruction> MG_GridInstructions { get { return SR_ModGraph.Graph_Instructions; } }
        ///// <summary> Current executing field mod (for field modification graph) </summary>
        //public Vector3? Graph_RestrictDir { get { return SR_ModGraph.Graph_Mod; } }


        public ModificatorsPack MGGetParentPack()
        {
            SR_ModGraph owner = ParentNodesContainer as SR_ModGraph;
            if (owner == null) return null;
            return owner.TryGetParentModPack();
        }

        public UnityEngine.Object MGGetFieldSetup()
        {
            SR_ModGraph owner = ParentNodesContainer as SR_ModGraph;
            if (owner == null) return null;

            var fs = owner.TryGetParentFieldSetup();
            if (fs == null) return null;

            //if (fs)
            //{
            //    if (fs.InstantiatedOutOf) return fs.InstantiatedOutOf;
            //}

            return fs;
        }

        protected List<FieldVariable> MGGetVariables(UnityEngine.Object tgt)
        {
            if (tgt == null) return null;

            FieldSetup fs = tgt as FieldSetup;
            if (fs)
            {
                return fs.Variables;
            }

            ModificatorsPack mp = tgt as ModificatorsPack;
            if (mp)
            {
                return mp.Variables;
            }

            return null;
        }

        protected FieldVariable MGGetVariable(UnityEngine.Object tgt, int index)
        {
            var variables = MGGetVariables(tgt);
            if (variables == null) return null;
            if (variables.ContainsIndex(index)) return variables[index];
            return null;
        }


        #endregion


        #region Basic Utilities

        protected GUIContent GetTryRefreshGUIC
        {
            get
            {
                return new GUIContent("error - try refresh", "Try refreshing preview re-open graph or try recompilling scripts.");
            }
        }

        #endregion

    }
}
