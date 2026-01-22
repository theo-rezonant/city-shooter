import { SceneManager } from './core/SceneManager';
import { LoadingScreen } from './ui/LoadingScreen';
import { GameState } from './types/GameState';

/**
 * Main application entry point
 * Initializes the Babylon.js engine, scene, and loading screen
 */
class App {
  private _sceneManager: SceneManager;
  private _loadingScreen: LoadingScreen;

  constructor() {
    // Get the canvas element
    const canvas = document.getElementById('renderCanvas') as HTMLCanvasElement;

    if (!canvas) {
      throw new Error('Canvas element not found');
    }

    // Initialize the scene manager
    this._sceneManager = new SceneManager(canvas);

    // Initialize the loading screen
    this._loadingScreen = new LoadingScreen(this._sceneManager);

    // Register state change handlers
    this._setupStateHandlers();

    // Start the render loop
    this._sceneManager.startRenderLoop();

    // Begin loading assets
    this._loadingScreen.startLoading();

    console.log('[App] Application initialized');
  }

  /**
   * Setup handlers for game state transitions
   */
  private _setupStateHandlers(): void {
    this._sceneManager.onStateChange(GameState.MAIN_MENU, () => {
      console.log('[App] Entered MAIN_MENU state');
      // Here you would show the main menu UI
      // For now, we just log the transition
    });

    this._sceneManager.onStateChange(GameState.PLAYING, () => {
      console.log('[App] Entered PLAYING state');
      // Here you would initialize gameplay
    });
  }

  /**
   * Get the scene manager instance
   */
  get sceneManager(): SceneManager {
    return this._sceneManager;
  }

  /**
   * Get the loading screen instance
   */
  get loadingScreen(): LoadingScreen {
    return this._loadingScreen;
  }
}

// Initialize the application when the DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  new App();
});

export { App };
