using UnityEngine;

namespace CityShooter.Player
{
    /// <summary>
    /// Manages the player's animation state machine with smooth transitions
    /// between movement states (Idle, Walk, Strafe) and additive firing layers.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string velocityParam = "Velocity";
        [SerializeField] private string horizontalParam = "Horizontal";
        [SerializeField] private string verticalParam = "Vertical";
        [SerializeField] private string isGroundedParam = "IsGrounded";
        [SerializeField] private string isSprintingParam = "IsSprinting";
        [SerializeField] private string isFiringParam = "IsFiring";
        [SerializeField] private string isMovingParam = "IsMoving";
        [SerializeField] private string hitReactionTrigger = "HitReaction";

        [Header("Blend Settings")]
        [SerializeField] private float animationDampTime = 0.1f;
        [SerializeField] private float velocityDampTime = 0.15f;

        [Header("Layer Settings")]
        [SerializeField] private int baseLayerIndex = 0;
        [SerializeField] private int upperBodyLayerIndex = 1;
        [SerializeField] private float upperBodyLayerWeight = 1f;

        // Components
        private Animator animator;

        // Animation parameter hash IDs (cached for performance)
        private int velocityHash;
        private int horizontalHash;
        private int verticalHash;
        private int isGroundedHash;
        private int isSprintingHash;
        private int isFiringHash;
        private int isMovingHash;
        private int hitReactionHash;

        // State
        private float currentVelocity;
        private float currentHorizontal;
        private float currentVertical;
        private bool isFiring;

        // Public properties
        public Animator Animator => animator;
        public bool IsFiring => isFiring;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            CacheParameterHashes();
        }

        private void Start()
        {
            // Set initial upper body layer weight for additive firing animations
            if (animator.layerCount > upperBodyLayerIndex)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, upperBodyLayerWeight);
            }
        }

        private void CacheParameterHashes()
        {
            velocityHash = Animator.StringToHash(velocityParam);
            horizontalHash = Animator.StringToHash(horizontalParam);
            verticalHash = Animator.StringToHash(verticalParam);
            isGroundedHash = Animator.StringToHash(isGroundedParam);
            isSprintingHash = Animator.StringToHash(isSprintingParam);
            isFiringHash = Animator.StringToHash(isFiringParam);
            isMovingHash = Animator.StringToHash(isMovingParam);
            hitReactionHash = Animator.StringToHash(hitReactionTrigger);
        }

        /// <summary>
        /// Update movement animation parameters based on player input and state.
        /// Called by FPSCharacterController every frame.
        /// </summary>
        /// <param name="movementInput">Raw WASD input (x = strafe, y = forward/back)</param>
        /// <param name="normalizedVelocity">Velocity normalized to 0-1 range</param>
        /// <param name="isSprinting">Whether the player is sprinting</param>
        /// <param name="isGrounded">Whether the player is on the ground</param>
        public void UpdateMovementAnimation(Vector2 movementInput, float normalizedVelocity, bool isSprinting, bool isGrounded)
        {
            // Smooth velocity for blend tree
            currentVelocity = Mathf.Lerp(currentVelocity, normalizedVelocity, Time.deltaTime / velocityDampTime);

            // Smooth horizontal and vertical for directional blend tree
            currentHorizontal = Mathf.Lerp(currentHorizontal, movementInput.x, Time.deltaTime / animationDampTime);
            currentVertical = Mathf.Lerp(currentVertical, movementInput.y, Time.deltaTime / animationDampTime);

            // Determine if moving (has any input)
            bool isMoving = movementInput.sqrMagnitude > 0.01f;

            // Set animator parameters
            animator.SetFloat(velocityHash, currentVelocity);
            animator.SetFloat(horizontalHash, currentHorizontal);
            animator.SetFloat(verticalHash, currentVertical);
            animator.SetBool(isGroundedHash, isGrounded);
            animator.SetBool(isSprintingHash, isSprinting);
            animator.SetBool(isMovingHash, isMoving);
        }

        /// <summary>
        /// Start or stop the firing animation on the upper body additive layer.
        /// </summary>
        /// <param name="firing">True to start firing, false to stop</param>
        public void SetFiring(bool firing)
        {
            isFiring = firing;
            animator.SetBool(isFiringHash, firing);
        }

        /// <summary>
        /// Trigger a hit reaction animation (can be used for player taking damage).
        /// </summary>
        public void TriggerHitReaction()
        {
            animator.SetTrigger(hitReactionHash);
        }

        /// <summary>
        /// Set the weight of the upper body layer for additive animations.
        /// </summary>
        /// <param name="weight">Layer weight (0-1)</param>
        public void SetUpperBodyLayerWeight(float weight)
        {
            if (animator.layerCount > upperBodyLayerIndex)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, Mathf.Clamp01(weight));
            }
        }

        /// <summary>
        /// Play a specific animation state directly.
        /// </summary>
        /// <param name="stateName">Name of the animation state</param>
        /// <param name="layer">Animator layer index</param>
        /// <param name="normalizedTime">Starting time (0-1)</param>
        public void PlayState(string stateName, int layer = 0, float normalizedTime = 0f)
        {
            animator.Play(stateName, layer, normalizedTime);
        }

        /// <summary>
        /// Crossfade to a specific animation state.
        /// </summary>
        /// <param name="stateName">Name of the animation state</param>
        /// <param name="transitionDuration">Duration of the crossfade</param>
        /// <param name="layer">Animator layer index</param>
        public void CrossFadeState(string stateName, float transitionDuration = 0.1f, int layer = 0)
        {
            animator.CrossFade(stateName, transitionDuration, layer);
        }

        /// <summary>
        /// Get the current animation state info for a specific layer.
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo(int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer);
        }

        /// <summary>
        /// Check if a specific state is currently playing.
        /// </summary>
        public bool IsPlayingState(string stateName, int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }
    }
}
