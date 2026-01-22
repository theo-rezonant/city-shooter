using NUnit.Framework;
using UnityEngine;
using CityShooter.Camera;

namespace CityShooter.Tests.EditMode
{
    /// <summary>
    /// Edit mode unit tests for CameraShake.
    /// Tests the camera shake component's configuration and state management.
    /// </summary>
    [TestFixture]
    public class CameraShakeTests
    {
        private GameObject testObject;
        private CameraShake cameraShake;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestCamera");
            cameraShake = testObject.AddComponent<CameraShake>();
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
        public void CameraShake_ComponentExists()
        {
            Assert.IsNotNull(cameraShake);
        }

        [Test]
        public void CameraShake_InitialPositionIsZero()
        {
            Assert.AreEqual(Vector3.zero, testObject.transform.localPosition);
        }

        [Test]
        public void CameraShake_PlayShake_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => cameraShake.PlayShake());
        }

        [Test]
        public void CameraShake_PlayShake_WithParameters_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => cameraShake.PlayShake(0.5f, 0.1f, 20f));
        }

        [Test]
        public void CameraShake_PlayFireShake_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => cameraShake.PlayFireShake());
        }

        [Test]
        public void CameraShake_PlayDamageShake_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => cameraShake.PlayDamageShake());
        }

        [Test]
        public void CameraShake_PlayExplosionShake_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => cameraShake.PlayExplosionShake());
        }

        [Test]
        public void CameraShake_StopShake_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => cameraShake.StopShake());
        }

        [Test]
        public void CameraShake_SetOriginalPosition_DoesNotThrow()
        {
            Vector3 newPosition = new Vector3(1f, 2f, 3f);
            Assert.DoesNotThrow(() => cameraShake.SetOriginalPosition(newPosition));
        }

        [Test]
        public void CameraShake_PlayAndStop_DoesNotThrow()
        {
            cameraShake.PlayShake();
            Assert.DoesNotThrow(() => cameraShake.StopShake());
        }

        [Test]
        public void CameraShake_MultiplePlayCalls_DoesNotThrow()
        {
            // Calling multiple times should safely interrupt previous shakes
            Assert.DoesNotThrow(() =>
            {
                cameraShake.PlayShake();
                cameraShake.PlayShake();
                cameraShake.PlayFireShake();
                cameraShake.PlayDamageShake();
            });
        }

        [Test]
        public void CameraShake_ShakePreset_HasValidDefaults()
        {
            // Test ShakePreset struct creation
            CameraShake.ShakePreset preset = new CameraShake.ShakePreset(0.5f, 0.2f, 25f);

            Assert.AreEqual(0.5f, preset.Duration);
            Assert.AreEqual(0.2f, preset.Magnitude);
            Assert.AreEqual(25f, preset.Frequency);
        }

        [Test]
        public void CameraShake_Events_CanBeSubscribed()
        {
            bool startedFired = false;
            bool endedFired = false;

            cameraShake.OnShakeStarted += () => startedFired = true;
            cameraShake.OnShakeEnded += () => endedFired = true;

            // Events won't fire in edit mode without update loop,
            // but subscription should work without throwing
            Assert.IsFalse(startedFired);
            Assert.IsFalse(endedFired);
        }
    }
}
