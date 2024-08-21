#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.BuildSetup
{

    public class PR_CallGraph : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Call Graph (experimental)" : "Call Other Graph Logic"; }
        public override string GetNodeTooltipDescription { get { return "Calling other graph nodes execution"; } }
        public override Color GetNodeColor() { return new Color(0.3f, .9f, 0.75f, 1f); }
        public override Vector2 NodeSize { get { return new Vector2(272, 130); } }
        public override EPlannerNodeVisibility NodeVisibility { get { return EPlannerNodeVisibility.JustPlanner; } }

        [HideInInspector] public int FieldID = 0;
        /// <summary> -3 is none, -2 is procedures, -1 is post procedures, 0 1,2,3 are sub graphs </summary>
        [HideInInspector] public int GraphID = -3;

        public enum EGraphExecutorSimulation
        {
            ThisPlanner, ExecutedPlanner
        }

        [Tooltip("If nodes executed on the target graph should use 'self' references as this graph or the target executed graph")]
        [HideInInspector] public EGraphExecutorSimulation SimulateExecutorAs = EGraphExecutorSimulation.ThisPlanner;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            if (GraphID == -3) return;

            if (ParentPlanner != null)
            {
                if (ParentPlanner.ParentBuildPlanner)
                {
                    FieldPlanner selectedPlanner = ParentPlanner.ParentBuildPlanner.BasePlanners[FieldID];
                    if (selectedPlanner == null) return;

                    System.Collections.Generic.List<Planner.Nodes.PGGPlanner_NodeBase> graphNodes = null;
                    if (GraphID == -2) graphNodes = selectedPlanner.Procedures;
                    else if (GraphID == -1) graphNodes = selectedPlanner.PostProcedures;
                    else
                    {
                        if (selectedPlanner.FSubGraphs == null || selectedPlanner.FSubGraphs.Count == 0) return;
                        if (!selectedPlanner.FSubGraphs.ContainsIndex(GraphID)) return;
                        if (selectedPlanner.FSubGraphs[GraphID] == null) return;
                        graphNodes = selectedPlanner.FSubGraphs[GraphID].Procedures;
                    }

                    if (graphNodes == null) return;

                    // Search for START node
                    PE_Start start = null;

                    for (int i = 0; i < graphNodes.Count; i++)
                    {
                        if (graphNodes[i] is PE_Start) { start = graphNodes[i] as PE_Start; break; }
                    }

                    if (start == null) return;

                    var trueExecutedGraph = CurrentExecutingPlanner;
                    //var memorizeResult = print.PlannerResults[ParentPlanner.IndexOnPrint];

                    if (SimulateExecutorAs == EGraphExecutorSimulation.ExecutedPlanner)
                    {
                        FieldPlanner.CurrentGraphExecutingPlanner = selectedPlanner;
                        //print.PlannerResults[ParentPlanner.IndexOnPrint] = print.PlannerResults[selectedPlanner.IndexOnPrint];
                    }

                    if (start.ParentNodesContainer is PlannerRuleBase)
                    {
                        PlannerRuleBase plRule = start as PlannerRuleBase;
                        plRule.OnCustomReadNode();
                    }

                    ParentPlanner.CallExecution(start, print);

                    if (SimulateExecutorAs == EGraphExecutorSimulation.ExecutedPlanner)
                    {
                        //print.PlannerResults[ParentPlanner.IndexOnPrint] = memorizeResult;
                        FieldPlanner.CurrentGraphExecutingPlanner = trueExecutedGraph;
                    }
                }
            }
        }


#if UNITY_EDITOR
        private UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (ParentPlanner != null)
            {
                EditorGUILayout.BeginHorizontal();
                UnityEditor.EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("Get From:", GUILayout.Width(100));
                FieldID = EditorGUILayout.IntPopup(FieldID, ParentPlanner.GetPlannersNameList(), ParentPlanner.GetPlannersIDList());
                if (UnityEditor.EditorGUI.EndChangeCheck()) _editorForceChanged = true;

                EditorGUILayout.EndHorizontal();


                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Selected Graph:", GUILayout.Width(100));

                string name = "None";
                if (GraphID == -2) name = "Procedures";
                else if (GraphID == -1) name = "Post Procedures";
                else if (GraphID > -1) name = "Sub Graph [" + GraphID + "]";

                FieldPlanner selectedPlanner = null;
                if (ParentPlanner.ParentBuildPlanner)
                {
                    selectedPlanner = ParentPlanner.ParentBuildPlanner.BasePlanners[FieldID];
                }

                if (GUILayout.Button(name, EditorStyles.layerMaskField))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("None"), GraphID == -3, () => { GraphID = -3; });
                    menu.AddItem(new GUIContent("Procedures"), GraphID == -2, () => { GraphID = -2; });
                    menu.AddItem(new GUIContent("Post Procedures"), GraphID == -1, () => { GraphID = -1; });

                    if (selectedPlanner != null)
                        if (selectedPlanner.FSubGraphs != null)
                        {
                            for (int i = 0; i < selectedPlanner.FSubGraphs.Count; i++)
                            {
                                var sub = selectedPlanner.FSubGraphs[i];
                                if (sub == null) continue;

                                int gr = i;
                                menu.AddItem(new GUIContent("Sub Graph [" + gr + "] : " + sub.GetDisplayName()), GraphID == gr, () => { GraphID = gr; });
                            }
                        }

                    menu.ShowAsContext();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);

                if (sp == null) sp = baseSerializedObject.FindProperty("SimulateExecutorAs");
                EditorGUILayout.PropertyField(sp);

                GUILayout.Space(5);
            }
            else
            {
                EditorGUILayout.HelpBox("Cant find parent planner! Try running node for refresh.", MessageType.Info);
            }

            baseSerializedObject.ApplyModifiedProperties();
        }
#endif

    }
}