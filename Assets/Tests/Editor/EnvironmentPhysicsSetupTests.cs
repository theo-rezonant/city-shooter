#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using CityShooter.Environment;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for EnvironmentPhysicsSetup component.
    /// </summary>
    [TestFixture]
    public class EnvironmentPhysicsSetupTests
    {
        private GameObject _testObject;
        private EnvironmentPhysicsSetup _physicsSetup;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestEnvironment");
            _physicsSetup = _testObject.AddComponent<EnvironmentPhysicsSetup>();
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
        public void EnvironmentPhysicsSetup_InitializesCorrectly()
        {
            Assert.IsNotNull(_physicsSetup);
            Assert.IsFalse(_physicsSetup.IsGenerating);
            Assert.AreEqual(0, _physicsSetup.ColliderCount);
        }

        [Test]
        public void EnvironmentPhysicsSetup_ColliderCountStartsAtZero()
        {
            Assert.AreEqual(0, _physicsSetup.ColliderCount);
        }

        [Test]
        public void EnvironmentPhysicsSetup_IsNotGeneratingByDefault()
        {
            Assert.IsFalse(_physicsSetup.IsGenerating);
        }

        [Test]
        public void EnvironmentPhysicsSetup_RemoveCollidersWhenEmpty()
        {
            // Should not throw when no colliders exist
            Assert.DoesNotThrow(() => _physicsSetup.RemoveAllColliders());
            Assert.AreEqual(0, _physicsSetup.ColliderCount);
        }

        [Test]
        public void EnvironmentPhysicsSetup_WithChildMesh_CountsCorrectly()
        {
            // Create a child with a mesh
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = "TestCube";
            child.transform.SetParent(_testObject.transform);

            // Remove the default collider (the test adds its own)
            Object.DestroyImmediate(child.GetComponent<BoxCollider>());

            // Child should have a MeshFilter
            MeshFilter mf = child.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf);
            Assert.IsNotNull(mf.sharedMesh);
        }

        [Test]
        public void EnvironmentPhysicsSetup_EventsCanBeSubscribed()
        {
            bool completeCalled = false;
            float lastProgress = -1f;

            _physicsSetup.OnCollisionSetupComplete += () => completeCalled = true;
            _physicsSetup.OnProgressUpdated += (p) => lastProgress = p;

            // Events should be subscribable without throwing
            Assert.IsFalse(completeCalled);
            Assert.AreEqual(-1f, lastProgress);
        }
    }
}
#endif
