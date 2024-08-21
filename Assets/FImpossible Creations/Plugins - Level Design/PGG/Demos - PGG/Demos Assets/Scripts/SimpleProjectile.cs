using UnityEngine;

namespace FIMSpace.Generating
{
    public class SimpleProjectile : MonoBehaviour
    {
        public Vector3 Velocity;
        private Rigidbody rig;
        private void Start()
        {
            rig = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (rig.detectCollisions)
                rig.linearVelocity = Velocity;
            else
                rig.linearVelocity = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            SimpleEnemy enemy = collision.transform.GetComponent<SimpleEnemy>();
            if (enemy)
            {
                enemy.Damage(1);
                Deactivate();
            }
            else
            {
                if (collision.transform.GetComponent<CharacterController>() == null)
                {
                    Deactivate();
                }
            }
        }

        private void Deactivate()
        {
            rig.detectCollisions = false;
            ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            GetComponentInChildren<MeshRenderer>().enabled = false;
            Destroy(gameObject, 3f);
        }
    }
}