using NUnit.Framework;
using UnityEngine;
using CityShooter.Core;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for CombatEvents static event system.
    /// </summary>
    [TestFixture]
    public class CombatEventsTests
    {
        private bool eventFired;
        private Vector3 receivedVector;
        private int receivedIntA;
        private int receivedIntB;
        private float receivedFloatA;
        private float receivedFloatB;
        private bool receivedBool;

        [SetUp]
        public void SetUp()
        {
            CombatEvents.ClearAllSubscriptions();
            ResetTestVariables();
        }

        [TearDown]
        public void TearDown()
        {
            CombatEvents.ClearAllSubscriptions();
        }

        private void ResetTestVariables()
        {
            eventFired = false;
            receivedVector = Vector3.zero;
            receivedIntA = 0;
            receivedIntB = 0;
            receivedFloatA = 0f;
            receivedFloatB = 0f;
            receivedBool = false;
        }

        // ==================== OnPlayerFire Tests ====================

        [Test]
        public void OnPlayerFire_WhenInvoked_FiresEvent()
        {
            CombatEvents.OnPlayerFire += () => eventFired = true;

            CombatEvents.InvokePlayerFire();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnPlayerFire_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CombatEvents.InvokePlayerFire());
        }

        // ==================== OnEnemyHit Tests ====================

        [Test]
        public void OnEnemyHit_WhenInvoked_PassesHitPoint()
        {
            Vector3 hitPoint = new Vector3(1f, 2f, 3f);
            CombatEvents.OnEnemyHit += (point) => receivedVector = point;

            CombatEvents.InvokeEnemyHit(hitPoint);

            Assert.AreEqual(hitPoint, receivedVector);
        }

        [Test]
        public void OnEnemyHit_ZeroVector_PassesCorrectly()
        {
            receivedVector = Vector3.one; // Set to non-zero first
            CombatEvents.OnEnemyHit += (point) => receivedVector = point;

            CombatEvents.InvokeEnemyHit(Vector3.zero);

            Assert.AreEqual(Vector3.zero, receivedVector);
        }

        // ==================== OnAmmoChanged Tests ====================

        [Test]
        public void OnAmmoChanged_WhenInvoked_PassesBothValues()
        {
            CombatEvents.OnAmmoChanged += (current, max) =>
            {
                receivedIntA = current;
                receivedIntB = max;
            };

            CombatEvents.InvokeAmmoChanged(25, 30);

            Assert.AreEqual(25, receivedIntA);
            Assert.AreEqual(30, receivedIntB);
        }

        [Test]
        public void OnAmmoChanged_ZeroAmmo_PassesCorrectly()
        {
            CombatEvents.OnAmmoChanged += (current, max) =>
            {
                receivedIntA = current;
                receivedIntB = max;
            };

            CombatEvents.InvokeAmmoChanged(0, 30);

            Assert.AreEqual(0, receivedIntA);
        }

        // ==================== OnFiringStateChanged Tests ====================

        [Test]
        public void OnFiringStateChanged_True_PassesCorrectly()
        {
            CombatEvents.OnFiringStateChanged += (isFiring) => receivedBool = isFiring;

            CombatEvents.InvokeFiringStateChanged(true);

            Assert.IsTrue(receivedBool);
        }

        [Test]
        public void OnFiringStateChanged_False_PassesCorrectly()
        {
            receivedBool = true; // Set to true first
            CombatEvents.OnFiringStateChanged += (isFiring) => receivedBool = isFiring;

            CombatEvents.InvokeFiringStateChanged(false);

            Assert.IsFalse(receivedBool);
        }

        // ==================== OnReloadStateChanged Tests ====================

        [Test]
        public void OnReloadStateChanged_WhenInvoked_PassesBothValues()
        {
            CombatEvents.OnReloadStateChanged += (isReloading, duration) =>
            {
                receivedBool = isReloading;
                receivedFloatA = duration;
            };

            CombatEvents.InvokeReloadStateChanged(true, 2.5f);

            Assert.IsTrue(receivedBool);
            Assert.AreEqual(2.5f, receivedFloatA, 0.01f);
        }

        // ==================== OnHealthChanged Tests ====================

        [Test]
        public void OnHealthChanged_WhenInvoked_PassesBothValues()
        {
            CombatEvents.OnHealthChanged += (current, max) =>
            {
                receivedFloatA = current;
                receivedFloatB = max;
            };

            CombatEvents.InvokeHealthChanged(75f, 100f);

            Assert.AreEqual(75f, receivedFloatA, 0.01f);
            Assert.AreEqual(100f, receivedFloatB, 0.01f);
        }

        [Test]
        public void OnHealthChanged_ZeroHealth_PassesCorrectly()
        {
            CombatEvents.OnHealthChanged += (current, max) =>
            {
                receivedFloatA = current;
                receivedFloatB = max;
            };

            CombatEvents.InvokeHealthChanged(0f, 100f);

            Assert.AreEqual(0f, receivedFloatA, 0.01f);
        }

        // ==================== OnPlayerDamaged Tests ====================

        [Test]
        public void OnPlayerDamaged_WhenInvoked_PassesDamageSource()
        {
            Vector3 damageSource = new Vector3(10f, 0f, 5f);
            CombatEvents.OnPlayerDamaged += (source) => receivedVector = source;

            CombatEvents.InvokePlayerDamaged(damageSource);

            Assert.AreEqual(damageSource, receivedVector);
        }

        // ==================== OnPlayerMovementChanged Tests ====================

        [Test]
        public void OnPlayerMovementChanged_WhenInvoked_PassesBothValues()
        {
            CombatEvents.OnPlayerMovementChanged += (isMoving, speed) =>
            {
                receivedBool = isMoving;
                receivedFloatA = speed;
            };

            CombatEvents.InvokePlayerMovementChanged(true, 0.8f);

            Assert.IsTrue(receivedBool);
            Assert.AreEqual(0.8f, receivedFloatA, 0.01f);
        }

        [Test]
        public void OnPlayerMovementChanged_NotMoving_PassesCorrectly()
        {
            receivedBool = true;
            CombatEvents.OnPlayerMovementChanged += (isMoving, speed) =>
            {
                receivedBool = isMoving;
                receivedFloatA = speed;
            };

            CombatEvents.InvokePlayerMovementChanged(false, 0f);

            Assert.IsFalse(receivedBool);
            Assert.AreEqual(0f, receivedFloatA, 0.01f);
        }

        // ==================== ClearAllSubscriptions Tests ====================

        [Test]
        public void ClearAllSubscriptions_RemovesAllListeners()
        {
            CombatEvents.OnPlayerFire += () => eventFired = true;
            CombatEvents.OnEnemyHit += (point) => receivedVector = point;

            CombatEvents.ClearAllSubscriptions();

            CombatEvents.InvokePlayerFire();
            CombatEvents.InvokeEnemyHit(Vector3.one);

            Assert.IsFalse(eventFired);
            Assert.AreEqual(Vector3.zero, receivedVector);
        }

        // ==================== Multiple Subscribers Tests ====================

        [Test]
        public void OnPlayerFire_MultipleSubscribers_AllFired()
        {
            int callCount = 0;

            CombatEvents.OnPlayerFire += () => callCount++;
            CombatEvents.OnPlayerFire += () => callCount++;
            CombatEvents.OnPlayerFire += () => callCount++;

            CombatEvents.InvokePlayerFire();

            Assert.AreEqual(3, callCount);
        }
    }
}
