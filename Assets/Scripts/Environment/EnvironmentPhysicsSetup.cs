using System.Collections.Generic;
using UnityEngine;

namespace CityShooter.Environment
{
    /// <summary>
    /// Sets up physics colliders for the environment geometry.
    /// Handles the monolithic town4new.glb mesh with optimized collision generation.
    /// </summary>
    public class EnvironmentPhysicsSetup : MonoBehaviour
    {
        [Header("Collision Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useConvexColliders = false;
        [SerializeField] private MeshColliderCookingOptions cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;

        [Header("Layer Configuration")]
        [SerializeField] private string environmentLayer = "Environment";
        [SerializeField] private PhysicMaterial environmentPhysicMaterial;

        [Header("Optimization")]
        [SerializeField] private bool enableCollisionMerging = true;
        [SerializeField] private int maxCollidersPerFrame = 5;

        [Header("Static Flags")]
        [SerializeField] private bool markAsStatic = true;
        [SerializeField] private StaticEditorFlags staticFlags = StaticEditorFlags.BatchingStatic |
                                                                  StaticEditorFlags.NavigationStatic |
                                                                  StaticEditorFlags.OccludeeStatic |
                                                                  StaticEditorFlags.OccluderStatic;

        private List<MeshCollider> _generatedColliders = new List<MeshCollider>();
        private int _processedCount;
        private bool _isGenerating;

        /// <summary>
        /// Event fired when collision setup is complete.
        /// </summary>
        public event System.Action OnCollisionSetupComplete;

        /// <summary>
        /// Event fired with progress updates (0-1).
        /// </summary>
        public event System.Action<float> OnProgressUpdated;

        private void Start()
        {
            if (generateOnStart)
            {
                SetupEnvironmentPhysics();
            }
        }

        /// <summary>
        /// Initiates the physics setup for all child meshes.
        /// </summary>
        public void SetupEnvironmentPhysics()
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[EnvironmentPhysicsSetup] Already generating colliders.");
                return;
            }

            Debug.Log("[EnvironmentPhysicsSetup] Starting environment physics setup...");
            StartCoroutine(SetupCollidersCoroutine());
        }

        private System.Collections.IEnumerator SetupCollidersCoroutine()
        {
            _isGenerating = true;
            _processedCount = 0;
            _generatedColliders.Clear();

            // Get all mesh filters in children
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);

            int totalCount = meshFilters.Length;
            Debug.Log($"[EnvironmentPhysicsSetup] Found {totalCount} meshes to process.");

            int processedThisFrame = 0;

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                // Add mesh collider
                MeshCollider collider = AddMeshCollider(meshFilter.gameObject, meshFilter.sharedMesh);
                if (collider != null)
                {
                    _generatedColliders.Add(collider);
                }

                // Mark as static for NavMesh and optimization
                if (markAsStatic)
                {
                    SetStaticFlags(meshFilter.gameObject);
                }

                // Set layer
                SetEnvironmentLayer(meshFilter.gameObject);

                _processedCount++;
                processedThisFrame++;

                // Update progress
                float progress = (float)_processedCount / totalCount;
                OnProgressUpdated?.Invoke(progress);

                // Yield to prevent frame drops
                if (processedThisFrame >= maxCollidersPerFrame)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }

            // Mark root as static as well
            if (markAsStatic)
            {
                SetStaticFlags(gameObject);
            }

            _isGenerating = false;
            Debug.Log($"[EnvironmentPhysicsSetup] Completed. Generated {_generatedColliders.Count} colliders.");
            OnCollisionSetupComplete?.Invoke();
        }

        private MeshCollider AddMeshCollider(GameObject target, Mesh mesh)
        {
            // Skip if collider already exists
            MeshCollider existingCollider = target.GetComponent<MeshCollider>();
            if (existingCollider != null)
            {
                return existingCollider;
            }

            try
            {
                MeshCollider collider = target.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = useConvexColliders;
                collider.cookingOptions = cookingOptions;

                if (environmentPhysicMaterial != null)
                {
                    collider.sharedMaterial = environmentPhysicMaterial;
                }

                return collider;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnvironmentPhysicsSetup] Failed to add collider to {target.name}: {e.Message}");
                return null;
            }
        }

        private void SetStaticFlags(GameObject target)
        {
#if UNITY_EDITOR
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(target, staticFlags);
#endif
            // Runtime static flag (limited functionality)
            target.isStatic = true;
        }

        private void SetEnvironmentLayer(GameObject target)
        {
            int layerIndex = LayerMask.NameToLayer(environmentLayer);
            if (layerIndex >= 0)
            {
                target.layer = layerIndex;
            }
        }

        /// <summary>
        /// Removes all generated colliders.
        /// </summary>
        public void RemoveAllColliders()
        {
            foreach (MeshCollider collider in _generatedColliders)
            {
                if (collider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(collider);
                    }
                    else
                    {
                        DestroyImmediate(collider);
                    }
                }
            }
            _generatedColliders.Clear();
            Debug.Log("[EnvironmentPhysicsSetup] All generated colliders removed.");
        }

        /// <summary>
        /// Gets the count of generated colliders.
        /// </summary>
        public int ColliderCount => _generatedColliders.Count;

        /// <summary>
        /// Gets whether collision generation is in progress.
        /// </summary>
        public bool IsGenerating => _isGenerating;
    }
}
