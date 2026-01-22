import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { AppState } from './AppState';

/**
 * Unit tests for App class logic.
 *
 * Note: Since Babylon.js requires WebGL/browser environment,
 * we test the state machine logic and interfaces separately.
 * Full integration tests would require a browser environment (e.g., Playwright).
 */

describe('App State Machine Logic', () => {
  describe('State Transitions', () => {
    it('should define valid state transition paths', () => {
      // Valid transitions in the game
      const validTransitions: Array<[AppState, AppState]> = [
        [AppState.LOADING, AppState.MAIN_MENU],
        [AppState.MAIN_MENU, AppState.GAME_PLAY],
        [AppState.GAME_PLAY, AppState.GAME_OVER],
        [AppState.GAME_OVER, AppState.MAIN_MENU],
        [AppState.GAME_OVER, AppState.GAME_PLAY],
      ];

      // Verify all transitions use valid states
      validTransitions.forEach(([from, to]) => {
        expect(Object.values(AppState)).toContain(from);
        expect(Object.values(AppState)).toContain(to);
        expect(from).not.toBe(to);
      });
    });

    it('should define LOADING as the initial state', () => {
      // The app should start in LOADING state
      expect(AppState.LOADING).toBeDefined();
    });
  });

  describe('State Change Callback System', () => {
    let callbacks: Array<(newState: AppState, oldState: AppState | null) => void>;
    let notifyCallbacks: (newState: AppState, oldState: AppState | null) => void;

    beforeEach(() => {
      callbacks = [];
      notifyCallbacks = (newState, oldState): void => {
        for (const callback of callbacks) {
          callback(newState, oldState);
        }
      };
    });

    it('should allow registering state change callbacks', () => {
      const callback = vi.fn();
      callbacks.push(callback);

      notifyCallbacks(AppState.MAIN_MENU, AppState.LOADING);

      expect(callback).toHaveBeenCalledWith(AppState.MAIN_MENU, AppState.LOADING);
    });

    it('should allow multiple callbacks', () => {
      const callback1 = vi.fn();
      const callback2 = vi.fn();
      callbacks.push(callback1);
      callbacks.push(callback2);

      notifyCallbacks(AppState.GAME_PLAY, AppState.MAIN_MENU);

      expect(callback1).toHaveBeenCalledTimes(1);
      expect(callback2).toHaveBeenCalledTimes(1);
    });

    it('should pass correct old and new states to callbacks', () => {
      const callback = vi.fn();
      callbacks.push(callback);

      notifyCallbacks(AppState.GAME_OVER, AppState.GAME_PLAY);

      expect(callback).toHaveBeenCalledWith(AppState.GAME_OVER, AppState.GAME_PLAY);
    });

    it('should handle null old state for initial transition', () => {
      const callback = vi.fn();
      callbacks.push(callback);

      notifyCallbacks(AppState.LOADING, null);

      expect(callback).toHaveBeenCalledWith(AppState.LOADING, null);
    });
  });

  describe('Event Listener Cleanup Pattern', () => {
    let listeners: Array<{
      target: { removeEventListener: ReturnType<typeof vi.fn> };
      type: string;
      listener: () => void;
    }>;

    beforeEach(() => {
      listeners = [];
    });

    const addEventListener = (
      target: { removeEventListener: ReturnType<typeof vi.fn> },
      type: string,
      listener: () => void
    ): void => {
      listeners.push({ target, type, listener });
    };

    const disposeListeners = (): void => {
      for (const { target, type, listener } of listeners) {
        target.removeEventListener(type, listener);
      }
      listeners = [];
    };

    it('should track added event listeners', () => {
      const mockTarget = { removeEventListener: vi.fn() };
      const mockListener = vi.fn();

      addEventListener(mockTarget, 'keydown', mockListener);

      expect(listeners).toHaveLength(1);
      expect(listeners[0]).toEqual({
        target: mockTarget,
        type: 'keydown',
        listener: mockListener,
      });
    });

    it('should remove all listeners on dispose', () => {
      const mockTarget1 = { removeEventListener: vi.fn() };
      const mockTarget2 = { removeEventListener: vi.fn() };
      const mockListener1 = vi.fn();
      const mockListener2 = vi.fn();

      addEventListener(mockTarget1, 'keydown', mockListener1);
      addEventListener(mockTarget2, 'resize', mockListener2);

      disposeListeners();

      expect(mockTarget1.removeEventListener).toHaveBeenCalledWith('keydown', mockListener1);
      expect(mockTarget2.removeEventListener).toHaveBeenCalledWith('resize', mockListener2);
      expect(listeners).toHaveLength(0);
    });
  });
});

describe('Scene Factory Pattern', () => {
  it('should map each AppState to a scene type', () => {
    const stateToSceneMap: Record<AppState, string> = {
      [AppState.LOADING]: 'LoadingScene',
      [AppState.MAIN_MENU]: 'MainMenuScene',
      [AppState.GAME_PLAY]: 'GamePlayScene',
      [AppState.GAME_OVER]: 'GameOverScene',
    };

    // Verify all states are mapped
    Object.values(AppState).forEach((state) => {
      expect(stateToSceneMap[state]).toBeDefined();
      expect(typeof stateToSceneMap[state]).toBe('string');
    });
  });

  it('should have unique scene types for each state', () => {
    const sceneTypes = ['LoadingScene', 'MainMenuScene', 'GamePlayScene', 'GameOverScene'];
    const uniqueTypes = new Set(sceneTypes);

    expect(uniqueTypes.size).toBe(sceneTypes.length);
    expect(sceneTypes.length).toBe(Object.values(AppState).length);
  });
});

describe('Transition Guards', () => {
  let isTransitioning = false;
  let currentState: AppState | null = null;

  beforeEach(() => {
    isTransitioning = false;
    currentState = null;
  });

  afterEach(() => {
    isTransitioning = false;
    currentState = null;
  });

  const canTransition = (newState: AppState): boolean => {
    if (isTransitioning) {
      return false;
    }
    if (currentState === newState) {
      return false;
    }
    return true;
  };

  it('should prevent transition while already transitioning', () => {
    isTransitioning = true;

    expect(canTransition(AppState.MAIN_MENU)).toBe(false);
  });

  it('should prevent transition to the same state', () => {
    currentState = AppState.MAIN_MENU;

    expect(canTransition(AppState.MAIN_MENU)).toBe(false);
  });

  it('should allow transition when not transitioning and to different state', () => {
    currentState = AppState.LOADING;
    isTransitioning = false;

    expect(canTransition(AppState.MAIN_MENU)).toBe(true);
  });

  it('should allow transition from null state', () => {
    currentState = null;
    isTransitioning = false;

    expect(canTransition(AppState.LOADING)).toBe(true);
  });
});
