using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    /// <summary>
    /// Instance type handling of presets. A category can exist only once in the microverse environment
    /// </summary>
    public class PresetInstance : MonoBehaviour, IContentBrowserDropAction
    {
        public enum Category
        {
            None,
            Sky,
            Fog,
            Water,
        }

        /// <summary>
        /// What should happen when a duplicate instance is found?
        /// </summary>
        public enum DuplicateFoundAction
        {
            Hide,
            Destroy,
        }

        public Category category = Category.None;

        private DuplicateFoundAction duplicateFoundAction = DuplicateFoundAction.Destroy;

        #region ContentBrowser
        /// <summary>
        /// Execute an action after this prefab was dropped into the scene from the content browser
        /// </summary>
        /// <param name="destroyAfterExecute"></param>
        public void Execute(out bool destroyAfterExecute)
        {
            // assuming this is the only gameobject of the type in the hierarchy
            destroyAfterExecute = false;

            // find all of type (including self)
            PresetInstance[] instances = GameObject.FindObjectsByType<PresetInstance>(FindObjectsSortMode.None);

            foreach (PresetInstance instance in instances)
            {
                if (instance.category != category)
                    continue;

                // skip deactivated ones
                if (!instance.isActiveAndEnabled)
                    continue;

                // exclude self
                if (instance.transform == this.transform)
                    continue;

                // Debug.Log($"Prefab {timeOfDay.name} exists, applying settings to it");
                switch(duplicateFoundAction)
                {
                    case DuplicateFoundAction.Hide:

                        // Undo.RegisterCompleteObjectUndo(instance.gameObject, "Hide duplicate");

                        instance.transform.gameObject.SetActive(false);

                        break;

                    case DuplicateFoundAction.Destroy:
                        
                        // Undo.RegisterCompleteObjectUndo(instance.gameObject, "Delete duplicate");

                        DestroyImmediate(instance.gameObject);

                        break;

                    default: 
                        Debug.LogError($"Unsupported duplicate found action {duplicateFoundAction}");
                        break;

                }
                // the gameobject already existed, destroy this one
                destroyAfterExecute = false;
            }
        }
        #endregion ContentBrowser
    }
}
