using NUnit.Framework;
using UnityEngine;
using CityShooter.Weapons;

namespace CityShooter.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the LaserHitInfo struct.
    /// </summary>
    [TestFixture]
    public class LaserHitInfoTests
    {
        [Test]
        public void LaserHitInfo_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            LaserHitInfo hitInfo = new LaserHitInfo();

            // Assert
            Assert.AreEqual(Vector3.zero, hitInfo.Origin);
            Assert.AreEqual(Vector3.zero, hitInfo.Direction);
            Assert.AreEqual(0f, hitInfo.MaxRange);
            Assert.IsFalse(hitInfo.DidHit);
            Assert.AreEqual(Vector3.zero, hitInfo.HitPoint);
            Assert.AreEqual(Vector3.zero, hitInfo.HitNormal);
            Assert.AreEqual(0f, hitInfo.HitDistance);
            Assert.IsNull(hitInfo.HitCollider);
            Assert.IsNull(hitInfo.HitTransform);
            Assert.IsFalse(hitInfo.IsEnemyHit);
            Assert.IsNull(hitInfo.DamageReceiver);
        }

        [Test]
        public void LaserHitInfo_CanSetAllProperties()
        {
            // Arrange
            Vector3 origin = new Vector3(1, 2, 3);
            Vector3 direction = Vector3.forward;
            Vector3 hitPoint = new Vector3(1, 2, 103);
            Vector3 hitNormal = Vector3.back;

            // Act
            LaserHitInfo hitInfo = new LaserHitInfo
            {
                Origin = origin,
                Direction = direction,
                MaxRange = 100f,
                DidHit = true,
                HitPoint = hitPoint,
                HitNormal = hitNormal,
                HitDistance = 100f,
                IsEnemyHit = true
            };

            // Assert
            Assert.AreEqual(origin, hitInfo.Origin);
            Assert.AreEqual(direction, hitInfo.Direction);
            Assert.AreEqual(100f, hitInfo.MaxRange);
            Assert.IsTrue(hitInfo.DidHit);
            Assert.AreEqual(hitPoint, hitInfo.HitPoint);
            Assert.AreEqual(hitNormal, hitInfo.HitNormal);
            Assert.AreEqual(100f, hitInfo.HitDistance);
            Assert.IsTrue(hitInfo.IsEnemyHit);
        }

        [Test]
        public void LaserHitInfo_MissScenario_HasCorrectValues()
        {
            // Arrange & Act
            LaserHitInfo hitInfo = new LaserHitInfo
            {
                Origin = Vector3.zero,
                Direction = Vector3.forward,
                MaxRange = 100f,
                DidHit = false,
                HitPoint = Vector3.forward * 100f, // End point at max range
                HitDistance = 100f
            };

            // Assert
            Assert.IsFalse(hitInfo.DidHit);
            Assert.AreEqual(Vector3.forward * 100f, hitInfo.HitPoint);
            Assert.AreEqual(100f, hitInfo.HitDistance);
        }

        [Test]
        public void LaserHitInfo_HitScenario_HasCorrectValues()
        {
            // Arrange
            Vector3 origin = new Vector3(0, 1, 0);
            Vector3 direction = Vector3.forward;
            float hitDistance = 50f;
            Vector3 hitPoint = origin + direction * hitDistance;

            // Act
            LaserHitInfo hitInfo = new LaserHitInfo
            {
                Origin = origin,
                Direction = direction,
                MaxRange = 100f,
                DidHit = true,
                HitPoint = hitPoint,
                HitNormal = -direction,
                HitDistance = hitDistance,
                IsEnemyHit = true
            };

            // Assert
            Assert.IsTrue(hitInfo.DidHit);
            Assert.AreEqual(hitPoint, hitInfo.HitPoint);
            Assert.Less(hitInfo.HitDistance, hitInfo.MaxRange);
            Assert.IsTrue(hitInfo.IsEnemyHit);
        }

        [Test]
        public void LaserHitInfo_StructCopy_CreatesIndependentCopy()
        {
            // Arrange
            LaserHitInfo original = new LaserHitInfo
            {
                Origin = Vector3.one,
                DidHit = true,
                HitDistance = 50f
            };

            // Act
            LaserHitInfo copy = original;
            copy.Origin = Vector3.zero;
            copy.DidHit = false;
            copy.HitDistance = 100f;

            // Assert - original should be unchanged (struct semantics)
            Assert.AreEqual(Vector3.one, original.Origin);
            Assert.IsTrue(original.DidHit);
            Assert.AreEqual(50f, original.HitDistance);

            // Copy should have new values
            Assert.AreEqual(Vector3.zero, copy.Origin);
            Assert.IsFalse(copy.DidHit);
            Assert.AreEqual(100f, copy.HitDistance);
        }
    }
}
