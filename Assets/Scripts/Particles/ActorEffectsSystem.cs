using System;
using Collisions;
using ProjectDawn.Navigation;
using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

//using Unity.Burst;

public enum SpawnStage
{
    None,
    Start,
    Play,
    End
}


public struct ParticleSystemComponent : IComponentData
{
    public float Value;
    public LocalTransform psLocalTransform;
    public SpawnStage spawnStage;
    public Entity parentEntity;
}

public struct PlayAndDestroyEffectComponent : IComponentData
{
    public int effectIndex;
    public bool play;
    public float playTime;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(HealthSystem))]
[UpdateAfter(typeof(PlayerMoveSystem))]
public partial struct ActorEffectsManagedSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (actor, impulseComponent) in SystemAPI.Query<ActorInstance, RefRW<ImpulseComponent>>())
        {
            var impulse = actor.actorPrefabInstance.GetComponent<Impulse>();
            if(!impulse) continue;
            var hitLanded = impulse.impulseSourceHitLanded;
            var hitReceived = impulse.impulseSourceHitReceived;
            if(!hitLanded || !hitReceived) continue;
            if (impulseComponent.ValueRW.hitLandedGenerateImpulse)
            {
                hitLanded.GenerateImpulse();
                impulseComponent.ValueRW.hitLandedGenerateImpulse = false;
            }
            else if (impulseComponent.ValueRW.hitReceivedGenerateImpulse)
            {
                hitReceived.GenerateImpulse();
                impulseComponent.ValueRW.hitReceivedGenerateImpulse = false;
            }
        }
    }
    
}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(HealthSystem))]
[UpdateAfter(typeof(PlayerMoveSystem))]
public partial struct ActorImpulseEffectsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var impulseGroup = SystemAPI.GetComponentLookup<ImpulseComponent>();

        foreach (var (impulseComponent, physicsVelocity, animatorWeights, e) in SystemAPI
                     .Query<RefRW<ImpulseComponent>, RefRW<PhysicsVelocity>, RefRW<AnimatorWeightsComponent>>()
                     .WithEntityAccess())
        {
            float damageLanded = 0;
            float damageReceived = 0;
            if (impulseComponent.ValueRW.maxTime <= 0) return;
            bool hasNavMesh = SystemAPI.HasComponent<AgentLocomotion>(e);
            bool hasDamageComponent = SystemAPI.HasComponent<DamageComponent>(e);
            if (hasDamageComponent)
            {
                var damageComponent = SystemAPI.GetComponent<DamageComponent>(e);
                damageLanded = damageComponent.DamageLanded;
                damageReceived = damageComponent.DamageReceived;
                if (damageLanded > 0 && impulseComponent.ValueRW.activate == false)
                {
                    impulseComponent.ValueRW.hitLandedGenerateImpulse = true;
                    //impulse.impulseSourceHitLanded.GenerateImpulse();
                    impulseComponent.ValueRW.activate = true;
                    ecb.AddComponent<Pause>(e);
                    var animSpeed = impulseComponent.ValueRW.animSpeedRatio;
                    impulseComponent.ValueRW.impulseAnimSpeed = animSpeed;
                    if (hasNavMesh)
                    {
                        var agent = SystemAPI.GetComponent<AgentLocomotion>(e);
                        agent.Speed = animSpeed;
                        SystemAPI.SetComponent(e, agent);
                    }

                    physicsVelocity.ValueRW.Linear *= math.float3(animSpeed, animSpeed, animSpeed);
                    animatorWeights.ValueRW.useImpulseSpeed = true;
                    animatorWeights.ValueRW.impulseSpeed = animSpeed;
                }
                else if (damageReceived > 0 && impulseComponent.ValueRW.activateOnReceived == false)
                {
                    impulseComponent.ValueRW.hitReceivedGenerateImpulse = true;
                    //impulse.impulseSourceHitReceived.GenerateImpulse();
                    impulseComponent.ValueRW.activateOnReceived = true;
                    ecb.AddComponent<Pause>(e);

                    var animSpeed = impulseComponent.ValueRW.animSpeedRatioOnReceived;
                    impulseComponent.ValueRW.impulseAnimSpeed = animSpeed;
                    if (hasNavMesh)
                    {
                        var agent = SystemAPI.GetComponent<AgentLocomotion>(e);
                        agent.Speed = animSpeed;
                        SystemAPI.SetComponent(e, agent);
                    }

                    physicsVelocity.ValueRW.Linear *= math.float3(animSpeed, animSpeed, animSpeed);
                    animatorWeights.ValueRW.useImpulseSpeed = true;
                    animatorWeights.ValueRW.impulseSpeed = animSpeed;
                }
            }
            else if (impulseComponent.ValueRW.activate &&
                     impulseComponent.ValueRW.timer <= impulseComponent.ValueRW.maxTime)
            {
                impulseComponent.ValueRW.timer += SystemAPI.Time.DeltaTime;
                //var animSpeed = animatorWeights.ValueRW.animSpeed;
                var animSpeed = impulseComponent.ValueRW.animSpeedRatio;

                physicsVelocity.ValueRW.Linear *= math.float3(animSpeed, animSpeed, animSpeed);
                //Debug.Log("timer " + impulseComponent.timer);
                if (impulseComponent.ValueRW.timer >= impulseComponent.ValueRW.maxTime)
                {
                    animatorWeights.ValueRW.useImpulseSpeed = false;
                    impulseComponent.ValueRW.timer = 0;
                    impulseComponent.ValueRW.activate = false;
                    animatorWeights.ValueRW.resetSpeed = true;
                    if (hasNavMesh)
                    {
                        var agent = SystemAPI.GetComponent<AgentLocomotion>(e);
                        agent.Speed = animSpeed;
                        SystemAPI.SetComponent(e, agent);
                    }

                    ecb.RemoveComponent<Pause>(e);
                }
            }
            else if (impulseComponent.ValueRW.activateOnReceived &&
                     impulseComponent.ValueRW.timerOnReceived <= impulseComponent.ValueRW.maxTimeOnReceived)
            {
                impulseComponent.ValueRW.timerOnReceived += SystemAPI.Time.DeltaTime;
                //var animSpeed = animatorWeights.ValueRW.animSpeed;
                var animSpeed = impulseComponent.ValueRW.animSpeedRatioOnReceived;

                physicsVelocity.ValueRW.Linear *= math.float3(animSpeed, animSpeed, animSpeed);
                //Debug.Log("timer " + impulseComponent.timer);
                if (impulseComponent.ValueRW.timerOnReceived >= impulseComponent.ValueRW.maxTimeOnReceived)
                {
                    animatorWeights.ValueRW.useImpulseSpeed = false;
                    impulseComponent.ValueRW.timerOnReceived = 0;
                    impulseComponent.ValueRW.activateOnReceived = false;
                    animatorWeights.ValueRW.resetSpeed = true;
                    //anim.speed = 1;
                    //if (hasNavMesh) anim.gameObject.GetComponent<NavMeshAgent>().speed = anim.speed;
                    if (hasNavMesh)
                    {
                        var agent = SystemAPI.GetComponent<AgentLocomotion>(e);
                        agent.Speed = animSpeed;
                        SystemAPI.SetComponent(e, agent);
                    }

                    ecb.RemoveComponent<Pause>(e);
                }
            }
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(HealthSystem))]
[UpdateAfter(typeof(PlayerMoveSystem))]
public partial struct SlashManagedSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (slash, slashClass) in SystemAPI.Query<RefRO<SlashComponent>, SlashClass>())
        {
            var audioSource = slashClass.audioSource;
            if (!audioSource) return;
            if (slash.ValueRO.slashState == (int)SlashStates.Started)
            {
                var clip = slashClass.audioSource.clip;
                audioSource.PlayOneShot(clip);
            }
        }
    }
}

[UpdateAfter(typeof(HealthSystem))]
[UpdateAfter(typeof(AttackerSystem))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct ActorDamageEffectsSystem : ISystem
{
    private bool isInitialized;
    private static readonly int HitReact = Animator.StringToHash("HitReact");
    private static readonly int Dead = Animator.StringToHash("Dead");

    public void OnUpdate(ref SystemState state)
    {
        if (!isInitialized)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var effectBuffer = SystemAPI.GetBufferLookup<EffectComponentElement>(true);


            foreach (var (actor, transform, effect, effectHolder, entity) in SystemAPI
                         .Query<ActorInstance, RefRW<LocalTransform>, RefRO<EffectComponent>, EffectClassHolder>()
                         .WithEntityAccess())
            {
                isInitialized = true;
                var effectElement = effectBuffer[entity];
                var count = effectElement.Length;
                var asPrefab = effectHolder.effectAudioSourcePrefab;
                var asInstance = GameObject.Instantiate(asPrefab);
                effectHolder.effectAudioSourceInstance = asInstance.GetComponent<AudioSource>();


                for (var i = 0; i < count; i++)
                {
                    var ve = effectHolder.effectsClassList[i].effectVisualEffect;
                    var ps = effectHolder.effectsClassList[i].effectParticleSystem;
                    //ve
                    var go = GameObject.Instantiate(ve);

                    go.transform.parent = actor.actorPrefabInstance.transform;
                    go.transform.localPosition = Vector3.zero;
                    effectHolder.effectsClassList[i].effectVisualEffectInstance = go; //can change to vfx member
                }
            }
        }
        else
        {
            //since managed may not need this could probably just use effects holder but never know
            var effectBuffer = SystemAPI.GetBufferLookup<EffectComponentElement>(true);


            foreach (var (dead, actor, effect, effectHolder, entity) in SystemAPI
                         .Query<RefRO<DeadComponent>, ActorInstance, RefRW<EffectComponent>,
                             EffectClassHolder>().WithEntityAccess())
            {
             
                //if (dead.ValueRO.isDead == false && damage.ValueRO.DamageReceived <= .0001f) continue;
                var hasDamage = SystemAPI.HasComponent<DamageComponent>(entity);


                var effectElement = effectBuffer[entity];
                var count = effectElement.Length;
                GameObject veInstance = null;
                AudioClip clip = null;
                AudioSource audioSource = effectHolder.effectAudioSourceInstance;

                if (hasDamage)
                {
                    var damage = SystemAPI.GetComponent<DamageComponent>(entity).DamageReceived;
                    if (damage < .0001f)
                    {
                        hasDamage = false;
                    }
                }

                if (dead.ValueRO.isDead == false && hasDamage)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (effectElement[i].playEffectType == EffectType.Damaged)
                        {
                            veInstance = effectHolder.effectsClassList[i].effectVisualEffectInstance;
                            clip = effectHolder.effectsClassList[i].effectClip;
                        }
                    }

                    if (veInstance != null)
                    {
                        veInstance.GetComponent<VisualEffect>().Play();
                    }

                    if (audioSource != null)
                    {
                        audioSource.clip = clip;
                        audioSource.PlayOneShot(clip);
                    }

                    var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                    Debug.Log("Animator " + animator);
                    animator.SetInteger(HitReact, 1);
                }
                else if (dead.ValueRO.isDead && effect.ValueRW.disableEffect == false)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (effectElement[i].playEffectType == EffectType.Dead)
                        {
                            veInstance = effectHolder.effectsClassList[i].effectVisualEffectInstance;
                            clip = effectHolder.effectsClassList[i].effectClip;
                        }
                    }

                    if (veInstance != null)
                    {
                        veInstance.GetComponent<VisualEffect>().Play();
                        Debug.Log("dead " + dead.ValueRO.isDead);
                        effect.ValueRW.disableEffect = true;
                        
                    }

                    if (audioSource != null)
                    {
                        audioSource.clip = clip;
                        audioSource.PlayOneShot(clip);
                    }

                    var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                    Debug.Log("Animator " + animator);
                    animator.SetInteger(Dead, 1);
                }
            }
        }
    }
}




//
// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
// [UpdateAfter(typeof(BreakableEffectsSystem))]
// [RequireMatchingQueriesForUpdate]
// public partial class ParticleInstanceSystem : SystemBase
// {
//     protected override void OnUpdate()
//     {
//         var ecb = new EntityCommandBuffer(Allocator.Temp);
//
//
//         Entities.WithoutBurst().ForEach(
//             (
//                 Entity e,
//                 ref ParticleSystemComponent psComponent,
//                 in ParticleSystem particleSystem
//             ) =>
//             {
//                 if (psComponent.spawnStage == SpawnStage.None)
//                 {
//                     psComponent.spawnStage = SpawnStage.Play;
//                     if (particleSystem != null)
//                     {
//                         Debug.Log("ENTITY SPAWN STAGE " + e + " PS " + particleSystem.isPlaying + " " +
//                                   particleSystem);
//                     }
//                 }
//                 else if (!particleSystem.isPlaying && psComponent.spawnStage == SpawnStage.Play)
//                 {
//                     particleSystem.Play(true);
//                     particleSystem.transform.position = psComponent.psLocalTransform.Position;
//                     Debug.Log("SPAWN PLAY " + e + " PS " + particleSystem.isPlaying + " " + particleSystem);
//                 }
//                 else if (particleSystem.isPlaying && psComponent.spawnStage == SpawnStage.Play)
//                 {
//                     if (SystemAPI.HasComponent<PlayAndDestroyEffectComponent>(psComponent.parentEntity)
//                         && particleSystem.time >= .2) //make sure it plays first
//                     {
//                         psComponent.spawnStage = SpawnStage.End;
//                         ecb.AddComponent(e, typeof(DestroyComponent));
//                         particleSystem.Stop();
//                     }
//                 }
//             }
//         ).Run();
//
//
//         ecb.Playback(EntityManager);
//         ecb.Dispose();
//     }
// }
//

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BreakableCollisionHandlerSystem))]
public partial class BreakableEffectsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Entities.WithoutBurst().ForEach(
        //     (
        //         Entity e,
        //         AudioSource audioSource,
        //         EffectClass effect,
        //         ref PlayAndDestroyEffectComponent playAndDestroyEffect
        //     ) =>
        //     {
        //         if (playAndDestroyEffect.play && effect.psEntity != Entity.Null &&
        //             effect.transformGameObject != null)
        //         {
        //             var tr = LocalTransform.FromPosition(effect.transformGameObject.transform.position);
        //             var psTag = new ParticleSystemComponent
        //             {
        //                 psLocalTransform = tr,
        //                 parentEntity = e
        //             };
        //
        //             playAndDestroyEffect.play = false;
        //             if (effect.clip)
        //             {
        //                 if (!audioSource.isPlaying)
        //                 {
        //                     audioSource.PlayOneShot(effect.clip, 1.0f);
        //                 }
        //             }
        //
        //             Entity ps = ecb.Instantiate(effect.psEntity);
        //             ecb.AddComponent(ps, psTag);
        //             ecb.SetComponent(ps, tr);
        //         }
        //     }
        // ).Run();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}