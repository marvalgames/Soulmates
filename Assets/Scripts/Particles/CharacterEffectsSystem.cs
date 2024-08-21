using Collisions;
using Sandbox.Player;
using Unity.Entities;
//using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using UnityEngine.AI;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine.VFX;

public enum SpawnStage
{
    None,
    Start,
    Play,
    End
}

public enum EffectType
{
    None,
    Damaged,
    Dead,
    TwoClose,
}

[System.Serializable]
public class EffectClass : IComponentData
{
    public EffectType effectType;
    public ParticleSystem psPrefab;
    public ParticleSystem psInstance;
    public VisualEffect vePrefab;
    public VisualEffect veInstance;
    public Entity psEntity;
    public AudioClip clip;
    public GameObject transformGameObject;
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
public partial class CharacterImpulseEffectsSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        //timer += SystemAPI.Time.DeltaTime;

        // var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer();
        var ecb = new EntityCommandBuffer(Allocator.Temp);


        var impulseGroup = GetComponentLookup<ImpulseComponent>();


        //player causes damage triggers impulse
        Entities.WithoutBurst().ForEach(
            (
                Entity e,
                Animator anim,
                ref PhysicsVelocity physicsVelocity,
                in Impulse impulse
            
            ) =>
            {
                if (!SystemAPI.HasComponent<ImpulseComponent>(e)) return;


                //anim null check later
                if (anim != null)
                {
                    var impulseComponent = impulseGroup[e];


                    float damageLanded = 0;
                    float damageReceived = 0;
                    if (impulseComponent.maxTime <= 0) return;
                    bool hasDamageComponent = SystemAPI.HasComponent<DamageComponent>(e);
                    bool hasNavMesh = anim.gameObject.GetComponent<NavMeshAgent>();

                    if (hasDamageComponent)
                    {
                        var damageComponent = SystemAPI.GetComponent<DamageComponent>(e);
                        damageLanded = damageComponent.DamageLanded;
                        damageReceived = damageComponent.DamageReceived;
                        if (damageLanded > 0 && impulseComponent.activate == false)
                        {
                            impulse.impulseSourceHitLanded.GenerateImpulse();
                            impulseComponent.activate = true;
                            ecb.AddComponent<Pause>(e);
                            anim.speed = impulseComponent.animSpeedRatio;
                            if (hasNavMesh) anim.gameObject.GetComponent<NavMeshAgent>().speed = anim.speed;
                            physicsVelocity.Linear =
                                physicsVelocity.Linear * math.float3(anim.speed, anim.speed, anim.speed);
                        }
                        else if (damageReceived > 0 && impulseComponent.activateOnReceived == false)
                        {
                            impulse.impulseSourceHitReceived.GenerateImpulse();
                            impulseComponent.activateOnReceived = true;
                            ecb.AddComponent<Pause>(e);
                            anim.speed = impulseComponent.animSpeedRatioOnReceived;
                            if (hasNavMesh) anim.gameObject.GetComponent<NavMeshAgent>().speed = anim.speed;
                            physicsVelocity.Linear =
                                physicsVelocity.Linear * math.float3(anim.speed, anim.speed, anim.speed);
                        }
                    }
                    else if (impulseComponent.activate == true && impulseComponent.timer <= impulseComponent.maxTime)
                    {
                        impulseComponent.timer += SystemAPI.Time.DeltaTime;
                        physicsVelocity.Linear =
                            physicsVelocity.Linear * math.float3(anim.speed, anim.speed, anim.speed);
                        //Debug.Log("timer " + impulseComponent.timer);
                        if (impulseComponent.timer >= impulseComponent.maxTime)
                        {
                            impulseComponent.timer = 0;
                            impulseComponent.activate = false;
                            anim.speed = 1;
                            if (hasNavMesh) anim.gameObject.GetComponent<NavMeshAgent>().speed = anim.speed;
                            ecb.RemoveComponent<Pause>(e);
                        }
                    }
                    else if (impulseComponent.activateOnReceived == true &&
                             impulseComponent.timerOnReceived <= impulseComponent.maxTimeOnReceived)
                    {
                        impulseComponent.timerOnReceived += SystemAPI.Time.DeltaTime;
                        physicsVelocity.Linear =
                            physicsVelocity.Linear * math.float3(anim.speed, anim.speed, anim.speed);
                        //Debug.Log("timer " + impulseComponent.timer);
                        if (impulseComponent.timerOnReceived >= impulseComponent.maxTimeOnReceived)
                        {
                            impulseComponent.timerOnReceived = 0;
                            impulseComponent.activateOnReceived = false;
                            anim.speed = 1;
                            if (hasNavMesh) anim.gameObject.GetComponent<NavMeshAgent>().speed = anim.speed;
                            ecb.RemoveComponent<Pause>(e);
                        }
                    }


                    SystemAPI.SetComponent(e, impulseComponent);
                }
            }
        ).Run();


        Entities.WithoutBurst().WithNone<Pause>().ForEach(
            (
                Entity e,
                ref DeadComponent deadComponent,
                ref EffectsComponent effectsComponent,
                in Animator animator, 
                in EffectsManager effects) =>
            {
                var audioSource = effects.audioSource;
                if (effectsComponent.playEffectType == EffectType.TwoClose)
                {
                    //var effectsIndex = effectsComponent.effectIndex;
                    if (effects.actorEffect != null)
                    {
                        if (effects.actorEffect[effectsComponent.effectIndex]
                            .psInstance) //tryinmg to match index to effect type - 3 is 2 close
                        {
                            if (effects.actorEffect[effectsComponent.effectIndex].psInstance.isPlaying == false &&
                                effectsComponent.playEffectAllowed)
                            {
                                effects.actorEffect[effectsComponent.effectIndex].psInstance.Play(true);
                                if (effects.actorEffect[effectsComponent.effectIndex].clip)
                                {
                                    //effectsComponent.startEffectSound = false;
                                    audioSource.clip = effects.actorEffect[effectsComponent.effectIndex].clip;
                                    if (!audioSource.isPlaying)
                                        audioSource.PlayOneShot(audioSource.clip, .5f);
                                    //Debug.Log("play audio " + audioSource.clip);
                                }
                            }
                            else if (effectsComponent.playEffectAllowed == false)
                            {
                                effects.actorEffect[effectsComponent.effectIndex].psInstance.Stop(true);
                            }
                        }
                    }
                }
            }
        ).Run();


        Entities.WithoutBurst().WithNone<Pause>().ForEach(
            (
                in Entity e,
                in SlashComponent slashComponent,
                in SlashClass slashComponentAuthoring) =>
            {
                var audioSource = slashComponentAuthoring.audioSource;
                if(audioSource == null) return;
                if (!slashComponentAuthoring.audioSource.clip || !slashComponentAuthoring.audioSource) return;
                if (slashComponent.slashState == (int)SlashStates.Started)
                {
                    audioSource.clip = slashComponentAuthoring.audioSource.clip;
                    audioSource.PlayOneShot(audioSource.clip);
                }
            }
        ).Run();


        Entities.WithoutBurst().ForEach
        (
            (ref EffectsComponent effectsComponent,
                in LevelCompleteComponent goal, in Entity entity, in EffectsManager effects,
                in AudioSource audioSource) =>
            {
                if (goal.active == true || effectsComponent.soundPlaying == true) return;
            }
        ).Run();
    }
}


[UpdateAfter(typeof(HealthSystem))]
[UpdateAfter(typeof(Collisions.AttackerSystem))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class CharacterDamageEffectsSystem : SystemBase
{
    private static readonly int HitReact = Animator.StringToHash("HitReact");

    protected override void OnUpdate()
    {
        Entities.WithoutBurst().WithNone<Pause>().ForEach(
            (
                Entity e,
                ref DeadComponent deadComponent,
                ref EffectsComponent effectsComponent,
                in Animator animator,
                in AudioSource audioSource,
                in EffectsManager effects) =>
            {
                var hasDamage = SystemAPI.HasComponent<DamageComponent>(e);
                var losingDamage = false;
                if (hasDamage)
                {
                    losingDamage = SystemAPI.GetComponent<DamageComponent>(e).LosingDamage;
                }

                if (losingDamage) return;

                if (hasDamage == true && deadComponent.isDead == false)
                {
                    var damageComponent = SystemAPI.GetComponent<DamageComponent>(e);
                    var effectsIndex = damageComponent.EffectsIndex;
                    //set in attackersystem by readin visualeffect component index
                    if (damageComponent.DamageReceived <= .0001) return;
                    Debug.Log("effects index " + effectsIndex);
                    animator.SetInteger(HitReact,
                        1); // can easily change to effect index (maybe new field in component ammo and visual effect) if we add more hitreact animations
                    if (effects.actorEffect.Count > 0 )
                    {
                        if (effects.actorEffect != null)
                        {
                            effectsIndex--;
                            if (effects.actorEffect[effectsIndex].psInstance )
                            {
                                effects.actorEffect[effectsIndex].psInstance.Play(true);
                                //Debug.Log("ps dam " + effects.actorEffect[effectsIndex].psInstance);
                            }

                            if (effects.actorEffect[effectsIndex].veInstance)
                            {
                                effects.actorEffect[effectsComponent.effectIndex].veInstance.Play(); 
                                Debug.Log("ps dam " + effects.actorEffect[effectsIndex].veInstance);
                                
                            }


                            if (effects.actorEffect[effectsIndex].clip)
                            {
                                audioSource.clip = effects.actorEffect[effectsIndex].clip;
                                audioSource.PlayOneShot(audioSource.clip);
                            }
                        }
                    }
                }
            }
        ).Run();
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(HealthSystem))]
public partial class CharacterDeadEffectsSystem : SystemBase
{
    private static readonly int Dead = Animator.StringToHash("Dead");


    protected override void OnUpdate()
    {
        Entities.WithoutBurst().WithNone<Pause>().ForEach(
            (
                Entity e,
                ref DeadComponent deadComponent,
                ref EffectsComponent effectsComponent,
                in AudioSource audioSource,
                in Animator animator,
                in EffectsManager effects) =>
            {
                //var audioSource = effects.audioSource;


                //int state = animator.GetInteger("Dead");

                if (deadComponent.isDead &&
                    deadComponent.playDeadEffects) //can probably just use playEffectType in effectsComponent TO DO
                {
                    deadComponent.playDeadEffects = false;
                    var isEnemy = SystemAPI.HasComponent<EnemyComponent>(e);
                    var isPlayer = SystemAPI.HasComponent<PlayerComponent>(e);
                    if (isPlayer)
                        animator.SetInteger(Dead,
                            1); // can easily change to effect index (maybe new field in component ammo and visual effect) if we add more DEAD animations
                    if (isEnemy) animator.SetInteger(Dead, 2);
                    var effectsIndex = deadComponent.effectsIndex;
                    Debug.Log("eff ind play " + effectsIndex);

                    if (effects.actorEffect != null && effects.actorEffect.Count > 0)
                    {
                        
                        if (effects.actorEffect[effectsIndex].veInstance)
                        {
                            effects.actorEffect[effectsComponent.effectIndex].veInstance.Play();
                            if (effects.actorEffect[effectsIndex].clip)
                            {
                                //effectsComponent.startEffectSound = false;
                                audioSource.clip = effects.actorEffect[effectsIndex].clip;
                                if (!audioSource.isPlaying)
                                {
                                    audioSource.PlayOneShot(audioSource.clip, .5f);
                                    //Log("play audio dead " + audioSource.clip);
                                }
                            }
                        }
                        
                        if (effects.actorEffect[effectsIndex]
                            .psInstance) //tryinmg to match index to effect type - 1 is dead
                        {
                            if (effects.actorEffect[effectsIndex].psInstance.isPlaying == false)
                            {
                                effects.actorEffect[effectsIndex].psInstance.Play(true);
                                //Debug.Log("ps dead " + effects.actorEffect[effectsIndex].psInstance);
                                if (effects.actorEffect[effectsIndex].clip)
                                {
                                    //effectsComponent.startEffectSound = false;
                                    audioSource.clip = effects.actorEffect[effectsIndex].clip;
                                    if (!audioSource.isPlaying)
                                    {
                                        audioSource.PlayOneShot(audioSource.clip, .5f);
                                        //Log("play audio dead " + audioSource.clip);
                                    }
                                }
                            }
                            else if (effectsComponent.playEffectAllowed == false)
                            {
                                effects.actorEffect[effectsIndex].psInstance.Stop(true);
                            }
                        }
                    }
                }
            }
        ).Run();
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
                        Debug.Log("ENTITY SPAWN STAGE " + e + " PS " + particleSystem.isPlaying + " " + particleSystem);
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
[UpdateAfter(typeof(Collisions.BreakableCollisionHandlerSystem))]
public partial class BreakableEffectsSystem : SystemBase
{

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        Entities.WithoutBurst().ForEach(
            (
                Entity e,
                AudioSource audioSource,
                EffectClass effect,
                ref PlayAndDestroyEffectComponent playAndDestroyEffect
            ) =>
            {
                if (playAndDestroyEffect.play && effect.psEntity != Entity.Null && effect.transformGameObject != null)
                {
                    var tr = LocalTransform.FromPosition(effect.transformGameObject.transform.position );
                    var psTag = new ParticleSystemComponent
                    {
                        psLocalTransform = tr,
                        parentEntity = e
                    };

                    playAndDestroyEffect.play = false;
                    if (effect.clip)
                    {
                        if (!audioSource.isPlaying)
                        {
                            audioSource.PlayOneShot(effect.clip, 1.0f);
                        }
                    }

                    Entity ps = ecb.Instantiate(effect.psEntity);
                    ecb.AddComponent(ps, psTag);
                    ecb.SetComponent(ps, tr);
                }
            }
        ).Run();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}