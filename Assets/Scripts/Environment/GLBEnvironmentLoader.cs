using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CityShooter.Environment
{
    /// <summary>
    /// Handles runtime loading of GLB/glTF environment assets.
    /// Designed for loading the monolithic town4new.glb file asynchronously.
    /// </summary>
    public class GLBEnvironmentLoader : MonoBehaviour
    {
        [Header("Asset Configuration")]
        [SerializeField] private string glbAssetPath = "map/source/town4new.glb";
        [SerializeField] private string texturesPath = "map/textures";
        [SerializeField] private bool loadOnStart = false;

        [Header("Import Settings")]
        [SerializeField] private bool applyCoordinateConversion = true;
        [SerializeField] private Vector3 importRotation = new Vector3(-90f, 0f, 0f);
        [SerializeField] private Vector3 importScale = Vector3.one;
        [SerializeField] private Vector3 importPosition = Vector3.zero;

        [Header("Physics Setup")]
        [SerializeField] private bool generateColliders = true;
        [SerializeField] private bool markAsStatic = true;

        [Header("References")]
        [SerializeField] private Transform environmentParent;
        [SerializeField] private EnvironmentPhysicsSetup physicsSetup;

        private GameObject _loadedEnvironment;
        private bool _isLoading;
        private float _loadProgress;

        /// <summary>
        /// Event fired when loading starts.
        /// </summary>
        public event Action OnLoadStarted;

        /// <summary>
        /// Event fired with progress updates (0-1).
        /// </summary>
        public event Action<float> OnProgressUpdated;

        /// <summary>
        /// Event fired when loading completes successfully.
        /// </summary>
        public event Action<GameObject> OnLoadComplete;

        /// <summary>
        /// Event fired when loading fails.
        /// </summary>
        public event Action<string> OnLoadError;

        /// <summary>
        /// Gets the current loading progress (0-1).
        /// </summary>
        public float LoadProgress => _loadProgress;

        /// <summary>
        /// Gets whether loading is in progress.
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// Gets the loaded environment GameObject.
        /// </summary>
        public GameObject LoadedEnvironment => _loadedEnvironment;

        private void Start()
        {
            if (loadOnStart)
            {
                LoadEnvironment();
            }
        }

        /// <summary>
        /// Starts the asynchronous loading of the GLB environment.
        /// </summary>
        public void LoadEnvironment()
        {
            if (_isLoading)
            {
                Debug.LogWarning("[GLBEnvironmentLoader] Loading already in progress.");
                return;
            }

            StartCoroutine(LoadEnvironmentCoroutine());
        }

        /// <summary>
        /// Starts loading from a specific path.
        /// </summary>
        /// <param name="path">Path to the GLB file.</param>
        public void LoadEnvironment(string path)
        {
            glbAssetPath = path;
            LoadEnvironment();
        }

        private IEnumerator LoadEnvironmentCoroutine()
        {
            _isLoading = true;
            _loadProgress = 0f;
            OnLoadStarted?.Invoke();

            Debug.Log($"[GLBEnvironmentLoader] Starting to load environment from: {glbAssetPath}");

            // Phase 1: Validate asset paths (10%)
            _loadProgress = 0.1f;
            OnProgressUpdated?.Invoke(_loadProgress);
            yield return null;

            string fullPath = Path.Combine(Application.dataPath, "..", glbAssetPath);
            if (!ValidateAssetPath(fullPath))
            {
                HandleLoadError($"GLB file not found at: {fullPath}");
                yield break;
            }

            // Phase 2: Load textures info (20%)
            _loadProgress = 0.2f;
            OnProgressUpdated?.Invoke(_loadProgress);
            yield return null;

            string texturesFullPath = Path.Combine(Application.dataPath, "..", texturesPath);
            int textureCount = CountTextures(texturesFullPath);
            Debug.Log($"[GLBEnvironmentLoader] Found {textureCount} textures in {texturesPath}");

            // Phase 3: Create environment container (30%)
            _loadProgress = 0.3f;
            OnProgressUpdated?.Invoke(_loadProgress);
            yield return null;

            _loadedEnvironment = CreateEnvironmentContainer();

            // Phase 4: Simulate GLB loading (30-80%)
            // In a real implementation, this would use glTFast or similar importer
            // For now, we create a placeholder and demonstrate the loading flow
            yield return StartCoroutine(SimulateGLBLoading());

            // Phase 5: Apply coordinate conversion (85%)
            _loadProgress = 0.85f;
            OnProgressUpdated?.Invoke(_loadProgress);
            yield return null;

            if (applyCoordinateConversion)
            {
                ApplyCoordinateConversion(_loadedEnvironment);
            }

            // Phase 6: Setup physics (90%)
            _loadProgress = 0.9f;
            OnProgressUpdated?.Invoke(_loadProgress);
            yield return null;

            if (generateColliders)
            {
                yield return StartCoroutine(SetupPhysics());
            }

            // Phase 7: Mark as static (95%)
            _loadProgress = 0.95f;
            OnProgressUpdated?.Invoke(_loadProgress);
            yield return null;

            if (markAsStatic)
            {
                MarkEnvironmentStatic(_loadedEnvironment);
            }

            // Phase 8: Complete (100%)
            _loadProgress = 1f;
            OnProgressUpdated?.Invoke(_loadProgress);

            _isLoading = false;
            Debug.Log("[GLBEnvironmentLoader] Environment loading complete.");
            OnLoadComplete?.Invoke(_loadedEnvironment);
        }

        private bool ValidateAssetPath(string path)
        {
            bool exists = File.Exists(path);
            if (!exists)
            {
                Debug.LogWarning($"[GLBEnvironmentLoader] File not found: {path}");
            }
            return exists;
        }

        private int CountTextures(string path)
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            string[] jpgFiles = Directory.GetFiles(path, "*.jpg");
            string[] jpegFiles = Directory.GetFiles(path, "*.jpeg");
            string[] pngFiles = Directory.GetFiles(path, "*.png");

            return jpgFiles.Length + jpegFiles.Length + pngFiles.Length;
        }

        private GameObject CreateEnvironmentContainer()
        {
            GameObject container = new GameObject("Town_Environment");

            if (environmentParent != null)
            {
                container.transform.SetParent(environmentParent);
            }

            container.transform.localPosition = importPosition;
            container.transform.localScale = importScale;

            // Add necessary components
            var coordConverter = container.AddComponent<EnvironmentCoordinateConverter>();

            if (generateColliders)
            {
                physicsSetup = container.AddComponent<EnvironmentPhysicsSetup>();
            }

            return container;
        }

        private IEnumerator SimulateGLBLoading()
        {
            // This simulates the GLB loading process
            // In production, replace with actual glTFast loading:
            // var gltf = new GLTFast.GltfImport();
            // var success = await gltf.Load(fullPath);

            float startProgress = 0.3f;
            float endProgress = 0.8f;
            int steps = 10;

            for (int i = 0; i < steps; i++)
            {
                _loadProgress = Mathf.Lerp(startProgress, endProgress, (float)i / steps);
                OnProgressUpdated?.Invoke(_loadProgress);
                yield return new WaitForSeconds(0.1f); // Simulate loading time
            }

            // Create placeholder geometry to demonstrate the system
            // In production, this would be the actual loaded mesh
            CreatePlaceholderGeometry();
        }

        private void CreatePlaceholderGeometry()
        {
            // Create a simple floor plane as placeholder
            // This represents where the loaded GLB geometry would be placed
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Town_Ground_Placeholder";
            floor.transform.SetParent(_loadedEnvironment.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(100f, 1f, 100f);

            // Remove default collider (we'll add our own)
            var defaultCollider = floor.GetComponent<Collider>();
            if (defaultCollider != null)
            {
                Destroy(defaultCollider);
            }

            Debug.Log("[GLBEnvironmentLoader] Created placeholder geometry. Replace with actual GLB import in production.");
        }

        private void ApplyCoordinateConversion(GameObject target)
        {
            target.transform.rotation = Quaternion.Euler(importRotation);
            Debug.Log($"[GLBEnvironmentLoader] Applied coordinate conversion: {importRotation}");
        }

        private IEnumerator SetupPhysics()
        {
            if (physicsSetup != null)
            {
                physicsSetup.SetupEnvironmentPhysics();

                // Wait for physics setup to complete
                while (physicsSetup.IsGenerating)
                {
                    yield return null;
                }
            }
            else
            {
                // Fallback: add colliders directly
                MeshFilter[] meshFilters = _loadedEnvironment.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf.GetComponent<MeshCollider>() == null)
                    {
                        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
            }
        }

        private void MarkEnvironmentStatic(GameObject target)
        {
            target.isStatic = true;

            foreach (Transform child in target.GetComponentsInChildren<Transform>())
            {
                child.gameObject.isStatic = true;
            }

#if UNITY_EDITOR
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(target,
                UnityEditor.StaticEditorFlags.BatchingStatic |
                UnityEditor.StaticEditorFlags.NavigationStatic |
                UnityEditor.StaticEditorFlags.OccludeeStatic |
                UnityEditor.StaticEditorFlags.OccluderStatic);
#endif

            Debug.Log("[GLBEnvironmentLoader] Environment marked as static.");
        }

        private void HandleLoadError(string error)
        {
            Debug.LogError($"[GLBEnvironmentLoader] {error}");
            _isLoading = false;
            OnLoadError?.Invoke(error);
        }

        /// <summary>
        /// Unloads the current environment.
        /// </summary>
        public void UnloadEnvironment()
        {
            if (_loadedEnvironment != null)
            {
                Destroy(_loadedEnvironment);
                _loadedEnvironment = null;
                Debug.Log("[GLBEnvironmentLoader] Environment unloaded.");
            }
        }

        private void OnDestroy()
        {
            UnloadEnvironment();
        }
    }
}
