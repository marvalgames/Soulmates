using Unity.Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    public class Impulse : MonoBehaviour
    {
        [Header("Player Only")] [Tooltip("Player Hurt")]
        public CinemachineImpulseSource impulseSourceHitReceived;

        [Tooltip("Player Hits Enemy")] public CinemachineImpulseSource impulseSourceHitLanded;

        private Entity entity;
        private EntityManager manager;
        //public Animator animator;

        void Start()
        {
//        animator = GetComponent<Animator>();
        
            if (entity == Entity.Null)
            {
                entity = GetComponent<CharacterEntityTracker>().linkedEntity;
                if (manager == default)
                {
                    manager = GetComponent<CharacterEntityTracker>().entityManager;
                }

                if(entity != Entity.Null) manager.AddComponentObject(entity, this);

                //manager.AddComponentObject(entity, this);
            }

        }






    }
}
