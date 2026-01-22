using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CityShooter.Player;
using CityShooter.Camera;
using CityShooter.Weapon;

namespace CityShooter.Tests.PlayMode
{
    /// <summary>
    /// Play mode integration tests for the FPS player system.
    /// Tests component interactions during runtime.
    /// </summary>
    [TestFixture]
    public class PlayerIntegrationTests
    {
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            // Create player hierarchy
            playerObject = new GameObject("TestPlayer");
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.Destroy(playerObject);
            }
        }

        [UnityTest]
        public IEnumerator PlayerSetup_CreatesExpectedHierarchy()
        {
            // Add PlayerSetup component
            playerObject.AddComponent<PlayerSetup>();

            // Wait for Awake to complete
            yield return null;

            // Verify CharacterController exists
            Assert.IsNotNull(playerObject.GetComponent<CharacterController>(), "CharacterController should be created");

            // Verify FPSCharacterController exists
            Assert.IsNotNull(playerObject.GetComponent<FPSCharacterController>(), "FPSCharacterController should be created");

            // Verify Main Camera child exists
            Transform cameraTransform = playerObject.transform.Find("Main Camera");
            Assert.IsNotNull(cameraTransform, "Main Camera child should exist");

            // Verify Camera component
            Assert.IsNotNull(cameraTransform.GetComponent<UnityEngine.Camera>(), "Camera component should exist");

            // Verify CameraShake component
            Assert.IsNotNull(cameraTransform.GetComponent<CameraShake>(), "CameraShake should be on camera");

            // Verify GroundCheck child exists
            Transform groundCheck = playerObject.transform.Find("GroundCheck");
            Assert.IsNotNull(groundCheck, "GroundCheck child should exist");
        }

        [UnityTest]
        public IEnumerator FPSController_MaintainsPositionWithoutInput()
        {
            playerObject.AddComponent<CharacterController>();
            FPSCharacterController controller = playerObject.AddComponent<FPSCharacterController>();

            Vector3 initialPosition = playerObject.transform.position;

            // Wait a few frames
            yield return null;
            yield return null;
            yield return null;

            // Position should be roughly the same (may have small gravity effect)
            // Since we haven't set up ground, allow for some vertical movement
            float horizontalDiff = Vector2.Distance(
                new Vector2(playerObject.transform.position.x, playerObject.transform.position.z),
                new Vector2(initialPosition.x, initialPosition.z)
            );

            Assert.LessOrEqual(horizontalDiff, 0.1f, "Horizontal position should not drift without input");
        }

        [UnityTest]
        public IEnumerator AnimationController_UpdatesWithoutCrashing()
        {
            // Create player with animator
            Animator animator = playerObject.AddComponent<Animator>();
            PlayerAnimationController animController = playerObject.AddComponent<PlayerAnimationController>();

            // Wait for initialization
            yield return null;

            // Update movement animation
            animController.UpdateMovementAnimation(new Vector2(0.5f, 0.5f), 0.5f, false, true);

            yield return null;

            // Set firing state
            animController.SetFiring(true);
            Assert.IsTrue(animController.IsFiring);

            yield return null;

            animController.SetFiring(false);
            Assert.IsFalse(animController.IsFiring);
        }

        [UnityTest]
        public IEnumerator CameraShake_PlaysAndCompletes()
        {
            CameraShake shake = playerObject.AddComponent<CameraShake>();
            Vector3 originalPos = playerObject.transform.localPosition;

            bool shakeStarted = false;
            bool shakeEnded = false;

            shake.OnShakeStarted += () => shakeStarted = true;
            shake.OnShakeEnded += () => shakeEnded = true;

            // Trigger shake
            shake.PlayShake(0.1f, 0.05f, 25f);

            yield return null;

            Assert.IsTrue(shakeStarted, "Shake should have started");

            // Wait for shake to complete
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(shakeEnded, "Shake should have ended");

            // Position should be back to original (or very close)
            float distance = Vector3.Distance(playerObject.transform.localPosition, originalPos);
            Assert.LessOrEqual(distance, 0.01f, "Position should reset after shake");
        }

        [UnityTest]
        public IEnumerator WeaponHandler_TracksParentTransform()
        {
            // Create camera parent
            GameObject cameraObj = new GameObject("MainCamera");
            cameraObj.transform.SetParent(playerObject.transform);
            cameraObj.AddComponent<UnityEngine.Camera>();
            cameraObj.tag = "MainCamera";

            // Create weapon as child of camera
            GameObject weaponObj = new GameObject("Weapon");
            weaponObj.transform.SetParent(cameraObj.transform);
            WeaponHandler weapon = weaponObj.AddComponent<WeaponHandler>();

            yield return null;

            // Move camera
            cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);

            yield return null;

            // Weapon should have moved with camera (parent)
            Assert.AreEqual(cameraObj.transform.position.y, weaponObj.transform.position.y - weapon.transform.localPosition.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator FullPlayerHierarchy_InitializesCorrectly()
        {
            // Setup complete player
            playerObject.AddComponent<CharacterController>();
            FPSCharacterController controller = playerObject.AddComponent<FPSCharacterController>();

            // Add camera
            GameObject camera = new GameObject("Main Camera");
            camera.transform.SetParent(playerObject.transform);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            camera.AddComponent<UnityEngine.Camera>();
            camera.AddComponent<AudioListener>();
            CameraShake shake = camera.AddComponent<CameraShake>();

            // Add weapon to camera
            GameObject weapon = new GameObject("Weapon");
            weapon.transform.SetParent(camera.transform);
            weapon.transform.localPosition = new Vector3(0.2f, -0.15f, 0.4f);
            WeaponHandler weaponHandler = weapon.AddComponent<WeaponHandler>();

            // Add character model with animator
            GameObject characterModel = new GameObject("CharacterModel");
            characterModel.transform.SetParent(playerObject.transform);
            Animator animator = characterModel.AddComponent<Animator>();
            PlayerAnimationController animController = characterModel.AddComponent<PlayerAnimationController>();

            yield return null;

            // Verify all components are accessible
            Assert.IsNotNull(controller);
            Assert.IsNotNull(shake);
            Assert.IsNotNull(weaponHandler);
            Assert.IsNotNull(animController);

            // Verify hierarchy
            Assert.AreEqual(playerObject.transform, camera.transform.parent);
            Assert.AreEqual(camera.transform, weapon.transform.parent);
            Assert.AreEqual(playerObject.transform, characterModel.transform.parent);
        }
    }
}
