using FIMSpace.Generating.Planner.Nodes;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    public interface IPlanNodesContainer 
    {

        List<PGGPlanner_NodeBase> Procedures { get; }
        List<PGGPlanner_NodeBase> PostProcedures { get; }
        List<FieldVariable> Variables { get; }
        ScriptableObject ScrObj { get; }
        FieldPlanner.LocalVariables GraphLocalVariables { get; }

    }

    /// <summary>
    /// Plan Nodes Container with common methods extension
    /// </summary>
    public interface IPlanNodesHandler
    {
        IPlanNodesContainer NodesHandler_Container { get; }
        void NodesHandler_OnNodeRemove(FGraph_NodeBase node);
        void NodesHandler_AddRule(FGraph_NodeBase rule, bool postProcedure = false);
    }


}
