using UnityEngine;

namespace CityShooter.Core
{
    /// <summary>
    /// ScriptableObject configuration for laser gun settings.
    /// Allows designers to tweak weapon parameters without modifying code.
    /// </summary>
    [CreateAssetMenu(fileName = "LaserGunConfig", menuName = "City Shooter/Laser Gun Config", order = 1)]
    public class LaserGunConfig : ScriptableObject
    {
        [Header("Firing Configuration")]
        [Tooltip("Time between shots in seconds")]
        [Range(0.01f, 2f)]
        public float fireRate = 0.15f;

        [Tooltip("Maximum raycast distance")]
        [Range(10f, 500f)]
        public float maxRange = 100f;

        [Tooltip("Damage per shot")]
        [Range(1f, 500f)]
        public float damage = 25f;

        [Header("Laser Visual Settings")]
        [Tooltip("Primary laser color")]
        public Color laserColorPrimary = new Color(0f, 1f, 1f, 1f); // Cyan

        [Tooltip("Secondary laser color")]
        public Color laserColorSecondary = new Color(1f, 0f, 0f, 1f); // Red

        [Tooltip("Laser beam width at start")]
        [Range(0.01f, 0.5f)]
        public float laserWidthStart = 0.05f;

        [Tooltip("Laser beam width at end")]
        [Range(0.01f, 0.5f)]
        public float laserWidthEnd = 0.02f;

        [Tooltip("Laser visible duration")]
        [Range(0.01f, 1f)]
        public float laserDuration = 0.1f;

        [Tooltip("Laser fade out duration")]
        [Range(0.01f, 0.5f)]
        public float laserFadeOutDuration = 0.05f;

        [Header("Emission Settings")]
        [Tooltip("Emission color for fuel and barrel")]
        public Color emissionColor = new Color(0f, 1f, 1f, 1f);

        [Tooltip("Peak emission intensity during flash")]
        [Range(0f, 20f)]
        public float peakEmissionIntensity = 8f;

        [Tooltip("Base emission intensity")]
        [Range(0f, 5f)]
        public float baseEmissionIntensity = 0f;

        [Tooltip("Flash duration")]
        [Range(0.01f, 0.5f)]
        public float flashDuration = 0.15f;

        [Tooltip("Flash fade out duration")]
        [Range(0.01f, 1f)]
        public float flashFadeOutDuration = 0.25f;

        [Header("Impact Effect Settings")]
        [Tooltip("Default impact particle color")]
        public Color defaultImpactColor = new Color(0f, 1f, 1f, 1f);

        [Tooltip("Enemy impact particle color")]
        public Color enemyImpactColor = new Color(1f, 0.3f, 0f, 1f);

        [Tooltip("Impact effect duration")]
        [Range(0.1f, 3f)]
        public float impactEffectDuration = 1f;

        [Tooltip("Impact decal lifetime")]
        [Range(1f, 60f)]
        public float decalLifetime = 10f;

        [Tooltip("Impact decal size")]
        [Range(0.05f, 1f)]
        public float decalSize = 0.2f;

        [Header("Object Pool Settings")]
        [Tooltip("Initial pool size for impact effects")]
        [Range(5, 100)]
        public int initialPoolSize = 20;

        [Tooltip("Maximum pool size")]
        [Range(10, 200)]
        public int maxPoolSize = 50;

        [Header("Audio Settings")]
        [Tooltip("Fire sound volume")]
        [Range(0f, 1f)]
        public float fireSoundVolume = 0.8f;

        [Tooltip("Impact sound volume")]
        [Range(0f, 1f)]
        public float impactSoundVolume = 0.5f;

        /// <summary>
        /// Applies this configuration to a LaserGunController.
        /// </summary>
        public void ApplyTo(Weapons.LaserGunController controller)
        {
            if (controller == null)
                return;

            controller.FireRate = fireRate;
            controller.Damage = damage;
        }

        /// <summary>
        /// Applies this configuration to a LaserBoltVFX.
        /// </summary>
        public void ApplyTo(Weapons.LaserBoltVFX laserBolt)
        {
            if (laserBolt == null)
                return;

            laserBolt.EmissionIntensity = peakEmissionIntensity;
            laserBolt.SetLaserColor(laserColorPrimary);
        }

        /// <summary>
        /// Applies this configuration to an EmissiveFlashController.
        /// </summary>
        public void ApplyTo(Weapons.EmissiveFlashController flashController)
        {
            if (flashController == null)
                return;

            flashController.PeakIntensity = peakEmissionIntensity;
            flashController.SetEmissionColor(emissionColor);
        }

#if UNITY_EDITOR
        [ContextMenu("Create Default Config Asset")]
        private static void CreateDefaultConfigAsset()
        {
            LaserGunConfig config = CreateInstance<LaserGunConfig>();
            UnityEditor.AssetDatabase.CreateAsset(config, "Assets/Settings/LaserGunConfig.asset");
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
