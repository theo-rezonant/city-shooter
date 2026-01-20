using UnityEngine;
using CityShooter.Core;

namespace CityShooter.UI
{
    /// <summary>
    /// Main HUD Manager that coordinates all UI elements.
    /// Attach to the main HUD Canvas.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("HUD Components")]
        [SerializeField] private DynamicCrosshair crosshair;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private AmmoCounter ammoCounter;
        [SerializeField] private HitMarker hitMarker;
        [SerializeField] private DamageIndicator damageIndicator;

        [Header("Canvas Settings")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Camera hudCamera;
        [SerializeField] private float canvasTilt = 5f;
        [SerializeField] private Vector3 canvasOffset = new Vector3(0f, 0f, 0.1f);

        private static HUDManager _instance;
        public static HUDManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            InitializeCanvas();
            ValidateComponents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeCanvas()
        {
            if (hudCanvas != null && hudCamera != null)
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                hudCanvas.worldCamera = hudCamera;
                hudCanvas.planeDistance = 1f;

                // Apply slight tilt for floating effect
                if (canvasTilt != 0f)
                {
                    hudCanvas.transform.localRotation = Quaternion.Euler(canvasTilt, 0f, 0f);
                }

                hudCanvas.transform.localPosition += canvasOffset;
            }
        }

        private void ValidateComponents()
        {
            if (crosshair == null) Debug.LogWarning("[HUDManager] DynamicCrosshair not assigned!");
            if (healthBar == null) Debug.LogWarning("[HUDManager] HealthBar not assigned!");
            if (ammoCounter == null) Debug.LogWarning("[HUDManager] AmmoCounter not assigned!");
            if (hitMarker == null) Debug.LogWarning("[HUDManager] HitMarker not assigned!");
            if (damageIndicator == null) Debug.LogWarning("[HUDManager] DamageIndicator not assigned!");
        }

        private void SubscribeToEvents()
        {
            CombatEvents.OnPlayerFire += HandlePlayerFire;
            CombatEvents.OnEnemyHit += HandleEnemyHit;
            CombatEvents.OnAmmoChanged += HandleAmmoChanged;
            CombatEvents.OnHealthChanged += HandleHealthChanged;
            CombatEvents.OnPlayerDamaged += HandlePlayerDamaged;
            CombatEvents.OnPlayerMovementChanged += HandlePlayerMovement;
            CombatEvents.OnFiringStateChanged += HandleFiringStateChanged;
        }

        private void UnsubscribeFromEvents()
        {
            CombatEvents.OnPlayerFire -= HandlePlayerFire;
            CombatEvents.OnEnemyHit -= HandleEnemyHit;
            CombatEvents.OnAmmoChanged -= HandleAmmoChanged;
            CombatEvents.OnHealthChanged -= HandleHealthChanged;
            CombatEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            CombatEvents.OnPlayerMovementChanged -= HandlePlayerMovement;
            CombatEvents.OnFiringStateChanged -= HandleFiringStateChanged;
        }

        // ==================== EVENT HANDLERS ====================

        private void HandlePlayerFire()
        {
            crosshair?.TriggerFireExpansion();
        }

        private void HandleEnemyHit(Vector3 hitPoint)
        {
            hitMarker?.ShowHitMarker();
        }

        private void HandleAmmoChanged(int current, int max)
        {
            ammoCounter?.UpdateAmmo(current, max);
        }

        private void HandleHealthChanged(float current, float max)
        {
            healthBar?.UpdateHealth(current, max);
        }

        private void HandlePlayerDamaged(Vector3 damageSourcePosition)
        {
            damageIndicator?.ShowDamageDirection(damageSourcePosition);
        }

        private void HandlePlayerMovement(bool isMoving, float speed)
        {
            crosshair?.SetMovementState(isMoving, speed);
        }

        private void HandleFiringStateChanged(bool isFiring)
        {
            crosshair?.SetFiringState(isFiring);
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Initialize HUD with player starting values.
        /// </summary>
        public void InitializeHUD(float maxHealth, int maxAmmo)
        {
            healthBar?.UpdateHealth(maxHealth, maxHealth);
            ammoCounter?.UpdateAmmo(maxAmmo, maxAmmo);
        }

        /// <summary>
        /// Show or hide the entire HUD.
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            if (hudCanvas != null)
            {
                hudCanvas.enabled = visible;
            }
        }

        /// <summary>
        /// Reference to player transform for damage direction calculations.
        /// </summary>
        public void SetPlayerTransform(Transform playerTransform)
        {
            if (damageIndicator != null)
            {
                damageIndicator.SetPlayerTransform(playerTransform);
            }
        }
    }
}
