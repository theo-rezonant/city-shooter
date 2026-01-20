using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CityShooter.Loading
{
    /// <summary>
    /// Handles asynchronous scene loading with progress tracking.
    /// Designed to load large assets like the 50MB town4new.glb without freezing the application.
    /// </summary>
    public class AsyncLevelLoader : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string targetSceneName = "Town";
        [SerializeField] private bool activateSceneOnLoad = true;
        [SerializeField] private float minimumLoadTime = 1.5f;

        [Header("Progress Events")]
        public event Action<float> OnProgressUpdated;
        public event Action OnLoadingStarted;
        public event Action OnLoadingComplete;
        public event Action<string> OnLoadingError;

        private AsyncOperation _asyncOperation;
        private bool _isLoading;
        private float _currentProgress;

        /// <summary>
        /// Gets the current loading progress (0-1).
        /// </summary>
        public float Progress => _currentProgress;

        /// <summary>
        /// Gets whether a load operation is currently in progress.
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Starts the asynchronous loading of the target scene.
        /// </summary>
        public void StartLoading()
        {
            if (_isLoading)
            {
                Debug.LogWarning("[AsyncLevelLoader] Loading already in progress.");
                return;
            }

            StartCoroutine(LoadSceneCoroutine(targetSceneName));
        }

        /// <summary>
        /// Starts the asynchronous loading of a specified scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        public void StartLoading(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[AsyncLevelLoader] Loading already in progress.");
                return;
            }

            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            _isLoading = true;
            _currentProgress = 0f;
            float startTime = Time.time;

            OnLoadingStarted?.Invoke();
            Debug.Log($"[AsyncLevelLoader] Starting async load of scene: {sceneName}");

            // Start the async load operation
            _asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (_asyncOperation == null)
            {
                string errorMsg = $"Failed to start loading scene: {sceneName}. Ensure the scene is added to Build Settings.";
                Debug.LogError($"[AsyncLevelLoader] {errorMsg}");
                OnLoadingError?.Invoke(errorMsg);
                _isLoading = false;
                yield break;
            }

            // Prevent scene activation until we're ready
            _asyncOperation.allowSceneActivation = false;

            // Track progress
            // Note: AsyncOperation.progress goes from 0 to 0.9, then jumps to 1.0 when activated
            while (!_asyncOperation.isDone)
            {
                // Calculate normalized progress (0.9 = 100% loaded, waiting for activation)
                float loadProgress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);

                // Apply minimum load time to show progress smoothly
                float elapsedTime = Time.time - startTime;
                float timeProgress = minimumLoadTime > 0 ? Mathf.Clamp01(elapsedTime / minimumLoadTime) : 1f;

                // Use the minimum of load progress and time progress for smooth display
                _currentProgress = Mathf.Min(loadProgress, timeProgress);

                // If both conditions are met (loaded and min time elapsed), use the load progress
                if (elapsedTime >= minimumLoadTime)
                {
                    _currentProgress = loadProgress;
                }

                OnProgressUpdated?.Invoke(_currentProgress);

                // Check if loading is complete (progress reaches 0.9)
                if (_asyncOperation.progress >= 0.9f && elapsedTime >= minimumLoadTime)
                {
                    _currentProgress = 1f;
                    OnProgressUpdated?.Invoke(_currentProgress);

                    if (activateSceneOnLoad)
                    {
                        // Allow scene activation
                        _asyncOperation.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            _isLoading = false;
            Debug.Log($"[AsyncLevelLoader] Scene {sceneName} loaded successfully.");
            OnLoadingComplete?.Invoke();
        }

        /// <summary>
        /// Manually activates the loaded scene (if activateSceneOnLoad is false).
        /// </summary>
        public void ActivateScene()
        {
            if (_asyncOperation != null && !_asyncOperation.allowSceneActivation)
            {
                _asyncOperation.allowSceneActivation = true;
            }
        }

        /// <summary>
        /// Cancels the current loading operation if possible.
        /// </summary>
        public void CancelLoading()
        {
            if (_isLoading)
            {
                StopAllCoroutines();
                _isLoading = false;
                _currentProgress = 0f;
                Debug.Log("[AsyncLevelLoader] Loading cancelled.");
            }
        }

        private void OnDestroy()
        {
            CancelLoading();
        }
    }
}
