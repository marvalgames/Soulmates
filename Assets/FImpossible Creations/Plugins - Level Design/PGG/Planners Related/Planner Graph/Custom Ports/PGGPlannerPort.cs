using FIMSpace.Generating;
using FIMSpace.Generating.Checker;
using FIMSpace.Generating.Planning;
using FIMSpace.Generating.Planning.GeneratingLogics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Graph
{
    [System.Serializable]
    public class PGGPlannerPort : NodePortBase
    {
        /// <summary> 
        /// If it's input port with no connection, it will return field of this ID or self if value is lower than zero 
        /// This value is not just Graph Runtime but also saved in node!
        /// </summary>
        [HideInInspector] public int UniquePlannerID = -1;

        FieldPlanner currentExecutingPlanner { get { return FieldPlanner.CurrentGraphExecutingPlanner; } }

        [NonSerialized] PlannerContainHelper ContainHelper = new PlannerContainHelper();
        public PlannerContainHelper GetContainHelper { get { return ContainHelper; } }


        #region Editor Related Variables and methods

        public bool Editor_DisplayVariableName = true;

        /// <summary> Text display in port if it's input with no connection </summary>
        [NonSerialized] public string Editor_DefaultValueInfo = "(Self)";

        public override Color GetColor()
        {
            return new Color(0.9f, 0.7f, .3f, 1f);
        }

        #endregion


        /// <summary> 
        /// (only for output) Read-Get value out of the output port 
        /// Returning references to field planner, checker field or list instances
        /// Values to return here are defined by the "Output_Provide_..." methods
        /// Never outputs int/vectors, this types are for input
        /// </summary>
        //object Output_Read_Value(/*bool prioritizeCheckerField = false*/)
        //{
            // Output port returns null, nothing to do (not needed by anything)
            // We are providing values to the output port then it's transferring it further
            // No connection = nothing to give it's data
            // if (IsNotConnected) return null; <- can be uncommented but for debugging cases it can be useful to still return values on demand

            // Providing data contained in this port for some input port or for the debugging tools
            //if (prioritizeCheckerField) return Internal_Output_PlannerOrChecker(ContainHelper.GetPlanner(), prioritizeCheckerField);
        //}


        void Utility_AssignNumberedID(int plannerIndex, int duplicateIndex = -1, int subFieldIndex = -1, bool additive = false)
        {
            ContainHelper.AssignPlanner(new Vector3Int(plannerIndex, duplicateIndex, subFieldIndex), additive);
        }

        /// <summary>
        /// Provide some value to the input port to interpretate it and stage correct data within
        /// </summary>
        /// <param name="multipleConnections"> Grouping multiple connections values into this port. !!! Requires Clear() before multiple read. </param>
        void Input_InterpretateValue(object val)
        {
            #region Not connected handling

            if (IsOutput) return;

            if (IsNotConnected && IsInput)
            {
                Output_Provide_Planner(Internal_Output_GetNonConnected_Planner(Switch_MinusOneReturnsMainField));
                return;
            }

            #endregion

            ContainHelper.Clear();

            if (Connections.Count > 1)
            {
                // Handling reading multiple connected ports
                for (int i = 0; i < Connections.Count; i++)
                {
                    NodePortBase ndPort = Connections[i].PortReference.ToNodePortBase();
                    if (ndPort == null) continue;
                    var value = Connections[i].PortReference.ToNodePortBase().GetPortValueSafe;
                    DecodeAndAssignValue(value, true);
                }

                return;
            }

            DecodeAndAssignValue(val); // Read from one port (current val)
        }


        public void DecodeAndAssignValue(object val, bool inputMultipleConnections = false)
        {
            #region Numbered Read (Ints, floats, vectors)

            if (val is int)
            {
                Utility_AssignNumberedID((int)val, -1, -1, inputMultipleConnections);
            }
            else if (val is float)
            {
                Utility_AssignNumberedID(Mathf.RoundToInt((float)val));
            }
            else if (val is Vector2)
            {
                Vector2 v2 = (Vector2)val;
                Utility_AssignNumberedID(Mathf.RoundToInt(v2.x), Mathf.RoundToInt(v2.y), -1, inputMultipleConnections);
            }
            else if (val is Vector2Int)
            {
                Vector2Int v2 = (Vector2Int)val;
                Utility_AssignNumberedID(Mathf.RoundToInt(v2.x), Mathf.RoundToInt(v2.y), -1, inputMultipleConnections);
            }
            else if (val is Vector3)
            {
                Vector3 v3 = (Vector3)val;
                Utility_AssignNumberedID(Mathf.RoundToInt(v3.x), Mathf.RoundToInt(v3.y), Mathf.RoundToInt(v3.z), inputMultipleConnections);
            }
            else if (val is Vector3Int)
            {
                Vector3Int v3 = (Vector3Int)val;
                Utility_AssignNumberedID((v3.x), (v3.y), (v3.z), inputMultipleConnections);
            }

            #endregion

            #region References Read (planner, checker, cell port, other references)

            else if (val is FieldPlanner)
            {
                ContainHelper.AssignPlanner(val as FieldPlanner, inputMultipleConnections);
            }
            else if (val is CheckerField3D)
            {
                ContainHelper.AssignChecker((CheckerField3D)val, inputMultipleConnections);

                //if (ContainHelper.GetChecker().SubFieldPlannerReference != null)
                //    ContainHelper.AssignPlanner(ContainHelper.GetChecker().SubFieldPlannerReference, multipleConnections);
            }
            else if (val is PGGCellPort.Data)
            {
                PGGCellPort.Data dt = (PGGCellPort.Data)val;

                if (dt.ParentResult != null)
                    if (dt.ParentResult.ParentFieldPlanner)
                        ContainHelper.AssignPlanner(dt.ParentResult.ParentFieldPlanner, inputMultipleConnections);
            }

            #endregion

            //#region Port Forwarding Read?
            //#endregion

            #region  Lists Read

            else if (val is List<FieldPlanner>)
            {
                ContainHelper.AssignPlannerList(val as List<FieldPlanner>, inputMultipleConnections);
            }
            else if (val is List<ICheckerReference>)
            {
                ContainHelper.AssignCheckerList(val as List<ICheckerReference>, inputMultipleConnections);
            }
            else if (val is List<CheckerField3D>)
            {
                ContainHelper.AssignCheckerList(val as List<CheckerField3D>, inputMultipleConnections);
            }
            #endregion
        }



        #region Reading state of the output port

        public bool IsContaining_Null { get { return ContainHelper.Contains == PlannerContainHelper.EContains.Null; } }

        /// <summary> True if containing planner or planner list </summary>
        public bool IsContaining_Planner { get { return ContainHelper.Contains == PlannerContainHelper.EContains.Planner || ContainHelper.Contains == PlannerContainHelper.EContains.PlannerList; } }

        /// <summary> If its list with single element then returns false </summary>
        public bool IsContaining_MultiplePlanners { get { return ContainHelper.Contains == PlannerContainHelper.EContains.PlannerList && ContainHelper.GetPlannerListCount > 1; } }
        /// <summary> True if containing checker or checkers list (not checking for field planner checker if contains field planner) </summary>
        public bool IsContaining_Checker { get { return ContainHelper.Contains == PlannerContainHelper.EContains.Checker || ContainHelper.Contains == PlannerContainHelper.EContains.CheckerList; } }
        /// <summary> If its list with single element then returns false </summary>
        public bool IsContaining_MultipleCheckers { get { return ContainHelper.Contains == PlannerContainHelper.EContains.CheckerList && ContainHelper.GetCheckerListCount > 1; } }

        public int IsContaining_PlannersCount { get { if (ContainHelper.Contains == PlannerContainHelper.EContains.Planner) return 1; if (ContainHelper.Contains == PlannerContainHelper.EContains.PlannerList) return ContainHelper.GetPlannerListCount; return 0; } }
        public int IsContaining_CheckersCount { get { if (ContainHelper.Contains == PlannerContainHelper.EContains.Checker) return 1; if (ContainHelper.Contains == PlannerContainHelper.EContains.CheckerList) return ContainHelper.GetCheckerListCount; return 0; } }

        public FieldPlanner Get_Planner { get { return ContainHelper.GetPlanner(); } }

        /// <summary> Returns checker (not checking for field planner checker if contains field planner) </summary>
        public CheckerField3D Get_Checker { get { return ContainHelper.GetChecker(); } }


        /// <summary> Gets contained checker or contained Field Planner's checker </summary>
        public ICheckerReference Get_CheckerReference 
        {
            get 
            {
                if (ContainHelper.ContainsChecker) return ContainHelper.GetChecker();
                if (ContainHelper.ContainsPlanner) return ContainHelper.GetPlanner();
                return null; 
            } 
        }


        static List<FieldPlanner> _emptyFieldList = new List<FieldPlanner>();

        /// <summary> Returns list of planners out of other port and joining Field read out of all other connected ports. Returns dedicated single element list if just one field is contained. </summary>
        public List<FieldPlanner> Get_GetMultipleFields
        {
            get
            {
                if (_emptyFieldList.Count > 0) _emptyFieldList.Clear();

                if (ContainHelper.GetPlannerList() == null) return _emptyFieldList;

                var list = ContainHelper.GetPlannerList();
                if (list.Count == 1 && list[0] == null) return _emptyFieldList;

                return ContainHelper.GetPlannerList();
            }
        }

        static List<ICheckerReference> _emptyCheckerList = new List<ICheckerReference>();

        /// <summary> 
        /// Never return null. Returns empty list if nothing contained.
        /// Returns list of checkers out of other port and joining Checkers read out of all other connected ports. 
        /// Returns dedicated single element list if just one checker is contained. 
        /// If there are no direct checkers but field planners, it will return list of field planners but as ICheckerReference list. (if Switch_ switches allows for it)
        /// </summary>
        public List<ICheckerReference> Get_GetMultipleCheckers
        {
            get
            {
                if (_emptyCheckerList.Count > 0) _emptyCheckerList.Clear();
                if (ContainHelper.Contains == PlannerContainHelper.EContains.Null) return _emptyCheckerList;

                if (ContainHelper.ContainsPlanner)
                {
                    if (Switch_ReturnOnlyCheckers || Switch_DontUsePlannerForCheckerReference)
                        return _emptyCheckerList;
                    else
                        return FieldPlanner.PlannerListToCheckerRefList(Get_GetMultipleFields);
                }

                if (ContainHelper.GetCheckerList() == null)
                {
                    return _emptyCheckerList;
                }

                var list = ContainHelper.GetCheckerList();
                if (list.Count == 1 && list[0] == null) return _emptyCheckerList;

                return ContainHelper.GetCheckerList();
            }
        }


        //static List<ICheckerReference> _emptyCheckerRefList = new List<ICheckerReference>();
        ///// <summary> Getting all fields or checkers contained within </summary>
        //public List<ICheckerReference> Get_GetMultipleCheckerReferences
        //{
        //    get
        //    {
        //        if (_emptyCheckerList.Count > 0) _emptyCheckerList.Clear();
        //        if (ContainHelper.Contains == PlannerContainHelper.EContains.Null) return _emptyCheckerRefList;

        //        if (ContainHelper.GetCheckerList() == null)
        //        {
        //            if (Switch_ReturnOnlyCheckers || Switch_DontUsePlannerForCheckerReference)
        //                return _emptyCheckerRefList;
        //            else
        //            {
        //                return FieldPlanner.PlannerListToCheckerRefList(Get_GetMultipleFields);
        //            }
        //        }

        //        var list = ContainHelper.GetCheckerList();
        //        if (list.Count == 1 && list[0] == null) return _emptyCheckerRefList;

        //        return ContainHelper.GetCheckerList();
        //    }
        //}


        //public List<CheckerField3D> Get_GetMultipleCheckersAndFieldCheckers
        //{
        //    get
        //    {
        //        return ContainHelper.GetCheckerList();
        //    }
        //}

        #endregion



        /// <summary> (For Inputs) If disconnected input field should return field reference by planner Index selected in GUI </summary>
        [HideInInspector] public bool Switch_DisconnectedReturnsByID = true;
        /// <summary>  When reading -1 value for planner index will result in currently executed planner return </summary>
        [HideInInspector] public bool Switch_MinusOneReturnsMainField = true;
        /// <summary>  (For Outputs) Disallow returning CheckerFields </summary>
        [HideInInspector] public bool Switch_ReturnOnlyFieldPlanners = false;
        /// <summary>  (For Outputs) Disallow returning Field Planners (it will return checker field of contained field planner if possible) </summary>
        [HideInInspector] public bool Switch_ReturnOnlyCheckers = false;
        /// <summary>  (For Outputs) Disallow forcing reading checker reference out of contained Field Planner when Checker field read demand </summary>
        [HideInInspector] public bool Switch_DontUsePlannerForCheckerReference = false;


        #region Provide Values for Output Port


        #region Numbered Handling

        public void Output_Provide_Planner(int index)
        {
            Utility_AssignNumberedID(index);
        }

        public void Output_Provide_Planner(int index, int duplicateIndex)
        {
            Utility_AssignNumberedID(index, duplicateIndex);
        }

        public void Output_Provide_Planner(int index, int duplicateIndex, int subFieldID)
        {
            Utility_AssignNumberedID(index, duplicateIndex, subFieldID);
        }

        #endregion


        public void Output_Provide_Planner(FieldPlanner planner)
        {
            ContainHelper.AssignPlanner(planner);
        }

        public void Output_Provide_PlannersList(List<FieldPlanner> planners)
        {
            ContainHelper.AssignPlannerList(planners);
        }

        public void Output_Provide_Checker(CheckerField3D checker)
        {
            ContainHelper.AssignChecker(checker);
        }

        public void Output_Provide_CheckerReference(ICheckerReference checkerRef)
        {
            if (checkerRef is FieldPlanner)
                Output_Provide_Planner(checkerRef as FieldPlanner);
            else
                Output_Provide_Checker(checkerRef.CheckerReference);
        }

        public void Output_Provide_CheckerList(List<CheckerField3D> checkerList)
        {
            ContainHelper.AssignCheckerList(checkerList);
        }

        public void Output_Provide_CheckerReferenceList(List<ICheckerReference> checkerList)
        {
            if (checkerList == null) return;

            bool arePlanners = false;

            if (checkerList.Count > 0)
            {
                arePlanners = true;

                for (int i = 0; i < checkerList.Count; i++)
                {
                    if ((checkerList[i] as FieldPlanner) == null) { arePlanners = false; break; }
                }
            }

            if (arePlanners)
            {
                ContainHelper.AssignPlannerList(FieldPlanner.CheckerRefListToPlannerList(checkerList));
            }
            else
                ContainHelper.AssignCheckerList(checkerList);
        }

        #endregion


        #region Utility Read

        /// <summary> Returning desired FieldPlanner reference out of currently executed BuildPlan. ! MINUS ONE RETURNS NULL ! </summary>
        public static FieldPlanner Utility_GetFieldPlannerByID(int plannerId, int duplicateId = -1, int subFieldID = -1)
        {
            FieldPlanner planner = FieldPlanner.CurrentGraphExecutingPlanner;
            if (planner == null) { planner = null; }

            if (plannerId < 0) return null; // Undefined ID to read

            BuildPlannerPreset build = null;
            if (planner != null) build = planner.ParentBuildPlanner;

            if (build == null) return null; // No Build Plan to read

            if (plannerId >= 0 && plannerId < build.BasePlanners.Count) // Id is in build plan's array range
            {
                planner = build.BasePlanners[plannerId];

                bool dup = false;

                var duplList = planner.GetDuplicatesPlannersList();

                if (planner.IsDuplicate == false)
                    if (duplicateId >= 0) if (duplList != null) if (duplicateId < duplList.Count)
                            {
                                planner = duplList[duplicateId];
                                dup = true;

                                if (planner.GetSubFieldsCount > 0)
                                    if (subFieldID != -1)
                                    {
                                        if (subFieldID >= planner.GetSubFieldsCount) return null;
                                        planner = planner.GetSubField(subFieldID);
                                    }
                            }

                if (!dup)
                {
                    if (subFieldID > -1)
                        if (planner.GetSubFieldsCount > 0)
                            planner = planner.GetSubField(subFieldID);
                }
            }

            if (planner.Discarded) // We still can read field instances and sub-fields!
            {
                FieldPlanner gatherChildPlanner = planner;

                if (duplicateId == -1) // If discarded then get first not discarded duplicate planner
                {
                    var duplList = planner.GetDuplicatesPlannersList();

                    if (duplList != null)
                    {
                        if (planner.IsDuplicate == false)
                            for (int i = 0; i < duplList.Count; i++)
                            {
                                var plan = duplList[i];

                                if (plan == null) continue;
                                if (plan.Available == false) continue;

                                gatherChildPlanner = plan;

                                if (subFieldID != -1)
                                    if (gatherChildPlanner.GetSubFieldsCount > 0)
                                    {
                                        if (subFieldID >= gatherChildPlanner.GetSubFieldsCount) return null;
                                        gatherChildPlanner = gatherChildPlanner.GetSubField(subFieldID);
                                    }

                                break;
                            }
                    }
                }

                return gatherChildPlanner;
            }

            return planner;
        }


        #endregion


        #region Small Utilities


        /// <summary> Get FieldPlanner using just Port's UniquePlannerID int variable. Quick internal method for more complex methods </summary>
        FieldPlanner Internal_Output_GetNonConnected_Planner(bool minusOneReturnsMain)
        {
            if (UniquePlannerID < 0)
            {
                if (minusOneReturnsMain) return currentExecutingPlanner;
                return null;
            }

            // On UniquePlannerID >= 0
            return Utility_GetFieldPlannerByID(UniquePlannerID);
        }


        object Internal_Output_PlannerOrChecker(FieldPlanner planner, bool prioritizeChecker = false)
        {
            if (planner == null) return null;
            if (prioritizeChecker) return planner.LatestChecker; return planner;
        }


        /// <summary>
        /// To use correctly, put it in if (IsNotConnected) { Internal_Input_GetDisconnectedPortValue...
        /// </summary>
        object Internal_Input_GetDisconnectedPortValue(bool prioritizeCheckerField)
        {
            if (Switch_DisconnectedReturnsByID == false) return null;

            // No connection - get planner using port helper ID value visible in GUI

            if (Switch_ReturnOnlyCheckers)
            {
                // Not allowed returning checkers without direct references
                if (Switch_DontUsePlannerForCheckerReference) return null;

                FieldPlanner nonConnecterPlannerRef = Internal_Output_GetNonConnected_Planner(Switch_MinusOneReturnsMainField);
                return Internal_Output_PlannerOrChecker(nonConnecterPlannerRef, prioritizeCheckerField);
            }

            FieldPlanner planner = Internal_Output_GetNonConnected_Planner(Switch_MinusOneReturnsMainField);
            return Internal_Output_PlannerOrChecker(planner, prioritizeCheckerField);
        }


        #endregion


        #region Port Methods Implementation

        public override void RefreshPortConnections<T>(List<T> allNodes)
        {
            base.RefreshPortConnections(allNodes);
            Clear(); // Clear on prepare
        }

        public override System.Type GetPortValueType
        {
            get
            {
                if (IsContaining_Null) return typeof(object);
                if (ContainHelper.ContainsPlanner) return typeof(FieldPlanner);
                if (ContainHelper.ContainsChecker) return typeof(CheckerField3D);
                return typeof(object);
            }
        }


        public override bool AllowConnectionWithValueType(IFGraphPort other)
        {
            if ((other is PGGPlannerPort)) return true;
            if ((other is PGGUniversalPort)) return true;
            if (FGenerators.CheckIfIsNull(other)) return false;
            if (FGenerators.CheckIfIsNull(other.GetPortValue)) return false; // If null then allow connect only with PGGPlannerPort
            if (other.GetPortValue.GetType() == typeof(int)) return true;
            if (other.GetPortValue.GetType() == typeof(float)) return true;
            if (other.GetPortValue.GetType() == typeof(CheckerField3D)) return true;
            if (other.GetPortValue.GetType() == typeof(FieldPlanner)) return true;
            if (other.GetPortValue.GetType() == typeof(Vector2)) return true;
            if (other.GetPortValue.GetType() == typeof(Vector2Int)) return true;
            if (other.GetPortValue.GetType() == typeof(Vector3)) return true;
            if (other.GetPortValue.GetType() == typeof(Vector3Int)) return true;
            return base.AllowConnectionWithValueType(other);
        }


        public override bool CanConnectWith(IFGraphPort toPort)
        {
            if (toPort is PGGCellPort) return true;
            return base.CanConnectWith(toPort);
        }


        public override void TriggerReadPort(bool callRead = false)
        {
            //ContainHelper.Clear();
            base.TriggerReadPort(callRead);
        }


        // Most important call for assigning values
        public override object GetPortValueCall(bool onReadPortCall = true)
        {
            var val = base.GetPortValueCall(onReadPortCall);
            lastestReadValue = val;

            Input_InterpretateValue(val);

            return val;
        }


        public void Clear()
        {
            ContainHelper.Clear();
        }


        /// <summary>
        /// Planner Index (-1 if use self) and Duplicate Index (in most cases just zero)
        /// </summary>
        public override object DefaultValue { get { return ContainHelper.GetValue(); } }


        #endregion


        #region Obsoletes


        [System.Obsolete("Use Switch_MinusOneReturnsMainField instead")]
        public bool MinusOneReturnsSelf
        {
            get { return Switch_MinusOneReturnsMainField; }
            set { Switch_MinusOneReturnsMainField = value; }
        }

        [System.Obsolete("Use Switch_DisconnectedReturnsByID instead")]
        [HideInInspector]
        public bool DefaultValueIsNumberedID
        {
            get { return Switch_DisconnectedReturnsByID; }
            set { Switch_DisconnectedReturnsByID = value; }
        }

        [System.Obsolete("Use Switch_ReturnOnlyCheckers instead")]
        [HideInInspector]
        public bool OnlyReferenceContainer
        {
            get { return Switch_ReturnOnlyCheckers; }
            set { Switch_ReturnOnlyCheckers = value; }
        }


        [System.Obsolete("Use Input_InterpretateValue instead")]
        public void SetIDsFromNumberVar(object numberVar)
        {
            if (numberVar == null) return;
            DecodeAndAssignValue(numberVar);
        }

        [System.Obsolete("Use Output_Provide_Planner instead")]
        public void SetIDsOfPlanner(FieldPlanner planner)
        {
            Output_Provide_Planner(planner);
        }

        [System.Obsolete("Use Output_Provide_Checker instead")]
        public void ProvideShape(CheckerField3D rectChecker)
        {
            Output_Provide_Checker(rectChecker);
        }

        [System.Obsolete("Use Output_Provide_PlannersList instead")]
        public void AssignPlannersList(List<FieldPlanner> choosen)
        {
            Output_Provide_PlannersList(choosen);
        }

        [System.Obsolete("Use Get_GetMultipleCheckers instead")]
        public List<ICheckerReference> GetAllInputCheckers(bool v = false)
        {
            return Get_GetMultipleCheckers;
        }

        [System.Obsolete("Use Output_Provide_Checker instead")]
        public void AssignCheckerField3D(CheckerField3D checkerField3D)
        {
            Output_Provide_Checker(checkerField3D);
        }

        [System.Obsolete("Use DecodeAndAssignValue instead")]
        public void ProvideValueToPort(object val)
        {
            DecodeAndAssignValue(val);
        }

        [System.Obsolete("Use IsContaining_MultiplePlanners instead")]
        public bool ContainsMultiple { get { return IsContaining_MultiplePlanners; } }


        [System.Obsolete("Use Output_Provide_CheckerList instead")]
        public void AssignCheckersList(List<CheckerField3D> chLevels)
        {
            Output_Provide_CheckerList(chLevels);
        }

        #endregion


        #region Helper Methods


        public FieldPlanner GetPlannerFromPort(bool callRead = true)
        {
            if (callRead) TriggerReadPort(true);
            return Get_Planner;
        }

        public int GetPlannerIndex()
        {
            if (IsContaining_Planner == false) return -1;
            return Get_Planner.IndexOnPrint;
        }

        public int GetPlannerDuplicateIndex()
        {
            if (IsContaining_Planner == false) return -1;
            if (Get_Planner.IsDuplicate == false) return -1;
            return Get_Planner.IndexOfDuplicate;
        }

        public int GetPlannerSubFieldIndex()
        {
            if (IsContaining_Planner == false) return -1;
            if (Get_Planner.IsSubField == false) return -1;
            return Get_Planner.GetSubFieldID();
        }

        public Vector3Int GetNumberedIDArrayVector()
        {
            if (IsContaining_Planner == false) return new Vector3Int(-1, -1, -1);
            FieldPlanner planner = Get_Planner;
            return planner.ArrayNameIDVector.V3toV3Int();
        }

        public string GetNumberedIDArrayString()
        {
            if (IsContaining_Planner == false)
            {
                if (IsContaining_Null) return "None";
                if (IsContaining_MultipleCheckers) return "Shapes (" + IsContaining_CheckersCount + ")";
                if (IsContaining_Checker) return "Shape {"+ContainHelper.GetChecker()?.ChildPositionsCount+"}";
                return "Null"; // Should it happen ever?
            }
            else
            {
                if (IsContaining_MultiplePlanners) return "Multiple (" + IsContaining_PlannersCount + ")";
                if (Get_Planner != null) return Get_Planner.ArrayNameString;
                return "(wrong)";
            }
        }


        #endregion



        #region Target Operations

        object lastestReadValue = null;
        public object GetInputValueSafe { get { return lastestReadValue; } }

        public CheckerField3D GetInputCheckerSafe
        {
            get
            {
                if (ContainHelper.GetChecker() != null) return ContainHelper.GetChecker();
                if (Switch_DontUsePlannerForCheckerReference == false) if (IsContaining_Planner) return ContainHelper.GetPlanner().LatestChecker;
                return null;
            }
        }


        public PGGCellPort IsConnectedJustWithCellPort()
        {
            if (IsNotConnected) return null;

            PGGCellPort firstCPort = null;

            for (int i = 0; i < Connections.Count; i++)
            {
                if (Connections[i] == null) continue;

                if (Connections[i].PortReference is PGGCellPort)
                {
                    if ( firstCPort == null) firstCPort = Connections[i].PortReference as PGGCellPort;
                }
                else return null;
            }

            return firstCPort;
        }


        public void CopyValuesOfOtherPort(NodePortBase p)
        {
            PGGPlannerPort otherPlannerPort = p as PGGPlannerPort;
            if (otherPlannerPort == null) return;

            UniquePlannerID = otherPlannerPort.UniquePlannerID;

            Switch_DisconnectedReturnsByID = otherPlannerPort.Switch_DisconnectedReturnsByID;
            Switch_DontUsePlannerForCheckerReference = otherPlannerPort.Switch_DontUsePlannerForCheckerReference;
            Switch_MinusOneReturnsMainField = otherPlannerPort.Switch_MinusOneReturnsMainField;
            Switch_ReturnOnlyCheckers = otherPlannerPort.Switch_ReturnOnlyCheckers;
            Switch_ReturnOnlyFieldPlanners = otherPlannerPort.Switch_ReturnOnlyFieldPlanners;

            if (otherPlannerPort.IsContaining_Checker)
            {
                if (otherPlannerPort.IsContaining_MultipleCheckers) ContainHelper.AssignCheckerList(otherPlannerPort.Get_GetMultipleCheckers);
                else ContainHelper.AssignChecker(otherPlannerPort.Get_Checker);
                return;
            }

            ContainHelper.Assign(otherPlannerPort.ContainHelper);
        }


        #endregion


        #region Static Utilities


        public static List<FieldPlanner> GetPlannersFromPort(PGGPlannerPort port, bool newListInstance = false, bool callRead = true)
        {
            if (callRead) port.TriggerReadPort(true);

            if (newListInstance)
            {
                var fields = new List<FieldPlanner>();
                PGGUtils.TransferFromListToList(port.Get_GetMultipleFields, fields);
                return fields;
            }

            return port.Get_GetMultipleFields;
        }

        public static FieldPlanner GetPlannerFromPort(PGGPlannerPort port, bool callRead = true)
        {
            return port.GetPlannerFromPort(callRead);
        }

        public static FieldPlanner GetPlannerByID(int plannerId, int duplicateId = -1, int subId = -1)
        {
            FieldPlanner planner = GetFieldPlannerByID(plannerId, duplicateId, subId);
            return planner;
        }


        /// <summary> Field from CELL Port </summary>
        public static FieldPlanner GetPlannerFromCellPort(IFGraphPort cellPrt)
        {
            PGGCellPort cPort = cellPrt as PGGCellPort;
            if (cPort != null) return cPort.GetInputPlannerIfPossible;
            return null;
        }


        public static bool _debug = false;

        public static FieldPlanner GetFieldPlannerByID(int plannerId, int duplicateId = -1, int subFieldID = -1, bool selfOnUndefined = true)
        {

            FieldPlanner planner = FieldPlanner.CurrentGraphExecutingPlanner;
            if (planner == null) { planner = null; }

            if (plannerId < 0 && selfOnUndefined == false)
            {
                return null;
            }

            BuildPlannerPreset build = null;
            if (planner != null) build = planner.ParentBuildPlanner;

            if (build == null)
            {
                return null;
            }

            if (plannerId >= 0 && plannerId < build.BasePlanners.Count)
            {
                planner = build.BasePlanners[plannerId];

                bool dup = false;

                var duplList = planner.GetDuplicatesPlannersList();

                if (planner.IsDuplicate == false) if (duplicateId >= 0) if (duplList != null) if (duplicateId < duplList.Count)
                            {
                                planner = duplList[duplicateId];
                                dup = true;

                                if (planner.GetSubFieldsCount > 0)
                                    if (subFieldID != -1)
                                    {
                                        if (subFieldID >= planner.GetSubFieldsCount) return null;
                                        planner = planner.GetSubField(subFieldID);
                                    }
                            }

                if (!dup)
                {
                    if (subFieldID > -1)
                        if (planner.GetSubFieldsCount > 0)
                            planner = planner.GetSubField(subFieldID);
                }
            }

            if (planner.Discarded)
            {
                FieldPlanner getPl = planner;

                if (duplicateId == -1) // if discarded then get first not discarded duplicate planner
                {

                    var duplList = planner.GetDuplicatesPlannersList();

                    if (duplList != null)
                        if (planner.IsDuplicate == false)
                            for (int i = 0; i < duplList.Count; i++)
                            {
                                var plan = duplList[i];
                                if (plan == null) continue;
                                if (plan.Available == false) continue;
                                getPl = plan;

                                if (subFieldID != -1)
                                    if (getPl.GetSubFieldsCount > 0)
                                    {
                                        if (subFieldID >= getPl.GetSubFieldsCount) return null;
                                        getPl = getPl.GetSubField(subFieldID);
                                    }

                                break;
                            }
                }

                return getPl;
            }

            return planner;
        }


        #endregion



        /// <summary> Returns contained checker or checker out of the contained planner </summary>
        public static CheckerField3D GetCheckerFromPort(PGGPlannerPort port, bool callRead = true)
        {
            if (callRead) port.TriggerReadPort(true);

            CheckerField3D portPlanner = port.GetInputCheckerSafe;
            if (portPlanner != null)
            {
                return portPlanner;
            }

            if (port.Switch_DontUsePlannerForCheckerReference) return null;

            FieldPlanner planner = PGGPlannerPort.GetPlannerFromPort(port, callRead);
            if (planner == null) return null;
            if (planner.Available == false) return null;

            return planner.LatestChecker;
        }


        /// <summary> Returns contained checkers or checkers out of the contained planners </summary>
        public static List<ICheckerReference> GetCheckersFromPort(PGGPlannerPort port, bool callRead = true)
        {
            if (callRead) port.TriggerReadPort(true);

            List<ICheckerReference> checkers = new List<ICheckerReference>();

            var myChec = port.Get_GetMultipleCheckers;
            if (myChec.Count > 0)
            {
                PGGUtils.TransferFromListToListI(myChec, checkers);
                return checkers;
            }

            if (port.Switch_DontUsePlannerForCheckerReference) return null;

            var myFields = port.Get_GetMultipleFields;
            if (myFields.Count > 0)
            {
                for (int i = 0; i < myFields.Count; i++) checkers.Add(myFields[i].LatestChecker);
                return checkers;
            }

            return null;
        }



        public override bool OnClicked(Event e)
        {
            bool baseClick = base.OnClicked(e);
            if (baseClick) return true;

            List<ICheckerReference> chk = Get_GetMultipleCheckers;

            for (int c = 0; c < chk.Count; c++)
            {
                chk[c].CheckerReference.DebugLogDrawCellsInWorldSpace(Color.HSVToRGB(((float)c * 0.05f + 0.3f) % 1f, 0.7f, 0.6f), 0.1f);
            }

#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif

            return false;

        }

    }
}