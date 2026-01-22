import { Engine, Scene } from '@babylonjs/core';
import HavokPhysics from '@babylonjs/havok';
import { HavokPlugin } from '@babylonjs/core/Physics/v2/Plugins/havokPlugin';
import { AppState } from './AppState';
import type { IScene } from './IScene';
import { LoadingScene } from '../scenes/LoadingScene';
import { MainMenuScene } from '../scenes/MainMenuScene';
import { GamePlayScene } from '../scenes/GamePlayScene';
import { GameOverScene } from '../scenes/GameOverScene';

/**
 * Configuration options for the App.
 */
export interface AppConfig {
  /** The canvas element to render to */
  canvas: HTMLCanvasElement;
  /** Whether to enable anti-aliasing (default: true) */
  antialias?: boolean;
  /** Whether to enable the Havok physics engine (default: true) */
  enablePhysics?: boolean;
}

/**
 * Central application class that manages the Babylon.js Engine,
 * the current active Scene, and state transitions between game scenes.
 *
 * This class implements a state machine pattern to control the flow
 * between Loading, Main Menu, Gameplay, and Game Over states.
 */
export class App {
  /** The Babylon.js rendering engine */
  public readonly engine: Engine;

  /** The canvas element being rendered to */
  public readonly canvas: HTMLCanvasElement;

  /** The current active scene implementation */
  private activeScene: IScene | null = null;

  /** The current application state */
  private currentState: AppState | null = null;

  /** The Havok physics plugin instance (shared across scenes that need physics) */
  private havokPlugin: HavokPlugin | null = null;

  /** Whether the app has been initialized */
  private initialized = false;

  /** Whether the app is currently transitioning between states */
  private transitioning = false;

  /** Callback for when state changes */
  private onStateChangeCallbacks: Array<(newState: AppState, oldState: AppState | null) => void> =
    [];

  constructor(config: AppConfig) {
    this.canvas = config.canvas;

    // Create the Babylon.js engine
    this.engine = new Engine(this.canvas, config.antialias ?? true, {
      preserveDrawingBuffer: true,
      stencil: true,
      disableWebGL2Support: false,
    });

    // Handle window resize
    this.setupResizeHandler();
  }

  /**
   * Initialize the application.
   * This must be called before starting the render loop.
   * Optionally initializes the Havok physics engine.
   *
   * @param enablePhysics Whether to initialize Havok physics (default: true)
   */
  async init(enablePhysics = true): Promise<void> {
    if (this.initialized) {
      console.warn('App.init() called multiple times');
      return;
    }

    // Initialize Havok physics if enabled
    if (enablePhysics) {
      try {
        const havokInstance = await HavokPhysics();
        this.havokPlugin = new HavokPlugin(true, havokInstance);
        console.log('Havok physics initialized successfully');
      } catch (error) {
        console.error('Failed to initialize Havok physics:', error);
        // Continue without physics - some scenes may not need it
      }
    }

    this.initialized = true;
  }

  /**
   * Get the current application state.
   */
  get state(): AppState | null {
    return this.currentState;
  }

  /**
   * Get the Havok physics plugin for use in scenes.
   */
  get physics(): HavokPlugin | null {
    return this.havokPlugin;
  }

  /**
   * Get the current active Babylon.js Scene.
   */
  get scene(): Scene | null {
    return this.activeScene?.scene ?? null;
  }

  /**
   * Check if the app is currently transitioning between states.
   */
  get isTransitioning(): boolean {
    return this.transitioning;
  }

  /**
   * Register a callback to be called when the state changes.
   * @param callback The callback function
   */
  onStateChange(callback: (newState: AppState, oldState: AppState | null) => void): void {
    this.onStateChangeCallbacks.push(callback);
  }

  /**
   * Remove a state change callback.
   * @param callback The callback to remove
   */
  offStateChange(callback: (newState: AppState, oldState: AppState | null) => void): void {
    const index = this.onStateChangeCallbacks.indexOf(callback);
    if (index !== -1) {
      this.onStateChangeCallbacks.splice(index, 1);
    }
  }

  /**
   * Transition to a new application state.
   * This will:
   * 1. Dispose the current scene (if any)
   * 2. Create and initialize the new scene
   * 3. Start rendering the new scene
   *
   * @param newState The state to transition to
   */
  async goToState(newState: AppState): Promise<void> {
    if (!this.initialized) {
      throw new Error('App must be initialized before changing state. Call init() first.');
    }

    if (this.transitioning) {
      console.warn('State transition already in progress, ignoring request');
      return;
    }

    if (this.currentState === newState) {
      console.warn(`Already in state ${newState}`);
      return;
    }

    this.transitioning = true;
    const oldState = this.currentState;

    try {
      console.log(`Transitioning from ${oldState ?? 'null'} to ${newState}`);

      // Dispose the current scene
      if (this.activeScene) {
        console.log(`Disposing scene for state ${oldState}`);
        this.activeScene.dispose();
        this.activeScene = null;
      }

      // Create the new scene
      this.activeScene = this.createSceneForState(newState);
      this.currentState = newState;

      // Initialize the new scene
      console.log(`Initializing scene for state ${newState}`);
      await this.activeScene.init();

      // Notify listeners
      for (const callback of this.onStateChangeCallbacks) {
        try {
          callback(newState, oldState);
        } catch (error) {
          console.error('Error in state change callback:', error);
        }
      }

      console.log(`Successfully transitioned to ${newState}`);
    } catch (error) {
      console.error(`Failed to transition to state ${newState}:`, error);
      throw error;
    } finally {
      this.transitioning = false;
    }
  }

  /**
   * Start the application by transitioning to the LOADING state
   * and beginning the render loop.
   */
  async start(): Promise<void> {
    if (!this.initialized) {
      await this.init();
    }

    // Start with the LOADING state
    await this.goToState(AppState.LOADING);

    // Start the render loop
    this.startRenderLoop();
  }

  /**
   * Start the render loop.
   * This continuously renders the active scene and calls its update method.
   */
  private startRenderLoop(): void {
    let lastTime = performance.now();

    this.engine.runRenderLoop(() => {
      const currentTime = performance.now();
      const deltaTime = currentTime - lastTime;
      lastTime = currentTime;

      if (this.activeScene && !this.transitioning) {
        // Call the scene's update method
        this.activeScene.update(deltaTime);

        // Render the scene
        this.activeScene.scene.render();
      }
    });
  }

  /**
   * Set up the window resize handler to keep the engine responsive.
   */
  private setupResizeHandler(): void {
    const resizeHandler = (): void => {
      this.engine.resize();
    };

    window.addEventListener('resize', resizeHandler);
  }

  /**
   * Create the appropriate scene implementation for a given state.
   * @param state The application state
   * @returns The scene implementation for that state
   */
  private createSceneForState(state: AppState): IScene {
    switch (state) {
      case AppState.LOADING:
        return new LoadingScene(this);
      case AppState.MAIN_MENU:
        return new MainMenuScene(this);
      case AppState.GAME_PLAY:
        return new GamePlayScene(this);
      case AppState.GAME_OVER:
        return new GameOverScene(this);
      default:
        throw new Error(`Unknown state: ${state}`);
    }
  }

  /**
   * Unlock the audio engine (required due to browser autoplay policies).
   * This should be called in response to a user gesture (e.g., button click).
   */
  unlockAudio(): void {
    if (Engine.audioEngine) {
      Engine.audioEngine.unlock();
      console.log('Audio engine unlocked');
    }
  }

  /**
   * Request pointer lock on the canvas.
   * This should be called in response to a user gesture.
   * @returns Promise that resolves when pointer lock is acquired or rejects if it fails
   */
  requestPointerLock(): Promise<void> {
    return new Promise((resolve, reject) => {
      const onPointerLockChange = (): void => {
        if (document.pointerLockElement === this.canvas) {
          document.removeEventListener('pointerlockchange', onPointerLockChange);
          document.removeEventListener('pointerlockerror', onPointerLockError);
          resolve();
        }
      };

      const onPointerLockError = (): void => {
        document.removeEventListener('pointerlockchange', onPointerLockChange);
        document.removeEventListener('pointerlockerror', onPointerLockError);
        reject(new Error('Failed to acquire pointer lock'));
      };

      document.addEventListener('pointerlockchange', onPointerLockChange);
      document.addEventListener('pointerlockerror', onPointerLockError);

      this.canvas.requestPointerLock();
    });
  }

  /**
   * Exit pointer lock.
   */
  exitPointerLock(): void {
    if (document.pointerLockElement === this.canvas) {
      document.exitPointerLock();
    }
  }

  /**
   * Check if pointer lock is currently active.
   */
  get isPointerLocked(): boolean {
    return document.pointerLockElement === this.canvas;
  }

  /**
   * Dispose of the entire application and clean up all resources.
   */
  dispose(): void {
    // Dispose the active scene
    if (this.activeScene) {
      this.activeScene.dispose();
      this.activeScene = null;
    }

    // Dispose the physics plugin
    if (this.havokPlugin) {
      this.havokPlugin.dispose();
      this.havokPlugin = null;
    }

    // Dispose the engine
    this.engine.dispose();

    // Clear callbacks
    this.onStateChangeCallbacks = [];

    this.currentState = null;
    this.initialized = false;

    console.log('App disposed');
  }
}
