using UnityEngine;

namespace CityShooter.Core
{
    /// <summary>
    /// Player health management with HUD integration.
    /// Attach to the player character.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float healthRegenRate = 0f;
        [SerializeField] private float regenDelay = 5f;

        [Header("Damage Settings")]
        [SerializeField] private float invulnerabilityDuration = 0.1f;

        private float lastDamageTime;
        private float lastRegenTime;
        private bool isInvulnerable;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsDead => currentHealth <= 0;

        private void Start()
        {
            currentHealth = maxHealth;
            CombatEvents.InvokeHealthChanged(currentHealth, maxHealth);
        }

        private void Update()
        {
            if (healthRegenRate > 0 && currentHealth < maxHealth && !IsDead)
            {
                if (Time.time - lastDamageTime >= regenDelay)
                {
                    Regenerate();
                }
            }

            // Reset invulnerability
            if (isInvulnerable && Time.time - lastDamageTime >= invulnerabilityDuration)
            {
                isInvulnerable = false;
            }
        }

        private void Regenerate()
        {
            float regenAmount = healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + regenAmount, maxHealth);
            CombatEvents.InvokeHealthChanged(currentHealth, maxHealth);
        }

        /// <summary>
        /// Apply damage to the player from a damage source.
        /// </summary>
        public void TakeDamage(float damage, Vector3 damageSourcePosition)
        {
            if (IsDead || isInvulnerable) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            lastDamageTime = Time.time;
            isInvulnerable = true;

            // Notify HUD
            CombatEvents.InvokeHealthChanged(currentHealth, maxHealth);
            CombatEvents.InvokePlayerDamaged(damageSourcePosition);

            if (IsDead)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// Apply damage without direction indicator.
        /// </summary>
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, transform.position + transform.forward);
        }

        /// <summary>
        /// Heal the player.
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            CombatEvents.InvokeHealthChanged(currentHealth, maxHealth);
        }

        /// <summary>
        /// Set health to a specific value.
        /// </summary>
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            CombatEvents.InvokeHealthChanged(currentHealth, maxHealth);
        }

        /// <summary>
        /// Reset health to maximum.
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            CombatEvents.InvokeHealthChanged(currentHealth, maxHealth);
        }

        protected virtual void OnDeath()
        {
            Debug.Log($"[PlayerHealth] Player died!");
            // Override in subclass for death handling
        }
    }
}
