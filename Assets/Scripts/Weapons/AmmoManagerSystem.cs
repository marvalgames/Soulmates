using Unity.Entities;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
public partial class AmmoManagerSystem : SystemBase
{


    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach(
            (
                 Entity e,
                 AmmoManager ammoManager,
                 Animator animator,
                 ref AmmoManagerComponent ammoManagerComponent
                 ) =>
            {

                if (ammoManager.weaponAudioSource && ammoManagerComponent.playSound)
                {
                    if (!ammoManager.weaponAudioSource.clip) return;
                    //Debug.Log("BULLET " + bulletManagerComponent.playSound);
                    ammoManager.weaponAudioSource.PlayOneShot(ammoManager.weaponAudioSource.clip, 1.0f);
                    ammoManagerComponent.playSound = false;
                }

            }
        ).Run();


    }

}
