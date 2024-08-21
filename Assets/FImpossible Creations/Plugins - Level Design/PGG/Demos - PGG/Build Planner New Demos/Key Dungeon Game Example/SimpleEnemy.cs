using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class SimpleEnemy : MonoBehaviour
    {
        public int Hp = 3;
        public bool IsBoss = false;
        public GameObject OnDestroy;
        private Rigidbody rig;

        void Start()
        {
            rig = GetComponent<Rigidbody>();
        }

        float dist = 100f;
        void Update()
        {
            if (SimpleWSADMovement.Instance == null) return;

            dist = Vector2.Distance(transform.position.V3toV2(), SimpleWSADMovement.Instance.transform.position.V3toV2());
            if (dist < 4f)
            {
                transform.rotation = Quaternion.LerpUnclamped(transform.rotation, Quaternion.LookRotation(SimpleWSADMovement.Instance.transform.position - transform.position), Time.deltaTime * 3f);
            }
        }

        private void FixedUpdate()
        {
            if (dist < 4f)
            {
                rig.linearVelocity = transform.forward * 0.4f;
            }
            else
            {
                rig.linearVelocity = Vector3.zero;
            }
        }

        public void Damage(int amount)
        {
            Hp -= amount;
            if (Hp <= 0)
            {
                GameObject expl = null;
                if (OnDestroy)
                    expl = GameObject.Instantiate(OnDestroy, transform.position + Vector3.up * 0.7f, Quaternion.identity);

                if (IsBoss)
                {
                    if (expl != null) expl.transform.localScale = Vector3.one * 2f;

                    if (DungeonGameController_PGGDemo.Instance) DungeonGameController_PGGDemo.Instance.OnBossDeath();
                    else
                        SimpleGameController.Instance.OnBossDeath();
                }

                GameObject.Destroy(gameObject);
            }
        }

        private void OnMouseDown()
        {
            Damage(1);
        }
    }
}