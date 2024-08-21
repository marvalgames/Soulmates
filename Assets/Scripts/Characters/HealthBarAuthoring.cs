using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;


public enum ShowText3D
{
    None,
    HitDamage,
    HitScore
}

[System.Serializable]
public struct HealthComponent : IComponentData
{
    public bool combineDamage;
    [FormerlySerializedAs("TotalDamageLanded")] public float totalDamageLanded;
    [FormerlySerializedAs("TotalDamageReceived")] public float totalDamageReceived;
    [FormerlySerializedAs("AlwaysDamage")] public bool alwaysDamage;//ignore hit weights and similar
    [FormerlySerializedAs("ShowDamageMin")] public float showDamageMin;
    [FormerlySerializedAs("ShowDamage")] public bool showDamage;
    [FormerlySerializedAs("ShowText3D")] public ShowText3D showText3D;
    public int count;
    public bool losingHealth;
    public float losingHealthRate;
    public int meleeDamageEffectsIndex;
    //Entity Entity;
}



public struct DamageComponent : IComponentData
{
    public Entity EntityCausingDamage;
    public float DamageLanded;
    public float DamageReceived;
    public float ScorePointsReceived;//to track if hit and points scored by player how many and what enemy
    public float StunLanded;
    public int EffectsIndex;
    public bool LosingDamage;


}
public struct DeflectComponent : IComponentData
{
    public Entity EntityDeflecting;
    public float DeflectLanded;
    public float DeflectReceived;
    public float ScorePointsReceived;//to track if hit and points scored by player how many and what enemy
    public int EffectsIndex;


}


public class HealthBarAuthoring : MonoBehaviour
{
   


    [SerializeField] bool combineDamage = false;
    [SerializeField] private float showDamageMin = 50;
    [SerializeField] private ShowText3D showText3D = ShowText3D.HitDamage;
    [SerializeField]
    bool losingHealth = true;
    [SerializeField]
    float losingHealthRate = 1;
    public int meleeDamageEffectsIndex = 1;    
    public bool alwaysDamaging;


   


    class HealthBaker : Baker<HealthBarAuthoring>
    {
        
        public override void Bake(HealthBarAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e, new HealthComponent
            {
                combineDamage = authoring.combineDamage,
                totalDamageLanded = 0, totalDamageReceived = 0,
                alwaysDamage = authoring.alwaysDamaging,
                showDamageMin = authoring.showDamageMin,
                showText3D = authoring.showText3D,
                losingHealth = authoring.losingHealth,
                losingHealthRate = authoring.losingHealthRate,
                meleeDamageEffectsIndex = authoring.meleeDamageEffectsIndex
            });;
            
            AddComponent(e, new AnimatorWeightsComponent());

        }
    }

    

}
