import { Engine } from '@babylonjs/core/Engines/engine';
import { Observable } from '@babylonjs/core/Misc/observable';
import { PointerLockState, PointerLockError, PointerLockCallbacks } from '../types/GameTypes';

/**
 * PointerLockManager - Robust management of the Pointer Lock API for FPS games
 *
 * This manager handles:
 * - User activation requirements (lock must be requested within user gesture)
 * - Browser-specific pointer lock errors
 * - State management and visual feedback coordination
 * - Proper cleanup during pause/menu states
 */
export class PointerLockManager {
  private engine: Engine;
  private state: PointerLockState = PointerLockState.UNLOCKED;
  private lastError: PointerLockError | null = null;
  private retryCount: number = 0;
  private readonly maxRetries: number = 3;
  private callbacks: PointerLockCallbacks;
  private lastUserGestureTime: number = 0;
  private readonly userGestureTimeout: number = 1000; // 1 second timeout for user activation

  // Observables for state changes
  public readonly onStateChanged: Observable<PointerLockState> = new Observable();
  public readonly onErrorOccurred: Observable<PointerLockError> = new Observable();

  constructor(engine: Engine, callbacks: PointerLockCallbacks = {}) {
    this.engine = engine;
    this.callbacks = callbacks;
    this.setupEventListeners();
  }

  /**
   * Setup native document event listeners for pointer lock changes and errors
   */
  private setupEventListeners(): void {
    // Listen for pointer lock state changes
    document.addEventListener('pointerlockchange', this.handlePointerLockChange);
    document.addEventListener('mozpointerlockchange', this.handlePointerLockChange);
    document.addEventListener('webkitpointerlockchange', this.handlePointerLockChange);

    // Listen for pointer lock errors
    document.addEventListener('pointerlockerror', this.handlePointerLockError);
    document.addEventListener('mozpointerlockerror', this.handlePointerLockError);
    document.addEventListener('webkitpointerlockerror', this.handlePointerLockError);
  }

  /**
   * Handler for pointer lock state changes
   */
  private handlePointerLockChange = (): void => {
    const canvas = this.engine.getRenderingCanvas();
    const isLocked =
      document.pointerLockElement === canvas ||
      (document as unknown as { mozPointerLockElement?: Element }).mozPointerLockElement ===
        canvas ||
      (document as unknown as { webkitPointerLockElement?: Element }).webkitPointerLockElement ===
        canvas;

    if (isLocked) {
      this.state = PointerLockState.LOCKED;
      this.retryCount = 0;
      this.lastError = null;
      this.onStateChanged.notifyObservers(this.state);
      this.callbacks.onLocked?.();
    } else {
      this.state = PointerLockState.UNLOCKED;
      this.onStateChanged.notifyObservers(this.state);
      this.callbacks.onUnlocked?.();
    }
  };

  /**
   * Handler for pointer lock errors
   * Categorizes the error type for appropriate user feedback
   */
  private handlePointerLockError = (): void => {
    const timeSinceGesture = Date.now() - this.lastUserGestureTime;

    let errorType: PointerLockError['type'];
    let message: string;

    if (timeSinceGesture > this.userGestureTimeout) {
      // User activation expired
      errorType = 'user_activation';
      message =
        'Pointer lock must be requested immediately after a user interaction. Please click the Start button again.';
    } else if (this.retryCount >= this.maxRetries) {
      // Hardware or browser settings preventing lock
      errorType = 'hardware_error';
      message =
        'Unable to capture mouse after multiple attempts. Please check your browser settings or try a different browser.';
    } else {
      // Browser denied for unknown reason
      errorType = 'browser_denied';
      message =
        'The browser denied the pointer lock request. Please try clicking the Start button.';
    }

    this.lastError = {
      type: errorType,
      message,
      timestamp: Date.now(),
    };

    this.state = PointerLockState.ERROR;
    this.retryCount++;

    this.onStateChanged.notifyObservers(this.state);
    this.onErrorOccurred.notifyObservers(this.lastError);
    this.callbacks.onError?.(this.lastError);
  };

  /**
   * Request pointer lock - MUST be called within a user gesture event handler
   * This method should be invoked directly from the Start button's click handler
   *
   * @returns Promise that resolves when lock is acquired or rejects on error
   */
  public async requestLock(): Promise<void> {
    this.lastUserGestureTime = Date.now();
    this.state = PointerLockState.REQUESTING;
    this.onStateChanged.notifyObservers(this.state);

    const canvas = this.engine.getRenderingCanvas();
    if (!canvas) {
      const error: PointerLockError = {
        type: 'unknown',
        message: 'No rendering canvas available for pointer lock',
        timestamp: Date.now(),
      };
      this.lastError = error;
      this.state = PointerLockState.ERROR;
      this.onErrorOccurred.notifyObservers(error);
      throw new Error(error.message);
    }

    return new Promise((resolve, reject) => {
      // Setup one-time listeners for this specific request
      const handleSuccess = (): void => {
        cleanup();
        resolve();
      };

      const handleError = (): void => {
        cleanup();
        reject(new Error(this.lastError?.message ?? 'Pointer lock failed'));
      };

      const cleanup = (): void => {
        document.removeEventListener('pointerlockchange', handleSuccess);
        document.removeEventListener('pointerlockerror', handleError);
      };

      document.addEventListener('pointerlockchange', handleSuccess, { once: true });
      document.addEventListener('pointerlockerror', handleError, { once: true });

      // Use Babylon's native wrapper - this handles engine-level camera state
      try {
        this.engine.enterPointerlock();
      } catch (e) {
        cleanup();
        const error: PointerLockError = {
          type: 'browser_denied',
          message: `Failed to request pointer lock: ${e instanceof Error ? e.message : 'Unknown error'}`,
          timestamp: Date.now(),
        };
        this.lastError = error;
        this.state = PointerLockState.ERROR;
        this.onErrorOccurred.notifyObservers(error);
        reject(new Error(error.message));
      }
    });
  }

  /**
   * Release pointer lock - call this when entering pause menu or escape state
   */
  public releaseLock(): void {
    if (this.state === PointerLockState.LOCKED) {
      document.exitPointerLock();
    }
  }

  /**
   * Check if pointer lock is currently active
   */
  public isLocked(): boolean {
    return this.state === PointerLockState.LOCKED;
  }

  /**
   * Get current pointer lock state
   */
  public getState(): PointerLockState {
    return this.state;
  }

  /**
   * Get the last error that occurred
   */
  public getLastError(): PointerLockError | null {
    return this.lastError;
  }

  /**
   * Clear the error state (after user has acknowledged)
   */
  public clearError(): void {
    this.lastError = null;
    if (this.state === PointerLockState.ERROR) {
      this.state = PointerLockState.UNLOCKED;
      this.onStateChanged.notifyObservers(this.state);
    }
  }

  /**
   * Reset retry counter (call this before a new game session)
   */
  public resetRetries(): void {
    this.retryCount = 0;
  }

  /**
   * Check if pointer lock is supported by the browser
   */
  public static isSupported(): boolean {
    return (
      'pointerLockElement' in document ||
      'mozPointerLockElement' in document ||
      'webkitPointerLockElement' in document
    );
  }

  /**
   * Cleanup event listeners when disposing
   */
  public dispose(): void {
    document.removeEventListener('pointerlockchange', this.handlePointerLockChange);
    document.removeEventListener('mozpointerlockchange', this.handlePointerLockChange);
    document.removeEventListener('webkitpointerlockchange', this.handlePointerLockChange);
    document.removeEventListener('pointerlockerror', this.handlePointerLockError);
    document.removeEventListener('mozpointerlockerror', this.handlePointerLockError);
    document.removeEventListener('webkitpointerlockerror', this.handlePointerLockError);

    this.onStateChanged.clear();
    this.onErrorOccurred.clear();
  }
}
