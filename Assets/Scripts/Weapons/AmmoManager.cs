using Unity.Entities;
using UnityEngine;


public class AmmoManager : MonoBehaviour
{
    public AudioSource weaponAudioSource;
    [HideInInspector]
    //public List<GameObject> AmmoInstances = new List<GameObject>();
    public AudioClip weaponAudioClip;
    public Transform AmmoStartLocation;

    private EntityManager manager;
    private Entity entity;

    private void Start()
    {
        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;
            }

            if(entity != Entity.Null) manager.AddComponentObject(entity, this);
        }
    }
}


