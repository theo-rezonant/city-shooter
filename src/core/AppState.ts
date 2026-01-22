/**
 * Enum representing the different application states.
 * The state machine transitions between these states during gameplay.
 */
export enum AppState {
  /** Initial loading state - loads assets before showing main menu */
  LOADING = 'LOADING',
  /** Main menu state - user can start game or change settings */
  MAIN_MENU = 'MAIN_MENU',
  /** Active gameplay state - the main game loop */
  GAME_PLAY = 'GAME_PLAY',
  /** Game over state - shown when player loses or wins */
  GAME_OVER = 'GAME_OVER',
}
