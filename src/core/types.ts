import { Vector3, UniversalCamera, Scene, Engine } from '@babylonjs/core';
import { GameState } from '../states/GameState';

/**
 * Configuration options for spatial audio sounds.
 */
export interface SpatialAudioOptions {
  /** The position in 3D space for the sound */
  position?: Vector3;
  /** Maximum distance at which the sound can be heard */
  maxDistance?: number;
  /** Minimum distance before the sound starts attenuating */
  refDistance?: number;
  /** How fast the sound falls off */
  rolloffFactor?: number;
  /** The panning model to use */
  panningModel?: 'HRTF' | 'equalpower';
  /** Volume of the sound (0-1) */
  volume?: number;
  /** Whether the sound should loop */
  loop?: boolean;
  /** Whether the sound should auto-play */
  autoplay?: boolean;
}

/**
 * Interface for audio manager configuration.
 */
export interface AudioManagerConfig {
  /** The Babylon.js engine instance */
  engine: Engine;
  /** The current scene */
  scene: Scene;
  /** The active camera to attach the audio listener to */
  camera?: UniversalCamera;
  /** Master volume (0-1) */
  masterVolume?: number;
}

/**
 * Event callback type for state changes.
 */
export type StateChangeCallback = (newState: GameState, previousState: GameState) => void;

/**
 * Event callback type for pointer lock changes.
 */
export type PointerLockChangeCallback = (isLocked: boolean) => void;

/**
 * Interface for scene manager to handle game state transitions.
 */
export interface ISceneManager {
  /** Get the current game state */
  getCurrentState(): GameState;
  /** Transition to a new game state */
  transitionTo(state: GameState): void;
  /** Subscribe to state change events */
  onStateChange(callback: StateChangeCallback): void;
  /** Get the active camera */
  getActiveCamera(): UniversalCamera | null;
  /** Get the current scene */
  getScene(): Scene | null;
  /** Get the engine */
  getEngine(): Engine | null;
}
