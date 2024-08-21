using Unity.Entities;
using UnityEngine;


struct SimpleFollowComponent : IComponentData
{
    
}



public class SimpleFollowAuthoring : MonoBehaviour
{
    private Entity _entity;
    private EntityManager _entityManager;
    void Start()
    {
        if (_entity == Entity.Null)
        {
            _entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (_entityManager == default)
            {
                _entityManager = GetComponent<CharacterEntityTracker>().entityManager;
                
                _entityManager.AddComponentObject(_entity, this);
                _entityManager.AddComponentData(_entity, new SimpleFollowComponent());

            }
            
           
        }
    }

}
