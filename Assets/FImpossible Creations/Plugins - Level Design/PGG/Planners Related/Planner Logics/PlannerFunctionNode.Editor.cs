using FIMSpace.Generating.Planner.Nodes;
using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Generating.Planning.PlannerNodes.FunctionNode;
using System.Collections.Generic;
using FIMSpace.Graph;
using static FEasing;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    public partial class PlannerFunctionNode
    {

        #region Utilities

        public override Color GetNodeColor()
        {
            return NodeColor;
        }

        public override string GetDisplayName(float maxWidth = 120)
        {
            string nme = DisplayName;
            if (string.IsNullOrEmpty(nme)) return name;
            return nme + " (f)";
        }

        #endregion

        private Vector2 _editorExpandSize = new Vector2(0, 0);
#if UNITY_EDITOR
        List<SerializedProperty> sp_portsToDisplay = new List<SerializedProperty>();
        SerializedProperty sp_parent = null;
        GUIContent _measureCont = new GUIContent();
        bool firstDraw = false;
        bool _editor_wrongName = false;
        bool _editor_isCloneName = false;

        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            float height = 70;
            float singleLine = EditorGUIUtility.singleLineHeight;

            if (firstDraw == false)
            {
                _editor_isCloneName = name.Contains("(Clone)");
                RefreshDisplayPortInstances();
                firstDraw = true;
            }

            for (int i = 0; i < sp_portsToDisplay.Count; i++)
            {
                EditorGUILayout.PropertyField(sp_portsToDisplay[i]);
                height += singleLine;
            }

            if (sp_portsToDisplay.Count == 0) height = 54;

            _measureCont.text = DisplayName;
            float measure = EditorStyles.boldLabel.CalcSize(_measureCont).x + 70;

            nodeSize = new Vector2(Mathf.Max(210, measure), height);


            if (sp_parent == null)
            {
                sp_parent = baseSerializedObject.FindProperty("ProjectFileParent");
                _Editor_CheckWrongName();
            }

            if (_editor_wrongName)
            {
                _Editor_CheckWrongNameGUI(sp_parent);
                _editorExpandSize.x = 38;
                _editorExpandSize.y = 114;
            }
            else
            {
                _editorExpandSize.x = 0;
                _editorExpandSize.y = 0;
            }
        }

        void _Editor_CheckWrongNameGUI(SerializedProperty sp)
        {

            if (ProjectFileParent == null)
            {
                if (_editor_isCloneName)
                {
                    EditorGUILayout.HelpBox("Detected '(clone)' in the Function Node's Instance parent name. Looks like it lost reference to the function node. Try assigning right, project file asset of this node to fix it.", MessageType.Warning);
                    if (sp != null)
                    {
                        EditorGUILayout.PropertyField(sp);
                        if (ProjectFileParent) EditorGUILayout.LabelField(ProjectFileParent.name);
                        if (sp.serializedObject.ApplyModifiedProperties()) _Editor_CheckWrongName();
                    }
                }

                return;
            }

            EditorGUILayout.HelpBox("Detected '(clone)' in the Function Node's Instance parent name. Looks like it lost the right reference by some isse. Try assigning right, project file asset of this node to fix it.", MessageType.Warning);
            if (sp != null)
            {
                EditorGUILayout.PropertyField(sp);
                if (ProjectFileParent) EditorGUILayout.LabelField(ProjectFileParent.name);
                if (sp.serializedObject.ApplyModifiedProperties()) _Editor_CheckWrongName();
            }
        }

        void _Editor_CheckWrongName()
        {
            if (ProjectFileParent == null)
            {
                if (_editor_isCloneName) _editor_wrongName = true;
                return;
            }

            _editor_wrongName = false;
            if (ProjectFileParent.name.ToLower().Contains("(clone)")) _editor_wrongName = true;
        }
#endif






#if UNITY_EDITOR
        [UnityEditor.CanEditMultipleObjects]
        [UnityEditor.CustomEditor(typeof(PlannerFunctionNode))]
        public class PlannerFunctionNodeEditor : UnityEditor.Editor
        {
            public PlannerFunctionNode Get { get { if (_get == null) _get = (PlannerFunctionNode)target; return _get; } }
            private PlannerFunctionNode _get;

            bool IsPortNode(PGGPlanner_NodeBase node)
            {
                if (node is FN_Input || node is FN_Output || node is FN_Parameter) return true;
                return false;
            }

            SerializedProperty sp_parent = null;

            private void OnEnable()
            {
                sp_parent = serializedObject.FindProperty("ProjectFileParent");
            }

            PlannerFunctionNode parentCheck = null;

            public override void OnInspectorGUI()
            {
                if (parentCheck == null)
                {
                    Get._Editor_CheckWrongName();
                    if (Get.ProjectFileParent != null) parentCheck = Get.ProjectFileParent;
                }

                serializedObject.Update();

                GUILayout.Space(4f);
                DrawPropertiesExcluding(serializedObject, "m_Script");


                GUILayout.Space(4f);

                FGUI_Inspector.FoldHeaderStart(ref portsFoldout, "Ports Order", FGUI_Resources.BGInBoxStyle);

                if (portsFoldout)
                {
                    int locI = 0;
                    //int realI = 0;
                    int firstI = 0;
                    int lastI = 0;

                    for (int i = 0; i < Get.Procedures.Count; i++)
                    {
                        if (Get.Procedures[i] is FN_Input || Get.Procedures[i] is FN_Output || Get.Procedures[i] is FN_Parameter)
                        {
                            if (locI == 0) firstI = i;
                            lastI = i;
                            locI += 1;
                        }
                    }

                    locI = 0;

                    for (int i = 0; i < Get.Procedures.Count; i++)
                    {
                        if (Get.Procedures[i] is FN_Input || Get.Procedures[i] is FN_Output || Get.Procedures[i] is FN_Parameter)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField(Get.Procedures[i].GetDisplayName(), Get.Procedures[i], typeof(PGGPlanner_NodeBase), true);

                            if (lastI != i)
                            {
                                if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_ArrowDown), FGUI_Resources.ButtonStyle, GUILayout.Width(20)))
                                {
                                    for (int j = i + 1; j < Get.Procedures.Count; j++)
                                    {
                                        var nde = Get.Procedures[j];
                                        if (IsPortNode(nde))
                                        {
                                            Get.Procedures[j] = Get.Procedures[i];
                                            Get.Procedures[i] = nde;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (firstI != i)
                            {
                                if (GUILayout.Button(new GUIContent(FGUI_Resources.Tex_ArrowUp), FGUI_Resources.ButtonStyle, GUILayout.Width(20)))
                                {
                                    for (int j = i - 1; j >= 0; j--)
                                    {
                                        var nde = Get.Procedures[j];
                                        if (IsPortNode(nde))
                                        {
                                            Get.Procedures[j] = Get.Procedures[i];
                                            Get.Procedures[i] = nde;
                                            break;
                                        }
                                    }
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.EndVertical();

                if (Get.DefaultFunctionsDirectory)
                {
                    GUILayout.Space(4);
                    if (GUILayout.Button("Move function node to the default directory in project"))
                    {
                        AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(Get), AssetDatabase.GetAssetPath(Get.DefaultFunctionsDirectory) + "/" + Get.name + ".asset");
                        //AssetDatabase.Refresh();
                        //AssetDatabase.SaveAssets();
                        EditorGUIUtility.PingObject(Get);
                    }
                }


                if (Get._editor_wrongName) Get._Editor_CheckWrongNameGUI(sp_parent);


                serializedObject.ApplyModifiedProperties();

            }

            bool portsFoldout = false;

        }
#endif

        void _Editor_EnsureProjectFileParent()
        {
#if UNITY_EDITOR

            if (ProjectFileParent == null)
            {
                if (name.Contains("(Clone)"))
                {
                    var functionNodes = AssetDatabase.FindAssets("t:PlannerFunctionNode");

                    string myOriginalName = name.Substring(0, name.IndexOf("(Clo"));

                    for (int i = 0; i < functionNodes.Length; i++)
                    {
                        PlannerFunctionNode projectFunc = AssetDatabase.LoadAssetAtPath<PlannerFunctionNode>(AssetDatabase.GUIDToAssetPath(functionNodes[i]));
                        if (projectFunc == null) continue;

                        if (projectFunc.name.Contains("(Clone)") == false)
                        {
                            if (projectFunc.name == myOriginalName)
                            {
                                ProjectFileParent = projectFunc;
                                break;
                            }
                        }
                    }

                    //if (PGGInspectorUtilities.LogPGGWarnings)
                    {
                        if (ProjectFileParent != null)
                        {
                            UnityEngine.Debug.Log("[PGG Planner] Function node parent project file was lacking! Automatically found target parent file  for Function Node Instance '" + name + "' (" + ParentNodesContainer?.name + ")");
                            EditorUtility.SetDirty(this);
                        }
                        else
                            UnityEngine.Debug.Log("[PGG Planner] !! Function node parent project file is lacking !! Not found parent file for Function Node Instance '" + name + "' ! (" + ParentNodesContainer?.name + ")\nTry assigning it manually using inspector window (right moouse button on the node -> [Debugging - Display In Inspector Window]))");
                    }

                }
            }

#endif

        }



    }


}