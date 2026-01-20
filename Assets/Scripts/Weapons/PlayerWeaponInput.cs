using UnityEngine;

namespace CityShooter.Weapons
{
    /// <summary>
    /// Handles player input for the weapon system.
    /// Connects movement state from character controller to weapon animations.
    /// </summary>
    public class PlayerWeaponInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LaserGunController laserGunController;
        [SerializeField] private WeaponAnimationController animationController;

        [Header("Movement Detection")]
        [SerializeField] private float movementThreshold = 0.1f;
        [SerializeField] private bool useCharacterControllerVelocity = true;

        [Header("Input Settings")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string fireButton = "Fire1";

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private Vector3 _lastPosition;
        private bool _isMoving;

        private void Awake()
        {
            // Try to find components if not assigned
            if (laserGunController == null)
            {
                laserGunController = GetComponentInChildren<LaserGunController>();
            }

            if (animationController == null)
            {
                animationController = GetComponentInChildren<WeaponAnimationController>();
            }

            // Find movement components
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();

            _lastPosition = transform.position;
        }

        private void Update()
        {
            UpdateMovementState();
            HandleFireInput();
        }

        /// <summary>
        /// Updates the movement state for animation blending.
        /// </summary>
        private void UpdateMovementState()
        {
            bool wasMoving = _isMoving;
            float movementSpeed = 0f;

            if (useCharacterControllerVelocity && _characterController != null)
            {
                // Use CharacterController velocity
                movementSpeed = new Vector2(_characterController.velocity.x, _characterController.velocity.z).magnitude;
            }
            else if (_rigidbody != null)
            {
                // Use Rigidbody velocity (using velocity for Unity 2022 compatibility)
#if UNITY_6000_0_OR_NEWER
                movementSpeed = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z).magnitude;
#else
                movementSpeed = new Vector2(_rigidbody.velocity.x, _rigidbody.velocity.z).magnitude;
#endif
            }
            else
            {
                // Calculate from position change
                Vector3 movement = transform.position - _lastPosition;
                movementSpeed = new Vector2(movement.x, movement.z).magnitude / Time.deltaTime;
                _lastPosition = transform.position;
            }

            // Check input as fallback
            float inputMagnitude = new Vector2(
                Input.GetAxis(horizontalAxis),
                Input.GetAxis(verticalAxis)
            ).magnitude;

            _isMoving = movementSpeed > movementThreshold || inputMagnitude > movementThreshold;

            // Update weapon systems
            if (laserGunController != null)
            {
                laserGunController.SetMovementState(_isMoving);
            }

            if (animationController != null)
            {
                float normalizedSpeed = Mathf.Clamp01(movementSpeed / 5f); // Normalize to max walk speed
                animationController.SetMovementState(_isMoving, normalizedSpeed);
            }
        }

        /// <summary>
        /// Handles fire button input.
        /// </summary>
        private void HandleFireInput()
        {
            if (Input.GetButton(fireButton))
            {
                if (laserGunController != null)
                {
                    laserGunController.TryFire();
                }
            }
        }

        /// <summary>
        /// Gets whether the player is currently moving.
        /// </summary>
        public bool IsMoving => _isMoving;

        /// <summary>
        /// Sets the laser gun controller reference.
        /// </summary>
        public void SetLaserGunController(LaserGunController controller)
        {
            laserGunController = controller;
        }

        /// <summary>
        /// Sets the animation controller reference.
        /// </summary>
        public void SetAnimationController(WeaponAnimationController controller)
        {
            animationController = controller;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Show movement state
            Gizmos.color = _isMoving ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
#endif
    }
}
