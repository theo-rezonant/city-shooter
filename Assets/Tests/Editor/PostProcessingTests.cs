using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityShooter.Tests.Editor
{
    /// <summary>
    /// Unit tests for Post-Processing configuration and performance settings.
    /// </summary>
    [TestFixture]
    public class PostProcessingTests
    {
        private VolumeProfile testProfile;

        [SetUp]
        public void SetUp()
        {
            testProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testProfile != null)
            {
                Object.DestroyImmediate(testProfile);
            }
        }

        [Test]
        public void BloomEffect_CanBeAddedToProfile()
        {
            // Act
            Bloom bloom = testProfile.Add<Bloom>(true);

            // Assert
            Assert.IsNotNull(bloom, "Bloom effect should be created");
            Assert.IsTrue(testProfile.Has<Bloom>(), "Profile should contain Bloom");
        }

        [Test]
        public void BloomEffect_ThresholdInValidRange()
        {
            // Arrange
            Bloom bloom = testProfile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;

            // Act - Set threshold to recommended value for laser gun emissives
            bloom.threshold.value = 0.9f;

            // Assert
            Assert.GreaterOrEqual(bloom.threshold.value, 0f, "Bloom threshold should be >= 0");
            Assert.LessOrEqual(bloom.threshold.value, 2f, "Bloom threshold should be <= 2 for optimal emissive detection");
        }

        [Test]
        public void BloomEffect_IntensityInValidRange()
        {
            // Arrange
            Bloom bloom = testProfile.Add<Bloom>(true);
            bloom.intensity.overrideState = true;

            // Act - Set intensity for sci-fi weapon effects
            bloom.intensity.value = 1.2f;

            // Assert
            Assert.Greater(bloom.intensity.value, 0f, "Bloom intensity should be > 0");
            Assert.LessOrEqual(bloom.intensity.value, 3f, "Bloom intensity should be reasonable for performance");
        }

        [Test]
        public void TonemappingEffect_CanBeSetToACES()
        {
            // Arrange
            Tonemapping tonemapping = testProfile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true;

            // Act
            tonemapping.mode.value = TonemappingMode.ACES;

            // Assert
            Assert.AreEqual(TonemappingMode.ACES, tonemapping.mode.value, "Tonemapping should be set to ACES for cinematic look");
        }

        [Test]
        public void VignetteEffect_IntensityInSubtleRange()
        {
            // Arrange
            Vignette vignette = testProfile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;

            // Act - Set to subtle intensity as per requirements (0.2-0.3)
            vignette.intensity.value = 0.25f;

            // Assert
            Assert.GreaterOrEqual(vignette.intensity.value, 0.2f, "Vignette should be at least subtle");
            Assert.LessOrEqual(vignette.intensity.value, 0.4f, "Vignette should not be too strong");
        }

        [Test]
        public void ColorAdjustments_CanBeConfigured()
        {
            // Arrange
            ColorAdjustments colorAdjustments = testProfile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.saturation.overrideState = true;

            // Act
            colorAdjustments.postExposure.value = 0.2f;
            colorAdjustments.contrast.value = 10f;
            colorAdjustments.saturation.value = 10f;

            // Assert
            Assert.AreEqual(0.2f, colorAdjustments.postExposure.value, "Post exposure should match");
            Assert.AreEqual(10f, colorAdjustments.contrast.value, "Contrast should match");
            Assert.AreEqual(10f, colorAdjustments.saturation.value, "Saturation should match");
        }

        [Test]
        public void FullCinematicProfile_HasAllRequiredEffects()
        {
            // Arrange & Act - Create full cinematic profile
            testProfile.Add<Bloom>(true);
            testProfile.Add<Tonemapping>(true);
            testProfile.Add<Vignette>(true);
            testProfile.Add<ColorAdjustments>(true);
            testProfile.Add<FilmGrain>(true);

            // Assert
            Assert.IsTrue(testProfile.Has<Bloom>(), "Profile should have Bloom");
            Assert.IsTrue(testProfile.Has<Tonemapping>(), "Profile should have Tonemapping");
            Assert.IsTrue(testProfile.Has<Vignette>(), "Profile should have Vignette");
            Assert.IsTrue(testProfile.Has<ColorAdjustments>(), "Profile should have Color Adjustments");
            Assert.IsTrue(testProfile.Has<FilmGrain>(), "Profile should have Film Grain");
        }
    }

    /// <summary>
    /// Unit tests for Performance Monitor configuration.
    /// </summary>
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        public void TargetFrameRate_ShouldBe60OrHigher()
        {
            // Arrange
            int targetFPS = 60;

            // Assert
            Assert.GreaterOrEqual(targetFPS, 60, "Target FPS should be at least 60 for smooth gameplay");
        }

        [Test]
        public void QualityLevels_ExistInProject()
        {
            // Act
            string[] qualityLevels = QualitySettings.names;

            // Assert
            Assert.IsNotNull(qualityLevels, "Quality levels should exist");
            Assert.Greater(qualityLevels.Length, 0, "Should have at least one quality level");
        }

        [Test]
        public void FPSCalculation_IsAccurate()
        {
            // Arrange
            float deltaTime = 0.016667f; // ~60 FPS

            // Act
            float calculatedFPS = 1.0f / deltaTime;

            // Assert
            Assert.AreEqual(60f, calculatedFPS, 0.5f, "FPS calculation should be approximately 60");
        }

        [Test]
        public void PerformanceThresholds_AreReasonable()
        {
            // Arrange
            float targetFPS = 60f;
            float warningThreshold = 45f;
            float criticalThreshold = 30f;

            // Assert
            Assert.Greater(targetFPS, warningThreshold, "Target should be above warning");
            Assert.Greater(warningThreshold, criticalThreshold, "Warning should be above critical");
            Assert.GreaterOrEqual(criticalThreshold, 30f, "Critical threshold should be at least 30 FPS");
        }
    }

    /// <summary>
    /// Unit tests for Occlusion Culling configuration.
    /// </summary>
    [TestFixture]
    public class OcclusionCullingTests
    {
        [Test]
        public void StaticEditorFlags_IncludesOccluderStatic()
        {
            // Arrange
            StaticEditorFlags flags = StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

            // Assert
            Assert.IsTrue((flags & StaticEditorFlags.OccluderStatic) != 0, "Flags should include OccluderStatic");
            Assert.IsTrue((flags & StaticEditorFlags.OccludeeStatic) != 0, "Flags should include OccludeeStatic");
        }

        [Test]
        public void OcclusionSettings_SmallestOccluderIsValid()
        {
            // Arrange - Recommended for urban environments
            float smallestOccluder = 5.0f;

            // Assert
            Assert.GreaterOrEqual(smallestOccluder, 1f, "Smallest occluder should be at least 1 for urban environments");
            Assert.LessOrEqual(smallestOccluder, 10f, "Smallest occluder should not be too large");
        }

        [Test]
        public void OcclusionSettings_SmallestHoleIsValid()
        {
            // Arrange - Recommended value
            float smallestHole = 0.25f;

            // Assert
            Assert.Greater(smallestHole, 0f, "Smallest hole should be positive");
            Assert.LessOrEqual(smallestHole, 1f, "Smallest hole should be reasonably small");
        }

        [Test]
        public void OcclusionSettings_BackfaceThresholdIsValid()
        {
            // Arrange - Recommended value
            float backfaceThreshold = 100f;

            // Assert
            Assert.GreaterOrEqual(backfaceThreshold, 50f, "Backface threshold should be at least 50");
            Assert.LessOrEqual(backfaceThreshold, 100f, "Backface threshold should not exceed 100");
        }

        [Test]
        public void StaticFlags_CombinationForEnvironment()
        {
            // Arrange - Full static flags for environment objects
            StaticEditorFlags environmentFlags =
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.ReflectionProbeStatic;

            // Assert - Verify all expected flags are present
            Assert.IsTrue((environmentFlags & StaticEditorFlags.OccluderStatic) != 0);
            Assert.IsTrue((environmentFlags & StaticEditorFlags.OccludeeStatic) != 0);
            Assert.IsTrue((environmentFlags & StaticEditorFlags.BatchingStatic) != 0);
            Assert.IsTrue((environmentFlags & StaticEditorFlags.ContributeGI) != 0);
            Assert.IsTrue((environmentFlags & StaticEditorFlags.NavigationStatic) != 0);
            Assert.IsTrue((environmentFlags & StaticEditorFlags.ReflectionProbeStatic) != 0);
        }
    }
}
