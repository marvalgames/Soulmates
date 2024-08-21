using Unity.Entities;
using UnityEngine;
[RequireMatchingQueriesForUpdate]
public partial class BossAmmoManagerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach(
            (
                Entity e,
                Animator animator,
                ref BossAmmoManagerComponent bulletManagerComponent,
                in BossAmmoManagerGO bossAmmoManager
            ) =>
            {
                //Debug.Log("BOSS AMMO MANAGER");

                var weaponAudioSource = bossAmmoManager.audioSource;
                if (weaponAudioSource && bulletManagerComponent.playSound)
                {
                    var clip = bossAmmoManager.clip;
                    weaponAudioSource.PlayOneShot(clip, .25f);
                    bulletManagerComponent.playSound = false;
                }

                if (bulletManagerComponent.setAnimationLayer)
                {
                    animator.SetLayerWeight(0, 0);
                    bulletManagerComponent.setAnimationLayer = false;
                }
            }
        ).Run();
    }
}
