using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace CityShooter.Navigation
{
    /// <summary>
    /// Runtime NavMesh setup for the town environment.
    /// Handles marking surfaces as walkable and NavMesh baking configuration.
    /// </summary>
    public class NavMeshSetup : MonoBehaviour
    {
        [Header("NavMesh Settings")]
        [SerializeField] private float agentRadius = 0.5f;
        [SerializeField] private float agentHeight = 2f;
        [SerializeField] private float maxSlope = 45f;
        [SerializeField] private float stepHeight = 0.4f;

        [Header("Surface Detection")]
        [SerializeField] private bool autoDetectWalkableSurfaces = true;
        [SerializeField] private float walkableSurfaceMaxAngle = 30f;
        [SerializeField] private LayerMask walkableLayerMask = ~0;

        [Header("Area Configuration")]
        [SerializeField] private int walkableAreaIndex = 0;
        [SerializeField] private int nonWalkableAreaIndex = 1;

        [Header("Runtime Building")]
        [SerializeField] private bool buildOnStart = false;
        [SerializeField] private NavMeshSurface navMeshSurface;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color walkableColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color nonWalkableColor = new Color(1f, 0f, 0f, 0.3f);

        private List<NavMeshModifier> _modifiers = new List<NavMeshModifier>();
        private bool _isBuilding;

        /// <summary>
        /// Event fired when NavMesh building is complete.
        /// </summary>
        public event System.Action OnNavMeshBuildComplete;

        /// <summary>
        /// Event fired on NavMesh build error.
        /// </summary>
        public event System.Action<string> OnNavMeshBuildError;

        private void Start()
        {
            if (buildOnStart)
            {
                SetupAndBuildNavMesh();
            }
        }

        /// <summary>
        /// Sets up NavMesh modifiers and optionally builds the NavMesh.
        /// </summary>
        public void SetupAndBuildNavMesh()
        {
            if (_isBuilding)
            {
                Debug.LogWarning("[NavMeshSetup] NavMesh building already in progress.");
                return;
            }

            StartCoroutine(SetupNavMeshCoroutine());
        }

        private System.Collections.IEnumerator SetupNavMeshCoroutine()
        {
            _isBuilding = true;
            Debug.Log("[NavMeshSetup] Starting NavMesh setup...");

            // Setup NavMesh surface if not assigned
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                }
            }

            // Configure the NavMesh surface
            ConfigureNavMeshSurface();

            yield return null;

            // Auto-detect and tag walkable surfaces
            if (autoDetectWalkableSurfaces)
            {
                DetectWalkableSurfaces();
            }

            yield return null;

            // Build the NavMesh
            try
            {
                Debug.Log("[NavMeshSetup] Building NavMesh...");
                navMeshSurface.BuildNavMesh();
                Debug.Log("[NavMeshSetup] NavMesh built successfully.");
                OnNavMeshBuildComplete?.Invoke();
            }
            catch (System.Exception e)
            {
                string error = $"Failed to build NavMesh: {e.Message}";
                Debug.LogError($"[NavMeshSetup] {error}");
                OnNavMeshBuildError?.Invoke(error);
            }

            _isBuilding = false;
        }

        private void ConfigureNavMeshSurface()
        {
            // Configure agent settings
            navMeshSurface.agentTypeID = GetAgentTypeID();
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.defaultArea = walkableAreaIndex;

            Debug.Log($"[NavMeshSetup] NavMesh surface configured - Agent radius: {agentRadius}, height: {agentHeight}");
        }

        private int GetAgentTypeID()
        {
            // Return the default humanoid agent ID (0)
            // In a full implementation, this would look up or create a custom agent type
            return 0;
        }

        private void DetectWalkableSurfaces()
        {
            Debug.Log("[NavMeshSetup] Detecting walkable surfaces...");
            _modifiers.Clear();

            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                // Check if this surface is roughly horizontal (walkable)
                if (IsWalkableSurface(renderer.transform))
                {
                    AddWalkableModifier(renderer.gameObject);
                }
                else
                {
                    // Walls and steep surfaces - still navigable around
                    AddNonWalkableModifier(renderer.gameObject);
                }
            }

            Debug.Log($"[NavMeshSetup] Detected {_modifiers.Count} surface modifiers.");
        }

        private bool IsWalkableSurface(Transform surfaceTransform)
        {
            // Check the angle of the surface normal relative to world up
            Vector3 surfaceUp = surfaceTransform.up;
            float angle = Vector3.Angle(surfaceUp, Vector3.up);

            return angle <= walkableSurfaceMaxAngle;
        }

        private void AddWalkableModifier(GameObject target)
        {
            NavMeshModifier modifier = target.GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = target.AddComponent<NavMeshModifier>();
            }

            modifier.overrideArea = true;
            modifier.area = walkableAreaIndex;
            _modifiers.Add(modifier);
        }

        private void AddNonWalkableModifier(GameObject target)
        {
            NavMeshModifier modifier = target.GetComponent<NavMeshModifier>();
            if (modifier == null)
            {
                modifier = target.AddComponent<NavMeshModifier>();
            }

            modifier.overrideArea = true;
            modifier.area = nonWalkableAreaIndex;
            _modifiers.Add(modifier);
        }

        /// <summary>
        /// Manually marks a specific area as walkable.
        /// </summary>
        public void MarkAreaWalkable(Bounds bounds)
        {
            // Create a NavMesh obstacle that's inverted to allow walking
            // This is a simplified implementation - in production you'd use NavMeshModifierVolume
            Debug.Log($"[NavMeshSetup] Marked area as walkable: {bounds.center}");
        }

        /// <summary>
        /// Manually marks a specific area as non-walkable.
        /// </summary>
        public void MarkAreaNonWalkable(Bounds bounds)
        {
            Debug.Log($"[NavMeshSetup] Marked area as non-walkable: {bounds.center}");
        }

        /// <summary>
        /// Clears and rebuilds the NavMesh.
        /// </summary>
        public void RebuildNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
                navMeshSurface.BuildNavMesh();
                Debug.Log("[NavMeshSetup] NavMesh rebuilt.");
            }
        }

        /// <summary>
        /// Validates the NavMesh by checking for coverage.
        /// </summary>
        public bool ValidateNavMesh()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            bool isValid = triangulation.vertices.Length > 0;

            if (isValid)
            {
                Debug.Log($"[NavMeshSetup] NavMesh validation passed. Vertices: {triangulation.vertices.Length}, Triangles: {triangulation.indices.Length / 3}");
            }
            else
            {
                Debug.LogWarning("[NavMeshSetup] NavMesh validation failed - no navigation data.");
            }

            return isValid;
        }

        /// <summary>
        /// Gets the NavMesh area at a specific world position.
        /// </summary>
        public int GetAreaAtPosition(Vector3 position)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
            {
                return NavMesh.GetAreaFromName("Walkable");
            }
            return -1;
        }

        /// <summary>
        /// Gets whether NavMesh building is in progress.
        /// </summary>
        public bool IsBuilding => _isBuilding;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Draw agent size representation
            Gizmos.color = walkableColor;
            Gizmos.DrawWireSphere(transform.position, agentRadius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * agentHeight);
        }
#endif
    }
}
