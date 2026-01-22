import { Engine, Scene, ArcRotateCamera, HemisphericLight, Vector3 } from '@babylonjs/core';
import '@babylonjs/loaders/glTF';
import { AssetsManagerService } from './core/AssetsManager';
import { SoldierAnimationController, AnimationTestController } from './animation';

/**
 * Main game application class.
 * Handles initialization, asset loading, and game state management.
 */
class CityShooterGame {
  private canvas: HTMLCanvasElement;
  private engine: Engine;
  private scene: Scene;
  private assetsManager: AssetsManagerService;
  private soldierController: SoldierAnimationController | null = null;
  private animationTestController: AnimationTestController | null = null;

  // UI Elements
  private loadingScreen: HTMLElement | null;
  private progressFill: HTMLElement | null;
  private progressText: HTMLElement | null;
  private enterButton: HTMLButtonElement | null;

  constructor() {
    // Get canvas element
    this.canvas = document.getElementById('renderCanvas') as HTMLCanvasElement;
    if (!this.canvas) {
      throw new Error('Canvas element not found');
    }

    // Get UI elements
    this.loadingScreen = document.getElementById('loadingScreen');
    this.progressFill = document.getElementById('progressFill');
    this.progressText = document.getElementById('progressText');
    this.enterButton = document.getElementById('enterButton') as HTMLButtonElement;

    // Initialize Babylon.js engine
    this.engine = new Engine(this.canvas, true, {
      preserveDrawingBuffer: true,
      stencil: true,
    });

    // Create the scene
    this.scene = new Scene(this.engine);

    // Initialize assets manager
    this.assetsManager = new AssetsManagerService(this.scene);
    this.assetsManager.setProgressCallback((progress, message) => {
      this.updateLoadingProgress(progress, message);
    });

    // Setup UI event listeners
    this.setupUIListeners();

    // Handle window resize
    window.addEventListener('resize', () => {
      this.engine.resize();
    });
  }

  /**
   * Setup UI event listeners
   */
  private setupUIListeners(): void {
    if (this.enterButton) {
      this.enterButton.addEventListener('click', () => {
        this.enterGame();
      });
    }
  }

  /**
   * Update loading progress UI
   */
  private updateLoadingProgress(progress: number, message: string): void {
    if (this.progressFill) {
      this.progressFill.style.width = `${progress}%`;
    }
    if (this.progressText) {
      this.progressText.textContent = message;
    }
  }

  /**
   * Initialize the game scene with basic setup
   */
  private async initializeScene(): Promise<void> {
    // Create a basic camera for viewing the soldier
    const camera = new ArcRotateCamera(
      'camera',
      Math.PI / 2,
      Math.PI / 3,
      5,
      Vector3.Zero(),
      this.scene
    );
    camera.attachControl(this.canvas, true);
    camera.wheelDeltaPercentage = 0.01;
    camera.minZ = 0.1;
    camera.maxZ = 1000;

    // Create basic lighting
    const light = new HemisphericLight('light', new Vector3(0, 1, 0), this.scene);
    light.intensity = 0.8;

    // Set background color
    this.scene.clearColor.set(0.2, 0.2, 0.3, 1);

    console.log('Scene initialized');
  }

  /**
   * Load all game assets
   */
  private async loadAssets(): Promise<void> {
    this.updateLoadingProgress(0, 'Loading assets...');

    try {
      // Load soldier and animation assets
      const containers = await this.assetsManager.loadSoldierAssetsAsync();

      // Validate loaded assets
      if (!this.assetsManager.validateSoldierAssets()) {
        throw new Error('Asset validation failed');
      }

      // Initialize soldier animation controller
      this.soldierController = new SoldierAnimationController(this.scene);
      await this.soldierController.initializeFromContainers(containers);

      // Position the soldier at origin
      this.soldierController.setPosition(new Vector3(0, 0, 0));

      // Initialize animation test controller
      this.animationTestController = new AnimationTestController(this.soldierController);

      this.updateLoadingProgress(100, 'Assets loaded successfully!');
      console.log('All assets loaded and initialized');
    } catch (error) {
      console.error('Failed to load assets:', error);
      this.updateLoadingProgress(
        0,
        `Error: ${error instanceof Error ? error.message : 'Failed to load assets'}`
      );
      throw error;
    }
  }

  /**
   * Show the enter game button after loading completes
   */
  private showEnterButton(): void {
    if (this.enterButton) {
      this.enterButton.style.display = 'block';
    }
  }

  /**
   * Enter the game (hide loading screen)
   */
  private enterGame(): void {
    if (this.loadingScreen) {
      this.loadingScreen.style.display = 'none';
    }

    // Start with idle animation
    if (this.soldierController) {
      this.soldierController.playIdle();
    }

    // Run animation test after a short delay
    this.runAnimationTest();
  }

  /**
   * Run the animation test to verify strafe/fire transitions
   */
  private async runAnimationTest(): Promise<void> {
    if (!this.animationTestController) {
      console.warn('Animation test controller not initialized');
      return;
    }

    console.log('='.repeat(50));
    console.log('ANIMATION TRANSITION TEST');
    console.log('='.repeat(50));

    // Run the strafe/fire test as required by acceptance criteria
    const result = await this.animationTestController.runStrafeFireTest(2000, 3);

    console.log('='.repeat(50));
    console.log('TEST RESULT:', result.success ? 'PASSED' : 'FAILED');
    console.log('Message:', result.message);
    console.log('Total Duration:', `${result.totalDuration.toFixed(2)}ms`);
    console.log('='.repeat(50));

    // After test, return to idle
    if (this.soldierController) {
      this.soldierController.playIdle();
    }
  }

  /**
   * Start the game loop
   */
  private startRenderLoop(): void {
    this.engine.runRenderLoop(() => {
      this.scene.render();
    });
  }

  /**
   * Main initialization method
   */
  public async initialize(): Promise<void> {
    try {
      // Initialize scene first
      await this.initializeScene();

      // Load all assets
      await this.loadAssets();

      // Show enter button
      this.showEnterButton();

      // Start render loop
      this.startRenderLoop();

      console.log('Game initialized successfully');
    } catch (error) {
      console.error('Failed to initialize game:', error);
      this.updateLoadingProgress(0, 'Failed to initialize game. Check console for details.');
    }
  }

  /**
   * Expose test controller for manual testing
   */
  public getTestController(): AnimationTestController | null {
    return this.animationTestController;
  }

  /**
   * Expose soldier controller for external access
   */
  public getSoldierController(): SoldierAnimationController | null {
    return this.soldierController;
  }
}

// Initialize and start the game
const game = new CityShooterGame();
game.initialize();

// Expose game instance for debugging
declare global {
  interface Window {
    game: CityShooterGame;
  }
}
window.game = game;
