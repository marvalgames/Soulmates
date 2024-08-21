
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using UnityEngine;



public struct UseItem1 : IComponentData
{
    public float Value;
}
public struct UseItem2 : IComponentData
{
    public float Value;

}

//public struct ItemStatsList : IBufferElementData
//{
//    public Entity e;
//    public FixedString64Bytes statDescription;
//    public FixedString64Bytes statDescriptionLong;
//    public float statRating;//can convert as needed

//}


public struct PowerItemComponent : IComponentData
{
    public Entity pickedUpActor;
    public Entity pickupEntity;
    public bool active;
    public bool enabled;
    public Entity particleSystemEntity;
    public bool particleSystemEntitySpawned;
    public Entity particleSystemInstance;
    public Entity addPickupEntityToInventory;
    public bool itemPickedUp;
    public FixedString64Bytes description;
    public FixedString64Bytes longDescription;
    public bool buttonAssigned;
    public bool useSlot1;
    public bool useSlot2;
    public int index;
    public PickupType pickupType;
    public int count;
    public int menuIndex;

    public int statCount;
    public FixedString64Bytes statDescription1;
    public FixedString64Bytes statDescriptionLong1;
    public float statRating1;//can convert as needed
    public FixedString64Bytes statDescription2;
    public FixedString64Bytes statDescriptionLong2;
    public float statRating2;//can convert as needed
    public FixedString64Bytes statDescription3;
    public FixedString64Bytes statDescriptionLong3;
    public float statRating3;//can convert as needed

}

[Serializable]
public struct ResourceItemComponent : IComponentData
{
    public Entity pickedUpActor;
    public Entity pickupEntity;
    public bool active;
    public bool enabled;
    public Entity particleSystemEntity;
    public bool particleSystemEntitySpawned;
    public Entity addPickupEntityToInventory;
    public bool itemPickedUp;
    public FixedString64Bytes description;
    public FixedString64Bytes longDescription;
    public bool buttonAssigned;
    public bool useSlot1;
    public bool useSlot2;
    public int index;
    public ResourceType resourceType;
    public int count;
    public int menuIndex;

    public int statCount;
    public FixedString64Bytes statDescription1;
    public FixedString64Bytes statDescriptionLong1;
    public float statRating1;//can convert as needed
    public FixedString64Bytes statDescription2;
    public FixedString64Bytes statDescriptionLong2;
    public float statRating2;//can convert as needed
    public FixedString64Bytes statDescription3;
    public FixedString64Bytes statDescriptionLong3;
    public float statRating3;//can convert as needed

}

public struct CurrencyComponent : IComponentData
{
    public Entity psAttached;
    public Entity pickedUpActor;
    public Entity itemEntity;
    public float currencyValue;
    public bool enabled;

}

public struct TalentItemComponent : IComponentData
{
    public Entity pickedUpActor;
    public Entity pickupEntity;
    public bool active;
    public bool enabled;
    public Entity particleSystemEntity;
    public Entity addPickupEntityToInventory;
    public bool itemPickedUp;
    public FixedString64Bytes description;
    public FixedString64Bytes longDescription;
    public bool buttonAssigned;
    public bool useSlot1;
    public bool useSlot2;
    public int index;
    public TalentType talentType;
    public int count;
    public int menuIndex;

    public int statCount;
    public FixedString64Bytes statDescription1;
    public FixedString64Bytes statDescriptionLong1;
    public float statRating1;//can convert as needed
    public FixedString64Bytes statDescription2;
    public FixedString64Bytes statDescriptionLong2;
    public float statRating2;//can convert as needed
    public FixedString64Bytes statDescription3;
    public FixedString64Bytes statDescriptionLong3;
    public float statRating3;//can convert as needed

}



[Serializable]
public class StatsList
{
    string description;
    string longDescription;
    float rating;

}
public struct PowerItemStats
{
    List<StatsList> statsList;
}

public class PowerItemStatsClass
{
    public PowerItemComponent powerItemComponent;
    List<StatsList> statsList;
}

public struct PowerItemIndexComparer : IComparer<PowerItemComponent> 
{
    

    public int Compare(PowerItemComponent a, PowerItemComponent b)
    {
      
        var a_index = a.index;
        var b_index = b.index;
        if (a_index > b_index)
            return 1;
        else if (a_index < b_index)
            return -1;
        else
            return 0;

    }
}


public struct ImmediateUseComponent : IComponentData
{
    public float value;
}

public struct PickupSystemComponent : IComponentData
{
    public float value;
    public bool followActor;
    public Entity pickedUpActor;
}

public struct AudioSourceComponent : IComponentData
{
    public bool active;
}



public struct ControlPower : IComponentData
{
    public Entity psAttached;
    public Entity pickedUpActor;
    public Entity itemEntity;
    public bool enabled;
    public float controlMultiplier;
}



[InternalBufferCapacity(24)]
public struct PickupListBuffer : IBufferElementData
{
    public Entity e;
    public Entity _parent;
    public bool active;
    public bool pickedUp;
    public bool special;//for ld
    public bool reset;
    public bool playerPickupAllowed;
    public bool enemyPickupAllowed;
    public int index;
    public int statl;
    public float3 position;
}


public struct PickupComponent : IComponentData
{
    public Entity e;
    public Entity _parent;
    public bool active;
    public bool pickedUp;
    public bool special;//for ld
    public bool reset;
    public bool playerPickupAllowed;
    public bool enemyPickupAllowed;
    public int index;
    public int statl;

}

public struct PickupManagerComponent : IComponentData //used for managed components - read and then call methods from MB
{
    public bool playSound;
    public bool setAnimationLayer;
}


public enum PickupType
{
    None,
    Speed,
    Health,
    Dash,
    HealthRate,
    Control
}
public enum TalentType //not used
{
    None,
    Talent1,
    Talent2,
    Talent3,
    Talent4,
    Talent5,
}

public enum CurrencyType
{
    None,
    Pouds,
    Tanzies,
    Beni,
    Drites,
    Beryl
}
public enum ResourceType
{
    None,
    Currency,
    Mineral,
    Consumable,
    Dust
}

[System.Serializable]
public class ItemClass
{
    public Transform location;
    public GameObject ItemPrefab;
}



