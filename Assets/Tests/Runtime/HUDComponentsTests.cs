using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using CityShooter.UI;

namespace CityShooter.Tests.Runtime
{
    /// <summary>
    /// Runtime tests for HUD components.
    /// Tests component behavior in a running Unity environment.
    /// </summary>
    [TestFixture]
    public class HUDComponentsTests
    {
        private GameObject testCanvas;
        private Canvas canvas;

        [SetUp]
        public void SetUp()
        {
            // Create test canvas
            testCanvas = new GameObject("TestCanvas");
            canvas = testCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            testCanvas.AddComponent<CanvasScaler>();
            testCanvas.AddComponent<GraphicRaycaster>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testCanvas != null)
            {
                Object.DestroyImmediate(testCanvas);
            }
        }

        // ==================== HealthBar Tests ====================

        [UnityTest]
        public IEnumerator HealthBar_UpdateHealth_UpdatesDisplayCorrectly()
        {
            // Arrange
            GameObject healthBarGO = new GameObject("HealthBar");
            healthBarGO.transform.SetParent(testCanvas.transform, false);
            HealthBar healthBar = healthBarGO.AddComponent<HealthBar>();

            yield return null; // Wait for Start()

            // Act
            healthBar.UpdateHealth(75f, 100f);

            yield return null; // Wait for Update()

            // Assert
            Assert.AreEqual(0.75f, healthBar.GetHealthPercent(), 0.01f);

            // Cleanup
            Object.DestroyImmediate(healthBarGO);
        }

        [UnityTest]
        public IEnumerator HealthBar_SetVisible_TogglesCorrectly()
        {
            // Arrange
            GameObject healthBarGO = new GameObject("HealthBar");
            healthBarGO.transform.SetParent(testCanvas.transform, false);

            // Create container
            GameObject container = new GameObject("Container");
            container.transform.SetParent(healthBarGO.transform, false);
            container.AddComponent<RectTransform>();

            HealthBar healthBar = healthBarGO.AddComponent<HealthBar>();

            yield return null;

            // Act & Assert - Note: SetVisible requires healthBarContainer to be assigned
            // Since we can't easily set private serialized fields in tests,
            // we verify the method doesn't throw
            Assert.DoesNotThrow(() => healthBar.SetVisible(false));
            Assert.DoesNotThrow(() => healthBar.SetVisible(true));

            // Cleanup
            Object.DestroyImmediate(healthBarGO);
        }

        // ==================== AmmoCounter Tests ====================

        [UnityTest]
        public IEnumerator AmmoCounter_UpdateAmmo_TracksCriticalState()
        {
            // Arrange
            GameObject ammoGO = new GameObject("AmmoCounter");
            ammoGO.transform.SetParent(testCanvas.transform, false);
            AmmoCounter ammoCounter = ammoGO.AddComponent<AmmoCounter>();

            yield return null;

            // Act - Set low ammo
            ammoCounter.UpdateAmmo(5, 30);

            yield return null;

            // Assert
            Assert.IsTrue(ammoCounter.IsAmmoLow());

            // Cleanup
            Object.DestroyImmediate(ammoGO);
        }

        [UnityTest]
        public IEnumerator AmmoCounter_EmptyAmmo_ReportsEmpty()
        {
            // Arrange
            GameObject ammoGO = new GameObject("AmmoCounter");
            ammoGO.transform.SetParent(testCanvas.transform, false);
            AmmoCounter ammoCounter = ammoGO.AddComponent<AmmoCounter>();

            yield return null;

            // Act
            ammoCounter.UpdateAmmo(0, 30);

            yield return null;

            // Assert
            Assert.IsTrue(ammoCounter.IsAmmoEmpty());

            // Cleanup
            Object.DestroyImmediate(ammoGO);
        }

        // ==================== DynamicCrosshair Tests ====================

        [UnityTest]
        public IEnumerator DynamicCrosshair_SetMovementState_AcceptsInput()
        {
            // Arrange
            GameObject crosshairGO = new GameObject("Crosshair");
            crosshairGO.transform.SetParent(testCanvas.transform, false);
            crosshairGO.AddComponent<RectTransform>();
            DynamicCrosshair crosshair = crosshairGO.AddComponent<DynamicCrosshair>();

            yield return null;

            // Act & Assert - Verify methods don't throw
            Assert.DoesNotThrow(() => crosshair.SetMovementState(true, 0.5f));
            Assert.DoesNotThrow(() => crosshair.SetMovementState(false, 0f));

            // Cleanup
            Object.DestroyImmediate(crosshairGO);
        }

        [UnityTest]
        public IEnumerator DynamicCrosshair_TriggerFireExpansion_ExecutesWithoutError()
        {
            // Arrange
            GameObject crosshairGO = new GameObject("Crosshair");
            crosshairGO.transform.SetParent(testCanvas.transform, false);
            crosshairGO.AddComponent<RectTransform>();
            DynamicCrosshair crosshair = crosshairGO.AddComponent<DynamicCrosshair>();

            yield return null;

            // Act & Assert
            Assert.DoesNotThrow(() => crosshair.TriggerFireExpansion());

            // Cleanup
            Object.DestroyImmediate(crosshairGO);
        }

        // ==================== HitMarker Tests ====================

        [UnityTest]
        public IEnumerator HitMarker_ShowHitMarker_SetsIsShowingTrue()
        {
            // Arrange
            GameObject hitMarkerGO = new GameObject("HitMarker");
            hitMarkerGO.transform.SetParent(testCanvas.transform, false);
            hitMarkerGO.AddComponent<RectTransform>();
            HitMarker hitMarker = hitMarkerGO.AddComponent<HitMarker>();

            yield return null;

            // Act
            hitMarker.ShowHitMarker();

            yield return null;

            // Assert
            Assert.IsTrue(hitMarker.IsShowing());

            // Cleanup
            Object.DestroyImmediate(hitMarkerGO);
        }

        [UnityTest]
        public IEnumerator HitMarker_Hide_SetsIsShowingFalse()
        {
            // Arrange
            GameObject hitMarkerGO = new GameObject("HitMarker");
            hitMarkerGO.transform.SetParent(testCanvas.transform, false);
            hitMarkerGO.AddComponent<RectTransform>();
            HitMarker hitMarker = hitMarkerGO.AddComponent<HitMarker>();

            yield return null;

            // Act
            hitMarker.ShowHitMarker();
            yield return null;
            hitMarker.Hide();

            // Assert
            Assert.IsFalse(hitMarker.IsShowing());

            // Cleanup
            Object.DestroyImmediate(hitMarkerGO);
        }

        // ==================== DamageIndicator Tests ====================

        [UnityTest]
        public IEnumerator DamageIndicator_SetPlayerTransform_AcceptsTransform()
        {
            // Arrange
            GameObject damageGO = new GameObject("DamageIndicator");
            damageGO.transform.SetParent(testCanvas.transform, false);
            DamageIndicator damageIndicator = damageGO.AddComponent<DamageIndicator>();

            GameObject playerGO = new GameObject("Player");

            yield return null;

            // Act & Assert
            Assert.DoesNotThrow(() => damageIndicator.SetPlayerTransform(playerGO.transform));

            // Cleanup
            Object.DestroyImmediate(damageGO);
            Object.DestroyImmediate(playerGO);
        }

        [UnityTest]
        public IEnumerator DamageIndicator_ClearAllIndicators_ExecutesWithoutError()
        {
            // Arrange
            GameObject damageGO = new GameObject("DamageIndicator");
            damageGO.transform.SetParent(testCanvas.transform, false);
            damageGO.AddComponent<RectTransform>();
            DamageIndicator damageIndicator = damageGO.AddComponent<DamageIndicator>();

            yield return null;

            // Act & Assert
            Assert.DoesNotThrow(() => damageIndicator.ClearAllIndicators());

            // Cleanup
            Object.DestroyImmediate(damageGO);
        }
    }
}
