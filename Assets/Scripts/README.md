# Soldier Enemy AI System

A complete enemy AI system for the City Shooter game, featuring NavMesh-based pathfinding, state machine AI, hit reaction animations, and integration with the modular laser combat system.

## Overview

This system provides:
- **State Machine AI**: Idle, Chase, Attack, React, and Death states
- **NavMesh Navigation**: Uses Unity's NavMeshAgent for pathfinding on the `town4new` map
- **Hit Reaction System**: Immediate animation interrupts when hit by laser weapons
- **Combat Integration**: IDamageable interface for damage handling
- **Event-Driven Architecture**: Performance-optimized with events instead of polling

## Quick Start

### 1. Set Up the Soldier Prefab

1. Import `Soldier.fbx` into Unity
2. Add the following components to the root object:
   - `NavMeshAgent`
   - `Animator`
   - `CapsuleCollider`
   - `SoldierAI`
   - `EnemyHealth`
   - `SoldierSetupHelper` (optional, for automatic configuration)
3. Set the GameObject layer to "Enemy"
4. Assign the `SoldierAnimatorController` to the Animator

### 2. Create the Animator Controller

Use the menu: `CityShooter > Setup > Create Soldier Animator Controller`

This creates an animator controller with:
- **Base Layer**: Idle, Move, Attack, Death states
- **Reaction Layer**: Override layer for immediate hit reactions

### 3. Set Up Animation Clips

Map the following animations:
| State | Animation Source |
|-------|-----------------|
| Idle | Soldier.fbx (idle clip) |
| Move | Soldier.fbx or Strafe.fbx |
| Attack | static_fire.fbx or moving fire.fbx |
| React | Reaction.fbx |
| Death | Soldier.fbx (death clip) |

### 4. Bake the NavMesh

1. Import `town4new.glb` into your scene
2. Mark static geometry as "Navigation Static"
3. Open Window > AI > Navigation
4. Bake the NavMesh

### 5. Set Up the Combat System

Add `LaserCombatSystem` to your player's weapon:
1. Assign fire point transform
2. Configure damage, range, and fire rate
3. Set hit layers to include "Enemy"

## Architecture

```
CityShooter.Enemy
├── SoldierAI.cs          # Main AI controller with state machine
├── EnemyHealth.cs        # Health management with damage events
├── SoldierSpawner.cs     # Enemy spawning and management
└── SoldierSetupHelper.cs # Prefab configuration utility

CityShooter.Combat
└── LaserCombatSystem.cs  # Raycasting weapon system

CityShooter.Interfaces
└── IDamageable.cs        # Interface for damageable objects

CityShooter.Editor
├── SoldierAnimatorSetup.cs # Animator controller generator
└── SoldierFBXImporter.cs   # FBX import configuration
```

## Component Reference

### SoldierAI

Main AI controller managing enemy behavior.

**Inspector Settings:**
| Field | Description | Default |
|-------|-------------|---------|
| Detection Range | Distance to detect player | 20 |
| Attack Range | Distance to start attacking | 2 |
| Field of View | Detection angle in degrees | 120 |
| Walk Speed | Movement speed when patrolling | 2 |
| Run Speed | Movement speed when chasing | 5 |
| Stopping Distance | Distance to stop from target | 1.5 |
| Hit Reaction Duration | Time to play hit animation | 0.5 |

**Events:**
- `OnStateChanged(SoldierState)` - Fired when state transitions
- `OnDeath` - Fired when soldier dies

### EnemyHealth

Health management component implementing `IDamageable`.

**Inspector Settings:**
| Field | Description | Default |
|-------|-------------|---------|
| Max Health | Maximum health points | 100 |
| Invulnerability Duration | Brief immunity after hit | 0.2 |
| Death Delay | Time before destruction | 3 |
| Destroy On Death | Auto-destroy when dead | true |

**Events:**
- `OnDamageTaken(float damage, Vector3 hitPoint)` - Fired when damaged
- `OnHealthChanged(float current, float max)` - Fired on health changes
- `OnDeath` - Fired when health reaches zero

### LaserCombatSystem

Modular weapon system with raycast hit detection.

**Inspector Settings:**
| Field | Description | Default |
|-------|-------------|---------|
| Damage | Damage per hit | 25 |
| Range | Maximum raycast distance | 100 |
| Fire Rate | Time between shots | 0.15 |
| Hit Layers | Physics layers to hit | All except IgnoreRaycast |
| Automatic Fire | Hold to fire continuously | true |

**Events:**
- `OnLaserHit(RaycastHit, IDamageable)` - Fired when hitting damageable target
- `OnLaserFired` - Fired when weapon fires

## Physics Layer Setup

1. Open Project Settings > Tags and Layers
2. Add a layer named "Enemy" (suggested: Layer 8)
3. Configure Physics collision matrix as needed
4. Soldier prefabs will auto-assign to this layer

## Animation Integration

### Hit Reaction (Interrupt)

The Reaction animation is set up on a separate Animator layer with Override blending. This ensures:
- Immediate response to damage
- Interrupts any current animation
- Automatic return to previous state

### Animator Parameters

| Parameter | Type | Purpose |
|-----------|------|---------|
| Speed | Float | NavMeshAgent velocity magnitude |
| IsMoving | Bool | Whether currently moving |
| Attack | Trigger | Initiates attack animation |
| React | Trigger | Triggers hit reaction |
| Death | Trigger | Triggers death animation |
| IsAlive | Bool | Prevents transitions when dead |

## Performance Considerations

- **Event-Driven**: Damage handling uses events instead of Update polling
- **Layered Raycasting**: Only targets "Enemy" layer for combat
- **Object Pooling Ready**: SoldierSpawner can be extended for pooling
- **NavMesh Optimization**: Uses NavMesh.SamplePosition for spawn validation

## Dependencies

- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP)
- Navigation package (AI Navigation)

## Asset Requirements

| Asset | Location | Purpose |
|-------|----------|---------|
| Soldier.fbx | Assets/ | Enemy character model |
| Reaction.fbx | Root | Hit reaction animation |
| Strafe.fbx | Root | Strafing movement animation |
| town4new.glb | map/source/ | Environment with NavMesh |

## Troubleshooting

### Soldier doesn't move
- Verify NavMesh is baked for the scene
- Check that NavMeshAgent is enabled
- Ensure "Player" tag is set on player object

### Hit reactions don't play
- Verify Animator has "React" trigger parameter
- Check Reaction layer weight is 1
- Confirm Reaction.fbx animation is assigned

### Raycasts don't hit enemies
- Verify soldiers are on "Enemy" layer
- Check LaserCombatSystem hit layers include "Enemy"
- Ensure colliders are enabled on soldier

### Coordinate system issues
- Ensure FBX files have "Bake Axis Conversion" enabled
- Check model orientation after import

## License

Part of the City Shooter project.
