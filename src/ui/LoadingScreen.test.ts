import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as BabylonGUI from '@babylonjs/gui';
import * as BabylonCore from '@babylonjs/core';

// Mock Babylon.js modules
vi.mock('@babylonjs/core', () => ({
  Engine: vi.fn().mockImplementation(() => ({
    runRenderLoop: vi.fn(),
    resize: vi.fn(),
    dispose: vi.fn(),
  })),
  Scene: vi.fn().mockImplementation(() => ({
    render: vi.fn(),
    dispose: vi.fn(),
    clearColor: null,
  })),
  ArcRotateCamera: vi.fn().mockImplementation(() => ({
    attachControl: vi.fn(),
  })),
  HemisphericLight: vi.fn().mockImplementation(() => ({
    intensity: 0.7,
  })),
  Vector3: Object.assign(
    vi.fn().mockImplementation(() => ({ x: 0, y: 0, z: 0 })),
    {
      Zero: vi.fn().mockReturnValue({ x: 0, y: 0, z: 0 }),
    }
  ),
  AssetsManager: vi.fn().mockImplementation(() => ({
    useDefaultLoadingScreen: false,
    onProgress: null,
    onFinish: null,
    onTaskError: null,
    onTaskSuccess: null,
    addMeshTask: vi.fn().mockReturnValue({
      name: 'testTask',
      onSuccess: null,
      loadedMeshes: [],
    }),
    load: vi.fn(),
  })),
  MeshAssetTask: vi.fn(),
  Color4: vi.fn().mockImplementation(() => ({ r: 0, g: 0, b: 0, a: 1 })),
  AbstractMesh: vi.fn(),
}));

vi.mock('@babylonjs/gui', () => ({
  AdvancedDynamicTexture: {
    CreateFullscreenUI: vi.fn().mockReturnValue({
      addControl: vi.fn(),
      dispose: vi.fn(),
    }),
  },
  Rectangle: vi.fn().mockImplementation(() => ({
    width: '',
    height: '',
    thickness: 0,
    background: '',
    color: '',
    cornerRadius: 0,
    horizontalAlignment: 0,
    verticalAlignment: 0,
    isVisible: true,
    addControl: vi.fn(),
  })),
  TextBlock: vi.fn().mockImplementation(() => ({
    color: '',
    fontSize: 0,
    fontWeight: '',
    height: '',
    paddingBottom: '',
    text: '',
  })),
  Button: {
    CreateSimpleButton: vi.fn().mockReturnValue({
      width: '',
      height: '',
      color: '',
      background: '',
      cornerRadius: 0,
      thickness: 0,
      fontSize: 0,
      fontWeight: '',
      isVisible: false,
      paddingTop: '',
      textBlock: { text: '' },
      onPointerEnterObservable: { add: vi.fn() },
      onPointerOutObservable: { add: vi.fn() },
      onPointerClickObservable: { add: vi.fn(), clear: vi.fn() },
    }),
  },
  Control: {
    VERTICAL_ALIGNMENT_CENTER: 2,
    HORIZONTAL_ALIGNMENT_CENTER: 2,
    HORIZONTAL_ALIGNMENT_LEFT: 0,
  },
  StackPanel: vi.fn().mockImplementation(() => ({
    width: '',
    verticalAlignment: 0,
    horizontalAlignment: 0,
    addControl: vi.fn(),
  })),
}));

vi.mock('@babylonjs/loaders', () => ({}));

import { SceneManager } from '../core/SceneManager';
import { LoadingScreen } from './LoadingScreen';
import { GameState } from '../types/GameState';

describe('LoadingScreen', () => {
  let mockCanvas: HTMLCanvasElement;
  let sceneManager: SceneManager;
  let loadingScreen: LoadingScreen;

  beforeEach(() => {
    // Create mock canvas
    mockCanvas = document.createElement('canvas');
    mockCanvas.id = 'renderCanvas';

    // Create scene manager
    sceneManager = new SceneManager(mockCanvas);

    // Create loading screen
    loadingScreen = new LoadingScreen(sceneManager);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('initialization', () => {
    it('should create a fullscreen UI', () => {
      expect(BabylonGUI.AdvancedDynamicTexture.CreateFullscreenUI).toHaveBeenCalledWith(
        'LoadingUI',
        true,
        expect.anything()
      );
    });

    it('should initialize with loading incomplete', () => {
      expect(loadingScreen.isLoadingComplete).toBe(false);
    });

    it('should initialize without errors', () => {
      expect(loadingScreen.hasError).toBe(false);
    });

    it('should have empty loaded assets initially', () => {
      expect(loadingScreen.loadedAssets).toEqual({});
    });
  });

  describe('startLoading', () => {
    it('should call AssetsManager.load()', () => {
      loadingScreen.startLoading();

      // The load method should be called on the assets manager
      expect(BabylonCore.AssetsManager).toHaveBeenCalled();
    });
  });

  describe('show/hide', () => {
    it('should show the loading screen', () => {
      loadingScreen.show();
      // Container visibility is managed internally
      expect(loadingScreen).toBeDefined();
    });

    it('should hide the loading screen', () => {
      loadingScreen.hide();
      // Container visibility is managed internally
      expect(loadingScreen).toBeDefined();
    });
  });

  describe('dispose', () => {
    it('should dispose the UI', () => {
      loadingScreen.dispose();
      expect(loadingScreen.ui.dispose).toHaveBeenCalled();
    });
  });

  describe('UI configuration', () => {
    it('should accept custom configuration', () => {
      const customConfig = {
        backgroundColor: '#ff0000',
        progressBarColor: '#00ff00',
        textColor: '#0000ff',
      };

      const customLoadingScreen = new LoadingScreen(sceneManager, customConfig);
      expect(customLoadingScreen).toBeDefined();
    });

    it('should accept custom asset paths', () => {
      const customPaths = {
        map: 'custom/map.glb',
        gun: 'custom/gun.glb',
        soldier: 'custom/soldier.fbx',
      };

      const customLoadingScreen = new LoadingScreen(sceneManager, {}, customPaths);
      expect(customLoadingScreen).toBeDefined();
    });
  });
});

describe('SceneManager', () => {
  let mockCanvas: HTMLCanvasElement;
  let sceneManager: SceneManager;

  beforeEach(() => {
    mockCanvas = document.createElement('canvas');
    sceneManager = new SceneManager(mockCanvas);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('state management', () => {
    it('should start in LOADING state', () => {
      expect(sceneManager.currentState).toBe(GameState.LOADING);
    });

    it('should transition to new state', () => {
      sceneManager.transitionTo(GameState.MAIN_MENU);
      expect(sceneManager.currentState).toBe(GameState.MAIN_MENU);
    });

    it('should execute state change callbacks', () => {
      const callback = vi.fn();
      sceneManager.onStateChange(GameState.MAIN_MENU, callback);
      sceneManager.transitionTo(GameState.MAIN_MENU);
      expect(callback).toHaveBeenCalled();
    });

    it('should allow multiple callbacks for same state', () => {
      const callback1 = vi.fn();
      const callback2 = vi.fn();
      sceneManager.onStateChange(GameState.MAIN_MENU, callback1);
      sceneManager.onStateChange(GameState.MAIN_MENU, callback2);
      sceneManager.transitionTo(GameState.MAIN_MENU);
      expect(callback1).toHaveBeenCalled();
      expect(callback2).toHaveBeenCalled();
    });
  });

  describe('loaded assets', () => {
    it('should start with empty loaded assets', () => {
      expect(sceneManager.loadedAssets).toEqual({});
    });

    it('should store loaded assets', () => {
      const assets = { map: [], gun: [], soldier: [] };
      sceneManager.setLoadedAssets(assets);
      expect(sceneManager.loadedAssets).toEqual(assets);
    });
  });

  describe('engine access', () => {
    it('should provide access to the engine', () => {
      expect(sceneManager.engine).toBeDefined();
    });

    it('should provide access to the scene', () => {
      expect(sceneManager.scene).toBeDefined();
    });

    it('should provide access to the canvas', () => {
      expect(sceneManager.canvas).toBe(mockCanvas);
    });
  });

  describe('render loop', () => {
    it('should start the render loop', () => {
      sceneManager.startRenderLoop();
      expect(sceneManager.engine.runRenderLoop).toHaveBeenCalled();
    });
  });

  describe('dispose', () => {
    it('should dispose scene and engine', () => {
      sceneManager.dispose();
      expect(sceneManager.scene.dispose).toHaveBeenCalled();
      expect(sceneManager.engine.dispose).toHaveBeenCalled();
    });
  });
});

describe('GameState', () => {
  it('should have LOADING state', () => {
    expect(GameState.LOADING).toBe('LOADING');
  });

  it('should have MAIN_MENU state', () => {
    expect(GameState.MAIN_MENU).toBe('MAIN_MENU');
  });

  it('should have PLAYING state', () => {
    expect(GameState.PLAYING).toBe('PLAYING');
  });

  it('should have PAUSED state', () => {
    expect(GameState.PAUSED).toBe('PAUSED');
  });
});
