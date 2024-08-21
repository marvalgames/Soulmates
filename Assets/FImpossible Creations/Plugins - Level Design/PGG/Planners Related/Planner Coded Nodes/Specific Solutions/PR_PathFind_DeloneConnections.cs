using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.SpecificSolutions
{

    public class PR_PathFind_DeloneConnections : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return (wasCreated) ? "    Generate Fields Connections" : "Path Find/Generate Fields Connections (Path Find)"; }
        public override string GetNodeTooltipDescription { get { return "Defining possible from room to room paths using 'Delone Triangulation' with the Bowyer–Watson algorithm.\nFirst it finds connection between rooms which ensures all rooms are reachable, then you can use 'Extra Connections' to generate additional possible pathways."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.ReadData; } }
        public override Color GetNodeColor() { return new Color(0.3f, 0.825f, 0.6f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(252, 154 + nodeExtraHeight); } }
        int nodeExtraHeight = 0;

        public override bool IsFoldable { get { return true; } }

        public override int OutputConnectorsCount { get { return 2; } }
        public override int AllowedOutputConnectionIndex { get { return 0; } }
        public override int HotOutputConnectionIndex { get { return 1; } }
        public override string GetOutputHelperText(int outputId = 0)
        {
            if (outputId == 0) return "Finish";
            return "Connect";
        }


        [Tooltip("Group of fields to generate path connection map.")]
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort ChooseFrom;
        [Tooltip("Current iteration Path connection A Field.")]
        [HideInInspector][Port(EPortPinType.Output)] public PGGPlannerPort CurrentA;
        [Tooltip("Current iteration Path connection target B Field.")]

        [HideInInspector][Port(EPortPinType.Output)] public PGGPlannerPort CurrentB;
        [Tooltip("Triggering calls for extra pathway connections.\n!Right Side INPUT Port! Custom Condition Port, use it if you want to prevent some fields from being considered for connecting.")]
        [HideInInspector] public bool CallExtraConnections = false;

        [Tooltip("Extra possible connections count of current executed extra connection.\nCan be used for propability multiplier as connection creation condition.")]
        [HideInInspector][Port(EPortPinType.Output, EPortNameDisplay.Default, EPortValueDisplay.NotEditable)] public IntPort ExtraConnectionsCount;

        [Tooltip("Ignore generating connection between two fields with certain tag")]
        [HideInInspector][Port(EPortPinType.Input, EPortNameDisplay.Default)] public PGGStringPort IgnoreTagged;
        [Tooltip("Useful for rectangle-packing generating connections")]
        [HideInInspector] public bool OnlyAligning = false;


        [HideInInspector] public bool DebugDraw = false;

        [HideInInspector][Port(EPortPinType.Input, true)] public BoolPort CustomCondition;

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            if (CurrentExecutingPlanner == null) return;

            #region Node prepare

            ChooseFrom.Editor_DefaultValueInfo = "(Off)";

            CurrentA.Clear();
            CurrentB.Clear();
            ExtraConnectionsCount.Value = 0;
            ChooseFrom.TriggerReadPort(true);

            List<FieldPlanner> connWithPlanners;

            if (ChooseFrom.IsConnected == false) return;
            else
            {
                connWithPlanners = GetPlannersFromPort(ChooseFrom, false, false);
            }

            if (connWithPlanners.Count <= 1) return;

            #endregion


            for (int i = connWithPlanners.Count - 1; i >= 0; i--)
                if (connWithPlanners[i].LatestChecker.ChildPositionsCount <= 0)
                    connWithPlanners.RemoveAt(i);

            if (connWithPlanners.Count <= 1) return;

            CheckTriangulation(connWithPlanners);

            if (deloneSinglePath == null || deloneSinglePath.Count < connWithPlanners.Count - 1)
            {
                ComputeDistanceBasedConnections(connWithPlanners);
            }

            if (fieldCenters.Count > 0)
            {
                DefineConnections(print);
            }

            #region Debugging Gizmos

#if UNITY_EDITOR

            if (Debugging)
            {
                DebuggingInfo = "Generating triangulated fields connection";

                DeloneField field = deloneField;

                DebuggingGizmoEvent = () =>
                {
                    if (field != null)
                    {
                        Gizmos.color = Color.green;
                        Vector3 mainOff = Vector3.up;

                        Gizmos.DrawRay(Vector3.zero, Vector3.up * 100);
                        for (int i = 0; i < field.Verts.Count; i++)
                        {
                            Gizmos.DrawRay(field.Verts[i].Position + mainOff, Vector3.up * 1f);
                        }

                        Gizmos.color = Color.green;
                        for (int i = 0; i < field.Edges.Count; i++)
                        {
                            var tr = field.Edges[i];
                            Gizmos.DrawLine(tr.S.Position + mainOff, tr.E.Position + mainOff);
                        }

                        for (int i = 0; i < field.Verts.Count; i++)
                        {
                            var f = field.Verts[i] as DeloneField.Vertex<DeloneFindHelper>;
                            Vector3 off = Vector3.up * 0.25f + mainOff;

                            Gizmos.color = Color.red;
                            for (int c = 0; c < f.Item.mainConnections.Count; c++)
                            {
                                Gizmos.DrawLine(f.Position + off, f.Item.mainConnections[c].targetVertex.Position + off);
                            }

                            Gizmos.color = Color.blue;
                            off = Vector3.up * 0.5f;
                            for (int c = 0; c < f.Item.secondaryConnections.Count; c++)
                            {
                                Gizmos.DrawLine(f.Position + off, f.Item.secondaryConnections[c].targetVertex.Position + off);
                            }
                        }

                    }
                };
            }
#endif
            #endregion

        }

        protected virtual void DefineConnections(PlanGenerationPrint print)
        {
            IgnoreTagged.TriggerReadPort(true);
            string ignore = IgnoreTagged.GetInputValue;
            bool ignoring = !string.IsNullOrEmpty(ignore);

            // Main path connection
            for (int v = 0; v < fieldCenters.Count; v++)
            {
                DeloneField.Vertex<DeloneFindHelper> vert = fieldCenters[v] as DeloneField.Vertex<DeloneFindHelper>;
                if (vert != null)
                {
                    DeloneFindHelper helper = vert.Item;
                    CurrentA.Output_Provide_Planner(helper.planner);

                    for (int m = 0; m < helper.mainConnections.Count; m++)
                    {
                        DeloneFindHelper other = helper.mainConnections[m].targetVertex.Item;

                        if (ignoring)
                        {
                            if (
                            helper.planner.IsTagged(ignore) && other.planner.IsTagged(ignore))
                                continue;
                        }

                        CurrentB.Output_Provide_Planner(other.planner);
                        CallOtherExecutionWithConnector(1, print);
                    }
                }
            }

            if (CallExtraConnections)
            {
                for (int v = 0; v < fieldCenters.Count; v++)
                {
                    // Extra pathways call
                    DeloneField.Vertex<DeloneFindHelper> vertx = fieldCenters[v] as DeloneField.Vertex<DeloneFindHelper>;

                    if (vertx != null)
                    {
                        DeloneFindHelper helper = vertx.Item;
                        CurrentA.Output_Provide_Planner(helper.planner);
                        ExtraConnectionsCount.Value = helper.secondaryConnections.Count;

                        for (int m = 0; m < helper.secondaryConnections.Count; m++)
                        {
                            var conn = helper.secondaryConnections[m];
                            if (conn.connectionEdge.Used) continue;

                            DeloneFindHelper other = conn.targetVertex.Item;

                            CurrentB.Output_Provide_Planner(other.planner);

                            if (CustomCondition.IsConnected)
                            {
                                CustomCondition.TriggerReadPort(true);
                                if (CustomCondition.GetInputValue == false) continue;
                            }

                            CallOtherExecutionWithConnector(1, print);
                            conn.connectionEdge.Used = true;
                        }
                    }
                }
            }
        }


        #region Editor Code

#if UNITY_EDITOR
        SerializedProperty sp = null;
        SerializedProperty spCust = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);
            nodeExtraHeight = 0;

            baseSerializedObject.Update();

            if (sp == null) sp = baseSerializedObject.FindProperty("ChooseFrom");

            SerializedProperty s = sp.Copy();
            EditorGUILayout.PropertyField(s);
            //s.Next(false); EditorGUILayout.PropertyField(s);

            s.Next(false); EditorGUILayout.PropertyField(s);

            s.Next(false); EditorGUILayout.PropertyField(s);


            if (spCust == null) spCust = baseSerializedObject.FindProperty("CustomCondition");
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 140;
            s.Next(false); EditorGUILayout.PropertyField(s, GUILayout.Width(NodeSize.x - 76));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.PropertyField(spCust, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            s.Next(false);
            if (CallExtraConnections)
            {
                nodeExtraHeight += 21;
                EditorGUILayout.PropertyField(s);
            }

            if (ChooseFrom.IsConnected == false)
            {
                GUILayout.Space(-1);
                nodeExtraHeight += 18;
                EditorGUILayout.HelpBox("Require Multiple Fields as Input", MessageType.None);
            }



            if (_EditorFoldout)
            {
                IgnoreTagged.AllowDragWire = true;
                s.Next(false);
                EditorGUILayout.PropertyField(s);
                s.Next(false);
                EditorGUILayout.PropertyField(s);
                nodeExtraHeight += 18;
                nodeExtraHeight += 18;
            }
            else
            {
                IgnoreTagged.AllowDragWire = false;
            }



            if (DebugDraw)
            {
                if (deloneField != null)
                {
                    for (int i = 0; i < deloneField.Verts.Count; i++)
                    {
                        UnityEngine.Debug.DrawRay(deloneField.Verts[i].Position, Vector3.up * 1f, Color.green, 0.01f);
                    }

                    for (int i = 0; i < deloneField.Edges.Count; i++)
                    {
                        var tr = deloneField.Edges[i];
                        UnityEngine.Debug.DrawLine(tr.S.Position, tr.E.Position, Color.green, 0.01f);
                    }

                    for (int i = 0; i < deloneField.Verts.Count; i++)
                    {
                        var f = deloneField.Verts[i] as DeloneField.Vertex<DeloneFindHelper>;
                        Vector3 off = Vector3.up * 0.25f;

                        for (int c = 0; c < f.Item.mainConnections.Count; c++)
                        {
                            UnityEngine.Debug.DrawLine(f.Position + off, f.Item.mainConnections[c].targetVertex.Position + off, Color.red, 0.01f);
                        }

                        off = Vector3.up * 0.5f;
                        for (int c = 0; c < f.Item.secondaryConnections.Count; c++)
                        {
                            UnityEngine.Debug.DrawLine(f.Position + off, f.Item.secondaryConnections[c].targetVertex.Position + off, Color.blue, 0.01f);
                        }
                    }

                    //if (deloneSinglePath != null)
                    //{
                    //    Vector3 off = Vector3.up * 0.1f;
                    //    for (int i = 0; i < deloneSinglePath.Count; i++)
                    //    {
                    //        var tr = deloneSinglePath[i];
                    //        UnityEngine.Debug.DrawLine(tr.S.Position + off, tr.E.Position + off, Color.red, 0.01f);
                    //    }
                    //}
                }
            }

            baseSerializedObject.ApplyModifiedProperties();

        }

        public override void Editor_OnAdditionalInspectorGUI()
        {
            EditorGUILayout.LabelField("Debugging:", EditorStyles.helpBox);
            DebugDraw = EditorGUILayout.Toggle("DebugDraw", DebugDraw);
        }

#endif

        #endregion


        #region Delone Triangulation


        protected class DeloneFindHelper
        {
            public DeloneFindHelper(FieldPlanner plann)
            {
                planner = plann;
            }

            public FieldPlanner planner;
            public DeloneField.Vertex<DeloneFindHelper> vertex;
            public List<DeloneConnection> mainConnections = new List<DeloneConnection>();
            public List<DeloneConnection> secondaryConnections = new List<DeloneConnection>();
            public int helperVariable = 0;

            public bool DoesMainContains(DeloneField.Vertex<DeloneFindHelper> o)
            {
                for (int c = 0; c < mainConnections.Count; c++)
                    if (mainConnections[c].targetVertex == o) return true;
                return false;
            }

            public bool DoesSecondaryContains(DeloneField.Vertex<DeloneFindHelper> o)
            {
                for (int c = 0; c < secondaryConnections.Count; c++)
                    if (secondaryConnections[c].targetVertex == o) return true;
                return false;
            }

            public DeloneConnection GenerateConnection(DeloneField.Vertex target, DeloneField.Edge edge)
            {
                DeloneConnection conn = new DeloneConnection()
                {
                    parent = this,
                    parentVertex = vertex,
                    targetVertex = target as DeloneField.Vertex<DeloneFindHelper>,
                    connectionEdge = edge
                };

                return conn;
            }
        }

        protected struct DeloneConnection
        {
            public DeloneFindHelper parent;
            public DeloneField.Vertex<DeloneFindHelper> parentVertex;
            public DeloneField.Vertex<DeloneFindHelper> targetVertex;
            public DeloneField.Edge connectionEdge;
        }


        DeloneField deloneField = null;
        List<DeloneField.Edge> deloneSinglePath = null;
        protected List<DeloneField.Vertex> fieldCenters = new List<DeloneField.Vertex>();
        void CheckTriangulation(List<FieldPlanner> targets)
        {
            fieldCenters = new List<DeloneField.Vertex>();

            // Create Rooms as Vertex List
            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 pos = targets[i].LatestChecker.GetFullBoundsWorldSpace().center; 
                
                FieldCell centerCell = targets[i].LatestChecker.GetNearestCellInWorldPos(pos);
                if (centerCell.Available()) pos = targets[i].LatestChecker.GetWorldPos(centerCell);

                pos.y = targets[i].LatestChecker.RootPosition.y;
                DeloneField.Vertex<DeloneFindHelper> checkerCenter = new DeloneField.Vertex<DeloneFindHelper>(pos, new DeloneFindHelper(targets[i]));
                checkerCenter.Item.vertex = checkerCenter;

                fieldCenters.Add(checkerCenter);

                //if (lastPos != Vector3.zero) UnityEngine.Debug.DrawLine(targets[i].LatestChecker.GetFullBoundsWorldSpace().center, lastPos, Color.green, 1.01f);
            }


            deloneField = new DeloneField(fieldCenters);

            deloneField.CalculateConcaveTriangulate();

            if (OnlyAligning) // Restricting to not connect with fields which are disconnected (not aligning with any cell)
                deloneSinglePath = DeloneField.MinimumEdgesAllVertexConnectionGroup(deloneField.Edges, deloneField.Verts[0], CheckAllowConnection);
            else
                deloneSinglePath = DeloneField.MinimumEdgesAllVertexConnectionGroup(deloneField.Edges, deloneField.Verts[0]);

            if (deloneSinglePath == null) return;

            for (int p = 0; p < deloneSinglePath.Count; p++)
            {
                var pathEdge = deloneSinglePath[p];

                var f = pathEdge.S as DeloneField.Vertex<DeloneFindHelper>;
                var helper = f.Item;

                // Define the main connection which makes all rooms being connected
                helper.mainConnections.Add(helper.GenerateConnection(pathEdge.E, pathEdge));
            }

            // Check all edges for secondary connections definition
            for (int a = 0; a < deloneField.Edges.Count; a++)
            {
                var edge = deloneField.Edges[a];
                var s = edge.S as DeloneField.Vertex<DeloneFindHelper>;
                var e = edge.E as DeloneField.Vertex<DeloneFindHelper>;

                if (s.Item.DoesMainContains(e) == false)
                    if (e.Item.DoesMainContains(s) == false)
                    {
                        if (!s.Item.DoesSecondaryContains(e)) s.Item.secondaryConnections.Add(s.Item.GenerateConnection(e, edge));
                        if (!e.Item.DoesSecondaryContains(s)) e.Item.secondaryConnections.Add(e.Item.GenerateConnection(s, edge));
                    }
            }

        }

        bool CheckAllowConnection(DeloneField.Vertex a, DeloneField.Vertex b)
        {
            DeloneField.Vertex<DeloneFindHelper> va = a as DeloneField.Vertex<DeloneFindHelper>;
            DeloneField.Vertex<DeloneFindHelper> vb = b as DeloneField.Vertex<DeloneFindHelper>;

            if (va != null && vb != null)
            {
                if (!va.Item.planner.CheckerReference.IsAnyAligning(vb.Item.planner.CheckerReference))
                {
                    //UnityEngine.Debug.DrawLine(
                    //    va.Item.planner.CheckerReference.GetFullBoundsWorldSpace().center,
                    //    vb.Item.planner.CheckerReference.GetFullBoundsWorldSpace().center, Color.red, 1.01f);
                    return false;
                }
                else
                {
                    //va.Item.planner.CheckerReference.DebugLogDrawCellsInWorldSpace(Color.blue);
                    //vb.Item.planner.CheckerReference.DebugLogDrawCellsInWorldSpace(Color.green);
                }
                //UnityEngine.Debug.Log("Checking " + va.Item.planner.ArrayNameString + " and " + vb.Item.planner.ArrayNameString);
            }

            return true;
        }

        #endregion


        #region Non-Delone Definition in case delone fails

        void ComputeDistanceBasedConnections(List<FieldPlanner> targets)
        {
            fieldCenters = new List<DeloneField.Vertex>();
            var edges = new List<DeloneField.Edge>();

            for (int i = 0; i < targets.Count; i++)
            {
                Vector3 pos = targets[i].LatestChecker.GetFullBoundsWorldSpace().center; pos.y = targets[i].LatestChecker.RootPosition.y;
                DeloneField.Vertex<DeloneFindHelper> checkerCenter = new DeloneField.Vertex<DeloneFindHelper>(pos, new DeloneFindHelper(targets[i]));
                checkerCenter.Item.vertex = checkerCenter;
                fieldCenters.Add(checkerCenter);
            }

            for (int c = 0; c < fieldCenters.Count; c++)
            {
                var myVert = fieldCenters[c] as DeloneField.Vertex<DeloneFindHelper>;

                float nearestDist = float.MaxValue;
                DeloneField.Vertex<DeloneFindHelper> nearestAvailable = null;

                for (int o = 0; o < fieldCenters.Count; o++)
                {
                    if (c == o) continue; // Skip self
                    var other = fieldCenters[o] as DeloneField.Vertex<DeloneFindHelper>;
                    if (other.Item.helperVariable > 2) continue;
                    if (other.Item.mainConnections.Count > 0) continue;

                    float distance = Vector3.Distance(myVert.Position, other.Position);

                    if (distance < nearestDist)
                    {
                        nearestDist = distance;
                        nearestAvailable = other;
                    }
                }

                if (nearestAvailable != null)
                {
                    var edge = new DeloneField.Edge(myVert, nearestAvailable);
                    myVert.Item.mainConnections.Add(new DeloneConnection()
                    {
                        connectionEdge = edge,
                        parent = myVert.Item,
                        parentVertex = myVert,
                        targetVertex = nearestAvailable
                    });

                    myVert.Item.helperVariable += 1;
                    nearestAvailable.Item.helperVariable += 1;
                    edges.Add(edge);
                }
            }

            deloneSinglePath = DeloneField.MinimumEdgesAllVertexConnectionGroup(edges, fieldCenters[0]);
        }

        #endregion

    }
}