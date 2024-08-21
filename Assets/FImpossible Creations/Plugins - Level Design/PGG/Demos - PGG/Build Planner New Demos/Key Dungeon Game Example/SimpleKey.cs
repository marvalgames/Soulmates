using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FIMSpace.Generating
{
    public class SimpleKey : MonoBehaviour
    {
        public UnityEvent OnCollected;
        public GameObject OnCollectedCreate;
        public bool IsBossKey = false;

        void Start()
        {

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.CompareTag("Player"))
            {
                if (IsBossKey)
                {
                    if (DungeonGameController_PGGDemo.Instance) DungeonGameController_PGGDemo.Instance.OnKeyCollected();
                    else
                    SimpleGameController.Instance.OnKeyCollected();
                }

                OnCollected.Invoke();
                if (OnCollectedCreate) GameObject.Instantiate(OnCollectedCreate, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                GameObject.Destroy(gameObject);
            }
        }
    }
}