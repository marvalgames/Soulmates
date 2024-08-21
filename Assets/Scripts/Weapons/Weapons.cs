using UnityEngine;



public enum WeaponType
{
    None = 0,
    Gun = 1,
    Hand = 2,
    Hammer = 3,
    Body = 4

}

[System.Serializable]

public class Weapons
{
    public bool isAttachedAtStart;
    public WeaponType weaponType;
    public bool secondaryWeapon;
    public GameObject weaponGameObject;//actual weapon
    public Transform weaponLocation;//added transform to rig

}