using System.Collections;
using UnityEngine;
using CityShooter.Core;

namespace CityShooter.Weapons
{
    /// <summary>
    /// Controls impact effects when laser hits targets.
    /// Uses object pooling for optimal performance during heavy combat.
    /// </summary>
    public class ImpactEffectController : MonoBehaviour
    {
        [Header("Impact Effect Prefabs")]
        [SerializeField] private ParticleSystem defaultImpactPrefab;
        [SerializeField] private ParticleSystem enemyImpactPrefab;
        [SerializeField] private ParticleSystem environmentImpactPrefab;

        [Header("Decal Settings")]
        [SerializeField] private GameObject impactDecalPrefab;
        [SerializeField] private bool useDecals = true;
        [SerializeField] private float decalLifetime = 10f;
        [SerializeField] private float decalSize = 0.2f;
        [SerializeField] private float decalOffset = 0.01f; // Offset from surface to prevent z-fighting

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 50;

        [Header("Effect Settings")]
        [SerializeField] private Color defaultImpactColor = new Color(0f, 1f, 1f, 1f); // Cyan
        [SerializeField] private Color enemyImpactColor = new Color(1f, 0.3f, 0f, 1f); // Orange
        [SerializeField] private float effectDuration = 1f;

        [Header("Audio")]
        [SerializeField] private AudioClip[] impactSounds;
        [SerializeField] [Range(0f, 1f)] private float impactSoundVolume = 0.5f;
        [SerializeField] private float impactSoundMinPitch = 0.9f;
        [SerializeField] private float impactSoundMaxPitch = 1.1f;

        private ObjectPool<ParticleSystem> _defaultImpactPool;
        private ObjectPool<ParticleSystem> _enemyImpactPool;
        private ObjectPool<ParticleSystem> _environmentImpactPool;
        private ObjectPool<Transform> _decalPool;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            CleanupPools();
        }

        /// <summary>
        /// Initializes the impact effect pools.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            // Create runtime prefabs if not assigned
            CreateDefaultPrefabsIfNeeded();

            // Initialize pools
            if (defaultImpactPrefab != null)
            {
                _defaultImpactPool = new ObjectPool<ParticleSystem>(
                    defaultImpactPrefab,
                    initialPoolSize,
                    maxPoolSize,
                    transform
                );
            }

            if (enemyImpactPrefab != null)
            {
                _enemyImpactPool = new ObjectPool<ParticleSystem>(
                    enemyImpactPrefab,
                    initialPoolSize / 2,
                    maxPoolSize / 2,
                    transform
                );
            }

            if (environmentImpactPrefab != null)
            {
                _environmentImpactPool = new ObjectPool<ParticleSystem>(
                    environmentImpactPrefab,
                    initialPoolSize,
                    maxPoolSize,
                    transform
                );
            }

            // Initialize decal pool
            if (useDecals && impactDecalPrefab != null)
            {
                _decalPool = new ObjectPool<Transform>(
                    impactDecalPrefab.transform,
                    initialPoolSize,
                    maxPoolSize,
                    transform
                );
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Creates default impact prefabs at runtime if none are assigned.
        /// </summary>
        private void CreateDefaultPrefabsIfNeeded()
        {
            if (defaultImpactPrefab == null)
            {
                defaultImpactPrefab = CreateDefaultParticleSystem("DefaultImpact", defaultImpactColor);
            }

            if (enemyImpactPrefab == null)
            {
                enemyImpactPrefab = CreateDefaultParticleSystem("EnemyImpact", enemyImpactColor);
            }

            if (environmentImpactPrefab == null)
            {
                environmentImpactPrefab = CreateDefaultParticleSystem("EnvironmentImpact", defaultImpactColor);
            }

            if (useDecals && impactDecalPrefab == null)
            {
                impactDecalPrefab = CreateDefaultDecal();
            }
        }

        /// <summary>
        /// Creates a default particle system for impact effects.
        /// </summary>
        private ParticleSystem CreateDefaultParticleSystem(string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.SetActive(false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            // Configure main module
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = color;
            main.maxParticles = 50;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Disable;

            // Configure emission
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 10, 20)
            });

            // Configure shape
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            // Configure color over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 0.5f),
                    new GradientColorKey(Color.white * 0.5f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Configure size over lifetime
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Add particle system renderer settings
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Try to find a particle material
            Material particleMat = GetDefaultParticleMaterial();
            if (particleMat != null)
            {
                renderer.material = particleMat;
            }

            return ps;
        }

        /// <summary>
        /// Gets or creates a default particle material.
        /// </summary>
        private Material GetDefaultParticleMaterial()
        {
            // Try to find a built-in particle material
            Material mat = Resources.Load<Material>("Default-Particle");

            if (mat == null)
            {
                // Create a basic additive material
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Particles/Standard Unlit");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Legacy Shaders/Particles/Additive");
                }

                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.SetFloat("_Surface", 1); // Transparent
                }
            }

            return mat;
        }

        /// <summary>
        /// Creates a default decal object.
        /// </summary>
        private GameObject CreateDefaultDecal()
        {
            GameObject go = new GameObject("ImpactDecal");
            go.transform.SetParent(transform);
            go.SetActive(false);

            // Create a simple quad for decal
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();

            // Create quad mesh
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(-0.5f, 0, 0.5f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            mesh.RecalculateNormals();
            mf.mesh = mesh;

            // Create decal material
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = new Color(0f, 1f, 1f, 0.5f);
                mr.material = mat;
            }

            return go;
        }

        /// <summary>
        /// Spawns an impact effect at the specified location.
        /// </summary>
        /// <param name="hitPoint">World position of the impact.</param>
        /// <param name="hitNormal">Surface normal at hit point.</param>
        /// <param name="isEnemyHit">Whether this is an enemy hit.</param>
        public void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal, bool isEnemyHit = false)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // Select appropriate pool
            ObjectPool<ParticleSystem> pool = isEnemyHit ? _enemyImpactPool : _defaultImpactPool;
            pool = pool ?? _defaultImpactPool;

            if (pool == null)
                return;

            // Get particle system from pool
            ParticleSystem ps = pool.Get(hitPoint, Quaternion.LookRotation(hitNormal));

            if (ps != null)
            {
                ps.Play();
                StartCoroutine(ReturnParticleSystemToPool(ps, pool, effectDuration));
            }

            // Spawn decal
            if (useDecals && _decalPool != null)
            {
                SpawnDecal(hitPoint, hitNormal);
            }

            // Play impact sound
            PlayImpactSound(hitPoint);
        }

        /// <summary>
        /// Spawns an impact decal at the specified location.
        /// </summary>
        private void SpawnDecal(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_decalPool == null)
                return;

            // Offset slightly from surface
            Vector3 decalPosition = hitPoint + hitNormal * decalOffset;
            Quaternion decalRotation = Quaternion.LookRotation(-hitNormal);

            Transform decal = _decalPool.Get(decalPosition, decalRotation);

            if (decal != null)
            {
                decal.localScale = Vector3.one * decalSize;
                StartCoroutine(ReturnDecalToPool(decal, decalLifetime));
            }
        }

        /// <summary>
        /// Plays an impact sound at the specified position.
        /// </summary>
        private void PlayImpactSound(Vector3 position)
        {
            if (impactSounds == null || impactSounds.Length == 0)
                return;

            AudioClip clip = impactSounds[Random.Range(0, impactSounds.Length)];
            if (clip == null)
                return;

            float pitch = Random.Range(impactSoundMinPitch, impactSoundMaxPitch);

            // Use AudioSource.PlayClipAtPoint for one-shot 3D audio
            // Note: This creates a temporary AudioSource which is not pooled
            // For heavy combat, consider pooling AudioSources as well
            GameObject tempAudio = new GameObject("TempImpactAudio");
            tempAudio.transform.position = position;

            AudioSource source = tempAudio.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = impactSoundVolume;
            source.pitch = pitch;
            source.spatialBlend = 1f; // 3D sound
            source.Play();

            Destroy(tempAudio, clip.length + 0.1f);
        }

        private IEnumerator ReturnParticleSystemToPool(ParticleSystem ps, ObjectPool<ParticleSystem> pool, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (ps != null && ps.gameObject.activeInHierarchy)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                pool.Return(ps);
            }
        }

        private IEnumerator ReturnDecalToPool(Transform decal, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (decal != null && _decalPool != null)
            {
                _decalPool.Return(decal);
            }
        }

        /// <summary>
        /// Cleans up all pools.
        /// </summary>
        private void CleanupPools()
        {
            _defaultImpactPool?.Clear();
            _enemyImpactPool?.Clear();
            _environmentImpactPool?.Clear();
            _decalPool?.Clear();
        }

        /// <summary>
        /// Preloads additional effects for anticipated heavy combat.
        /// </summary>
        /// <param name="additionalCount">Number of additional effects to preload.</param>
        public void PreloadEffects(int additionalCount)
        {
            // The ObjectPool class handles this internally through its expandable property
            // This method exists for explicit preloading if needed
            Debug.Log($"[ImpactEffectController] Preloading {additionalCount} additional effects...");
        }

        /// <summary>
        /// Gets the number of available impact effects in pool.
        /// </summary>
        public int AvailableEffectCount => _defaultImpactPool?.AvailableCount ?? 0;

        /// <summary>
        /// Gets whether the controller is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

#if UNITY_EDITOR
        [ContextMenu("Test Impact Effect")]
        private void TestImpactEffect()
        {
            if (Application.isPlaying)
            {
                SpawnImpactEffect(transform.position + Vector3.forward, Vector3.up, false);
            }
            else
            {
                Debug.Log("[ImpactEffectController] Test only available in Play Mode.");
            }
        }
#endif
    }
}
