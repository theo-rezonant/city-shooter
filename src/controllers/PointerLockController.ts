import { UniversalCamera } from '@babylonjs/core';
import { GameState } from '../states/GameState';
import { ISceneManager, PointerLockChangeCallback } from '../core/types';

/**
 * PointerLockController manages FPS mouse capture using the Pointer Lock API.
 *
 * It handles:
 * - Requesting and releasing pointer lock based on game state
 * - Detecting browser-initiated pointer lock releases (Esc key)
 * - Transitioning game state when pointer lock changes
 * - Connecting/disconnecting camera input based on lock state
 *
 * Important: Pointer lock requests must be made from within a user gesture (click event)
 * to satisfy browser security requirements. Do not request pointer lock automatically.
 *
 * @example
 * ```typescript
 * const controller = new PointerLockController(canvas, sceneManager);
 *
 * // In a button click handler:
 * enterGameButton.onPointerUpObservable.add(() => {
 *   controller.requestLock();
 * });
 *
 * // The controller automatically handles:
 * // - Esc key releasing the lock
 * // - Transitioning to MAIN_MENU/PAUSED state on lock release
 * ```
 */
export class PointerLockController {
  private _canvas: HTMLCanvasElement;
  private _sceneManager: ISceneManager | null;
  private _camera: UniversalCamera | null = null;
  private _isLocked: boolean = false;
  private _listeners: PointerLockChangeCallback[] = [];
  private _boundHandleLockChange: () => void;
  private _boundHandleLockError: () => void;

  /**
   * Creates a new PointerLockController instance.
   *
   * @param canvas - The canvas element to request pointer lock on
   * @param sceneManager - Optional scene manager for state transitions
   */
  constructor(canvas: HTMLCanvasElement, sceneManager: ISceneManager | null = null) {
    this._canvas = canvas;
    this._sceneManager = sceneManager;

    // Bind event handlers
    this._boundHandleLockChange = this._handleLockChange.bind(this);
    this._boundHandleLockError = this._handleLockError.bind(this);

    // Set up pointer lock event listeners
    this._setupEventListeners();
  }

  /**
   * Gets whether pointer lock is currently active.
   */
  public get isLocked(): boolean {
    return this._isLocked;
  }

  /**
   * Gets the canvas element.
   */
  public get canvas(): HTMLCanvasElement {
    return this._canvas;
  }

  /**
   * Sets the scene manager for state transitions.
   *
   * @param sceneManager - The scene manager instance
   */
  public setSceneManager(sceneManager: ISceneManager): void {
    this._sceneManager = sceneManager;
  }

  /**
   * Attaches the controller to a camera for input management.
   *
   * @param camera - The UniversalCamera to control
   */
  public attachCamera(camera: UniversalCamera): void {
    this._camera = camera;
  }

  /**
   * Detaches the current camera.
   */
  public detachCamera(): void {
    if (this._camera) {
      this._disableCameraInput();
    }
    this._camera = null;
  }

  /**
   * Requests pointer lock on the canvas.
   * Must be called from within a user gesture (click, touch, etc.) to satisfy
   * browser security requirements.
   *
   * @returns Promise that resolves when lock is acquired or rejects on error
   */
  public async requestLock(): Promise<void> {
    if (this._isLocked) {
      console.log('PointerLockController: Already locked');
      return;
    }

    try {
      // Request pointer lock - must be called from user gesture
      await this._canvas.requestPointerLock();
      console.log('PointerLockController: Pointer lock requested');
    } catch (error) {
      console.error('PointerLockController: Failed to request pointer lock', error);
      throw error;
    }
  }

  /**
   * Releases pointer lock if currently held.
   */
  public releaseLock(): void {
    if (document.pointerLockElement === this._canvas) {
      document.exitPointerLock();
      console.log('PointerLockController: Pointer lock released');
    }
  }

  /**
   * Subscribes to pointer lock change events.
   *
   * @param callback - Function to call when lock state changes
   * @returns Unsubscribe function
   */
  public onLockChange(callback: PointerLockChangeCallback): () => void {
    this._listeners.push(callback);
    return () => {
      const index = this._listeners.indexOf(callback);
      if (index > -1) {
        this._listeners.splice(index, 1);
      }
    };
  }

  /**
   * Cleans up event listeners and releases resources.
   */
  public dispose(): void {
    // Remove event listeners
    document.removeEventListener('pointerlockchange', this._boundHandleLockChange);
    document.removeEventListener('pointerlockerror', this._boundHandleLockError);

    // Release lock if held
    this.releaseLock();

    // Detach camera
    this.detachCamera();

    // Clear listeners
    this._listeners = [];
  }

  /**
   * Sets up the pointer lock event listeners on the document.
   */
  private _setupEventListeners(): void {
    document.addEventListener('pointerlockchange', this._boundHandleLockChange);
    document.addEventListener('pointerlockerror', this._boundHandleLockError);
  }

  /**
   * Handles pointer lock state changes.
   * Called by browser when lock is acquired or released (including Esc key).
   */
  private _handleLockChange(): void {
    const wasLocked = this._isLocked;
    this._isLocked = document.pointerLockElement === this._canvas;

    console.log(`PointerLockController: Lock state changed - locked: ${this._isLocked}`);

    if (this._isLocked && !wasLocked) {
      // Lock acquired
      this._onLockAcquired();
    } else if (!this._isLocked && wasLocked) {
      // Lock released (user pressed Esc or programmatic release)
      this._onLockReleased();
    }

    // Notify listeners
    this._notifyListeners();
  }

  /**
   * Handles pointer lock errors.
   */
  private _handleLockError(): void {
    console.error('PointerLockController: Pointer lock error occurred');
    this._isLocked = false;
    this._notifyListeners();
  }

  /**
   * Called when pointer lock is acquired.
   */
  private _onLockAcquired(): void {
    console.log('PointerLockController: Lock acquired');

    // Enable camera input
    this._enableCameraInput();

    // Transition to gameplay state if scene manager is available
    if (this._sceneManager) {
      const currentState = this._sceneManager.getCurrentState();
      if (currentState !== GameState.GAMEPLAY) {
        this._sceneManager.transitionTo(GameState.GAMEPLAY);
      }
    }
  }

  /**
   * Called when pointer lock is released.
   * This can happen when user presses Esc or programmatically.
   */
  private _onLockReleased(): void {
    console.log('PointerLockController: Lock released');

    // Disable camera input
    this._disableCameraInput();

    // Transition to appropriate state if scene manager is available
    if (this._sceneManager) {
      const currentState = this._sceneManager.getCurrentState();
      if (currentState === GameState.GAMEPLAY) {
        // Transition to pause or main menu when lock is released during gameplay
        this._sceneManager.transitionTo(GameState.PAUSED);
      }
    }
  }

  /**
   * Enables camera mouse input.
   */
  private _enableCameraInput(): void {
    if (this._camera) {
      // Attach control to the canvas for mouse input
      this._camera.attachControl(this._canvas, true);
      console.log('PointerLockController: Camera input enabled');
    }
  }

  /**
   * Disables camera mouse input.
   */
  private _disableCameraInput(): void {
    if (this._camera) {
      // Detach control from the canvas
      this._camera.detachControl();
      console.log('PointerLockController: Camera input disabled');
    }
  }

  /**
   * Notifies all listeners of lock state change.
   */
  private _notifyListeners(): void {
    this._listeners.forEach((callback) => {
      try {
        callback(this._isLocked);
      } catch (error) {
        console.error('PointerLockController: Error in listener callback', error);
      }
    });
  }
}
