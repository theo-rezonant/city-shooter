using UnityEngine;

namespace CityShooter.Settings
{
    /// <summary>
    /// ScriptableObject for storing environment configuration settings.
    /// Create via: Right-click > Create > City Shooter > Environment Settings
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentSettings", menuName = "City Shooter/Environment Settings", order = 1)]
    public class EnvironmentSettings : ScriptableObject
    {
        [Header("Asset Paths")]
        [Tooltip("Path to the GLB environment file relative to project root")]
        public string glbAssetPath = "map/source/town4new.glb";

        [Tooltip("Path to the textures folder relative to project root")]
        public string texturesPath = "map/textures";

        [Header("Import Settings")]
        [Tooltip("Rotation to apply for coordinate system conversion (Blender Z-up to Unity Y-up)")]
        public Vector3 importRotation = new Vector3(-90f, 0f, 0f);

        [Tooltip("Scale multiplier for the imported environment")]
        public Vector3 importScale = Vector3.one;

        [Tooltip("Position offset for the imported environment")]
        public Vector3 importPosition = Vector3.zero;

        [Header("Physics")]
        [Tooltip("Generate mesh colliders for environment geometry")]
        public bool generateColliders = true;

        [Tooltip("Use convex colliders (faster but less accurate)")]
        public bool useConvexColliders = false;

        [Tooltip("Physics material for environment surfaces")]
        public PhysicMaterial environmentPhysicMaterial;

        [Header("Static Settings")]
        [Tooltip("Mark environment as static for batching and NavMesh")]
        public bool markAsStatic = true;

        [Header("NavMesh Settings")]
        [Tooltip("Agent radius for NavMesh baking")]
        [Range(0.1f, 2f)]
        public float navMeshAgentRadius = 0.5f;

        [Tooltip("Agent height for NavMesh baking")]
        [Range(0.5f, 3f)]
        public float navMeshAgentHeight = 2f;

        [Tooltip("Maximum slope angle in degrees")]
        [Range(0f, 60f)]
        public float maxSlope = 45f;

        [Tooltip("Step height for NavMesh")]
        [Range(0f, 1f)]
        public float stepHeight = 0.4f;

        [Header("Optimization")]
        [Tooltip("Enable static batching for rendering optimization")]
        public bool enableStaticBatching = true;

        [Tooltip("Enable occlusion culling")]
        public bool enableOcclusionCulling = true;

        [Header("Loading")]
        [Tooltip("Minimum time to show loading screen (seconds)")]
        [Range(0f, 5f)]
        public float minimumLoadTime = 1.5f;

        [Tooltip("Maximum colliders to process per frame during setup")]
        [Range(1, 20)]
        public int maxCollidersPerFrame = 5;

        /// <summary>
        /// Gets the full path to the GLB asset.
        /// </summary>
        public string GetFullGLBPath()
        {
            return System.IO.Path.Combine(Application.dataPath, "..", glbAssetPath);
        }

        /// <summary>
        /// Gets the full path to the textures folder.
        /// </summary>
        public string GetFullTexturesPath()
        {
            return System.IO.Path.Combine(Application.dataPath, "..", texturesPath);
        }

        /// <summary>
        /// Validates the settings and returns any issues.
        /// </summary>
        public string[] Validate()
        {
            var issues = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(glbAssetPath))
            {
                issues.Add("GLB asset path is not set.");
            }
            else if (!System.IO.File.Exists(GetFullGLBPath()))
            {
                issues.Add($"GLB file not found at: {GetFullGLBPath()}");
            }

            if (string.IsNullOrEmpty(texturesPath))
            {
                issues.Add("Textures path is not set.");
            }
            else if (!System.IO.Directory.Exists(GetFullTexturesPath()))
            {
                issues.Add($"Textures directory not found at: {GetFullTexturesPath()}");
            }

            if (navMeshAgentRadius <= 0)
            {
                issues.Add("NavMesh agent radius must be positive.");
            }

            if (navMeshAgentHeight <= 0)
            {
                issues.Add("NavMesh agent height must be positive.");
            }

            return issues.ToArray();
        }
    }
}
