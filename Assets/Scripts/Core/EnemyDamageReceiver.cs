using UnityEngine;
using CityShooter.Weapons;

namespace CityShooter.Core
{
    /// <summary>
    /// Basic damage receiver component for enemies.
    /// Implements the IDamageReceiver interface from the weapon system.
    /// </summary>
    public class EnemyDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Visual Feedback")]
        [SerializeField] private bool flashOnHit = true;
        [SerializeField] private Color hitFlashColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.1f;

        [Header("Death Settings")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float deathDelay = 0f;
        [SerializeField] private GameObject deathEffectPrefab;

        private Renderer[] _renderers;
        private Color[] _originalColors;
        private bool _isDead;

        /// <summary>
        /// Event triggered when damage is taken.
        /// </summary>
        public event System.Action<float, Vector3> OnDamageTaken;

        /// <summary>
        /// Event triggered when the enemy dies.
        /// </summary>
        public event System.Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;

            // Cache renderers for hit flash
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_Color"))
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
                else if (_renderers[i].material.HasProperty("_BaseColor"))
                {
                    _originalColors[i] = _renderers[i].material.GetColor("_BaseColor");
                }
            }
        }

        /// <summary>
        /// Takes damage from a source.
        /// </summary>
        /// <param name="damage">Amount of damage to take.</param>
        /// <param name="hitPoint">World position of the hit.</param>
        /// <param name="hitDirection">Direction of the hit (from attacker).</param>
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (_isDead)
                return;

            currentHealth -= damage;

            // Trigger event
            OnDamageTaken?.Invoke(damage, hitPoint);

            // Visual feedback
            if (flashOnHit)
            {
                StartCoroutine(HitFlashCoroutine());
            }

            // Check for death
            if (currentHealth <= 0f)
            {
                Die(hitDirection);
            }
        }

        /// <summary>
        /// Performs death logic.
        /// </summary>
        private void Die(Vector3 hitDirection)
        {
            if (_isDead)
                return;

            _isDead = true;
            currentHealth = 0f;

            // Trigger death event
            OnDeath?.Invoke();

            // Spawn death effect
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            // Apply death force to rigidbody if present
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(hitDirection.normalized * 5f + Vector3.up * 2f, ForceMode.Impulse);
            }

            // Destroy or disable
            if (destroyOnDeath)
            {
                if (deathDelay > 0f)
                {
                    Destroy(gameObject, deathDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                // Just disable behavior
                enabled = false;
            }
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            // Flash to hit color
            SetRendererColors(hitFlashColor);

            yield return new WaitForSeconds(hitFlashDuration);

            // Restore original colors
            RestoreOriginalColors();
        }

        private void SetRendererColors(Color color)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                if (_renderers[i].material.HasProperty("_Color"))
                {
                    _renderers[i].material.color = color;
                }
                else if (_renderers[i].material.HasProperty("_BaseColor"))
                {
                    _renderers[i].material.SetColor("_BaseColor", color);
                }
            }
        }

        private void RestoreOriginalColors()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                if (_renderers[i].material.HasProperty("_Color"))
                {
                    _renderers[i].material.color = _originalColors[i];
                }
                else if (_renderers[i].material.HasProperty("_BaseColor"))
                {
                    _renderers[i].material.SetColor("_BaseColor", _originalColors[i]);
                }
            }
        }

        /// <summary>
        /// Heals the enemy by the specified amount.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        public void Heal(float amount)
        {
            if (_isDead)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        /// <summary>
        /// Resets the enemy to full health.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            _isDead = false;
            enabled = true;
        }

        /// <summary>
        /// Gets the current health.
        /// </summary>
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Gets the maximum health.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Gets the health percentage (0-1).
        /// </summary>
        public float HealthPercentage => currentHealth / maxHealth;

        /// <summary>
        /// Gets whether the enemy is dead.
        /// </summary>
        public bool IsDead => _isDead;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
        }
#endif
    }
}
