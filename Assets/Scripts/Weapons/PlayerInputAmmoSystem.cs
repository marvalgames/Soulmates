using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class PlayerInputAmmoSystem : SystemBase
{
    private static readonly int WeaponRaised = Animator.StringToHash("WeaponRaised");

    protected override void OnUpdate()
    {
        //var check = new NativeArray<int>(1, Allocator.TempJob);
        Entities.WithoutBurst().ForEach((
            Animator animator, Entity e,
            ref WeaponComponent gunComponent, ref ActorWeaponAimComponent playerWeaponAimComponent,
            in InputControllerComponent inputController, in AttachWeaponComponent attachWeapon) =>
        {
            //lt mapped to 1 on keyboard when LT is not used for shooting - if not map to left mouse
            var dpadY = inputController.dpadY;
            var currentWeaponMotion = (WeaponMotion) animator.GetInteger(WeaponRaised);
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
            
            // if ( 
            //(
            //attachWeapon.attachWeaponType == (int)WeaponType.Gun && rtPressed == true ||
            // attachWeapon.attachSecondaryWeaponType == (int)WeaponType.Gun && rtPressed == true))
            if (aimMode &&
                (attachWeapon.attachWeaponType == (int) WeaponType.Gun && rtPressed == true ||
                 attachWeapon.attachSecondaryWeaponType == (int) WeaponType.Gun && rtPressed == true))
            {
                gunComponent.IsFiring = 1;
                playerWeaponAimComponent.weaponUpTimer = 0;
                if (SystemAPI.HasComponent<ScoreComponent>(e))
                {
                    var score = SystemAPI.GetComponent<ScoreComponent>(e);
                    score.startShotValue = score.score;
                    score.zeroPoints = false; //also in ammosystem but thats for normal not GMTK 23
                    SystemAPI.SetComponent(e, score);
                }

                playerWeaponAimComponent.weaponRaised = WeaponMotion.Raised;
                SetAnimationLayerWeights(animator, WeaponMotion.Raised);
            }

            if (playerWeaponAimComponent.weaponRaised == WeaponMotion.Lowering)
            {
                playerWeaponAimComponent.weaponUpTimer += SystemAPI.Time.DeltaTime;
                if (playerWeaponAimComponent.weaponUpTimer > 2)
                {
                    playerWeaponAimComponent.weaponUpTimer = 0;
                    playerWeaponAimComponent.weaponRaised = WeaponMotion.None;
                    SetAnimationLayerWeights(animator, WeaponMotion.None);
                }
            }
        }).Run();
    }

    public void SetAnimationLayerWeights(Animator animator, WeaponMotion weaponMotion)
    {
        if (weaponMotion == WeaponMotion.Raised)
        {
            //animator.SetInteger("WeaponRaised", 1);
            animator.SetInteger(WeaponRaised, 2);
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