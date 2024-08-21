using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    /// <summary>
    /// Implementations of this interface will be executed when dropped from the content browser into the scene view
    /// </summary>
    public interface IContentBrowserDropAction
    {
        /// <summary>
        /// Function that is invoked when a prefab is dropped from the content browser
        /// </summary>
        /// <param name="destroyAfterExecute">If true the gameobject will be destroyed from the content browser after the execute method was invoked</param>
        void Execute( out bool destroyAfterExecute);

    }

}