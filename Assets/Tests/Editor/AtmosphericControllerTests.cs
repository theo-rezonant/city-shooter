using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Unit tests for the AtmosphericController class.
/// Tests preset configurations and value validation.
/// </summary>
[TestFixture]
public class AtmosphericControllerTests
{
    private GameObject _testGameObject;
    private AtmosphericController _controller;
    private GameObject _sunGameObject;
    private Light _sunLight;

    [SetUp]
    public void SetUp()
    {
        // Create test objects
        _testGameObject = new GameObject("TestAtmosphericController");
        _controller = _testGameObject.AddComponent<AtmosphericController>();

        // Create sun light for testing
        _sunGameObject = new GameObject("TestSunLight");
        _sunLight = _sunGameObject.AddComponent<Light>();
        _sunLight.type = LightType.Directional;

        _controller.sunLight = _sunLight;
    }

    [TearDown]
    public void TearDown()
    {
        if (_testGameObject != null)
            Object.DestroyImmediate(_testGameObject);
        if (_sunGameObject != null)
            Object.DestroyImmediate(_sunGameObject);
    }

    #region Preset Tests

    [Test]
    public void ApplyPreset_CinematicSunset_SetsCorrectSunElevation()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.CinematicSunset);

        // Assert
        Assert.AreEqual(15f, _controller.sunElevation, 0.01f,
            "CinematicSunset preset should set sun elevation to 15 degrees");
    }

    [Test]
    public void ApplyPreset_CinematicSunset_SetsWarmFogColor()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.CinematicSunset);

        // Assert - fog should be warm/orange tinted
        Assert.Greater(_controller.fogColor.r, _controller.fogColor.b,
            "CinematicSunset fog should be warmer (more red than blue)");
    }

    [Test]
    public void ApplyPreset_CyberpunkNight_SetsCoolFogColor()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.CyberpunkNight);

        // Assert - fog should be cool/purple tinted
        Assert.Greater(_controller.fogColor.b, _controller.fogColor.r,
            "CyberpunkNight fog should be cooler (more blue than red)");
    }

    [Test]
    public void ApplyPreset_CyberpunkNight_SetsLowSunIntensity()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.CyberpunkNight);

        // Assert - night should have low sun intensity
        Assert.Less(_controller.sunIntensity, 1f,
            "CyberpunkNight should have low sun intensity for nighttime");
    }

    [Test]
    public void ApplyPreset_GoldenHour_SetsHighSunIntensity()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.GoldenHour);

        // Assert
        Assert.GreaterOrEqual(_controller.sunIntensity, 2.0f,
            "GoldenHour should have high sun intensity");
    }

    [Test]
    public void ApplyPreset_GoldenHour_SetsLowColorTemperature()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.GoldenHour);

        // Assert - golden hour has warm (low) color temperature
        Assert.Less(_controller.sunColorTemperature, 4000f,
            "GoldenHour should have warm color temperature below 4000K");
    }

    [Test]
    public void ApplyPreset_OvercastMoody_SetsDesaturatedFog()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.OvercastMoody);

        // Assert - overcast fog should be nearly grey (R ≈ G ≈ B)
        float rgDiff = Mathf.Abs(_controller.fogColor.r - _controller.fogColor.g);
        float gbDiff = Mathf.Abs(_controller.fogColor.g - _controller.fogColor.b);
        Assert.Less(rgDiff, 0.1f, "Overcast fog should be desaturated");
        Assert.Less(gbDiff, 0.1f, "Overcast fog should be desaturated");
    }

    [Test]
    public void ApplyPreset_ClearDay_SetsLongFogDistance()
    {
        // Act
        _controller.ApplyPreset(AtmosphericController.AtmospherePreset.ClearDay);

        // Assert
        Assert.GreaterOrEqual(_controller.fogEndDistance, 400f,
            "ClearDay should have long fog end distance for visibility");
    }

    [Test]
    public void ApplyPreset_AllPresets_FogEnabled()
    {
        // Test all presets enable fog
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.IsTrue(_controller.fogEnabled,
                $"Preset {preset} should have fog enabled");
        }
    }

    #endregion

    #region Value Validation Tests

    [Test]
    public void SunIntensity_AllPresets_WithinValidRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.GreaterOrEqual(_controller.sunIntensity, 0f,
                $"Preset {preset}: sunIntensity should be non-negative");
            Assert.LessOrEqual(_controller.sunIntensity, 5f,
                $"Preset {preset}: sunIntensity should not exceed 5");
        }
    }

    [Test]
    public void SunElevation_AllPresets_WithinValidRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.GreaterOrEqual(_controller.sunElevation, -10f,
                $"Preset {preset}: sunElevation should be at least -10");
            Assert.LessOrEqual(_controller.sunElevation, 90f,
                $"Preset {preset}: sunElevation should not exceed 90");
        }
    }

    [Test]
    public void FogDensity_AllPresets_WithinValidRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.GreaterOrEqual(_controller.fogDensity, 0.001f,
                $"Preset {preset}: fogDensity should be at least 0.001");
            Assert.LessOrEqual(_controller.fogDensity, 0.1f,
                $"Preset {preset}: fogDensity should not exceed 0.1");
        }
    }

    [Test]
    public void FogEndDistance_AllPresets_GreaterThanStart()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.Greater(_controller.fogEndDistance, _controller.fogStartDistance,
                $"Preset {preset}: fogEndDistance should be greater than fogStartDistance");
        }
    }

    [Test]
    public void AmbientIntensity_AllPresets_WithinValidRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.GreaterOrEqual(_controller.ambientIntensity, 0f,
                $"Preset {preset}: ambientIntensity should be non-negative");
            Assert.LessOrEqual(_controller.ambientIntensity, 2f,
                $"Preset {preset}: ambientIntensity should not exceed 2");
        }
    }

    [Test]
    public void BloomIntensity_AllPresets_WithinValidRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.GreaterOrEqual(_controller.bloomIntensity, 0f,
                $"Preset {preset}: bloomIntensity should be non-negative");
            Assert.LessOrEqual(_controller.bloomIntensity, 2f,
                $"Preset {preset}: bloomIntensity should not exceed 2");
        }
    }

    [Test]
    public void VignetteIntensity_AllPresets_WithinValidRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            Assert.GreaterOrEqual(_controller.vignetteIntensity, 0f,
                $"Preset {preset}: vignetteIntensity should be non-negative");
            Assert.LessOrEqual(_controller.vignetteIntensity, 1f,
                $"Preset {preset}: vignetteIntensity should not exceed 1");
        }
    }

    #endregion

    #region Color Validation Tests

    [Test]
    public void FogColor_AllPresets_ValidColorRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            AssertValidColor(_controller.fogColor, $"Preset {preset}: fogColor");
        }
    }

    [Test]
    public void AmbientSkyColor_AllPresets_ValidColorRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            AssertValidColor(_controller.ambientSkyColor, $"Preset {preset}: ambientSkyColor");
        }
    }

    [Test]
    public void AmbientEquatorColor_AllPresets_ValidColorRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            AssertValidColor(_controller.ambientEquatorColor, $"Preset {preset}: ambientEquatorColor");
        }
    }

    [Test]
    public void AmbientGroundColor_AllPresets_ValidColorRange()
    {
        foreach (AtmosphericController.AtmospherePreset preset in
            System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)))
        {
            _controller.ApplyPreset(preset);
            AssertValidColor(_controller.ambientGroundColor, $"Preset {preset}: ambientGroundColor");
        }
    }

    private void AssertValidColor(Color color, string context)
    {
        Assert.GreaterOrEqual(color.r, 0f, $"{context}: red channel should be >= 0");
        Assert.LessOrEqual(color.r, 1f, $"{context}: red channel should be <= 1");
        Assert.GreaterOrEqual(color.g, 0f, $"{context}: green channel should be >= 0");
        Assert.LessOrEqual(color.g, 1f, $"{context}: green channel should be <= 1");
        Assert.GreaterOrEqual(color.b, 0f, $"{context}: blue channel should be >= 0");
        Assert.LessOrEqual(color.b, 1f, $"{context}: blue channel should be <= 1");
    }

    #endregion

    #region Light Application Tests

    [Test]
    public void ApplySettings_WithSunLight_AppliesIntensity()
    {
        // Arrange
        _controller.sunIntensity = 2.5f;

        // Act
        _controller.ApplySettings();

        // Assert
        Assert.AreEqual(2.5f, _sunLight.intensity, 0.01f,
            "Sun light intensity should match controller value");
    }

    [Test]
    public void ApplySettings_WithSunLight_AppliesColorTemperature()
    {
        // Arrange
        _controller.sunColorTemperature = 5500f;

        // Act
        _controller.ApplySettings();

        // Assert
        Assert.AreEqual(5500f, _sunLight.colorTemperature, 0.01f,
            "Sun light color temperature should match controller value");
        Assert.IsTrue(_sunLight.useColorTemperature,
            "Sun light should use color temperature");
    }

    [Test]
    public void ApplySettings_WithSunLight_AppliesRotation()
    {
        // Arrange
        _controller.sunAngle = 90f;
        _controller.sunElevation = 45f;

        // Act
        _controller.ApplySettings();

        // Assert
        Vector3 eulerAngles = _sunLight.transform.rotation.eulerAngles;
        Assert.AreEqual(45f, eulerAngles.x, 1f,
            "Sun light X rotation (elevation) should match");
        Assert.AreEqual(90f, eulerAngles.y, 1f,
            "Sun light Y rotation (angle) should match");
    }

    [Test]
    public void ApplySettings_WithoutSunLight_DoesNotThrow()
    {
        // Arrange
        _controller.sunLight = null;

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _controller.ApplySettings(),
            "ApplySettings should handle null sunLight gracefully");
    }

    #endregion

    #region Fog Application Tests

    [Test]
    public void ApplySettings_AppliesFogSettings_ToRenderSettings()
    {
        // Arrange
        _controller.fogEnabled = true;
        _controller.fogColor = Color.red;
        _controller.fogStartDistance = 25f;
        _controller.fogEndDistance = 200f;

        // Act
        _controller.ApplySettings();

        // Assert
        Assert.IsTrue(RenderSettings.fog, "RenderSettings.fog should be enabled");
        Assert.AreEqual(Color.red, RenderSettings.fogColor, "Fog color should match");
        Assert.AreEqual(25f, RenderSettings.fogStartDistance, 0.01f, "Fog start distance should match");
        Assert.AreEqual(200f, RenderSettings.fogEndDistance, 0.01f, "Fog end distance should match");
    }

    [Test]
    public void ApplySettings_DisablesFog_WhenFogEnabledFalse()
    {
        // Arrange
        _controller.fogEnabled = false;

        // Act
        _controller.ApplySettings();

        // Assert
        Assert.IsFalse(RenderSettings.fog, "RenderSettings.fog should be disabled");
    }

    #endregion

    #region Ambient Light Tests

    [Test]
    public void ApplySettings_AppliesAmbientSettings_ToRenderSettings()
    {
        // Arrange
        _controller.ambientSkyColor = Color.cyan;
        _controller.ambientEquatorColor = Color.yellow;
        _controller.ambientGroundColor = Color.magenta;
        _controller.ambientIntensity = 1.5f;

        // Act
        _controller.ApplySettings();

        // Assert
        Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode,
            "Ambient mode should be Trilight");
        Assert.AreEqual(Color.cyan, RenderSettings.ambientSkyColor,
            "Ambient sky color should match");
        Assert.AreEqual(Color.yellow, RenderSettings.ambientEquatorColor,
            "Ambient equator color should match");
        Assert.AreEqual(Color.magenta, RenderSettings.ambientGroundColor,
            "Ambient ground color should match");
        Assert.AreEqual(1.5f, RenderSettings.ambientIntensity, 0.01f,
            "Ambient intensity should match");
    }

    #endregion

    #region Preset Enum Tests

    [Test]
    public void AtmospherePreset_HasExpectedValues()
    {
        // Assert all expected presets exist
        Assert.IsTrue(System.Enum.IsDefined(typeof(AtmosphericController.AtmospherePreset),
            "CinematicSunset"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(AtmosphericController.AtmospherePreset),
            "CyberpunkNight"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(AtmosphericController.AtmospherePreset),
            "GoldenHour"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(AtmosphericController.AtmospherePreset),
            "OvercastMoody"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(AtmosphericController.AtmospherePreset),
            "ClearDay"));
    }

    [Test]
    public void AtmospherePreset_HasFiveOptions()
    {
        // Assert
        int presetCount = System.Enum.GetValues(typeof(AtmosphericController.AtmospherePreset)).Length;
        Assert.AreEqual(5, presetCount, "Should have exactly 5 atmosphere presets");
    }

    #endregion
}
