import { AbstractMesh } from '@babylonjs/core';

/**
 * Enum representing the different states of the game application.
 */
export enum GameState {
  /** Initial state while loading assets */
  LOADING = 'LOADING',
  /** Main menu state after assets are loaded */
  MAIN_MENU = 'MAIN_MENU',
  /** Active gameplay state */
  PLAYING = 'PLAYING',
  /** Paused state during gameplay */
  PAUSED = 'PAUSED',
}

/**
 * Interface for assets loaded by the game
 */
export interface LoadedAssets {
  map?: AbstractMesh[];
  gun?: AbstractMesh[];
  soldier?: AbstractMesh[];
}
