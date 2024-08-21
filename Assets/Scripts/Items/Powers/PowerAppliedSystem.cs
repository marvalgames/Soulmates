using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
public partial class PowerAppliedSystem : SystemBase
{


    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        var dep0 = Entities.ForEach((in PickupSystemComponent ps, in Entity e) =>
            {
                if (!SystemAPI.HasComponent<LocalTransform>(ps.pickedUpActor)) return;
                var value = SystemAPI.GetComponent<LocalTransform>(ps.pickedUpActor).Position;


                if (ps.followActor == false)
                {
                    ecb.DestroyEntity(e);
                    return;
                }

                var localTransform = LocalTransform.FromPosition(value);
                SystemAPI.SetComponent(e, localTransform);

            }
        ).Schedule(Dependency);
        dep0.Complete();

        var dep1 = Entities.ForEach(
            (
                    ref Speed speed, ref RatingsComponent ratings,
                        in Entity e


                ) =>
            {

                if (speed.startTimer == false && speed.enabled == true)
                {
                    speed.triggered = true;
                    speed.startTimer = true;
                    speed.timer = 0;
                    ratings.gameSpeed = ratings.gameSpeed * speed.multiplier;
                }
                else if (speed.enabled && speed.timer < speed.timeOn)
                {
                    speed.triggered = false;
                    speed.timer += SystemAPI.Time.DeltaTime;
                }
                else if (speed.enabled)
                {
                    speed.startTimer = false;
                    speed.timer = 0;
                    speed.enabled = false;
                    ratings.gameSpeed = ratings.speed;
                    ecb.DestroyEntity(speed.psAttached);
                    ecb.RemoveComponent<Speed>(e);

                }

            }
        ).Schedule(Dependency);
        dep1.Complete();



        var dep2 = Entities.ForEach(
            (
                ref HealthPower healthPower, ref HealthComponent healthComponent, in RatingsComponent ratings, in Entity e

            ) =>
            {
                if (healthPower.enabled == true)
                {
                    //healthPower.enabled = false;
                    healthComponent.totalDamageReceived = healthComponent.totalDamageReceived * healthPower.healthMultiplier;
                    if (healthPower.slowDown)
                    {
                        healthComponent.losingHealthRate *= .8f;
                    }
                    //Rare used if multiplier is > 1 meaning health damage increased
                    if (healthComponent.totalDamageReceived > ratings.maxHealth)
                    {
                        healthComponent.totalDamageReceived = ratings.maxHealth;
                    }
                    ecb.RemoveComponent<HealthPower>(e);
                    ecb.DestroyEntity(healthPower.psAttached);

                }

            }
        ).Schedule(Dependency);
        dep2.Complete();


        var dep3 =  Entities.ForEach(
        (
           ref DashPower power, in Entity e

        ) =>
            {
                if (power.enabled)//just destroy powerup ?
                {
                    ecb.RemoveComponent<DashPower>(e);
                    ecb.DestroyEntity(power.psAttached);
                }
            }
        ).Schedule(Dependency);

        dep3.Complete();



        
        Entities.WithoutBurst().ForEach(
            (
                HealthBar healthBar, ref HealthPower healthPower) =>
            {
                if (healthPower.enabled == true)
                {
                    healthPower.enabled = false;
                    healthBar.HealthChange();
                }
            }
        ).Run();
        
        
        
        
        Entities.WithoutBurst().WithAll<AudioSourceComponent>().ForEach(
            (
                AudioSource audioSource, ref PowerItemComponent powerItemComponent, in Entity e) =>
            {
                if (audioSource.isPlaying == false
                    && powerItemComponent.enabled == true
                )
                {
                    powerItemComponent.enabled = false;
                }
            }
        ).Run();
        


        ecb.Playback(EntityManager);
        ecb.Dispose();



    }




}

