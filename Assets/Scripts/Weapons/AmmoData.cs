using Unity.Entities;
using UnityEngine;

public struct AmmoDataComponent : IComponentData
{

    public float AmmoTime;
    public float Strength;
    public float Damage;
    public float Rate;
    public float GameAmmoTime;
    public float GameStrength;
    public float GameDamage;
    public float GameRate;
    public bool ChargeRequired;
    public float AmmoScale;
    public bool SpawnVisualEffect;
    public Entity Shooter;

}
[System.Serializable]
public class AmmoClass : IComponentData
{
    public Transform ammoStartLocation;
    public Quaternion ammoStartRotation;
    public GameObject primaryAmmoPrefab;
}
public class AmmoStartLocationClass : IComponentData
{
    public Transform AmmoStartLocation;
    public Quaternion AmmoStartRotation;
    public GameObject PrimaryAmmoPrefab;
}


public class AmmoData : MonoBehaviour
{

    [Header("Ammo Ratings")]
    [SerializeField]
    bool randomize;
    [HideInInspector]
    public float AmmoTime;
    public float Strength;
    public float Damage;
    public float Rate;
    public float AmmoScale = .5f;
    public bool ChargeRequired;
    public bool SpawnVisualEffect = true;

    class AmmoDataBaker : Baker<AmmoData>
    {
        public override void Bake(AmmoData authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, new AmmoDataComponent()
                {
                    Strength = authoring.Strength,
                    Damage = authoring.Damage,
                    Rate = authoring.Rate,
                    AmmoTime = authoring.AmmoTime,
                    GameStrength = authoring.Strength,
                    GameDamage = authoring.Damage,
                    GameRate = authoring.Rate,
                    AmmoScale = authoring.AmmoScale,
                    GameAmmoTime = authoring.AmmoTime,
                    ChargeRequired = authoring.ChargeRequired,
                    SpawnVisualEffect = authoring.SpawnVisualEffect
                
                }

            );
        }
    }


}
