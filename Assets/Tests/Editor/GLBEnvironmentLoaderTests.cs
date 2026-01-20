#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using CityShooter.Environment;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for GLBEnvironmentLoader component.
    /// </summary>
    [TestFixture]
    public class GLBEnvironmentLoaderTests
    {
        private GameObject _testObject;
        private GLBEnvironmentLoader _loader;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestGLBLoader");
            _loader = _testObject.AddComponent<GLBEnvironmentLoader>();
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
        public void GLBEnvironmentLoader_InitializesCorrectly()
        {
            Assert.IsNotNull(_loader);
            Assert.IsFalse(_loader.IsLoading);
            Assert.AreEqual(0f, _loader.LoadProgress);
            Assert.IsNull(_loader.LoadedEnvironment);
        }

        [Test]
        public void GLBEnvironmentLoader_ProgressStartsAtZero()
        {
            Assert.AreEqual(0f, _loader.LoadProgress);
        }

        [Test]
        public void GLBEnvironmentLoader_IsNotLoadingByDefault()
        {
            Assert.IsFalse(_loader.IsLoading);
        }

        [Test]
        public void GLBEnvironmentLoader_LoadedEnvironmentNullByDefault()
        {
            Assert.IsNull(_loader.LoadedEnvironment);
        }

        [Test]
        public void GLBEnvironmentLoader_UnloadWhenNothingLoaded()
        {
            // Should not throw when nothing is loaded
            Assert.DoesNotThrow(() => _loader.UnloadEnvironment());
        }

        [Test]
        public void GLBEnvironmentLoader_EventsCanBeSubscribed()
        {
            bool startedCalled = false;
            float lastProgress = -1f;
            GameObject loadedObject = null;
            string errorMessage = null;

            _loader.OnLoadStarted += () => startedCalled = true;
            _loader.OnProgressUpdated += (p) => lastProgress = p;
            _loader.OnLoadComplete += (go) => loadedObject = go;
            _loader.OnLoadError += (err) => errorMessage = err;

            // Events should be subscribable without throwing
            Assert.IsFalse(startedCalled);
            Assert.AreEqual(-1f, lastProgress);
            Assert.IsNull(loadedObject);
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void GLBEnvironmentLoader_LoadEnvironmentWithPath()
        {
            // Should not throw even with a custom path
            // Note: This will fail to load since the path doesn't exist,
            // but it should handle the error gracefully
            Assert.DoesNotThrow(() => _loader.LoadEnvironment("test/path.glb"));
        }
    }
}
#endif
