using UnityEngine;
using CityShooter.Core;

namespace CityShooter.Combat
{
    /// <summary>
    /// Bridge component that connects a weapon to the HUD event system.
    /// Attach this to any weapon GameObject that implements IWeaponHUD.
    /// Automatically broadcasts weapon state changes to the UI.
    /// </summary>
    public class WeaponHUDBridge : MonoBehaviour
    {
        [Header("Weapon Reference")]
        [SerializeField] private MonoBehaviour weaponComponent;

        [Header("Settings")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float maxRaycastDistance = 100f;

        private IWeaponHUD weapon;
        private int lastAmmo;
        private bool lastFiringState;
        private bool lastReloadState;

        private void Start()
        {
            // Try to get IWeaponHUD from assigned component or this GameObject
            if (weaponComponent != null)
            {
                weapon = weaponComponent as IWeaponHUD;
            }

            if (weapon == null)
            {
                weapon = GetComponent<IWeaponHUD>();
            }

            if (weapon == null)
            {
                Debug.LogWarning($"[WeaponHUDBridge] No IWeaponHUD found on {gameObject.name}");
                enabled = false;
                return;
            }

            // Initialize HUD with starting values
            lastAmmo = weapon.CurrentAmmo;
            CombatEvents.InvokeAmmoChanged(weapon.CurrentAmmo, weapon.MaxAmmo);
        }

        private void Update()
        {
            if (weapon == null) return;

            CheckAmmoChange();
            CheckFiringStateChange();
            CheckReloadStateChange();
        }

        private void CheckAmmoChange()
        {
            if (weapon.CurrentAmmo != lastAmmo)
            {
                lastAmmo = weapon.CurrentAmmo;
                CombatEvents.InvokeAmmoChanged(weapon.CurrentAmmo, weapon.MaxAmmo);
            }
        }

        private void CheckFiringStateChange()
        {
            if (weapon.IsFiring != lastFiringState)
            {
                lastFiringState = weapon.IsFiring;
                CombatEvents.InvokeFiringStateChanged(weapon.IsFiring);
            }
        }

        private void CheckReloadStateChange()
        {
            if (weapon.IsReloading != lastReloadState)
            {
                lastReloadState = weapon.IsReloading;
                // Use 0 duration if not reloading, otherwise get from weapon config
                CombatEvents.InvokeReloadStateChanged(weapon.IsReloading, weapon.IsReloading ? 2f : 0f);
            }
        }

        /// <summary>
        /// Call this from the weapon when it fires.
        /// </summary>
        public void NotifyFire()
        {
            CombatEvents.InvokePlayerFire();
        }

        /// <summary>
        /// Call this from the weapon's raycast system when hitting an enemy.
        /// </summary>
        public void NotifyEnemyHit(Vector3 hitPoint)
        {
            CombatEvents.InvokeEnemyHit(hitPoint);
        }

        /// <summary>
        /// Call this from the weapon's raycast system with the RaycastHit.
        /// Automatically checks if hit was on Enemy layer.
        /// </summary>
        public void ProcessRaycastHit(RaycastHit hit)
        {
            // Check if hit object is on enemy layer
            if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
                NotifyEnemyHit(hit.point);
            }
        }

        /// <summary>
        /// Perform a raycast and process the hit automatically.
        /// Call this from the weapon's fire method.
        /// </summary>
        public bool FireRaycast(Vector3 origin, Vector3 direction)
        {
            NotifyFire();

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRaycastDistance))
            {
                ProcessRaycastHit(hit);
                return true;
            }

            return false;
        }
    }
}
