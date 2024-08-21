using FIMSpace.Generating.Planning.PlannerNodes;
using FIMSpace.Generating.Planning.PlannerNodes.FunctionNode;
using FIMSpace.Graph;
using UnityEngine;

namespace FIMSpace.Generating.Planning
{
    public partial class PlannerFunctionNode
    {

        [System.Serializable]
        public class FunctionPortRef
        {
            public PlannerRuleBase Parent;
            public string DisplayName = "";
            public int RootPortID = -1;
            public int RootPortHelpID = -1;
            public EFunctionPortType ViewType = EFunctionPortType.Int;
            public EPortPinType PinType = EPortPinType.Input;

            public IntPort p_Int = null;
            public BoolPort p_Bool = null;
            public FloatPort p_Float = null;
            public PGGVector3Port p_Vector3 = null;
            public PGGCellPort p_Cell = null;
            public PGGPlannerPort p_Field = null;
            public PGGStringPort p_String = null;


            public FunctionPortRef(PlannerRuleBase parent, EFunctionPortType valueType, EPortPinType pinType)
            {
                Parent = parent;

                DisplayName = Parent.GetDisplayName();
                RootPortID = parent.IndividualID;

                ViewType = valueType;
                PinType = pinType;
                //UnityEngine.Debug.Log("Creat " + valueType);

                RefreshInstances();
            }


            public void PreGeneratePrepare()
            {
                if (ViewType == EFunctionPortType.Field)
                {
                    if (p_Field != null) p_Field.Clear();
                }
            }

            void RefreshInstances(bool force = true)
            {
                if (force)
                {
                    p_Int = new IntPort();
                    p_Bool = new BoolPort();
                    p_Float = new FloatPort();
                    p_Vector3 = new PGGVector3Port();
                    p_Cell = new PGGCellPort();
                    p_String = new PGGStringPort();
                    p_Field = new PGGPlannerPort();
                }
                else
                {
                    if (p_Int == null) p_Int = new IntPort();
                    if (p_Bool == null) p_Bool = new BoolPort();
                    if (p_Float == null) p_Float = new FloatPort();
                    if (p_Vector3 == null) p_Vector3 = new PGGVector3Port();
                    if (p_Cell == null) p_Cell = new PGGCellPort();
                    if (p_String == null) p_String = new PGGStringPort();
                    if (p_Field == null) p_Field = new PGGPlannerPort();
                }
            }


            public NodePortBase GetPort()
            {
                NodePortBase prt = null;


                switch (ViewType)
                {
                    case EFunctionPortType.Int: prt = p_Int; break;
                    case EFunctionPortType.Bool: prt = p_Bool; break;
                    case EFunctionPortType.Number: prt = p_Float; break;
                    case EFunctionPortType.Vector3: prt = p_Vector3; break;
                    case EFunctionPortType.String: prt = p_String; break;
                    case EFunctionPortType.Cell: prt = p_Cell; break;
                    case EFunctionPortType.Field: prt = p_Field; break;
                }

                if (prt != null)
                {
                    if (Parent) prt.DisplayName = Parent.GetDisplayName();
                    else prt.DisplayName = DisplayName;
                }
                else
                {
                    RefreshInstances(false);
                }

                return prt;
            }

            public void RefreshValue()
            {
                FN_Output fOut = Parent as FN_Output;

                if (fOut)
                {
                    fOut.OnStartReadingNode();

                    var port = fOut.GetFunctionOutputPort();
                    port.TriggerReadPort();
                    port.GetPortValueCall(false);

                    fOut.RefreshPortValue();

                    SetValueOf(port);
                }
                else if (Parent is FN_Input)
                {
                    FN_Input fInp = Parent as FN_Input;
                    fInp.OnStartReadingNode();

                    var port = GetPort();
                    ////UnityEngine.Debug.Log(port.DisplayName +" is connected ? " + port.Connections.Count);
                    port.TriggerReadPort(false);
                    port.GetPortValueCall(false);
                    fInp.SetValueOf(port);
                }
                else if (Parent is FN_Parameter)
                {
                    FN_Parameter fParam = Parent as FN_Parameter;
                    fParam.OnStartReadingNode();

                    var port = GetPort();
                    port.TriggerReadPort();
                    port.GetPortValueCall(false);

                    fParam.SetValue(port.GetPortValueSafe);
                }
            }

            void SetValueOf(NodePortBase p)
            {
                if (p == null) { UnityEngine.Debug.Log("Null port value!"); return; }

                object o = p.GetPortValueSafe;

                switch (ViewType)
                {
                    case EFunctionPortType.Int:
                        if (o != null)
                            p_Int.Value = (int)o;
                        break;

                    case EFunctionPortType.Bool:
                        if (o != null)
                            p_Bool.Value = (bool)o;
                        break;

                    case EFunctionPortType.Number:
                        if (o != null)
                            p_Float.Value = (float)o;
                        break;

                    case EFunctionPortType.Vector3:

                        if (o != null)
                        {
                            if (o is Vector3)
                            {
                                p_Vector3.Value = (Vector3)o;
                            }
                            else UnityEngine.Debug.Log("Inputting not Vector3! it's " + o.GetType() + " (" + o + ")");
                        }

                        break;

                    case EFunctionPortType.String:
                        if (o != null)
                            p_String.StringVal = (string)o;
                        break;

                    case EFunctionPortType.Cell:
                        if (p is PGGCellPort)
                        {
                            p_Cell.ProvideFullCellData(p as PGGCellPort);
                        }
                        break;

                    case EFunctionPortType.Field:
                        if (p is PGGPlannerPort)
                        {
                            p_Field.CopyValuesOfOtherPort(p);
                        }
                        break;
                }

            }

        }

    }

}