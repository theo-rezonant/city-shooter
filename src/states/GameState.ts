/**
 * Enum representing the different states of the game.
 */
export enum GameState {
  /** Initial loading state */
  LOADING = 'LOADING',
  /** Main menu state - pointer lock released */
  MAIN_MENU = 'MAIN_MENU',
  /** Active gameplay state - pointer lock engaged */
  GAMEPLAY = 'GAMEPLAY',
  /** Pause menu state - pointer lock released */
  PAUSED = 'PAUSED',
}
