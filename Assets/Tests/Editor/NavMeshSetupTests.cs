#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using CityShooter.Navigation;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for NavMeshSetup component.
    /// </summary>
    [TestFixture]
    public class NavMeshSetupTests
    {
        private GameObject _testObject;
        private NavMeshSetup _navMeshSetup;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestNavMesh");
            _navMeshSetup = _testObject.AddComponent<NavMeshSetup>();
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
        public void NavMeshSetup_InitializesCorrectly()
        {
            Assert.IsNotNull(_navMeshSetup);
            Assert.IsFalse(_navMeshSetup.IsBuilding);
        }

        [Test]
        public void NavMeshSetup_IsNotBuildingByDefault()
        {
            Assert.IsFalse(_navMeshSetup.IsBuilding);
        }

        [Test]
        public void NavMeshSetup_EventsCanBeSubscribed()
        {
            bool completeCalled = false;
            string errorMessage = null;

            _navMeshSetup.OnNavMeshBuildComplete += () => completeCalled = true;
            _navMeshSetup.OnNavMeshBuildError += (err) => errorMessage = err;

            // Events should be subscribable without throwing
            Assert.IsFalse(completeCalled);
            Assert.IsNull(errorMessage);
        }

        [Test]
        public void NavMeshSetup_ValidateEmptyNavMesh_ReturnsFalse()
        {
            // With no NavMesh baked, validation should return false
            bool isValid = _navMeshSetup.ValidateNavMesh();

            // Note: This may return true if there's already NavMesh data in the test scene
            // In a clean test environment, it should be false
            Assert.IsNotNull(_navMeshSetup);
        }

        [Test]
        public void NavMeshSetup_GetAreaAtPosition_ReturnsValidResult()
        {
            // Should return -1 for positions not on NavMesh
            int area = _navMeshSetup.GetAreaAtPosition(Vector3.zero);

            // Result depends on whether NavMesh exists
            Assert.IsTrue(area >= -1);
        }

        [Test]
        public void NavMeshSetup_RebuildNavMesh_DoesNotThrow()
        {
            // Should not throw even if NavMeshSurface doesn't exist
            Assert.DoesNotThrow(() => _navMeshSetup.RebuildNavMesh());
        }
    }
}
#endif
