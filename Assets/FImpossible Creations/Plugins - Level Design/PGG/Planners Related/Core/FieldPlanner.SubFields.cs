using UnityEngine;
using System.Collections.Generic;
using System;
using FIMSpace.Generating.Checker;

namespace FIMSpace.Generating.Planning
{
    public partial class FieldPlanner
    {

        /// <summary> If it's sub field generated through code </summary>
        public bool IsSubField { get; private set; }

        [NonSerialized] private List<FieldPlanner> SubFields = null;


        public int GetSubFieldsCount
        {
            get
            {
                if (SubFields == null) return 0;
                return SubFields.Count;
            }
        }

        public void ClearSubFields()
        {
            if (SubFields != null) SubFields.Clear();
        }

        public FieldPlanner GetSubField(int index)
        {
            if (GetSubFieldsCount == 0) return null;
            if (SubFields.ContainsIndex(index) == false) return null;
            return SubFields[index];
        }

        public int GetSubFieldID()
        {
            if (DuplicateParent == null) return -1;
            for (int s = 0; s < DuplicateParent.SubFields.Count; s++)
            {
                if (DuplicateParent.SubFields[s] == this) return s;
            }

            return -1;
        }

        public FieldPlanner AddSubField(CheckerField3D sourceChecker)
        {
            if (sourceChecker == null) return null;
            if (SubFields == null) SubFields = new List<FieldPlanner>();

            FieldPlanner subField = null;

            bool asyncGen = false;
            if (ParentBuildPlanner) asyncGen = ParentBuildPlanner.AsyncGenerating;

            if (asyncGen)
            {
                UnityEngine.Debug.Log(" PGG !!! Sub Fields Are Not Yet Implemented with Async Generating!");

                try
                {
                    subField = Instantiate(this);
                }
                catch (Exception exc)
                {
//#if UNITY_EDITOR
//                    UnityEditor.EditorUtility.DisplayDialog("Adding Sub Field Error", "Error occured when trying to add Sub-Field!\nCheck console log." + (asyncGen ? "\n\n!!! Sub Fields are NOT supporting yet the ASYNC GENERATING !!!" : ""), "Ok");
//#endif
                    Debug.LogException(exc);
                    return null;
                }
            }
            else subField = Instantiate(this);

            if (DefaultFieldSetup)
            {
                subField.PreviewCellSize = DefaultFieldSetup.GetCellUnitSize();
                sourceChecker.RootScale = subField.PreviewCellSize;
            }

            subField.LatestResult = PlannerResult.GenerateInstance(ParentBuildPlanner, subField);
            subField.LatestResult.ParentFieldPlanner = subField;
            subField.LatestResult.ParentBuildPlanner = ParentBuildPlanner;
            sourceChecker.SubFieldPlannerReference = subField;
            subField.LatestResult.Checker = sourceChecker;
            subField.DuplicateParent = this;
            subField.IsSubField = true;
            subField.DisableWholePlanner = false;
            
            subField.WasCalled = true;
            subField.WasPreExecuted = true;
            subField.Discarded = false;

            SubFields.Add(subField);

            return subField;
        }
    }
}