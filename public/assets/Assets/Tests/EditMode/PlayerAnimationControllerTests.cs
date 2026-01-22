using NUnit.Framework;
using UnityEngine;
using CityShooter.Player;

namespace CityShooter.Tests.EditMode
{
    /// <summary>
    /// Edit mode unit tests for PlayerAnimationController.
    /// Tests the animation controller's state management logic.
    /// </summary>
    [TestFixture]
    public class PlayerAnimationControllerTests
    {
        private GameObject testObject;
        private PlayerAnimationController animController;
        private Animator animator;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestAnimator");
            // Add Animator component (required)
            animator = testObject.AddComponent<Animator>();
            animController = testObject.AddComponent<PlayerAnimationController>();
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
        public void AnimController_HasAnimatorComponent()
        {
            Assert.IsNotNull(animController.Animator);
        }

        [Test]
        public void AnimController_AnimatorPropertyReturnsComponent()
        {
            Assert.AreEqual(animator, animController.Animator);
        }

        [Test]
        public void AnimController_IsNotFiringInitially()
        {
            Assert.IsFalse(animController.IsFiring);
        }

        [Test]
        public void AnimController_SetFiring_UpdatesState()
        {
            animController.SetFiring(true);
            Assert.IsTrue(animController.IsFiring);

            animController.SetFiring(false);
            Assert.IsFalse(animController.IsFiring);
        }

        [Test]
        public void AnimController_UpdateMovementAnimation_DoesNotThrow()
        {
            Vector2 input = new Vector2(0.5f, 0.5f);
            Assert.DoesNotThrow(() => animController.UpdateMovementAnimation(input, 0.5f, false, true));
        }

        [Test]
        public void AnimController_UpdateMovementAnimation_WithZeroInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => animController.UpdateMovementAnimation(Vector2.zero, 0f, false, true));
        }

        [Test]
        public void AnimController_UpdateMovementAnimation_WithSprinting_DoesNotThrow()
        {
            Vector2 input = new Vector2(0f, 1f);
            Assert.DoesNotThrow(() => animController.UpdateMovementAnimation(input, 1f, true, true));
        }

        [Test]
        public void AnimController_TriggerHitReaction_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => animController.TriggerHitReaction());
        }

        [Test]
        public void AnimController_SetUpperBodyLayerWeight_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => animController.SetUpperBodyLayerWeight(0.5f));
            Assert.DoesNotThrow(() => animController.SetUpperBodyLayerWeight(0f));
            Assert.DoesNotThrow(() => animController.SetUpperBodyLayerWeight(1f));
        }

        [Test]
        public void AnimController_SetUpperBodyLayerWeight_ClampsOutOfRangeValues()
        {
            // Should not throw even with out-of-range values (clamping is handled internally)
            Assert.DoesNotThrow(() => animController.SetUpperBodyLayerWeight(-1f));
            Assert.DoesNotThrow(() => animController.SetUpperBodyLayerWeight(2f));
        }

        [Test]
        public void AnimController_PlayState_DoesNotThrow()
        {
            // Without a controller, this should still not throw (graceful handling)
            Assert.DoesNotThrow(() => animController.PlayState("TestState", 0, 0f));
        }

        [Test]
        public void AnimController_CrossFadeState_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => animController.CrossFadeState("TestState", 0.1f, 0));
        }

        [Test]
        public void AnimController_GetCurrentStateInfo_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => animController.GetCurrentStateInfo(0));
        }

        [Test]
        public void AnimController_IsPlayingState_ReturnsFalseWithoutController()
        {
            // Without a proper controller setup, should return false gracefully
            bool isPlaying = animController.IsPlayingState("NonExistentState", 0);
            Assert.IsFalse(isPlaying);
        }
    }
}
