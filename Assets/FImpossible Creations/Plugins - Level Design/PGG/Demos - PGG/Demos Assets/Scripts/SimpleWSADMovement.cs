using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class SimpleWSADMovement : MonoBehaviour
    {
        public static SimpleWSADMovement Instance;
        private void Awake()
        {
            Instance = this;
        }

        public float Speed = 10f;
        public GameObject Projectile;
        public float ProjectileSpeed = 5f;
        public bool FreezeGravity = true;
        public bool JumpOnSpace = false;

        void Start()
        {
            rig = GetComponent<Rigidbody>();
        }

        private Rigidbody rig;
        private Vector3 translation;
        private Quaternion tgtRotation;
        private float sd_acc = 0f;
        private float acceleration = 0f;
        private float triggerJump = 0f;


        void Update()
        {
            Vector3 moveDir = Vector3.zero;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) moveDir += Vector3.forward;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) moveDir += Vector3.back;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) moveDir += Vector3.left;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) moveDir += Vector3.right;

            if (JumpOnSpace)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                    triggerJump = 3f;
            }
            else
            {
                if (Input.GetKey(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    CreateProjectile();
            }

            if (moveDir != Vector3.zero)
            {
                moveDir.Normalize();
                moveDir = Vector3.ProjectOnPlane(Camera.main.transform.TransformDirection(moveDir), Vector3.up).normalized;


                acceleration = Mathf.SmoothDamp(acceleration, 1f, ref sd_acc, 0.1f, 10000f, Time.deltaTime);

                tgtRotation = Quaternion.LookRotation(moveDir);
            }
            else
            {
                acceleration = Mathf.SmoothDamp(acceleration, 0f, ref sd_acc, 0.1f, 10000f, Time.deltaTime);
            }

            float speed = Speed;
            if (Input.GetKey(KeyCode.LeftShift)) speed *= 2f;
            translation = (moveDir * speed * acceleration);

        }

        void CreateProjectile()
        {
            if (Projectile == null) return;
            GameObject pr = GameObject.Instantiate(Projectile);

            Vector3 target = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Vector3.Distance(Camera.main.transform.position, transform.position)));
            Vector3 dir = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            dir.Normalize();

            pr.transform.position = transform.position + Vector3.up * 0.5f + dir * 0.65f;

            pr.GetComponent<SimpleProjectile>().Velocity = dir * ProjectileSpeed;
        }


        private void FixedUpdate()
        {
            if (FreezeGravity)
                rig.linearVelocity = translation;
            else
            {
                if ( triggerJump > 0f)
                {
                    rig.linearVelocity = new Vector3(rig.linearVelocity.x, triggerJump, rig.linearVelocity.z);
                    triggerJump = 0f;
                }

                rig.linearVelocity = new Vector3(translation.x, rig.linearVelocity.y, translation.z);
            }

            rig.maxAngularVelocity = 10f;

            if (rig.linearVelocity.sqrMagnitude > 0.05f)
                rig.angularVelocity = FEngineering.QToAngularVelocity(rig.rotation, tgtRotation, true);
            else
                rig.angularVelocity = Vector3.zero;
        }
    }
}