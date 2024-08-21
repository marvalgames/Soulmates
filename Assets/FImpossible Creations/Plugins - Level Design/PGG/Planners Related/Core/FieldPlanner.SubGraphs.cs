using UnityEngine;
using System.Collections.Generic;
using FIMSpace.Generating.Planner.Nodes;
using FIMSpace.Graph;
using FIMSpace.Generating.Planning.PlannerNodes;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning
{
    public partial class FieldPlanner
    {

        #region Removing / Adding Subgraphs


        public void AddNewSubGraph()
        {
            if (FSubGraphs == null) FSubGraphs = new List<SubGraph>();
            SubGraph newSubGr = new SubGraph(this);
            FSubGraphs.Add(newSubGr);
            newSubGr.RefreshStartGraphNodes();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveSubGraph(SubGraph sb)
        {
            if (sb == null) return;
            if (FSubGraphs == null) return;

            for (int s = 0; s < FSubGraphs.Count; s++)
            {
                if (FSubGraphs[s] == sb) { RemoveSubGraph(s); return; }
            }
        }

        public void RemoveSubGraph(int index)
        {
            if (FSubGraphs == null)
            {
                FSubGraphs = new List<SubGraph>();
                return;
            }

            if (!FSubGraphs.ContainsIndex(index)) return;
            FSubGraphs.RemoveAt(index);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        #endregion


        [System.Serializable]
        public class SubGraph : IPlanNodesContainer, IPlanNodesHandler
        {
            public string Name = "";

            public FieldPlanner Owner;
            public List<PGGPlanner_NodeBase> FProcedures = new List<PGGPlanner_NodeBase>();
            public LocalVariables FVariables;
            public List<PGGPlanner_NodeBase> Procedures { get { return FProcedures; } }
            public List<PGGPlanner_NodeBase> PostProcedures { get { return null; } }
            public List<FieldVariable> Variables { get { return Owner.Variables; } }
            public ScriptableObject ScrObj { get { return Owner; } }
            public LocalVariables GraphLocalVariables { get { if (FVariables == null) FVariables = new LocalVariables(this); return FVariables; } }


            public enum EExecutionOrder
            {
                [Tooltip("Default: Executing graph when all instanced of the Field Planner ends 'First Procedures'")]
                Default,
                [Tooltip("After Each Instance: Executing graph when the Planner Instance ends 'First Procedures'")]
                AfterEachInstance,
                [Tooltip("Post Procedure: Executing graph when all instanced of the Field Planner ends 'First Procedures' and after Custom Graphs")]
                PostProcedure,
                [Tooltip("Only External Call: Not executing graph, it can be done only through nodes.")]
                OnlyExternalCall
            }

            public EExecutionOrder ExecutionOrder = EExecutionOrder.Default;

            public SubGraph(FieldPlanner creator)
            {
                Owner = creator;
                FVariables = new LocalVariables(this);
            }

            public string GetDisplayName()
            {
                if (Name == "") return "Execution Graph";
                else return Name;
            }



            public IPlanNodesContainer NodesHandler_Container { get { return this; } }

            public void NodesHandler_OnNodeRemove(FGraph_NodeBase node)
            {
#if UNITY_EDITOR
                DestroyImmediate(node, true);
                if (Owner) EditorUtility.SetDirty(Owner);
#endif
            }

            public void NodesHandler_AddRule(FGraph_NodeBase node, bool postProcedure = false)
            {
                PlannerRuleBase rule = node as PlannerRuleBase;
                if (rule == null) return;

                rule.ParentPlanner = Owner;
                rule.hideFlags = HideFlags.HideInHierarchy;

                FProcedures.Add(rule);

#if UNITY_EDITOR
                FGenerators.AddScriptableTo(rule, Owner, false, false);
                EditorUtility.SetDirty(rule);
                EditorUtility.SetDirty(Owner);
#endif

            }

            public PE_Start proceduresBegin = null;
            public void RefreshStartGraphNodes()
            {
                for (int p = 0; p < Procedures.Count; p++)
                {
                    if (Procedures[p] is PE_Start)
                    {
                        proceduresBegin = Procedures[p] as PE_Start;
                        break;
                    }
                }

                if (proceduresBegin == null)
                {
                    PE_Start val = CreateInstance<PE_Start>();
                    val.NodePosition = new Vector2(690, 430);
                    NodesHandler_AddRule(val);
                }
            }

            public void RefreshLocalVariables()
            {
                if (FVariables == null) FVariables = new FieldPlanner.LocalVariables(this);
                FVariables.RefreshList();
            }
        }

    }
}