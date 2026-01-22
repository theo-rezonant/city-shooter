import { Scene } from '@babylonjs/core/scene';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { HavokPlugin } from '@babylonjs/core/Physics/v2/Plugins/havokPlugin';
import HavokPhysics from '@babylonjs/havok';
import type { HavokPhysicsWithBindings } from '@babylonjs/havok';

/**
 * Configuration options for the physics system
 */
export interface PhysicsSystemConfig {
  /** Gravity vector (default: -9.81 on Y axis) */
  gravity?: Vector3;
  /** Number of physics substeps per frame (default: 1) */
  substeps?: number;
  /** Fixed timestep for physics simulation (default: 1/60) */
  fixedTimestep?: number;
}

/**
 * Default physics configuration
 */
const DEFAULT_CONFIG: Required<PhysicsSystemConfig> = {
  gravity: new Vector3(0, -9.81, 0),
  substeps: 1,
  fixedTimestep: 1 / 60,
};

/**
 * Physics System Manager
 *
 * Handles Havok WASM initialization and physics world setup.
 * This is a singleton pattern to ensure only one physics engine is active.
 */
export class PhysicsSystem {
  private static instance: PhysicsSystem | null = null;

  private havokInstance: HavokPhysicsWithBindings | null = null;
  private havokPlugin: HavokPlugin | null = null;
  private scene: Scene | null = null;
  private config: Required<PhysicsSystemConfig>;
  private initialized: boolean = false;

  /**
   * Private constructor - use getInstance() instead
   */
  private constructor(config: PhysicsSystemConfig = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
    // Ensure gravity is a new Vector3 if overridden
    if (config.gravity) {
      this.config.gravity = config.gravity.clone();
    }
  }

  /**
   * Get the singleton instance of the physics system
   */
  public static getInstance(config?: PhysicsSystemConfig): PhysicsSystem {
    if (!PhysicsSystem.instance) {
      PhysicsSystem.instance = new PhysicsSystem(config);
    }
    return PhysicsSystem.instance;
  }

  /**
   * Reset the singleton instance (useful for testing)
   */
  public static resetInstance(): void {
    if (PhysicsSystem.instance) {
      PhysicsSystem.instance.dispose();
      PhysicsSystem.instance = null;
    }
  }

  /**
   * Initialize the Havok physics engine
   * Must be called before any physics operations
   *
   * @param scene - The Babylon.js scene to enable physics on
   * @returns Promise that resolves when physics is ready
   */
  public async initialize(scene: Scene): Promise<void> {
    if (this.initialized) {
      console.warn('PhysicsSystem already initialized');
      return;
    }

    this.scene = scene;

    try {
      console.log('Loading Havok WASM...');
      // Load the Havok WASM module
      this.havokInstance = await HavokPhysics();
      console.log('Havok WASM loaded successfully');

      // Create the Havok plugin
      // First parameter: useDeltaForWorldStep (true = use frame delta time)
      this.havokPlugin = new HavokPlugin(true, this.havokInstance);

      // Enable physics on the scene
      scene.enablePhysics(this.config.gravity, this.havokPlugin);

      this.initialized = true;
      console.log('Physics system initialized with gravity:', this.config.gravity.toString());
    } catch (error) {
      console.error('Failed to initialize Havok physics:', error);
      throw new Error(`Havok initialization failed: ${error}`);
    }
  }

  /**
   * Check if the physics system is initialized and ready
   */
  public isInitialized(): boolean {
    return this.initialized;
  }

  /**
   * Get the Havok plugin instance
   * Throws if not initialized
   */
  public getPlugin(): HavokPlugin {
    if (!this.havokPlugin) {
      throw new Error('PhysicsSystem not initialized. Call initialize() first.');
    }
    return this.havokPlugin;
  }

  /**
   * Get the raw Havok instance for advanced operations
   * Throws if not initialized
   */
  public getHavokInstance(): HavokPhysicsWithBindings {
    if (!this.havokInstance) {
      throw new Error('PhysicsSystem not initialized. Call initialize() first.');
    }
    return this.havokInstance;
  }

  /**
   * Get the current gravity vector
   */
  public getGravity(): Vector3 {
    return this.config.gravity.clone();
  }

  /**
   * Set a new gravity vector
   */
  public setGravity(gravity: Vector3): void {
    this.config.gravity = gravity.clone();
    if (this.scene?.getPhysicsEngine()) {
      this.scene.getPhysicsEngine()!.setGravity(gravity);
    }
  }

  /**
   * Get the scene this physics system is attached to
   */
  public getScene(): Scene | null {
    return this.scene;
  }

  /**
   * Dispose of physics resources
   */
  public dispose(): void {
    if (this.scene) {
      const physicsEngine = this.scene.getPhysicsEngine();
      if (physicsEngine) {
        physicsEngine.dispose();
      }
    }

    this.havokPlugin = null;
    this.havokInstance = null;
    this.scene = null;
    this.initialized = false;

    console.log('Physics system disposed');
  }
}

/**
 * Convenience function to initialize physics on a scene
 * @param scene - The scene to enable physics on
 * @param config - Optional physics configuration
 * @returns Promise that resolves to the PhysicsSystem instance
 */
export async function initializePhysics(
  scene: Scene,
  config?: PhysicsSystemConfig
): Promise<PhysicsSystem> {
  const physicsSystem = PhysicsSystem.getInstance(config);
  await physicsSystem.initialize(scene);
  return physicsSystem;
}
