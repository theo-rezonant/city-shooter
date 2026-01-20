using UnityEngine;

namespace CityShooter.Core
{
    /// <summary>
    /// Monitors player movement and broadcasts state changes to the HUD.
    /// Attach to the player character.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementMonitor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float movementThreshold = 0.01f;
        [SerializeField] private float maxSpeed = 10f;

        private CharacterController characterController;
        private Vector3 lastPosition;
        private bool wasMoving;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            lastPosition = transform.position;
        }

        private void Update()
        {
            Vector3 currentPosition = transform.position;
            Vector3 movement = currentPosition - lastPosition;
            movement.y = 0f; // Ignore vertical movement

            float speed = movement.magnitude / Time.deltaTime;
            float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
            bool isMoving = speed > movementThreshold;

            // Only broadcast if state changed or moving
            if (isMoving != wasMoving || (isMoving && normalizedSpeed > 0))
            {
                CombatEvents.InvokePlayerMovementChanged(isMoving, normalizedSpeed);
            }

            wasMoving = isMoving;
            lastPosition = currentPosition;
        }
    }

    /// <summary>
    /// Alternative movement monitor for Rigidbody-based characters.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerRigidbodyMonitor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float movementThreshold = 0.1f;
        [SerializeField] private float maxSpeed = 10f;

        private Rigidbody rb;
        private bool wasMoving;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f; // Ignore vertical velocity

            float speed = velocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
            bool isMoving = speed > movementThreshold;

            if (isMoving != wasMoving || isMoving)
            {
                CombatEvents.InvokePlayerMovementChanged(isMoving, normalizedSpeed);
            }

            wasMoving = isMoving;
        }
    }
}
