using System;
using UnityEngine;
using UnityEngine.AI;

namespace CityShooter.Enemy
{
    /// <summary>
    /// Main AI controller for Soldier enemies.
    /// Implements a state machine with Idle, Chase, Attack, React, and Death states.
    /// Uses NavMeshAgent for pathfinding on the town4new NavMesh.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class SoldierAI : MonoBehaviour
    {
        #region Enums

        public enum SoldierState
        {
            Idle,
            Chase,
            Attack,
            React,
            Death
        }

        #endregion

        #region Serialized Fields

        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 20f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float fieldOfView = 120f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float stoppingDistance = 1.5f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Combat Settings")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float hitReactionDuration = 0.5f;

        [Header("References")]
        [SerializeField] private Transform playerTarget;

        #endregion

        #region Private Fields

        private NavMeshAgent _navMeshAgent;
        private Animator _animator;
        private EnemyHealth _health;

        private SoldierState _currentState = SoldierState.Idle;
        private SoldierState _previousState;

        private float _lastAttackTime;
        private float _hitReactionEndTime;
        private bool _isReacting;

        // Animator parameter hashes for performance
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimReact = Animator.StringToHash("React");
        private static readonly int AnimDeath = Animator.StringToHash("Death");
        private static readonly int AnimIsAlive = Animator.StringToHash("IsAlive");

        #endregion

        #region Events

        public event Action<SoldierState> OnStateChanged;
        public event Action OnDeath;

        #endregion

        #region Properties

        public SoldierState CurrentState => _currentState;
        public bool IsAlive => _health == null || _health.IsAlive;
        public Transform Target => playerTarget;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _health = GetComponent<EnemyHealth>();

            ConfigureNavMeshAgent();
        }

        private void Start()
        {
            // Try to find player if not assigned
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                }
            }

            // Subscribe to health events
            if (_health != null)
            {
                _health.OnDamageTaken += HandleDamageTaken;
                _health.OnDeath += HandleDeath;
            }

            // Set animator alive state
            if (_animator != null)
            {
                _animator.SetBool(AnimIsAlive, true);
            }

            TransitionToState(SoldierState.Idle);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Handle hit reaction timing
            if (_isReacting && Time.time >= _hitReactionEndTime)
            {
                _isReacting = false;
                // Return to previous state after reaction
                TransitionToState(_previousState != SoldierState.React ? _previousState : SoldierState.Idle);
            }

            if (_isReacting) return;

            UpdateStateMachine();
            UpdateAnimator();
        }

        private void OnDestroy()
        {
            // Unsubscribe from health events
            if (_health != null)
            {
                _health.OnDamageTaken -= HandleDamageTaken;
                _health.OnDeath -= HandleDeath;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw field of view
            Vector3 forward = transform.forward;
            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2, 0) * forward;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
            Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
        }

        #endregion

        #region Configuration

        private void ConfigureNavMeshAgent()
        {
            if (_navMeshAgent == null) return;

            _navMeshAgent.speed = walkSpeed;
            _navMeshAgent.stoppingDistance = stoppingDistance;
            _navMeshAgent.angularSpeed = 120f;
            _navMeshAgent.acceleration = 8f;
        }

        /// <summary>
        /// Sets the player target for this soldier to track.
        /// </summary>
        public void SetTarget(Transform target)
        {
            playerTarget = target;
        }

        #endregion

        #region State Machine

        private void UpdateStateMachine()
        {
            switch (_currentState)
            {
                case SoldierState.Idle:
                    UpdateIdleState();
                    break;
                case SoldierState.Chase:
                    UpdateChaseState();
                    break;
                case SoldierState.Attack:
                    UpdateAttackState();
                    break;
                case SoldierState.React:
                    // Handled by timer in Update
                    break;
                case SoldierState.Death:
                    // No update needed
                    break;
            }
        }

        private void UpdateIdleState()
        {
            _navMeshAgent.isStopped = true;

            // Check for player detection
            if (CanSeePlayer())
            {
                TransitionToState(SoldierState.Chase);
            }
        }

        private void UpdateChaseState()
        {
            if (playerTarget == null)
            {
                TransitionToState(SoldierState.Idle);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Check if within attack range
            if (distanceToPlayer <= attackRange)
            {
                TransitionToState(SoldierState.Attack);
                return;
            }

            // Check if player escaped detection range
            if (distanceToPlayer > detectionRange && !CanSeePlayer())
            {
                TransitionToState(SoldierState.Idle);
                return;
            }

            // Continue chasing
            _navMeshAgent.isStopped = false;
            _navMeshAgent.speed = runSpeed;
            _navMeshAgent.SetDestination(playerTarget.position);
        }

        private void UpdateAttackState()
        {
            if (playerTarget == null)
            {
                TransitionToState(SoldierState.Idle);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Stop and face the player
            _navMeshAgent.isStopped = true;
            RotateTowardsTarget(playerTarget.position);

            // Check if player moved out of attack range
            if (distanceToPlayer > attackRange * 1.2f) // Small buffer to prevent jitter
            {
                TransitionToState(SoldierState.Chase);
                return;
            }

            // Attempt attack
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }

        private void TransitionToState(SoldierState newState)
        {
            if (_currentState == SoldierState.Death) return; // Can't transition from death

            _previousState = _currentState;
            _currentState = newState;

            OnEnterState(newState);
            OnStateChanged?.Invoke(newState);
        }

        private void OnEnterState(SoldierState state)
        {
            switch (state)
            {
                case SoldierState.Idle:
                    _navMeshAgent.isStopped = true;
                    _navMeshAgent.speed = walkSpeed;
                    break;

                case SoldierState.Chase:
                    _navMeshAgent.isStopped = false;
                    _navMeshAgent.speed = runSpeed;
                    break;

                case SoldierState.Attack:
                    _navMeshAgent.isStopped = true;
                    break;

                case SoldierState.React:
                    _navMeshAgent.isStopped = true;
                    _isReacting = true;
                    _hitReactionEndTime = Time.time + hitReactionDuration;
                    TriggerHitReaction();
                    break;

                case SoldierState.Death:
                    _navMeshAgent.isStopped = true;
                    _navMeshAgent.enabled = false;
                    TriggerDeathAnimation();
                    OnDeath?.Invoke();
                    break;
            }
        }

        #endregion

        #region Detection

        private bool CanSeePlayer()
        {
            if (playerTarget == null) return false;

            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Check if within detection range
            if (distanceToPlayer > detectionRange) return false;

            // Check if within field of view
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > fieldOfView / 2) return false;

            // Raycast to check for obstacles
            Vector3 eyePosition = transform.position + Vector3.up * 1.6f; // Approximate eye height
            if (Physics.Raycast(eyePosition, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleLayer))
            {
                // Check if the hit object is not the player
                if (!hit.collider.CompareTag("Player"))
                {
                    return false;
                }
            }

            return true;
        }

        private void RotateTowardsTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Combat

        private void PerformAttack()
        {
            _lastAttackTime = Time.time;

            if (_animator != null)
            {
                _animator.SetTrigger(AnimAttack);
            }

            // Attack damage logic can be added here or handled via animation events
            Debug.Log($"{gameObject.name} attacks!");
        }

        /// <summary>
        /// Called when the soldier takes damage. Triggers hit reaction animation.
        /// </summary>
        public void TriggerHitReaction()
        {
            if (!IsAlive) return;

            if (_animator != null)
            {
                // Use trigger for immediate interrupt behavior
                _animator.SetTrigger(AnimReact);
            }
        }

        private void TriggerDeathAnimation()
        {
            if (_animator != null)
            {
                _animator.SetBool(AnimIsAlive, false);
                _animator.SetTrigger(AnimDeath);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleDamageTaken(float damage, Vector3 hitPoint)
        {
            if (!IsAlive) return;

            // Only trigger react state if not already dead
            if (_health != null && _health.IsAlive)
            {
                TransitionToState(SoldierState.React);
            }
        }

        private void HandleDeath()
        {
            TransitionToState(SoldierState.Death);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the soldier into alert state and starts chasing.
        /// </summary>
        public void AlertToPlayer()
        {
            if (IsAlive && playerTarget != null)
            {
                TransitionToState(SoldierState.Chase);
            }
        }

        /// <summary>
        /// Resets the soldier to idle state.
        /// </summary>
        public void ResetToIdle()
        {
            if (IsAlive)
            {
                TransitionToState(SoldierState.Idle);
            }
        }

        #endregion

        #region Animation

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            float speed = _navMeshAgent.velocity.magnitude;
            _animator.SetFloat(AnimSpeed, speed);
            _animator.SetBool(AnimIsMoving, speed > 0.1f);
        }

        #endregion
    }
}
