using System;
using System.Collections;
using UnityEngine;

namespace CityShooter.Weapons
{
    /// <summary>
    /// Main controller for the modular laser gun weapon system.
    /// Handles firing, raycast hit detection, and coordinates with VFX and animation systems.
    /// </summary>
    public class LaserGunController : MonoBehaviour
    {
        [Header("Firing Configuration")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private float fireRate = 0.15f;
        [SerializeField] private float maxRange = 100f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Input")]
        [SerializeField] private string fireButton = "Fire1";
        [SerializeField] private bool useNewInputSystem = false;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] [Range(0f, 1f)] private float fireSoundVolume = 0.8f;

        [Header("Component References")]
        [SerializeField] private LaserBoltVFX laserBoltVFX;
        [SerializeField] private EmissiveFlashController emissiveFlashController;
        [SerializeField] private WeaponAnimationController animationController;
        [SerializeField] private ImpactEffectController impactEffectController;

        private float _nextFireTime;
        private bool _isFiring;
        private Camera _mainCamera;
        private bool _isMoving;

        /// <summary>
        /// Event triggered when the weapon fires, providing hit information.
        /// </summary>
        public event Action<LaserHitInfo> OnWeaponFired;

        /// <summary>
        /// Event triggered when an enemy is hit.
        /// </summary>
        public event Action<LaserHitInfo> OnEnemyHit;

        private void Awake()
        {
            ValidateComponents();
            _mainCamera = Camera.main;

            // Auto-setup muzzle point if not assigned
            if (muzzlePoint == null)
            {
                SetupMuzzlePoint();
            }
        }

        private void Start()
        {
            // Ensure target layers include Enemy layer if not set
            if (targetLayers == 0)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1)
                {
                    targetLayers = 1 << enemyLayer;
                }
                else
                {
                    Debug.LogWarning("[LaserGunController] Enemy layer not found. Using default layer mask.");
                    targetLayers = ~0; // All layers
                }
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (useNewInputSystem)
            {
                // New Input System support - to be implemented via action callbacks
                return;
            }

            // Legacy Input System
            if (Input.GetButton(fireButton))
            {
                TryFire();
            }
        }

        /// <summary>
        /// Attempts to fire the weapon. Called by input handlers.
        /// </summary>
        public void TryFire()
        {
            if (Time.time < _nextFireTime)
                return;

            _nextFireTime = Time.time + fireRate;
            Fire();
        }

        /// <summary>
        /// Executes the firing logic including raycast, VFX, and animation triggers.
        /// </summary>
        private void Fire()
        {
            _isFiring = true;

            // Get fire direction from camera center for accurate aiming
            Vector3 fireOrigin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 fireDirection = GetFireDirection();

            // Perform raycast
            LaserHitInfo hitInfo = PerformRaycast(fireOrigin, fireDirection);

            // Trigger VFX
            TriggerLaserBolt(fireOrigin, hitInfo);
            TriggerEmissiveFlash();

            // Trigger animation
            TriggerFireAnimation();

            // Play sound
            PlayFireSound();

            // Process hit
            ProcessHit(hitInfo);

            // Raise events
            OnWeaponFired?.Invoke(hitInfo);

            StartCoroutine(ResetFiringState());
        }

        /// <summary>
        /// Gets the firing direction, typically from camera center for FPS-style aiming.
        /// </summary>
        private Vector3 GetFireDirection()
        {
            if (_mainCamera != null)
            {
                // Fire from screen center for accurate FPS aiming
                Ray cameraRay = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                return cameraRay.direction;
            }

            return muzzlePoint != null ? muzzlePoint.forward : transform.forward;
        }

        /// <summary>
        /// Performs the raycast to detect hits.
        /// </summary>
        private LaserHitInfo PerformRaycast(Vector3 origin, Vector3 direction)
        {
            LaserHitInfo hitInfo = new LaserHitInfo
            {
                Origin = origin,
                Direction = direction,
                MaxRange = maxRange
            };

            // Use camera position for raycast origin to match visual aim
            Vector3 rayOrigin = _mainCamera != null ? _mainCamera.transform.position : origin;

            if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, maxRange, targetLayers))
            {
                hitInfo.DidHit = true;
                hitInfo.HitPoint = hit.point;
                hitInfo.HitNormal = hit.normal;
                hitInfo.HitDistance = hit.distance;
                hitInfo.HitCollider = hit.collider;
                hitInfo.HitTransform = hit.transform;

                // Check if we hit an enemy
                hitInfo.IsEnemyHit = hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy");

                // Try to get damage receiver component
                hitInfo.DamageReceiver = hit.collider.GetComponent<IDamageReceiver>();
            }
            else
            {
                // Set end point at max range if no hit
                hitInfo.HitPoint = rayOrigin + direction * maxRange;
                hitInfo.HitDistance = maxRange;
            }

            return hitInfo;
        }

        /// <summary>
        /// Triggers the laser bolt visual effect.
        /// </summary>
        private void TriggerLaserBolt(Vector3 origin, LaserHitInfo hitInfo)
        {
            if (laserBoltVFX != null)
            {
                laserBoltVFX.FireLaserBolt(origin, hitInfo.HitPoint);
            }
        }

        /// <summary>
        /// Triggers the emissive flash on fuel and barrel materials.
        /// </summary>
        private void TriggerEmissiveFlash()
        {
            if (emissiveFlashController != null)
            {
                emissiveFlashController.TriggerFlash();
            }
        }

        /// <summary>
        /// Triggers the appropriate fire animation based on movement state.
        /// </summary>
        private void TriggerFireAnimation()
        {
            if (animationController != null)
            {
                animationController.TriggerFireAnimation(_isMoving);
            }
        }

        /// <summary>
        /// Plays the fire sound effect.
        /// </summary>
        private void PlayFireSound()
        {
            if (audioSource != null && fireSound != null)
            {
                audioSource.PlayOneShot(fireSound, fireSoundVolume);
            }
        }

        /// <summary>
        /// Processes the hit, applying damage and spawning impact effects.
        /// </summary>
        private void ProcessHit(LaserHitInfo hitInfo)
        {
            if (!hitInfo.DidHit)
                return;

            // Spawn impact effect
            if (impactEffectController != null)
            {
                impactEffectController.SpawnImpactEffect(hitInfo.HitPoint, hitInfo.HitNormal, hitInfo.IsEnemyHit);
            }

            // Apply damage
            if (hitInfo.DamageReceiver != null)
            {
                hitInfo.DamageReceiver.TakeDamage(damage, hitInfo.HitPoint, hitInfo.Direction);
            }

            // Raise enemy hit event
            if (hitInfo.IsEnemyHit)
            {
                OnEnemyHit?.Invoke(hitInfo);
            }
        }

        private IEnumerator ResetFiringState()
        {
            yield return new WaitForSeconds(0.1f);
            _isFiring = false;
        }

        /// <summary>
        /// Sets the movement state for animation blending.
        /// </summary>
        /// <param name="isMoving">Whether the player is currently moving.</param>
        public void SetMovementState(bool isMoving)
        {
            _isMoving = isMoving;
        }

        /// <summary>
        /// Validates that required components are assigned.
        /// </summary>
        private void ValidateComponents()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        /// <summary>
        /// Auto-creates a muzzle point if not assigned.
        /// </summary>
        private void SetupMuzzlePoint()
        {
            // Try to find existing muzzle point
            Transform existingMuzzle = transform.Find("MuzzlePoint");
            if (existingMuzzle != null)
            {
                muzzlePoint = existingMuzzle;
                return;
            }

            // Try to find barrel sub-object
            Transform barrel = FindChildRecursive(transform, "barrel");
            if (barrel != null)
            {
                GameObject muzzleObj = new GameObject("MuzzlePoint");
                muzzleObj.transform.SetParent(barrel);
                muzzleObj.transform.localPosition = Vector3.forward * 0.5f; // Offset to barrel tip
                muzzleObj.transform.localRotation = Quaternion.identity;
                muzzlePoint = muzzleObj.transform;
                return;
            }

            // Create at gun position as fallback
            GameObject fallbackMuzzle = new GameObject("MuzzlePoint");
            fallbackMuzzle.transform.SetParent(transform);
            fallbackMuzzle.transform.localPosition = Vector3.forward;
            fallbackMuzzle.transform.localRotation = Quaternion.identity;
            muzzlePoint = fallbackMuzzle.transform;

            Debug.LogWarning("[LaserGunController] Muzzle point auto-created. Consider assigning it manually for accuracy.");
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;

                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Gets whether the weapon is currently firing.
        /// </summary>
        public bool IsFiring => _isFiring;

        /// <summary>
        /// Gets or sets the weapon damage.
        /// </summary>
        public float Damage
        {
            get => damage;
            set => damage = value;
        }

        /// <summary>
        /// Gets or sets the fire rate.
        /// </summary>
        public float FireRate
        {
            get => fireRate;
            set => fireRate = Mathf.Max(0.01f, value);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw firing range
            Gizmos.color = Color.cyan;
            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 direction = muzzlePoint != null ? muzzlePoint.forward : transform.forward;
            Gizmos.DrawLine(origin, origin + direction * maxRange);
            Gizmos.DrawWireSphere(origin + direction * maxRange, 0.1f);
        }
#endif
    }

    /// <summary>
    /// Contains information about a laser hit.
    /// </summary>
    public struct LaserHitInfo
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float MaxRange;
        public bool DidHit;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float HitDistance;
        public Collider HitCollider;
        public Transform HitTransform;
        public bool IsEnemyHit;
        public IDamageReceiver DamageReceiver;
    }

    /// <summary>
    /// Interface for objects that can receive damage.
    /// </summary>
    public interface IDamageReceiver
    {
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection);
    }
}
