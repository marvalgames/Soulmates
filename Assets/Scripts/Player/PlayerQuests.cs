using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public class PlayerQuests : MonoBehaviour
    {
        public Entity e;
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
    }
}