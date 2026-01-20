using System.Collections;
using UnityEngine;

namespace CityShooter.Weapons
{
    /// <summary>
    /// Creates and manages laser bolt visual effects using Unity's LineRenderer.
    /// Supports both immediate and travel-style laser beams with customizable appearance.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBoltVFX : MonoBehaviour
    {
        [Header("Laser Appearance")]
        [SerializeField] private Color laserColorStart = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color laserColorEnd = new Color(1f, 0f, 0f, 1f); // Red
        [SerializeField] private bool useCyanColor = true;
        [SerializeField] private float laserWidth = 0.05f;
        [SerializeField] private float laserWidthEnd = 0.02f;

        [Header("Timing")]
        [SerializeField] private float laserDuration = 0.1f;
        [SerializeField] private float fadeOutDuration = 0.05f;

        [Header("Material Settings")]
        [SerializeField] private Material laserMaterial;
        [SerializeField] private string emissionColorProperty = "_EmissionColor";
        [SerializeField] private float emissionIntensity = 5f;

        [Header("Visual Enhancements")]
        [SerializeField] private bool useGlow = true;
        [SerializeField] private int cornerVertices = 5;
        [SerializeField] private int endCapVertices = 5;
        [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

        [Header("Core Effect (Inner Bright Line)")]
        [SerializeField] private bool useCoreEffect = true;
        [SerializeField] private float coreWidthMultiplier = 0.3f;
        [SerializeField] private float coreEmissionBoost = 2f;

        private LineRenderer _lineRenderer;
        private LineRenderer _coreLineRenderer;
        private Coroutine _currentLaserCoroutine;
        private Color _activeColor;
        private Material _instancedMaterial;
        private Material _coreInstancedMaterial;

        private void Awake()
        {
            InitializeLineRenderer();
        }

        private void OnDestroy()
        {
            // Clean up instanced materials
            if (_instancedMaterial != null)
            {
                Destroy(_instancedMaterial);
            }
            if (_coreInstancedMaterial != null)
            {
                Destroy(_coreInstancedMaterial);
            }
        }

        /// <summary>
        /// Initializes the LineRenderer with proper settings for laser effect.
        /// </summary>
        private void InitializeLineRenderer()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            // Setup main laser
            SetupLineRenderer(_lineRenderer, false);

            // Setup core effect (brighter inner line)
            if (useCoreEffect)
            {
                SetupCoreLineRenderer();
            }
        }

        private void SetupLineRenderer(LineRenderer lr, bool isCore)
        {
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.allowOcclusionWhenDynamic = false;

            // Set width
            float startWidth = isCore ? laserWidth * coreWidthMultiplier : laserWidth;
            float endWidth = isCore ? laserWidthEnd * coreWidthMultiplier : laserWidthEnd;
            lr.startWidth = startWidth;
            lr.endWidth = endWidth;
            lr.widthCurve = widthCurve;

            // Set quality
            lr.numCornerVertices = cornerVertices;
            lr.numCapVertices = endCapVertices;

            // Setup material
            if (laserMaterial != null)
            {
                Material mat = new Material(laserMaterial);
                if (isCore)
                {
                    _coreInstancedMaterial = mat;
                }
                else
                {
                    _instancedMaterial = mat;
                }
                lr.material = mat;
            }
            else
            {
                // Create default unlit material for URP
                CreateDefaultLaserMaterial(lr, isCore);
            }

            // Set initial color
            _activeColor = useCyanColor ? laserColorStart : laserColorEnd;
            float intensity = isCore ? emissionIntensity * coreEmissionBoost : emissionIntensity;
            SetLineRendererColor(lr, _activeColor, intensity, isCore);

            // Initially disabled
            lr.enabled = false;
        }

        private void SetupCoreLineRenderer()
        {
            GameObject coreObj = new GameObject("LaserCore");
            coreObj.transform.SetParent(transform);
            coreObj.transform.localPosition = Vector3.zero;
            coreObj.transform.localRotation = Quaternion.identity;

            _coreLineRenderer = coreObj.AddComponent<LineRenderer>();
            SetupLineRenderer(_coreLineRenderer, true);
        }

        private void CreateDefaultLaserMaterial(LineRenderer lr, bool isCore)
        {
            // Create a basic URP unlit material
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha blend
            mat.EnableKeyword("_EMISSION");
            mat.renderQueue = 3000; // Transparent queue

            if (isCore)
            {
                _coreInstancedMaterial = mat;
            }
            else
            {
                _instancedMaterial = mat;
            }

            lr.material = mat;
        }

        private void SetLineRendererColor(LineRenderer lr, Color color, float intensity, bool isCore)
        {
            Color hdrColor = color * intensity;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(hdrColor, 0f),
                    new GradientColorKey(hdrColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 1f)
                }
            );

            lr.colorGradient = gradient;

            // Set material emission
            Material mat = isCore ? _coreInstancedMaterial : _instancedMaterial;
            if (mat != null && mat.HasProperty(emissionColorProperty))
            {
                mat.SetColor(emissionColorProperty, hdrColor);
            }
        }

        /// <summary>
        /// Fires a laser bolt from origin to target point.
        /// </summary>
        /// <param name="origin">Starting point of the laser (muzzle position).</param>
        /// <param name="target">End point of the laser (hit point or max range).</param>
        public void FireLaserBolt(Vector3 origin, Vector3 target)
        {
            // Stop any existing laser effect
            if (_currentLaserCoroutine != null)
            {
                StopCoroutine(_currentLaserCoroutine);
            }

            _currentLaserCoroutine = StartCoroutine(LaserBoltCoroutine(origin, target));
        }

        /// <summary>
        /// Fires a laser bolt using raycast hit info.
        /// </summary>
        /// <param name="origin">Starting point of the laser.</param>
        /// <param name="hitInfo">The laser hit information.</param>
        public void FireLaserBolt(Vector3 origin, LaserHitInfo hitInfo)
        {
            FireLaserBolt(origin, hitInfo.HitPoint);
        }

        private IEnumerator LaserBoltCoroutine(Vector3 origin, Vector3 target)
        {
            // Enable and set positions
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, target);

            if (_coreLineRenderer != null)
            {
                _coreLineRenderer.enabled = true;
                _coreLineRenderer.SetPosition(0, origin);
                _coreLineRenderer.SetPosition(1, target);
            }

            // Full intensity for duration
            float elapsed = 0f;
            while (elapsed < laserDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            float fadeElapsed = 0f;
            float initialWidth = laserWidth;
            float initialCoreWidth = laserWidth * coreWidthMultiplier;

            while (fadeElapsed < fadeOutDuration)
            {
                fadeElapsed += Time.deltaTime;
                float t = fadeElapsed / fadeOutDuration;
                float alpha = 1f - t;

                // Fade width
                _lineRenderer.startWidth = initialWidth * alpha;
                _lineRenderer.endWidth = laserWidthEnd * alpha;

                if (_coreLineRenderer != null)
                {
                    _coreLineRenderer.startWidth = initialCoreWidth * alpha;
                    _coreLineRenderer.endWidth = laserWidthEnd * coreWidthMultiplier * alpha;
                }

                // Fade color
                Color fadedColor = _activeColor;
                fadedColor.a = alpha;
                SetLineRendererColor(_lineRenderer, fadedColor, emissionIntensity * alpha, false);

                if (_coreLineRenderer != null)
                {
                    SetLineRendererColor(_coreLineRenderer, fadedColor, emissionIntensity * coreEmissionBoost * alpha, true);
                }

                yield return null;
            }

            // Disable
            _lineRenderer.enabled = false;
            if (_coreLineRenderer != null)
            {
                _coreLineRenderer.enabled = false;
            }

            // Reset widths
            _lineRenderer.startWidth = initialWidth;
            _lineRenderer.endWidth = laserWidthEnd;

            if (_coreLineRenderer != null)
            {
                _coreLineRenderer.startWidth = initialCoreWidth;
                _coreLineRenderer.endWidth = laserWidthEnd * coreWidthMultiplier;
            }

            _currentLaserCoroutine = null;
        }

        /// <summary>
        /// Sets the laser color. True for cyan, false for red.
        /// </summary>
        public void SetLaserColorMode(bool useCyan)
        {
            useCyanColor = useCyan;
            _activeColor = useCyan ? laserColorStart : laserColorEnd;
            SetLineRendererColor(_lineRenderer, _activeColor, emissionIntensity, false);

            if (_coreLineRenderer != null)
            {
                SetLineRendererColor(_coreLineRenderer, _activeColor, emissionIntensity * coreEmissionBoost, true);
            }
        }

        /// <summary>
        /// Sets a custom laser color.
        /// </summary>
        public void SetLaserColor(Color color)
        {
            _activeColor = color;
            SetLineRendererColor(_lineRenderer, _activeColor, emissionIntensity, false);

            if (_coreLineRenderer != null)
            {
                SetLineRendererColor(_coreLineRenderer, _activeColor, emissionIntensity * coreEmissionBoost, true);
            }
        }

        /// <summary>
        /// Gets whether the laser is currently active.
        /// </summary>
        public bool IsActive => _lineRenderer != null && _lineRenderer.enabled;

        /// <summary>
        /// Gets or sets the emission intensity.
        /// </summary>
        public float EmissionIntensity
        {
            get => emissionIntensity;
            set
            {
                emissionIntensity = Mathf.Max(0f, value);
                SetLineRendererColor(_lineRenderer, _activeColor, emissionIntensity, false);
                if (_coreLineRenderer != null)
                {
                    SetLineRendererColor(_coreLineRenderer, _activeColor, emissionIntensity * coreEmissionBoost, true);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw a sample laser line in editor
            Gizmos.color = useCyanColor ? laserColorStart : laserColorEnd;
            Vector3 start = transform.position;
            Vector3 end = transform.position + transform.forward * 5f;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.1f);
        }
#endif
    }
}
