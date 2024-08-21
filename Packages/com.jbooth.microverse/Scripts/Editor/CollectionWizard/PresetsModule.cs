using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    public class PresetsModule : IBrowserToolsModule
    {
        private BrowserToolsModule editor;
        private BrowserToolsData editorTarget;
        private SerializedObject serializedObject;

        private PresetsController controller1;

        public PresetsModule(BrowserToolsModule editor, SerializedObject serializedObject)
        {
            this.editor = editor;
            this.editorTarget = editor.GetEditorTarget();
            this.serializedObject = serializedObject;

            Init();
        }

        private void Init()
        {

            // controllers
            controller1 = new PresetsController(editor);

        }

        public void OnInspectorGUI()
        {

            // EditorGUILayout.LabelField("Game Manager", GUIStyles.BoxTitleStyle);

            controller1.OnInspectorGUI();

        }


    }
}