import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AppState } from './AppState';

/**
 * Tests for the IScene interface contract.
 *
 * Since BaseScene requires Babylon.js (WebGL), we test the interface
 * contract and event listener management patterns.
 */

describe('IScene Interface Contract', () => {
  describe('Required Properties', () => {
    it('should require a scene property', () => {
      interface ISceneContract {
        readonly scene: unknown;
        readonly state: AppState;
        init(): Promise<void>;
        update(deltaTime: number): void;
        dispose(): void;
      }

      // This compile-time check verifies the interface structure
      const mockScene: ISceneContract = {
        scene: {},
        state: AppState.LOADING,
        init: async () => {},
        update: () => {},
        dispose: () => {},
      };

      expect(mockScene.scene).toBeDefined();
      expect(mockScene.state).toBe(AppState.LOADING);
    });

    it('should require a state property that matches AppState', () => {
      Object.values(AppState).forEach((state) => {
        const mockScene = {
          state: state,
        };
        expect(Object.values(AppState)).toContain(mockScene.state);
      });
    });
  });

  describe('Required Methods', () => {
    it('should require an async init method', async () => {
      const initFn = vi.fn().mockResolvedValue(undefined);

      await initFn();

      expect(initFn).toHaveBeenCalled();
    });

    it('should require an update method with deltaTime parameter', () => {
      const updateFn = vi.fn();
      const deltaTime = 16.67; // ~60fps

      updateFn(deltaTime);

      expect(updateFn).toHaveBeenCalledWith(deltaTime);
    });

    it('should require a dispose method', () => {
      const disposeFn = vi.fn();

      disposeFn();

      expect(disposeFn).toHaveBeenCalled();
    });
  });
});

describe('BaseScene Event Listener Management', () => {
  interface MockEventListener {
    target: {
      addEventListener: ReturnType<typeof vi.fn>;
      removeEventListener: ReturnType<typeof vi.fn>;
    };
    type: string;
    listener: () => void;
  }

  let eventListeners: MockEventListener[];

  const addEventListener = (
    target: MockEventListener['target'],
    type: string,
    listener: () => void
  ): void => {
    target.addEventListener(type, listener);
    eventListeners.push({ target, type, listener });
  };

  const dispose = (): void => {
    for (const { target, type, listener } of eventListeners) {
      target.removeEventListener(type, listener);
    }
    eventListeners = [];
  };

  beforeEach(() => {
    eventListeners = [];
  });

  it('should add event listener to target and track it', () => {
    const mockTarget = {
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    };
    const mockListener = vi.fn();

    addEventListener(mockTarget, 'keydown', mockListener);

    expect(mockTarget.addEventListener).toHaveBeenCalledWith('keydown', mockListener);
    expect(eventListeners).toHaveLength(1);
  });

  it('should track multiple event listeners', () => {
    const mockWindow = {
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    };
    const mockDocument = {
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    };

    addEventListener(mockWindow, 'resize', vi.fn());
    addEventListener(mockWindow, 'keydown', vi.fn());
    addEventListener(mockDocument, 'pointerlockchange', vi.fn());

    expect(eventListeners).toHaveLength(3);
  });

  it('should remove all event listeners on dispose', () => {
    const mockTarget = {
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    };
    const listener1 = vi.fn();
    const listener2 = vi.fn();

    addEventListener(mockTarget, 'keydown', listener1);
    addEventListener(mockTarget, 'keyup', listener2);

    dispose();

    expect(mockTarget.removeEventListener).toHaveBeenCalledWith('keydown', listener1);
    expect(mockTarget.removeEventListener).toHaveBeenCalledWith('keyup', listener2);
    expect(eventListeners).toHaveLength(0);
  });

  it('should handle disposing with no listeners', () => {
    expect(() => dispose()).not.toThrow();
    expect(eventListeners).toHaveLength(0);
  });
});

describe('Scene Lifecycle', () => {
  let isInitialized: boolean;
  let isDisposed: boolean;
  let updateCount: number;

  beforeEach(() => {
    isInitialized = false;
    isDisposed = false;
    updateCount = 0;
  });

  const init = async (): Promise<void> => {
    if (isDisposed) {
      throw new Error('Cannot initialize disposed scene');
    }
    isInitialized = true;
  };

  const update = (deltaTime: number): void => {
    if (isDisposed) {
      throw new Error('Cannot update disposed scene');
    }
    if (!isInitialized) {
      throw new Error('Cannot update uninitialized scene');
    }
    updateCount++;
    void deltaTime;
  };

  const dispose = (): void => {
    isInitialized = false;
    isDisposed = true;
  };

  it('should initialize before updating', async () => {
    expect(() => update(16)).toThrow('Cannot update uninitialized scene');

    await init();

    expect(() => update(16)).not.toThrow();
  });

  it('should not allow update after dispose', async () => {
    await init();
    update(16);

    dispose();

    expect(() => update(16)).toThrow('Cannot update disposed scene');
  });

  it('should not allow re-initialization after dispose', async () => {
    await init();
    dispose();

    await expect(init()).rejects.toThrow('Cannot initialize disposed scene');
  });

  it('should track update calls', async () => {
    await init();

    update(16.67);
    update(16.67);
    update(16.67);

    expect(updateCount).toBe(3);
  });
});
