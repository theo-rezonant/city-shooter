import { Engine, Scene, ArcRotateCamera, HemisphericLight, Vector3, Color4 } from '@babylonjs/core';
import { GameState, LoadedAssets } from '../types/GameState';

/**
 * SceneManager handles the Babylon.js engine, scene lifecycle, and application state transitions.
 * It serves as the central hub for managing game states and providing access to core engine components.
 */
export class SceneManager {
  private _engine: Engine;
  private _scene: Scene;
  private _canvas: HTMLCanvasElement;
  private _currentState: GameState = GameState.LOADING;
  private _loadedAssets: LoadedAssets = {};
  private _stateChangeCallbacks: Map<GameState, Array<() => void>> = new Map();

  constructor(canvas: HTMLCanvasElement) {
    this._canvas = canvas;
    this._engine = new Engine(canvas, true, {
      preserveDrawingBuffer: true,
      stencil: true,
    });
    this._scene = new Scene(this._engine);

    // Initialize basic scene setup
    this._initializeScene();

    // Handle window resize
    window.addEventListener('resize', () => {
      this._engine.resize();
    });
  }

  /**
   * Initialize the basic scene with camera and lighting
   */
  private _initializeScene(): void {
    // Create a basic camera for the loading screen
    const camera = new ArcRotateCamera(
      'loadingCamera',
      Math.PI / 2,
      Math.PI / 2,
      10,
      Vector3.Zero(),
      this._scene
    );
    camera.attachControl(this._canvas, false);

    // Create ambient lighting
    const light = new HemisphericLight('ambientLight', new Vector3(0, 1, 0), this._scene);
    light.intensity = 0.7;

    // Set scene background color
    this._scene.clearColor = new Color4(0.05, 0.05, 0.1, 1);
  }

  /**
   * Get the Babylon.js engine instance
   */
  get engine(): Engine {
    return this._engine;
  }

  /**
   * Get the current scene instance
   */
  get scene(): Scene {
    return this._scene;
  }

  /**
   * Get the canvas element
   */
  get canvas(): HTMLCanvasElement {
    return this._canvas;
  }

  /**
   * Get the current game state
   */
  get currentState(): GameState {
    return this._currentState;
  }

  /**
   * Get loaded assets
   */
  get loadedAssets(): LoadedAssets {
    return this._loadedAssets;
  }

  /**
   * Set loaded assets (called by LoadingScreen after loading completes)
   */
  setLoadedAssets(assets: LoadedAssets): void {
    this._loadedAssets = assets;
  }

  /**
   * Register a callback for when a specific state is entered
   */
  onStateChange(state: GameState, callback: () => void): void {
    if (!this._stateChangeCallbacks.has(state)) {
      this._stateChangeCallbacks.set(state, []);
    }
    this._stateChangeCallbacks.get(state)!.push(callback);
  }

  /**
   * Transition to a new game state
   */
  transitionTo(newState: GameState): void {
    console.log(`[SceneManager] Transitioning from ${this._currentState} to ${newState}`);
    this._currentState = newState;

    // Execute state change callbacks
    const callbacks = this._stateChangeCallbacks.get(newState);
    if (callbacks) {
      callbacks.forEach((cb) => cb());
    }
  }

  /**
   * Unlock the audio engine (must be called from user interaction)
   */
  unlockAudioEngine(): void {
    if (Engine.audioEngine) {
      Engine.audioEngine.unlock();
      console.log('[SceneManager] Audio engine unlocked');
    }
  }

  /**
   * Start the render loop
   */
  startRenderLoop(): void {
    this._engine.runRenderLoop(() => {
      this._scene.render();
    });
  }

  /**
   * Dispose of all resources
   */
  dispose(): void {
    this._scene.dispose();
    this._engine.dispose();
  }
}
