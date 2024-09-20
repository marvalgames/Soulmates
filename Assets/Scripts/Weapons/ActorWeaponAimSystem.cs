// using Player;
// using Rewired;
// using Sandbox.Player;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
//
//
// public partial class PlayerWeaponAimSystemLateUpdate : SystemBase
// {
//     protected override void OnUpdate()
//     {
//         Entities.WithoutBurst().WithAny<DeadComponent>().WithNone<Pause>().ForEach((
//             ref PlayerAimComponent playerAimComponent,
//             ref LocalTransform localTransform) =>
//         {
//             //if (mb.Player.controllers.GetLastActiveController() == null) return;
//             //playerWeaponAimComponent.aimDirection = mb.aimDir;
//             Debug.Log("Rotate " + playerAimComponent.aimDirection);
//
//             var direction = math.normalize(playerAimComponent.aimDirection);
//             direction.y = 0;
//             var targetRotation = quaternion.LookRotationSafe(direction, math.up()); //always face xHair
//             localTransform.Rotation = targetRotation;
//         }).Run();
//     }
// }


using Player;
using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
//
// [UpdateInGroup(typeof(TransformSystemGroup))]
// [RequireMatchingQueriesForUpdate]
// public partial class EnemyWeaponAimSystemLateUpdate : SystemBase
// {
//     protected override void OnUpdate()
//     {
//         Entities.WithoutBurst().WithAny<DeadComponent>()
//             .ForEach((in EnemyWeaponAim mb, in ActorWeaponAimComponent actorWeaponAimComponent) =>
//             {
//                 mb.weaponRaised = actorWeaponAimComponent.weaponRaised == WeaponMotion.Started;
//                 mb.LateUpdateSystem();
//             }).Run();
//     }
// }

public partial class PlayerWeaponAimSystemLateUpdate : SystemBase
{
    private static readonly int Turning = Animator.StringToHash("Turning");

    protected override void OnUpdate()
    {
        Entities.WithoutBurst().WithAny<DeadComponent>().WithNone<Pause>().ForEach((
            ActorInstance actor,
            ref ActorWeaponAimComponent playerWeaponAimComponent,
            ref LocalTransform localTransform, ref PlayerAimComponent playerAimComponent) =>
        {
            playerWeaponAimComponent.aimMode = true;
            var animator = actor.actorPrefabInstance.GetComponent<Animator>();
            if (playerWeaponAimComponent.combatMode) return;
            var aimDir = playerAimComponent.aimDirection;
            playerWeaponAimComponent.aimDirection = aimDir;
            var direction = math.normalize(aimDir);
            direction.y = 0;

            var forwardVector = math.forward(localTransform.Rotation);
            forwardVector.y = 0;
            var degrees = Vector3.SignedAngle(forwardVector, direction, Vector3.up);
            var turnSpeed = 1;
            if (math.abs(degrees - playerWeaponAimComponent.angleToTarget) < .03)
            {
                degrees = 0;
            }

            playerWeaponAimComponent.angleToTarget = degrees;
            var turningValue = math.sign(degrees);
            var slerpDampTime = 15;
            //var slerpDampTime = mb.rotateSpeed;
            if (playerWeaponAimComponent.aimMode == false)
            {
                turningValue = 0;
                slerpDampTime = 0;
            }

            var crosshairAbovePlayer =
                (playerWeaponAimComponent.mousePosition.y - playerWeaponAimComponent.screenPosition.y) > 0;

            aimDir = playerAimComponent.aimDirection;

            playerAimComponent.aimDirection = aimDir;

            direction = math.normalize(aimDir);
            //direction = math.normalize(playerAimComponent.crosshairDirection);
            direction.y = 0;
            //direction = mb.playerToMouseDir;


            var targetRotation = quaternion.LookRotationSafe(direction, math.up());
            //always face xHair
            localTransform.Rotation = math.slerp(localTransform.Rotation, targetRotation.value,
                slerpDampTime * SystemAPI.Time.DeltaTime);
            //localTransform.Rotation = targetRotation;
            //animator.SetFloat(Turning, turningValue, turnSpeed, SystemAPI.Time.DeltaTime);
        }).Run();
    }
}