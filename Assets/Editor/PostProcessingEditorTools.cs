using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityShooter.Editor
{
    /// <summary>
    /// Editor tools for setting up and validating post-processing and occlusion culling.
    /// Provides menu items and utilities for the City Shooter project.
    /// </summary>
    public static class PostProcessingEditorTools
    {
        private const string MenuPrefix = "City Shooter/";

        [MenuItem(MenuPrefix + "Setup/Configure Post-Processing Volume")]
        public static void CreatePostProcessingVolume()
        {
            // Check if volume already exists
            Volume existingVolume = Object.FindFirstObjectByType<Volume>();
            if (existingVolume != null)
            {
                Debug.Log("[PostProcessingEditorTools] Post-Processing Volume already exists in scene.");
                Selection.activeGameObject = existingVolume.gameObject;
                return;
            }

            // Create new volume
            GameObject volumeObj = new GameObject("Global Post Processing Volume");
            Volume volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0;

            // Create and assign profile
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Add Bloom
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.2f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;

            // Add Tonemapping (ACES)
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // Add Vignette
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.25f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.4f;

            // Add Color Adjustments
            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = 0.2f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 10f;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 10f;

            // Save profile
            string profilePath = "Assets/Settings/PostProcessing/PostProcessingProfile_Generated.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            AssetDatabase.SaveAssets();

            volume.sharedProfile = profile;

            Selection.activeGameObject = volumeObj;
            Debug.Log("[PostProcessingEditorTools] Post-Processing Volume created successfully!");
        }

        [MenuItem(MenuPrefix + "Setup/Setup Occlusion Culling for Town Map")]
        public static void SetupOcclusionCulling()
        {
            // Find all renderers in scene
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;

            foreach (Renderer renderer in renderers)
            {
                // Check if this is part of the town map
                if (renderer.gameObject.name.Contains("town") ||
                    renderer.gameObject.name.Contains("Town") ||
                    renderer.transform.root.name.Contains("town") ||
                    renderer.transform.root.name.Contains("Town"))
                {
                    // Set static flags
                    StaticEditorFlags flags = StaticEditorFlags.OccluderStatic |
                                              StaticEditorFlags.OccludeeStatic |
                                              StaticEditorFlags.BatchingStatic |
                                              StaticEditorFlags.ContributeGI |
                                              StaticEditorFlags.ReflectionProbeStatic;

                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                    count++;
                }
            }

            Debug.Log($"[PostProcessingEditorTools] Set static flags on {count} objects for Occlusion Culling.");

            if (count > 0)
            {
                Debug.Log("[PostProcessingEditorTools] Next steps:");
                Debug.Log("  1. Open Window > Rendering > Occlusion Culling");
                Debug.Log("  2. Set Smallest Occluder: 5");
                Debug.Log("  3. Set Smallest Hole: 0.25");
                Debug.Log("  4. Set Backface Threshold: 100");
                Debug.Log("  5. Click 'Bake' to generate occlusion data");
            }
        }

        [MenuItem(MenuPrefix + "Setup/Mark All Environment as Static")]
        public static void MarkEnvironmentStatic()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;

            StaticEditorFlags environmentFlags = StaticEditorFlags.OccluderStatic |
                                                  StaticEditorFlags.OccludeeStatic |
                                                  StaticEditorFlags.BatchingStatic |
                                                  StaticEditorFlags.ContributeGI |
                                                  StaticEditorFlags.NavigationStatic |
                                                  StaticEditorFlags.ReflectionProbeStatic;

            foreach (Renderer renderer in renderers)
            {
                // Skip if it's tagged as Player, Enemy, or Weapon
                string tag = renderer.gameObject.tag;
                if (tag == "Player" || tag == "Enemy" || tag == "Weapon" || tag == "Projectile")
                    continue;

                // Skip if it has a Rigidbody (dynamic object)
                if (renderer.GetComponent<Rigidbody>() != null)
                    continue;

                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, environmentFlags);
                count++;
            }

            Debug.Log($"[PostProcessingEditorTools] Marked {count} environment objects as static.");
        }

        [MenuItem(MenuPrefix + "Validation/Validate Post-Processing Setup")]
        public static void ValidatePostProcessingSetup()
        {
            bool hasIssues = false;

            // Check for Volume
            Volume volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null)
            {
                Debug.LogWarning("[Validation] No Post-Processing Volume found in scene!");
                hasIssues = true;
            }
            else
            {
                Debug.Log($"[Validation] Volume found: {volume.gameObject.name}, Global: {volume.isGlobal}");

                if (volume.sharedProfile == null)
                {
                    Debug.LogWarning("[Validation] Volume has no profile assigned!");
                    hasIssues = true;
                }
                else
                {
                    // Check for required effects
                    VolumeProfile profile = volume.sharedProfile;

                    if (!profile.Has<Bloom>())
                    {
                        Debug.LogWarning("[Validation] Profile missing Bloom effect!");
                        hasIssues = true;
                    }
                    else
                    {
                        Debug.Log("[Validation] Bloom: OK");
                    }

                    if (!profile.Has<Tonemapping>())
                    {
                        Debug.LogWarning("[Validation] Profile missing Tonemapping effect!");
                        hasIssues = true;
                    }
                    else
                    {
                        Tonemapping tm;
                        if (profile.TryGet(out tm))
                        {
                            Debug.Log($"[Validation] Tonemapping: {tm.mode.value} (should be ACES)");
                            if (tm.mode.value != TonemappingMode.ACES)
                            {
                                Debug.LogWarning("[Validation] Tonemapping should be set to ACES for cinematic look!");
                            }
                        }
                    }

                    if (!profile.Has<Vignette>())
                    {
                        Debug.LogWarning("[Validation] Profile missing Vignette effect!");
                        hasIssues = true;
                    }
                    else
                    {
                        Debug.Log("[Validation] Vignette: OK");
                    }
                }
            }

            // Check camera for post-processing
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var additionalCameraData = mainCamera.GetUniversalAdditionalCameraData();
                if (additionalCameraData != null)
                {
                    if (!additionalCameraData.renderPostProcessing)
                    {
                        Debug.LogWarning("[Validation] Main Camera has post-processing disabled!");
                        hasIssues = true;
                    }
                    else
                    {
                        Debug.Log("[Validation] Camera post-processing: Enabled");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Validation] No Main Camera found!");
                hasIssues = true;
            }

            if (!hasIssues)
            {
                Debug.Log("[Validation] All post-processing checks passed!");
            }
        }

        [MenuItem(MenuPrefix + "Validation/Validate Occlusion Culling Setup")]
        public static void ValidateOcclusionSetup()
        {
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            int totalRenderers = 0;
            int occluderStatic = 0;
            int occludeeStatic = 0;
            int notStatic = 0;

            foreach (Renderer renderer in renderers)
            {
                totalRenderers++;
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);

                if ((flags & StaticEditorFlags.OccluderStatic) != 0)
                    occluderStatic++;
                if ((flags & StaticEditorFlags.OccludeeStatic) != 0)
                    occludeeStatic++;
                if (flags == 0)
                    notStatic++;
            }

            Debug.Log("[Occlusion Validation] Results:");
            Debug.Log($"  Total Renderers: {totalRenderers}");
            Debug.Log($"  Occluder Static: {occluderStatic}");
            Debug.Log($"  Occludee Static: {occludeeStatic}");
            Debug.Log($"  Not Static: {notStatic}");

            float coverage = (float)occluderStatic / totalRenderers * 100f;
            if (coverage < 50f)
            {
                Debug.LogWarning($"[Occlusion Validation] Only {coverage:F1}% of renderers are set as Occluder Static. Consider marking more environment objects!");
            }
            else
            {
                Debug.Log($"[Occlusion Validation] Good coverage: {coverage:F1}% objects are Occluder Static");
            }
        }

        [MenuItem(MenuPrefix + "Performance/Log Frame Stats")]
        public static void LogFrameStats()
        {
            Debug.Log("[Performance Stats]");
            Debug.Log($"  Target Frame Rate: {Application.targetFrameRate}");
            Debug.Log($"  VSync Count: {QualitySettings.vSyncCount}");
            Debug.Log($"  Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            Debug.Log($"  Anti-Aliasing: {QualitySettings.antiAliasing}x");
            Debug.Log($"  Shadow Resolution: {QualitySettings.shadowResolution}");
            Debug.Log($"  Shadow Distance: {QualitySettings.shadowDistance}");
            Debug.Log($"  LOD Bias: {QualitySettings.lodBias}");
        }
    }
}
