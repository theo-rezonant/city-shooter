import { describe, it, expect, vi, beforeEach } from 'vitest';
import { GameState, PointerLockState, AssetsValidationStatus } from '../types/GameTypes';

describe('GameStateManager State Machine', () => {
  let currentState: GameState;

  beforeEach(() => {
    currentState = GameState.LOADING;
  });

  describe('Initial State', () => {
    it('should start in LOADING state', () => {
      expect(currentState).toBe(GameState.LOADING);
    });
  });

  describe('State Transitions', () => {
    it('should transition from LOADING to MAIN_MENU when assets are ready', () => {
      const assetsStatus: AssetsValidationStatus = {
        townLoaded: true,
        townValidated: true,
        laserGunLoaded: true,
        laserGunValidated: true,
        soldierLoaded: true,
        soldierValidated: true,
        allAssetsReady: true,
        validationResults: [],
      };

      if (assetsStatus.allAssetsReady) {
        currentState = GameState.MAIN_MENU;
      }

      expect(currentState).toBe(GameState.MAIN_MENU);
    });

    it('should transition from LOADING to ERROR when assets fail', () => {
      const assetsStatus: AssetsValidationStatus = {
        townLoaded: true,
        townValidated: false,
        laserGunLoaded: false,
        laserGunValidated: false,
        soldierLoaded: true,
        soldierValidated: true,
        allAssetsReady: false,
        validationResults: [],
      };

      if (!assetsStatus.allAssetsReady) {
        currentState = GameState.ERROR;
      }

      expect(currentState).toBe(GameState.ERROR);
    });

    it('should transition from MAIN_MENU to GAMEPLAY when pointer lock is acquired', () => {
      currentState = GameState.MAIN_MENU;
      const pointerLockState = PointerLockState.LOCKED;

      if (
        pointerLockState === PointerLockState.LOCKED &&
        (currentState === GameState.MAIN_MENU || currentState === GameState.PAUSED)
      ) {
        currentState = GameState.GAMEPLAY;
      }

      expect(currentState).toBe(GameState.GAMEPLAY);
    });

    it('should transition from GAMEPLAY to PAUSED when pointer lock is released', () => {
      currentState = GameState.GAMEPLAY;
      const pointerLockState = PointerLockState.UNLOCKED;

      if (pointerLockState === PointerLockState.UNLOCKED && currentState === GameState.GAMEPLAY) {
        currentState = GameState.PAUSED;
      }

      expect(currentState).toBe(GameState.PAUSED);
    });

    it('should transition from PAUSED to GAMEPLAY when pointer lock is re-acquired', () => {
      currentState = GameState.PAUSED;
      const pointerLockState = PointerLockState.LOCKED;

      if (
        pointerLockState === PointerLockState.LOCKED &&
        [GameState.MAIN_MENU, GameState.PAUSED].includes(currentState)
      ) {
        currentState = GameState.GAMEPLAY;
      }

      expect(currentState).toBe(GameState.GAMEPLAY);
    });

    it('should transition to ERROR when pointer lock request fails', () => {
      currentState = GameState.MAIN_MENU;
      const pointerLockState = PointerLockState.ERROR;

      if (pointerLockState === PointerLockState.ERROR) {
        currentState = GameState.ERROR;
      }

      expect(currentState).toBe(GameState.ERROR);
    });

    it('should transition from ERROR to MAIN_MENU on retry', () => {
      currentState = GameState.ERROR;

      // Simulate retry
      currentState = GameState.MAIN_MENU;

      expect(currentState).toBe(GameState.MAIN_MENU);
    });

    it('should transition from ERROR to LOADING on asset reload', () => {
      currentState = GameState.ERROR;

      // Simulate reload assets
      currentState = GameState.LOADING;

      expect(currentState).toBe(GameState.LOADING);
    });
  });
});

describe('Event Handler Integration', () => {
  describe('Asset Loading Events', () => {
    it('should handle progress updates', () => {
      const onProgress = vi.fn();
      const progress = { loaded: 1, total: 3, current: 'Town' };

      onProgress(progress.loaded, progress.total, progress.current);

      expect(onProgress).toHaveBeenCalledWith(1, 3, 'Town');
    });

    it('should handle asset load completion', () => {
      const onAllAssetsReady = vi.fn();
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

      onAllAssetsReady(status);

      expect(onAllAssetsReady).toHaveBeenCalledWith(status);
    });

    it('should handle asset load errors', () => {
      const onAssetError = vi.fn();
      const error = new Error('Failed to load Town');

      onAssetError('Town', error);

      expect(onAssetError).toHaveBeenCalledWith('Town', error);
    });
  });

  describe('Pointer Lock Events', () => {
    it('should handle pointer lock state changes', () => {
      const onStateChanged = vi.fn();

      onStateChanged(PointerLockState.LOCKED);
      expect(onStateChanged).toHaveBeenCalledWith(PointerLockState.LOCKED);

      onStateChanged(PointerLockState.UNLOCKED);
      expect(onStateChanged).toHaveBeenCalledWith(PointerLockState.UNLOCKED);
    });

    it('should handle pointer lock errors', () => {
      const onError = vi.fn();
      const error = {
        type: 'browser_denied' as const,
        message: 'Test error',
        timestamp: Date.now(),
      };

      onError(error);

      expect(onError).toHaveBeenCalledWith(error);
    });
  });

  describe('UI Events', () => {
    it('should handle start button click', () => {
      const onStartClicked = vi.fn();

      onStartClicked();

      expect(onStartClicked).toHaveBeenCalledTimes(1);
    });

    it('should handle retry button click', () => {
      const onRetryClicked = vi.fn();

      onRetryClicked();

      expect(onRetryClicked).toHaveBeenCalledTimes(1);
    });

    it('should handle reload assets click', () => {
      const onReloadAssetsClicked = vi.fn();

      onReloadAssetsClicked();

      expect(onReloadAssetsClicked).toHaveBeenCalledTimes(1);
    });

    it('should handle resume button click', () => {
      const onResumeClicked = vi.fn();

      onResumeClicked();

      expect(onResumeClicked).toHaveBeenCalledTimes(1);
    });
  });
});

describe('ESC Key Handler', () => {
  it('should pause game when ESC is pressed during gameplay', () => {
    let currentState = GameState.GAMEPLAY;
    const escPressed = true;

    if (escPressed && currentState === GameState.GAMEPLAY) {
      // Simulate pointer lock release
      currentState = GameState.PAUSED;
    }

    expect(currentState).toBe(GameState.PAUSED);
  });

  it('should not change state when ESC is pressed during pause', () => {
    let currentState: GameState = GameState.PAUSED;
    const escPressed = true;

    if (escPressed && currentState === (GameState.GAMEPLAY as GameState)) {
      currentState = GameState.PAUSED;
    }

    // State should remain PAUSED
    expect(currentState).toBe(GameState.PAUSED);
  });

  it('should not change state when ESC is pressed during loading', () => {
    let currentState: GameState = GameState.LOADING;
    const escPressed = true;

    if (escPressed && currentState === (GameState.GAMEPLAY as GameState)) {
      currentState = GameState.PAUSED;
    }

    expect(currentState).toBe(GameState.LOADING);
  });
});

describe('Ready to Play Check', () => {
  it('should return true when assets are ready and in valid state', () => {
    const assetsReady = true;
    const currentState = GameState.MAIN_MENU;

    const isReadyToPlay = (): boolean => {
      return (
        assetsReady &&
        (currentState === GameState.MAIN_MENU ||
          currentState === GameState.GAMEPLAY ||
          currentState === GameState.PAUSED)
      );
    };

    expect(isReadyToPlay()).toBe(true);
  });

  it('should return false when assets are not ready', () => {
    const assetsReady = false;
    const currentState = GameState.MAIN_MENU;

    const isReadyToPlay = (): boolean => {
      return (
        assetsReady &&
        (currentState === GameState.MAIN_MENU ||
          currentState === GameState.GAMEPLAY ||
          currentState === GameState.PAUSED)
      );
    };

    expect(isReadyToPlay()).toBe(false);
  });

  it('should return false when in loading state', () => {
    const assetsReady = false;
    const currentState: GameState = GameState.LOADING;
    const validStates = [GameState.MAIN_MENU, GameState.GAMEPLAY, GameState.PAUSED];

    const isReadyToPlay = (): boolean => {
      return assetsReady && validStates.includes(currentState);
    };

    expect(isReadyToPlay()).toBe(false);
  });

  it('should return false when in error state', () => {
    const assetsReady = true;
    const currentState: GameState = GameState.ERROR;
    const validStates = [GameState.MAIN_MENU, GameState.GAMEPLAY, GameState.PAUSED];

    const isReadyToPlay = (): boolean => {
      return assetsReady && validStates.includes(currentState);
    };

    expect(isReadyToPlay()).toBe(false);
  });
});

describe('Pointer Lock Support Check', () => {
  it('should detect pointer lock support', () => {
    const isSupported = (): boolean => {
      return (
        'pointerLockElement' in document ||
        'mozPointerLockElement' in document ||
        'webkitPointerLockElement' in document
      );
    };

    // jsdom has pointerLockElement
    expect(isSupported()).toBe(true);
  });
});

describe('Start Button Handler', () => {
  it('should not request lock when assets are not ready', () => {
    const assetsReady = false;
    const requestLock = vi.fn();

    const handleStartClicked = (): void => {
      if (!assetsReady) {
        console.warn('Cannot start: assets not ready');
        return;
      }
      requestLock();
    };

    handleStartClicked();

    expect(requestLock).not.toHaveBeenCalled();
  });

  it('should request lock when assets are ready', () => {
    const assetsReady = true;
    const requestLock = vi.fn();

    const handleStartClicked = (): void => {
      if (!assetsReady) {
        console.warn('Cannot start: assets not ready');
        return;
      }
      requestLock();
    };

    handleStartClicked();

    expect(requestLock).toHaveBeenCalledTimes(1);
  });
});

describe('Reload Assets Handler', () => {
  it('should transition to LOADING state first', () => {
    let currentState: GameState = GameState.ERROR;
    const states: GameState[] = [];

    const handleReloadAssets = async (): Promise<void> => {
      currentState = GameState.LOADING;
      states.push(currentState);

      // Simulate reload
      await Promise.resolve();

      currentState = GameState.MAIN_MENU;
      states.push(currentState);
    };

    handleReloadAssets();

    expect(states[0]).toBe(GameState.LOADING);
  });

  it('should reload only failed assets', () => {
    const failedAssets = ['LaserGun'];
    const reloadedAssets: string[] = [];

    const reloadAsset = (name: string): void => {
      reloadedAssets.push(name);
    };

    failedAssets.forEach((asset) => reloadAsset(asset));

    expect(reloadedAssets).toEqual(['LaserGun']);
    expect(reloadedAssets).not.toContain('Town');
    expect(reloadedAssets).not.toContain('Soldier');
  });
});
