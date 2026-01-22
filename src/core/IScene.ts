import { Scene } from '@babylonjs/core';
import type { App } from './App';
import type { AppState } from './AppState';

/**
 * Interface that all game scenes must implement.
 * Ensures consistent lifecycle management across all scene types.
 */
export interface IScene {
  /** The Babylon.js Scene instance managed by this scene */
  readonly scene: Scene;

  /** The state this scene represents */
  readonly state: AppState;

  /**
   * Initialize the scene - set up cameras, lights, meshes, GUI, etc.
   * Called once when the scene is created.
   * @returns A promise that resolves when initialization is complete
   */
  init(): Promise<void>;

  /**
   * Update loop called every frame.
   * Use for game logic that needs to run continuously.
   * @param deltaTime Time elapsed since last frame in milliseconds
   */
  update(deltaTime: number): void;

  /**
   * Dispose of all resources associated with this scene.
   * Must clean up:
   * - Event listeners (keyboard, mouse, window resize)
   * - GUI textures
   * - Physics impostors
   * - Custom meshes and materials
   * - Any registered observables
   */
  dispose(): void;
}

/**
 * Abstract base class for scene implementations.
 * Provides common functionality and ensures proper scene lifecycle.
 */
export abstract class BaseScene implements IScene {
  /** The Babylon.js Scene instance */
  public readonly scene: Scene;

  /** Reference to the main App for state transitions */
  protected readonly app: App;

  /** Event listeners to clean up on dispose */
  protected eventListeners: Array<{
    target: EventTarget;
    type: string;
    listener: EventListenerOrEventListenerObject;
  }> = [];

  /**
   * The state this scene represents.
   * Must be implemented by derived classes.
   */
  abstract readonly state: AppState;

  constructor(app: App) {
    this.app = app;
    this.scene = new Scene(app.engine);
  }

  /**
   * Initialize the scene.
   * Override in derived classes to set up scene-specific content.
   */
  abstract init(): Promise<void>;

  /**
   * Update loop called every frame.
   * Override in derived classes for scene-specific game logic.
   */
  abstract update(deltaTime: number): void;

  /**
   * Add an event listener that will be automatically cleaned up on dispose.
   * @param target The event target (window, document, canvas, etc.)
   * @param type The event type (e.g., 'keydown', 'resize')
   * @param listener The event listener function
   */
  protected addEventListener(
    target: EventTarget,
    type: string,
    listener: EventListenerOrEventListenerObject
  ): void {
    target.addEventListener(type, listener);
    this.eventListeners.push({ target, type, listener });
  }

  /**
   * Dispose of all resources associated with this scene.
   * Derived classes should call super.dispose() after their own cleanup.
   */
  dispose(): void {
    // Clean up all registered event listeners
    for (const { target, type, listener } of this.eventListeners) {
      target.removeEventListener(type, listener);
    }
    this.eventListeners = [];

    // Dispose the Babylon.js scene (cleans up meshes, materials, textures, etc.)
    if (!this.scene.isDisposed) {
      this.scene.dispose();
    }
  }
}
