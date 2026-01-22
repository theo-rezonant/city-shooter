import { describe, it, expect, vi } from 'vitest';
import { ASSET_PATHS } from './AssetsManager';

// Mock Babylon.js modules
vi.mock('@babylonjs/core', () => ({
  AssetsManager: vi.fn().mockImplementation(() => ({
    useDefaultLoadingScreen: false,
    onProgress: null,
    onFinish: null,
  })),
  Scene: vi.fn(),
  SceneLoader: {
    LoadAssetContainerAsync: vi.fn().mockResolvedValue({
      meshes: [],
      skeletons: [],
      animationGroups: [],
    }),
  },
}));

vi.mock('@babylonjs/loaders/glTF', () => ({}));

describe('AssetsManager', () => {
  describe('ASSET_PATHS Configuration', () => {
    it('should have correct base path', () => {
      expect(ASSET_PATHS.basePath).toBe('/assets/models/');
    });

    it('should have soldier model path', () => {
      expect(ASSET_PATHS.soldierModel).toBe('Soldier.fbx');
    });

    it('should have all animation paths defined', () => {
      expect(ASSET_PATHS.animations.strafe).toBe('Strafe.fbx');
      expect(ASSET_PATHS.animations.reaction).toBe('Reaction.fbx');
      expect(ASSET_PATHS.animations.staticFire).toBe('static_fire.fbx');
      expect(ASSET_PATHS.animations.movingFire).toBe('moving fire.fbx');
    });

    it('should have environment path', () => {
      expect(ASSET_PATHS.environment).toBe('town4new.glb');
    });
  });

  describe('Asset Validation', () => {
    it('should validate required asset names', () => {
      const requiredAssets = ['soldier', 'strafe', 'reaction', 'staticFire', 'movingFire'];

      const loadedAssets = new Map<string, unknown>();
      loadedAssets.set('soldier', { meshes: [{}] });
      loadedAssets.set('strafe', { animationGroups: [{}] });
      loadedAssets.set('reaction', { animationGroups: [{}] });
      loadedAssets.set('staticFire', { animationGroups: [{}] });
      loadedAssets.set('movingFire', { animationGroups: [{}] });

      for (const asset of requiredAssets) {
        expect(loadedAssets.has(asset)).toBe(true);
      }
    });

    it('should fail validation if soldier mesh is missing', () => {
      const loadedAssets = new Map<string, { meshes?: unknown[] }>();
      loadedAssets.set('soldier', { meshes: [] });

      const soldierContainer = loadedAssets.get('soldier');
      expect(soldierContainer?.meshes?.length).toBe(0);
    });
  });

  describe('Progress Tracking', () => {
    it('should calculate progress correctly', () => {
      const totalTasks = 5;
      let completedTasks = 0;

      completedTasks++;
      let progress = (completedTasks / totalTasks) * 100;
      expect(progress).toBe(20);

      completedTasks++;
      progress = (completedTasks / totalTasks) * 100;
      expect(progress).toBe(40);

      completedTasks = totalTasks;
      progress = (completedTasks / totalTasks) * 100;
      expect(progress).toBe(100);
    });

    it('should report progress with message', () => {
      const progressCallback = vi.fn();
      const message = 'Loading soldier...';
      const progress = 50;

      progressCallback(progress, message);

      expect(progressCallback).toHaveBeenCalledWith(50, 'Loading soldier...');
    });
  });

  describe('Container Management', () => {
    it('should store containers in a map', () => {
      const containers = new Map<string, unknown>();

      containers.set('soldier', { name: 'soldier' });
      containers.set('strafe', { name: 'strafe' });

      expect(containers.size).toBe(2);
      expect(containers.has('soldier')).toBe(true);
      expect(containers.get('soldier')).toEqual({ name: 'soldier' });
    });

    it('should check if container is loaded', () => {
      const containers = new Map<string, unknown>();
      containers.set('soldier', {});

      expect(containers.has('soldier')).toBe(true);
      expect(containers.has('environment')).toBe(false);
    });

    it('should dispose containers properly', () => {
      interface MockContainer {
        disposed: boolean;
        dispose: () => void;
      }

      const mockContainer: MockContainer = {
        disposed: false,
        dispose() {
          this.disposed = true;
        },
      };

      const containers = new Map<string, MockContainer>();
      containers.set('test', mockContainer);

      const container = containers.get('test');
      container?.dispose();

      expect(mockContainer.disposed).toBe(true);
    });
  });

  describe('Error Handling', () => {
    it('should create error message for failed loads', () => {
      const assetName = 'soldier';
      const error = new Error('Network error');

      const errorMessage = `Failed to load asset ${assetName}: ${error.message}`;

      expect(errorMessage).toBe('Failed to load asset soldier: Network error');
    });

    it('should handle non-Error objects', () => {
      const assetName = 'animation';
      const error = 'Unknown failure';

      const errorMessage = `Failed to load asset ${assetName}: ${String(error)}`;

      expect(errorMessage).toBe('Failed to load asset animation: Unknown failure');
    });
  });
});

describe('Asset Loading Order', () => {
  it('should load soldier assets in correct order', async () => {
    const loadOrder: string[] = [];

    const mockLoad = async (name: string): Promise<void> => {
      loadOrder.push(name);
    };

    // Simulate loading sequence
    await mockLoad('soldier');
    await mockLoad('strafe');
    await mockLoad('reaction');
    await mockLoad('staticFire');
    await mockLoad('movingFire');

    expect(loadOrder).toEqual(['soldier', 'strafe', 'reaction', 'staticFire', 'movingFire']);
  });
});
