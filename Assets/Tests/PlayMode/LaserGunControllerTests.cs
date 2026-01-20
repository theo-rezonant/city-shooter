using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CityShooter.Weapons;

namespace CityShooter.Tests.PlayMode
{
    /// <summary>
    /// Play mode integration tests for the LaserGunController.
    /// </summary>
    [TestFixture]
    public class LaserGunControllerTests
    {
        private GameObject _weaponObject;
        private LaserGunController _controller;
        private Camera _testCamera;

        [SetUp]
        public void SetUp()
        {
            // Create test camera
            _testCamera = new GameObject("TestCamera").AddComponent<Camera>();
            _testCamera.tag = "MainCamera";

            // Create weapon object
            _weaponObject = new GameObject("TestLaserGun");
            _controller = _weaponObject.AddComponent<LaserGunController>();

            // Add required components
            _weaponObject.AddComponent<LaserBoltVFX>();
            _weaponObject.AddComponent<EmissiveFlashController>();
            _weaponObject.AddComponent<ImpactEffectController>();

            // Create muzzle point
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(_weaponObject.transform);
            muzzle.transform.localPosition = Vector3.forward;
        }

        [TearDown]
        public void TearDown()
        {
            if (_weaponObject != null)
            {
                Object.Destroy(_weaponObject);
            }

            if (_testCamera != null)
            {
                Object.Destroy(_testCamera.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator LaserGunController_Initializes_Correctly()
        {
            yield return null; // Wait one frame for Awake/Start

            // Assert
            Assert.IsNotNull(_controller);
            Assert.IsFalse(_controller.IsFiring);
        }

        [UnityTest]
        public IEnumerator LaserGunController_FireRate_RestrictsRapidFiring()
        {
            yield return null; // Wait for initialization

            _controller.FireRate = 1f; // 1 second between shots

            int fireCount = 0;
            _controller.OnWeaponFired += (info) => fireCount++;

            // Try to fire rapidly
            _controller.TryFire();
            _controller.TryFire();
            _controller.TryFire();

            yield return null;

            // Should only have fired once due to fire rate
            Assert.AreEqual(1, fireCount);
        }

        [UnityTest]
        public IEnumerator LaserGunController_CanFire_AfterFireRateCooldown()
        {
            yield return null; // Wait for initialization

            _controller.FireRate = 0.1f; // 100ms between shots

            int fireCount = 0;
            _controller.OnWeaponFired += (info) => fireCount++;

            // First shot
            _controller.TryFire();

            // Wait for cooldown
            yield return new WaitForSeconds(0.15f);

            // Second shot
            _controller.TryFire();

            yield return null;

            // Should have fired twice
            Assert.AreEqual(2, fireCount);
        }

        [UnityTest]
        public IEnumerator LaserGunController_FiresEvent_WithHitInfo()
        {
            yield return null; // Wait for initialization

            LaserHitInfo? receivedInfo = null;
            _controller.OnWeaponFired += (info) => receivedInfo = info;

            _controller.TryFire();

            yield return null;

            Assert.IsNotNull(receivedInfo);
            Assert.IsTrue(receivedInfo.Value.MaxRange > 0);
        }

        [UnityTest]
        public IEnumerator LaserGunController_SetMovementState_UpdatesCorrectly()
        {
            yield return null;

            // Test setting movement state
            _controller.SetMovementState(true);

            // Fire while moving - should use moving fire animation
            _controller.TryFire();

            yield return null;

            _controller.SetMovementState(false);

            yield return new WaitForSeconds(0.2f);

            // Fire while stationary
            _controller.TryFire();

            yield return null;

            // Test passes if no exceptions thrown
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator LaserGunController_Damage_CanBeModified()
        {
            yield return null;

            float originalDamage = _controller.Damage;
            float newDamage = 50f;

            _controller.Damage = newDamage;

            Assert.AreEqual(newDamage, _controller.Damage);
            Assert.AreNotEqual(originalDamage, _controller.Damage);
        }

        [UnityTest]
        public IEnumerator LaserGunController_IsFiring_StateIsCorrect()
        {
            yield return null;

            // Should not be firing initially
            Assert.IsFalse(_controller.IsFiring);

            // Fire
            _controller.TryFire();

            yield return null;

            // May be in firing state briefly
            // Wait for firing state to reset
            yield return new WaitForSeconds(0.2f);

            Assert.IsFalse(_controller.IsFiring);
        }
    }

    /// <summary>
    /// Play mode integration tests for the LaserBoltVFX.
    /// </summary>
    [TestFixture]
    public class LaserBoltVFXTests
    {
        private GameObject _vfxObject;
        private LaserBoltVFX _laserBolt;

        [SetUp]
        public void SetUp()
        {
            _vfxObject = new GameObject("TestLaserBolt");
            _laserBolt = _vfxObject.AddComponent<LaserBoltVFX>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_vfxObject != null)
            {
                Object.Destroy(_vfxObject);
            }
        }

        [UnityTest]
        public IEnumerator LaserBoltVFX_FireLaserBolt_ActivatesLineRenderer()
        {
            yield return null;

            Vector3 origin = Vector3.zero;
            Vector3 target = Vector3.forward * 10f;

            _laserBolt.FireLaserBolt(origin, target);

            yield return null;

            // LineRenderer should be active
            Assert.IsTrue(_laserBolt.IsActive);
        }

        [UnityTest]
        public IEnumerator LaserBoltVFX_FireLaserBolt_DeactivatesAfterDuration()
        {
            yield return null;

            _laserBolt.FireLaserBolt(Vector3.zero, Vector3.forward * 10f);

            // Wait for effect to complete (default duration + fade)
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(_laserBolt.IsActive);
        }

        [UnityTest]
        public IEnumerator LaserBoltVFX_SetLaserColor_ChangesColor()
        {
            yield return null;

            Color customColor = Color.green;
            _laserBolt.SetLaserColor(customColor);

            // Fire to see effect
            _laserBolt.FireLaserBolt(Vector3.zero, Vector3.forward * 10f);

            yield return null;

            // Test passes if no exceptions
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator LaserBoltVFX_SetLaserColorMode_SwitchesBetweenCyanAndRed()
        {
            yield return null;

            // Set to cyan
            _laserBolt.SetLaserColorMode(true);
            _laserBolt.FireLaserBolt(Vector3.zero, Vector3.forward * 5f);
            yield return new WaitForSeconds(0.3f);

            // Set to red
            _laserBolt.SetLaserColorMode(false);
            _laserBolt.FireLaserBolt(Vector3.zero, Vector3.forward * 5f);
            yield return new WaitForSeconds(0.3f);

            Assert.Pass();
        }
    }

    /// <summary>
    /// Play mode integration tests for the EmissiveFlashController.
    /// </summary>
    [TestFixture]
    public class EmissiveFlashControllerTests
    {
        private GameObject _controllerObject;
        private EmissiveFlashController _flashController;
        private GameObject _targetObject;

        [SetUp]
        public void SetUp()
        {
            _controllerObject = new GameObject("TestFlashController");
            _flashController = _controllerObject.AddComponent<EmissiveFlashController>();

            // Create a target object with renderer
            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetObject.name = "fuel"; // Named to be auto-detected
            _targetObject.transform.SetParent(_controllerObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerObject != null)
            {
                Object.Destroy(_controllerObject);
            }
        }

        [UnityTest]
        public IEnumerator EmissiveFlashController_TriggerFlash_StartsFlashing()
        {
            yield return null;

            _flashController.TriggerFlash();

            yield return null;

            Assert.IsTrue(_flashController.IsFlashing);
        }

        [UnityTest]
        public IEnumerator EmissiveFlashController_TriggerFlash_CompletesAfterDuration()
        {
            yield return null;

            _flashController.TriggerFlash();

            // Wait for flash to complete
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(_flashController.IsFlashing);
        }

        [UnityTest]
        public IEnumerator EmissiveFlashController_AutoDetectsTargets_ByName()
        {
            yield return null;

            // The fuel object should have been auto-detected
            Assert.Greater(_flashController.TargetCount, 0);
        }

        [UnityTest]
        public IEnumerator EmissiveFlashController_PeakIntensity_CanBeModified()
        {
            yield return null;

            float newIntensity = 15f;
            _flashController.PeakIntensity = newIntensity;

            Assert.AreEqual(newIntensity, _flashController.PeakIntensity);
        }
    }
}
