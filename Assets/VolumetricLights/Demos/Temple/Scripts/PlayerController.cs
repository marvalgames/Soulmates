using UnityEngine;

namespace VolumetricLightsDemo {

    public class PlayerController : MonoBehaviour {
        // Cached vars
        CharacterController thisCharacterController;
        Transform cameraTransform;

        // Internal vars
        float InpVer;
        float InpHor;
        float jumpTimer = 0;
        float yRotate;
        Vector3 direction;
        float sprint = 1;
        // Adjustable vars
        [SerializeField] float speed = 10;
        [SerializeField] float sprintMax = 2;
        [SerializeField] float jumpTime = 0.25f;
        [SerializeField] float jumpSpeed = 8;
        [SerializeField] float gravity = 6;
        [SerializeField] float mouseSpeed = 6;

        // Start is called before the first frame update
        void Start() {
            // Setup character controller
            thisCharacterController = gameObject.AddComponent<CharacterController>();
            thisCharacterController.height = 2;
            thisCharacterController.center = Vector3.up;
            thisCharacterController.stepOffset = 0.8f;
            // Setup camera
            cameraTransform = Camera.main.transform;
            cameraTransform.position = transform.position + Vector3.up * 2f;
            cameraTransform.transform.parent = transform;
            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Update is called once per frame
        void Update() {
            // Get Input
            InpHor = Input.GetAxis("Horizontal");
            InpVer = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.LeftShift)) {
                if (sprint < sprintMax) sprint += Time.deltaTime * 5;
            } else {
                if (sprint > 1) sprint -= Time.deltaTime * 5;
            }

            // Get Jump
            if (Input.GetButtonDown("Jump") && thisCharacterController.isGrounded == true) {
                // Make jump timer = to jump time
                jumpTimer = jumpTime;
            }

            // Rotate
            transform.Rotate(0, Input.GetAxis("Mouse X") * mouseSpeed, 0);

            // Construct input vector
            direction = (transform.forward * InpVer) + (transform.right * InpHor);
            direction *= speed * sprint;

            // If we are above zero move up, Jump
            if (jumpTimer > 0) {
                // decrease jumpTimer
                jumpTimer -= Time.deltaTime;
                direction.y += jumpSpeed * jumpTimer;
            } else {
                direction.y -= gravity;
            }

            // Move controller
            thisCharacterController.Move(direction * Time.deltaTime);

            // Camera rotate up down
            yRotate += -Input.GetAxis("Mouse Y") * mouseSpeed;
            // Clamp rotation
            yRotate = Mathf.Clamp(yRotate, -85, 89);
            // Apply rotation
            cameraTransform.localEulerAngles = new Vector3(yRotate, 0, 0);
        }
    }

}