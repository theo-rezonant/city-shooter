import {
  Scene,
  Vector3,
  PhysicsAggregate,
  PhysicsShapeType,
  Mesh,
  AbstractMesh,
} from "@babylonjs/core";
import HavokPhysics from "@babylonjs/havok";
import type { HavokPlugin as HavokPluginType } from "@babylonjs/core";

// Re-export PhysicsMotionType for consumers
export { PhysicsMotionType } from "@babylonjs/core";

/**
 * Collision filter groups for physics layers
 * Using bit flags for efficient collision filtering
 */
export const CollisionGroups = {
  NONE: 0x0000,
  STATIC_ENVIRONMENT: 0x0001, // Buildings, floors, walls
  PLAYER: 0x0002, // Player character
  ENEMY: 0x0004, // Enemy entities
  PROJECTILE: 0x0008, // Bullets, projectiles
  TRIGGER: 0x0010, // Non-physical trigger volumes
  ALL: 0xffff,
} as const;

/**
 * Collision masks defining what each group collides with
 */
export const CollisionMasks = {
  STATIC_ENVIRONMENT: CollisionGroups.ALL, // Collides with everything
  PLAYER:
    CollisionGroups.STATIC_ENVIRONMENT |
    CollisionGroups.ENEMY |
    CollisionGroups.TRIGGER,
  ENEMY:
    CollisionGroups.STATIC_ENVIRONMENT |
    CollisionGroups.PLAYER |
    CollisionGroups.ENEMY |
    CollisionGroups.PROJECTILE,
  PROJECTILE: CollisionGroups.STATIC_ENVIRONMENT | CollisionGroups.ENEMY,
  TRIGGER: CollisionGroups.PLAYER | CollisionGroups.ENEMY,
} as const;

export interface PhysicsAggregateOptions {
  mass?: number;
  restitution?: number;
  friction?: number;
  collisionGroup?: number;
  collisionMask?: number;
}

/**
 * Manages Havok physics initialization and collision setup
 */
export class PhysicsManager {
  private scene: Scene;
  private havokPlugin: HavokPluginType | null = null;
  private isInitialized = false;
  private aggregates: PhysicsAggregate[] = [];

  constructor(scene: Scene) {
    this.scene = scene;
  }

  /**
   * Initialize the Havok physics engine
   */
  async initialize(): Promise<void> {
    if (this.isInitialized) {
      console.warn("[PhysicsManager] Already initialized");
      return;
    }

    try {
      console.log("[PhysicsManager] Initializing Havok physics...");

      // Initialize Havok WASM
      const havokInstance = await HavokPhysics();

      // Create and enable the Havok plugin
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const { HavokPlugin } = await import("@babylonjs/core");
      this.havokPlugin = new HavokPlugin(true, havokInstance);

      // Enable physics in the scene with gravity
      this.scene.enablePhysics(new Vector3(0, -9.81, 0), this.havokPlugin);

      this.isInitialized = true;
      console.log("[PhysicsManager] Havok physics initialized successfully");
    } catch (error) {
      console.error("[PhysicsManager] Failed to initialize Havok:", error);
      throw new Error(`Havok initialization failed: ${error}`);
    }
  }

  /**
   * Check if physics is initialized
   */
  isReady(): boolean {
    return this.isInitialized && this.havokPlugin !== null;
  }

  /**
   * Get the Havok plugin instance
   */
  getPlugin(): HavokPluginType | null {
    return this.havokPlugin;
  }

  /**
   * Create a static box collider for a mesh
   */
  createStaticBoxCollider(
    mesh: AbstractMesh,
    options: PhysicsAggregateOptions = {}
  ): PhysicsAggregate | null {
    if (!this.isReady()) {
      console.error("[PhysicsManager] Physics not initialized");
      return null;
    }

    const aggregate = new PhysicsAggregate(
      mesh,
      PhysicsShapeType.BOX,
      {
        mass: 0, // Static objects have 0 mass
        restitution: options.restitution ?? 0.1,
        friction: options.friction ?? 0.5,
      },
      this.scene
    );

    // Set collision filtering
    this.setCollisionFiltering(
      aggregate,
      options.collisionGroup ?? CollisionGroups.STATIC_ENVIRONMENT,
      options.collisionMask ?? CollisionMasks.STATIC_ENVIRONMENT
    );

    this.aggregates.push(aggregate);
    return aggregate;
  }

  /**
   * Create a static mesh collider for complex geometry
   */
  createStaticMeshCollider(
    mesh: AbstractMesh,
    options: PhysicsAggregateOptions = {}
  ): PhysicsAggregate | null {
    if (!this.isReady()) {
      console.error("[PhysicsManager] Physics not initialized");
      return null;
    }

    if (!(mesh instanceof Mesh)) {
      console.warn("[PhysicsManager] Mesh collider requires a Mesh instance");
      return null;
    }

    const aggregate = new PhysicsAggregate(
      mesh,
      PhysicsShapeType.MESH,
      {
        mass: 0, // Static objects have 0 mass
        restitution: options.restitution ?? 0.1,
        friction: options.friction ?? 0.5,
      },
      this.scene
    );

    // Set collision filtering
    this.setCollisionFiltering(
      aggregate,
      options.collisionGroup ?? CollisionGroups.STATIC_ENVIRONMENT,
      options.collisionMask ?? CollisionMasks.STATIC_ENVIRONMENT
    );

    this.aggregates.push(aggregate);
    return aggregate;
  }

  /**
   * Create a player capsule collider
   */
  createPlayerCollider(
    mesh: AbstractMesh,
    height: number = 1.8,
    radius: number = 0.3,
    options: PhysicsAggregateOptions = {}
  ): PhysicsAggregate | null {
    if (!this.isReady()) {
      console.error("[PhysicsManager] Physics not initialized");
      return null;
    }

    const aggregate = new PhysicsAggregate(
      mesh,
      PhysicsShapeType.CAPSULE,
      {
        mass: options.mass ?? 80, // Player mass in kg
        restitution: options.restitution ?? 0.0,
        friction: options.friction ?? 0.8,
        pointA: new Vector3(0, radius, 0),
        pointB: new Vector3(0, height - radius, 0),
        radius: radius,
      },
      this.scene
    );

    // Set collision filtering for player
    this.setCollisionFiltering(
      aggregate,
      options.collisionGroup ?? CollisionGroups.PLAYER,
      options.collisionMask ?? CollisionMasks.PLAYER
    );

    // Lock rotation so player doesn't tip over
    if (aggregate.body) {
      aggregate.body.setMassProperties({
        inertia: new Vector3(0, 0, 0), // Prevent rotation
      });
    }

    this.aggregates.push(aggregate);
    return aggregate;
  }

  /**
   * Set collision filtering on an aggregate
   */
  setCollisionFiltering(
    aggregate: PhysicsAggregate,
    group: number,
    mask: number
  ): void {
    if (aggregate.body && aggregate.shape) {
      aggregate.shape.filterMembershipMask = group;
      aggregate.shape.filterCollideMask = mask;
    }
  }

  /**
   * Create multiple static colliders for an array of meshes
   * Uses box shapes for simple geometry, mesh shapes for complex
   */
  createStaticCollidersForMeshes(
    meshes: AbstractMesh[],
    useBoxForSimple: boolean = true
  ): number {
    let colliderCount = 0;

    for (const mesh of meshes) {
      // Skip meshes with no geometry
      if (!mesh.getBoundingInfo()) continue;

      // Determine if mesh is "simple" (box-like) based on vertex count
      // Use box collider for simple geometry (faster)
      // Use mesh collider for complex geometry (more accurate)
      const isSimple =
        useBoxForSimple &&
        mesh instanceof Mesh &&
        mesh.getTotalVertices() < 100;

      if (isSimple) {
        if (this.createStaticBoxCollider(mesh)) {
          colliderCount++;
        }
      } else {
        if (this.createStaticMeshCollider(mesh)) {
          colliderCount++;
        }
      }
    }

    console.log(
      `[PhysicsManager] Created ${colliderCount} static colliders for ${meshes.length} meshes`
    );
    return colliderCount;
  }

  /**
   * Remove all physics aggregates
   */
  clearAllAggregates(): void {
    for (const aggregate of this.aggregates) {
      aggregate.dispose();
    }
    this.aggregates = [];
  }

  /**
   * Dispose of the physics manager
   */
  dispose(): void {
    this.clearAllAggregates();
    if (this.havokPlugin) {
      this.havokPlugin.dispose();
      this.havokPlugin = null;
    }
    this.isInitialized = false;
  }
}
