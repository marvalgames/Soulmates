using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


[System.Serializable]
public struct AttachWeaponComponent : IComponentData
{
    public bool isPrimaryAttached; //na
    public bool isSecondaryAttached; //na
    public int attachedWeaponSlot;
    public int attachWeaponType;
    public Entity attachWeaponEntity;
    public int attachSecondaryWeaponType;
    public int weaponsAvailableCount;
}


public class WeaponManager : MonoBehaviour
{
    public Weapons primaryWeapon;
    public Weapons secondaryWeapon;

    public List<Weapons> weaponsList; //actual weapon

    //private int weaponIndex = 0;//index of weapons list to start with
    private EntityManager manager;
    public Entity entity;
    [HideInInspector] public bool primaryAttached = false;
    [HideInInspector] public bool secondaryAttached = false;
    GameObject primaryWeaponInstance;
    GameObject secondaryWeaponInstance;
    private bool attachedWeapon;

    //public CameraType weaponCamera;


    void Start()
    {
        if (weaponsList.Count > 0)
        {
            primaryWeapon = weaponsList[0];
        }

        if (weaponsList.Count > 1)
        {
            secondaryWeapon = weaponsList[1];
        }

        if (primaryWeapon is { isAttachedAtStart: true })
        {
            AttachPrimaryWeapon();
        }

        if (secondaryWeapon is { isAttachedAtStart: true })
        {
            AttachSecondaryWeapon();
        }

        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;
            }

            if (entity != Entity.Null)
            {
                manager.AddComponentObject(entity, this);
                manager.AddComponentData(entity,
                    new AttachWeaponComponent
                    {
                        attachedWeaponSlot = 0, //-1 is buggy but needed for pickup weapon IK
                        attachWeaponType = (int)primaryWeapon.weaponType,
                        attachSecondaryWeaponType = (int)secondaryWeapon.weaponType,
                        isPrimaryAttached = primaryAttached,
                        isSecondaryAttached = secondaryAttached,
                    });
            }
        }
    }

    public void AttachPrimaryWeapon()
    {
        if (primaryWeapon.weaponGameObject == null) return;
        primaryAttached = true;
        primaryWeaponInstance = Instantiate(primaryWeapon.weaponGameObject, primaryWeapon.weaponLocation, true);
        primaryWeaponInstance.transform.localPosition = Vector3.zero;
        primaryWeaponInstance.transform.localRotation = Quaternion.identity;
    }

    public void DetachPrimaryWeapon()
    {
        if (primaryWeapon != null)
        {
            primaryAttached = false;
            Destroy(primaryWeaponInstance);
        }
    }

    public void AttachSecondaryWeapon()
    {
        secondaryAttached = true;
        secondaryWeaponInstance = Instantiate(secondaryWeapon.weaponGameObject);
        secondaryWeaponInstance.transform.localPosition = Vector3.zero;
        secondaryWeaponInstance.transform.localRotation = Quaternion.identity;
    }
}