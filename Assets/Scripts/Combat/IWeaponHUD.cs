using UnityEngine;

namespace CityShooter.Combat
{
    /// <summary>
    /// Interface for weapons to communicate with the HUD system.
    /// Implement this interface in weapon classes to enable automatic HUD updates.
    /// </summary>
    public interface IWeaponHUD
    {
        /// <summary>
        /// Current ammo/energy count.
        /// </summary>
        int CurrentAmmo { get; }

        /// <summary>
        /// Maximum ammo/energy capacity.
        /// </summary>
        int MaxAmmo { get; }

        /// <summary>
        /// Whether the weapon is currently firing.
        /// </summary>
        bool IsFiring { get; }

        /// <summary>
        /// Whether the weapon is currently reloading/recharging.
        /// </summary>
        bool IsReloading { get; }

        /// <summary>
        /// Current reload progress (0-1).
        /// </summary>
        float ReloadProgress { get; }
    }
}
