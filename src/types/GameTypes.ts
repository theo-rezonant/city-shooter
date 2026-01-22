/**
 * Game state machine states
 */
export enum GameState {
  LOADING = 'LOADING',
  MAIN_MENU = 'MAIN_MENU',
  GAMEPLAY = 'GAMEPLAY',
  PAUSED = 'PAUSED',
  ERROR = 'ERROR',
}

/**
 * Asset loading status for individual assets
 */
export enum AssetLoadStatus {
  PENDING = 'PENDING',
  LOADING = 'LOADING',
  LOADED = 'LOADED',
  VALIDATED = 'VALIDATED',
  FAILED = 'FAILED',
}

/**
 * Pointer lock state
 */
export enum PointerLockState {
  UNLOCKED = 'UNLOCKED',
  LOCKED = 'LOCKED',
  ERROR = 'ERROR',
  REQUESTING = 'REQUESTING',
}

/**
 * Asset validation result
 */
export interface AssetValidationResult {
  assetName: string;
  isValid: boolean;
  meshCount: number;
  hasMaterials: boolean;
  hasTextures: boolean;
  errors: string[];
}

/**
 * Overall assets validation status
 */
export interface AssetsValidationStatus {
  townLoaded: boolean;
  townValidated: boolean;
  laserGunLoaded: boolean;
  laserGunValidated: boolean;
  soldierLoaded: boolean;
  soldierValidated: boolean;
  allAssetsReady: boolean;
  validationResults: AssetValidationResult[];
}

/**
 * Pointer lock error details
 */
export interface PointerLockError {
  type: 'user_activation' | 'browser_denied' | 'hardware_error' | 'unknown';
  message: string;
  timestamp: number;
}

/**
 * Asset definition for the loader
 */
export interface AssetDefinition {
  name: string;
  rootUrl: string;
  sceneFilename: string;
  isRequired: boolean;
}

/**
 * Event callbacks for the PointerLockManager
 */
export interface PointerLockCallbacks {
  onLocked?: () => void;
  onUnlocked?: () => void;
  onError?: (error: PointerLockError) => void;
}

/**
 * Event callbacks for the AssetsManager
 */
export interface AssetsManagerCallbacks {
  onProgress?: (loaded: number, total: number, assetName: string) => void;
  onAssetLoaded?: (assetName: string, result: AssetValidationResult) => void;
  onAllAssetsReady?: (status: AssetsValidationStatus) => void;
  onAssetError?: (assetName: string, error: Error) => void;
}
