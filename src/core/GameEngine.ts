import { Engine, Scene, FreeCamera, Vector3, Color4 } from "@babylonjs/core";
import "@babylonjs/loaders/glTF";
import { PhysicsManager } from "@/physics/PhysicsManager";
import { MapLoader } from "@/map/MapLoader";
import { PlayerController } from "@/player/PlayerController";
import { PerformanceMetrics } from "@/core/PerformanceMetrics";

/**
 * Main game engine class that orchestrates all game systems
 */
export class GameEngine {
  private canvas: HTMLCanvasElement;
  private engine: Engine;
  private scene: Scene;
  private camera: FreeCamera | null = null;
  private physicsManager: PhysicsManager;
  private mapLoader: MapLoader | null = null;
  private playerController: PlayerController | null = null;
  private performanceMetrics: PerformanceMetrics;
  private isInitialized = false;

  constructor(canvas: HTMLCanvasElement) {
    this.canvas = canvas;

    // Create Babylon engine with WebGPU fallback to WebGL
    this.engine = new Engine(canvas, true, {
      preserveDrawingBuffer: true,
      stencil: true,
      antialias: true,
      powerPreference: "high-performance",
    });

    // Create the main scene
    this.scene = new Scene(this.engine);
    this.scene.clearColor = new Color4(0.1, 0.1, 0.15, 1);

    // Initialize physics manager
    this.physicsManager = new PhysicsManager(this.scene);

    // Initialize performance metrics
    this.performanceMetrics = new PerformanceMetrics(this.scene, this.engine);
  }

  /**
   * Initialize all game systems
   */
  async initialize(
    onProgress?: (progress: number, message: string) => void
  ): Promise<void> {
    if (this.isInitialized) {
      console.warn("GameEngine already initialized");
      return;
    }

    try {
      // Step 1: Initialize Havok physics
      onProgress?.(0.1, "Initializing physics engine...");
      await this.physicsManager.initialize();

      // Step 2: Create camera
      onProgress?.(0.2, "Setting up camera...");
      this.createCamera();

      // Step 3: Initialize map loader
      onProgress?.(0.3, "Loading city map...");
      this.mapLoader = new MapLoader(this.scene, this.physicsManager);

      // Step 4: Load and optimize the map
      await this.mapLoader.loadMap("/map/source/town4new.glb", (progress) => {
        const mapProgress = 0.3 + progress * 0.5; // 30-80%
        onProgress?.(
          mapProgress,
          `Loading map: ${Math.round(progress * 100)}%`
        );
      });

      // Step 5: Bake collision
      onProgress?.(0.85, "Baking physics collision...");
      await this.mapLoader.bakeCollision();

      // Step 6: Initialize player
      onProgress?.(0.9, "Setting up player...");
      if (this.camera) {
        this.playerController = new PlayerController(
          this.scene,
          this.camera,
          this.physicsManager
        );
        await this.playerController.initialize();
      }

      // Step 7: Final optimizations
      onProgress?.(0.95, "Applying final optimizations...");
      this.applyFinalOptimizations();

      // Log initial performance metrics
      onProgress?.(1.0, "Ready!");
      this.performanceMetrics.logMetrics("Initial load complete");

      this.isInitialized = true;
    } catch (error) {
      console.error("Failed to initialize game engine:", error);
      throw error;
    }
  }

  /**
   * Create the player camera
   */
  private createCamera(): void {
    // Create FPS camera at a reasonable starting position
    this.camera = new FreeCamera(
      "playerCamera",
      new Vector3(0, 2, 0),
      this.scene
    );
    this.camera.attachControl(this.canvas, true);
    this.camera.minZ = 0.1;
    this.camera.maxZ = 500;
    this.camera.speed = 0.5;
    this.camera.angularSensibility = 2000;
    this.camera.inertia = 0.5;

    // Disable default keyboard controls (handled by PlayerController)
    this.camera.keysUp = [];
    this.camera.keysDown = [];
    this.camera.keysLeft = [];
    this.camera.keysRight = [];
  }

  /**
   * Apply final scene optimizations
   */
  private applyFinalOptimizations(): void {
    // Freeze materials for better performance
    this.scene.freezeMaterials();

    // Enable frustum clipping
    this.scene.frustumPlanes;

    // Log optimization results
    console.log(
      "[GameEngine] Final optimizations applied:",
      `\n  - Materials frozen: ${this.scene.materials.length}`,
      `\n  - Active meshes: ${this.scene.getActiveMeshes().length}`,
      `\n  - Total meshes: ${this.scene.meshes.length}`
    );
  }

  /**
   * Request pointer lock for FPS controls
   */
  requestPointerLock(): void {
    this.engine.enterPointerlock();
  }

  /**
   * Check if pointer is locked
   */
  isPointerLocked(): boolean {
    return this.engine.isPointerLock;
  }

  /**
   * Start the render loop
   */
  startRenderLoop(): void {
    this.engine.runRenderLoop(() => {
      if (this.playerController) {
        this.playerController.update();
      }
      this.scene.render();
      this.performanceMetrics.update();
    });
  }

  /**
   * Handle window resize
   */
  resize(): void {
    this.engine.resize();
  }

  /**
   * Get performance metrics
   */
  getPerformanceMetrics(): PerformanceMetrics {
    return this.performanceMetrics;
  }

  /**
   * Toggle metrics display
   */
  toggleMetricsDisplay(): void {
    this.performanceMetrics.toggleDisplay();
  }

  /**
   * Get the scene
   */
  getScene(): Scene {
    return this.scene;
  }

  /**
   * Get the physics manager
   */
  getPhysicsManager(): PhysicsManager {
    return this.physicsManager;
  }

  /**
   * Clean up resources
   */
  dispose(): void {
    this.playerController?.dispose();
    this.physicsManager.dispose();
    this.scene.dispose();
    this.engine.dispose();
  }
}
