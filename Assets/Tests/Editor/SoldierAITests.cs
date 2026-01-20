using NUnit.Framework;
using UnityEngine;
using CityShooter.Enemy;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for the Soldier AI system.
    /// These tests validate the core logic without requiring a running scene.
    /// </summary>
    [TestFixture]
    public class SoldierAITests
    {
        #region State Machine Tests

        [Test]
        public void SoldierState_HasAllRequiredStates()
        {
            // Verify all required states exist in the enum
            Assert.IsTrue(System.Enum.IsDefined(typeof(SoldierAI.SoldierState), SoldierAI.SoldierState.Idle));
            Assert.IsTrue(System.Enum.IsDefined(typeof(SoldierAI.SoldierState), SoldierAI.SoldierState.Chase));
            Assert.IsTrue(System.Enum.IsDefined(typeof(SoldierAI.SoldierState), SoldierAI.SoldierState.Attack));
            Assert.IsTrue(System.Enum.IsDefined(typeof(SoldierAI.SoldierState), SoldierAI.SoldierState.React));
            Assert.IsTrue(System.Enum.IsDefined(typeof(SoldierAI.SoldierState), SoldierAI.SoldierState.Death));
        }

        [Test]
        public void SoldierState_EnumValuesAreUnique()
        {
            var values = System.Enum.GetValues(typeof(SoldierAI.SoldierState));
            var uniqueCount = new System.Collections.Generic.HashSet<int>();

            foreach (var value in values)
            {
                uniqueCount.Add((int)value);
            }

            Assert.AreEqual(values.Length, uniqueCount.Count, "Enum values should be unique");
        }

        #endregion
    }

    /// <summary>
    /// Unit tests for the EnemyHealth system.
    /// </summary>
    [TestFixture]
    public class EnemyHealthTests
    {
        private GameObject _testObject;
        private EnemyHealth _health;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestEnemy");
            _health = _testObject.AddComponent<EnemyHealth>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        [Test]
        public void EnemyHealth_StartsAlive()
        {
            Assert.IsTrue(_health.IsAlive, "Enemy should start alive");
        }

        [Test]
        public void EnemyHealth_HealthPercentage_StartsAtFull()
        {
            Assert.AreEqual(1f, _health.HealthPercentage, 0.001f, "Health percentage should start at 100%");
        }

        [Test]
        public void EnemyHealth_MaxHealth_IsPositive()
        {
            Assert.Greater(_health.MaxHealth, 0f, "Max health should be positive");
        }

        [Test]
        public void EnemyHealth_ImplementsIDamageable()
        {
            Assert.IsInstanceOf<Interfaces.IDamageable>(_health, "EnemyHealth should implement IDamageable");
        }

        [Test]
        public void IDamageable_TakeDamage_ReducesHealth()
        {
            float initialHealth = _health.CurrentHealth;
            _health.TakeDamage(10f);

            Assert.Less(_health.CurrentHealth, initialHealth, "Health should decrease after taking damage");
        }

        [Test]
        public void IDamageable_TakeDamage_WithHitPoint_ReducesHealth()
        {
            float initialHealth = _health.CurrentHealth;
            _health.TakeDamage(10f, Vector3.zero);

            Assert.Less(_health.CurrentHealth, initialHealth, "Health should decrease after taking damage with hit point");
        }

        [Test]
        public void IDamageable_GetHealthPercentage_ReturnsValidRange()
        {
            float percentage = _health.GetHealthPercentage();

            Assert.GreaterOrEqual(percentage, 0f, "Health percentage should not be negative");
            Assert.LessOrEqual(percentage, 1f, "Health percentage should not exceed 1");
        }

        [Test]
        public void IDamageable_GetIsAlive_ReturnsTrueWhenHealthy()
        {
            Assert.IsTrue(_health.GetIsAlive(), "GetIsAlive should return true when health > 0");
        }

        [Test]
        public void EnemyHealth_Heal_IncreasesHealth()
        {
            _health.TakeDamage(50f);
            float damagedHealth = _health.CurrentHealth;

            _health.Heal(25f);

            Assert.Greater(_health.CurrentHealth, damagedHealth, "Health should increase after healing");
        }

        [Test]
        public void EnemyHealth_Heal_DoesNotExceedMaxHealth()
        {
            _health.TakeDamage(10f);
            _health.Heal(1000f); // Heal way more than max

            Assert.LessOrEqual(_health.CurrentHealth, _health.MaxHealth, "Health should not exceed max health");
        }

        [Test]
        public void EnemyHealth_ZeroDamage_DoesNotAffectHealth()
        {
            float initialHealth = _health.CurrentHealth;
            _health.TakeDamage(0f);

            Assert.AreEqual(initialHealth, _health.CurrentHealth, "Zero damage should not affect health");
        }

        [Test]
        public void EnemyHealth_NegativeDamage_DoesNotAffectHealth()
        {
            float initialHealth = _health.CurrentHealth;
            _health.TakeDamage(-10f);

            Assert.AreEqual(initialHealth, _health.CurrentHealth, "Negative damage should not affect health");
        }

        [Test]
        public void EnemyHealth_Kill_SetsHealthToZero()
        {
            _health.Kill();

            Assert.AreEqual(0f, _health.CurrentHealth, "Kill should set health to zero");
            Assert.IsFalse(_health.IsAlive, "Kill should mark as not alive");
        }

        [Test]
        public void EnemyHealth_OnDeathEvent_FiresWhenKilled()
        {
            bool eventFired = false;
            _health.OnDeath += () => eventFired = true;

            _health.Kill();

            Assert.IsTrue(eventFired, "OnDeath event should fire when killed");
        }

        [Test]
        public void EnemyHealth_OnDamageTakenEvent_FiresWhenDamaged()
        {
            bool eventFired = false;
            float reportedDamage = 0f;
            _health.OnDamageTaken += (damage, hitPoint) =>
            {
                eventFired = true;
                reportedDamage = damage;
            };

            _health.TakeDamage(15f);

            Assert.IsTrue(eventFired, "OnDamageTaken event should fire when damaged");
            Assert.AreEqual(15f, reportedDamage, "Event should report correct damage amount");
        }

        [Test]
        public void EnemyHealth_OnHealthChangedEvent_FiresWhenDamaged()
        {
            bool eventFired = false;
            _health.OnHealthChanged += (current, max) => eventFired = true;

            _health.TakeDamage(10f);

            Assert.IsTrue(eventFired, "OnHealthChanged event should fire when damaged");
        }

        [Test]
        public void EnemyHealth_SetHealth_UpdatesCurrentHealth()
        {
            _health.SetHealth(50f);

            Assert.AreEqual(50f, _health.CurrentHealth, "SetHealth should update current health");
        }

        [Test]
        public void EnemyHealth_SetHealth_ClampsToMax()
        {
            _health.SetHealth(1000f);

            Assert.LessOrEqual(_health.CurrentHealth, _health.MaxHealth, "SetHealth should clamp to max health");
        }

        [Test]
        public void EnemyHealth_ResetHealth_RestoresFullHealth()
        {
            _health.TakeDamage(50f);
            _health.ResetHealth();

            Assert.AreEqual(_health.MaxHealth, _health.CurrentHealth, "ResetHealth should restore to max health");
        }
    }

    /// <summary>
    /// Unit tests for the IDamageable interface.
    /// </summary>
    [TestFixture]
    public class IDamageableTests
    {
        [Test]
        public void IDamageable_InterfaceExists()
        {
            Assert.IsNotNull(typeof(Interfaces.IDamageable), "IDamageable interface should exist");
        }

        [Test]
        public void IDamageable_HasTakeDamageMethod()
        {
            var method = typeof(Interfaces.IDamageable).GetMethod("TakeDamage", new[] { typeof(float) });
            Assert.IsNotNull(method, "IDamageable should have TakeDamage(float) method");
        }

        [Test]
        public void IDamageable_HasTakeDamageWithHitPointMethod()
        {
            var method = typeof(Interfaces.IDamageable).GetMethod("TakeDamage", new[] { typeof(float), typeof(Vector3) });
            Assert.IsNotNull(method, "IDamageable should have TakeDamage(float, Vector3) method");
        }

        [Test]
        public void IDamageable_HasGetHealthPercentageMethod()
        {
            var method = typeof(Interfaces.IDamageable).GetMethod("GetHealthPercentage");
            Assert.IsNotNull(method, "IDamageable should have GetHealthPercentage method");
        }

        [Test]
        public void IDamageable_HasGetIsAliveMethod()
        {
            var method = typeof(Interfaces.IDamageable).GetMethod("GetIsAlive");
            Assert.IsNotNull(method, "IDamageable should have GetIsAlive method");
        }
    }
}
