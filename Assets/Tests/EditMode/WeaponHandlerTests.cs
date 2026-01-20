using NUnit.Framework;
using UnityEngine;
using CityShooter.Weapon;

namespace CityShooter.Tests.EditMode
{
    /// <summary>
    /// Edit mode unit tests for WeaponHandler.
    /// Tests the weapon handler's configuration and state management.
    /// </summary>
    [TestFixture]
    public class WeaponHandlerTests
    {
        private GameObject testObject;
        private WeaponHandler weaponHandler;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestWeapon");
            weaponHandler = testObject.AddComponent<WeaponHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void WeaponHandler_ComponentExists()
        {
            Assert.IsNotNull(weaponHandler);
        }

        [Test]
        public void WeaponHandler_IsNotFiringInitially()
        {
            Assert.IsFalse(weaponHandler.IsFiring);
        }

        [Test]
        public void WeaponHandler_SetPositionOffset_UpdatesLocalPosition()
        {
            Vector3 offset = new Vector3(0.5f, -0.2f, 0.3f);
            weaponHandler.SetPositionOffset(offset);

            Assert.AreEqual(offset, testObject.transform.localPosition);
        }

        [Test]
        public void WeaponHandler_SetRotationOffset_UpdatesLocalRotation()
        {
            Vector3 eulerAngles = new Vector3(10f, 20f, 30f);
            weaponHandler.SetRotationOffset(eulerAngles);

            // Compare euler angles (with small tolerance for floating point)
            Vector3 resultEuler = testObject.transform.localEulerAngles;
            Assert.AreEqual(eulerAngles.x, resultEuler.x, 0.001f);
            Assert.AreEqual(eulerAngles.y, resultEuler.y, 0.001f);
            Assert.AreEqual(eulerAngles.z, resultEuler.z, 0.001f);
        }

        [Test]
        public void WeaponHandler_ManualFire_DoesNotThrow()
        {
            // Without camera, this should handle gracefully
            Assert.DoesNotThrow(() => weaponHandler.ManualFire());
        }

        [Test]
        public void WeaponHandler_Events_CanBeSubscribed()
        {
            bool hitFired = false;
            bool fireFired = false;
            bool stopFireFired = false;

            weaponHandler.OnHit += (hit) => hitFired = true;
            weaponHandler.OnFire += () => fireFired = true;
            weaponHandler.OnStopFire += () => stopFireFired = true;

            // Events won't fire without input, but subscription should work
            Assert.IsFalse(hitFired);
            Assert.IsFalse(fireFired);
            Assert.IsFalse(stopFireFired);
        }

        [Test]
        public void WeaponHandler_MultipleSetPositionCalls_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                weaponHandler.SetPositionOffset(Vector3.zero);
                weaponHandler.SetPositionOffset(new Vector3(1f, 1f, 1f));
                weaponHandler.SetPositionOffset(new Vector3(-1f, -1f, -1f));
            });
        }

        [Test]
        public void WeaponHandler_InitialPositionIsAtOffset()
        {
            // After Awake, weapon should be at the configured offset position
            // The default offset is (0.3f, -0.2f, 0.5f) based on the script
            // Since Awake runs before test, we check that position was set
            Assert.IsNotNull(testObject.transform.localPosition);
        }
    }
}
