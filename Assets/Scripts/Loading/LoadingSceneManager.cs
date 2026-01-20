using UnityEngine;
using CityShooter.Environment;
using CityShooter.Navigation;

namespace CityShooter.Loading
{
    /// <summary>
    /// Main manager for the Loading scene. Orchestrates the loading process
    /// including environment loading, physics setup, and NavMesh initialization.
    /// </summary>
    public class LoadingSceneManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private AsyncLevelLoader levelLoader;
        [SerializeField] private LoadingUIController uiController;

        [Header("Environment Loading")]
        [SerializeField] private bool useRuntimeGLBLoading = true;
        [SerializeField] private GLBEnvironmentLoader glbLoader;

        [Header("Target Scene")]
        [SerializeField] private string targetSceneName = "Town";

        [Header("Timing")]
        [SerializeField] private float preLoadDelay = 0.5f;
        [SerializeField] private float postLoadDelay = 0.5f;

        private LoadingPhase _currentPhase = LoadingPhase.NotStarted;

        public enum LoadingPhase
        {
            NotStarted,
            Initializing,
            LoadingEnvironment,
            SettingUpPhysics,
            BakingNavMesh,
            Finalizing,
            Complete
        }

        /// <summary>
        /// Gets the current loading phase.
        /// </summary>
        public LoadingPhase CurrentPhase => _currentPhase;

        private void Start()
        {
            InitializeComponents();
            StartLoadingProcess();
        }

        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (levelLoader == null)
            {
                levelLoader = GetComponent<AsyncLevelLoader>();
                if (levelLoader == null)
                {
                    levelLoader = gameObject.AddComponent<AsyncLevelLoader>();
                }
            }

            if (uiController == null)
            {
                uiController = FindObjectOfType<LoadingUIController>();
            }

            if (useRuntimeGLBLoading && glbLoader == null)
            {
                glbLoader = FindObjectOfType<GLBEnvironmentLoader>();
            }
        }

        private void StartLoadingProcess()
        {
            StartCoroutine(LoadingSequence());
        }

        private System.Collections.IEnumerator LoadingSequence()
        {
            _currentPhase = LoadingPhase.Initializing;
            Debug.Log("[LoadingSceneManager] Starting loading sequence...");

            // Pre-load delay for smooth transition
            yield return new WaitForSeconds(preLoadDelay);

            if (useRuntimeGLBLoading && glbLoader != null)
            {
                // Runtime GLB loading path
                yield return StartCoroutine(RuntimeLoadingSequence());
            }
            else
            {
                // Standard scene loading path
                yield return StartCoroutine(StandardLoadingSequence());
            }

            _currentPhase = LoadingPhase.Complete;
            Debug.Log("[LoadingSceneManager] Loading sequence complete.");
        }

        private System.Collections.IEnumerator RuntimeLoadingSequence()
        {
            // Phase 1: Load Environment
            _currentPhase = LoadingPhase.LoadingEnvironment;
            Debug.Log("[LoadingSceneManager] Phase 1: Loading environment...");

            bool environmentLoaded = false;
            bool environmentError = false;

            glbLoader.OnLoadComplete += (go) => environmentLoaded = true;
            glbLoader.OnLoadError += (err) => environmentError = true;
            glbLoader.OnProgressUpdated += (progress) =>
            {
                // Scale environment loading to 0-60% of total progress
                float scaledProgress = progress * 0.6f;
                UpdateOverallProgress(scaledProgress);
            };

            glbLoader.LoadEnvironment();

            // Wait for environment to load
            while (!environmentLoaded && !environmentError)
            {
                yield return null;
            }

            if (environmentError)
            {
                Debug.LogError("[LoadingSceneManager] Environment loading failed!");
                yield break;
            }

            // Phase 2: Setup Physics (if not already done by GLBLoader)
            _currentPhase = LoadingPhase.SettingUpPhysics;
            Debug.Log("[LoadingSceneManager] Phase 2: Setting up physics...");
            UpdateOverallProgress(0.7f);
            yield return new WaitForSeconds(0.1f);

            // Phase 3: NavMesh Setup
            _currentPhase = LoadingPhase.BakingNavMesh;
            Debug.Log("[LoadingSceneManager] Phase 3: Setting up NavMesh...");
            UpdateOverallProgress(0.85f);

            var navMeshSetup = glbLoader.LoadedEnvironment?.GetComponentInChildren<NavMeshSetup>();
            if (navMeshSetup != null)
            {
                bool navMeshComplete = false;
                navMeshSetup.OnNavMeshBuildComplete += () => navMeshComplete = true;
                navMeshSetup.SetupAndBuildNavMesh();

                float timeout = 10f;
                float elapsed = 0f;
                while (!navMeshComplete && elapsed < timeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // Phase 4: Finalize
            _currentPhase = LoadingPhase.Finalizing;
            Debug.Log("[LoadingSceneManager] Phase 4: Finalizing...");
            UpdateOverallProgress(0.95f);

            yield return new WaitForSeconds(postLoadDelay);
            UpdateOverallProgress(1f);
        }

        private System.Collections.IEnumerator StandardLoadingSequence()
        {
            _currentPhase = LoadingPhase.LoadingEnvironment;
            Debug.Log("[LoadingSceneManager] Standard loading: Loading target scene...");

            // Use the AsyncLevelLoader to load the target scene
            if (levelLoader != null)
            {
                levelLoader.StartLoading(targetSceneName);

                // Wait for loading to complete
                while (levelLoader.IsLoading)
                {
                    yield return null;
                }
            }

            _currentPhase = LoadingPhase.Finalizing;
            yield return new WaitForSeconds(postLoadDelay);
        }

        private void UpdateOverallProgress(float progress)
        {
            // This would update the UI through an event or direct reference
            Debug.Log($"[LoadingSceneManager] Overall progress: {progress:P0}");
        }

        /// <summary>
        /// Restarts the loading process.
        /// </summary>
        public void RestartLoading()
        {
            StopAllCoroutines();
            _currentPhase = LoadingPhase.NotStarted;
            StartLoadingProcess();
        }

        /// <summary>
        /// Skips to the target scene (for debugging).
        /// </summary>
        public void SkipToScene()
        {
            if (levelLoader != null)
            {
                levelLoader.ActivateScene();
            }
        }
    }
}
