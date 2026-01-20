using System;
using UnityEngine;

namespace CityShooter.Enemy
{
    /// <summary>
    /// Health component for enemy soldiers.
    /// Manages health, damage, and death states with event-driven architecture.
    /// Implements IDamageable interface for combat system integration.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, Interfaces.IDamageable
    {
        #region Serialized Fields

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Damage Settings")]
        [SerializeField] private bool isInvulnerable;
        [SerializeField] private float invulnerabilityDuration = 0.2f;

        [Header("Death Settings")]
        [SerializeField] private float deathDelay = 3f;
        [SerializeField] private bool destroyOnDeath = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo;

        #endregion

        #region Private Fields

        private float _lastDamageTime;
        private bool _isDead;
        private Collider[] _colliders;

        #endregion

        #region Events

        /// <summary>
        /// Fired when damage is taken. Parameters: damage amount, hit point.
        /// </summary>
        public event Action<float, Vector3> OnDamageTaken;

        /// <summary>
        /// Fired when health changes. Parameters: current health, max health.
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Fired when the enemy dies.
        /// </summary>
        public event Action OnDeath;

        #endregion

        #region Properties

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsAlive => !_isDead && currentHealth > 0;
        public bool IsInvulnerable => isInvulnerable;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            currentHealth = maxHealth;
            _colliders = GetComponentsInChildren<Collider>();
        }

        private void OnValidate()
        {
            // Ensure current health doesn't exceed max in editor
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        #endregion

        #region IDamageable Implementation

        /// <summary>
        /// Applies damage to this enemy.
        /// </summary>
        /// <param name="damage">Amount of damage to apply.</param>
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, transform.position);
        }

        /// <summary>
        /// Applies damage to this enemy with hit location.
        /// </summary>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="hitPoint">World position where the hit occurred.</param>
        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (_isDead) return;
            if (isInvulnerable) return;
            if (damage <= 0) return;

            // Apply damage
            currentHealth = Mathf.Max(0f, currentHealth - damage);

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
            }

            // Fire events
            OnDamageTaken?.Invoke(damage, hitPoint);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Apply brief invulnerability to prevent damage stacking
            if (invulnerabilityDuration > 0)
            {
                StartCoroutine(ApplyInvulnerability());
            }

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Gets the current health percentage (0-1).
        /// </summary>
        public float GetHealthPercentage()
        {
            return HealthPercentage;
        }

        /// <summary>
        /// Checks if the entity is still alive.
        /// </summary>
        public bool GetIsAlive()
        {
            return IsAlive;
        }

        #endregion

        #region Health Management

        /// <summary>
        /// Heals the enemy by the specified amount.
        /// </summary>
        /// <param name="amount">Amount of health to restore.</param>
        public void Heal(float amount)
        {
            if (_isDead) return;
            if (amount <= 0) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} healed for {amount}. Health: {currentHealth}/{maxHealth}");
            }
        }

        /// <summary>
        /// Sets the current health to a specific value.
        /// </summary>
        /// <param name="health">New health value.</param>
        public void SetHealth(float health)
        {
            if (_isDead) return;

            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Sets the maximum health and optionally scales current health.
        /// </summary>
        /// <param name="newMaxHealth">New maximum health value.</param>
        /// <param name="scaleCurrentHealth">If true, current health scales proportionally.</param>
        public void SetMaxHealth(float newMaxHealth, bool scaleCurrentHealth = false)
        {
            if (newMaxHealth <= 0) return;

            if (scaleCurrentHealth)
            {
                float ratio = currentHealth / maxHealth;
                maxHealth = newMaxHealth;
                currentHealth = maxHealth * ratio;
            }
            else
            {
                maxHealth = newMaxHealth;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Resets health to maximum and revives if dead.
        /// </summary>
        public void ResetHealth()
        {
            _isDead = false;
            currentHealth = maxHealth;
            EnableColliders(true);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        #endregion

        #region Death

        private void Die()
        {
            if (_isDead) return;

            _isDead = true;

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name} has died.");
            }

            // Disable colliders to prevent further interactions
            EnableColliders(false);

            // Fire death event
            OnDeath?.Invoke();

            // Handle destruction
            if (destroyOnDeath)
            {
                Destroy(gameObject, deathDelay);
            }
        }

        /// <summary>
        /// Instantly kills this enemy.
        /// </summary>
        public void Kill()
        {
            if (_isDead) return;
            currentHealth = 0;
            Die();
        }

        #endregion

        #region Invulnerability

        private System.Collections.IEnumerator ApplyInvulnerability()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(invulnerabilityDuration);
            isInvulnerable = false;
        }

        /// <summary>
        /// Sets the invulnerability state.
        /// </summary>
        /// <param name="invulnerable">Whether the enemy should be invulnerable.</param>
        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        #endregion

        #region Helpers

        private void EnableColliders(bool enable)
        {
            foreach (var col in _colliders)
            {
                if (col != null)
                {
                    col.enabled = enable;
                }
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to test damage.
        /// </summary>
        [ContextMenu("Test Damage (10)")]
        private void TestDamage()
        {
            TakeDamage(10f);
        }

        /// <summary>
        /// Editor-only method to test kill.
        /// </summary>
        [ContextMenu("Test Kill")]
        private void TestKill()
        {
            Kill();
        }
#endif

        #endregion
    }
}
