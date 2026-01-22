import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { GameController } from '../src/controllers/GameController';
import { GameState } from '../src/states/GameState';
import { Engine, Scene, UniversalCamera } from '@babylonjs/core';
import { AdvancedDynamicTexture, Button } from '@babylonjs/gui';

// Mock Babylon.js core modules
vi.mock('@babylonjs/core', () => {
  const mockAudioContext = {
    state: 'suspended',
    resume: vi.fn().mockResolvedValue(undefined),
    currentTime: 0,
    listener: {
      positionX: { setValueAtTime: vi.fn() },
      positionY: { setValueAtTime: vi.fn() },
      positionZ: { setValueAtTime: vi.fn() },
      forwardX: { setValueAtTime: vi.fn() },
      forwardY: { setValueAtTime: vi.fn() },
      forwardZ: { setValueAtTime: vi.fn() },
      upX: { setValueAtTime: vi.fn() },
      upY: { setValueAtTime: vi.fn() },
      upZ: { setValueAtTime: vi.fn() },
    },
  };

  const mockAudioEngine = {
    unlock: vi.fn().mockResolvedValue(undefined),
    audioContext: mockAudioContext,
    setGlobalVolume: vi.fn(),
  };

  return {
    Engine: {
      audioEngine: mockAudioEngine,
    },
    Scene: vi.fn().mockImplementation(() => ({
      onBeforeRenderObservable: {
        add: vi.fn().mockReturnValue({ remove: vi.fn() }),
        remove: vi.fn(),
      },
    })),
    UniversalCamera: vi.fn().mockImplementation(() => ({
      position: { x: 0, y: 0, z: 0 },
      upVector: { x: 0, y: 1, z: 0 },
      getDirection: vi.fn().mockReturnValue({ x: 0, y: 0, z: 1 }),
      attachControl: vi.fn(),
      detachControl: vi.fn(),
    })),
    Vector3: {
      Forward: vi.fn().mockReturnValue({ x: 0, y: 0, z: 1 }),
    },
    Sound: vi.fn().mockImplementation((name) => ({
      name,
      play: vi.fn(),
      stop: vi.fn(),
      pause: vi.fn(),
      dispose: vi.fn(),
      setPosition: vi.fn(),
      setVolume: vi.fn(),
      switchPanningModelToHRTF: vi.fn(),
    })),
  };
});

// Mock Babylon.js GUI modules
vi.mock('@babylonjs/gui', () => {
  const mockButton = {
    width: '',
    height: '',
    color: '',
    background: '',
    cornerRadius: 0,
    fontSize: 0,
    fontWeight: '',
    hoverCursor: '',
    horizontalAlignment: 0,
    verticalAlignment: 0,
    isVisible: true,
    textBlock: { text: '', color: '', fontSize: 0 },
    onPointerUpObservable: { add: vi.fn() },
    onPointerEnterObservable: { add: vi.fn() },
    onPointerOutObservable: { add: vi.fn() },
  };

  return {
    AdvancedDynamicTexture: {
      CreateFullscreenUI: vi.fn().mockReturnValue({
        addControl: vi.fn(),
        dispose: vi.fn(),
      }),
    },
    Button: {
      CreateSimpleButton: vi.fn().mockReturnValue({ ...mockButton }),
    },
    Control: {
      HORIZONTAL_ALIGNMENT_CENTER: 2,
      VERTICAL_ALIGNMENT_CENTER: 2,
    },
  };
});

describe('GameController', () => {
  let controller: GameController;
  let mockEngine: typeof Engine;
  let mockCanvas: HTMLCanvasElement;
  let mockScene: Scene;
  let mockCamera: UniversalCamera;
  let documentEventListeners: Map<string, EventListener>;

  beforeEach(() => {
    // Store event listeners
    documentEventListeners = new Map();

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

    mockCanvas = {
      requestPointerLock: vi.fn().mockResolvedValue(undefined),
    } as unknown as HTMLCanvasElement;

    mockEngine = Engine;
    mockScene = new Scene(null as any);
    mockCamera = new UniversalCamera('camera', null as any, null as any);

    controller = new GameController(mockEngine as any, mockCanvas);
  });

  afterEach(() => {
    controller.dispose();
    vi.clearAllMocks();

    Object.defineProperty(document, 'pointerLockElement', {
      value: null,
      configurable: true,
    });
  });

  describe('constructor', () => {
    it('should create a GameController instance', () => {
      expect(controller).toBeDefined();
    });

    it('should start in LOADING state', () => {
      expect(controller.getCurrentState()).toBe(GameState.LOADING);
    });

    it('should have a pointer lock controller', () => {
      expect(controller.pointerLockController).toBeDefined();
    });
  });

  describe('initialize', () => {
    it('should create AudioManager', () => {
      controller.initialize(mockScene, mockCamera);

      expect(controller.audioManager).toBeDefined();
    });

    it('should create GUI texture', () => {
      controller.initialize(mockScene, mockCamera);

      expect(AdvancedDynamicTexture.CreateFullscreenUI).toHaveBeenCalledWith(
        'gameUI',
        true,
        mockScene
      );
    });

    it('should transition to MAIN_MENU state', () => {
      controller.initialize(mockScene, mockCamera);

      expect(controller.getCurrentState()).toBe(GameState.MAIN_MENU);
    });
  });

  describe('createEnterGameButton', () => {
    beforeEach(() => {
      controller.initialize(mockScene, mockCamera);
    });

    it('should create a button', () => {
      const button = controller.createEnterGameButton();

      expect(Button.CreateSimpleButton).toHaveBeenCalledWith(
        'enterGameBtn',
        'Enter Game'
      );
      expect(button).toBeDefined();
    });

    it('should add button to GUI', () => {
      controller.createEnterGameButton();

      expect(controller.guiTexture?.addControl).toHaveBeenCalled();
    });

    it('should throw if not initialized', () => {
      const uninitializedController = new GameController(mockEngine as any, mockCanvas);

      expect(() => uninitializedController.createEnterGameButton()).toThrow(
        'GameController: GUI not initialized'
      );

      uninitializedController.dispose();
    });

    it('should set up click handler that unlocks audio and requests pointer lock', async () => {
      controller.createEnterGameButton();

      // Get the click handler that was registered
      const buttonMock = Button.CreateSimpleButton('', '');
      const clickHandler = buttonMock.onPointerUpObservable.add.mock.calls[0][0];

      // Simulate click
      await clickHandler();

      expect(Engine.audioEngine!.unlock).toHaveBeenCalled();
      expect(mockCanvas.requestPointerLock).toHaveBeenCalled();
    });
  });

  describe('state transitions', () => {
    beforeEach(() => {
      controller.initialize(mockScene, mockCamera);
    });

    it('should transition between states', () => {
      controller.transitionTo(GameState.GAMEPLAY);
      expect(controller.getCurrentState()).toBe(GameState.GAMEPLAY);

      controller.transitionTo(GameState.PAUSED);
      expect(controller.getCurrentState()).toBe(GameState.PAUSED);

      controller.transitionTo(GameState.MAIN_MENU);
      expect(controller.getCurrentState()).toBe(GameState.MAIN_MENU);
    });

    it('should not transition to same state', () => {
      const callback = vi.fn();
      controller.onStateChange(callback);

      controller.transitionTo(GameState.MAIN_MENU);

      // Should not have been called since we're already in MAIN_MENU
      expect(callback).not.toHaveBeenCalled();
    });

    it('should notify state change listeners', () => {
      const callback = vi.fn();
      controller.onStateChange(callback);

      controller.transitionTo(GameState.GAMEPLAY);

      expect(callback).toHaveBeenCalledWith(GameState.GAMEPLAY, GameState.MAIN_MENU);
    });
  });

  describe('ISceneManager implementation', () => {
    beforeEach(() => {
      controller.initialize(mockScene, mockCamera);
    });

    it('should return current state', () => {
      expect(controller.getCurrentState()).toBe(GameState.MAIN_MENU);
    });

    it('should return active camera', () => {
      expect(controller.getActiveCamera()).toBe(mockCamera);
    });

    it('should return scene', () => {
      expect(controller.getScene()).toBe(mockScene);
    });

    it('should return engine', () => {
      expect(controller.getEngine()).toBe(mockEngine);
    });
  });

  describe('pointer lock integration', () => {
    beforeEach(() => {
      controller.initialize(mockScene, mockCamera);
    });

    it('should transition to GAMEPLAY when pointer lock is acquired', () => {
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });

      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(controller.getCurrentState()).toBe(GameState.GAMEPLAY);
    });

    it('should transition to PAUSED when pointer lock is released during gameplay', () => {
      // First enter gameplay
      Object.defineProperty(document, 'pointerLockElement', {
        value: mockCanvas,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(controller.getCurrentState()).toBe(GameState.GAMEPLAY);

      // Then release lock (simulate Esc key)
      Object.defineProperty(document, 'pointerLockElement', {
        value: null,
        configurable: true,
      });
      documentEventListeners.get('pointerlockchange')?.({} as Event);

      expect(controller.getCurrentState()).toBe(GameState.PAUSED);
    });
  });

  describe('dispose', () => {
    it('should dispose all resources', () => {
      controller.initialize(mockScene, mockCamera);

      // Verify audioManager was created before disposing
      expect(controller.audioManager).toBeDefined();

      controller.dispose();

      expect(document.removeEventListener).toHaveBeenCalled();
      expect(controller.getScene()).toBeNull();
      expect(controller.getActiveCamera()).toBeNull();
    });
  });

  describe('createPauseOverlay', () => {
    beforeEach(() => {
      controller.initialize(mockScene, mockCamera);
    });

    it('should create a pause overlay', () => {
      const overlay = controller.createPauseOverlay();

      expect(overlay).toBeDefined();
    });

    it('should throw if not initialized', () => {
      const uninitializedController = new GameController(mockEngine as any, mockCanvas);

      expect(() => uninitializedController.createPauseOverlay()).toThrow(
        'GameController: GUI not initialized'
      );

      uninitializedController.dispose();
    });
  });
});
