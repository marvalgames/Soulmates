using FIMSpace.Generating.Planner.Nodes;
using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    [CreateAssetMenu(fileName = "PF_", menuName = "FImpossible Creations/Procedural Generation/Create Build Planner Function Node", order = 1101)]
    public partial class PlannerFunctionNode : PlannerRuleBase, IPlanNodesContainer
    {

        #region Function Node Implementations Related

        public string DisplayName = "";
        [HideInInspector] public Object DefaultFunctionsDirectory = null;

        public override string CustomPath { get { return _customPath; } }
        /*[HideInInspector]*/
        [Space(4)]
        public string _customPath = "";
        public string _customTooltip = "";
        public Vector2 ScaleAdjustOffset = Vector2.zero;

        public override string GetNodeTooltipDescription 
        { get { if (string.IsNullOrEmpty(_customTooltip) == false) return _customTooltip; return base.GetNodeTooltipDescription; } }

        public Color NodeColor = new Color(.9f, 0.7f, 0.3f, 1f);
        [HideInInspector] public PlannerFunctionNode ProjectFileParent = null;
        [HideInInspector] public PE_Start StartNode;

        [Space(4)]
        public List<PGGPlanner_NodeBase> Rules = new List<PGGPlanner_NodeBase>();
        public List<PGGPlanner_NodeBase> RuntimeRules { get { return (ProjectFileParent == null || ProjectFileParent == this) ? Rules : ProjectFileParent.Rules; } }

        [HideInInspector] public List<FieldVariable> Variables = new List<FieldVariable>();

        public override Vector2 NodeSize { get { return nodeSize + ScaleAdjustOffset + _editorExpandSize; } }
        private Vector2 nodeSize = new Vector2(200, 140);

        [HideInInspector] public Texture2D Tex_Net;
        public List<PGGPlanner_NodeBase> Procedures { get { return Rules; } }
        public List<PGGPlanner_NodeBase> PostProcedures { get { return Rules; } }
        public ScriptableObject ScrObj { get { return this; } }
        List<FieldVariable> IPlanNodesContainer.Variables { get { return Variables; } }

        public FieldPlanner.LocalVariables GraphLocalVariables { get { if (localVars == null) RefreshNodeParams(); return localVars; } }

        private FieldPlanner.LocalVariables localVars;

        #endregion

        public override bool DrawInputConnector { get { return isExecutable; } }
        public override bool DrawOutputConnector { get { return isExecutable; } }
        bool isExecutable = false;

        /// <summary> Updating display ports and variables </summary>
        public override void RefreshNodeParams()
        {
            ParentPlanner = FieldPlanner.CurrentGraphExecutingPlanner;

            if (Rules == null) Rules = new List<PGGPlanner_NodeBase>();
            if (inputs == null) inputs = new List<PlannerNodes.FunctionNode.FN_Input>();
            if (inputPorts == null) inputPorts = new List<IFGraphPort>();
            if (outputs == null) outputs = new List<PlannerNodes.FunctionNode.FN_Output>();
            if (outputPorts == null) outputPorts = new List<IFGraphPort>();
            if (parameters == null) parameters = new List<PlannerNodes.FunctionNode.FN_Parameter>();

            inputs.Clear(); outputs.Clear(); parameters.Clear();

            DefineDisplayPorts();
            RefreshLocalVariables();
            RefreshDisplayPortInstances();
        }


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            ParentPlanner = FieldPlanner.CurrentGraphExecutingPlanner;
            DefineExecutionStartNode();
            CustomPrepareSubFunctionNodes();

            OnStartReadingNode();

            if (StartNode != null) CallExecution(StartNode, print, newResult);
            if (Debugging) DebuggingInfo = "Running custom function node: " + DisplayName;
        }


        public override void PreGeneratePrepare()
        {
            _Editor_EnsureProjectFileParent();

            ParentPlanner = FieldPlanner.CurrentGraphExecutingPlanner;

            SyncWithProjectFunctionFile();
            DefineExecutionStartNode();
            //OnStartReadingNode();

            RefreshNodeParams(); // Display refresh
            //CallRefreshOnFunctionPorts();

            FGenerators.CheckForNulls(Rules);

            // Need to refresh connections of outside input connections references
            if (ProjectFileParent)
            {
                FGraph_RunHandler.RefreshConnections(ProjectFileParent.Rules);
                //RefreshSubInputs(ProjectFileParent.Rules);
            }

            PreparePortsRefresh();
            PrepareSubFunctionNodes();
            PreGeneratePrepareSubFunctionNodes();
        }

        //public override void OnCustomReadNode()
        //{
        //    PreGeneratePrepareSubFunctionNodes();
        //    //CallCustomReadOnOwnedNodes();
        //}

        //public override void DONT_USE_IT_YET_OnReadPort(IFGraphPort port)
        //{
        //    if ( port is PGGPlannerPort)
        //    {
        //        PGGPlannerPort plannerPrt = port as PGGPlannerPort;
        //        plannerPrt.Clear();
        //    }
        //}

        private void RefreshSubInputs(List<PlannerRuleBase> rules)
        {
            for (int i = 0; i < DisplayPorts; i++)
            {
                var fport = GetFunctionPort(i);
                var port = fport.GetPort();
               
                if (port.IsOutput == false)
                {
                    for (int c = 0; c < port.Connections.Count; c++)
                    {
                        var conn = port.Connections[c];
                        if (conn.PortReference == null)
                        {//
                            UnityEngine.Debug.Log("null port ref port id = " + conn.ConnectedNodePortID);
                        }

                    }
                }
            }
        }

        public override void Prepare(PlanGenerationPrint print)
        {
            RefreshNodeParams();
            //CallReadOnDisplayedPorts();

            if (ProjectFileParent) FGraph_RunHandler.RefreshConnections(ProjectFileParent.Rules);
            //CallRefreshOnFunctionPorts();

            base.Prepare(print);
            PrepareRules(print);
        }


        public override void OnStartReadingNode()
        {
            ParentPlanner = FieldPlanner.CurrentGraphExecutingPlanner;
            CallCustomReadOnOwnedNodes();
            CallReadOnDisplayedPorts();
            CallRefreshOnFunctionPorts();
        }


    }

}