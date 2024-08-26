using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

[InternalBufferCapacity(8)]
public struct BossWaypointBufferElement : IBufferElementData
{
    public float3 wayPointPosition;
    public float wayPointSpeed;
    public bool wayPointChase;
    public int wayPointAction;
    public int wayPointAnimation;
    public int weaponListIndex;
    public int ammoListIndex;
    public int audioListIndex;
    public float duration; //n/a
}

public struct BossWeaponComponent : IComponentData
{
    public Entity PrimaryAmmo;
    public Entity Weapon;
    public float gameStrength;
    public float gameDamage;
    public float gameRate;
    public float Strength;
    public float Damage;
    public float Rate;
    public float Duration; //rate counter for job
    public bool CanFire;

    public int IsFiring;

    //public LocalToWorld AmmoStartLocalToWorld;
    public LocalTransform AmmoStartTransform;
    public bool Disable;
    public float ChangeAmmoStats;
    public float CurrentWaypointIndex;
}

public struct
    BossAmmoManagerComponent : IComponentData //used for managed components - read and then call methods from MB
{
    public bool playSound;
    public bool setAnimationLayer;
}

public class BossAmmoManagerClass : IComponentData
{
    public GameObject audioSourceGo;
    public GameObject vfxSystem;
    public AudioClip clip;
}



public class BossAmmoManagerAuthoring : MonoBehaviour
{
    public GameObject weaponAudioSource;
    public AudioClip audioClip;
    public GameObject PrimaryAmmoPrefab;
    public List<AmmoClass> AmmoPrefabList = new List<AmmoClass>();

    public AudioClip weaponAudioClip;

    [Header("Ammo Ratings")] public bool randomize;
    float AmmoTime;
    float Strength;
    float Damage;
    float Rate;


    void Generate(bool randomize)
    {
        PrimaryAmmoPrefab = AmmoPrefabList[0].primaryAmmoPrefab;

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
            //Debug.Log("dam " + Damage);
        }
    }

    class BossAmmoBaker : Baker<BossAmmoManagerAuthoring>
    {
        public override void Bake(BossAmmoManagerAuthoring authoring)
        {
            authoring.Generate(authoring.randomize);
            var bossAmmoManager = new BossAmmoManagerClass
            {
                audioSourceGo = authoring.weaponAudioSource,
                clip = authoring.audioClip
            };


            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponentObject(e, bossAmmoManager);
            var buffer = AddBuffer<BossAmmoListBuffer>(e);


            for (var i = 0; i < authoring.AmmoPrefabList.Count; i++)
            {
                var startLocationEntity = GetEntity(authoring.AmmoPrefabList[i].ammoStartLocation, TransformUsageFlags.Dynamic);


                var bossAmmo = new BossAmmoListBuffer
                {
                    E = GetEntity(authoring.AmmoPrefabList[i].primaryAmmoPrefab, TransformUsageFlags.Dynamic),
                    //ammoStartLocalToWorld = ltw,
                    StartLocationEntity = startLocationEntity,
                    AmmoStartTransform = LocalTransform.FromPositionRotation(
                        authoring.AmmoPrefabList[i].ammoStartLocation.position,
                        authoring.AmmoPrefabList[i].ammoStartLocation.rotation
                    )
                };

                buffer.Add(bossAmmo);
            }


            AddComponent(e,
                new BossWeaponComponent
                {
                    //AmmoStartLocalToWorld = localToWorld,
                    AmmoStartTransform = new LocalTransform
                    {
                        Position = authoring.AmmoPrefabList[0].ammoStartLocation.position,
                        Rotation = authoring.AmmoPrefabList[0].ammoStartLocation.rotation
                    },
                    PrimaryAmmo = GetEntity(authoring.PrimaryAmmoPrefab, TransformUsageFlags.Dynamic),
                    Strength = authoring.Strength,
                    gameStrength = authoring.Strength,
                    Damage = authoring.Damage,
                    Rate = authoring.Rate,
                    gameDamage = authoring.Damage,
                    gameRate = authoring.Rate,
                    CanFire = true,
                    IsFiring = 0
                });

            AddComponent(e,typeof(BossAmmoManagerComponent));
        }
    }
}