using Unity.Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    public struct ImpulseComponent : IComponentData
    {
        public float timer;
        public float maxTime;
        public float impulseAnimSpeed;
        public float animSpeedRatio;
        public bool activate;

        public float timerOnReceived;
        public float maxTimeOnReceived;
        public float animSpeedRatioOnReceived;
        public bool activateOnReceived;
        //public bool aiAgent;

        public bool hitReceivedGenerateImpulse;
        public bool hitLandedGenerateImpulse;
    }
    public class ImpulseEffectsAuthoring : MonoBehaviour
    {
        //public bool pauseEffect;
        [Header("All Characters")]
        public float maxTime = 1.0f;
        public float animSpeedRatio = .5f;

        public float maxTimeOnReceived = 1.0f;
        public float animSpeedRatioOnReceived = .5f;
        
        // [Header("Player Only")] [Tooltip("Player Hurt")]
        // public CinemachineImpulseSource impulseSourceHitReceived;
        //
        // [Tooltip("Player Hits Enemy")] public CinemachineImpulseSource impulseSourceHitLanded;

        //public bool aiAgent = true;

        class ImpulseBaker : Baker<ImpulseEffectsAuthoring>
        {
            public override void Bake(ImpulseEffectsAuthoring effectsAuthoring)
            {
                var e = GetEntity(effectsAuthoring.gameObject, TransformUsageFlags.Dynamic);

                //AddComponent(e, new EffectComponent());
                AddComponent(e, new ImpulseComponent
                    {
                        //aiAgent = effectsAuthoring.aiAgent,
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