import { Engine } from '@babylonjs/core/Engines/engine';
import { Scene } from '@babylonjs/core/scene';
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera';
import { HemisphericLight } from '@babylonjs/core/Lights/hemisphericLight';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { GameStateManager } from './managers';
import { GameState } from './types';

// Import required side effects
import '@babylonjs/core/Helpers/sceneHelpers';
import '@babylonjs/loaders/glTF';

/**
 * City Shooter - Main Entry Point
 *
 * Initializes the Babylon.js engine and scene, then hands control
 * to the GameStateManager for handling assets, pointer lock, and game state.
 */
class CityShooterGame {
  private canvas: HTMLCanvasElement;
  private engine: Engine;
  private scene: Scene;
  private camera: FreeCamera;
  private gameStateManager: GameStateManager;

  constructor(canvasId: string) {
    const canvasElement = document.getElementById(canvasId);
    if (!canvasElement || !(canvasElement instanceof HTMLCanvasElement)) {
      throw new Error(`Canvas element '${canvasId}' not found`);
    }
    this.canvas = canvasElement;

    // Initialize Babylon.js engine
    this.engine = new Engine(this.canvas, true, {
      preserveDrawingBuffer: true,
      stencil: true,
    });

    // Create scene
    this.scene = new Scene(this.engine);

    // Setup basic camera (will be positioned properly after assets load)
    this.camera = new FreeCamera('mainCamera', new Vector3(0, 5, -10), this.scene);
    this.camera.setTarget(Vector3.Zero());
    this.camera.attachControl(this.canvas, true);

    // Add basic lighting
    const light = new HemisphericLight('mainLight', new Vector3(0, 1, 0), this.scene);
    light.intensity = 0.7;

    // Initialize game state manager
    this.gameStateManager = new GameStateManager(this.engine, this.scene);

    // Setup resize handler
    window.addEventListener('resize', () => {
      this.engine.resize();
    });
  }

  /**
   * Start the game
   */
  public async start(): Promise<void> {
    // Start render loop
    this.engine.runRenderLoop(() => {
      // Only update scene if in gameplay or loading
      const state = this.gameStateManager.getCurrentState();
      if (state === GameState.GAMEPLAY || state === GameState.LOADING) {
        this.scene.render();
      } else {
        // Still render but at reduced rate for UI
        this.scene.render();
      }
    });

    // Initialize game state (loads assets, shows loading screen, etc.)
    await this.gameStateManager.initialize();

    console.log('City Shooter initialized successfully');
  }

  /**
   * Get the game state manager for external access
   */
  public getGameStateManager(): GameStateManager {
    return this.gameStateManager;
  }

  /**
   * Dispose game resources
   */
  public dispose(): void {
    this.gameStateManager.dispose();
    this.scene.dispose();
    this.engine.dispose();
  }
}

// Initialize game when DOM is ready
const initGame = (): void => {
  try {
    const game = new CityShooterGame('renderCanvas');
    game.start().catch((error) => {
      console.error('Failed to start game:', error);
    });

    // Expose game instance for debugging
    (window as unknown as { game: CityShooterGame }).game = game;
  } catch (error) {
    console.error('Failed to initialize game:', error);
    const errorDiv = document.createElement('div');
    errorDiv.style.cssText =
      'position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); ' +
      'background: #ff0000; color: white; padding: 20px; font-family: sans-serif;';
    errorDiv.textContent = `Game initialization failed: ${error instanceof Error ? error.message : 'Unknown error'}`;
    document.body.appendChild(errorDiv);
  }
};

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initGame);
} else {
  initGame();
}
