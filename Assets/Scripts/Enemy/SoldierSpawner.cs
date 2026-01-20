using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CityShooter.Enemy
{
    /// <summary>
    /// Spawns and manages Soldier enemies in the scene.
    /// Uses NavMesh sampling to ensure soldiers spawn on valid navigation surfaces.
    /// </summary>
    public class SoldierSpawner : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Prefab References")]
        [SerializeField] private GameObject soldierPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private int maxSoldiers = 10;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private float spawnRadius = 30f;
        [SerializeField] private float minSpawnDistanceFromPlayer = 10f;
        [SerializeField] private bool autoSpawn = true;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool useRandomSpawnPositions = true;

        [Header("NavMesh Settings")]
        [SerializeField] private float navMeshSampleRadius = 5f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        #endregion

        #region Private Fields

        private List<SoldierAI> _activeSoldiers = new List<SoldierAI>();
        private float _nextSpawnTime;

        #endregion

        #region Properties

        public int ActiveSoldierCount => _activeSoldiers.Count;
        public IReadOnlyList<SoldierAI> ActiveSoldiers => _activeSoldiers.AsReadOnly();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to find player if not assigned
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        private void Update()
        {
            if (!autoSpawn) return;

            // Clean up dead soldiers
            CleanupDeadSoldiers();

            // Spawn new soldiers if under limit
            if (_activeSoldiers.Count < maxSoldiers && Time.time >= _nextSpawnTime)
            {
                SpawnSoldier();
                _nextSpawnTime = Time.time + spawnInterval;
            }
        }

        #endregion

        #region Spawning

        /// <summary>
        /// Spawns a single soldier at a valid location.
        /// </summary>
        /// <returns>The spawned SoldierAI component, or null if spawn failed.</returns>
        public SoldierAI SpawnSoldier()
        {
            if (soldierPrefab == null)
            {
                Debug.LogError("SoldierSpawner: No soldier prefab assigned!");
                return null;
            }

            Vector3 spawnPosition;

            if (useRandomSpawnPositions)
            {
                if (!TryGetRandomSpawnPosition(out spawnPosition))
                {
                    Debug.LogWarning("SoldierSpawner: Failed to find valid spawn position");
                    return null;
                }
            }
            else if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                spawnPosition = spawnPoint.position;
            }
            else
            {
                Debug.LogWarning("SoldierSpawner: No spawn points configured and random spawning disabled");
                return null;
            }

            // Spawn the soldier
            GameObject soldierObj = Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);
            soldierObj.name = $"Soldier_{_activeSoldiers.Count}";

            SoldierAI soldier = soldierObj.GetComponent<SoldierAI>();

            if (soldier != null)
            {
                // Set up the soldier
                if (playerTransform != null)
                {
                    soldier.SetTarget(playerTransform);
                }

                // Subscribe to death event for cleanup
                soldier.OnDeath += () => OnSoldierDeath(soldier);

                _activeSoldiers.Add(soldier);

                Debug.Log($"Spawned soldier at {spawnPosition}");
            }
            else
            {
                Debug.LogError("SoldierSpawner: Prefab is missing SoldierAI component!");
                Destroy(soldierObj);
                return null;
            }

            return soldier;
        }

        /// <summary>
        /// Spawns a soldier at a specific position.
        /// </summary>
        /// <param name="position">World position to spawn at.</param>
        /// <returns>The spawned SoldierAI component, or null if spawn failed.</returns>
        public SoldierAI SpawnSoldierAt(Vector3 position)
        {
            if (soldierPrefab == null)
            {
                Debug.LogError("SoldierSpawner: No soldier prefab assigned!");
                return null;
            }

            // Validate position is on NavMesh
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleRadius, navMeshAreaMask))
            {
                position = hit.position;
            }
            else
            {
                Debug.LogWarning($"SoldierSpawner: Position {position} is not near NavMesh");
                return null;
            }

            GameObject soldierObj = Instantiate(soldierPrefab, position, Quaternion.identity);
            SoldierAI soldier = soldierObj.GetComponent<SoldierAI>();

            if (soldier != null)
            {
                if (playerTransform != null)
                {
                    soldier.SetTarget(playerTransform);
                }

                soldier.OnDeath += () => OnSoldierDeath(soldier);
                _activeSoldiers.Add(soldier);
            }

            return soldier;
        }

        /// <summary>
        /// Spawns multiple soldiers at once.
        /// </summary>
        /// <param name="count">Number of soldiers to spawn.</param>
        /// <returns>Number of soldiers successfully spawned.</returns>
        public int SpawnMultiple(int count)
        {
            int spawned = 0;

            for (int i = 0; i < count; i++)
            {
                if (_activeSoldiers.Count >= maxSoldiers) break;

                if (SpawnSoldier() != null)
                {
                    spawned++;
                }
            }

            return spawned;
        }

        #endregion

        #region Position Finding

        private bool TryGetRandomSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;

            for (int attempts = 0; attempts < 30; attempts++)
            {
                // Generate random position within spawn radius
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Check distance from player
                if (playerTransform != null)
                {
                    float distanceToPlayer = Vector3.Distance(randomPosition, playerTransform.position);
                    if (distanceToPlayer < minSpawnDistanceFromPlayer)
                    {
                        continue;
                    }
                }

                // Sample NavMesh to find valid position
                if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, navMeshSampleRadius, navMeshAreaMask))
                {
                    position = hit.position;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Management

        private void OnSoldierDeath(SoldierAI soldier)
        {
            // Soldier will be cleaned up in CleanupDeadSoldiers
            Debug.Log($"{soldier.gameObject.name} has been killed");
        }

        private void CleanupDeadSoldiers()
        {
            _activeSoldiers.RemoveAll(s => s == null || !s.IsAlive);
        }

        /// <summary>
        /// Removes all active soldiers from the scene.
        /// </summary>
        public void DespawnAll()
        {
            foreach (var soldier in _activeSoldiers)
            {
                if (soldier != null)
                {
                    Destroy(soldier.gameObject);
                }
            }

            _activeSoldiers.Clear();
        }

        /// <summary>
        /// Alerts all soldiers to the player's position.
        /// </summary>
        public void AlertAllSoldiers()
        {
            foreach (var soldier in _activeSoldiers)
            {
                if (soldier != null && soldier.IsAlive)
                {
                    soldier.AlertToPlayer();
                }
            }
        }

        /// <summary>
        /// Sets the player target for all active soldiers.
        /// </summary>
        /// <param name="player">The player transform to target.</param>
        public void SetPlayerTarget(Transform player)
        {
            playerTransform = player;

            foreach (var soldier in _activeSoldiers)
            {
                if (soldier != null)
                {
                    soldier.SetTarget(player);
                }
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            // Draw minimum player distance
            Gizmos.color = Color.red;
            if (playerTransform != null)
            {
                Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistanceFromPlayer);
            }

            // Draw spawn points
            if (spawnPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 1f);
                    }
                }
            }
        }

        #endregion
    }
}
