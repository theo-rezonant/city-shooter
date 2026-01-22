import { Engine } from '@babylonjs/core/Engines/engine';
import { Scene } from '@babylonjs/core/scene';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { Color3, Color4 } from '@babylonjs/core/Maths/math.color';
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera';
import { HemisphericLight } from '@babylonjs/core/Lights/hemisphericLight';
import { MeshBuilder } from '@babylonjs/core/Meshes/meshBuilder';
import { StandardMaterial } from '@babylonjs/core/Materials/standardMaterial';
import { PhysicsAggregate } from '@babylonjs/core/Physics/v2/physicsAggregate';
import { PhysicsShapeType } from '@babylonjs/core/Physics/v2/IPhysicsEnginePlugin';
import { PhysicsSystem, initializePhysics } from '../physics';
import { CollisionLayers, CollisionMasks } from '../physics/CollisionLayers';
import { PlayerPhysics } from '../player';
import { GameUI } from '../ui';

// Required side-effects for Babylon.js
import '@babylonjs/core/Physics/v2/physicsEngineComponent';

/**
 * Game states
 */
export enum GameState {
  LOADING = 'LOADING',
  READY = 'READY',
  GAMEPLAY = 'GAMEPLAY',
  PAUSED = 'PAUSED',
}

/**
 * Main Application Class
 *
 * Manages the game lifecycle, scene, physics, and state transitions.
 */
export class App {
  private canvas: HTMLCanvasElement;
  private engine: Engine;
  private scene: Scene;
  private camera: FreeCamera;
  private physicsSystem: PhysicsSystem | null = null;
  private playerPhysics: PlayerPhysics | null = null;
  private gameUI: GameUI;
  private gameState: GameState = GameState.LOADING;

  /**
   * Create the application
   * @param canvasId - ID of the canvas element
   */
  constructor(canvasId: string = 'renderCanvas') {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !(canvas instanceof HTMLCanvasElement)) {
      throw new Error(`Canvas element '${canvasId}' not found`);
    }
    this.canvas = canvas;

    // Create the Babylon.js engine
    this.engine = new Engine(this.canvas, true, {
      preserveDrawingBuffer: true,
      stencil: true,
    });

    // Create empty scene (will be configured in initialize)
    this.scene = new Scene(this.engine);

    // Create temporary camera (will be replaced after physics init)
    this.camera = new FreeCamera('camera', new Vector3(0, 5, -10), this.scene);

    // Create game UI
    this.gameUI = new GameUI(this.engine);

    // Handle window resize
    window.addEventListener('resize', () => {
      this.engine.resize();
    });
  }

  /**
   * Initialize the game
   * Loads Havok physics and sets up the scene
   */
  public async initialize(): Promise<void> {
    console.log('Initializing City Shooter...');

    try {
      // Configure scene
      this.scene.clearColor = new Color4(0.1, 0.1, 0.15, 1);

      // Initialize physics (await Havok WASM load)
      console.log('Initializing physics...');
      this.physicsSystem = await initializePhysics(this.scene);
      console.log('Physics initialized successfully');

      // Create the test scene
      this.createTestScene();

      // Create player physics
      this.createPlayer();

      // Setup camera to follow player
      this.setupCamera();

      // Setup UI
      this.setupUI();

      // Transition to READY state
      this.setGameState(GameState.READY);

      // Hide loading screen
      this.gameUI.hideLoadingScreen();

      console.log('City Shooter initialized successfully');
    } catch (error) {
      console.error('Failed to initialize:', error);
      throw error;
    }
  }

  /**
   * Create the test scene with ground and obstacles
   */
  private createTestScene(): void {
    // Create ambient light
    const light = new HemisphericLight('ambientLight', new Vector3(0, 1, 0), this.scene);
    light.intensity = 0.8;
    light.groundColor = new Color3(0.3, 0.3, 0.35);

    // Create ground plane
    const ground = MeshBuilder.CreateGround('ground', { width: 50, height: 50 }, this.scene);
    ground.position.y = 0;

    // Ground material
    const groundMaterial = new StandardMaterial('groundMaterial', this.scene);
    groundMaterial.diffuseColor = new Color3(0.3, 0.3, 0.3);
    groundMaterial.specularColor = new Color3(0.1, 0.1, 0.1);
    ground.material = groundMaterial;

    // Add physics to ground (static)
    const groundAggregate = new PhysicsAggregate(
      ground,
      PhysicsShapeType.BOX,
      { mass: 0, friction: 0.8, restitution: 0.1 },
      this.scene
    );

    // Set ground collision layer (Environment)
    groundAggregate.shape.filterMembershipMask = CollisionLayers.ENVIRONMENT;
    groundAggregate.shape.filterCollideMask = CollisionMasks.ENVIRONMENT;

    // Create some test obstacles (buildings)
    this.createTestObstacle(new Vector3(5, 1, 5), new Vector3(2, 2, 2));
    this.createTestObstacle(new Vector3(-5, 1.5, 8), new Vector3(3, 3, 2));
    this.createTestObstacle(new Vector3(8, 2, -3), new Vector3(2, 4, 2));
    this.createTestObstacle(new Vector3(-8, 0.75, -6), new Vector3(4, 1.5, 3));

    // Create a ramp for testing
    this.createRamp(new Vector3(0, 0.5, 10), new Vector3(0, 0.3, 0));

    console.log('Test scene created with ground and obstacles');
  }

  /**
   * Create a test obstacle (box)
   */
  private createTestObstacle(position: Vector3, size: Vector3): void {
    const box = MeshBuilder.CreateBox(
      `obstacle_${position.x}_${position.z}`,
      { width: size.x, height: size.y, depth: size.z },
      this.scene
    );
    box.position = position;

    // Random color material
    const material = new StandardMaterial(`obstacleMat_${position.x}_${position.z}`, this.scene);
    material.diffuseColor = new Color3(
      0.4 + Math.random() * 0.3,
      0.4 + Math.random() * 0.3,
      0.5 + Math.random() * 0.3
    );
    box.material = material;

    // Add physics (static)
    const aggregate = new PhysicsAggregate(
      box,
      PhysicsShapeType.BOX,
      { mass: 0, friction: 0.6, restitution: 0.1 },
      this.scene
    );

    // Set collision layer (Environment)
    aggregate.shape.filterMembershipMask = CollisionLayers.ENVIRONMENT;
    aggregate.shape.filterCollideMask = CollisionMasks.ENVIRONMENT;
  }

  /**
   * Create a ramp for testing slope physics
   */
  private createRamp(position: Vector3, rotation: Vector3): void {
    const ramp = MeshBuilder.CreateBox('ramp', { width: 4, height: 0.2, depth: 6 }, this.scene);
    ramp.position = position;
    ramp.rotation = rotation;

    const material = new StandardMaterial('rampMaterial', this.scene);
    material.diffuseColor = new Color3(0.6, 0.5, 0.4);
    ramp.material = material;

    // Add physics (static)
    const aggregate = new PhysicsAggregate(
      ramp,
      PhysicsShapeType.BOX,
      { mass: 0, friction: 0.8, restitution: 0.0 },
      this.scene
    );

    // Set collision layer (Environment)
    aggregate.shape.filterMembershipMask = CollisionLayers.ENVIRONMENT;
    aggregate.shape.filterCollideMask = CollisionMasks.ENVIRONMENT;
  }

  /**
   * Create the player physics capsule
   */
  private createPlayer(): void {
    this.playerPhysics = new PlayerPhysics(this.scene, {
      startPosition: new Vector3(0, 3, 0), // Start above ground
      showDebugMesh: true, // Show the capsule for testing
    });

    console.log('Player created with physics capsule');
  }

  /**
   * Setup the camera to follow player
   */
  private setupCamera(): void {
    if (!this.playerPhysics) return;

    // Position camera at player eye level
    const eyePos = this.playerPhysics.getEyePosition();
    this.camera.position = eyePos;

    // Attach camera controls to canvas
    this.camera.attachControl(this.canvas, true);
    this.camera.speed = 0.5;
    this.camera.angularSensibility = 1000;

    // Set camera as active
    this.scene.activeCamera = this.camera;
  }

  /**
   * Setup UI elements
   */
  private setupUI(): void {
    // Show enter button initially
    this.gameUI.showEnterButton();

    // Handle pointer lock changes
    this.gameUI.setPointerLockCallback((isLocked) => {
      if (isLocked) {
        this.setGameState(GameState.GAMEPLAY);
      } else {
        if (this.gameState === GameState.GAMEPLAY) {
          this.setGameState(GameState.PAUSED);
        }
      }
    });
  }

  /**
   * Set the game state
   */
  private setGameState(state: GameState): void {
    const previousState = this.gameState;
    this.gameState = state;
    console.log(`Game state: ${previousState} -> ${state}`);

    switch (state) {
      case GameState.READY:
        // Show enter button
        this.gameUI.showEnterButton();
        break;
      case GameState.GAMEPLAY:
        // Game is active
        this.gameUI.hideEnterButton();
        this.gameUI.hidePauseOverlay();
        break;
      case GameState.PAUSED:
        // Show pause overlay
        this.gameUI.showPauseOverlay();
        break;
    }
  }

  /**
   * Get the current game state
   */
  public getGameState(): GameState {
    return this.gameState;
  }

  /**
   * Get the physics system
   */
  public getPhysicsSystem(): PhysicsSystem | null {
    return this.physicsSystem;
  }

  /**
   * Get the player physics
   */
  public getPlayerPhysics(): PlayerPhysics | null {
    return this.playerPhysics;
  }

  /**
   * Get the scene
   */
  public getScene(): Scene {
    return this.scene;
  }

  /**
   * Get the engine
   */
  public getEngine(): Engine {
    return this.engine;
  }

  /**
   * Start the render loop
   */
  public run(): void {
    this.engine.runRenderLoop(() => {
      // Update camera to follow player in gameplay mode
      if (this.gameState === GameState.GAMEPLAY && this.playerPhysics) {
        // Sync camera position with player (in a real game, this would be more sophisticated)
        // For now, we just keep camera at a fixed position for testing physics
      }

      this.scene.render();
    });

    console.log('Render loop started');
  }

  /**
   * Dispose of all resources
   */
  public dispose(): void {
    if (this.playerPhysics) {
      this.playerPhysics.dispose();
    }
    if (this.physicsSystem) {
      this.physicsSystem.dispose();
    }
    this.gameUI.dispose();
    this.scene.dispose();
    this.engine.dispose();
    console.log('App disposed');
  }
}
