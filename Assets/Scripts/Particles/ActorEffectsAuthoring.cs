using Unity.Entities;
using UnityEngine;

namespace Particles
{
    public class ActorEffectsAuthoring : MonoBehaviour
    {
        private class ActorEffectsAuthoringBaker : Baker<ActorEffectsAuthoring>
        {
            public override void Bake(ActorEffectsAuthoring authoring)
            {
            }
        }
    }
}