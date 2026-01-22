import { Scene } from '@babylonjs/core/scene';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { Color3 } from '@babylonjs/core/Maths/math.color';
import { Mesh } from '@babylonjs/core/Meshes/mesh';
import { MeshBuilder } from '@babylonjs/core/Meshes/meshBuilder';
import { StandardMaterial } from '@babylonjs/core/Materials/standardMaterial';
import { PhysicsAggregate } from '@babylonjs/core/Physics/v2/physicsAggregate';
import { PhysicsShapeType } from '@babylonjs/core/Physics/v2/IPhysicsEnginePlugin';
import { PhysicsMotionType } from '@babylonjs/core/Physics/v2/IPhysicsEnginePlugin';
import { CollisionLayers, CollisionMasks } from '../physics/CollisionLayers';

/**
 * Player capsule dimensions
 * Sized to approximate human dimensions relative to the town4new.glb map
 */
export const PLAYER_DIMENSIONS = {
  /** Total height of player capsule in meters */
  HEIGHT: 1.8,
  /** Radius of the capsule in meters */
  RADIUS: 0.3,
  /** Camera/eye height from ground */
  EYE_HEIGHT: 1.6,
  /** Mass of the player in kg */
  MASS: 80,
} as const;

/**
 * Configuration for player physics
 */
export interface PlayerPhysicsConfig {
  /** Starting position for the player */
  startPosition?: Vector3;
  /** Player capsule height (default: 1.8m) */
  height?: number;
  /** Player capsule radius (default: 0.3m) */
  radius?: number;
  /** Player mass in kg (default: 80) */
  mass?: number;
  /** Whether to show the debug capsule mesh (default: false in production) */
  showDebugMesh?: boolean;
}

/**
 * Default player physics configuration
 */
const DEFAULT_CONFIG: Required<PlayerPhysicsConfig> = {
  startPosition: new Vector3(0, 2, 0), // Start slightly above ground
  height: PLAYER_DIMENSIONS.HEIGHT,
  radius: PLAYER_DIMENSIONS.RADIUS,
  mass: PLAYER_DIMENSIONS.MASS,
  showDebugMesh: true, // Visible for testing/debugging
};

/**
 * Player Physics Controller
 *
 * Manages the physical representation of the player using a capsule shape.
 * The capsule has rotation constraints to prevent tipping over.
 */
export class PlayerPhysics {
  private mesh: Mesh;
  private aggregate: PhysicsAggregate;
  private scene: Scene;
  private config: Required<PlayerPhysicsConfig>;

  /**
   * Create a new player physics controller
   *
   * @param scene - The Babylon.js scene
   * @param config - Optional configuration
   */
  constructor(scene: Scene, config: PlayerPhysicsConfig = {}) {
    this.scene = scene;
    this.config = {
      ...DEFAULT_CONFIG,
      ...config,
      startPosition: config.startPosition?.clone() ?? DEFAULT_CONFIG.startPosition.clone(),
    };

    // Create the capsule mesh
    this.mesh = this.createCapsuleMesh();

    // Create the physics aggregate
    this.aggregate = this.createPhysicsAggregate();

    // Apply rotation constraints to prevent tipping
    this.applyRotationConstraints();

    console.log(
      `PlayerPhysics created at ${this.config.startPosition.toString()} ` +
        `with height=${this.config.height}m, radius=${this.config.radius}m`
    );
  }

  /**
   * Create the visual capsule mesh for the player
   */
  private createCapsuleMesh(): Mesh {
    // Create a capsule mesh
    // Note: Babylon's capsule height is the total height including the hemispheres
    const capsule = MeshBuilder.CreateCapsule(
      'playerCapsule',
      {
        height: this.config.height,
        radius: this.config.radius,
        tessellation: 16,
        subdivisions: 1,
      },
      this.scene
    );

    // Position the mesh at the start position
    capsule.position = this.config.startPosition.clone();

    // Set visibility based on config
    capsule.isVisible = this.config.showDebugMesh;

    // Apply a basic material for visibility during testing
    if (this.config.showDebugMesh) {
      const material = new StandardMaterial('playerMaterial', this.scene);
      material.diffuseColor = new Color3(0.2, 0.6, 1.0); // Blue color
      material.alpha = 0.7; // Semi-transparent
      capsule.material = material;
    }

    return capsule;
  }

  /**
   * Create the physics aggregate for the player capsule
   */
  private createPhysicsAggregate(): PhysicsAggregate {
    const aggregate = new PhysicsAggregate(
      this.mesh,
      PhysicsShapeType.CAPSULE,
      {
        mass: this.config.mass,
        friction: 0.5,
        restitution: 0.0, // No bounce
      },
      this.scene
    );

    // Set collision filtering
    const body = aggregate.body;
    body.setCollisionCallbackEnabled(true);

    // Configure collision groups using shape filtering
    // The player belongs to PLAYER group and collides with ENVIRONMENT and ENEMIES
    const shape = aggregate.shape;
    shape.filterMembershipMask = CollisionLayers.PLAYER;
    shape.filterCollideMask = CollisionMasks.PLAYER;

    // Set motion type to dynamic (affected by physics)
    body.setMotionType(PhysicsMotionType.DYNAMIC);

    return aggregate;
  }

  /**
   * Apply rotation constraints to prevent the capsule from tipping over
   * This locks rotation on X and Z axes while allowing Y rotation (turning)
   */
  private applyRotationConstraints(): void {
    const body = this.aggregate.body;

    // Lock angular velocity on X and Z axes to prevent tipping
    // This is done by setting angular damping very high or using mass properties
    body.setAngularDamping(1000); // High damping to prevent rotation

    // Alternative approach: Set inertia to prevent rotation on certain axes
    // We'll set the mass properties to have very high inertia on X and Z
    body.setMassProperties({
      inertia: new Vector3(0, 1, 0), // Only allow rotation around Y axis
      mass: this.config.mass,
    });
  }

  /**
   * Get the player mesh
   */
  public getMesh(): Mesh {
    return this.mesh;
  }

  /**
   * Get the physics aggregate
   */
  public getAggregate(): PhysicsAggregate {
    return this.aggregate;
  }

  /**
   * Get the physics body
   */
  public getBody() {
    return this.aggregate.body;
  }

  /**
   * Get the current position of the player
   */
  public getPosition(): Vector3 {
    return this.mesh.position.clone();
  }

  /**
   * Set the player position (teleport)
   * @param position - New position
   */
  public setPosition(position: Vector3): void {
    this.aggregate.body.disablePreStep = false;
    this.mesh.position = position.clone();
    // Force physics body to sync with mesh position
    this.aggregate.body.setTargetTransform(position, this.mesh.rotationQuaternion!);
  }

  /**
   * Apply a linear impulse to the player (e.g., for jumping)
   * @param impulse - The impulse vector
   */
  public applyImpulse(impulse: Vector3): void {
    this.aggregate.body.applyImpulse(impulse, this.mesh.position);
  }

  /**
   * Get the current linear velocity
   */
  public getLinearVelocity(): Vector3 {
    return this.aggregate.body.getLinearVelocity();
  }

  /**
   * Set the linear velocity
   * @param velocity - The new velocity vector
   */
  public setLinearVelocity(velocity: Vector3): void {
    this.aggregate.body.setLinearVelocity(velocity);
  }

  /**
   * Get the eye/camera height position
   */
  public getEyePosition(): Vector3 {
    const pos = this.getPosition();
    // Eye position is at the top of the capsule minus a small offset
    pos.y += this.config.height / 2 - this.config.radius;
    return pos;
  }

  /**
   * Check if the player is grounded (for jumping logic)
   * This is a simple check - may need refinement based on game needs
   */
  public isGrounded(): boolean {
    const velocity = this.getLinearVelocity();
    // Consider grounded if vertical velocity is very small
    return Math.abs(velocity.y) < 0.1;
  }

  /**
   * Dispose of physics resources
   */
  public dispose(): void {
    this.aggregate.dispose();
    this.mesh.dispose();
    console.log('PlayerPhysics disposed');
  }
}
