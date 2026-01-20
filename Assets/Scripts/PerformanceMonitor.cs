using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityShooter.Performance
{
    /// <summary>
    /// Performance monitoring component for tracking FPS and optimization metrics.
    /// Displays real-time performance data and can adjust quality settings dynamically.
    /// Target: 60+ FPS on standard hardware.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showFPSCounter = true;
        [SerializeField] private bool showDetailedStats = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("Performance Thresholds")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float criticalFPSThreshold = 30f;
        [SerializeField] private float warningFPSThreshold = 45f;

        [Header("Dynamic Quality")]
        [SerializeField] private bool enableDynamicQuality = false;
        [SerializeField] private float qualityAdjustInterval = 5f;

        // FPS calculation
        private float deltaTime = 0.0f;
        private float fps = 0.0f;
        private float avgFPS = 0.0f;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0.0f;
        private int frameCount = 0;
        private float fpsAccumulator = 0.0f;

        // Performance tracking
        private float lastQualityAdjustTime = 0.0f;
        private int currentQualityLevel;

        // Rendering stats
        private int drawCalls = 0;
        private int triangles = 0;
        private int vertices = 0;

        // GUI Style
        private GUIStyle guiStyle;
        private Rect fpsRect;

        private void Awake()
        {
            // Ensure target frame rate is set
            Application.targetFrameRate = Mathf.RoundToInt(targetFPS);
            QualitySettings.vSyncCount = 0; // Disable VSync for accurate FPS measurement

            currentQualityLevel = QualitySettings.GetQualityLevel();

            // Initialize GUI style
            guiStyle = new GUIStyle();
            guiStyle.fontSize = 16;
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.normal.textColor = Color.white;

            fpsRect = new Rect(10, 10, 250, 150);
        }

        private void Update()
        {
            // Calculate FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;

            // Accumulate for average
            frameCount++;
            fpsAccumulator += fps;
            avgFPS = fpsAccumulator / frameCount;

            // Track min/max
            if (fps < minFPS && frameCount > 10) minFPS = fps;
            if (fps > maxFPS) maxFPS = fps;

            // Toggle display
            if (Input.GetKeyDown(toggleKey))
            {
                showFPSCounter = !showFPSCounter;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                showDetailedStats = !showDetailedStats;
            }

            // Dynamic quality adjustment
            if (enableDynamicQuality && Time.time - lastQualityAdjustTime > qualityAdjustInterval)
            {
                AdjustQualityBasedOnPerformance();
                lastQualityAdjustTime = Time.time;
            }

            // Reset stats periodically
            if (frameCount > 1000)
            {
                ResetStats();
            }
        }

        private void OnGUI()
        {
            if (!showFPSCounter) return;

            // Determine FPS color based on performance
            Color fpsColor = GetFPSColor(fps);
            guiStyle.normal.textColor = fpsColor;

            // Background box for readability
            GUI.Box(fpsRect, GUIContent.none);

            GUILayout.BeginArea(fpsRect);
            GUILayout.BeginVertical();

            // Main FPS display
            GUILayout.Label($"FPS: {fps:F1}", guiStyle);

            if (showDetailedStats)
            {
                guiStyle.fontSize = 12;
                guiStyle.normal.textColor = Color.white;

                GUILayout.Label($"Avg FPS: {avgFPS:F1}", guiStyle);
                GUILayout.Label($"Min/Max: {minFPS:F1} / {maxFPS:F1}", guiStyle);
                GUILayout.Label($"Frame Time: {deltaTime * 1000.0f:F2} ms", guiStyle);
                GUILayout.Label($"Quality Level: {QualitySettings.names[currentQualityLevel]}", guiStyle);
                GUILayout.Label($"Target: {targetFPS} FPS", guiStyle);

                // Status indicator
                string status = fps >= targetFPS ? "OPTIMAL" : (fps >= warningFPSThreshold ? "WARNING" : "CRITICAL");
                Color statusColor = fps >= targetFPS ? Color.green : (fps >= warningFPSThreshold ? Color.yellow : Color.red);
                guiStyle.normal.textColor = statusColor;
                GUILayout.Label($"Status: {status}", guiStyle);

                guiStyle.fontSize = 16;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private Color GetFPSColor(float currentFPS)
        {
            if (currentFPS >= targetFPS)
                return Color.green;
            else if (currentFPS >= warningFPSThreshold)
                return Color.yellow;
            else
                return Color.red;
        }

        private void AdjustQualityBasedOnPerformance()
        {
            if (avgFPS < criticalFPSThreshold && currentQualityLevel > 0)
            {
                // Decrease quality
                currentQualityLevel--;
                QualitySettings.SetQualityLevel(currentQualityLevel, true);
                Debug.Log($"[PerformanceMonitor] Decreased quality to {QualitySettings.names[currentQualityLevel]} due to low FPS ({avgFPS:F1})");
            }
            else if (avgFPS > targetFPS * 1.2f && currentQualityLevel < QualitySettings.names.Length - 1)
            {
                // Increase quality if we have headroom
                currentQualityLevel++;
                QualitySettings.SetQualityLevel(currentQualityLevel, true);
                Debug.Log($"[PerformanceMonitor] Increased quality to {QualitySettings.names[currentQualityLevel]} due to high FPS ({avgFPS:F1})");
            }
        }

        private void ResetStats()
        {
            frameCount = 0;
            fpsAccumulator = 0.0f;
            minFPS = float.MaxValue;
            maxFPS = 0.0f;
        }

        /// <summary>
        /// Get the current FPS value for external systems.
        /// </summary>
        public float GetCurrentFPS() => fps;

        /// <summary>
        /// Get the average FPS since last reset.
        /// </summary>
        public float GetAverageFPS() => avgFPS;

        /// <summary>
        /// Check if performance is meeting target.
        /// </summary>
        public bool IsMeetingTarget() => avgFPS >= targetFPS;

        /// <summary>
        /// Force a quality level change.
        /// </summary>
        public void SetQualityLevel(int level)
        {
            if (level >= 0 && level < QualitySettings.names.Length)
            {
                currentQualityLevel = level;
                QualitySettings.SetQualityLevel(level, true);
            }
        }
    }
}
