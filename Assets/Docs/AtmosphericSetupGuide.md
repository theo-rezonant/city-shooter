# Atmospheric Skybox, Fog, and Global Lighting Configuration Guide

## Overview

This document describes the atmospheric and lighting configuration for the City Shooter game's town map. The setup creates a cinematic, visually stunning environment using Unity's Universal Render Pipeline (URP) features.

## File Structure

```
Assets/
├── Settings/
│   ├── URP_HighFidelity.asset           # Main URP Pipeline Asset
│   ├── URP_HighFidelity_Renderer.asset  # URP Renderer configuration
│   └── LightingSettings.asset           # Global lighting settings
├── PostProcessing/
│   └── AtmosphericVolume_Profile.asset  # Volume profile with fog, tonemapping, bloom
├── Skybox/
│   ├── CinematicSunsetSkybox.mat        # Procedural sunset skybox
│   └── CyberpunkGradientSkybox.mat      # Gradient skybox alternative
├── Scenes/
│   └── MainTownScene.unity              # Main scene with lighting setup
├── Scripts/
│   └── AtmosphericController.cs         # Runtime atmosphere control
└── Docs/
    └── AtmosphericSetupGuide.md         # This documentation
```

## Quick Start

### 1. Assign URP Pipeline Asset

1. Go to **Edit > Project Settings > Graphics**
2. Set **Scriptable Render Pipeline Settings** to `Assets/Settings/URP_HighFidelity.asset`
3. Go to **Edit > Project Settings > Quality**
4. For each quality level, set the **Render Pipeline Asset** to the appropriate URP asset

### 2. Load the Main Scene

1. Open `Assets/Scenes/MainTownScene.unity`
2. The scene includes:
   - **Main Camera** with HDR enabled
   - **Directional Light (Sun)** configured for cinematic sunset
   - **Global Volume** with post-processing effects
   - **Light Probe Group** for dynamic object lighting
   - **Reflection Probe** for specular reflections

### 3. Import the Town Map

1. Import `map/source/town4new.glb` into the scene
2. Position it at origin (0, 0, 0)
3. The fog will automatically obscure horizon edges

## Configuration Details

### Directional Light (Sun)

| Property | Value | Description |
|----------|-------|-------------|
| Type | Directional | Primary scene light source |
| Color | RGB(255, 235, 204) | Warm sunset tone |
| Intensity | 2.0 | Bright but not overblown |
| Color Temperature | 6570K | Natural daylight reference |
| Rotation | (30°, 45°, 0°) | Aligned with skybox sun position |
| Shadow Type | Soft Shadows | High quality shadow casting |

### Fog Configuration

| Property | Value | Description |
|----------|-------|-------------|
| Mode | Linear | Best performance for large maps |
| Color | RGB(166, 140, 128) | Warm atmospheric haze |
| Start Distance | 10m | Fog begins near camera |
| End Distance | 300m | Full fog at 300 meters |
| Density | 0.005 | Subtle density for performance |

### Ambient Lighting

Using **Trilight** ambient mode for natural gradient:

| Property | Value | Description |
|----------|-------|-------------|
| Sky Color | RGB(153, 128, 115) | Warm overhead tint |
| Equator Color | RGB(89, 77, 71) | Mid-horizon warmth |
| Ground Color | RGB(38, 31, 26) | Dark warm shadows |
| Intensity | 1.0 | Standard intensity |

### Post-Processing (Volume Profile)

#### Tonemapping
- **Mode:** ACES (Academy Color Encoding System)
- Creates cinematic, film-like color response

#### Color Adjustments
- **Post Exposure:** +0.5
- **Contrast:** +15
- **Color Filter:** Warm tint RGB(255, 243, 230)
- **Saturation:** +10

#### Bloom
- **Threshold:** 0.9
- **Intensity:** 0.5
- **Scatter:** 0.7
- **Tint:** Warm orange RGB(255, 230, 204)
- **High Quality Filtering:** Enabled

#### Vignette
- **Color:** Black
- **Intensity:** 0.25
- **Smoothness:** 0.4

## AtmosphericController Script

The `AtmosphericController.cs` script provides runtime control over atmospheric settings.

### Setup

1. Add an empty GameObject to your scene
2. Attach the `AtmosphericController` component
3. Assign references:
   - **Sun Light:** The directional light
   - **Global Volume:** The post-processing volume

### Available Presets

```csharp
public enum AtmospherePreset
{
    CinematicSunset,    // Warm, dramatic sunset (default)
    CyberpunkNight,     // Cool, purple-tinted night
    GoldenHour,         // Intense warm golden light
    OvercastMoody,      // Grey, desaturated atmosphere
    ClearDay            // Bright, neutral daylight
}
```

### Runtime Preset Switching

```csharp
// Instant switch
atmosphericController.ApplyPreset(AtmospherePreset.CyberpunkNight);

// Smooth transition (5 seconds)
StartCoroutine(atmosphericController.TransitionToPreset(
    AtmospherePreset.GoldenHour,
    5f
));
```

### Exposed Properties

All properties are exposed in the Inspector for easy tweaking:

**Sun Settings:**
- `sunAngle` - Y-axis rotation (0-360°)
- `sunElevation` - X-axis elevation (-10 to 90°)
- `sunIntensity` - Light strength (0-5)
- `sunColorTemperature` - Color temp in Kelvin (1000-10000K)

**Fog Settings:**
- `fogEnabled` - Toggle fog on/off
- `fogColor` - Fog color
- `fogDensity` - Exponential fog density
- `fogStartDistance` - Linear fog start
- `fogEndDistance` - Linear fog end

**Ambient Settings:**
- `ambientSkyColor` - Sky ambient color
- `ambientEquatorColor` - Horizon ambient color
- `ambientGroundColor` - Ground bounce color
- `ambientIntensity` - Overall ambient strength

**Post-Processing:**
- `bloomIntensity` - Bloom strength
- `vignetteIntensity` - Vignette strength
- `postExposure` - Exposure adjustment

## Skybox Options

### Option 1: Procedural Sky (CinematicSunsetSkybox.mat)

Unity's built-in Procedural Sky shader with settings:
- **Atmosphere Thickness:** 1.2 (hazy atmosphere)
- **Exposure:** 1.3 (slightly bright)
- **Sun Disk:** High Quality
- **Sun Size:** 0.05 (subtle sun)
- **Sky Tint:** Warm RGB(153, 115, 102)
- **Ground Color:** RGB(89, 64, 51)

### Option 2: HDRI Skybox (Recommended for Production)

For best results, source a free HDRI from:
- [Poly Haven](https://polyhaven.com/hdris) - High quality, CC0 license
- Recommended: Search for "sunset", "dusk", or "golden hour"

**Steps to Use HDRI:**
1. Download `.hdr` or `.exr` file
2. Import into `Assets/Skybox/HDRI/`
3. Create new Material with `Skybox/Cubemap` shader
4. Assign HDRI as cubemap
5. In Lighting settings, assign as Environment > Skybox Material

### Option 3: Gradient Sky (CyberpunkGradientSkybox.mat)

Three-color gradient for stylized look:
- **Top:** Deep purple RGB(20, 13, 38)
- **Middle:** Magenta RGB(89, 38, 77)
- **Bottom:** Orange RGB(230, 102, 51)

## Light Probe Setup

The scene includes a **Light Probe Group** with probes arranged in a 3x3x3 grid covering:
- X: -20 to +20 meters
- Y: 1, 5, and 10 meters height
- Z: -20 to +20 meters

For the full town map, expand the light probe coverage:

1. Select the **Light Probe Group** GameObject
2. In Inspector, click **Edit Light Probes**
3. Add probes throughout walkable areas
4. Focus density around:
   - Street level
   - Building entrances
   - Key gameplay areas
5. Bake lighting: **Window > Rendering > Lighting > Generate Lighting**

## Performance Considerations

### For the 50MB town4new.glb Map

1. **Shadow Distance:** Set to 150m max (reduces shadow cascade complexity)
2. **Fog Mode:** Use Linear (faster than Exponential)
3. **Reflection Probe:** Single baked probe is sufficient
4. **Occlusion Culling:**
   - Window > Rendering > Occlusion Culling
   - Set **Smallest Occluder:** 5m (appropriate for buildings)
   - Bake occlusion data

### Quality Scaling

The URP Pipeline Asset supports multiple quality tiers:
- Ultra: 4K shadows, 4x MSAA, full post-processing
- High: 2K shadows, 2x MSAA, most effects
- Medium: 1K shadows, no MSAA, reduced bloom
- Low: No shadows, minimal post-processing

## Troubleshooting

### Scene Appears Too Dark
1. Check sun intensity (should be 1.5-2.5)
2. Increase ambient intensity
3. Verify Volume is set to **Global** mode

### Fog Not Visible
1. Ensure fog is enabled in RenderSettings
2. Check camera far clip plane extends beyond fog start
3. Verify Volume profile has Fog override enabled

### Skybox Not Showing
1. Camera Clear Flags should be **Skybox**
2. Assign skybox material in Lighting > Environment
3. Check shader compatibility with URP

### Dynamic Objects Look Flat
1. Ensure Light Probe Group exists in scene
2. Bake lighting to generate probe data
3. Check object has **Light Probes** set to **Blend Probes**

## Visual Style Reference

The default configuration targets a **cinematic sunset/golden hour** aesthetic:
- Warm color temperature (4500-6500K)
- Orange-tinted shadows
- Atmospheric haze obscuring distant geometry
- High contrast with soft bloom
- Subtle vignette for focus

For **cyberpunk/sci-fi** style, switch to the `CyberpunkNight` preset:
- Cool blue/purple tones
- Dense fog for mystery
- High bloom for neon glow effects
- Strong vignette

## Integration with Game Systems

### Player Spawn
Position player within the Light Probe Group coverage area for correct ambient lighting.

### Enemy AI (Soldier.fbx)
Ensure enemy prefabs have:
- Mesh Renderer > Light Probes: **Blend Probes**
- Mesh Renderer > Reflection Probes: **Blend Probes**

### Weapons (Laser Gun)
For emissive/glowing weapon effects:
- Add bloom threshold to URP asset
- Use emissive materials with HDR color values > 1.0

---

*Configuration created for Unity 2022.3+ LTS with Universal Render Pipeline*
