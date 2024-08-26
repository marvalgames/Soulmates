using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(TransformSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class EnemyWeaponAimSystemLateUpdate : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithoutBurst().WithAny<DeadComponent>()
            .ForEach((in EnemyWeaponAim mb, in ActorWeaponAimComponent actorWeaponAimComponent) =>
            {
                mb.weaponRaised = actorWeaponAimComponent.weaponRaised == WeaponMotion.Started;
                mb.LateUpdateSystem();
            }).Run();
    }
}

public partial class PlayerWeaponAimSystemLateUpdate : SystemBase
{
    private static readonly int Turning = Animator.StringToHash("Turning");

    protected override void OnUpdate()
    {
        Entities.WithoutBurst().WithAny<DeadComponent>().WithNone<Pause>().ForEach((
            PlayerWeaponAim mb, ref ActorWeaponAimComponent playerWeaponAimComponent,
            ref LocalTransform localTransform) =>
        {
            if (mb.Player.controllers.GetLastActiveController() == null || playerWeaponAimComponent.combatMode) return;
            mb.LateUpdateSystem(playerWeaponAimComponent.weaponRaised);
            playerWeaponAimComponent.aimDirection = mb.aimDir;
            var direction = math.normalize(mb.aimDir);
            direction.y = 0;


            var forwardVector = math.forward(localTransform.Rotation);
            forwardVector.y = 0;
            var degrees = Vector3.SignedAngle(forwardVector, direction, Vector3.up);
            var turnSpeed = mb.turnSpeed;

            if (math.abs(degrees - playerWeaponAimComponent.angleToTarget) < .03)
            {
                degrees = 0;
            }

            playerWeaponAimComponent.angleToTarget = degrees;
            var turningValue = math.sign(degrees);
            var slerpDampTime = mb.rotateSpeed;


            if (playerWeaponAimComponent.aimMode == false)
            {
                turningValue = 0;
                slerpDampTime = 0;
            }

            var targetRotation = quaternion.LookRotationSafe(direction, math.up()); //always face xHair
            localTransform.Rotation = math.slerp(localTransform.Rotation, targetRotation.value,
                slerpDampTime * SystemAPI.Time.DeltaTime);
            mb.animator.SetFloat(Turning, turningValue, turnSpeed, SystemAPI.Time.DeltaTime);
        }).Run();
    }
}
