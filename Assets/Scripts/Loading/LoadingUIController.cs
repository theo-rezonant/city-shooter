using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace CityShooter.Loading
{
    /// <summary>
    /// Controls the loading screen UI elements including progress bar and status text.
    /// </summary>
    public class LoadingUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual Settings")]
        [SerializeField] private float smoothSpeed = 3f;
        [SerializeField] private Color loadingColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color completeColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f);

        [Header("Status Messages")]
        [SerializeField] private string[] loadingMessages = new string[]
        {
            "Loading town environment...",
            "Processing textures...",
            "Building collision mesh...",
            "Setting up navigation...",
            "Preparing environment...",
            "Almost ready..."
        };

        [Header("Auto Start")]
        [SerializeField] private bool autoStartLoading = true;
        [SerializeField] private AsyncLevelLoader levelLoader;

        private float _targetProgress;
        private float _displayedProgress;
        private int _currentMessageIndex;
        private Coroutine _messageRotationCoroutine;

        private void Start()
        {
            InitializeUI();

            if (levelLoader == null)
            {
                levelLoader = GetComponent<AsyncLevelLoader>();
            }

            if (levelLoader != null)
            {
                // Subscribe to events
                levelLoader.OnProgressUpdated += HandleProgressUpdated;
                levelLoader.OnLoadingStarted += HandleLoadingStarted;
                levelLoader.OnLoadingComplete += HandleLoadingComplete;
                levelLoader.OnLoadingError += HandleLoadingError;

                if (autoStartLoading)
                {
                    levelLoader.StartLoading();
                }
            }
            else
            {
                Debug.LogWarning("[LoadingUIController] No AsyncLevelLoader found. Please assign one.");
            }
        }

        private void InitializeUI()
        {
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }

            if (progressFill != null)
            {
                progressFill.color = loadingColor;
            }

            if (progressText != null)
            {
                progressText.text = "0%";
            }

            if (statusText != null)
            {
                statusText.text = "Initializing...";
            }

            _targetProgress = 0f;
            _displayedProgress = 0f;
        }

        private void Update()
        {
            // Smoothly interpolate the displayed progress
            if (Mathf.Abs(_displayedProgress - _targetProgress) > 0.001f)
            {
                _displayedProgress = Mathf.Lerp(_displayedProgress, _targetProgress, Time.deltaTime * smoothSpeed);
                UpdateProgressDisplay(_displayedProgress);
            }
        }

        private void HandleProgressUpdated(float progress)
        {
            _targetProgress = progress;

            // Update message based on progress thresholds
            int newMessageIndex = Mathf.FloorToInt(progress * (loadingMessages.Length - 1));
            newMessageIndex = Mathf.Clamp(newMessageIndex, 0, loadingMessages.Length - 1);

            if (newMessageIndex != _currentMessageIndex && statusText != null)
            {
                _currentMessageIndex = newMessageIndex;
                statusText.text = loadingMessages[_currentMessageIndex];
            }
        }

        private void HandleLoadingStarted()
        {
            Debug.Log("[LoadingUIController] Loading started");

            if (statusText != null)
            {
                statusText.text = loadingMessages[0];
            }

            _messageRotationCoroutine = StartCoroutine(RotateLoadingDots());
        }

        private void HandleLoadingComplete()
        {
            Debug.Log("[LoadingUIController] Loading complete");

            if (_messageRotationCoroutine != null)
            {
                StopCoroutine(_messageRotationCoroutine);
            }

            // Set to full progress
            _targetProgress = 1f;
            _displayedProgress = 1f;
            UpdateProgressDisplay(1f);

            if (progressFill != null)
            {
                progressFill.color = completeColor;
            }

            if (statusText != null)
            {
                statusText.text = "Loading complete!";
            }

            // Optionally fade out the loading screen
            StartCoroutine(FadeOutLoadingScreen());
        }

        private void HandleLoadingError(string error)
        {
            Debug.LogError($"[LoadingUIController] Loading error: {error}");

            if (_messageRotationCoroutine != null)
            {
                StopCoroutine(_messageRotationCoroutine);
            }

            if (progressFill != null)
            {
                progressFill.color = errorColor;
            }

            if (statusText != null)
            {
                statusText.text = $"Error: {error}";
            }
        }

        private void UpdateProgressDisplay(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                progressText.text = $"{percentage}%";
            }
        }

        private IEnumerator RotateLoadingDots()
        {
            int dotCount = 0;
            while (true)
            {
                dotCount = (dotCount + 1) % 4;
                // This adds animated dots to give feedback that the system is working
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator FadeOutLoadingScreen()
        {
            yield return new WaitForSeconds(0.5f);

            if (canvasGroup != null)
            {
                float fadeTime = 0.5f;
                float elapsed = 0f;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1f - (elapsed / fadeTime);
                    yield return null;
                }

                canvasGroup.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            if (levelLoader != null)
            {
                levelLoader.OnProgressUpdated -= HandleProgressUpdated;
                levelLoader.OnLoadingStarted -= HandleLoadingStarted;
                levelLoader.OnLoadingComplete -= HandleLoadingComplete;
                levelLoader.OnLoadingError -= HandleLoadingError;
            }
        }

        /// <summary>
        /// Manually trigger loading start (for button-based loading).
        /// </summary>
        public void TriggerLoading()
        {
            if (levelLoader != null && !levelLoader.IsLoading)
            {
                levelLoader.StartLoading();
            }
        }

        /// <summary>
        /// Set the target scene name programmatically.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        public void SetTargetScene(string sceneName)
        {
            if (levelLoader != null)
            {
                // This would require exposing a setter in AsyncLevelLoader
                // For now, we load directly
                levelLoader.StartLoading(sceneName);
            }
        }
    }
}
