# Async Level Loading and Environment Physics System

This document describes the implementation of the async level loading system for the `town4new.glb` environment.

## Overview

The system provides:
- Asynchronous scene loading with progress tracking
- UI loading bar with smooth progress visualization
- Automatic mesh collider generation for physics
- NavMesh baking for AI pathfinding
- Blender to Unity coordinate system conversion

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Loading/
│   │   ├── AsyncLevelLoader.cs        # Core async loading logic
│   │   ├── LoadingUIController.cs     # UI progress bar controller
│   │   └── LoadingSceneManager.cs     # Main loading orchestrator
│   ├── Environment/
│   │   ├── EnvironmentPhysicsSetup.cs      # Mesh collider generation
│   │   ├── EnvironmentCoordinateConverter.cs # Blender Z-up to Unity Y-up
│   │   ├── GLBEnvironmentLoader.cs         # GLB asset loading
│   │   └── TownSceneInitializer.cs         # Town scene setup
│   ├── NavMesh/
│   │   └── NavMeshSetup.cs            # NavMesh configuration and baking
│   ├── Settings/
│   │   └── EnvironmentSettings.cs     # Configuration ScriptableObject
│   └── CityShooter.asmdef
├── Editor/
│   └── TownEnvironmentEditor.cs       # Editor tools and inspectors
├── Tests/
│   └── Editor/
│       ├── AsyncLevelLoaderTests.cs
│       ├── EnvironmentPhysicsSetupTests.cs
│       ├── EnvironmentCoordinateConverterTests.cs
│       ├── NavMeshSetupTests.cs
│       ├── GLBEnvironmentLoaderTests.cs
│       └── CityShooter.Tests.Editor.asmdef
├── Scenes/                            # Create Loading.unity and Town.unity here
├── Prefabs/                           # Store prefabs here
├── Materials/                         # Materials for the environment
├── Resources/                         # Runtime-loadable assets
└── Settings/                          # ScriptableObject configurations
```

## Setup Instructions

### 1. Create Scenes

1. **Loading Scene** (`Assets/Scenes/Loading.unity`):
   - Create a new scene
   - Add a Canvas with UI elements:
     - Slider (progress bar)
     - TextMeshPro text for percentage
     - TextMeshPro text for status messages
   - Create an empty GameObject "LoadingManager" with:
     - `AsyncLevelLoader` component
     - `LoadingUIController` component
     - `LoadingSceneManager` component

2. **Town Scene** (`Assets/Scenes/Town.unity`):
   - Create a new scene
   - Import `town4new.glb` as a child of an empty "Town_Environment" object
   - Add `TownSceneInitializer` to the scene root
   - Add `NavMeshSurface` component to the environment

### 2. Configure the GLB Import

1. Select `map/source/town4new.glb` in the Project window
2. In the Inspector, configure Model Import Settings:
   - **Model Tab**:
     - Mesh Compression: Medium
     - Read/Write: Enabled
     - Generate Colliders: ON
   - **Animation Tab**:
     - Import Animation: OFF (not needed for environment)
3. Click Apply

### 3. Add Scenes to Build Settings

1. Open File > Build Settings
2. Add both scenes in order:
   - Loading (index 0)
   - Town (index 1)
3. Set Loading as the startup scene

### 4. Configure Environment Settings

1. Right-click in Project > Create > City Shooter > Environment Settings
2. Configure the settings:
   - GLB Asset Path: `map/source/town4new.glb`
   - Textures Path: `map/textures`
   - Import Rotation: (-90, 0, 0)
   - Generate Colliders: ON
   - Mark as Static: ON

### 5. Setup NavMesh

1. Select the Town_Environment object
2. Menu: City Shooter > Environment > Mark Selection as Static
3. Menu: City Shooter > Environment > Bake NavMesh
4. Or use the NavMeshSetup component's editor button

## Usage

### Loading Scene Flow

```
1. App starts at Loading scene
2. LoadingSceneManager initiates loading sequence
3. AsyncLevelLoader begins async load of Town scene
4. LoadingUIController displays progress (0-100%)
5. Progress reaches 100%, scene transitions to Town
6. TownSceneInitializer validates physics and NavMesh
7. Player is spawned and gameplay begins
```

### AsyncOperation.progress Behavior

Note: Unity's `AsyncOperation.progress` goes from 0 to 0.9 while loading, then jumps to 1.0 when the scene activates. The `AsyncLevelLoader` normalizes this:

```csharp
float loadProgress = Mathf.Clamp01(_asyncOperation.progress / 0.9f);
```

This ensures the UI shows 0-100% smoothly.

### Coordinate Conversion

The `town4new.glb` is exported from Blender (Z-up coordinate system). Unity uses Y-up. The `EnvironmentCoordinateConverter` applies a -90° X rotation:

```csharp
// Blender: (X, Y, Z) -> Unity: (X, Z, -Y)
transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * originalRotation;
```

## Editor Tools

Access via menu: **City Shooter > Environment**

- **Setup Town Environment**: Creates root object with required components
- **Generate Mesh Colliders**: Adds MeshCollider to all child meshes
- **Mark Selection as Static**: Sets static flags for batching and NavMesh
- **Bake NavMesh**: Builds the navigation mesh
- **Clear NavMesh**: Removes NavMesh data
- **Validate Environment Setup**: Checks for common issues
- **Configure GLB Import Settings**: Auto-configures import settings

## Component Reference

### AsyncLevelLoader

```csharp
public class AsyncLevelLoader : MonoBehaviour
{
    // Events
    public event Action<float> OnProgressUpdated;
    public event Action OnLoadingStarted;
    public event Action OnLoadingComplete;
    public event Action<string> OnLoadingError;

    // Properties
    public float Progress { get; }
    public bool IsLoading { get; }

    // Methods
    public void StartLoading();
    public void StartLoading(string sceneName);
    public void ActivateScene();
    public void CancelLoading();
}
```

### EnvironmentPhysicsSetup

```csharp
public class EnvironmentPhysicsSetup : MonoBehaviour
{
    // Events
    public event Action OnCollisionSetupComplete;
    public event Action<float> OnProgressUpdated;

    // Properties
    public int ColliderCount { get; }
    public bool IsGenerating { get; }

    // Methods
    public void SetupEnvironmentPhysics();
    public void RemoveAllColliders();
}
```

### NavMeshSetup

```csharp
public class NavMeshSetup : MonoBehaviour
{
    // Events
    public event Action OnNavMeshBuildComplete;
    public event Action<string> OnNavMeshBuildError;

    // Properties
    public bool IsBuilding { get; }

    // Methods
    public void SetupAndBuildNavMesh();
    public void RebuildNavMesh();
    public bool ValidateNavMesh();
}
```

## Performance Considerations

### Monolithic Asset Handling

The `town4new.glb` is a 50MB monolithic file. Recommendations:

1. **Mesh Colliders**: Generated automatically. If performance drops, consider:
   - Using primitive colliders for large buildings
   - Enabling mesh compression

2. **Occlusion Culling**: Bake occlusion data for the town:
   - Window > Rendering > Occlusion Culling
   - Set up Occlusion Areas
   - Bake

3. **Static Batching**: All environment objects are marked static for draw call optimization

4. **NavMesh**: Bake at edit time, not runtime, for production builds

### Memory Management

- ~80MB total assets (50MB mesh + ~27MB textures)
- Use addressables for production to manage memory
- Consider texture streaming for the 99 textures

## Testing

Run tests via: Window > General > Test Runner > Edit Mode

Tests cover:
- Component initialization
- Event subscription
- Coordinate conversion math
- Progress tracking
- Error handling

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Player falls through floor | Check mesh colliders are generated. Run Generate Mesh Colliders from editor menu |
| NavMesh not working | Ensure environment is marked Static. Re-bake NavMesh |
| Textures not loading | Verify textures are in `map/textures/` and GLB references are correct |
| Coordinate system wrong | Apply -90° X rotation to the root or enable coordinate conversion |
| Scene load freezes | Ensure using `LoadSceneAsync`, not `LoadScene` |
