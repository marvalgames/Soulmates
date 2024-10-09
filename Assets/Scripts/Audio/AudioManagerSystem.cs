using Enemy;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Audio
{
    public struct AudioManagerComponent : IComponentData
    {
        public bool play;
    }

    public class AudioManagerClass : IComponentData
    {
        public AudioSource source;
        public AudioClip clip;
        public AnimationStage stage;
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    //[UpdateAfter(typeof(EnemySelectMoveManagedMeleeSystem))]
    [RequireMatchingQueriesForUpdate] 
    public partial struct AudioManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (audioClass, movesInstance, audio, entity) in SystemAPI
                         .Query<AudioManagerClass, MovesInstance, RefRW<AudioManagerComponent>>()
                         .WithEntityAccess())
            {
                var audioSource = movesInstance.meleeAudioSourceInstance.GetComponent<AudioSource>();

                if (audio.ValueRW.play && audioSource.isPlaying == false)
                {
                    audioSource.PlayOneShot(audioClass.clip);
                }
                else
                {
                    audio.ValueRW.play = false;
                }
            }
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}