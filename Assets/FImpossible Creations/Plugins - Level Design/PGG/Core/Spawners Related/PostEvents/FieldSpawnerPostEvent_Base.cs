using UnityEngine;

namespace FIMSpace.Generating
{
    public abstract class FieldSpawnerPostEvent_Base : ScriptableObject
    {
        [HideInInspector] public string PostEventInfo = "";


        /// <summary> Called before proceeding to run grid procedures </summary>
        public virtual void OnBeforeRunningCall(FieldSetup.CustomPostEventHelper helper, FieldSetup preset)
        {

        }

        /// <summary> Called before proceeding objects spawn </summary>
        public virtual void OnBeforeGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {

        }

        /// <summary> Called after generating, but before custom generators (like stampers), before reflection probes generating, before trigger zones generating and before mesh combining </summary>
        public virtual void OnBeforeCorePostEventsCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {

        }

        /// <summary> Called after all generating, after all core post events and after mesh combining </summary>
        public virtual void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {

        }

#if UNITY_EDITOR
        /// <summary> [ Needs to be inside   !!!   #if UNITY_EDITOR #endif   !!! ] </summary>
        public virtual void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
        }
#endif

    }
}