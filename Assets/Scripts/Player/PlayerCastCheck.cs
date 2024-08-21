using UnityEngine;

namespace Sandbox.Player
{

    public class PlayerCastCheck : MonoBehaviour
    {
        [SerializeField]
        private float hitDistance = 0;

        [SerializeField] 
        private float radius = 0.5f;

        [SerializeField]
        float checkMaxDistance = 5f;

        [SerializeField] Transform startTransform;

        [SerializeField] private float forwardSpeed = 1.0f;
        [SerializeField] private float upSpeed = 1.0f;

        [SerializeField]
        private GameObject hitGameObject;


        private Rigidbody rb;

        [SerializeField]
        //private float addedNegativeForce = -9.81f;


        public float maxStepHeight = 0.4f;        // The maximum a player can set upwards in units when they hit a wall that's potentially a step
        public float stepSearchOvershoot = 0.01f; // How much to overshoot into the direction a potential step in units when testing. High values prevent player from walking up tiny steps but may cause problems.

        void Awake()
        {
            if (startTransform == null)
            {
                startTransform = this.transform;
            }

            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {

            ObstacleChecker();


        }

    


        public void ObstacleChecker()
        {
            rb.isKinematic = false;
            RaycastHit hit;
            var position = startTransform.position;
            var radius = .25f;
            var forwardRay = new Ray(position, transform.forward);
            LayerMask mask = ~(1 << LayerMask.NameToLayer("Player"));//so it cant touch itself
            var touchObstacle = Physics.SphereCast(position, radius,transform.forward, out hit, checkMaxDistance, mask);
            hitDistance = hit.distance;
            hitGameObject = null;
            if (hit.collider != null)
            {
                hitGameObject = hit.transform.gameObject;
                // Move the object forward along its z axis 1 unit/second.
                //Vector3 speed = new Vector3(0, upSpeed, forwardSpeed);

                //transform.Translate(speed * SystemAPI.Time.DeltaTime);
                //transform.Translate(Vector3.forward * 16f * SystemAPI.Time.DeltaTime);
                //transform.Translate(Vector3.forward * speed);


                if (hitGameObject.transform.gameObject.layer == LayerMask.NameToLayer("Step"))
                {
                    //rb.MovePosition(transform.position + transform.forward * forwardSpeed + transform.up * upSpeed * SystemAPI.Time.DeltaTime);
                    rb.AddRelativeForce(transform.forward*forwardSpeed+transform.up*upSpeed, ForceMode.VelocityChange);
                    // Do something for this layer
                    //rb.AddForce(0f, addedNegativeForce, 0f, ForceMode.Acceleration);

                    var speed = new Vector3(0, upSpeed, forwardSpeed);

                    transform.Translate(speed * Time.deltaTime);
                    rb.isKinematic = true;

                }


            }


        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Debug.DrawLine(startTransform.position, startTransform.position + transform.forward * hitDistance);
            Gizmos.DrawWireSphere(transform.position + transform.forward * hitDistance, radius);

        }


    }
}
