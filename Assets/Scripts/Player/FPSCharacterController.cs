using UnityEngine;

namespace CityShooter.Player
{
    /// <summary>
    /// First-Person Shooter Character Controller with WASD movement and mouse look.
    /// Uses Unity's CharacterController for collision detection and physics-independent movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSCharacterController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Mouse Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalLookLimit = 85f;
        [SerializeField] private bool invertY = false;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerAnimationController animationController;

        // Components
        private CharacterController characterController;

        // State
        private Vector3 velocity;
        private float currentSpeed;
        private float verticalRotation;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;

        // Input
        private Vector2 movementInput;
        private Vector2 lookInput;
        private bool jumpInput;
        private bool sprintInput;

        // Public properties for external access
        public Vector3 Velocity => velocity;
        public Vector2 MovementInput => movementInput;
        public float CurrentSpeed => currentSpeed;
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;

        /// <summary>
        /// Normalized horizontal velocity magnitude (0-1 range for animation blending)
        /// </summary>
        public float NormalizedVelocity
        {
            get
            {
                Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
                return Mathf.Clamp01(horizontalVelocity.magnitude / sprintSpeed);
            }
        }

        /// <summary>
        /// Horizontal strafe input (-1 to 1)
        /// </summary>
        public float StrafeInput => movementInput.x;

        /// <summary>
        /// Forward/backward input (-1 to 1)
        /// </summary>
        public float ForwardInput => movementInput.y;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }

            if (animationController == null)
            {
                animationController = GetComponent<PlayerAnimationController>();
            }

            // Lock and hide cursor for FPS gameplay
            LockCursor(true);
        }

        private void Update()
        {
            GatherInput();
            HandleMouseLook();
            HandleMovement();
            ApplyGravity();
            UpdateAnimationController();
        }

        private void GatherInput()
        {
            // Movement input (WASD)
            movementInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            // Mouse look input
            lookInput = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            );

            // Action inputs
            jumpInput = Input.GetButtonDown("Jump");
            sprintInput = Input.GetKey(KeyCode.LeftShift);
        }

        private void HandleMouseLook()
        {
            if (cameraTransform == null) return;

            // Horizontal rotation (rotate the entire player)
            float horizontalRotation = lookInput.x * mouseSensitivity;
            transform.Rotate(Vector3.up * horizontalRotation);

            // Vertical rotation (rotate only the camera)
            float verticalInput = lookInput.y * mouseSensitivity;
            if (invertY) verticalInput = -verticalInput;

            verticalRotation -= verticalInput;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void HandleMovement()
        {
            // Ground check
            if (groundCheck != null)
            {
                isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }
            else
            {
                isGrounded = characterController.isGrounded;
            }

            // Reset vertical velocity when grounded
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small negative value to keep grounded
            }

            // Determine movement speed
            isSprinting = sprintInput && movementInput.y > 0 && !isCrouching;
            currentSpeed = isSprinting ? sprintSpeed : (isCrouching ? crouchSpeed : walkSpeed);

            // Calculate movement direction relative to player facing
            Vector3 moveDirection = transform.right * movementInput.x + transform.forward * movementInput.y;
            moveDirection = moveDirection.normalized;

            // Apply horizontal movement
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Handle jumping
            if (jumpInput && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        private void ApplyGravity()
        {
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateAnimationController()
        {
            if (animationController != null)
            {
                animationController.UpdateMovementAnimation(
                    movementInput,
                    NormalizedVelocity,
                    isSprinting,
                    isGrounded
                );
            }
        }

        /// <summary>
        /// Lock or unlock the cursor for FPS gameplay
        /// </summary>
        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>
        /// Set mouse sensitivity at runtime
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        /// <summary>
        /// Toggle crouch state
        /// </summary>
        public void SetCrouching(bool crouch)
        {
            isCrouching = crouch;
            // Could also adjust CharacterController height here for crouch collision
        }

        /// <summary>
        /// Apply external velocity (e.g., knockback, explosions)
        /// </summary>
        public void AddImpulse(Vector3 impulse)
        {
            velocity += impulse;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize ground check sphere
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }
    }
}
