using Player;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

public class VisualEffectGO : IComponentData
{
    public VisualEffect VisualEffect;
}

public class AudioPlayerGO : IComponentData
{
    public AudioSource AudioSource;
    public AudioClip AudioClip;
}

public class VisualEffectJumpGO : IComponentData
{
    public VisualEffect VisualEffect;
}

public class AudioPlayerJumpGO : IComponentData
{
    public AudioSource AudioSource;
    public AudioClip AudioClip;
}

public class BossAmmoManagerGO : IComponentData
{
    public AudioSource audioSource;
    public AudioClip clip;
}

public class DashAudioVideoGO : IComponentData
{
    public AudioSource AudioSource;
    public AudioClip AudioClip;
    public VisualEffect VisualEffect;
}


public partial struct InstantiatePrefabSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (prefab, actor, entity) in
                 SystemAPI.Query<PlayerMoveGameObjectClass, ActorInstance>().WithEntityAccess())
        {
            var visualEffectGo = new VisualEffectGO();
            if (prefab.vfxSystemGo)
            {
                var vfxGo = GameObject.Instantiate(prefab.vfxSystemGo, actor.actorPrefabInstance.transform, true);
                vfxGo.transform.localPosition = Vector3.zero;
                visualEffectGo.VisualEffect = vfxGo.GetComponent<VisualEffect>();
            }

            ecb.AddComponent(entity, visualEffectGo);

            var audioPlayerGo = new AudioPlayerGO();
            if (prefab.audioSourceGo)
            {
                var audioGo = GameObject.Instantiate(prefab.audioSourceGo);
                audioPlayerGo.AudioSource = audioGo.GetComponent<AudioSource>();
                audioPlayerGo.AudioClip = prefab.clip;
            }

            ecb.AddComponent(entity, audioPlayerGo);


            ecb.RemoveComponent<PlayerMoveGameObjectClass>(
                entity); //did it like this so to remove the class so it doesn't instantiate each update (Old method)
        }

        foreach (var (prefab, actor, entity) in
                 SystemAPI.Query<PlayerJumpGameObjectClass, ActorInstance>().WithEntityAccess())
        {
            var visualEffectJumpGo = new VisualEffectJumpGO();
            if (prefab.vfxSystem)
            {
                var vfxGo = GameObject.Instantiate(prefab.vfxSystem, actor.actorPrefabInstance.transform, true);
                vfxGo.transform.localPosition = Vector3.zero;
                visualEffectJumpGo.VisualEffect = vfxGo.GetComponent<VisualEffect>();
            }

            ecb.AddComponent(entity, visualEffectJumpGo);

            var audioPlayerJumpGo = new AudioPlayerJumpGO();

            if (prefab.audioSourceGo)
            {
                var audioGo = GameObject.Instantiate(prefab.audioSourceGo);
                audioPlayerJumpGo.AudioSource = audioGo.GetComponent<AudioSource>();
                audioPlayerJumpGo.AudioClip = prefab.clip;
                ecb.AddComponent(entity, audioPlayerJumpGo);
                Debug.Log("Audio Player Jumped");
            }

            ecb.AddComponent(entity, audioPlayerJumpGo);

            ecb.RemoveComponent<PlayerJumpGameObjectClass>(entity);
        }


        foreach (var (prefab, actor, entity) in
                 SystemAPI.Query<PlayerDashGameObjectClass, ActorInstance>().WithEntityAccess())
        {
            var dashAudioVideo = new DashAudioVideoGO();
            if (prefab.audioSource)
            {
                var audioGo = GameObject.Instantiate(prefab.audioSource);
                dashAudioVideo.AudioSource = audioGo.GetComponent<AudioSource>();
                dashAudioVideo.AudioClip = prefab.audioClip;
            }

            if (prefab.vfxPrefab)
            {
                var vfxGo = GameObject.Instantiate(prefab.vfxPrefab, actor.actorPrefabInstance.transform, true);
                vfxGo.transform.localPosition = Vector3.zero;
                dashAudioVideo.VisualEffect = vfxGo.GetComponent<VisualEffect>();
            }

            ecb.AddComponent(entity, dashAudioVideo);
            ecb.RemoveComponent<PlayerDashGameObjectClass>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}