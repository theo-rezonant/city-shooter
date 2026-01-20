using UnityEngine;
using CityShooter.Interfaces;

namespace CityShooter.Combat
{
    /// <summary>
    /// Modular Laser Combat System for the city shooter.
    /// Handles raycasting for laser weapons and damage application to IDamageable targets.
    /// Uses the "Enemy" physics layer for hit detection.
    /// </summary>
    public class LaserCombatSystem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Laser Settings")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private float range = 100f;
        [SerializeField] private float fireRate = 0.15f;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool useMainCamera = true;

        [Header("Visual Effects")]
        [SerializeField] private LineRenderer laserBeam;
        [SerializeField] private float beamDuration = 0.05f;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private Color laserColor = Color.red;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip hitSound;

        [Header("Input")]
        [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
        [SerializeField] private bool automaticFire = true;

        #endregion

        #region Private Fields

        private Camera _mainCamera;
        private float _nextFireTime;
        private float _beamHideTime;

        #endregion

        #region Events

        public delegate void LaserHitEvent(RaycastHit hit, IDamageable damageable);
        public event LaserHitEvent OnLaserHit;
        public event System.Action OnLaserFired;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (useMainCamera)
            {
                _mainCamera = Camera.main;
            }

            if (firePoint == null && _mainCamera != null)
            {
                firePoint = _mainCamera.transform;
            }

            // Configure line renderer if present
            if (laserBeam != null)
            {
                laserBeam.enabled = false;
                laserBeam.startColor = laserColor;
                laserBeam.endColor = laserColor;
                laserBeam.startWidth = 0.05f;
                laserBeam.endWidth = 0.02f;
            }

            // Set up default hit layers if not configured
            if (hitLayers == 0)
            {
                // Default to hitting everything except IgnoreRaycast
                hitLayers = ~LayerMask.GetMask("Ignore Raycast");
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateBeamVisibility();
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            bool firePressed = automaticFire ? Input.GetKey(fireKey) : Input.GetKeyDown(fireKey);

            if (firePressed && Time.time >= _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + fireRate;
            }
        }

        #endregion

        #region Firing

        /// <summary>
        /// Fires the laser weapon.
        /// </summary>
        public void Fire()
        {
            if (firePoint == null)
            {
                Debug.LogWarning("LaserCombatSystem: No fire point set!");
                return;
            }

            Vector3 origin = firePoint.position;
            Vector3 direction = firePoint.forward;

            // If using camera, aim toward screen center
            if (useMainCamera && _mainCamera != null)
            {
                Ray centerRay = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                origin = centerRay.origin;
                direction = centerRay.direction;
            }

            // Perform raycast
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitLayers))
            {
                ProcessHit(hit, origin);
            }
            else
            {
                // Draw beam to max range
                ShowLaserBeam(origin, origin + direction * range);
            }

            // Play fire sound
            PlaySound(fireSound);

            OnLaserFired?.Invoke();
        }

        /// <summary>
        /// Fires the laser in a specific direction.
        /// </summary>
        /// <param name="origin">Origin point of the laser.</param>
        /// <param name="direction">Direction to fire.</param>
        public void Fire(Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitLayers))
            {
                ProcessHit(hit, origin);
            }
            else
            {
                ShowLaserBeam(origin, origin + direction * range);
            }

            PlaySound(fireSound);
            OnLaserFired?.Invoke();
        }

        #endregion

        #region Hit Processing

        private void ProcessHit(RaycastHit hit, Vector3 origin)
        {
            // Show laser beam
            ShowLaserBeam(origin, hit.point);

            // Spawn hit effect
            SpawnHitEffect(hit.point, hit.normal);

            // Try to get IDamageable component
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();

            // Also check parent objects if not found on collider
            if (damageable == null)
            {
                damageable = hit.collider.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                // Apply damage
                damageable.TakeDamage(damage, hit.point);

                // Play hit sound
                PlaySound(hitSound);

                Debug.Log($"LaserCombatSystem: Hit {hit.collider.gameObject.name} for {damage} damage");

                // Fire event
                OnLaserHit?.Invoke(hit, damageable);
            }
            else
            {
                Debug.Log($"LaserCombatSystem: Hit {hit.collider.gameObject.name} (not damageable)");
            }
        }

        #endregion

        #region Visual Effects

        private void ShowLaserBeam(Vector3 start, Vector3 end)
        {
            if (laserBeam == null) return;

            laserBeam.enabled = true;
            laserBeam.SetPosition(0, start);
            laserBeam.SetPosition(1, end);
            _beamHideTime = Time.time + beamDuration;
        }

        private void UpdateBeamVisibility()
        {
            if (laserBeam != null && laserBeam.enabled && Time.time >= _beamHideTime)
            {
                laserBeam.enabled = false;
            }
        }

        private void SpawnHitEffect(Vector3 position, Vector3 normal)
        {
            if (hitEffectPrefab == null) return;

            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));

            // Auto-destroy after a short time
            Destroy(effect, 2f);
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Sets the damage per shot.
        /// </summary>
        public void SetDamage(float newDamage)
        {
            damage = Mathf.Max(0, newDamage);
        }

        /// <summary>
        /// Sets the fire rate (time between shots).
        /// </summary>
        public void SetFireRate(float newFireRate)
        {
            fireRate = Mathf.Max(0.01f, newFireRate);
        }

        /// <summary>
        /// Sets the maximum range.
        /// </summary>
        public void SetRange(float newRange)
        {
            range = Mathf.Max(1, newRange);
        }

        /// <summary>
        /// Sets the layers that can be hit.
        /// </summary>
        public void SetHitLayers(LayerMask layers)
        {
            hitLayers = layers;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (firePoint == null) return;

            Gizmos.color = laserColor;
            Gizmos.DrawRay(firePoint.position, firePoint.forward * range);
        }

        #endregion
    }
}
