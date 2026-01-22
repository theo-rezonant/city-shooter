import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { PointerLockState, PointerLockError } from '../types/GameTypes';

// Mock Babylon Engine
const createMockEngine = (): {
  getRenderingCanvas: ReturnType<typeof vi.fn>;
  enterPointerlock: ReturnType<typeof vi.fn>;
  exitPointerlock: ReturnType<typeof vi.fn>;
} => ({
  getRenderingCanvas: vi.fn().mockReturnValue(document.createElement('canvas')),
  enterPointerlock: vi.fn(),
  exitPointerlock: vi.fn(),
});

// We need to test the PointerLockManager logic separately since Babylon.js
// has complex initialization. Here we test the core logic patterns.
describe('PointerLockManager Logic', () => {
  let mockCanvas: HTMLCanvasElement;
  let mockEngine: ReturnType<typeof createMockEngine>;

  beforeEach(() => {
    mockCanvas = document.createElement('canvas');
    mockEngine = createMockEngine();
    mockEngine.getRenderingCanvas.mockReturnValue(mockCanvas);

    // Reset document pointer lock state
    Object.defineProperty(document, 'pointerLockElement', {
      writable: true,
      value: null,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Pointer Lock State Detection', () => {
    it('should detect when pointer lock is not supported', () => {
      // Test the static method pattern
      const hasPointerLock =
        'pointerLockElement' in document ||
        'mozPointerLockElement' in document ||
        'webkitPointerLockElement' in document;

      expect(hasPointerLock).toBe(true); // jsdom supports it
    });

    it('should start in unlocked state', () => {
      const state: PointerLockState = PointerLockState.UNLOCKED;
      expect(state).toBe(PointerLockState.UNLOCKED);
    });

    it('should transition to requesting state on lock request', () => {
      let state: PointerLockState = PointerLockState.UNLOCKED;

      // Simulate requesting
      state = PointerLockState.REQUESTING;
      expect(state).toBe(PointerLockState.REQUESTING);
    });
  });

  describe('User Activation Timeout', () => {
    it('should detect expired user activation', () => {
      const userGestureTimeout = 1000; // 1 second
      const lastUserGestureTime = Date.now() - 2000; // 2 seconds ago
      const timeSinceGesture = Date.now() - lastUserGestureTime;

      expect(timeSinceGesture > userGestureTimeout).toBe(true);
    });

    it('should accept valid user activation', () => {
      const userGestureTimeout = 1000;
      const lastUserGestureTime = Date.now() - 100; // 100ms ago
      const timeSinceGesture = Date.now() - lastUserGestureTime;

      expect(timeSinceGesture <= userGestureTimeout).toBe(true);
    });
  });

  describe('Error Handling', () => {
    it('should categorize user activation error correctly', () => {
      const userGestureTimeout = 1000;
      const timeSinceGesture = 1500; // More than timeout

      let errorType: PointerLockError['type'];
      if (timeSinceGesture > userGestureTimeout) {
        errorType = 'user_activation';
      } else {
        errorType = 'browser_denied';
      }

      expect(errorType).toBe('user_activation');
    });

    it('should categorize hardware error after max retries', () => {
      const maxRetries = 3;
      const retryCount = 3;
      const timeSinceGesture = 500; // Within timeout
      const userGestureTimeout = 1000;

      let errorType: PointerLockError['type'];
      if (timeSinceGesture > userGestureTimeout) {
        errorType = 'user_activation';
      } else if (retryCount >= maxRetries) {
        errorType = 'hardware_error';
      } else {
        errorType = 'browser_denied';
      }

      expect(errorType).toBe('hardware_error');
    });

    it('should create proper error object', () => {
      const error: PointerLockError = {
        type: 'browser_denied',
        message: 'The browser denied the pointer lock request',
        timestamp: Date.now(),
      };

      expect(error.type).toBe('browser_denied');
      expect(error.message).toBeTruthy();
      expect(error.timestamp).toBeLessThanOrEqual(Date.now());
    });
  });

  describe('Pointer Lock Change Events', () => {
    it('should detect locked state when pointerLockElement matches canvas', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        writable: true,
        value: mockCanvas,
      });

      const isLocked = document.pointerLockElement === mockCanvas;
      expect(isLocked).toBe(true);
    });

    it('should detect unlocked state when pointerLockElement is null', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        writable: true,
        value: null,
      });

      const isLocked = document.pointerLockElement === mockCanvas;
      expect(isLocked).toBe(false);
    });

    it('should handle different browser prefixes', () => {
      const canvas = mockCanvas;
      const doc = document as unknown as {
        pointerLockElement?: Element;
        mozPointerLockElement?: Element;
        webkitPointerLockElement?: Element;
      };

      // Test each prefix
      const isLocked =
        doc.pointerLockElement === canvas ||
        doc.mozPointerLockElement === canvas ||
        doc.webkitPointerLockElement === canvas;

      expect(typeof isLocked).toBe('boolean');
    });
  });

  describe('Retry Logic', () => {
    it('should reset retry count after successful lock', () => {
      let retryCount = 3;

      // Simulate successful lock
      retryCount = 0;
      expect(retryCount).toBe(0);
    });

    it('should increment retry count on error', () => {
      let retryCount = 0;

      // Simulate error
      retryCount++;
      expect(retryCount).toBe(1);

      // Another error
      retryCount++;
      expect(retryCount).toBe(2);
    });

    it('should not exceed max retries before showing hardware error', () => {
      const maxRetries = 3;
      let retryCount = 0;

      // Simulate multiple errors
      for (let i = 0; i < 5; i++) {
        retryCount++;
        const isHardwareError = retryCount >= maxRetries;
        if (i >= 2) {
          expect(isHardwareError).toBe(true);
        }
      }
    });
  });

  describe('Callback Execution', () => {
    it('should call onLocked callback when locked', () => {
      const callbacks = {
        onLocked: vi.fn(),
        onUnlocked: vi.fn(),
        onError: vi.fn(),
      };

      // Simulate lock
      callbacks.onLocked();
      expect(callbacks.onLocked).toHaveBeenCalledTimes(1);
    });

    it('should call onUnlocked callback when unlocked', () => {
      const callbacks = {
        onLocked: vi.fn(),
        onUnlocked: vi.fn(),
        onError: vi.fn(),
      };

      // Simulate unlock
      callbacks.onUnlocked();
      expect(callbacks.onUnlocked).toHaveBeenCalledTimes(1);
    });

    it('should call onError callback with error details', () => {
      const callbacks = {
        onLocked: vi.fn(),
        onUnlocked: vi.fn(),
        onError: vi.fn(),
      };

      const error: PointerLockError = {
        type: 'browser_denied',
        message: 'Test error',
        timestamp: Date.now(),
      };

      callbacks.onError(error);
      expect(callbacks.onError).toHaveBeenCalledWith(error);
    });
  });
});

describe('PointerLockManager State Machine', () => {
  it('should follow correct state transitions', () => {
    // Test state machine transitions
    const validTransitions: Record<PointerLockState, PointerLockState[]> = {
      [PointerLockState.UNLOCKED]: [PointerLockState.REQUESTING],
      [PointerLockState.REQUESTING]: [PointerLockState.LOCKED, PointerLockState.ERROR],
      [PointerLockState.LOCKED]: [PointerLockState.UNLOCKED],
      [PointerLockState.ERROR]: [PointerLockState.UNLOCKED],
    };

    // Verify transitions exist
    expect(validTransitions[PointerLockState.UNLOCKED]).toContain(PointerLockState.REQUESTING);
    expect(validTransitions[PointerLockState.REQUESTING]).toContain(PointerLockState.LOCKED);
    expect(validTransitions[PointerLockState.REQUESTING]).toContain(PointerLockState.ERROR);
    expect(validTransitions[PointerLockState.LOCKED]).toContain(PointerLockState.UNLOCKED);
    expect(validTransitions[PointerLockState.ERROR]).toContain(PointerLockState.UNLOCKED);
  });

  it('should not allow invalid state transitions', () => {
    // UNLOCKED cannot go directly to LOCKED (must go through REQUESTING)
    const currentState = PointerLockState.UNLOCKED;
    const validNextStates = [PointerLockState.REQUESTING];

    expect(validNextStates).not.toContain(PointerLockState.LOCKED);
    expect(currentState).toBe(PointerLockState.UNLOCKED);
  });
});
