//THIS IS A DEMO SCRIPT - WITH THE SOLE PORPUSE OF WORKING WITH THIS SUMMON CREATURES EFFECT
//THIS IS NOT A GOOD SCRIPT - BUT YOU CAN TAKE IDEAS FROM HERE AND IMPROVE THEM


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TigerAttackShooter : MonoBehaviour
{
    public Camera cam;
    public GameObject projectile;
    public Transform firePoint;
    public float fireRate = 4;
    public bool rotateOnlyY = true; //disable this if shooting flying enemies, so they properly rotate to where we are aiming

    private Vector3 destination;
    private float timeToFire;
    private TigerAttack tigerAttackScript;

    void Update()
    {
        if(Input.GetButton("Fire1") && Time.time >= timeToFire)
        {
            timeToFire = Time.time + 1/fireRate;
            ShootProjectile();
        }
    }

    void ShootProjectile()
    {
        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            destination = ray.GetPoint(1000);

            InstantiateProjectile();
        }
        else
        {
            Debug.Log("B");
            InstantiateProjectileAtFirePoint();
        }
    }

    void InstantiateProjectile()
    {
        var projectileObj = Instantiate (projectile, firePoint.position, Quaternion.identity) as GameObject;

        tigerAttackScript = projectileObj.GetComponent<TigerAttack>();
        RotateToDestination(projectileObj, destination, rotateOnlyY);
        projectileObj.GetComponent<Rigidbody>().linearVelocity = transform.forward * tigerAttackScript.speed;
        
    }

    void InstantiateProjectileAtFirePoint()
    {
        var projectileObj = Instantiate(projectile, firePoint.position, Quaternion.identity) as GameObject;

        tigerAttackScript = projectileObj.GetComponent<TigerAttack>();
        RotateToDestination(projectileObj, firePoint.transform.forward*1000, rotateOnlyY);
        projectileObj.GetComponent<Rigidbody>().linearVelocity = firePoint.transform.forward * tigerAttackScript.speed;
    }

    void RotateToDestination(GameObject obj, Vector3 destination, bool onlyY)
    {
        var direction = destination - obj.transform.position;
        var rotation = Quaternion.LookRotation(direction);

        if (onlyY)
        {
            rotation.x = 0;
            rotation.z = 0;
        }

        obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1);
    }
}
