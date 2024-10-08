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
        // Get all Entities that have the component with the Entity reference
        foreach (var (prefab, entity) in
                 SystemAPI.Query<PlayerMoveGameObjectClass>().WithEntityAccess())
        {
            if (prefab.vfxSystemGo)
            {
                GameObject vfxGo = GameObject.Instantiate(prefab.vfxSystemGo);
                ecb.AddComponent(entity,
                    new VisualEffectGO { VisualEffect = vfxGo.GetComponent<VisualEffect>() });
            }

            GameObject audioGo = GameObject.Instantiate(prefab.audioSourceGo);
            ecb.AddComponent(entity,
                new AudioPlayerGO { AudioSource = audioGo.GetComponent<AudioSource>(), AudioClip = prefab.clip });
            ecb.RemoveComponent<PlayerMoveGameObjectClass>(entity);
        }

        foreach (var (prefab, actor, entity) in
                 SystemAPI.Query<PlayerJumpGameObjectClass, ActorInstance>().WithEntityAccess())
        {
            if (prefab.vfxSystem)
            {
                GameObject vfxGo = GameObject.Instantiate(prefab.vfxSystem);
                vfxGo.transform.parent = actor.actorPrefabInstance.transform;
                vfxGo.transform.localPosition = Vector3.zero;
                
                ecb.AddComponent(entity,
                    new VisualEffectJumpGO { VisualEffect = vfxGo.GetComponent<VisualEffect>() });
            }

            GameObject audioGo = GameObject.Instantiate(prefab.audioSourceGo);
            Debug.Log("Audio Player Jumped");
            ecb.AddComponent(entity,
                new AudioPlayerJumpGO { AudioSource = audioGo.GetComponent<AudioSource>(), AudioClip = prefab.clip });
            ecb.RemoveComponent<PlayerJumpGameObjectClass>(entity);
        }

        foreach (var (prefab, entity) in
                 SystemAPI.Query<BossAmmoManagerClass>().WithEntityAccess())
        {

            GameObject audioGo = GameObject.Instantiate(prefab.audioSourceGo);
            ecb.AddComponent(entity,
                new BossAmmoManagerGO { audioSource = audioGo.GetComponent<AudioSource>(), clip = prefab.clip });
            ecb.RemoveComponent<BossAmmoManagerClass>(entity);
        }

        foreach (var (prefab, entity) in
                 SystemAPI.Query<PlayerDashGameObjectClass>().WithEntityAccess())
        {

            GameObject audioGo = GameObject.Instantiate(prefab.audioSource);
            ecb.AddComponent(entity,
                new DashAudioVideoGO
                { AudioSource = audioGo.GetComponent<AudioSource>(), AudioClip = prefab.audioClip
                    // , VisualEffect = prefab.vfxPrefab.GetComponent<VisualEffect>()
                    
                });
            ecb.RemoveComponent<PlayerDashGameObjectClass>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}