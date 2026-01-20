using NUnit.Framework;
using UnityEngine;
using CityShooter.Core;

namespace CityShooter.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the ObjectPool system.
    /// </summary>
    [TestFixture]
    public class ObjectPoolTests
    {
        private GameObject _testPrefab;
        private Transform _poolParent;

        [SetUp]
        public void SetUp()
        {
            // Create a test prefab
            _testPrefab = new GameObject("TestPrefab");
            _testPrefab.AddComponent<TestComponent>();

            // Create a parent for pooled objects
            _poolParent = new GameObject("PoolParent").transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testPrefab != null)
            {
                Object.DestroyImmediate(_testPrefab);
            }

            if (_poolParent != null)
            {
                Object.DestroyImmediate(_poolParent.gameObject);
            }
        }

        [Test]
        public void Constructor_CreatesInitialObjects()
        {
            // Arrange & Act
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 5,
                parent: _poolParent
            );

            // Assert
            Assert.AreEqual(5, pool.AvailableCount);
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(5, pool.TotalCount);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Get_ReturnsObjectFromPool()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 3,
                parent: _poolParent
            );

            // Act
            TestComponent obj = pool.Get();

            // Assert
            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.gameObject.activeInHierarchy);
            Assert.AreEqual(2, pool.AvailableCount);
            Assert.AreEqual(1, pool.ActiveCount);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Get_WithPosition_SetsTransform()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 1,
                parent: _poolParent
            );
            Vector3 position = new Vector3(1, 2, 3);
            Quaternion rotation = Quaternion.Euler(45, 90, 0);

            // Act
            TestComponent obj = pool.Get(position, rotation);

            // Assert
            Assert.AreEqual(position, obj.transform.position);
            Assert.IsTrue(Quaternion.Angle(rotation, obj.transform.rotation) < 0.01f);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Return_ReturnsObjectToPool()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 2,
                parent: _poolParent
            );
            TestComponent obj = pool.Get();

            // Act
            pool.Return(obj);

            // Assert
            Assert.IsFalse(obj.gameObject.activeInHierarchy);
            Assert.AreEqual(2, pool.AvailableCount);
            Assert.AreEqual(0, pool.ActiveCount);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void ReturnAll_ReturnsAllActiveObjects()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 5,
                parent: _poolParent
            );

            // Get multiple objects
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            var obj3 = pool.Get();

            Assert.AreEqual(3, pool.ActiveCount);

            // Act
            pool.ReturnAll();

            // Assert
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(5, pool.AvailableCount);
            Assert.IsFalse(obj1.gameObject.activeInHierarchy);
            Assert.IsFalse(obj2.gameObject.activeInHierarchy);
            Assert.IsFalse(obj3.gameObject.activeInHierarchy);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Get_ExpandsPool_WhenExpandable()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 2,
                maxSize: 0, // Unlimited
                parent: _poolParent,
                expandable: true
            );

            // Get all initial objects
            pool.Get();
            pool.Get();

            // Act - should expand
            TestComponent obj = pool.Get();

            // Assert
            Assert.IsNotNull(obj);
            Assert.AreEqual(3, pool.TotalCount);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Get_ReturnsNull_WhenNotExpandableAndEmpty()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 1,
                maxSize: 1,
                parent: _poolParent,
                expandable: false
            );

            // Get the only object
            pool.Get();

            // Act
            TestComponent obj = pool.Get();

            // Assert
            Assert.IsNull(obj);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Get_RespectsMaxSize()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 2,
                maxSize: 3,
                parent: _poolParent,
                expandable: true
            );

            // Get all initial and expand to max
            pool.Get();
            pool.Get();
            pool.Get(); // Expands to 3

            // Act - should return null at max
            TestComponent obj = pool.Get();

            // Assert
            Assert.IsNull(obj);
            Assert.AreEqual(3, pool.TotalCount);

            // Cleanup
            pool.Clear();
        }

        [Test]
        public void Clear_DestroysAllObjects()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 3,
                parent: _poolParent
            );

            var activeObj = pool.Get();

            // Act
            pool.Clear();

            // Assert
            Assert.AreEqual(0, pool.AvailableCount);
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(0, pool.TotalCount);
        }

        [Test]
        public void HasAvailable_ReturnsCorrectState()
        {
            // Arrange
            var pool = new ObjectPool<TestComponent>(
                _testPrefab.GetComponent<TestComponent>(),
                initialSize: 1,
                maxSize: 1,
                parent: _poolParent,
                expandable: false
            );

            // Initially should have available
            Assert.IsTrue(pool.HasAvailable);

            // Get the only object
            pool.Get();

            // Should not have available
            Assert.IsFalse(pool.HasAvailable);

            // Cleanup
            pool.Clear();
        }

        /// <summary>
        /// Test component for object pool testing.
        /// </summary>
        private class TestComponent : MonoBehaviour
        {
        }
    }
}
