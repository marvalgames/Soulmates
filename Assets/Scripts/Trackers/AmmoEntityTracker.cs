using Unity.Entities;
using UnityEngine;


public struct AmmoComponent : IComponentData
{
    public bool playerOwner;
    public int effectIndex;
    public int deathBlowEffectsIndex;
    public Entity OwnerAmmoEntity;
    public Entity ammoEntity;
    public bool AmmoDead;
    public float AmmoTime;
    public float AmmoTimeCounter;
    public bool DamageCausedPreviously;
    public int framesToSkip;
    public int frameSkipCounter;
    public bool bulletSpotted;
    public bool Charged;
    public bool rewinding;
    public int ammoHits;//count how many hits this ammo connected before ammoTime is zero (entity destroyed by ammo system)
    public float comboTimeAdd;
}

public class AmmoEntityTracker : MonoBehaviour
{
    public int effectIndex;
    public int deathBlowEffectsIndex;
    public float ammoTime; //????? 
    
    public bool playerOwner;
    [SerializeField] private float comboTimeAdd = 1.0f;
    [SerializeField]int framesToSkip = 2;

    class AmmoEntityTrackerBaker : Baker<AmmoEntityTracker>
    {
        public override void Bake(AmmoEntityTracker authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e,
                new AmmoComponent
                {
                    playerOwner = authoring.playerOwner,
                    effectIndex = authoring.effectIndex, deathBlowEffectsIndex = authoring.deathBlowEffectsIndex,
                    AmmoDead = false,
                    AmmoTimeCounter = 0,
                    AmmoTime = authoring.ammoTime,
                    comboTimeAdd = authoring.comboTimeAdd,
                    Charged = false,
                    ammoEntity = e,
                    framesToSkip = authoring.framesToSkip
                });
            
        }
    }
   
}
