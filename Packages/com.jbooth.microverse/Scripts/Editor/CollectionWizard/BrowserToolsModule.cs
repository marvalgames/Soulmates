using JBooth.MicroVerseCore;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    public class BrowserToolsModule
    {
        private BrowserToolsModule editor;
        private BrowserToolsData editorTarget;
        private SerializedObject serializedObject;

        private SerializedProperty module;

        private IBrowserToolsModule presetsModule;

        private GUIContent[] modeButtons;

  
        public void OnEnable(SerializedObject serializedObject, BrowserToolsData target)
        {
            this.editor = this;
            this.editorTarget = target;
            this.serializedObject = serializedObject;

            this.presetsModule = new PresetsModule(editor, serializedObject);

            Init();

        }

        private void Init()
        {
            modeButtons = new GUIContent[]
            {
                new GUIContent( "Presets", "Presets settings")
            };

            module = serializedObject.FindProperty("module");

        }


        public void OnDisable()
        {
        }

        public BrowserToolsData GetEditorTarget()
        {
            return editorTarget;
        }

        #region Inspector
        public void OnInspectorGUI()
        {
            serializedObject.Update();

            // modules
            EditorGUI.BeginChangeCheck();
            {
                module.intValue = GUILayout.Toolbar(module.intValue, modeButtons, EditorStyles.miniButton);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ModeChanged((BrowserToolsData.Module)module.intValue);
            }

            // control
            switch (editorTarget.module)
            {
                case BrowserToolsData.Module.Presets:
                    presetsModule.OnInspectorGUI();
                    break;

                default:
                    throw new System.Exception("Unsupported mode: " + editorTarget.module);
            }

            // apply changes to serializedProperty
            serializedObject.ApplyModifiedProperties();

        }
        #endregion Inspector

        private void ModeChanged(BrowserToolsData.Module mode)
        {
            // nothing to do yet
        }
        public SerializedObject GetSerializedObject()
        {
            return this.serializedObject;
        }
    }

}

