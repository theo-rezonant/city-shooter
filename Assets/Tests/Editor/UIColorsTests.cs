using NUnit.Framework;
using UnityEngine;
using CityShooter.UI;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for UIColors utility class.
    /// </summary>
    [TestFixture]
    public class UIColorsTests
    {
        private UIColors uiColors;

        [SetUp]
        public void SetUp()
        {
            uiColors = ScriptableObject.CreateInstance<UIColors>();
        }

        [TearDown]
        public void TearDown()
        {
            if (uiColors != null)
            {
                Object.DestroyImmediate(uiColors);
            }
        }

        // ==================== GetHealthColor Tests ====================

        [Test]
        public void GetHealthColor_FullHealth_ReturnsPrimaryCyan()
        {
            Color result = uiColors.GetHealthColor(1.0f);
            Assert.AreEqual(uiColors.primaryCyan, result);
        }

        [Test]
        public void GetHealthColor_HighHealth_ReturnsPrimaryCyan()
        {
            Color result = uiColors.GetHealthColor(0.75f);
            Assert.AreEqual(uiColors.primaryCyan, result);
        }

        [Test]
        public void GetHealthColor_MediumHealth_ReturnsWarning()
        {
            Color result = uiColors.GetHealthColor(0.4f);
            Assert.AreEqual(uiColors.warning, result);
        }

        [Test]
        public void GetHealthColor_LowHealth_ReturnsCritical()
        {
            Color result = uiColors.GetHealthColor(0.2f);
            Assert.AreEqual(uiColors.critical, result);
        }

        [Test]
        public void GetHealthColor_ZeroHealth_ReturnsCritical()
        {
            Color result = uiColors.GetHealthColor(0f);
            Assert.AreEqual(uiColors.critical, result);
        }

        // ==================== GetAmmoColor Tests ====================

        [Test]
        public void GetAmmoColor_FullAmmo_ReturnsPrimaryCyan()
        {
            Color result = uiColors.GetAmmoColor(1.0f);
            Assert.AreEqual(uiColors.primaryCyan, result);
        }

        [Test]
        public void GetAmmoColor_MediumAmmo_ReturnsLightCyan()
        {
            Color result = uiColors.GetAmmoColor(0.5f);
            Assert.AreEqual(uiColors.lightCyan, result);
        }

        [Test]
        public void GetAmmoColor_LowAmmo_ReturnsDamage()
        {
            Color result = uiColors.GetAmmoColor(0.2f);
            Assert.AreEqual(uiColors.damage, result);
        }

        [Test]
        public void GetAmmoColor_EmptyAmmo_ReturnsCritical()
        {
            Color result = uiColors.GetAmmoColor(0f);
            Assert.AreEqual(uiColors.critical, result);
        }

        // ==================== GetHitColor Tests ====================

        [Test]
        public void GetHitColor_NormalHit_ReturnsHitNormal()
        {
            Color result = uiColors.GetHitColor(HitType.Normal);
            Assert.AreEqual(uiColors.hitNormal, result);
        }

        [Test]
        public void GetHitColor_CriticalHit_ReturnsHitCritical()
        {
            Color result = uiColors.GetHitColor(HitType.Critical);
            Assert.AreEqual(uiColors.hitCritical, result);
        }

        [Test]
        public void GetHitColor_HeadShot_ReturnsHitHeadshot()
        {
            Color result = uiColors.GetHitColor(HitType.HeadShot);
            Assert.AreEqual(uiColors.hitHeadshot, result);
        }

        [Test]
        public void GetHitColor_Kill_ReturnsHitKill()
        {
            Color result = uiColors.GetHitColor(HitType.Kill);
            Assert.AreEqual(uiColors.hitKill, result);
        }

        // ==================== WithAlpha Tests ====================

        [Test]
        public void WithAlpha_ModifiesAlphaCorrectly()
        {
            Color original = new Color(1f, 0.5f, 0.25f, 1f);
            Color result = UIColors.WithAlpha(original, 0.5f);

            Assert.AreEqual(original.r, result.r);
            Assert.AreEqual(original.g, result.g);
            Assert.AreEqual(original.b, result.b);
            Assert.AreEqual(0.5f, result.a);
        }

        [Test]
        public void WithAlpha_ZeroAlpha_ReturnsTransparent()
        {
            Color original = Color.cyan;
            Color result = UIColors.WithAlpha(original, 0f);

            Assert.AreEqual(0f, result.a);
        }

        // ==================== LerpPreserveAlpha Tests ====================

        [Test]
        public void LerpPreserveAlpha_HalfwayLerp_ReturnsMiddleValue()
        {
            Color a = new Color(0f, 0f, 0f, 0.2f);
            Color b = new Color(1f, 1f, 1f, 0.8f);
            Color result = UIColors.LerpPreserveAlpha(a, b, 0.5f, true);

            Assert.AreEqual(0.5f, result.r, 0.01f);
            Assert.AreEqual(0.5f, result.g, 0.01f);
            Assert.AreEqual(0.5f, result.b, 0.01f);
            Assert.AreEqual(0.5f, result.a, 0.01f);
        }

        [Test]
        public void LerpPreserveAlpha_ZeroT_ReturnsFirstColor()
        {
            Color a = Color.red;
            Color b = Color.blue;
            Color result = UIColors.LerpPreserveAlpha(a, b, 0f, true);

            Assert.AreEqual(a.r, result.r, 0.01f);
            Assert.AreEqual(a.g, result.g, 0.01f);
            Assert.AreEqual(a.b, result.b, 0.01f);
        }

        [Test]
        public void LerpPreserveAlpha_OneT_ReturnsSecondColor()
        {
            Color a = Color.red;
            Color b = Color.blue;
            Color result = UIColors.LerpPreserveAlpha(a, b, 1f, true);

            Assert.AreEqual(b.r, result.r, 0.01f);
            Assert.AreEqual(b.g, result.g, 0.01f);
            Assert.AreEqual(b.b, result.b, 0.01f);
        }

        // ==================== Default Colors Tests ====================

        [Test]
        public void DefaultColors_PrimaryCyan_HasCorrectValue()
        {
            Color expected = new Color(0f, 1f, 1f, 1f);
            Assert.AreEqual(expected, UIColors.Defaults.PrimaryCyan);
        }

        [Test]
        public void DefaultColors_Critical_HasCorrectValue()
        {
            Color expected = new Color(1f, 0.2f, 0.2f, 1f);
            Assert.AreEqual(expected, UIColors.Defaults.Critical);
        }

        [Test]
        public void DefaultColors_Warning_HasCorrectValue()
        {
            Color expected = new Color(1f, 0.8f, 0f, 1f);
            Assert.AreEqual(expected, UIColors.Defaults.Warning);
        }
    }
}
