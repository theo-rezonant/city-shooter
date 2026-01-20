#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using CityShooter.Loading;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for AsyncLevelLoader component.
    /// </summary>
    [TestFixture]
    public class AsyncLevelLoaderTests
    {
        private GameObject _testObject;
        private AsyncLevelLoader _loader;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestLoader");
            _loader = _testObject.AddComponent<AsyncLevelLoader>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void AsyncLevelLoader_InitializesCorrectly()
        {
            Assert.IsNotNull(_loader);
            Assert.IsFalse(_loader.IsLoading);
            Assert.AreEqual(0f, _loader.Progress);
        }

        [Test]
        public void AsyncLevelLoader_ProgressStartsAtZero()
        {
            Assert.AreEqual(0f, _loader.Progress);
        }

        [Test]
        public void AsyncLevelLoader_IsLoadingDefaultFalse()
        {
            Assert.IsFalse(_loader.IsLoading);
        }

        [Test]
        public void AsyncLevelLoader_CancelLoadingWhenNotLoading()
        {
            // Should not throw when cancelling while not loading
            Assert.DoesNotThrow(() => _loader.CancelLoading());
        }

        [Test]
        public void AsyncLevelLoader_ActivateSceneWhenNoOperation()
        {
            // Should not throw when no async operation exists
            Assert.DoesNotThrow(() => _loader.ActivateScene());
        }

        [Test]
        public void AsyncLevelLoader_EventsInitiallyNull()
        {
            // Events should be subscribable
            bool startedCalled = false;
            bool completeCalled = false;
            bool progressCalled = false;
            bool errorCalled = false;

            _loader.OnLoadingStarted += () => startedCalled = true;
            _loader.OnLoadingComplete += () => completeCalled = true;
            _loader.OnProgressUpdated += (p) => progressCalled = true;
            _loader.OnLoadingError += (e) => errorCalled = true;

            // Events should not be called until loading starts
            Assert.IsFalse(startedCalled);
            Assert.IsFalse(completeCalled);
            Assert.IsFalse(progressCalled);
            Assert.IsFalse(errorCalled);
        }

        [Test]
        public void AsyncLevelLoader_MultipleSubscribers()
        {
            int callCount = 0;

            _loader.OnProgressUpdated += (p) => callCount++;
            _loader.OnProgressUpdated += (p) => callCount++;

            // Verify multiple subscribers don't cause issues
            Assert.AreEqual(0, callCount);
        }
    }
}
#endif
