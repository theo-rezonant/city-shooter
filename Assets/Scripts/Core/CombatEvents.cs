using System;
using UnityEngine;

namespace CityShooter.Core
{
    /// <summary>
    /// Static event system for combat-related events.
    /// Used for decoupled communication between combat system and UI.
    /// </summary>
    public static class CombatEvents
    {
        // ==================== WEAPON EVENTS ====================

        /// <summary>
        /// Fired when the player shoots their weapon.
        /// </summary>
        public static event Action OnPlayerFire;

        /// <summary>
        /// Fired when the weapon successfully hits a target on the Enemy layer.
        /// Vector3 parameter is the hit point in world space.
        /// </summary>
        public static event Action<Vector3> OnEnemyHit;

        /// <summary>
        /// Fired when ammo/energy level changes.
        /// Parameters: (current ammo, max ammo)
        /// </summary>
        public static event Action<int, int> OnAmmoChanged;

        /// <summary>
        /// Fired when the weapon starts or stops firing (for automatic weapons).
        /// </summary>
        public static event Action<bool> OnFiringStateChanged;

        /// <summary>
        /// Fired when the weapon is reloading.
        /// Parameters: (isReloading, reloadDuration)
        /// </summary>
        public static event Action<bool, float> OnReloadStateChanged;

        // ==================== PLAYER STATE EVENTS ====================

        /// <summary>
        /// Fired when player health changes.
        /// Parameters: (current health, max health)
        /// </summary>
        public static event Action<float, float> OnHealthChanged;

        /// <summary>
        /// Fired when the player takes damage.
        /// Vector3 parameter is the world position of the damage source.
        /// </summary>
        public static event Action<Vector3> OnPlayerDamaged;

        /// <summary>
        /// Fired when player movement state changes.
        /// Parameters: (isMoving, movementSpeed normalized 0-1)
        /// </summary>
        public static event Action<bool, float> OnPlayerMovementChanged;

        // ==================== INVOKE METHODS ====================

        public static void InvokePlayerFire()
        {
            OnPlayerFire?.Invoke();
        }

        public static void InvokeEnemyHit(Vector3 hitPoint)
        {
            OnEnemyHit?.Invoke(hitPoint);
        }

        public static void InvokeAmmoChanged(int current, int max)
        {
            OnAmmoChanged?.Invoke(current, max);
        }

        public static void InvokeFiringStateChanged(bool isFiring)
        {
            OnFiringStateChanged?.Invoke(isFiring);
        }

        public static void InvokeReloadStateChanged(bool isReloading, float duration)
        {
            OnReloadStateChanged?.Invoke(isReloading, duration);
        }

        public static void InvokeHealthChanged(float current, float max)
        {
            OnHealthChanged?.Invoke(current, max);
        }

        public static void InvokePlayerDamaged(Vector3 damageSourcePosition)
        {
            OnPlayerDamaged?.Invoke(damageSourcePosition);
        }

        public static void InvokePlayerMovementChanged(bool isMoving, float speed)
        {
            OnPlayerMovementChanged?.Invoke(isMoving, speed);
        }

        /// <summary>
        /// Clears all event subscriptions. Call when loading new scenes.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnPlayerFire = null;
            OnEnemyHit = null;
            OnAmmoChanged = null;
            OnFiringStateChanged = null;
            OnReloadStateChanged = null;
            OnHealthChanged = null;
            OnPlayerDamaged = null;
            OnPlayerMovementChanged = null;
        }
    }
}
