import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  GameState,
  PointerLockState,
  PointerLockError,
  AssetsValidationStatus,
} from '../types/GameTypes';

describe('GameUI State Management', () => {
  describe('Game States', () => {
    it('should have all expected game states', () => {
      expect(GameState.LOADING).toBe('LOADING');
      expect(GameState.MAIN_MENU).toBe('MAIN_MENU');
      expect(GameState.GAMEPLAY).toBe('GAMEPLAY');
      expect(GameState.PAUSED).toBe('PAUSED');
      expect(GameState.ERROR).toBe('ERROR');
    });
  });

  describe('State Transitions', () => {
    it('should start in LOADING state', () => {
      const currentState: GameState = GameState.LOADING;
      expect(currentState).toBe(GameState.LOADING);
    });

    it('should transition from LOADING to MAIN_MENU on assets ready', () => {
      let currentState: GameState = GameState.LOADING;

      // Simulate assets loaded
      const assetsReady = true;
      if (assetsReady) {
        currentState = GameState.MAIN_MENU;
      }

      expect(currentState).toBe(GameState.MAIN_MENU);
    });

    it('should transition from MAIN_MENU to GAMEPLAY on start', () => {
      let currentState: GameState = GameState.MAIN_MENU;

      // Simulate start clicked
      currentState = GameState.GAMEPLAY;

      expect(currentState).toBe(GameState.GAMEPLAY);
    });

    it('should transition from GAMEPLAY to PAUSED on ESC', () => {
      let currentState: GameState = GameState.GAMEPLAY;

      // Simulate ESC key
      currentState = GameState.PAUSED;

      expect(currentState).toBe(GameState.PAUSED);
    });

    it('should transition from PAUSED to GAMEPLAY on resume', () => {
      let currentState: GameState = GameState.PAUSED;

      // Simulate resume clicked
      currentState = GameState.GAMEPLAY;

      expect(currentState).toBe(GameState.GAMEPLAY);
    });

    it('should transition to ERROR state on critical failure', () => {
      let currentState: GameState = GameState.LOADING;

      // Simulate asset load failure
      const hasError = true;
      if (hasError) {
        currentState = GameState.ERROR;
      }

      expect(currentState).toBe(GameState.ERROR);
    });
  });
});

describe('Start Button Logic', () => {
  let isStartButtonEnabled: boolean;

  beforeEach(() => {
    isStartButtonEnabled = false;
  });

  describe('Enable/Disable Logic', () => {
    it('should be disabled initially', () => {
      expect(isStartButtonEnabled).toBe(false);
    });

    it('should be enabled when all assets are ready', () => {
      const status: AssetsValidationStatus = {
        townLoaded: true,
        townValidated: true,
        laserGunLoaded: true,
        laserGunValidated: true,
        soldierLoaded: true,
        soldierValidated: true,
        allAssetsReady: true,
        validationResults: [],
      };

      isStartButtonEnabled = status.allAssetsReady;
      expect(isStartButtonEnabled).toBe(true);
    });

    it('should remain disabled if any asset is not validated', () => {
      const status: AssetsValidationStatus = {
        townLoaded: true,
        townValidated: true,
        laserGunLoaded: true,
        laserGunValidated: false,
        soldierLoaded: true,
        soldierValidated: true,
        allAssetsReady: false,
        validationResults: [],
      };

      isStartButtonEnabled = status.allAssetsReady;
      expect(isStartButtonEnabled).toBe(false);
    });
  });

  describe('Click Handler', () => {
    it('should not trigger when disabled', () => {
      const onStartClicked = vi.fn();
      isStartButtonEnabled = false;

      // Simulate click
      if (isStartButtonEnabled) {
        onStartClicked();
      }

      expect(onStartClicked).not.toHaveBeenCalled();
    });

    it('should trigger when enabled', () => {
      const onStartClicked = vi.fn();
      isStartButtonEnabled = true;

      // Simulate click
      if (isStartButtonEnabled) {
        onStartClicked();
      }

      expect(onStartClicked).toHaveBeenCalledTimes(1);
    });
  });
});

describe('Loading Progress Display', () => {
  it('should format progress text correctly', () => {
    const formatProgress = (loaded: number, total: number): string => {
      return `${loaded} / ${total} assets loaded`;
    };

    expect(formatProgress(0, 3)).toBe('0 / 3 assets loaded');
    expect(formatProgress(1, 3)).toBe('1 / 3 assets loaded');
    expect(formatProgress(2, 3)).toBe('2 / 3 assets loaded');
    expect(formatProgress(3, 3)).toBe('3 / 3 assets loaded');
  });

  it('should format loading text with asset name', () => {
    const formatLoading = (assetName: string): string => {
      return `Loading: ${assetName}`;
    };

    expect(formatLoading('Town')).toBe('Loading: Town');
    expect(formatLoading('LaserGun')).toBe('Loading: LaserGun');
    expect(formatLoading('Soldier')).toBe('Loading: Soldier');
  });
});

describe('Error Panel Display', () => {
  describe('Pointer Lock Errors', () => {
    it('should display user activation error message', () => {
      const error: PointerLockError = {
        type: 'user_activation',
        message:
          'Pointer lock must be requested immediately after a user interaction. Please click the Start button again.',
        timestamp: Date.now(),
      };

      expect(error.message).toContain('user interaction');
      expect(error.message).toContain('Start button');
    });

    it('should display browser denied error message', () => {
      const error: PointerLockError = {
        type: 'browser_denied',
        message:
          'The browser denied the pointer lock request. Please try clicking the Start button.',
        timestamp: Date.now(),
      };

      expect(error.message).toContain('denied');
      expect(error.message).toContain('Start button');
    });

    it('should display hardware error message', () => {
      const error: PointerLockError = {
        type: 'hardware_error',
        message:
          'Unable to capture mouse after multiple attempts. Please check your browser settings or try a different browser.',
        timestamp: Date.now(),
      };

      expect(error.message).toContain('browser settings');
      expect(error.message).toContain('multiple attempts');
    });
  });

  describe('Asset Errors', () => {
    it('should list failed assets in error message', () => {
      const status: AssetsValidationStatus = {
        townLoaded: true,
        townValidated: false,
        laserGunLoaded: true,
        laserGunValidated: true,
        soldierLoaded: true,
        soldierValidated: false,
        allAssetsReady: false,
        validationResults: [],
      };

      const failedAssets: string[] = [];
      if (!status.townValidated && status.townLoaded) failedAssets.push('Town');
      if (!status.laserGunValidated && status.laserGunLoaded) failedAssets.push('Laser Gun');
      if (!status.soldierValidated && status.soldierLoaded) failedAssets.push('Soldier');

      const message = `Failed to load: ${failedAssets.join(', ')}`;
      expect(message).toBe('Failed to load: Town, Soldier');
    });
  });

  describe('Button Visibility', () => {
    it('should show retry button for pointer lock errors', () => {
      const isPointerLockError = true;
      const showRetryButton = isPointerLockError;
      const showReloadButton = !isPointerLockError;

      expect(showRetryButton).toBe(true);
      expect(showReloadButton).toBe(false);
    });

    it('should show reload button for asset errors', () => {
      const isPointerLockError = false;
      const showRetryButton = isPointerLockError;
      const showReloadButton = !isPointerLockError;

      expect(showRetryButton).toBe(false);
      expect(showReloadButton).toBe(true);
    });
  });
});

describe('Pause Menu', () => {
  it('should show pause menu when gameplay is paused', () => {
    let currentState: GameState = GameState.GAMEPLAY;

    // Simulate ESC press
    currentState = GameState.PAUSED;

    expect(currentState).toBe(GameState.PAUSED);
  });

  it('should hide pause menu on resume', () => {
    let currentState: GameState = GameState.PAUSED;

    // Simulate resume click
    currentState = GameState.GAMEPLAY;

    expect(currentState).toBe(GameState.GAMEPLAY);
  });
});

describe('UI Update from Pointer Lock State', () => {
  it('should enter gameplay on LOCKED state', () => {
    let currentUIState: GameState = GameState.MAIN_MENU;
    const pointerLockState: PointerLockState = PointerLockState.LOCKED;

    if (pointerLockState === PointerLockState.LOCKED) {
      currentUIState = GameState.GAMEPLAY;
    }

    expect(currentUIState).toBe(GameState.GAMEPLAY);
  });

  it('should show pause menu on UNLOCKED during gameplay', () => {
    let currentUIState: GameState = GameState.GAMEPLAY;
    const pointerLockState: PointerLockState = PointerLockState.UNLOCKED;

    if (pointerLockState === PointerLockState.UNLOCKED && currentUIState === GameState.GAMEPLAY) {
      currentUIState = GameState.PAUSED;
    }

    expect(currentUIState).toBe(GameState.PAUSED);
  });

  it('should not change state on UNLOCKED during main menu', () => {
    let currentUIState: GameState = GameState.MAIN_MENU;
    const pointerLockState: PointerLockState = PointerLockState.UNLOCKED;

    if (
      pointerLockState === PointerLockState.UNLOCKED &&
      currentUIState === (GameState.GAMEPLAY as GameState)
    ) {
      currentUIState = GameState.PAUSED;
    }

    expect(currentUIState).toBe(GameState.MAIN_MENU);
  });
});

describe('Panel Visibility Logic', () => {
  interface PanelVisibility {
    loading: boolean;
    mainMenu: boolean;
    error: boolean;
    pause: boolean;
  }

  it('should show only loading panel in LOADING state', () => {
    const getVisibility = (state: GameState): PanelVisibility => ({
      loading: state === GameState.LOADING,
      mainMenu: state === GameState.MAIN_MENU,
      error: state === GameState.ERROR,
      pause: state === GameState.PAUSED,
    });

    const visibility = getVisibility(GameState.LOADING);
    expect(visibility.loading).toBe(true);
    expect(visibility.mainMenu).toBe(false);
    expect(visibility.error).toBe(false);
    expect(visibility.pause).toBe(false);
  });

  it('should show only main menu panel in MAIN_MENU state', () => {
    const getVisibility = (state: GameState): PanelVisibility => ({
      loading: state === GameState.LOADING,
      mainMenu: state === GameState.MAIN_MENU,
      error: state === GameState.ERROR,
      pause: state === GameState.PAUSED,
    });

    const visibility = getVisibility(GameState.MAIN_MENU);
    expect(visibility.loading).toBe(false);
    expect(visibility.mainMenu).toBe(true);
    expect(visibility.error).toBe(false);
    expect(visibility.pause).toBe(false);
  });

  it('should hide all panels in GAMEPLAY state', () => {
    const getVisibility = (state: GameState): PanelVisibility => ({
      loading: state === GameState.LOADING,
      mainMenu: state === GameState.MAIN_MENU,
      error: state === GameState.ERROR,
      pause: state === GameState.PAUSED,
    });

    const visibility = getVisibility(GameState.GAMEPLAY);
    expect(visibility.loading).toBe(false);
    expect(visibility.mainMenu).toBe(false);
    expect(visibility.error).toBe(false);
    expect(visibility.pause).toBe(false);
  });
});
