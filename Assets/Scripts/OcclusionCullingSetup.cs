using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CityShooter.Performance
{
    /// <summary>
    /// Editor utility script to set up Occlusion Culling for the town4new.glb map.
    /// This script configures the monolithic map asset as both Occluder Static and Occludee Static
    /// to optimize draw calls in the urban environment.
    /// </summary>
    public class OcclusionCullingSetup : MonoBehaviour
    {
        [Header("Target Objects")]
        [Tooltip("The root transform containing the town4new map geometry")]
        [SerializeField] private Transform townMapRoot;

        [Header("Occlusion Settings")]
        [SerializeField] private bool setOccluderStatic = true;
        [SerializeField] private bool setOccludeeStatic = true;
        [SerializeField] private bool setLightmapStatic = true;
        [SerializeField] private bool setNavigationStatic = true;
        [SerializeField] private bool setReflectionProbeStatic = true;

        [Header("Bake Settings")]
        [Tooltip("Smallest occluder size (5m recommended for urban environments)")]
        [SerializeField] private float smallestOccluder = 5.0f;
        [Tooltip("Smallest hole size (0.25m recommended)")]
        [SerializeField] private float smallestHole = 0.25f;
        [Tooltip("Backface threshold (100 recommended)")]
        [SerializeField] private float backfaceThreshold = 100f;

#if UNITY_EDITOR
        /// <summary>
        /// Sets up all child objects with the appropriate static flags for occlusion culling.
        /// Call this from the editor context menu or via script.
        /// </summary>
        [ContextMenu("Setup Static Flags for Occlusion Culling")]
        public void SetupStaticFlags()
        {
            if (townMapRoot == null)
            {
                townMapRoot = transform;
            }

            int objectsProcessed = 0;
            StaticEditorFlags flags = StaticEditorFlags.BatchingStatic;

            if (setOccluderStatic)
                flags |= StaticEditorFlags.OccluderStatic;
            if (setOccludeeStatic)
                flags |= StaticEditorFlags.OccludeeStatic;
            if (setLightmapStatic)
                flags |= StaticEditorFlags.ContributeGI;
            if (setNavigationStatic)
                flags |= StaticEditorFlags.NavigationStatic;
            if (setReflectionProbeStatic)
                flags |= StaticEditorFlags.ReflectionProbeStatic;

            // Process all child objects recursively
            ProcessTransform(townMapRoot, flags, ref objectsProcessed);

            Debug.Log($"[OcclusionCullingSetup] Processed {objectsProcessed} objects with static flags: {flags}");
        }

        private void ProcessTransform(Transform root, StaticEditorFlags flags, ref int count)
        {
            // Set static flags on this object
            GameObjectUtility.SetStaticEditorFlags(root.gameObject, flags);
            count++;

            // Process all children
            foreach (Transform child in root)
            {
                ProcessTransform(child, flags, ref count);
            }
        }

        /// <summary>
        /// Configures the Occlusion Culling bake settings in the editor.
        /// </summary>
        [ContextMenu("Configure Occlusion Bake Settings")]
        public void ConfigureOcclusionBakeSettings()
        {
            // These settings are applied through the Unity Editor's Occlusion Culling window
            // This method serves as documentation for the recommended settings

            Debug.Log($"[OcclusionCullingSetup] Recommended Occlusion Culling Settings:");
            Debug.Log($"  Smallest Occluder: {smallestOccluder}");
            Debug.Log($"  Smallest Hole: {smallestHole}");
            Debug.Log($"  Backface Threshold: {backfaceThreshold}");
            Debug.Log($"");
            Debug.Log($"To bake Occlusion Culling:");
            Debug.Log($"  1. Open Window > Rendering > Occlusion Culling");
            Debug.Log($"  2. Set the values above in the Bake tab");
            Debug.Log($"  3. Click 'Bake' to generate occlusion data");
        }

        /// <summary>
        /// Validates the current occlusion setup.
        /// </summary>
        [ContextMenu("Validate Occlusion Setup")]
        public void ValidateOcclusionSetup()
        {
            if (townMapRoot == null)
            {
                townMapRoot = transform;
            }

            int totalObjects = 0;
            int occluderCount = 0;
            int occludeeCount = 0;
            int missingFlags = 0;

            ValidateTransform(townMapRoot, ref totalObjects, ref occluderCount, ref occludeeCount, ref missingFlags);

            Debug.Log($"[OcclusionCullingSetup] Validation Results:");
            Debug.Log($"  Total Objects: {totalObjects}");
            Debug.Log($"  Occluder Static: {occluderCount}");
            Debug.Log($"  Occludee Static: {occludeeCount}");
            Debug.Log($"  Missing Flags: {missingFlags}");

            if (missingFlags > 0)
            {
                Debug.LogWarning($"[OcclusionCullingSetup] {missingFlags} objects are missing static flags. Run 'Setup Static Flags' to fix.");
            }
            else
            {
                Debug.Log("[OcclusionCullingSetup] All objects are properly configured for occlusion culling!");
            }
        }

        private void ValidateTransform(Transform root, ref int total, ref int occluder, ref int occludee, ref int missing)
        {
            total++;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(root.gameObject);

            if ((flags & StaticEditorFlags.OccluderStatic) != 0)
                occluder++;
            if ((flags & StaticEditorFlags.OccludeeStatic) != 0)
                occludee++;
            if ((flags & (StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic)) == 0)
                missing++;

            foreach (Transform child in root)
            {
                ValidateTransform(child, ref total, ref occluder, ref occludee, ref missing);
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Visualize the occlusion area
            Gizmos.color = new Color(0, 1, 0, 0.2f);

            if (townMapRoot != null)
            {
                Bounds bounds = CalculateBounds(townMapRoot);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private Bounds CalculateBounds(Transform root)
        {
            Bounds bounds = new Bounds(root.position, Vector3.zero);
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
