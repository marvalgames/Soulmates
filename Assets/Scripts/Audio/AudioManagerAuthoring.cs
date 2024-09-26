using Unity.Entities;
using UnityEngine;

namespace Audio
{
    public class AudioManagerAuthoring : MonoBehaviour
    {
        private class AudioManagerAuthoringBaker : Baker<AudioManagerAuthoring>
        {
            public override void Bake(AudioManagerAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e,
                    new AudioManagerComponent
                    {
                        
                    });
                AddComponentObject(e, new AudioManagerClass());
            }
        }
    }
}