using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RoleReversalMode
{
    Off,
    On,
    Toggle
}

public enum FiringStage
{
    None,
    Start,
    Update,
    End
}

public struct WeaponComponent : IComponentData
{
    public RoleReversalMode roleReversal;
    public Entity PrimaryAmmo;
    public Entity SecondaryAmmo;
    public Entity Weapon;
    //public float AmmoTime;
    public float gameStrength;
    public float gameDamage;
    public float gameRate;

    public float Strength;
    public float Damage;
    public float Rate;
    public float Duration;//rate counter for job
    //public bool CanFire;
    public int IsFiring;
    public FiringStage firingStage;
    public LocalToWorld AmmoStartLocalToWorld;
    public LocalTransform AmmoStartTransform;
    //public Rotation AmmoStartRotation;
    public bool Disable;
    public float ChangeAmmoStats;
    public float animTriggerWeight;

    public float roleReversalRangeMechanic;
    public bool tooFarTooAttack;
    //public LocalTransform firingPosition;
}

public struct AmmoManagerComponent : IComponentData //used for managed components - read and then call methods from MB
{
    public bool playSound;
    public bool setAnimationLayer;
}



public class AmmoManagerAuthoring : MonoBehaviour
{
    public GameObject PrimaryAmmoPrefab;
    public GameObject SecondaryAmmoPrefab;
    public bool aimMode;
    public CameraTypes weaponCamera;
    public float animTriggerWeight = .7f;
    public RoleReversalMode roleReversal = RoleReversalMode.On;
    public float roleReversalRangeMechanic;


    [Header("Read Only Ammo Ratings from Prefab")]
    [SerializeField]
    bool randomize;
    //float AmmoTime;
    [SerializeField]
    float Strength;
    [SerializeField]
    float Damage;
    [SerializeField]
    float Rate;

    //[Header("Misc")]
    //public bool Disable;


    void Generate(bool randomize)
    {
        if (randomize)
        {
            var multiplier = .7f;
            Strength = Random.Range(Strength * multiplier, Strength * (2 - multiplier));
            Damage = Random.Range(Damage * multiplier, Damage * (2 - multiplier));
            Rate = Random.Range(Rate * multiplier, Rate * (2 - multiplier));
        }
        else
        {
            Strength = PrimaryAmmoPrefab.GetComponent<AmmoData>().Strength;
            Damage = PrimaryAmmoPrefab.GetComponent<AmmoData>().Damage;
            Rate = PrimaryAmmoPrefab.GetComponent<AmmoData>().Rate;
        }


    }

    class BulletManagerBaker : Baker<AmmoManagerAuthoring>
    {
        public override void Bake(AmmoManagerAuthoring authoring)
        {
            authoring.Generate(authoring.randomize);


            var localToWorld = new LocalToWorld
            {
                //Value = float4x4.TRS(authoring.transform.position, authoring.transform.rotation, Vector3.one)
            };


            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e, 
                new WeaponComponent
                {
                    AmmoStartLocalToWorld = localToWorld,
                    //AmmoStartPosition = new LocalTransform() { Value = authoring.AmmoStartLocation.position },//not used because cant track bone 
                    //AmmoStartRotation = new Rotation() { Value = authoring.AmmoStartLocation.rotation },
                    PrimaryAmmo = GetEntity(authoring.PrimaryAmmoPrefab, TransformUsageFlags.Dynamic),
                    SecondaryAmmo = GetEntity(authoring.SecondaryAmmoPrefab, TransformUsageFlags.Dynamic),
                    Strength = authoring.Strength,
                    gameStrength = authoring.Strength,
                    Damage = authoring.Damage,
                    Rate = authoring.Rate,
                    gameDamage = authoring.Damage,
                    gameRate = authoring.Rate,
                    IsFiring = 0,
                    animTriggerWeight = authoring.animTriggerWeight,
                    roleReversal = authoring.roleReversal,
                    roleReversalRangeMechanic = authoring.roleReversalRangeMechanic

                });
            
            AddComponent(e, 
                new ActorWeaponAimComponent
                {
                    aimMode = authoring.aimMode,
                    weaponCamera = authoring.weaponCamera,
                    crosshairRaycastTarget =
                        new float3 { x = authoring.transform.position.x, y = authoring.transform.position.y, z = authoring.transform.position.z }
                });

            AddComponent(e, new AmmoManagerComponent());

        }
    }


}


