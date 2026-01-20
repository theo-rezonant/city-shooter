using UnityEngine;

namespace CityShooter.Interfaces
{
    /// <summary>
    /// Interface for objects that can receive damage.
    /// Implement this interface on any component that should respond to the combat system's raycast hits.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this object.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        void TakeDamage(float damage);

        /// <summary>
        /// Apply damage to this object with hit location information.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        /// <param name="hitPoint">The world-space position where the hit occurred.</param>
        void TakeDamage(float damage, Vector3 hitPoint);

        /// <summary>
        /// Get the current health as a percentage (0.0 to 1.0).
        /// </summary>
        /// <returns>Health percentage between 0 and 1.</returns>
        float GetHealthPercentage();

        /// <summary>
        /// Check if this object is still alive (health > 0).
        /// </summary>
        /// <returns>True if alive, false if dead.</returns>
        bool GetIsAlive();
    }
}
