using Unity.Entities;
using Unity.Physics;
using UnityEngine;

// IConvertGameObjectToEntity pipeline is called *before* the Physics Body & Shape Conversion Systems
// This means that there would be no PhysicsMass component to tweak when Convert is called.
// Instead Convert is called from the PhysicsSamplesConversionSystem instead.
public class SetInertiaInverseBehaviour : MonoBehaviour
{
    public bool LockX = false;
    public bool LockY = false;
    public bool LockZ = false;

    public bool LockVelocity = false;


    private Entity e;
    private EntityManager manager;


    void Start()
    {
        if (e == Entity.Null)
        {
            e = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;

            }

        }
    }

    void Update()
    {
       
        if(e == Entity.Null) return;
        //Debug.Log("lock");
        if (manager.HasComponent<PhysicsMass>(e))
        {
            var mass = manager.GetComponentData<PhysicsMass>(e);
            //mass.InverseMass = .01f;
            mass.InverseInertia[0] = LockX ? .0f: mass.InverseInertia[0];
            mass.InverseInertia[1] = LockY ? .0f: mass.InverseInertia[1];
            mass.InverseInertia[2] = LockZ ? .0f : mass.InverseInertia[2]; 
            manager.SetComponentData(e, mass);
        }


    }

 
}

