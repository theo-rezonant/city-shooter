using UnityEngine;
using System.Collections;

namespace CityShooter.Camera
{
    /// <summary>
    /// Camera shake effect for weapon firing, damage, and high-intensity actions.
    /// Uses Perlin noise for natural-feeling shake with configurable intensity and duration.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Default Shake Settings")]
        [SerializeField] private float defaultDuration = 0.15f;
        [SerializeField] private float defaultMagnitude = 0.1f;
        [SerializeField] private float defaultFrequency = 25f;

        [Header("Shake Presets")]
        [SerializeField] private ShakePreset fireShake = new ShakePreset(0.05f, 0.02f, 30f);
        [SerializeField] private ShakePreset damageShake = new ShakePreset(0.3f, 0.15f, 20f);
        [SerializeField] private ShakePreset explosionShake = new ShakePreset(0.5f, 0.3f, 15f);

        [Header("Recovery Settings")]
        [SerializeField] private float recoverySpeed = 5f;
        [SerializeField] private bool smoothRecovery = true;

        // State
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Coroutine currentShakeCoroutine;
        private float currentShakeTime;
        private float currentMagnitude;
        private float currentFrequency;
        private float noiseSeedX;
        private float noiseSeedY;
        private float noiseSeedZ;

        // Events
        public event System.Action OnShakeStarted;
        public event System.Action OnShakeEnded;

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            RandomizeNoiseSeeds();
        }

        private void RandomizeNoiseSeeds()
        {
            noiseSeedX = Random.Range(0f, 100f);
            noiseSeedY = Random.Range(0f, 100f);
            noiseSeedZ = Random.Range(0f, 100f);
        }

        /// <summary>
        /// Play a shake effect with default parameters.
        /// </summary>
        public void PlayShake()
        {
            PlayShake(defaultDuration, defaultMagnitude, defaultFrequency);
        }

        /// <summary>
        /// Play a shake effect with custom parameters.
        /// </summary>
        /// <param name="duration">How long the shake lasts in seconds</param>
        /// <param name="magnitude">How intense the shake is (offset magnitude)</param>
        /// <param name="frequency">How fast the shake oscillates</param>
        public void PlayShake(float duration, float magnitude, float frequency)
        {
            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
            }

            currentShakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude, frequency));
        }

        /// <summary>
        /// Play the fire/recoil shake preset.
        /// </summary>
        public void PlayFireShake()
        {
            PlayShake(fireShake.Duration, fireShake.Magnitude, fireShake.Frequency);
        }

        /// <summary>
        /// Play the damage/hit shake preset.
        /// </summary>
        public void PlayDamageShake()
        {
            PlayShake(damageShake.Duration, damageShake.Magnitude, damageShake.Frequency);
        }

        /// <summary>
        /// Play the explosion shake preset.
        /// </summary>
        public void PlayExplosionShake()
        {
            PlayShake(explosionShake.Duration, explosionShake.Magnitude, explosionShake.Frequency);
        }

        /// <summary>
        /// Immediately stop any active shake and reset camera position.
        /// </summary>
        public void StopShake()
        {
            if (currentShakeCoroutine != null)
            {
                StopCoroutine(currentShakeCoroutine);
                currentShakeCoroutine = null;
            }

            if (smoothRecovery)
            {
                StartCoroutine(SmoothResetCoroutine());
            }
            else
            {
                ResetCameraTransform();
            }
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude, float frequency)
        {
            currentShakeTime = 0f;
            currentMagnitude = magnitude;
            currentFrequency = frequency;

            OnShakeStarted?.Invoke();
            RandomizeNoiseSeeds();

            while (currentShakeTime < duration)
            {
                currentShakeTime += Time.deltaTime;
                float progress = currentShakeTime / duration;

                // Ease out the shake over time
                float currentIntensity = currentMagnitude * (1f - EaseOutQuad(progress));

                // Generate shake offset using Perlin noise
                float time = currentShakeTime * currentFrequency;
                Vector3 shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(noiseSeedX + time, 0f) - 0.5f) * 2f * currentIntensity,
                    (Mathf.PerlinNoise(noiseSeedY + time, 1f) - 0.5f) * 2f * currentIntensity,
                    (Mathf.PerlinNoise(noiseSeedZ + time, 2f) - 0.5f) * 2f * currentIntensity * 0.5f
                );

                // Apply shake offset
                transform.localPosition = originalLocalPosition + shakeOffset;

                yield return null;
            }

            // Recovery
            if (smoothRecovery)
            {
                yield return SmoothResetCoroutine();
            }
            else
            {
                ResetCameraTransform();
            }

            currentShakeCoroutine = null;
            OnShakeEnded?.Invoke();
        }

        private IEnumerator SmoothResetCoroutine()
        {
            Vector3 startPosition = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < 1f / recoverySpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * recoverySpeed;
                transform.localPosition = Vector3.Lerp(startPosition, originalLocalPosition, EaseOutQuad(t));
                yield return null;
            }

            ResetCameraTransform();
        }

        private void ResetCameraTransform()
        {
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// Set the original position that the camera resets to after shaking.
        /// Useful if the camera moves to a new position that should be the new "rest" position.
        /// </summary>
        public void SetOriginalPosition(Vector3 position)
        {
            originalLocalPosition = position;
        }

        /// <summary>
        /// Shake preset data structure for organizing different shake intensities.
        /// </summary>
        [System.Serializable]
        public struct ShakePreset
        {
            public float Duration;
            public float Magnitude;
            public float Frequency;

            public ShakePreset(float duration, float magnitude, float frequency)
            {
                Duration = duration;
                Magnitude = magnitude;
                Frequency = frequency;
            }
        }
    }
}
