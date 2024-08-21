using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{

    internal enum NavMeshLodIndex
    {
        First,
        Last,
        Custom
    }

    class TreeWizard : ScriptableWizard
    {

        private TreeManager treeManager;

        public static void CreateWindow(TreeManager treeManager, string buttonName)
        {
            var w = ScriptableWizard.DisplayWizard<TreeWizard>("Edit Trees", buttonName);
            w.InitializeDefaults(treeManager);
        }

        public void OnEnable()
        {
            minSize = new Vector2(400, 150);
        }

        internal void InitializeDefaults(TreeManager treeManager)
        {
            this.treeManager = treeManager;

            isValid = treeManager.IsValidTree();

            OnWizardUpdate();
        }

        void OnWizardCreate()
        {
            treeManager.DoApply();
        }

        void OnWizardOtherButton()
        {
            treeManager.DoApply();
        }


        protected override bool DrawWizardGUI()
        {
            bool changed = treeManager.DrawWizardGUI();

            if( changed)
            {
                isValid = treeManager.IsValidTree();
            }
            
            return changed;
        }

        internal void OnWizardUpdate()
        {
            if (treeManager == null || !treeManager.HasTree())
            {
                errorString = "Please assign a tree";
                isValid = false;
            }
            else
            {
                isValid = true;
                errorString = null;
            }
        }
    }


} //namespace