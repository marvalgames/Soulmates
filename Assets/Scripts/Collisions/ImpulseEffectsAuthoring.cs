using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    public struct ImpulseComponent : IComponentData
    {
        public float timer;
        public float maxTime;
        public float animSpeedRatio;
        public bool activate;

        public float timerOnReceived;
        public float maxTimeOnReceived;
        public float animSpeedRatioOnReceived;
        public bool activateOnReceived;
    }
    public class ImpulseEffectsAuthoring : MonoBehaviour
    {
        //public bool pauseEffect;
        [Header("All Characters")]
        public float maxTime = 1.0f;
        public float animSpeedRatio = .5f;

        public float maxTimeOnReceived = 1.0f;
        public float animSpeedRatioOnReceived = .5f;

        class ImpulseBaker : Baker<ImpulseEffectsAuthoring>
        {
            public override void Bake(ImpulseEffectsAuthoring effectsAuthoring)
            {
                var e = GetEntity(effectsAuthoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(e, new EffectsComponent());
                AddComponent(e, new ImpulseComponent
                    {
                        maxTime = effectsAuthoring.maxTime,
                        animSpeedRatio = effectsAuthoring.animSpeedRatio,
                        maxTimeOnReceived = effectsAuthoring.maxTimeOnReceived,
                        animSpeedRatioOnReceived = effectsAuthoring.animSpeedRatioOnReceived
                    }
                );

            }
        }


    }
}