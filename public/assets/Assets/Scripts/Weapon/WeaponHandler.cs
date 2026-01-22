using UnityEngine;
using CityShooter.Camera;
using CityShooter.Player;

namespace CityShooter.Weapon
{
    /// <summary>
    /// Handles weapon behavior including firing, animation integration, and camera effects.
    /// The weapon should be parented to the Main Camera to follow the view transform.
    /// </summary>
    public class WeaponHandler : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private float fireRate = 0.2f;
        [SerializeField] private float maxRange = 100f;
        [SerializeField] private LayerMask hitLayers = -1;

        [Header("Visual Effects")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private LineRenderer laserBeamRenderer;
        [SerializeField] private float laserDisplayTime = 0.05f;

        [Header("Weapon Position (Blender Z-up to Unity Y-up)")]
        [SerializeField] private Vector3 weaponPositionOffset = new Vector3(0.3f, -0.2f, 0.5f);
        [SerializeField] private Vector3 weaponRotationOffset = new Vector3(0f, 0f, 0f);

        [Header("References")]
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private PlayerAnimationController animationController;

        // State
        private float nextFireTime;
        private bool isFiring;
        private bool triggerHeld;

        // Events for combat system integration
        public event System.Action<RaycastHit> OnHit;
        public event System.Action OnFire;
        public event System.Action OnStopFire;

        public bool IsFiring => isFiring;

        private void Awake()
        {
            // Apply position offset to compensate for Blender Z-up export
            ApplyCoordinateCorrection();

            if (cameraShake == null)
            {
                cameraShake = GetComponentInParent<CameraShake>();
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateLaserBeam();
        }

        /// <summary>
        /// Apply coordinate system correction for Blender-exported models.
        /// Blender uses Z-up, Unity uses Y-up.
        /// </summary>
        private void ApplyCoordinateCorrection()
        {
            // Position the weapon in front and to the side of the camera
            transform.localPosition = weaponPositionOffset;
            transform.localRotation = Quaternion.Euler(weaponRotationOffset);
        }

        private void HandleInput()
        {
            // Fire input (left mouse button)
            bool firePressed = Input.GetMouseButton(0);

            if (firePressed)
            {
                if (!triggerHeld)
                {
                    triggerHeld = true;
                    StartFiring();
                }
                TryFire();
            }
            else if (triggerHeld)
            {
                triggerHeld = false;
                StopFiring();
            }
        }

        private void StartFiring()
        {
            isFiring = true;
            animationController?.SetFiring(true);
            OnFire?.Invoke();
        }

        private void StopFiring()
        {
            isFiring = false;
            animationController?.SetFiring(false);
            OnStopFire?.Invoke();
        }

        private void TryFire()
        {
            if (Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + fireRate;
            Fire();
        }

        private void Fire()
        {
            // Camera shake for recoil
            cameraShake?.PlayFireShake();

            // Muzzle flash
            if (muzzleFlashPrefab != null && muzzlePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
                Destroy(flash, 0.1f);
            }

            // Raycast for hit detection
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitLayers))
            {
                // Hit something
                OnHit?.Invoke(hit);

                // Show impact effect
                if (impactEffectPrefab != null)
                {
                    GameObject impact = Instantiate(
                        impactEffectPrefab,
                        hit.point,
                        Quaternion.LookRotation(hit.normal)
                    );
                    Destroy(impact, 2f);
                }

                // Update laser beam to hit point
                ShowLaserBeam(muzzlePoint?.position ?? transform.position, hit.point);
            }
            else
            {
                // No hit - show laser beam to max range
                Vector3 endPoint = ray.origin + ray.direction * maxRange;
                ShowLaserBeam(muzzlePoint?.position ?? transform.position, endPoint);
            }
        }

        private void ShowLaserBeam(Vector3 start, Vector3 end)
        {
            if (laserBeamRenderer != null)
            {
                laserBeamRenderer.enabled = true;
                laserBeamRenderer.SetPosition(0, start);
                laserBeamRenderer.SetPosition(1, end);

                CancelInvoke(nameof(HideLaserBeam));
                Invoke(nameof(HideLaserBeam), laserDisplayTime);
            }
        }

        private void HideLaserBeam()
        {
            if (laserBeamRenderer != null)
            {
                laserBeamRenderer.enabled = false;
            }
        }

        private void UpdateLaserBeam()
        {
            // Could add continuous laser sight here if needed
        }

        /// <summary>
        /// Set the weapon's local position offset.
        /// </summary>
        public void SetPositionOffset(Vector3 offset)
        {
            weaponPositionOffset = offset;
            transform.localPosition = offset;
        }

        /// <summary>
        /// Set the weapon's local rotation offset.
        /// </summary>
        public void SetRotationOffset(Vector3 eulerAngles)
        {
            weaponRotationOffset = eulerAngles;
            transform.localRotation = Quaternion.Euler(eulerAngles);
        }

        /// <summary>
        /// Manually trigger a fire (e.g., from AI or automated systems).
        /// </summary>
        public void ManualFire()
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                Fire();
            }
        }
    }
}
