//using FIMSpace.Generating;
//using FIMSpace.Generating.Checker;
//using FIMSpace.Generating.Planning;
//using FIMSpace.Generating.Planning.GeneratingLogics;
//using FIMSpace.Generating.Planning.PlannerNodes;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace FIMSpace.Graph
//{
//    [System.Serializable]
//    public class PGGFieldPort : NodePortBase
//    {
//        /// <summary> Graph runtime References </summary>
//        private FieldPlannerReference Containing;



//        #region "Containing" Reference Utilities / Shortcuts

//        /// <summary> 
//        /// If it's input port with no connection, it will return field of this ID or self if value is lower than zero 
//        /// This value is not just Graph Runtime but also saved in node!
//        /// </summary>
//        [HideInInspector] public int UniquePlannerID = -1;
//        [NonSerialized] public int DuplicatePlannerID = -1;
//        [NonSerialized] public int SubFieldID = -1;

//        /// Updating 'Containing' reference with current  'UniquePlannerID', 'DuplicatePlannerID', 'SubFieldID' values 
//        void RefreshNumberedID()
//        {
//            SetNumberedID(UniquePlannerID, DuplicatePlannerID, SubFieldID);
//        }

//        /// <summary> Setting 'UniquePlannerID', 'DuplicatePlannerID', 'SubFieldID' and updating 'Containing' reference </summary>
//        void SetNumberedID(int plannerId, int duplicateId = -1, int subFieldId = -1)
//        {
//            UniquePlannerID = plannerId;
//            DuplicatePlannerID = duplicateId;
//            SubFieldID = subFieldId;

//            FieldPlannerReference cont = Containing;
//            cont.SetNumberedID(plannerId, duplicateId, subFieldId);
//            Containing = cont;
//        }

//        #endregion



//        #region Editor Related Variables and methods

//        public bool Editor_DisplayVariableName = true;

//        /// <summary> Text display in port if it's input with no connection </summary>
//        [NonSerialized] public string Editor_DefaultValueInfo = "(Self)";

//        public override Color GetColor()
//        {
//            return new Color(0.9f, 0.7f, .3f, 1f);
//        }

//        #endregion



//        #region Handling Multiple Fields


//        private List<FieldPlannerReference> MultipleContaining = null;
//        public bool ContainsMultiplePlanners { get { if (MultipleContaining == null) return false; return MultipleContaining.Count > 1; } }
//        public int GetContainedCount()
//        {
//            int count = 0;
//            if (!Containing.IsNull) count += 1;

//            if (MultipleContaining != null)
//                for (int i = 0; i < MultipleContaining.Count; i++)
//                {
//                    if (MultipleContaining[i] != Containing) count += 1;
//                }

//            return count;
//        }

//        #endregion



//        #region Handling Contained Info Helper Methods

//        public override System.Type GetPortValueType 
//        { 
//            get 
//            { 
//                if ( UsingNumberedID) return typeof(int);
//                if (Containing.IsFreeChecker) return typeof(object);
//                return typeof(object);
//            } 
//        }

//        public bool ContainsForcedNull
//        {
//            get { if (ContainsMultiplePlanners) return false; return Containing.IsNull; }
//            set { var cnt = Containing; cnt.ForcedNull = value; Containing = cnt; }
//        }

//        public bool UsingNumberedID { get { return Containing.UsingNumberedID; } }

//        public bool ContainsSubField
//        {
//            get
//            {
//                if (ContainsMultiplePlanners)
//                {
//                    if (Containing.IsSubField) return true;
//                    for (int i = 0; i < MultipleContaining.Count; i++) if (MultipleContaining[i].IsSubField) return true;
//                }

//                return Containing.IsSubField;
//            }
//        }

//        public int ContainedSubFieldID { get { RefreshNumberedID(); return Containing.GetSubFieldID(); } }

//        public string GetNumberedIDArrayString()
//        {
//            string subStr = "";
//            if (SubFieldID != -1) subStr = "SUB(" + SubFieldID + "):";

//            string mainStr = "";
//            if (UniquePlannerID <= -1) mainStr = Editor_DefaultValueInfo; else mainStr = "[" + UniquePlannerID + "]";

//            if (DuplicatePlannerID <= -1) return subStr + mainStr;

//            return subStr + mainStr + "[" + DuplicatePlannerID + "]";
//        }

//        public bool ContainsJustChecker
//        {
//            get
//            {
//                if (ContainsMultiplePlanners)
//                {
//                    if (Containing.IsFreeChecker) return true;
//                    for (int i = 0; i < MultipleContaining.Count; i++) if (MultipleContaining[i].IsFreeChecker) return true;
//                }

//                return Containing.IsFreeChecker;
//            }
//        }


//        #endregion



//        #region Basic Connection Handling


//        public override bool AllowConnectionWithValueType(IFGraphPort other)
//        {
//            if (FGenerators.CheckIfIsNull(other)) return false;
//            if (FGenerators.CheckIfIsNull(other.GetPortValue)) return false;
//            if (other.GetPortValue.GetType() == typeof(int)) return true;
//            if (other.GetPortValue.GetType() == typeof(float)) return true;
//            if (other.GetPortValue.GetType() == typeof(Vector2)) return true;
//            if (other.GetPortValue.GetType() == typeof(Vector2Int)) return true;
//            if (other.GetPortValue.GetType() == typeof(Vector3)) return true;
//            if (other.GetPortValue.GetType() == typeof(Vector3Int)) return true;
//            return base.AllowConnectionWithValueType(other);
//        }

//        public override bool CanConnectWith(IFGraphPort toPort)
//        {
//            if (toPort is PGGCellPort) return true;
//            bool can = base.CanConnectWith(toPort);
//            return can;
//        }

//        public override object GetPortValueCall(bool onReadPortCall = true)
//        {
//            var val = base.GetPortValueCall(onReadPortCall);

//            if (val == null)
//            {
//                return val;
//            }

//            ReadValue(val);

//            return val;
//        }

//        void ReadValue(object val)
//        {
//            if (val.GetType() == typeof(int))
//            {
//                SetNumberedID((int)val);
//            }
//            else
//            if (val.GetType() == typeof(float)) SetNumberedID(Mathf.RoundToInt((float)val));
//            else
//            if (val.GetType() == typeof(Vector2))
//            {
//                Vector2 v2 = (Vector2)val;
//                SetNumberedID(Mathf.RoundToInt(v2.x), Mathf.RoundToInt(v2.y));
//            }
//            else if (val.GetType() == typeof(Vector2Int))
//            {
//                Vector2Int v2 = (Vector2Int)val;
//                SetNumberedID(Mathf.RoundToInt(v2.x), Mathf.RoundToInt(v2.y));
//            }
//            else if (val.GetType() == typeof(Vector3))
//            {
//                Vector3 v3 = (Vector3)val;
//                SetNumberedID(Mathf.RoundToInt(v3.x), Mathf.RoundToInt(v3.y), Mathf.RoundToInt(v3.z));
//            }
//            else if (val.GetType() == typeof(Vector3Int))
//            {
//                Vector3Int v3 = (Vector3Int)val;
//                SetNumberedID(Mathf.RoundToInt(v3.x), Mathf.RoundToInt(v3.y), Mathf.RoundToInt(v3.z));
//            }
//            else if (val.GetType() == typeof(PGGCellPort.Data))
//            {
//                PGGCellPort.Data dt = (PGGCellPort.Data)val;

//                if (dt.ParentResult != null)
//                    if (dt.ParentResult.ParentFieldPlanner)
//                        SetIDsOfPlanner(dt.ParentResult.ParentFieldPlanner);
//            }
//        }


//        public void SetIDsOfPlanner(FieldPlanner planner)
//        {
//            FieldPlannerReference cont = Containing;

//            if (planner == null)
//            {
//                cont.SetNumberedID(-1);
//                cont.OwnerPlanner = null;
//                cont.FreeChecker = null;
//                cont.ForcedNull = true;
//                Containing = cont;
//                return;
//            }

//            cont.ForcedNull = false;
//            UniquePlannerID = planner.IndexOnPreset;
//            DuplicatePlannerID = planner.IndexOfDuplicate;
//            SubFieldID = planner.IsSubField ? (planner.GetSubFieldID()) : -1;

//            Containing = cont;
//        }


//        #endregion



//        #region Value Helper Methods


//        public void Clear()
//        {
//            SetIDsOfPlanner(null);
//            ContainsForcedNull = false;
//        }

//        /// <summary>
//        /// Planner Index (-1 if use self) and Duplicate Index (in most cases just zero)
//        /// </summary>
//        public override object DefaultValue
//        {
//            get
//            {
//                if (Containing.IsFreeChecker) return Containing.FreeChecker;
//                if (Containing.IsRootChecker) return Containing.OwnerPlanner;
//                if (Containing.IsSubField) return Containing.OwnerPlanner;

//                return new Vector3Int(UniquePlannerID, DuplicatePlannerID, SubFieldID);
//            }
//        }

//        public object AcquireObjectReferenceFromInput()
//        {
//            for (int c = 0; c < Connections.Count; c++)
//            {
//                var conn = Connections[c];
//                var value = conn.PortReference.GetPortValue;
//                if (value != null) return value;
//            }

//            return null;
//        }


//        /// <summary>
//        ///  -1 is use self else use index in field planner
//        /// </summary>
//        public int GetPlannerIndex()
//        {
//            if (UniquePlannerID < -1) return -1;
//            if (PortState() != EPortPinState.Connected) return -1;
//            return UniquePlannerID;
//        }

//        /// <summary>
//        /// Index of duplicated planner field
//        /// </summary>
//        public int GetPlannerDuplicateIndex()
//        {
//            if (DuplicatePlannerID < 0) return -1;
//            return DuplicatePlannerID;
//        }

//        public int GetPlannerSubFieldIndex()
//        {
//            if (SubFieldID < 0) return -1;
//            return SubFieldID;
//        }



//        #endregion



//        #region Target Operations



//        // Complex Read Referece Methods   START   ------------------------------------------

//        public CheckerField3D GetInputCheckerSafe
//        {
//            get
//            {
//                FieldPlanner planner;

//                if (PortState() == EPortPinState.Empty) // Not Connected
//                {
//                    planner = GetPlannerFromPort(false);
//                    if (planner) return planner.LatestChecker;
//                }
//                else // Connected
//                {
//                    // Getting connected checker to the input port

//                    if (BaseConnection != null && BaseConnection.PortReference != null)
//                    {
//                        if (BaseConnection.PortReference is PGGPlannerPort)
//                        {
//                            PGGPlannerPort plannerPort = BaseConnection.PortReference as PGGPlannerPort;
//                            if (plannerPort.HasShape) return plannerPort.shape;
//                        }
//                        else if (BaseConnection.PortReference is PGGCellPort)
//                        {
//                            PGGCellPort cellPort = BaseConnection.PortReference as PGGCellPort;
//                            var cellChecker = GetCheckerFromCellPort(cellPort);
//                            if (cellChecker != null) return cellChecker;
//                        }
//                    }

//                    CheckerField3D containedChecker = Containing.GetCheckerReference();
//                    if (containedChecker != null) return containedChecker;
//                }

//                object val = GetPortValueSafe;

//                if (val is PGGCellPort.Data)
//                {
//                    PGGCellPort.Data data = (PGGCellPort.Data)val;
//                    if (FGenerators.CheckIfExist_NOTNULL(data.CellRef))
//                        if (FGenerators.CheckIfExist_NOTNULL(data.ParentChecker))
//                            return data.ParentChecker;
//                }

//                planner = GetPlannerFromPort(false);
//                if (planner) return planner.LatestChecker;

//                return null;
//            }
//        }


//        public FieldPlanner GetPlannerFromPort(bool callRead = true)
//        {
//            if (callRead) GetPortValueCall();

//            int plannerId = GetPlannerIndex();
//            int duplicateId = GetPlannerDuplicateIndex();
//            int subFieldID = GetPlannerSubFieldIndex();

//            if (Containing.UsingNumberedID)
//            {
//                if (Connections.Count == 0)
//                {
//                    if (UniquePlannerID > -1)
//                    {
//                        return PlannerRuleBase.GetFieldPlannerByID(UniquePlannerID, DuplicatePlannerID, SubFieldID);
//                    }
//                }
//                else
//                {
//                    if (BaseConnection.PortReference != null)
//                    {
//                        PGGCellPort cellPrt = BaseConnection.PortReference as PGGCellPort;

//                        if (cellPrt != null) // Cell Port
//                        {
//                            if (cellPrt.GetInputResultValue != null)
//                            {
//                                return cellPrt.GetInputResultValue.ParentFieldPlanner;
//                            }
//                        }
//                        else // Planner port
//                        {
//                            PGGPlannerPort fPort = BaseConnection.PortReference as PGGPlannerPort;
//                            if (fPort != null)
//                            {
//                                return PlannerRuleBase.GetFieldPlannerByID(fPort.UniquePlannerID, fPort.DuplicatePlannerID, fPort.SubFieldID);
//                            }
//                        }
//                    }
//                }

//            }
//            else // Containing references
//            {
//                return Containing.GetFieldPlannerReference();
//            }


//            return PlannerRuleBase.GetFieldPlannerByID(plannerId, duplicateId, subFieldID);
//        }


//        // Complex Read Referece Methods   END   ------------------------------------------


//        public void ProvideShape(CheckerField3D newChecker, Vector3? extraOffset = null)
//        {
//            var contained = Containing;
//            contained.FreeChecker = newChecker;
//            contained.OwnerPlanner = null;
//            contained.ForcedNull = false;
//            Containing = contained;
//        }

//        public CheckerField3D shape
//        {
//            get
//            {
//                if (!IsOutput)
//                    if (PortState() == EPortPinState.Connected)
//                    {
//                        var port = FirstConnectedPortOfType(typeof(PGGPlannerPort));
//                        if (port != null)
//                        {
//                            var plPrt = port as PGGPlannerPort;
//                            if (plPrt.HasShape) return plPrt.shape;
//                        }
//                        else
//                        {
//                            var cellPrt = FirstConnectedPortOfType(typeof(PGGCellPort));
//                            if (cellPrt != null) return GetCheckerFromCellPort(cellPrt);
//                        }
//                    }

//                return Containing.GetCheckerReference();
//            }
//        }





//        // CELL RELATED   START   ---------------------------------------------------


//        /// <summary> Checker from CELL Port </summary>
//        CheckerField3D GetCheckerFromCellPort(IFGraphPort cellPrt)
//        {
//            PGGCellPort plPrt = cellPrt as PGGCellPort;
//            PGGCellPort.Data cData = plPrt.CellData;

//            if (FGenerators.NotNull(cData.CellRef))
//            {
//                if (cData.ParentChecker != null)
//                {
//                    CheckerField3D ch = new CheckerField3D();
//                    ch.CopyParamsFrom(cData.ParentChecker);
//                    ch.AddLocal(cData.CellRef.Pos);
//                    return ch;
//                }
//            }

//            return null;
//        }


//        // CELL RELATED   END   ---------------------------------------------------



//        #endregion


//    }
//}