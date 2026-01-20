using UnityEngine;
using UnityEngine.UI;

namespace CityShooter.UI
{
    /// <summary>
    /// Hit marker that appears on the crosshair when a raycast successfully hits an enemy.
    /// Features sci-fi styled X-pattern with animated feedback.
    /// </summary>
    public class HitMarker : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform hitMarkerContainer;
        [SerializeField] private Image topLeftLine;
        [SerializeField] private Image topRightLine;
        [SerializeField] private Image bottomLeftLine;
        [SerializeField] private Image bottomRightLine;
        [SerializeField] private CanvasGroup hitMarkerCanvasGroup;

        [Header("Colors")]
        [SerializeField] private Color normalHitColor = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color criticalHitColor = new Color(1f, 0.3f, 0.3f, 1f); // Red
        [SerializeField] private Color headShotColor = new Color(1f, 1f, 0f, 1f); // Yellow
        [SerializeField] private Color killColor = new Color(1f, 0f, 0.5f, 1f); // Magenta

        [Header("Size Settings")]
        [SerializeField] private float lineLength = 10f;
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private float initialOffset = 8f;
        [SerializeField] private float expandedOffset = 15f;
        [SerializeField] private float rotation = 45f;

        [Header("Animation Settings")]
        [SerializeField] private float displayDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private float expansionSpeed = 20f;
        [SerializeField] private float pulseScale = 1.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip criticalSound;
        [SerializeField] private AudioClip killSound;

        // State
        private bool isShowing;
        private float showTimer;
        private float currentOffset;
        private Color currentColor;
        private Coroutine activeCoroutine;

        private void Start()
        {
            InitializeHitMarker();
            Hide();
        }

        private void Update()
        {
            if (isShowing)
            {
                UpdateAnimation();
            }
        }

        private void InitializeHitMarker()
        {
            // Setup line rotations for X pattern
            SetupLine(topLeftLine, -rotation);
            SetupLine(topRightLine, rotation);
            SetupLine(bottomLeftLine, rotation);
            SetupLine(bottomRightLine, -rotation);

            if (hitMarkerCanvasGroup == null)
            {
                hitMarkerCanvasGroup = GetComponent<CanvasGroup>();
                if (hitMarkerCanvasGroup == null)
                {
                    hitMarkerCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void SetupLine(Image line, float rotationAngle)
        {
            if (line == null) return;

            line.rectTransform.sizeDelta = new Vector2(lineThickness, lineLength);
            line.rectTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            line.color = normalHitColor;
        }

        private void UpdateAnimation()
        {
            // Contract from expanded to initial position
            currentOffset = Mathf.Lerp(currentOffset, initialOffset, expansionSpeed * Time.deltaTime);
            UpdateLinePositions(currentOffset);
        }

        private void UpdateLinePositions(float offset)
        {
            float diagonalOffset = offset * 0.707f; // cos(45) for diagonal positioning

            if (topLeftLine != null)
                topLeftLine.rectTransform.anchoredPosition = new Vector2(-diagonalOffset, diagonalOffset);
            if (topRightLine != null)
                topRightLine.rectTransform.anchoredPosition = new Vector2(diagonalOffset, diagonalOffset);
            if (bottomLeftLine != null)
                bottomLeftLine.rectTransform.anchoredPosition = new Vector2(-diagonalOffset, -diagonalOffset);
            if (bottomRightLine != null)
                bottomRightLine.rectTransform.anchoredPosition = new Vector2(diagonalOffset, -diagonalOffset);
        }

        private void SetAllLinesColor(Color color)
        {
            if (topLeftLine != null) topLeftLine.color = color;
            if (topRightLine != null) topRightLine.color = color;
            if (bottomLeftLine != null) bottomLeftLine.color = color;
            if (bottomRightLine != null) bottomRightLine.color = color;
        }

        // ==================== PUBLIC METHODS ====================

        /// <summary>
        /// Show the hit marker for a normal hit.
        /// </summary>
        public void ShowHitMarker()
        {
            ShowHitMarker(HitType.Normal);
        }

        /// <summary>
        /// Show the hit marker with a specific hit type.
        /// </summary>
        public void ShowHitMarker(HitType hitType)
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            currentColor = GetColorForHitType(hitType);
            SetAllLinesColor(currentColor);
            PlayHitSound(hitType);

            activeCoroutine = StartCoroutine(ShowHitMarkerCoroutine(hitType));
        }

        private Color GetColorForHitType(HitType hitType)
        {
            switch (hitType)
            {
                case HitType.Critical:
                    return criticalHitColor;
                case HitType.HeadShot:
                    return headShotColor;
                case HitType.Kill:
                    return killColor;
                default:
                    return normalHitColor;
            }
        }

        private void PlayHitSound(HitType hitType)
        {
            if (audioSource == null) return;

            AudioClip clip = hitType switch
            {
                HitType.Kill => killSound,
                HitType.Critical or HitType.HeadShot => criticalSound,
                _ => hitSound
            };

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private System.Collections.IEnumerator ShowHitMarkerCoroutine(HitType hitType)
        {
            isShowing = true;

            // Show with initial expansion
            if (hitMarkerContainer != null)
            {
                hitMarkerContainer.gameObject.SetActive(true);
            }

            if (hitMarkerCanvasGroup != null)
            {
                hitMarkerCanvasGroup.alpha = 1f;
            }

            // Start expanded then contract
            currentOffset = expandedOffset;
            UpdateLinePositions(currentOffset);

            // Apply pulse scale for more impactful hits
            float targetScale = hitType == HitType.Kill || hitType == HitType.HeadShot ? pulseScale : 1f;
            if (hitMarkerContainer != null)
            {
                hitMarkerContainer.localScale = Vector3.one * targetScale;
            }

            // Display phase
            float displayTimer = 0f;
            while (displayTimer < displayDuration)
            {
                displayTimer += Time.deltaTime;

                // Scale back to normal during display
                if (hitMarkerContainer != null && targetScale > 1f)
                {
                    float t = displayTimer / displayDuration;
                    hitMarkerContainer.localScale = Vector3.Lerp(Vector3.one * targetScale, Vector3.one, t);
                }

                yield return null;
            }

            // Fade out phase
            float fadeTimer = 0f;
            while (fadeTimer < fadeOutDuration)
            {
                fadeTimer += Time.deltaTime;
                float alpha = 1f - (fadeTimer / fadeOutDuration);

                if (hitMarkerCanvasGroup != null)
                {
                    hitMarkerCanvasGroup.alpha = alpha;
                }

                yield return null;
            }

            Hide();
            isShowing = false;
            activeCoroutine = null;
        }

        /// <summary>
        /// Hide the hit marker immediately.
        /// </summary>
        public void Hide()
        {
            if (hitMarkerContainer != null)
            {
                hitMarkerContainer.gameObject.SetActive(false);
            }

            if (hitMarkerCanvasGroup != null)
            {
                hitMarkerCanvasGroup.alpha = 0f;
            }

            isShowing = false;
        }

        /// <summary>
        /// Check if hit marker is currently visible.
        /// </summary>
        public bool IsShowing()
        {
            return isShowing;
        }
    }

    /// <summary>
    /// Types of hits for different visual feedback.
    /// </summary>
    public enum HitType
    {
        Normal,
        Critical,
        HeadShot,
        Kill
    }
}
