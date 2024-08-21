using PixelCrushers.DialogueSystem;
using Unity.Entities;
using UnityEngine;

namespace Quests
{
    public class MissionComponentAuthoring : MonoBehaviour
    {
       
        // [VariablePopup] public string variable = string.Empty;
        //
        // protected virtual string actualVariableName
        // {
        //     get { return string.IsNullOrEmpty(variable) ? DialogueActor.GetPersistentDataName(transform) : variable; }
        // }


        class MissionComponentBaker : Baker<MissionComponentAuthoring>
        {
            public override void Bake(MissionComponentAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new MissionComponent());
            }
        }
    }
}