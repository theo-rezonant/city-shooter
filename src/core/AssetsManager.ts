import { AssetsManager as BabylonAssetsManager, Scene, SceneLoader } from '@babylonjs/core';
import type { AssetContainer } from '@babylonjs/core';
import '@babylonjs/loaders/glTF';

// Note: FBX loading requires additional setup. Babylon.js natively supports GLB/GLTF.
// For FBX files, they should be converted to GLB format for production use.
// During development, we'll load them through the SceneLoader which attempts
// to handle various formats.

import type { LoadingProgressCallback } from '../types';

/**
 * Asset paths configuration
 */
export const ASSET_PATHS = {
  basePath: '/assets/models/',
  soldierModel: 'Soldier.fbx',
  animations: {
    strafe: 'Strafe.fbx',
    reaction: 'Reaction.fbx',
    staticFire: 'static_fire.fbx',
    movingFire: 'moving fire.fbx',
  },
  environment: 'town4new.glb',
} as const;

/**
 * Manages loading of all game assets using Babylon.js AssetsManager.
 * Provides progress tracking and validation of loaded assets.
 */
export class AssetsManagerService {
  private scene: Scene;
  private assetsManager: BabylonAssetsManager;
  private loadedContainers: Map<string, AssetContainer> = new Map();
  private progressCallback: LoadingProgressCallback | null = null;
  private totalTasks = 0;
  private completedTasks = 0;

  constructor(scene: Scene) {
    this.scene = scene;
    this.assetsManager = new BabylonAssetsManager(scene);
    this.setupAssetsManager();
  }

  /**
   * Configure the AssetsManager with progress and error handling
   */
  private setupAssetsManager(): void {
    this.assetsManager.useDefaultLoadingScreen = false;

    this.assetsManager.onProgress = (
      remainingCount: number,
      totalCount: number,
      lastFinishedTask
    ): void => {
      const progress = ((totalCount - remainingCount) / totalCount) * 100;
      const taskName = lastFinishedTask?.name || 'Unknown';
      this.reportProgress(progress, `Loaded: ${taskName}`);
    };

    this.assetsManager.onFinish = (): void => {
      this.reportProgress(100, 'All assets loaded');
    };
  }

  /**
   * Set the progress callback for loading updates
   */
  public setProgressCallback(callback: LoadingProgressCallback): void {
    this.progressCallback = callback;
  }

  /**
   * Report loading progress
   */
  private reportProgress(progress: number, message: string): void {
    if (this.progressCallback) {
      this.progressCallback(progress, message);
    }
  }

  /**
   * Load a container asset (GLB/GLTF/FBX)
   */
  public async loadContainerAsync(
    name: string,
    rootUrl: string,
    fileName: string
  ): Promise<AssetContainer> {
    this.reportProgress(0, `Loading ${name}...`);

    try {
      const container = await SceneLoader.LoadAssetContainerAsync(
        rootUrl,
        fileName,
        this.scene,
        (event) => {
          if (event.lengthComputable) {
            const progress = (event.loaded / event.total) * 100;
            this.reportProgress(progress, `Loading ${name}: ${Math.round(progress)}%`);
          }
        }
      );

      this.loadedContainers.set(name, container);
      this.completedTasks++;
      this.reportProgress(
        (this.completedTasks / this.totalTasks) * 100,
        `Loaded ${name} successfully`
      );

      return container;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      console.error(`Failed to load ${name}:`, errorMessage);
      throw new Error(`Failed to load asset ${name}: ${errorMessage}`);
    }
  }

  /**
   * Load all soldier-related assets
   */
  public async loadSoldierAssetsAsync(): Promise<Map<string, AssetContainer>> {
    this.totalTasks = 5; // Soldier + 4 animations
    this.completedTasks = 0;

    const basePath = ASSET_PATHS.basePath;

    // Load soldier model
    await this.loadContainerAsync('soldier', basePath, ASSET_PATHS.soldierModel);

    // Load all animation files
    await this.loadContainerAsync('strafe', basePath, ASSET_PATHS.animations.strafe);
    await this.loadContainerAsync('reaction', basePath, ASSET_PATHS.animations.reaction);
    await this.loadContainerAsync('staticFire', basePath, ASSET_PATHS.animations.staticFire);
    await this.loadContainerAsync('movingFire', basePath, ASSET_PATHS.animations.movingFire);

    return this.loadedContainers;
  }

  /**
   * Load the environment/map
   */
  public async loadEnvironmentAsync(): Promise<AssetContainer> {
    this.totalTasks = 1;
    this.completedTasks = 0;

    return this.loadContainerAsync('environment', ASSET_PATHS.basePath, ASSET_PATHS.environment);
  }

  /**
   * Get a loaded container by name
   */
  public getContainer(name: string): AssetContainer | undefined {
    return this.loadedContainers.get(name);
  }

  /**
   * Check if an asset is loaded
   */
  public isLoaded(name: string): boolean {
    return this.loadedContainers.has(name);
  }

  /**
   * Validate that all required soldier assets are loaded
   */
  public validateSoldierAssets(): boolean {
    const requiredAssets = ['soldier', 'strafe', 'reaction', 'staticFire', 'movingFire'];

    for (const asset of requiredAssets) {
      if (!this.isLoaded(asset)) {
        console.error(`Missing required asset: ${asset}`);
        return false;
      }

      const container = this.getContainer(asset);
      if (!container) {
        console.error(`Asset container is null: ${asset}`);
        return false;
      }

      // Validate soldier has meshes
      if (asset === 'soldier' && container.meshes.length === 0) {
        console.error('Soldier container has no meshes');
        return false;
      }

      // Validate animations have animation groups
      if (asset !== 'soldier' && container.animationGroups.length === 0) {
        console.warn(`Animation asset ${asset} has no animation groups`);
        // This is a warning, not an error, as the animations might be in a different format
      }
    }

    return true;
  }

  /**
   * Dispose of all loaded assets
   */
  public dispose(): void {
    for (const [name, container] of this.loadedContainers) {
      try {
        container.dispose();
      } catch (error) {
        console.warn(`Failed to dispose container ${name}:`, error);
      }
    }
    this.loadedContainers.clear();
  }
}
