using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class PlayerInputAmmoSystem : SystemBase
{
    private static readonly int WeaponRaised = Animator.StringToHash("WeaponRaised");
    private static readonly int AimWeight = Animator.StringToHash("AimWeight");

    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach((
            //Animator animator, 
            Entity e,
            ref WeaponComponent gunComponent, ref ActorWeaponAimComponent playerWeaponAimComponent,
            in InputControllerComponent inputController,
            in ActorInstance actor
            //, in AttachWeaponComponent attachWeapon
        ) =>
        {
            var animator = actor.actorPrefabInstance.GetComponent<Animator>();
            var currentWeaponMotion = (WeaponMotion)animator.GetInteger(WeaponRaised);
            playerWeaponAimComponent.weaponRaised = currentWeaponMotion;
            if (inputController.leftTriggerPressed)
            {
                playerWeaponAimComponent.aimMode = !playerWeaponAimComponent.aimMode;
            }

            var rtPressed = inputController.rightTriggerPressed;
            var aimMode = playerWeaponAimComponent.aimMode;
            if (gunComponent.roleReversal == RoleReversalMode.On)
            {
                aimMode = true;
                playerWeaponAimComponent.aimDisabled = true;
            }

            if (aimMode)
            {
                playerWeaponAimComponent.combatMode = false;
            }


            if (aimMode && playerWeaponAimComponent.weaponRaised == WeaponMotion.None &&
                (
                    //attachWeapon.attachWeaponType == (int) WeaponType.Gun &&
                    rtPressed
                    //|| attachWeapon.attachSecondaryWeaponType == (int) WeaponType.Gun && rtPressed
                )
               )
            {
                gunComponent.IsFiring = 1;
                if (SystemAPI.HasComponent<ScoreComponent>(e))
                {
                    var score = SystemAPI.GetComponent<ScoreComponent>(e);
                    score.startShotValue = score.score;
                    score.zeroPoints = false; //also in ammosystem but thats for normal not GMTK 23
                    SystemAPI.SetComponent(e, score);
                }

                playerWeaponAimComponent.weaponRaised = WeaponMotion.Started;
                SetAnimationLayerWeights(animator, WeaponMotion.Started);
            }

            if (playerWeaponAimComponent.weaponRaised == WeaponMotion.Raised)
            {
                playerWeaponAimComponent.weaponUpTimer += SystemAPI.Time.DeltaTime;
            }
        }).Run();
    }

    private void SetAnimationLayerWeights(Animator animator, WeaponMotion weaponMotion)
    {
        if (weaponMotion == WeaponMotion.Started)
        {
            animator.SetInteger(WeaponRaised, 1);
            animator.SetLayerWeight(0, 0);
            animator.SetLayerWeight(1, 1);
        }
        else if (weaponMotion == WeaponMotion.None)
        {
            animator.SetInteger(WeaponRaised, 0);
            animator.SetLayerWeight(0, 1);
            animator.SetLayerWeight(1, 0);
        }
    }
}