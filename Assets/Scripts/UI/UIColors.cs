using UnityEngine;

namespace CityShooter.UI
{
    /// <summary>
    /// Centralized color palette for sci-fi HUD elements.
    /// Provides consistent neon/cyan theming across all UI components.
    /// Colors are designed to remain legible against Bloom and SSAO post-processing.
    /// </summary>
    [CreateAssetMenu(fileName = "UIColors", menuName = "City Shooter/UI Colors")]
    public class UIColors : ScriptableObject
    {
        [Header("Primary Palette - Cyan/Neon")]
        [Tooltip("Primary cyan color for healthy states and normal UI")]
        public Color primaryCyan = new Color(0f, 1f, 1f, 1f);

        [Tooltip("Lighter cyan for highlights and glow effects")]
        public Color lightCyan = new Color(0.5f, 1f, 1f, 1f);

        [Tooltip("Darker cyan for secondary elements")]
        public Color darkCyan = new Color(0f, 0.7f, 0.8f, 1f);

        [Header("Alert Colors")]
        [Tooltip("Warning color for medium alerts (low ammo, medium health)")]
        public Color warning = new Color(1f, 0.8f, 0f, 1f);

        [Tooltip("Critical color for dangerous situations")]
        public Color critical = new Color(1f, 0.2f, 0.2f, 1f);

        [Tooltip("Orange for damage indicators")]
        public Color damage = new Color(1f, 0.5f, 0f, 1f);

        [Header("Hit Feedback Colors")]
        [Tooltip("Normal hit marker color")]
        public Color hitNormal = new Color(0f, 1f, 1f, 1f);

        [Tooltip("Critical hit color")]
        public Color hitCritical = new Color(1f, 0.3f, 0.3f, 1f);

        [Tooltip("Headshot hit color")]
        public Color hitHeadshot = new Color(1f, 1f, 0f, 1f);

        [Tooltip("Kill confirmed color")]
        public Color hitKill = new Color(1f, 0f, 0.5f, 1f);

        [Header("Background Colors")]
        [Tooltip("Dark background for UI panels")]
        public Color backgroundDark = new Color(0.05f, 0.05f, 0.1f, 0.7f);

        [Tooltip("Semi-transparent background")]
        public Color backgroundTransparent = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        [Header("Glow Effects")]
        [Tooltip("Glow color for active elements")]
        public Color glowActive = new Color(0f, 1f, 1f, 0.4f);

        [Tooltip("Glow color for critical states")]
        public Color glowCritical = new Color(1f, 0.2f, 0.2f, 0.5f);

        [Header("Text Colors")]
        [Tooltip("Primary text color")]
        public Color textPrimary = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Secondary/dimmed text")]
        public Color textSecondary = new Color(0.7f, 0.7f, 0.7f, 0.8f);

        [Tooltip("Highlighted text")]
        public Color textHighlight = new Color(0f, 1f, 1f, 1f);

        // ==================== STATIC DEFAULTS ====================

        /// <summary>
        /// Static default colors for use without ScriptableObject reference.
        /// </summary>
        public static class Defaults
        {
            // Primary Palette
            public static readonly Color PrimaryCyan = new Color(0f, 1f, 1f, 1f);
            public static readonly Color LightCyan = new Color(0.5f, 1f, 1f, 1f);
            public static readonly Color DarkCyan = new Color(0f, 0.7f, 0.8f, 1f);

            // Alert Colors
            public static readonly Color Warning = new Color(1f, 0.8f, 0f, 1f);
            public static readonly Color Critical = new Color(1f, 0.2f, 0.2f, 1f);
            public static readonly Color Damage = new Color(1f, 0.5f, 0f, 1f);

            // Hit Colors
            public static readonly Color HitNormal = new Color(0f, 1f, 1f, 1f);
            public static readonly Color HitCritical = new Color(1f, 0.3f, 0.3f, 1f);
            public static readonly Color HitHeadshot = new Color(1f, 1f, 0f, 1f);
            public static readonly Color HitKill = new Color(1f, 0f, 0.5f, 1f);

            // Backgrounds
            public static readonly Color BackgroundDark = new Color(0.05f, 0.05f, 0.1f, 0.7f);
            public static readonly Color BackgroundTransparent = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            // Glow
            public static readonly Color GlowActive = new Color(0f, 1f, 1f, 0.4f);
            public static readonly Color GlowCritical = new Color(1f, 0.2f, 0.2f, 0.5f);

            // Text
            public static readonly Color TextPrimary = new Color(1f, 1f, 1f, 1f);
            public static readonly Color TextSecondary = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            public static readonly Color TextHighlight = new Color(0f, 1f, 1f, 1f);
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Get color based on health percentage.
        /// </summary>
        public Color GetHealthColor(float healthPercent)
        {
            if (healthPercent <= 0.25f)
                return critical;
            else if (healthPercent <= 0.5f)
                return warning;
            return primaryCyan;
        }

        /// <summary>
        /// Get color based on ammo percentage.
        /// </summary>
        public Color GetAmmoColor(float ammoPercent)
        {
            if (ammoPercent <= 0f)
                return critical;
            else if (ammoPercent <= 0.3f)
                return damage;
            else if (ammoPercent <= 0.6f)
                return lightCyan;
            return primaryCyan;
        }

        /// <summary>
        /// Get hit marker color based on hit type.
        /// </summary>
        public Color GetHitColor(HitType hitType)
        {
            return hitType switch
            {
                HitType.Critical => hitCritical,
                HitType.HeadShot => hitHeadshot,
                HitType.Kill => hitKill,
                _ => hitNormal
            };
        }

        /// <summary>
        /// Create a color with modified alpha.
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Lerp between two colors with optional alpha preservation.
        /// </summary>
        public static Color LerpPreserveAlpha(Color a, Color b, float t, bool preserveAlpha = true)
        {
            Color result = Color.Lerp(a, b, t);
            if (preserveAlpha)
            {
                result.a = Mathf.Lerp(a.a, b.a, t);
            }
            return result;
        }
    }
}
