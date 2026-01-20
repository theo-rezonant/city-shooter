using UnityEngine;
using CityShooter.Navigation;

namespace CityShooter.Environment
{
    /// <summary>
    /// Initializes the Town scene after it has been loaded.
    /// Handles post-load setup including physics, NavMesh validation, and player spawning.
    /// </summary>
    public class TownSceneInitializer : MonoBehaviour
    {
        [Header("Scene Components")]
        [SerializeField] private EnvironmentPhysicsSetup physicsSetup;
        [SerializeField] private NavMeshSetup navMeshSetup;
        [SerializeField] private GLBEnvironmentLoader environmentLoader;

        [Header("Player Configuration")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private bool spawnPlayerOnInit = true;

        [Header("Environment Settings")]
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool setupOcclusionCulling = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private bool _isInitialized;
        private GameObject _spawnedPlayer;

        /// <summary>
        /// Event fired when initialization is complete.
        /// </summary>
        public event System.Action OnInitializationComplete;

        /// <summary>
        /// Event fired when initialization fails.
        /// </summary>
        public event System.Action<string> OnInitializationError;

        private void Start()
        {
            InitializeScene();
        }

        /// <summary>
        /// Initializes the town scene.
        /// </summary>
        public void InitializeScene()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[TownSceneInitializer] Scene already initialized.");
                return;
            }

            StartCoroutine(InitializeCoroutine());
        }

        private System.Collections.IEnumerator InitializeCoroutine()
        {
            Debug.Log("[TownSceneInitializer] Beginning scene initialization...");

            // Step 1: Find or setup environment
            yield return StartCoroutine(SetupEnvironment());

            // Step 2: Validate physics
            if (validateOnStart)
            {
                yield return StartCoroutine(ValidatePhysics());
            }

            // Step 3: Validate NavMesh
            if (validateOnStart)
            {
                yield return StartCoroutine(ValidateNavMesh());
            }

            // Step 4: Setup occlusion culling hints
            if (setupOcclusionCulling)
            {
                SetupOcclusionHints();
            }

            // Step 5: Spawn player
            if (spawnPlayerOnInit)
            {
                SpawnPlayer();
            }

            _isInitialized = true;

            if (showDebugInfo)
            {
                LogSceneInfo();
            }

            Debug.Log("[TownSceneInitializer] Scene initialization complete.");
            OnInitializationComplete?.Invoke();
        }

        private System.Collections.IEnumerator SetupEnvironment()
        {
            // Find environment loader if not assigned
            if (environmentLoader == null)
            {
                environmentLoader = FindObjectOfType<GLBEnvironmentLoader>();
            }

            // Find physics setup if not assigned
            if (physicsSetup == null)
            {
                physicsSetup = FindObjectOfType<EnvironmentPhysicsSetup>();
            }

            // Find NavMesh setup if not assigned
            if (navMeshSetup == null)
            {
                navMeshSetup = FindObjectOfType<NavMeshSetup>();
            }

            yield return null;
        }

        private System.Collections.IEnumerator ValidatePhysics()
        {
            Debug.Log("[TownSceneInitializer] Validating physics setup...");

            // Check for colliders
            Collider[] colliders = FindObjectsOfType<Collider>();
            int meshColliderCount = 0;

            foreach (Collider col in colliders)
            {
                if (col is MeshCollider)
                {
                    meshColliderCount++;
                }
            }

            Debug.Log($"[TownSceneInitializer] Found {colliders.Length} colliders ({meshColliderCount} mesh colliders).");

            if (meshColliderCount == 0)
            {
                Debug.LogWarning("[TownSceneInitializer] No mesh colliders found. Player may fall through geometry.");

                // Attempt to generate colliders if physics setup exists
                if (physicsSetup != null)
                {
                    Debug.Log("[TownSceneInitializer] Attempting to generate colliders...");
                    physicsSetup.SetupEnvironmentPhysics();

                    while (physicsSetup.IsGenerating)
                    {
                        yield return null;
                    }
                }
            }

            yield return null;
        }

        private System.Collections.IEnumerator ValidateNavMesh()
        {
            Debug.Log("[TownSceneInitializer] Validating NavMesh...");

            UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();

            if (triangulation.vertices.Length == 0)
            {
                Debug.LogWarning("[TownSceneInitializer] No NavMesh data found. AI navigation will not work.");

                // Attempt to build NavMesh if setup exists
                if (navMeshSetup != null)
                {
                    Debug.Log("[TownSceneInitializer] Attempting to build NavMesh...");

                    bool buildComplete = false;
                    navMeshSetup.OnNavMeshBuildComplete += () => buildComplete = true;
                    navMeshSetup.SetupAndBuildNavMesh();

                    float timeout = 30f;
                    float elapsed = 0f;

                    while (!buildComplete && elapsed < timeout)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (!buildComplete)
                    {
                        Debug.LogError("[TownSceneInitializer] NavMesh build timed out.");
                        OnInitializationError?.Invoke("NavMesh build timed out");
                    }
                }
            }
            else
            {
                Debug.Log($"[TownSceneInitializer] NavMesh valid. Vertices: {triangulation.vertices.Length}");
            }

            yield return null;
        }

        private void SetupOcclusionHints()
        {
            Debug.Log("[TownSceneInitializer] Setting up occlusion culling hints...");

            // For a monolithic asset, we rely on Unity's occlusion culling
            // Mark large objects as occluders

            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();

            foreach (MeshRenderer renderer in renderers)
            {
                // Large objects can be both occluders and occludees
                // This is set via static flags in the editor
                // At runtime, we can only set the isStatic flag
                if (renderer.bounds.size.magnitude > 10f)
                {
                    renderer.gameObject.isStatic = true;
                }
            }

            Debug.Log("[TownSceneInitializer] Occlusion hints configured.");
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[TownSceneInitializer] No player prefab assigned. Skipping player spawn.");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            if (playerSpawnPoint != null)
            {
                spawnPosition = playerSpawnPoint.position;
                spawnRotation = playerSpawnPoint.rotation;
            }
            else
            {
                // Find a valid spawn point on the NavMesh
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(Vector3.zero, out hit, 100f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    spawnPosition = hit.position + Vector3.up;
                }
            }

            _spawnedPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            _spawnedPlayer.name = "Player";

            Debug.Log($"[TownSceneInitializer] Player spawned at {spawnPosition}.");
        }

        private void LogSceneInfo()
        {
            Debug.Log("=== Town Scene Info ===");
            Debug.Log($"Total GameObjects: {FindObjectsOfType<GameObject>().Length}");
            Debug.Log($"Total Colliders: {FindObjectsOfType<Collider>().Length}");
            Debug.Log($"Total Mesh Renderers: {FindObjectsOfType<MeshRenderer>().Length}");

            UnityEngine.AI.NavMeshTriangulation nav = UnityEngine.AI.NavMesh.CalculateTriangulation();
            Debug.Log($"NavMesh Vertices: {nav.vertices.Length}");
            Debug.Log("=======================");
        }

        /// <summary>
        /// Gets whether the scene is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the spawned player instance.
        /// </summary>
        public GameObject SpawnedPlayer => _spawnedPlayer;

        /// <summary>
        /// Manually spawns the player (if not auto-spawned).
        /// </summary>
        public void ManuallySpawnPlayer()
        {
            if (_spawnedPlayer == null)
            {
                SpawnPlayer();
            }
        }
    }
}
