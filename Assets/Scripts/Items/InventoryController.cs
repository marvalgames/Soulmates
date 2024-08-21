using UnityEngine;
using Unity.Entities;
using System;

public struct InventoryComponent : IComponentData
{


}

public class InventoryController : MonoBehaviour
{
    public enum InventoryCategory
    {
        pickup,
        resources,
        talents
    }

    //public static event Action<bool> HideSubscriberMenu;
    private EntityManager manager;
    public Entity entity;
    [SerializeField]
    private CanvasGroup canvasGroup;



    // Start is called before the first frame update
    [HideInInspector]
    InventoryCategory inventoryMenuIndex;
    void Start()
    {
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        manager.AddComponentData(entity, new InventoryComponent
        {

        });
        manager.AddComponentObject(entity, this);
        canvasGroup = GetComponent<CanvasGroup>();


    }

    // Update is called once per frame


    void Update()
    {
        if (entity == Entity.Null || manager == default) return;

    }


    public void ShowMenu()
    {
      

    }

}
