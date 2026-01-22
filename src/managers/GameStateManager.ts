import { Scene } from '@babylonjs/core/scene';
import { Engine } from '@babylonjs/core/Engines/engine';
import { Observable } from '@babylonjs/core/Misc/observable';
import { GameState, PointerLockState } from '../types/GameTypes';
import { PointerLockManager } from './PointerLockManager';
import { GameAssetsManager } from './GameAssetsManager';
import { GameUI } from '../ui/GameUI';

/**
 * GameStateManager - Orchestrates the game state machine
 *
 * States:
 * - LOADING: Assets are being loaded and validated
 * - MAIN_MENU: Assets ready, waiting for user to click Start
 * - GAMEPLAY: Active gameplay with pointer lock
 * - PAUSED: Pause menu (pointer lock released)
 * - ERROR: Error state (asset or pointer lock failure)
 *
 * This manager integrates:
 * - PointerLockManager for mouse capture
 * - GameAssetsManager for asset loading/validation
 * - GameUI for visual feedback
 */
export class GameStateManager {
  private readonly _engine: Engine;
  private readonly _scene: Scene;
  private currentState: GameState = GameState.LOADING;

  // Sub-managers
  private pointerLockManager: PointerLockManager;
  private assetsManager: GameAssetsManager;
  private gameUI: GameUI;

  // State observables
  public readonly onStateChanged: Observable<GameState> = new Observable();

  // Keyboard listener for ESC handling
  private escKeyHandler: ((e: KeyboardEvent) => void) | null = null;

  constructor(engine: Engine, scene: Scene) {
    this._engine = engine;
    this._scene = scene;

    // Initialize managers
    this.pointerLockManager = new PointerLockManager(engine);
    this.assetsManager = new GameAssetsManager(scene);
    this.gameUI = new GameUI(scene);

    this.setupEventHandlers();
  }

  /**
   * Get the engine instance
   */
  public get engine(): Engine {
    return this._engine;
  }

  /**
   * Get the scene instance
   */
  public get scene(): Scene {
    return this._scene;
  }

  /**
   * Setup all event handlers between managers
   */
  private setupEventHandlers(): void {
    // Asset loading events
    this.assetsManager.onProgress.add((progress) => {
      this.gameUI.updateLoadingProgress(progress.loaded, progress.total, progress.current);
    });

    this.assetsManager.onAllAssetsReady.add((status) => {
      this.gameUI.updateFromAssetsStatus(status);
      if (status.allAssetsReady) {
        this.transitionTo(GameState.MAIN_MENU);
      } else {
        this.transitionTo(GameState.ERROR);
      }
    });

    this.assetsManager.onAssetError.add(({ assetName, error }) => {
      console.error(`Asset error [${assetName}]:`, error);
    });

    // Pointer lock events
    this.pointerLockManager.onStateChanged.add((state) => {
      this.handlePointerLockStateChange(state);
    });

    this.pointerLockManager.onErrorOccurred.add((error) => {
      this.gameUI.showPointerLockError(error);
      this.transitionTo(GameState.ERROR);
    });

    // UI events
    this.gameUI.onStartClicked.add(() => {
      this.handleStartClicked();
    });

    this.gameUI.onRetryClicked.add(() => {
      this.handleRetryClicked();
    });

    this.gameUI.onReloadAssetsClicked.add(() => {
      this.handleReloadAssets();
    });

    this.gameUI.onResumeClicked.add(() => {
      this.handleResumeClicked();
    });

    // ESC key for pausing
    this.setupEscHandler();
  }

  /**
   * Setup ESC key handler for pause functionality
   */
  private setupEscHandler(): void {
    this.escKeyHandler = (e: KeyboardEvent): void => {
      if (e.key === 'Escape') {
        if (this.currentState === GameState.GAMEPLAY) {
          // ESC during gameplay - pointer lock will be released by browser
          // The pointerlockchange event will trigger the pause state
          this.pointerLockManager.releaseLock();
        } else if (this.currentState === GameState.PAUSED) {
          // ESC during pause - do nothing (they should click Resume)
        }
      }
    };

    document.addEventListener('keydown', this.escKeyHandler);
  }

  /**
   * Handle pointer lock state changes
   */
  private handlePointerLockStateChange(state: PointerLockState): void {
    switch (state) {
      case PointerLockState.LOCKED:
        if (this.currentState === GameState.MAIN_MENU || this.currentState === GameState.PAUSED) {
          this.transitionTo(GameState.GAMEPLAY);
        }
        break;

      case PointerLockState.UNLOCKED:
        if (this.currentState === GameState.GAMEPLAY) {
          // User pressed ESC or clicked outside - show pause menu
          this.transitionTo(GameState.PAUSED);
        }
        break;

      case PointerLockState.ERROR:
        // Error already handled via onErrorOccurred
        break;
    }

    this.gameUI.updateFromPointerLockState(state);
  }

  /**
   * Handle Start button click
   * CRITICAL: This must request pointer lock immediately (user activation requirement)
   */
  private handleStartClicked(): void {
    if (!this.assetsManager.areAllAssetsReady()) {
      console.warn('Cannot start: assets not ready');
      return;
    }

    // Request pointer lock IMMEDIATELY within the user gesture context
    // This is critical for browsers to accept the request
    this.pointerLockManager.requestLock().catch((error) => {
      console.error('Pointer lock request failed:', error);
      // Error will be handled by onErrorOccurred
    });
  }

  /**
   * Handle Retry button click (for pointer lock errors)
   */
  private handleRetryClicked(): void {
    this.pointerLockManager.clearError();
    this.pointerLockManager.resetRetries();
    this.transitionTo(GameState.MAIN_MENU);
  }

  /**
   * Handle Resume button click
   */
  private handleResumeClicked(): void {
    // Request pointer lock again within user gesture
    this.pointerLockManager.requestLock().catch((error) => {
      console.error('Pointer lock request failed on resume:', error);
    });
  }

  /**
   * Handle reload assets request
   */
  private async handleReloadAssets(): Promise<void> {
    this.transitionTo(GameState.LOADING);
    this.gameUI.showLoadingScreen();

    const failedAssets = this.assetsManager.getFailedAssets();
    for (const assetName of failedAssets) {
      await this.assetsManager.reloadAsset(assetName);
    }

    const status = this.assetsManager.getValidationStatus();
    this.gameUI.updateFromAssetsStatus(status);

    if (status.allAssetsReady) {
      this.transitionTo(GameState.MAIN_MENU);
    } else {
      this.transitionTo(GameState.ERROR);
    }
  }

  /**
   * Transition to a new game state
   */
  private transitionTo(newState: GameState): void {
    const oldState = this.currentState;
    this.currentState = newState;

    console.log(`State transition: ${oldState} -> ${newState}`);

    // Update UI based on new state
    switch (newState) {
      case GameState.LOADING:
        this.gameUI.showLoadingScreen();
        break;
      case GameState.MAIN_MENU:
        this.gameUI.showMainMenu();
        break;
      case GameState.GAMEPLAY:
        this.gameUI.enterGameplay();
        break;
      case GameState.PAUSED:
        this.gameUI.showPauseMenu();
        break;
      case GameState.ERROR:
        // Error panel shown by specific error handlers
        break;
    }

    this.onStateChanged.notifyObservers(newState);
  }

  /**
   * Start the game initialization
   */
  public async initialize(): Promise<void> {
    // Check pointer lock support
    if (!PointerLockManager.isSupported()) {
      this.gameUI.showError(
        'Your browser does not support pointer lock. Please use a modern browser like Chrome or Firefox.',
        false
      );
      this.transitionTo(GameState.ERROR);
      return;
    }

    // Start loading assets
    this.transitionTo(GameState.LOADING);
    await this.assetsManager.loadAllAssets();
  }

  /**
   * Get current game state
   */
  public getCurrentState(): GameState {
    return this.currentState;
  }

  /**
   * Get the pointer lock manager for external use
   */
  public getPointerLockManager(): PointerLockManager {
    return this.pointerLockManager;
  }

  /**
   * Get the assets manager for external use
   */
  public getAssetsManager(): GameAssetsManager {
    return this.assetsManager;
  }

  /**
   * Get the game UI for external use
   */
  public getGameUI(): GameUI {
    return this.gameUI;
  }

  /**
   * Check if the game is ready to play
   */
  public isReadyToPlay(): boolean {
    return (
      this.assetsManager.areAllAssetsReady() &&
      (this.currentState === GameState.MAIN_MENU ||
        this.currentState === GameState.GAMEPLAY ||
        this.currentState === GameState.PAUSED)
    );
  }

  /**
   * Force pause the game (for testing or external control)
   */
  public pause(): void {
    if (this.currentState === GameState.GAMEPLAY) {
      this.pointerLockManager.releaseLock();
    }
  }

  /**
   * Dispose all resources
   */
  public dispose(): void {
    if (this.escKeyHandler) {
      document.removeEventListener('keydown', this.escKeyHandler);
    }

    this.pointerLockManager.dispose();
    this.assetsManager.dispose();
    this.gameUI.dispose();
    this.onStateChanged.clear();
  }
}
