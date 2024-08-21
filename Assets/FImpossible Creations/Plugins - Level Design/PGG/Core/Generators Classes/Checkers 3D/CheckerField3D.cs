using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Checker
{
    /// <summary> Interface to ease iterating checkers through lists/arrays without using separate type lists etc. </summary>
    public interface ICheckerReference
    {
        CheckerField3D CheckerReference { get; }
    }

    public partial class CheckerField3D : ICheckerReference
    {
        /// <summary> Implemented mainly for the SubFields </summary>
        public FIMSpace.Generating.Planning.FieldPlanner SubFieldPlannerReference = null;
        public CheckerField3D CheckerReference { get { return this; } }

        /// <summary> Implemented for use of Cell Ports to define cells parent </summary>
        //public FIMSpace.Generating.Planning.FieldPlanner CellsFieldParent = null;

        public CheckerField3D()
        {
            MatrixReset();
        }

        public Vector3 RootPosition
        {
            get { if (AttachRootTo) return AttachRootTo.position; else return _rootPosition; }
            set { if (AttachRootTo) AttachRootTo.position = value; else _rootPosition = value; RefreshMatrix(); }
        }

        public void RoundRootPositionToScale()
        {
            RootPosition = FVectorMethods.FlattenVector(RootPosition, RootScale);
        }

        /// <summary> Checker bounds must be recalculated before calling this </summary>
        public void SetRootPositionInWorldPosCentered(Vector3 worldPos, bool roundPosition = false)
        {
            Bounds full = GetFullBoundsWorldSpace();
            Vector3 diff = worldPos - full.center;
            RootPosition = RootPosition + diff;
            if (roundPosition) RoundRootPosition();
        }

        [SerializeField] Vector3 _rootPosition = Vector3.zero;

        public Quaternion RootRotation
        {
            get { if (AttachRootTo) return AttachRootTo.rotation; else return _rootRotation; }
            set { if (AttachRootTo) AttachRootTo.rotation = value; else _rootRotation = value; RefreshMatrix(); }
        }

        [SerializeField] Quaternion _rootRotation = Quaternion.identity;

        public Vector3 RootScale
        {
            get { return _RootScale; }
            set { _RootScale = value; RefreshMatrix(); }
        }

        public Vector3 _RootScale = Vector3.one;

        public Vector3 RootScaleX { get { return new Vector3(RootScale.x, 0f, 0f); } }
        public Vector3 RootScaleY { get { return new Vector3(0f, RootScale.y, 0f); } }
        public Vector3 RootScaleZ { get { return new Vector3(0f, 0f, RootScale.z); } }

        public Vector3 ScaleV3(Vector3 toScale) { if (RootScale == Vector3.one) return toScale; return Vector3.Scale(toScale, RootScale); }

        public FGenGraph<FieldCell, FGenPoint> Grid = new FGenGraph<FieldCell, FGenPoint>();

        public bool UseBounds = false;
        public int HelperId = 0;

        public int ChildPositionsCount { get { return AllCells.Count; } }
        public System.Collections.Generic.List<FieldCell> AllCells { get { return Grid.AllApprovedCells; } }

        public Vector3 ChildPos(int index) { if (index < 0) return Vector3.zero; if (index >= AllCells.Count) return Vector3.zero; return AllCells[index].Pos; }

        //public List<SpawnInstruction> Instructions { get { return _instructions; } }
        //[SerializeField] List<SpawnInstruction> _instructions = new List<SpawnInstruction>();

    }
}