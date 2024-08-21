using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(BrowserToolsData))]
    public class BrowserToolsEditor : Editor
    {
        private BrowserToolsEditor editor;
        private BrowserToolsData editorTarget;

        private BrowserToolsModule module;

        public BrowserToolsEditor()
        {
            this.module = new BrowserToolsModule();
        }

        public void OnEnable()
        {
            editor = this;
            editorTarget = (BrowserToolsData)target;

            module.OnEnable(serializedObject, editorTarget);

        }

        public void OnDisable()
        {
            module.OnDisable();
        }
        public override void OnInspectorGUI()
        {
            module.OnInspectorGUI();
        }

    }
}

