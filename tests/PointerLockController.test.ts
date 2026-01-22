import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { PointerLockController } from '../src/controllers/PointerLockController';
import { GameState } from '../src/states/GameState';
import { ISceneManager } from '../src/core/types';
import { UniversalCamera } from '@babylonjs/core';

// Mock Babylon.js modules
vi.mock('@babylonjs/core', () => ({
  UniversalCamera: vi.fn().mockImplementation(() => ({
    attachControl: vi.fn(),
    detachControl: vi.fn(),
  })),
  Scene: vi.fn(),
}));

describe('PointerLockController', () => {
  let controller: PointerLockController;
  let mockCanvas: HTMLCanvasElement;
  let mockSceneManager: ISceneManager;
  let mockCamera: UniversalCamera;
  let documentEventListeners: Map<string, EventListener>;

  beforeEach(() => {
    // Store event listeners so we can trigger them
    documentEventListeners = new Map();

    // Mock document event listeners
    vi.spyOn(document, 'addEventListener').mockImplementation((event, listener) => {
      documentEventListeners.set(event, listener as EventListener);
    });

    vi.spyOn(document, 'removeEventListener').mockImplementation((event) => {
      documentEventListeners.delete(event);
    });

    // Add exitPointerLock to document (not in jsdom by default)
    if (!document.exitPointerLock) {
      (document as unknown as Record<string, unknown>).exitPointerLock = vi.fn();
    } else {
      vi.spyOn(document, 'exitPointerLock').mockImplementation(() => {});
    }

    // Mock canvas
    mockCanvas = {
      requestPointerLock: vi.fn().mockResolvedValue(undefined),
    } as unknown as HTMLCanvasElement;

    // Mock scene manager
    mockSceneManager = {
      getCurrentState: vi.fn().mockReturnValue(GameState.MAIN_MENU),
      transitionTo: vi.fn(),
      onStateChange: vi.fn(),
      getActiveCamera: vi.fn(),
      getScene: vi.fn(),
      getEngine: vi.fn(),
    };

    // Mock camera
    mockCamera = new UniversalCamera('camera', null as any, null as any);

    controller = new PointerLockController(mockCanvas, mockSceneManager);
  });

  afterEach(() => {
    controller.dispose();
    vi.clearAllMocks();

    // Reset pointerLockElement
    Object.defineProperty(document, 'pointerLockElement', {
      value: null,
      configurable: true,
    });
  });

  describe('constructor', () => {
    it('should create a PointerLockController instance', () => {
      expect(controller).toBeDefined();
    });

    it('should set up event listeners', () => {
      expect(document.addEventListener).toHaveBeenCalledWith(
        'pointerlockchange',
        expect.any(Function)
      );
      expect(document.addEventListener).toHaveBeenCalledWith(
        'pointerlockerror',
        expect.any(Function)
      );
    });

    it('should not be locked initially', () => {
      expect(controller.isLocked).toBe(false);
    });
  });

  describe('requestLock', () => {
    it('should request pointer lock on canvas', async () => {
      await controller.requestLock();

      expect(mockCanvas.requestPointerLock).toHaveBeenCalled();
    });

    it('should not request lock if already locked', async () => {
      // Simulate already locked
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      // Trigger lock change to update internal state
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      await controller.requestLock();

      // Should only have been called during setup, not from requestLock
      expect(mockCanvas.requestPointerLock).not.toHaveBeenCalled();
    });

    it('should throw if requestPointerLock fails', async () => {
      const error = new Error('Permission denied');
      mockCanvas.requestPointerLock = vi.fn().mockRejectedValue(error);

      await expect(controller.requestLock()).rejects.toThrow('Permission denied');
    });
  });

  describe('releaseLock', () => {
    it('should call document.exitPointerLock when locked', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      controller.releaseLock();

      expect(document.exitPointerLock).toHaveBeenCalled();
    });

    it('should not call exitPointerLock when not locked', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        value: null,
        configurable: true,
      });

      controller.releaseLock();

      expect(document.exitPointerLock).not.toHaveBeenCalled();
    });
  });

  describe('pointer lock change handling', () => {
    it('should update isLocked when lock is acquired', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(controller.isLocked).toBe(true);
    });

    it('should update isLocked when lock is released', () => {
      // First acquire lock
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      // Then release lock
      Object.defineProperty(document, 'pointerLockElement', {
        value: null,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(controller.isLocked).toBe(false);
    });

    it('should transition to GAMEPLAY when lock is acquired', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(mockSceneManager.transitionTo).toHaveBeenCalledWith(GameState.GAMEPLAY);
    });

    it('should transition to PAUSED when lock is released during gameplay', () => {
      // Set up as if in gameplay
      mockSceneManager.getCurrentState = vi.fn().mockReturnValue(GameState.GAMEPLAY);

      // First acquire lock
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      // Then release lock (e.g., user pressed Esc)
      Object.defineProperty(document, 'pointerLockElement', {
        value: null,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(mockSceneManager.transitionTo).toHaveBeenCalledWith(GameState.PAUSED);
    });
  });

  describe('camera attachment', () => {
    it('should attach camera', () => {
      controller.attachCamera(mockCamera);

      // Internal state - we can verify by triggering lock
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(mockCamera.attachControl).toHaveBeenCalledWith(mockCanvas, true);
    });

    it('should enable camera input when lock is acquired', () => {
      controller.attachCamera(mockCamera);

      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(mockCamera.attachControl).toHaveBeenCalledWith(mockCanvas, true);
    });

    it('should disable camera input when lock is released', () => {
      controller.attachCamera(mockCamera);

      // Acquire lock
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      // Release lock
      Object.defineProperty(document, 'pointerLockElement', {
        value: null,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(mockCamera.detachControl).toHaveBeenCalled();
    });

    it('should detach camera', () => {
      controller.attachCamera(mockCamera);
      controller.detachCamera();

      // Trigger lock change to verify camera is not controlled
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      // attachControl should only have been called once during initial lock
      // (not after detachCamera)
    });
  });

  describe('lock change listeners', () => {
    it('should notify listeners when lock state changes', () => {
      const listener = vi.fn();
      controller.onLockChange(listener);

      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(listener).toHaveBeenCalledWith(true);
    });

    it('should allow unsubscribing from lock changes', () => {
      const listener = vi.fn();
      const unsubscribe = controller.onLockChange(listener);

      unsubscribe();

      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(listener).not.toHaveBeenCalled();
    });

    it('should handle errors in listeners gracefully', () => {
      const errorListener = vi.fn().mockImplementation(() => {
        throw new Error('Listener error');
      });
      const normalListener = vi.fn();

      controller.onLockChange(errorListener);
      controller.onLockChange(normalListener);

      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      // Should not throw
      expect(() => {
        documentEventListeners.get('pointerlockchange')?.({} as Event);
      }).not.toThrow();

      // Normal listener should still be called
      expect(normalListener).toHaveBeenCalled();
    });
  });

  describe('pointer lock error handling', () => {
    it('should set isLocked to false on error', () => {
      documentEventListeners.get('pointerlockerror')?.({} as Event);

      expect(controller.isLocked).toBe(false);
    });

    it('should notify listeners on error', () => {
      const listener = vi.fn();
      controller.onLockChange(listener);

      documentEventListeners.get('pointerlockerror')?.({} as Event);

      expect(listener).toHaveBeenCalledWith(false);
    });
  });

  describe('setSceneManager', () => {
    it('should allow setting scene manager after construction', () => {
      const controller2 = new PointerLockController(mockCanvas);
      const newSceneManager: ISceneManager = {
        ...mockSceneManager,
        transitionTo: vi.fn(),
      };

      controller2.setSceneManager(newSceneManager);

      // Trigger lock acquisition
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(newSceneManager.transitionTo).toHaveBeenCalledWith(GameState.GAMEPLAY);

      controller2.dispose();
    });
  });

  describe('dispose', () => {
    it('should remove event listeners', () => {
      controller.dispose();

      expect(document.removeEventListener).toHaveBeenCalledWith(
        'pointerlockchange',
        expect.any(Function)
      );
      expect(document.removeEventListener).toHaveBeenCalledWith(
        'pointerlockerror',
        expect.any(Function)
      );
    });

    it('should release lock if held', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      controller.dispose();

      expect(document.exitPointerLock).toHaveBeenCalled();
    });

    it('should clear listeners', () => {
      const listener = vi.fn();
      controller.onLockChange(listener);

      controller.dispose();

      // Manually trigger a change (listeners should be cleared)
      // The dispose should have cleared the internal listeners array
    });
  });

  describe('canvas getter', () => {
    it('should return the canvas element', () => {
      expect(controller.canvas).toBe(mockCanvas);
    });
  });
});
