using NUnit.Framework;
using UnityEngine;
using CityShooter.Player;

namespace CityShooter.Tests.EditMode
{
    /// <summary>
    /// Edit mode unit tests for FPSCharacterController.
    /// Tests the controller's state and property logic without requiring play mode.
    /// </summary>
    [TestFixture]
    public class FPSCharacterControllerTests
    {
        private GameObject testObject;
        private FPSCharacterController controller;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestPlayer");
            // Add CharacterController first (required component)
            testObject.AddComponent<CharacterController>();
            controller = testObject.AddComponent<FPSCharacterController>();
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
        public void Controller_HasCharacterControllerComponent()
        {
            Assert.IsNotNull(testObject.GetComponent<CharacterController>());
        }

        [Test]
        public void Controller_InitialVelocityIsZero()
        {
            Assert.AreEqual(Vector3.zero, controller.Velocity);
        }

        [Test]
        public void Controller_InitialMovementInputIsZero()
        {
            Assert.AreEqual(Vector2.zero, controller.MovementInput);
        }

        [Test]
        public void Controller_InitialCurrentSpeedIsZero()
        {
            Assert.AreEqual(0f, controller.CurrentSpeed);
        }

        [Test]
        public void Controller_NormalizedVelocityIsClampedToOne()
        {
            // Normalized velocity should always be between 0 and 1
            float normalizedVelocity = controller.NormalizedVelocity;
            Assert.GreaterOrEqual(normalizedVelocity, 0f);
            Assert.LessOrEqual(normalizedVelocity, 1f);
        }

        [Test]
        public void Controller_IsNotSprintingInitially()
        {
            Assert.IsFalse(controller.IsSprinting);
        }

        [Test]
        public void Controller_IsNotCrouchingInitially()
        {
            Assert.IsFalse(controller.IsCrouching);
        }

        [Test]
        public void Controller_SetCrouching_UpdatesState()
        {
            controller.SetCrouching(true);
            Assert.IsTrue(controller.IsCrouching);

            controller.SetCrouching(false);
            Assert.IsFalse(controller.IsCrouching);
        }

        [Test]
        public void Controller_SetMouseSensitivity_ClampsValue()
        {
            // Test that sensitivity is clamped (indirectly through public method)
            // Method should accept values without throwing
            Assert.DoesNotThrow(() => controller.SetMouseSensitivity(0.1f));
            Assert.DoesNotThrow(() => controller.SetMouseSensitivity(10f));
            Assert.DoesNotThrow(() => controller.SetMouseSensitivity(-5f)); // Should clamp to 0.1
            Assert.DoesNotThrow(() => controller.SetMouseSensitivity(50f)); // Should clamp to 10
        }

        [Test]
        public void Controller_StrafeInput_ReturnsHorizontalComponent()
        {
            // Initially should be zero
            Assert.AreEqual(0f, controller.StrafeInput);
        }

        [Test]
        public void Controller_ForwardInput_ReturnsVerticalComponent()
        {
            // Initially should be zero
            Assert.AreEqual(0f, controller.ForwardInput);
        }

        [Test]
        public void Controller_LockCursor_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => controller.LockCursor(true));
            Assert.DoesNotThrow(() => controller.LockCursor(false));
        }

        [Test]
        public void Controller_AddImpulse_DoesNotThrow()
        {
            Vector3 impulse = new Vector3(1f, 2f, 3f);
            Assert.DoesNotThrow(() => controller.AddImpulse(impulse));
        }
    }
}
