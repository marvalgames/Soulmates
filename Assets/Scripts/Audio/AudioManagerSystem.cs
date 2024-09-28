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
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySelectMoveManagedMeleeSystem))]
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
            foreach (var (audioClass, audio, entity) in SystemAPI
                         .Query<AudioManagerClass, RefRW<AudioManagerComponent>>()
                         .WithEntityAccess())
            {
                if (audio.ValueRW.play && audioClass.source.isPlaying == false)
                {
                    audioClass.source.PlayOneShot(audioClass.clip);
                    Debug.Log("Move started audio " + audioClass.clip);
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