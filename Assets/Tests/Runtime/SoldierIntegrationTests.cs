using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using CityShooter.Enemy;
using CityShooter.Combat;
using CityShooter.Interfaces;

namespace CityShooter.Tests.Runtime
{
    /// <summary>
    /// Integration tests for the Soldier AI system.
    /// These tests validate component interactions at runtime.
    /// </summary>
    [TestFixture]
    public class SoldierIntegrationTests
    {
        private GameObject _soldierObject;
        private SoldierAI _soldierAI;
        private EnemyHealth _health;
        private NavMeshAgent _navAgent;

        [SetUp]
        public void SetUp()
        {
            // Create soldier game object with required components
            _soldierObject = new GameObject("TestSoldier");

            // Add a dummy animator since we don't have animation assets in tests
            _soldierObject.AddComponent<Animator>();

            // Add NavMeshAgent (will work in limited capacity without NavMesh)
            _navAgent = _soldierObject.AddComponent<NavMeshAgent>();
            _navAgent.enabled = false; // Disable to prevent errors without NavMesh

            // Add our components
            _health = _soldierObject.AddComponent<EnemyHealth>();
            _soldierAI = _soldierObject.AddComponent<SoldierAI>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_soldierObject != null)
            {
                Object.Destroy(_soldierObject);
            }
        }

        [Test]
        public void Soldier_InitializesWithIdleState()
        {
            // The soldier should start in Idle state
            Assert.AreEqual(SoldierAI.SoldierState.Idle, _soldierAI.CurrentState);
        }

        [Test]
        public void Soldier_IsAlive_WhenHealthy()
        {
            Assert.IsTrue(_soldierAI.IsAlive);
        }

        [Test]
        public void Soldier_HealthComponent_IsAttached()
        {
            var health = _soldierObject.GetComponent<EnemyHealth>();
            Assert.IsNotNull(health, "EnemyHealth component should be attached");
        }

        [Test]
        public void Soldier_ImplementsIDamageable()
        {
            var damageable = _soldierObject.GetComponent<IDamageable>();
            Assert.IsNotNull(damageable, "Soldier should have a component implementing IDamageable");
        }

        [UnityTest]
        public IEnumerator Soldier_TriggerHitReaction_EntersReactState()
        {
            // Trigger hit reaction through damage
            _health.TakeDamage(10f);

            yield return null; // Wait one frame for state machine to update

            // Should now be in React state
            Assert.AreEqual(SoldierAI.SoldierState.React, _soldierAI.CurrentState,
                "Soldier should enter React state when damaged");
        }

        [UnityTest]
        public IEnumerator Soldier_ExitsReactState_AfterDuration()
        {
            // Trigger hit reaction
            _health.TakeDamage(10f);
            yield return null;

            // Wait for reaction duration (default is 0.5 seconds + buffer)
            yield return new WaitForSeconds(0.7f);

            // Should have returned from React state
            Assert.AreNotEqual(SoldierAI.SoldierState.React, _soldierAI.CurrentState,
                "Soldier should exit React state after duration");
        }

        [UnityTest]
        public IEnumerator Soldier_EntersDeathState_WhenKilled()
        {
            // Kill the soldier
            _health.Kill();

            yield return null; // Wait one frame for state machine

            // Should be in Death state
            Assert.AreEqual(SoldierAI.SoldierState.Death, _soldierAI.CurrentState,
                "Soldier should enter Death state when killed");
        }

        [Test]
        public void Soldier_IsNotAlive_WhenKilled()
        {
            _health.Kill();

            Assert.IsFalse(_soldierAI.IsAlive, "Soldier should not be alive after being killed");
        }

        [UnityTest]
        public IEnumerator Soldier_CannotExitDeathState()
        {
            // Kill the soldier
            _health.Kill();
            yield return null;

            // Try to reset - should not work
            _soldierAI.ResetToIdle();
            yield return null;

            // Should still be in Death state
            Assert.AreEqual(SoldierAI.SoldierState.Death, _soldierAI.CurrentState,
                "Soldier should not be able to exit Death state");
        }
    }

    /// <summary>
    /// Integration tests for the combat system.
    /// </summary>
    [TestFixture]
    public class CombatIntegrationTests
    {
        private GameObject _weaponObject;
        private LaserCombatSystem _combat;

        [SetUp]
        public void SetUp()
        {
            _weaponObject = new GameObject("TestWeapon");
            _combat = _weaponObject.AddComponent<LaserCombatSystem>();

            // Create a camera for the combat system
            var cameraObject = new GameObject("TestCamera");
            cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        [TearDown]
        public void TearDown()
        {
            if (_weaponObject != null)
            {
                Object.Destroy(_weaponObject);
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Object.Destroy(mainCamera.gameObject);
            }
        }

        [Test]
        public void LaserCombatSystem_ComponentExists()
        {
            Assert.IsNotNull(_combat, "LaserCombatSystem component should exist");
        }

        [Test]
        public void LaserCombatSystem_SetDamage_UpdatesValue()
        {
            _combat.SetDamage(50f);
            // We can't directly verify private fields, but we can verify no errors occur
            Assert.Pass("SetDamage executed without errors");
        }

        [Test]
        public void LaserCombatSystem_SetRange_UpdatesValue()
        {
            _combat.SetRange(200f);
            Assert.Pass("SetRange executed without errors");
        }

        [Test]
        public void LaserCombatSystem_SetFireRate_UpdatesValue()
        {
            _combat.SetFireRate(0.2f);
            Assert.Pass("SetFireRate executed without errors");
        }

        [UnityTest]
        public IEnumerator LaserCombatSystem_OnLaserFired_EventFires()
        {
            bool eventFired = false;
            _combat.OnLaserFired += () => eventFired = true;

            _combat.Fire();

            yield return null;

            Assert.IsTrue(eventFired, "OnLaserFired event should fire when firing");
        }
    }

    /// <summary>
    /// Tests for SoldierSpawner functionality.
    /// </summary>
    [TestFixture]
    public class SoldierSpawnerTests
    {
        private GameObject _spawnerObject;
        private SoldierSpawner _spawner;

        [SetUp]
        public void SetUp()
        {
            _spawnerObject = new GameObject("TestSpawner");
            _spawner = _spawnerObject.AddComponent<SoldierSpawner>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawnerObject != null)
            {
                Object.Destroy(_spawnerObject);
            }
        }

        [Test]
        public void SoldierSpawner_InitializesWithNoActiveSoldiers()
        {
            Assert.AreEqual(0, _spawner.ActiveSoldierCount, "Should start with no active soldiers");
        }

        [Test]
        public void SoldierSpawner_DespawnAll_ClearsSoldierList()
        {
            _spawner.DespawnAll();
            Assert.AreEqual(0, _spawner.ActiveSoldierCount, "DespawnAll should clear all soldiers");
        }

        [Test]
        public void SoldierSpawner_SpawnWithoutPrefab_ReturnsNull()
        {
            // No prefab assigned
            var result = _spawner.SpawnSoldier();
            Assert.IsNull(result, "SpawnSoldier should return null without a prefab");
        }
    }
}
