using UnityEngine;

namespace CityShooter.Weapons
{
    /// <summary>
    /// Controls weapon animations with support for additive animation layers.
    /// Handles firing animations (static_fire.fbx and moving_fire.fbx) with proper blending.
    /// Uses Animator Controller with additive layers to overlay fire animations on movement.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WeaponAnimationController : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private string fireTrigger = "Fire";
        [SerializeField] private string movingFireTrigger = "MovingFire";
        [SerializeField] private string isMovingBool = "IsMoving";
        [SerializeField] private string movementSpeedFloat = "MovementSpeed";

        [Header("Layer Configuration")]
        [Tooltip("Index of the base movement layer")]
        [SerializeField] private int baseLayerIndex = 0;

        [Tooltip("Index of the fire animation layer (should be additive)")]
        [SerializeField] private int fireLayerIndex = 1;

        [Tooltip("Name of the fire layer for runtime lookup")]
        [SerializeField] private string fireLayerName = "Fire";

        [Header("Blend Settings")]
        [SerializeField] private float fireLayerWeight = 1f;
        [SerializeField] private float blendSpeed = 10f;

        [Header("Animation State Names")]
        [SerializeField] private string staticFireState = "StaticFire";
        [SerializeField] private string movingFireState = "MovingFire";
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string strafeState = "Strafe";

        private Animator _animator;
        private int _fireTriggerHash;
        private int _movingFireTriggerHash;
        private int _isMovingHash;
        private int _movementSpeedHash;
        private bool _isMoving;
        private float _currentMovementSpeed;
        private float _targetFireLayerWeight;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateLayerWeights();
        }

        /// <summary>
        /// Initializes the animation controller and caches parameter hashes.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.LogError("[WeaponAnimationController] No Animator component found!");
                return;
            }

            // Cache parameter hashes for performance
            _fireTriggerHash = Animator.StringToHash(fireTrigger);
            _movingFireTriggerHash = Animator.StringToHash(movingFireTrigger);
            _isMovingHash = Animator.StringToHash(isMovingBool);
            _movementSpeedHash = Animator.StringToHash(movementSpeedFloat);

            // Try to find fire layer by name
            if (fireLayerIndex < 0)
            {
                fireLayerIndex = _animator.GetLayerIndex(fireLayerName);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Triggers the appropriate fire animation based on movement state.
        /// </summary>
        /// <param name="isMoving">Whether the character is currently moving.</param>
        public void TriggerFireAnimation(bool isMoving)
        {
            if (_animator == null)
            {
                Initialize();
                if (_animator == null)
                    return;
            }

            _isMoving = isMoving;

            if (isMoving)
            {
                // Use moving fire animation (designed to blend with strafe)
                TriggerMovingFireAnimation();
            }
            else
            {
                // Use static fire animation
                TriggerStaticFireAnimation();
            }

            // Temporarily boost fire layer weight
            _targetFireLayerWeight = fireLayerWeight;
        }

        /// <summary>
        /// Triggers the static fire animation.
        /// </summary>
        public void TriggerStaticFireAnimation()
        {
            if (_animator == null)
                return;

            _animator.SetTrigger(_fireTriggerHash);

            // Directly play if needed
            if (fireLayerIndex >= 0)
            {
                _animator.Play(staticFireState, fireLayerIndex, 0f);
            }
        }

        /// <summary>
        /// Triggers the moving fire animation (additive/overlay).
        /// </summary>
        public void TriggerMovingFireAnimation()
        {
            if (_animator == null)
                return;

            _animator.SetTrigger(_movingFireTriggerHash);

            // Directly play on fire layer if needed
            if (fireLayerIndex >= 0)
            {
                _animator.Play(movingFireState, fireLayerIndex, 0f);
            }
        }

        /// <summary>
        /// Updates the movement state for animation blending.
        /// </summary>
        /// <param name="isMoving">Whether the character is moving.</param>
        /// <param name="speed">Current movement speed (0-1 normalized).</param>
        public void SetMovementState(bool isMoving, float speed = 0f)
        {
            if (_animator == null)
                return;

            _isMoving = isMoving;
            _currentMovementSpeed = speed;

            _animator.SetBool(_isMovingHash, isMoving);
            _animator.SetFloat(_movementSpeedHash, speed);
        }

        /// <summary>
        /// Smoothly updates layer weights for proper blending.
        /// </summary>
        private void UpdateLayerWeights()
        {
            if (_animator == null || fireLayerIndex < 0)
                return;

            // Smoothly interpolate to target weight
            float currentWeight = _animator.GetLayerWeight(fireLayerIndex);
            float newWeight = Mathf.Lerp(currentWeight, _targetFireLayerWeight, Time.deltaTime * blendSpeed);
            _animator.SetLayerWeight(fireLayerIndex, newWeight);

            // Gradually reduce fire layer weight when not firing
            if (_targetFireLayerWeight > 0f)
            {
                _targetFireLayerWeight = Mathf.Max(0f, _targetFireLayerWeight - Time.deltaTime);
            }
        }

        /// <summary>
        /// Sets the fire animation layer weight.
        /// </summary>
        /// <param name="weight">Layer weight (0-1).</param>
        public void SetFireLayerWeight(float weight)
        {
            if (_animator == null || fireLayerIndex < 0)
                return;

            _animator.SetLayerWeight(fireLayerIndex, Mathf.Clamp01(weight));
        }

        /// <summary>
        /// Plays a specific animation state on a layer.
        /// </summary>
        /// <param name="stateName">Name of the animation state.</param>
        /// <param name="layerIndex">Layer to play on.</param>
        /// <param name="normalizedTime">Starting time (0-1).</param>
        public void PlayState(string stateName, int layerIndex = 0, float normalizedTime = 0f)
        {
            if (_animator == null)
                return;

            _animator.Play(stateName, layerIndex, normalizedTime);
        }

        /// <summary>
        /// Crossfades to a specific animation state.
        /// </summary>
        /// <param name="stateName">Name of the animation state.</param>
        /// <param name="transitionDuration">Duration of the crossfade.</param>
        /// <param name="layerIndex">Layer to crossfade on.</param>
        public void CrossFadeTo(string stateName, float transitionDuration = 0.1f, int layerIndex = 0)
        {
            if (_animator == null)
                return;

            _animator.CrossFade(stateName, transitionDuration, layerIndex);
        }

        /// <summary>
        /// Gets whether the animator is currently playing a fire animation.
        /// </summary>
        public bool IsPlayingFireAnimation
        {
            get
            {
                if (_animator == null || fireLayerIndex < 0)
                    return false;

                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(fireLayerIndex);
                return stateInfo.IsName(staticFireState) || stateInfo.IsName(movingFireState);
            }
        }

        /// <summary>
        /// Gets the underlying Animator component.
        /// </summary>
        public Animator Animator => _animator;

        /// <summary>
        /// Gets whether the controller is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets or sets whether movement is active.
        /// </summary>
        public bool IsMoving
        {
            get => _isMoving;
            set => SetMovementState(value, _currentMovementSpeed);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper to setup the Animator Controller with proper layers.
        /// </summary>
        [ContextMenu("Log Animator Info")]
        private void LogAnimatorInfo()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                Debug.Log("[WeaponAnimationController] No Animator found.");
                return;
            }

            Debug.Log($"[WeaponAnimationController] Animator Info:");
            Debug.Log($"  Controller: {(_animator.runtimeAnimatorController != null ? _animator.runtimeAnimatorController.name : "None")}");
            Debug.Log($"  Layer Count: {_animator.layerCount}");

            for (int i = 0; i < _animator.layerCount; i++)
            {
                Debug.Log($"  Layer {i}: {_animator.GetLayerName(i)} (Weight: {_animator.GetLayerWeight(i)})");
            }
        }
#endif
    }
}
