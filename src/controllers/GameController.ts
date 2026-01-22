import { Engine, Scene, UniversalCamera } from '@babylonjs/core';
import { AdvancedDynamicTexture, Button, Control } from '@babylonjs/gui';
import { AudioManager } from '../managers/AudioManager';
import { PointerLockController } from './PointerLockController';
import { GameState } from '../states/GameState';
import { ISceneManager, StateChangeCallback } from '../core/types';

/**
 * GameController coordinates the AudioManager and PointerLockController
 * to handle game initialization, state transitions, and user interactions.
 *
 * This controller:
 * - Links the "Enter Game" button to unlock audio and request pointer lock
 * - Manages state transitions based on pointer lock changes
 * - Ensures browser security policies are satisfied
 *
 * @example
 * ```typescript
 * const controller = new GameController(engine, canvas);
 * controller.initialize(scene, camera);
 *
 * // Create the Enter Game button
 * controller.createEnterGameButton();
 * ```
 */
export class GameController implements ISceneManager {
  private _engine: Engine;
  private _canvas: HTMLCanvasElement;
  private _scene: Scene | null = null;
  private _camera: UniversalCamera | null = null;
  private _audioManager: AudioManager | null = null;
  private _pointerLockController: PointerLockController;
  private _currentState: GameState = GameState.LOADING;
  private _stateChangeCallbacks: StateChangeCallback[] = [];
  private _guiTexture: AdvancedDynamicTexture | null = null;
  private _enterGameButton: Button | null = null;
  private _pauseOverlay: Control | null = null;

  /**
   * Creates a new GameController instance.
   *
   * @param engine - The Babylon.js engine
   * @param canvas - The canvas element
   */
  constructor(engine: Engine, canvas: HTMLCanvasElement) {
    this._engine = engine;
    this._canvas = canvas;

    // Create the pointer lock controller
    this._pointerLockController = new PointerLockController(canvas, this);

    // Listen for pointer lock changes
    this._pointerLockController.onLockChange((isLocked) => {
      this._handlePointerLockChange(isLocked);
    });
  }

  /**
   * Gets the AudioManager instance.
   */
  public get audioManager(): AudioManager | null {
    return this._audioManager;
  }

  /**
   * Gets the PointerLockController instance.
   */
  public get pointerLockController(): PointerLockController {
    return this._pointerLockController;
  }

  /**
   * Gets the GUI texture.
   */
  public get guiTexture(): AdvancedDynamicTexture | null {
    return this._guiTexture;
  }

  /**
   * Initializes the controller with a scene and camera.
   *
   * @param scene - The Babylon.js scene
   * @param camera - The UniversalCamera for the player
   */
  public initialize(scene: Scene, camera: UniversalCamera): void {
    this._scene = scene;
    this._camera = camera;

    // Create the AudioManager
    this._audioManager = new AudioManager({
      engine: this._engine,
      scene: this._scene,
      camera: this._camera,
    });

    // Attach the camera to the pointer lock controller
    this._pointerLockController.attachCamera(this._camera);

    // Create the GUI
    this._createGUI();

    // Transition to main menu
    this.transitionTo(GameState.MAIN_MENU);

    console.log('GameController: Initialized');
  }

  /**
   * Creates the "Enter Game" button that unlocks audio and requests pointer lock.
   * This is the critical integration point that satisfies browser security policies.
   */
  public createEnterGameButton(): Button {
    if (!this._guiTexture) {
      throw new Error('GameController: GUI not initialized. Call initialize() first.');
    }

    // Create the Enter Game button
    const button = Button.CreateSimpleButton('enterGameBtn', 'Enter Game');
    button.width = '200px';
    button.height = '60px';
    button.color = 'white';
    button.background = '#2d5a27';
    button.cornerRadius = 10;
    button.fontSize = 24;
    button.fontWeight = 'bold';
    button.hoverCursor = 'pointer';

    // Center the button
    button.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_CENTER;
    button.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;

    // CRITICAL: Handle button click - this is a user gesture
    // Both audio unlock and pointer lock must be called here
    button.onPointerUpObservable.add(async () => {
      await this._handleEnterGameClick();
    });

    // Add hover effects
    button.onPointerEnterObservable.add(() => {
      button.background = '#3d7a37';
    });

    button.onPointerOutObservable.add(() => {
      button.background = '#2d5a27';
    });

    this._guiTexture.addControl(button);
    this._enterGameButton = button;

    return button;
  }

  /**
   * Creates a pause overlay shown when the game is paused.
   */
  public createPauseOverlay(): Control {
    if (!this._guiTexture) {
      throw new Error('GameController: GUI not initialized. Call initialize() first.');
    }

    const pauseText = Button.CreateSimpleButton(
      'pauseText',
      'Game Paused\n\nClick to Resume'
    );
    pauseText.width = '100%';
    pauseText.height = '100%';
    pauseText.background = 'rgba(0, 0, 0, 0.7)';
    pauseText.color = 'white';
    pauseText.fontSize = 32;
    pauseText.isVisible = false;

    // Click to resume
    pauseText.onPointerUpObservable.add(async () => {
      await this._handleResumeClick();
    });

    this._guiTexture.addControl(pauseText);
    this._pauseOverlay = pauseText;

    return pauseText;
  }

  // ISceneManager implementation

  /**
   * Gets the current game state.
   */
  public getCurrentState(): GameState {
    return this._currentState;
  }

  /**
   * Transitions to a new game state.
   *
   * @param state - The target state
   */
  public transitionTo(state: GameState): void {
    if (state === this._currentState) {
      return;
    }

    const previousState = this._currentState;
    console.log(`GameController: Transitioning from ${previousState} to ${state}`);

    // Exit current state
    this._exitState(previousState);

    // Update current state
    this._currentState = state;

    // Enter new state
    this._enterState(state);

    // Notify listeners
    this._notifyStateChange(state, previousState);
  }

  /**
   * Subscribes to state change events.
   *
   * @param callback - Function to call when state changes
   */
  public onStateChange(callback: StateChangeCallback): void {
    this._stateChangeCallbacks.push(callback);
  }

  /**
   * Gets the active camera.
   */
  public getActiveCamera(): UniversalCamera | null {
    return this._camera;
  }

  /**
   * Gets the current scene.
   */
  public getScene(): Scene | null {
    return this._scene;
  }

  /**
   * Gets the engine.
   */
  public getEngine(): Engine | null {
    return this._engine;
  }

  /**
   * Disposes all resources.
   */
  public dispose(): void {
    this._pointerLockController.dispose();
    this._audioManager?.dispose();
    this._guiTexture?.dispose();

    this._scene = null;
    this._camera = null;
    this._stateChangeCallbacks = [];
  }

  /**
   * Creates the GUI texture.
   */
  private _createGUI(): void {
    if (!this._scene) return;

    this._guiTexture = AdvancedDynamicTexture.CreateFullscreenUI(
      'gameUI',
      true,
      this._scene
    );
  }

  /**
   * Handles the "Enter Game" button click.
   * This is a user gesture, so both audio unlock and pointer lock can be requested.
   */
  private async _handleEnterGameClick(): Promise<void> {
    console.log('GameController: Enter Game clicked');

    try {
      // 1. Unlock the audio context (must be in user gesture)
      if (this._audioManager) {
        await this._audioManager.unlock();
      }

      // 2. Request pointer lock (must be in user gesture)
      await this._pointerLockController.requestLock();

      // Note: State transition to GAMEPLAY will happen automatically
      // when pointer lock is acquired (handled in PointerLockController)
    } catch (error) {
      console.error('GameController: Failed to enter game', error);
    }
  }

  /**
   * Handles the resume click when paused.
   */
  private async _handleResumeClick(): Promise<void> {
    console.log('GameController: Resume clicked');

    try {
      await this._pointerLockController.requestLock();
    } catch (error) {
      console.error('GameController: Failed to resume', error);
    }
  }

  /**
   * Handles pointer lock state changes.
   */
  private _handlePointerLockChange(_isLocked: boolean): void {
    // UI updates based on lock state are handled in state transitions
    // This is called by the PointerLockController which already triggers state changes
  }

  /**
   * Exits the current state, performing cleanup.
   */
  private _exitState(state: GameState): void {
    switch (state) {
      case GameState.MAIN_MENU:
        // Hide main menu UI
        if (this._enterGameButton) {
          this._enterGameButton.isVisible = false;
        }
        break;

      case GameState.GAMEPLAY:
        // Pause sounds when leaving gameplay
        this._audioManager?.pauseAllSounds();
        break;

      case GameState.PAUSED:
        // Hide pause overlay
        if (this._pauseOverlay) {
          this._pauseOverlay.isVisible = false;
        }
        break;
    }
  }

  /**
   * Enters a new state, performing setup.
   */
  private _enterState(state: GameState): void {
    switch (state) {
      case GameState.LOADING:
        // Show loading UI
        break;

      case GameState.MAIN_MENU:
        // Show main menu UI
        if (this._enterGameButton) {
          this._enterGameButton.isVisible = true;
        }
        // Ensure pointer lock is released
        this._pointerLockController.releaseLock();
        break;

      case GameState.GAMEPLAY:
        // Hide all UI overlays
        if (this._enterGameButton) {
          this._enterGameButton.isVisible = false;
        }
        if (this._pauseOverlay) {
          this._pauseOverlay.isVisible = false;
        }
        break;

      case GameState.PAUSED:
        // Show pause overlay
        if (this._pauseOverlay) {
          this._pauseOverlay.isVisible = true;
        }
        break;
    }
  }

  /**
   * Notifies all state change listeners.
   */
  private _notifyStateChange(newState: GameState, previousState: GameState): void {
    this._stateChangeCallbacks.forEach((callback) => {
      try {
        callback(newState, previousState);
      } catch (error) {
        console.error('GameController: Error in state change callback', error);
      }
    });
  }
}
