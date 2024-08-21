using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(ParentSystem))]
[RequireMatchingQueriesForUpdate]
partial class SynchronizeGameObjectTransformsBossAmmoWeaponEntities : SystemBase
{

    protected override void OnCreate()
    {
        base.OnCreate();

    }

    protected override void OnUpdate()
    {
        var ammoList = GetBufferLookup<BossAmmoListBuffer>(true);
        var positionBuffer = GetBufferLookup<BossWaypointBufferElement>(true);


        Entities.WithBurst().ForEach(
            (Entity enemyE,
                //BossAmmoManager bulletManager,
                ref BossWeaponComponent bossWeaponComponent,
                in BossMovementComponent bossMovementComponent
            ) =>
            {
                var targetPointBuffer = positionBuffer[enemyE];
                var ammoListBuffer = ammoList[enemyE];
                if (ammoListBuffer.Length <= 0 || bossMovementComponent.CurrentIndex < 0) return;

                var ammoIndex = targetPointBuffer[bossMovementComponent.CurrentIndex].ammoListIndex;

                if (ammoIndex < 0) return;

                var ammoE = ammoListBuffer[ammoIndex].E;
                var startLocationE = ammoListBuffer[ammoIndex].StartLocationEntity;
                bossWeaponComponent.PrimaryAmmo = ammoE;

                bossWeaponComponent.AmmoStartTransform.Position =
                    SystemAPI.GetComponent<LocalTransform>(startLocationE).Position;
                bossWeaponComponent.AmmoStartTransform.Rotation =
                    SystemAPI.GetComponent<LocalTransform>(startLocationE).Rotation;
            }
        ).Schedule();

    }
}



[UpdateInGroup(typeof(TransformSystemGroup))]
[RequireMatchingQueriesForUpdate]
partial class SynchronizeGameObjectTransformsGunEntities : SystemBase
{

    protected override void OnUpdate()
    {
        
        Entities.WithoutBurst().ForEach(
            (AmmoManager ammoManager, ref WeaponComponent weaponComponent) =>
            {
                var position = ammoManager.AmmoStartLocation.position;
                var rotation = ammoManager.AmmoStartLocation.rotation;
                var localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(position, rotation, Vector3.one)
                };

                weaponComponent.AmmoStartLocalToWorld = localToWorld;
                weaponComponent.AmmoStartTransform.Position = position;
                weaponComponent.AmmoStartTransform.Rotation = rotation;
                //Debug.Log("AMMO START");
            }
        ).Run();

    }
}




