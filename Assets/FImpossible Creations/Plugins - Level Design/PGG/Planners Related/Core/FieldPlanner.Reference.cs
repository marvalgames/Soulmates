using FIMSpace.Generating.Checker;
using FIMSpace.Generating.Planning.PlannerNodes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.GeneratingLogics
{

    public class PlannerContainHelper
    {
        public enum EContains { Null, Planner, Checker, PlannerList, CheckerList  }
        public EContains Contains { get; private set; }

        FieldPlanner _planner = null;
        CheckerField3D _checker = null;

        List<FieldPlanner> _plannerList = null;
        public int GetPlannerListCount { get { if (_plannerList == null) return 0; return _plannerList.Count; } }

        List<ICheckerReference> _checkerList = null;
        public int GetCheckerListCount { get { if (_checkerList == null) return 0; return _checkerList.Count; } }

        /// <summary> List reserved for the single element list return without need to generate new instances of the planner list for gc optimization </summary>
        List<FieldPlanner> _input_SingleElementPlanner = new List<FieldPlanner>() { null };
        /// <summary> List reserved for the single element list return without need to generate new instances of the checker list for gc optimization </summary>
        List<ICheckerReference> _input_SingleElementChecker = new List<ICheckerReference>() { null };


        public void AssignPlanner(Vector3Int planner, bool multipleConnections_Additive = false)
        {
            AssignPlanner(PlannerRuleBase.GetFieldPlannerByID(planner.x, planner.y, planner.z, false), multipleConnections_Additive);
        }


        #region Int parameters

        public void AssignPlanner(int plannerIndex, bool multipleConnections_Additive = false)
        {
            AssignPlanner(new Vector3Int(plannerIndex, -1, -1));
        }

        public void AssignPlanner(int plannerIndex, int duplicateIndex, bool multipleConnections_Additive = false)
        {
            AssignPlanner(new Vector3Int(plannerIndex, duplicateIndex, -1));
        }

        public void AssignPlanner(int plannerIndex, int duplicateIndex, int subFieldIndex, bool multipleConnections_Additive = false)
        {
            AssignPlanner(new Vector3Int(plannerIndex, duplicateIndex, subFieldIndex));
        }

        #endregion


        public void AssignPlanner(FieldPlanner planner, bool multipleConnections_Additive = false)
        {
            if (multipleConnections_Additive) { AddPlanner(planner); return; }
            _planner = planner;

            if (_planner == null) Contains = EContains.Null;
            else
            Contains = EContains.Planner;
        }

        public void AssignChecker(CheckerField3D checker, bool multipleConnections_Additive = false)
        {
            if (multipleConnections_Additive) { AddChecker(checker); return; }
            _checker = checker;

            if (_checker == null) Contains = EContains.Null;
            else
            Contains = EContains.Checker;
        }

        public void AssignNull(bool multipleConnections_Additive = false)
        {
            if (multipleConnections_Additive) return;
            Contains = EContains.Null;
        }


        public void AssignPlannerList(List<FieldPlanner> planners, bool multipleConnections_Additive = false)
        {
            if (planners == null) return;

            if ( multipleConnections_Additive)
            {
                for (int i = 0; i < planners.Count; i++) AddPlanner(planners[i]);
                return;
            }

            if (_plannerList == null) _plannerList = new List<FieldPlanner>();

            _plannerList.Clear();
            PGGUtils.TransferFromListToList(planners, _plannerList);
            Contains = EContains.PlannerList;
        }

        private void AddPlanner(FieldPlanner planner)
        {
            if (planner == null) return;
            if (_plannerList == null) _plannerList = new List<FieldPlanner>();
            if (!_plannerList.Contains(planner)) _plannerList.Add(planner);
            Contains = EContains.PlannerList;
        }

        public void AssignCheckerList(List<ICheckerReference> checkers, bool multipleConnections_Additive = false)
        {
            if (checkers == null) return;

            if (multipleConnections_Additive)
            {
                for (int i = 0; i < checkers.Count; i++) AddChecker(checkers[i].CheckerReference);
                return;
            }

            if (_checkerList == null) _checkerList = new List<ICheckerReference>();
            _checkerList.Clear();

            PGGUtils.TransferFromListToListI(checkers, _checkerList);
            Contains = EContains.CheckerList;
        }

        public void AssignCheckerList(List<CheckerField3D> checkers, bool multipleConnections_Additive = false)
        {
            if (checkers == null) return;

            if (multipleConnections_Additive)
            {
                for (int i = 0; i < checkers.Count; i++) AddChecker(checkers[i]);
                return;
            }

            if (_checkerList == null) _checkerList = new List<ICheckerReference>();
            _checkerList.Clear();

            PGGUtils.TransferFromListToListI(checkers, _checkerList);
            Contains = EContains.CheckerList;
        }

        private void AddChecker(CheckerField3D checkers)
        {
            if (checkers == null) return;
            if (_checkerList == null) _checkerList = new List<ICheckerReference>();
            if (!_checkerList.Contains(checkers)) _checkerList.Add(checkers);
            Contains = EContains.CheckerList;
        }


        public object GetValue()
        {
            switch (Contains)
            {
                case EContains.Planner: return _planner;
                case EContains.PlannerList: return _plannerList;
                case EContains.Checker: return _checker;
                case EContains.CheckerList: return _checkerList;
            }

            return null;
        }

        public FieldPlanner GetPlanner()
        {
            if ( Contains == EContains.Planner ) return _planner;
            if ( Contains == EContains.PlannerList ) if ( _plannerList.Count > 0) return _plannerList[0];
            return null;
        }

        /// <summary> Returns single element dedicated list if demanding list but actually containing just single planner </summary>
        public List<FieldPlanner> GetPlannerList()
        {
            if (Contains == EContains.Planner) { _input_SingleElementPlanner[0] = _planner; return _input_SingleElementPlanner; }
            return _plannerList;
        }

        public CheckerField3D GetChecker()
        {
            if (Contains == EContains.Checker) return _checker;
            if ( Contains == EContains.CheckerList ) if ( _checkerList.Count > 0) return _checkerList[0].CheckerReference;
            return null;
        }

        /// <summary> Returns single element dedicated list if demanding list but actually containing just single checker </summary>
        public List<ICheckerReference> GetCheckerList()
        {
            if (Contains == EContains.Checker) { _input_SingleElementChecker[0] = _checker; return _input_SingleElementChecker; }
            return _checkerList;
        }

        public void Clear()
        {
            Contains = EContains.Null;
            _planner = null;
            _checker = null;

            if (_plannerList != null) _plannerList.Clear();
            //_plannerList = null;

            if (_checkerList != null) _checkerList.Clear();
            //_checkerList = null;
        }

        public bool ContainsNull { get { return Contains == EContains.Null; } }
        public bool ContainsPlanner { get { return Contains == EContains.Planner || Contains == EContains.PlannerList; } }
        public bool ContainsChecker { get { return Contains == EContains.Checker || Contains == EContains.CheckerList; } }

        public void Assign(PlannerContainHelper paramsOf)
        {
            Contains = paramsOf.Contains;
            _planner = paramsOf._planner;
            _checker = paramsOf._checker;
            _input_SingleElementChecker[0] = paramsOf._input_SingleElementChecker[0];
            _input_SingleElementPlanner[0] = paramsOf._input_SingleElementPlanner[0];

            if (paramsOf._plannerList != null)
            {
                if (_plannerList == null) _plannerList = new List<FieldPlanner>();
                _plannerList.Clear();
                PGGUtils.TransferFromListToList(paramsOf._plannerList, _plannerList);
            }

            if (paramsOf._checkerList != null)
            {
                if (_checkerList == null) _checkerList = new List<ICheckerReference>();
                _checkerList.Clear();
                PGGUtils.TransferFromListToList(paramsOf._checkerList, _checkerList);
            }
        }
    }



    /// <summary> Helper structure to support async operations and simplify some scripts. 
    /// Its used by sub-fields feature and by the planner nodes. </summary>
    public struct FieldPlannerReference
    {
        public FieldPlanner OwnerPlanner;
        public CheckerField3D FreeChecker;
        public bool ForcedNull;

        /// <summary> 
        /// X - FieldPlanner index on Build list
        /// Y - Duplicate Index (Instance-1). It's -1 if its no duplicate
        /// Z - Sub-Field Index. It's -1 if its no sub-field
        /// </summary>
        public Vector3Int NumberedID;

        public void SetNumberedID(int plannerId, int duplicateId = -1, int subFieldId = -1, bool nullPlanner = false)
        {
            NumberedID = new Vector3Int(plannerId, duplicateId, subFieldId);

            if (nullPlanner)
                OwnerPlanner = null;
            else
                OwnerPlanner = PlannerRuleBase.GetFieldPlannerByID(plannerId, duplicateId, subFieldId, false);
        }

        public void SetNumberedID(FieldPlanner of) { if (of == null) return; OwnerPlanner = of; NumberedID = new Vector3Int(of.IndexOnPreset, of.IsDuplicate ? of.IndexOfDuplicate : -1, of.IsSubField ? of.GetSubFieldID() : -1); }

        public int UniquePlannerID { get { return NumberedID.x; } set { Vector3Int n = NumberedID; n.x = value; NumberedID = n; } }
        public int DuplicatePlannerID { get { return NumberedID.y; } set { Vector3Int n = NumberedID; n.y = value; NumberedID = n; } }
        public int SubFieldID { get { return NumberedID.z; } set { Vector3Int n = NumberedID; n.z = value; NumberedID = n; } }

        public FieldPlannerReference(int x)
        {
            OwnerPlanner = null;
            FreeChecker = null;
            ForcedNull = false;
            NumberedID = new Vector3Int(-1, -1, -1);
        }

        public FieldPlannerReference(FieldPlanner parent, CheckerField3D extraChecker)
        {
            OwnerPlanner = parent;
            FreeChecker = extraChecker;
            ForcedNull = false;
            NumberedID = new Vector3Int(-1, -1, -1);
            if (extraChecker == null) SetNumberedID(parent);
        }

        public bool IsDefaultReturnCurrentChecker { get { return IsDefault && IsNumberedMinusOne; } }
        public bool IsNumberedMinusOne { get { return NumberedID == new Vector3Int(-1, -1, -1); } }
        /// <summary> When using just numbered ID : it's not null and not containing any checker / planner reference </summary>
        public bool IsDefault { get { return ForcedNull == false && /*OwnerPlanner == null &&*/ FreeChecker == null; } }
        public bool UsingNumberedID { get { return IsDefault; } }
        public bool IsNull { get { return ForcedNull; } }
        public bool IsAnyReferenceContained { get { return OwnerPlanner != null || FreeChecker != null; } }
        public bool IsFreeChecker { get { return OwnerPlanner == null && FreeChecker != null; } }
        public bool IsRootChecker { get { return OwnerPlanner != null && FreeChecker == null && !OwnerPlanner.IsDuplicate && !OwnerPlanner.IsSubField; } }
        public bool IsDuplicate { get { return OwnerPlanner != null && FreeChecker == null && OwnerPlanner.IsDuplicate; } }
        public bool IsSubField
        {
            get
            {
                if (OwnerPlanner == null) { if (FreeChecker == null) if (NumberedID.z > -1) return true; }
                else if (OwnerPlanner.IsSubField)
                    return true; return OwnerPlanner != null && FreeChecker != null && OwnerPlanner.IsSubField;
            }
        }


        public CheckerField3D GetCheckerReference()
        {
            if (IsNull) return null;
            if (IsFreeChecker) return FreeChecker;
            if (OwnerPlanner == null) return null;
            return OwnerPlanner.LatestChecker;
        }

        /// <summary> Trying to use OwnerPlanner, if null then trying to get planner from the checker reference, otherwise returning null </summary>
        public FieldPlanner GetFieldPlannerReference(bool checkFreeCheckerToo = true)
        {
            if (IsNull) return null;
            if (!checkFreeCheckerToo) { if (IsFreeChecker) return null; } else { if (FreeChecker != null) if (FreeChecker.SubFieldPlannerReference) return FreeChecker.SubFieldPlannerReference; }
            if (OwnerPlanner != null) if (OwnerPlanner.Available == false) return null;
            return OwnerPlanner;
        }

        public FieldPlanner GetFieldPlannerReference(FieldPlanner defaultPlanner, bool checkFreeCheckerToo = true)
        {
            if (IsNull) return null;
            if (!checkFreeCheckerToo) { if (IsFreeChecker) return null; } else { if (FreeChecker != null) if (FreeChecker.SubFieldPlannerReference) return FreeChecker.SubFieldPlannerReference; }
            if (OwnerPlanner == null) return defaultPlanner == null ? FieldPlanner.CurrentGraphExecutingPlanner : defaultPlanner;
            return OwnerPlanner;
        }

        public static bool operator ==(FieldPlannerReference a, FieldPlannerReference b) { return a.Equals(b); }
        public static bool operator !=(FieldPlannerReference a, FieldPlannerReference b) { return a.Equals(b); }

        public override bool Equals(object obj)
        {
            if ((obj is FieldPlannerReference) == false) return false;
            FieldPlannerReference fRef = (FieldPlannerReference)obj;

            if (OwnerPlanner == fRef.OwnerPlanner && FreeChecker == fRef.FreeChecker &&
                ForcedNull == fRef.ForcedNull && NumberedID == fRef.NumberedID)
                return true;

            return false;
        }


        public int GetSubFieldID()
        {
            if (IsSubField == false) return -1;
            if (OwnerPlanner == null) return -1;
            return OwnerPlanner.GetSubFieldID();
        }


        public override int GetHashCode() { return base.GetHashCode(); }

    }


    /// <summary>
    /// Interface implementing main required classes reference in order to be used during Build Planning
    /// </summary>
    //public interface IFieldPlanningContainer
    //{
    //    FieldPlanner ParentPlanner { get; }
    //    CheckerField3D LastestChecker { get; }
    //    PlannerResult LastestResult { get; }
    //}

}