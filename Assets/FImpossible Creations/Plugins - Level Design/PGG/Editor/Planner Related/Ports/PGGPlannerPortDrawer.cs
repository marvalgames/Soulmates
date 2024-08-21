using FIMSpace.Generating.Planning;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Graph
{

    [CustomPropertyDrawer(typeof(PGGPlannerPort))]
    public class PGGFieldPort_Drawer : NodePort_DrawerBase
    {
        PGGPlannerPort plPrt = null; PGGPlannerPort PlannerPort { get { if (plPrt == null) plPrt = port as PGGPlannerPort; return plPrt; } }
        protected override string InputTooltipText
        {
            get
            {
                if (PlannerPort != null)
                {
                    if (plPrt.ParentNode != null)
                        if (plPrt.ParentNode._EditorDebugMode)
                            return "Field Planner :: " + PlannerPort.GetContainHelper.Contains + " :: " + PlannerPort.GetNumberedIDArrayString();
                }

                return "Field Planner " + base.InputTooltipText + "\n(Using " + DefaultValueInfo + " planner if not connected)";
            }
        }

        protected override string OutputTooltipText
        {
            get
            {
                if (PlannerPort != null)
                {
                    if (plPrt.ParentNode != null)
                        if (plPrt.ParentNode._EditorDebugMode)
                            return "Field Planner :: " + PlannerPort.GetContainHelper.Contains + " :: " + PlannerPort.GetNumberedIDArrayString();
                }

                return "Field Planner " + base.OutputTooltipText + "\n(Using " + DefaultValueInfo + " planner if not connected)";
            }
        }

        string DefaultValueInfo { get { if (plPrt == null) return "(Self)"; else return plPrt.Editor_DefaultValueInfo; } }



        protected override void DrawLabel(Rect fieldRect)
        {
            if (port.PortState() == EPortPinState.Connected)
            {
                if (PlannerPort != null)
                {
                    displayContent.text = GetDisplayLabelPrefixText(PlannerPort);

                    SetLabelWidth();
                    EditorGUI.LabelField(fieldRect, displayContent);
                    RestoreLabelWidth();
                }
            }
            else // Disconnected
            {
                displayContent.text = GetDisplayLabelTextFull(PlannerPort);
                SetLabelWidth();
                EditorGUI.LabelField(fieldRect, displayContent.text);
                RestoreLabelWidth();
            }
        }



        protected override void DrawValueField(Rect fieldRect)
        {
            PlannerPort.UniquePlannerID = EditorGUI.IntField(fieldRect, GUIContent.none, PlannerPort.UniquePlannerID);
            if (PlannerPort.UniquePlannerID < -1) PlannerPort.UniquePlannerID = -1;
        }

        protected override void DrawValueFieldNoEditable(Rect fieldRect)
        {
            string suffix = "";

            // Drawing suffix of connected port
            //if (port.IsInput)
            //{
            //    PGGPlannerPort connected = GetConnectedPort(PlannerPort);
            //    if (connected != null)
            //    {
            //        suffix = GetDisplayLabelSuffixText(connected);
            //    }
            //}

            //if (suffix == "") 
            suffix = GetDisplayLabelSuffixText(PlannerPort);


            EditorGUI.LabelField(fieldRect, suffix);
        }


        protected override void DrawValueWithLabelField(Rect labelRect, Rect fieldRect, Rect bothRect)
        {
            if (port.IsOutput) // Output is not containing value field
            {
                displayContent.text = GetDisplayLabelPrefixText(PlannerPort);
                SetLabelWidth();
                EditorGUI.LabelField(labelRect, displayContent);
                EditorGUI.LabelField(fieldRect, GetDisplayLabelSuffixText(PlannerPort));
                RestoreLabelWidth();
                return;
            }

            // Input Port
            if (port.PortState() == EPortPinState.Connected)
            {
                displayContent.text = "";

                //PGGPlannerPort otherPort = GetConnectedPort(port);
                //if (otherPort != null) displayContent.text = GetDisplayLabelPrefixText(PlannerPort) + " " + GetDisplayLabelSuffixText(otherPort);

                //if (displayContent.text == "") 
                displayContent.text = GetDisplayLabelTextFull(PlannerPort);

                SetLabelWidth();
                PlannerPort.UniquePlannerID = EditorGUI.IntField(bothRect, displayContent, PlannerPort.UniquePlannerID);
                RestoreLabelWidth();
            }
            else // Disconnected
            {
                displayContent.text = GetDisplayLabelTextFull(PlannerPort);
                SetLabelWidth();
                PlannerPort.UniquePlannerID = EditorGUI.IntField(bothRect, displayContent, PlannerPort.UniquePlannerID);
                RestoreLabelWidth();
            }
        }

        //PGGPlannerPort GetConnectedPort(NodePortBase port)
        //{
        //    if (port != null) if (port.BaseConnection != null)
        //        {
        //            PGGPlannerPort otherPort = port.BaseConnection.PortReference as PGGPlannerPort;
        //            return otherPort;
        //        }

        //    return null;
        //}


        string GetDisplayLabelTextFull(PGGPlannerPort port)
        {
            return GetDisplayLabelPrefixText(port) + " " + GetDisplayLabelSuffixText(port);
        }

        string GetDisplayLabelPrefixText(PGGPlannerPort port)
        {
            if (port == null) return "";
            if (!string.IsNullOrEmpty(port.OverwriteName)) return port.OverwriteName;
            if (port.Editor_DisplayVariableName) return port.DisplayName; else return "";
        }

        string GetDisplayLabelSuffixText(PGGPlannerPort port)
        {
            if (port == null) return "";

            #region Disconnected drawing handling

            // ID string selected in gui field
            if (port.IsInput && port.IsNotConnected)
            {
                if (port.UniquePlannerID < 0) // Default value
                {
                    if (port.Editor_DefaultValueInfo != "(Self)" && string.IsNullOrWhiteSpace(port.Editor_DefaultValueInfo) == false) return port.Editor_DefaultValueInfo;

                    if (port.Switch_MinusOneReturnsMainField)
                        return "(self)";
                    else
                        return "(none)";
                }
                else
                {
                    if (FieldPlanner.CurrentGraphExecutingBuild)
                    {
                        return "[" + Mathf.Min(FieldPlanner.CurrentGraphExecutingBuild.BasePlanners.Count - 1, port.UniquePlannerID) + "]";
                    }
                    else
                        return "[" + port.UniquePlannerID + "]";
                }

            }

            #endregion

            return port.GetNumberedIDArrayString();
        }


    }

}
