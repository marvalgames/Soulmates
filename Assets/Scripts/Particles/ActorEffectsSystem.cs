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
    public void Update(ref SystemState state)
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

    public void OnUpdate(ref SystemState state)
    {
        if (!isInitialized)
        {
            isInitialized = true;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var effectBuffer = SystemAPI.GetBufferLookup<EffectComponentElement>(true);
            

            foreach (var (transform, effect, effectHolder, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<EffectComponent>, EffectClassHolder>()
                         .WithEntityAccess())
            {
                var effectElement = effectBuffer[entity]; 
                var count = effectElement.Length;
                Debug.Log("EFFECT COUNT " + count);
                for (var i = 0; i < count; i++)
                {
                    var ve = effectHolder.effectsClassList[i].effectVisualEffect;
                    var ps = effectHolder.effectsClassList[i].effectParticleSystem;
                    //ve
                    var go = GameObject.Instantiate(ve);
                    effectHolder.effectsClassList[i].effectVisualEffectInstance = go;
                }
            }
        }
    }
}


// public partial class CharacterDamageEffectsSystem : SystemBase
// {
//     private static readonly int HitReact = Animator.StringToHash("HitReact");
//
//     protected override void OnUpdate()
//     {
//         // Entities.WithoutBurst().WithNone<Pause>().ForEach(
//         //     (
//         //         Entity e,
//         //         ref DeadComponent deadComponent,
//         //         ref EffectComponent effectsComponent,
//         //         in Animator animator,
//         //         in AudioSource audioSource,
//         //         in EffectsManager effects
//         //         ) =>
//         //     {
//         //         var hasDamage = SystemAPI.HasComponent<DamageComponent>(e);
//         //         var losingDamage = false;
//         //         if (hasDamage)
//         //         {
//         //             losingDamage = SystemAPI.GetComponent<DamageComponent>(e).LosingDamage;
//         //         }
//         //
//         //         if (losingDamage) return;
//         //
//         //         if (hasDamage && deadComponent.isDead == false)
//         //         {
//         //             var damageComponent = SystemAPI.GetComponent<DamageComponent>(e);
//         //             var effectsIndex = damageComponent.EffectsIndex;
//         //             //set in attackersystem by readin visualeffect component index
//         //             if (damageComponent.DamageReceived <= .0001) return;
//         //             Debug.Log("effects index " + effectsIndex);
//         //             animator.SetInteger(HitReact,
//         //                 1); // can easily change to effect index (maybe new field in component ammo and visual effect) if we add more hitreact animations
//         //             if (effects.actorEffect.Count > 0)
//         //             {
//         //                 if (effects.actorEffect != null)
//         //                 {
//         //                     effectsIndex--;
//         //                     if (effects.actorEffect[effectsIndex].psInstance)
//         //                     {
//         //                         effects.actorEffect[effectsIndex].psInstance.Play(true);
//         //                         //Debug.Log("ps dam " + effects.actorEffect[effectsIndex].psInstance);
//         //                     }
//         //
//         //                     if (effects.actorEffect[effectsIndex].veInstance)
//         //                     {
//         //                         effects.actorEffect[effectsComponent.effectIndex].veInstance.Play();
//         //                         Debug.Log("ps dam " + effects.actorEffect[effectsIndex].veInstance);
//         //                     }
//         //
//         //
//         //                     if (effects.actorEffect[effectsIndex].clip)
//         //                     {
//         //                         audioSource.clip = effects.actorEffect[effectsIndex].clip;
//         //                         audioSource.PlayOneShot(audioSource.clip);
//         //                     }
//         //                 }
//         //             }
//         //         }
//         //     }
//         // ).Run();
//     }
// }

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(HealthSystem))]
public partial class CharacterDeadEffectsSystem : SystemBase
{
    private static readonly int Dead = Animator.StringToHash("Dead");


    protected override void OnUpdate()
    {
        // Entities.WithoutBurst().WithNone<Pause>().ForEach(
        //     (
        //         Entity e,
        //         ref DeadComponent deadComponent,
        //         ref EffectComponent effectsComponent,
        //         in AudioSource audioSource,
        //         in Animator animator,
        //         in EffectsManager effects) =>
        //     {
        //         //var audioSource = effects.audioSource;
        //
        //
        //         //int state = animator.GetInteger("Dead");
        //
        //         if (deadComponent.isDead &&
        //             deadComponent.playDeadEffects) //can probably just use playEffectType in effectsComponent TO DO
        //         {
        //             deadComponent.playDeadEffects = false;
        //             var isEnemy = SystemAPI.HasComponent<EnemyComponent>(e);
        //             var isPlayer = SystemAPI.HasComponent<PlayerComponent>(e);
        //             if (isPlayer)
        //                 animator.SetInteger(Dead,
        //                     1); // can easily change to effect index (maybe new field in component ammo and visual effect) if we add more DEAD animations
        //             if (isEnemy) animator.SetInteger(Dead, 2);
        //             var effectsIndex = deadComponent.effectsIndex;
        //             Debug.Log("eff ind play " + effectsIndex);
        //
        //             if (effects.actorEffect != null && effects.actorEffect.Count > 0)
        //             {
        //                 if (effects.actorEffect[effectsIndex].veInstance)
        //                 {
        //                     effects.actorEffect[effectsComponent.effectIndex].veInstance.Play();
        //                     if (effects.actorEffect[effectsIndex].clip)
        //                     {
        //                         //effectsComponent.startEffectSound = false;
        //                         audioSource.clip = effects.actorEffect[effectsIndex].clip;
        //                         if (!audioSource.isPlaying)
        //                         {
        //                             audioSource.PlayOneShot(audioSource.clip, .5f);
        //                             //Log("play audio dead " + audioSource.clip);
        //                         }
        //                     }
        //                 }
        //
        //                 if (effects.actorEffect[effectsIndex]
        //                     .psInstance) //tryinmg to match index to effect type - 1 is dead
        //                 {
        //                     if (effects.actorEffect[effectsIndex].psInstance.isPlaying == false)
        //                     {
        //                         effects.actorEffect[effectsIndex].psInstance.Play(true);
        //                         //Debug.Log("ps dead " + effects.actorEffect[effectsIndex].psInstance);
        //                         if (effects.actorEffect[effectsIndex].clip)
        //                         {
        //                             //effectsComponent.startEffectSound = false;
        //                             audioSource.clip = effects.actorEffect[effectsIndex].clip;
        //                             if (!audioSource.isPlaying)
        //                             {
        //                                 audioSource.PlayOneShot(audioSource.clip, .5f);
        //                                 //Log("play audio dead " + audioSource.clip);
        //                             }
        //                         }
        //                     }
        //                     else if (effectsComponent.playEffectAllowed == false)
        //                     {
        //                         effects.actorEffect[effectsIndex].psInstance.Stop(true);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // ).Run();
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BreakableEffectsSystem))]
[RequireMatchingQueriesForUpdate]
public partial class ParticleInstanceSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);


        Entities.WithoutBurst().ForEach(
            (
                Entity e,
                ref ParticleSystemComponent psComponent,
                in ParticleSystem particleSystem
            ) =>
            {
                if (psComponent.spawnStage == SpawnStage.None)
                {
                    psComponent.spawnStage = SpawnStage.Play;
                    if (particleSystem != null)
                    {
                        Debug.Log("ENTITY SPAWN STAGE " + e + " PS " + particleSystem.isPlaying + " " +
                                  particleSystem);
                    }
                }
                else if (!particleSystem.isPlaying && psComponent.spawnStage == SpawnStage.Play)
                {
                    particleSystem.Play(true);
                    particleSystem.transform.position = psComponent.psLocalTransform.Position;
                    Debug.Log("SPAWN PLAY " + e + " PS " + particleSystem.isPlaying + " " + particleSystem);
                }
                else if (particleSystem.isPlaying && psComponent.spawnStage == SpawnStage.Play)
                {
                    if (SystemAPI.HasComponent<PlayAndDestroyEffectComponent>(psComponent.parentEntity)
                        && particleSystem.time >= .2) //make sure it plays first
                    {
                        psComponent.spawnStage = SpawnStage.End;
                        ecb.AddComponent(e, typeof(DestroyComponent));
                        particleSystem.Stop();
                    }
                }
            }
        ).Run();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}


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