using FIMSpace.Graph;
using UnityEngine;
using System;
using FIMSpace.Generating.Checker;
#if UNITY_EDITOR
using FIMSpace.FEditor;
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.FunctionNode
{

    public class FN_Input : PlannerRuleBase
    {
        [HideInInspector] public string InputName = "Input";
        public override string GetDisplayName(float maxWidth = 120) { return InputName; }
        public override string GetNodeTooltipDescription { get { return "Defining input port for other nodes which will use this function node.\nCan be ordered through inspector window if you select this function node file"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.Externals; } }
        public override EPlannerNodeVisibility NodeVisibility { get { return EPlannerNodeVisibility.JustFunctions; } }

        public override Vector2 NodeSize { get { return new Vector2(Mathf.Max(160, InputName.Length * 12), 84); } }
        public override Color GetNodeColor() { return new Color(.4f, .4f, .4f, .95f); }
        //public override Color _E_GetColor() { return new Color(.8f, .55f, .3f, .95f); }

        public override bool DrawInspector { get { return true; } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }

        public EFunctionPortType InputType = EFunctionPortType.Number;

        [HideInInspector] [Port(EPortPinType.Output, true)] public IntPort IntInput;
        [HideInInspector] [Port(EPortPinType.Output, true)] public BoolPort BoolInput;
        [HideInInspector] [Port(EPortPinType.Output, true)] public FloatPort FloatInput;
        [HideInInspector] [Port(EPortPinType.Output, true)] public PGGVector3Port Vector3Input;
        [HideInInspector] [Port(EPortPinType.Output, true)] public PGGStringPort StringInput;
        [HideInInspector] [Port(EPortPinType.Output, true)] public PGGCellPort CellInput;
        [HideInInspector] [Port(EPortPinType.Output, true)] public PGGPlannerPort FieldInput;


        public NodePortBase GetFunctionOutputPort()
        {
            switch (InputType)
            {
                case EFunctionPortType.Int: return IntInput;
                case EFunctionPortType.Bool: return BoolInput;
                case EFunctionPortType.Number: return FloatInput;
                case EFunctionPortType.Vector3: return Vector3Input;
                case EFunctionPortType.String: return StringInput;
                case EFunctionPortType.Cell: return CellInput;
                case EFunctionPortType.Field: return FieldInput;
            }

            return null;
        }



        public override void PreGeneratePrepare()
        {
            if ( InputType == EFunctionPortType.Field)
            {
                if (FieldInput == null) return;
                FieldInput.Clear();
            }
        }


        public static void SetValueToPort(NodePortBase port, NodePortBase otherPort)
        {
            if (otherPort == null) return;
            object o = otherPort.GetPortValueSafe;

            if ( port is IntPort)
            {
                IntPort p = port as IntPort;
                if (o != null) if (o is int || o is float || o is double || o is Single) p.Value = Mathf.RoundToInt(Convert.ToSingle(o));
            }
            else if (port is BoolPort)
            {
                BoolPort p = port as BoolPort;
                if (o != null) if (o is bool) p.Value = (bool)o;
            }
            else if (port is FloatPort)
            {
                FloatPort p = port as FloatPort;
                if (o != null) if (o is float) p.Value = (float)(o); else p.Value = Convert.ToSingle(o);
            }
            else if (port is PGGVector3Port)
            {
                PGGVector3Port p = port as PGGVector3Port;
                if (o != null) p.Value = (Vector3)o;

            }
            else if (port is PGGStringPort)
            {
                PGGStringPort p = port as PGGStringPort;
                if (o != null) if (o is string) p.StringVal = (string)o;

            }
            else if (port is PGGCellPort)
            {
                PGGCellPort p = port as PGGCellPort;
                if (otherPort is PGGCellPort) p.ProvideFullCellData(otherPort as PGGCellPort);

            }
            else if (port is PGGPlannerPort)
            {
                PGGPlannerPort p = port as PGGPlannerPort;
                p.Clear();

                if (o is CheckerField3D)
                {
                    p.Output_Provide_Checker(o as CheckerField3D);
                    return;
                }

                p.CopyValuesOfOtherPort(otherPort);
            }
        }

        public void SetValueOf(NodePortBase p)
        {
            if (p == null) { /*UnityEngine.Debug.Log("Null port value!");*/ return; }
            SetValueToPort(GetFunctionOutputPort(), p);
        }


#if UNITY_EDITOR

        public override bool Editor_PreBody()
        {
            Rect r = new Rect(NodeSize.x - 37, 18, 14, 14);
            if (GUI.Button(r, new GUIContent(FGUI_Resources.Tex_Rename), EditorStyles.label))
            {
                string filename = EditorUtility.SaveFilePanelInProject("Type new name (no file will be created)", InputName, "", "Type new display name for the input (no file will be created)");
                if (!string.IsNullOrEmpty(filename)) InputName = System.IO.Path.GetFileNameWithoutExtension(filename);
            }

            Color preC = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            r.size = new Vector2(12, 12);
            r.position = new Vector2(24, r.position.y + 1);
            //if (_port == null) _port = Resources.Load<Texture2D>("ESPR_InputConnected");
            if (_port2 == null) _port2 = Resources.Load<Texture2D>("ESPR_Input.fw");
            GUI.DrawTexture(r, _port2);
            //GUI.DrawTexture(r, _port);
            GUI.color = preC;

            return false;
        }

        //Texture2D _port = null;
        Texture2D _port2 = null;

        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            //UnityEditor.EditorGUILayout.BeginVertical();
            InputType = (EFunctionPortType)UnityEditor.EditorGUILayout.EnumPopup(InputType, GUILayout.Width(NodeSize.x - 80));

            GUILayout.Space(-20);
            NodePortBase port = null;

            IntInput.AllowDragWire = false;
            BoolInput.AllowDragWire = false;
            FloatInput.AllowDragWire = false;
            Vector3Input.AllowDragWire = false;
            StringInput.AllowDragWire = false;
            CellInput.AllowDragWire = false;
            FieldInput.AllowDragWire = false;

            switch (InputType)
            {
                case EFunctionPortType.Int: port = IntInput; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("IntInput")); break;
                case EFunctionPortType.Bool: port = BoolInput; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("BoolInput")); break;
                case EFunctionPortType.Number: port = FloatInput; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("FloatInput")); break;
                case EFunctionPortType.Vector3: port = Vector3Input; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("Vector3Input")); break;
                case EFunctionPortType.String: port = StringInput; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("StringInput")); break;
                case EFunctionPortType.Cell: port = CellInput; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("CellInput")); break;
                case EFunctionPortType.Field: port = FieldInput; EditorGUILayout.PropertyField(baseSerializedObject.FindProperty("FieldInput")); break;
            }

            if (port != null) port.AllowDragWire = true;

        }

#endif

#if UNITY_EDITOR

        SerializedProperty sp_InputName = null;
        public override void Editor_OnAdditionalInspectorGUI()
        {
            if (sp_InputName == null) sp_InputName = baseSerializedObject.FindProperty("InputName");
            EditorGUILayout.PropertyField(sp_InputName);
            GUILayout.Space(4);

            UnityEditor.EditorGUILayout.LabelField("Debugging:", UnityEditor.EditorStyles.helpBox);

            switch (InputType)
            {
                case EFunctionPortType.Int: GUILayout.Label("Port Value: " + IntInput.Value); break;
                case EFunctionPortType.Bool: GUILayout.Label("Port Value: " + BoolInput.Value); break;
                case EFunctionPortType.Number: GUILayout.Label("Port Value: " + FloatInput.Value); break;
                case EFunctionPortType.Vector3: GUILayout.Label("Port Value: " + Vector3Input.Value); break;
                case EFunctionPortType.String: GUILayout.Label("Port Value: " + StringInput.StringVal); break;

                case EFunctionPortType.Cell:
                    GUILayout.Label("Port Value: " + CellInput.Cell);
                    if (CellInput.Cell != null) GUILayout.Label("Cell Pos: " + CellInput.Cell.Pos);
                    if (CellInput.Checker != null)
                    {
                        GUILayout.Label("Cell World Pos: " + CellInput.Checker.GetWorldPos(CellInput.Cell));

                        if ( CellInput.GetInputCheckerValue != null)
                        GUILayout.Label("Parent Field Cells Count: " + CellInput.GetInputCheckerValue.ChildPositionsCount);
                    }

                    break;

                case EFunctionPortType.Field: 
                    GUILayout.Label("Port Value: " + FieldInput.GetNumberedIDArrayString()); 
                    //GUILayout.Label("Array Value: " + FieldInput.GetNumberedIDArrayString()); 
                    break;

            }

        }
#endif

    }
}