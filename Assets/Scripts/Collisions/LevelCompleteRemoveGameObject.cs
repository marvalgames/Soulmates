using System.Collections;
using System.Collections.Generic;
using Collisions;
using Unity.Entities;
using UnityEngine;

public class LevelCompleteRemoveGameObject : MonoBehaviour //need for webgl since gam object mesh required because entity mesh not visible in sub-scenes
{
    private Entity entity;
    private EntityManager entityManager;

    [Tooltip("Level to Remove Entity and corresponding GameObject Mesh  (0 never removes or creates Component Tag)  ")]
    public int levelCompleteIndex;
    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        entity = entityManager.CreateEntity();

        entityManager.AddComponentObject(entity, this);
        if (levelCompleteIndex > 0)
        {
            entityManager.AddComponentData(entity, new LevelCompleteRemove { levelCompleteIndex = levelCompleteIndex});
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (entityManager == default || entity == Entity.Null) return;
        if (entityManager.HasComponent<LevelCompleteRemove>(entity))
        {
            int level = LevelManager.instance.currentLevelCompleted;
            if (level == levelCompleteIndex)
            {
                //gameObject.SetActive(false);
                Destroy(gameObject);
                Debug.Log("LEVEL " + level);
            }
        }
        
    }
}
