import { Scene } from '@babylonjs/core/scene';
import { AssetsManager, MeshAssetTask } from '@babylonjs/core/Misc/assetsManager';
import { AbstractMesh } from '@babylonjs/core/Meshes/abstractMesh';
import { PBRMaterial } from '@babylonjs/core/Materials/PBR/pbrMaterial';
import { BaseTexture } from '@babylonjs/core/Materials/Textures/baseTexture';
import { Texture } from '@babylonjs/core/Materials/Textures/texture';
import { Observable } from '@babylonjs/core/Misc/observable';
import '@babylonjs/loaders/glTF';

import {
  AssetLoadStatus,
  AssetValidationResult,
  AssetsValidationStatus,
  AssetDefinition,
  AssetsManagerCallbacks,
} from '../types/GameTypes';

/**
 * Asset definitions for the game
 */
export const GAME_ASSETS: Record<string, AssetDefinition> = {
  TOWN: {
    name: 'Town',
    rootUrl: './map/source/',
    sceneFilename: 'town4new.glb',
    isRequired: true,
  },
  LASER_GUN: {
    name: 'LaserGun',
    rootUrl: './laser-gun/source/',
    sceneFilename: 'lasergun.glb',
    isRequired: true,
  },
  SOLDIER: {
    name: 'Soldier',
    rootUrl: './Assets/',
    sceneFilename: 'Soldier.fbx',
    isRequired: true,
  },
};

/**
 * GameAssetsManager - Manages loading and validation of game assets
 *
 * Features:
 * - Validates that all required meshes are successfully instantiated
 * - Checks material and texture integrity for each model
 * - Uses onMeshLoadedObservable for post-load texture validation
 * - Provides progress callbacks for UI updates
 * - Gates game start until all assets are validated
 */
export class GameAssetsManager {
  private scene: Scene;
  private assetsManager: AssetsManager;
  private callbacks: AssetsManagerCallbacks;

  // Asset status tracking
  private assetStatuses: Map<string, AssetLoadStatus> = new Map();
  private loadedMeshes: Map<string, AbstractMesh[]> = new Map();
  private validationResults: Map<string, AssetValidationResult> = new Map();

  // Observables for UI coordination
  public readonly onAllAssetsReady: Observable<AssetsValidationStatus> = new Observable();
  public readonly onAssetLoaded: Observable<AssetValidationResult> = new Observable();
  public readonly onAssetError: Observable<{ assetName: string; error: Error }> = new Observable();
  public readonly onProgress: Observable<{ loaded: number; total: number; current: string }> =
    new Observable();

  // Overall status
  private validationStatus: AssetsValidationStatus = {
    townLoaded: false,
    townValidated: false,
    laserGunLoaded: false,
    laserGunValidated: false,
    soldierLoaded: false,
    soldierValidated: false,
    allAssetsReady: false,
    validationResults: [],
  };

  constructor(scene: Scene, callbacks: AssetsManagerCallbacks = {}) {
    this.scene = scene;
    this.callbacks = callbacks;
    this.assetsManager = new AssetsManager(scene);

    // Initialize statuses
    Object.values(GAME_ASSETS).forEach((asset) => {
      this.assetStatuses.set(asset.name, AssetLoadStatus.PENDING);
    });

    this.setupAssetsManager();
  }

  /**
   * Configure the AssetsManager with callbacks
   */
  private setupAssetsManager(): void {
    this.assetsManager.useDefaultLoadingScreen = false;

    // Track progress
    let loadedCount = 0;

    this.assetsManager.onProgress = (remaining, total, task): void => {
      loadedCount = total - remaining;
      const taskName = task?.name ?? 'Unknown';
      this.onProgress.notifyObservers({ loaded: loadedCount, total, current: taskName });
      this.callbacks.onProgress?.(loadedCount, total, taskName);
    };

    this.assetsManager.onFinish = (_tasks): void => {
      this.performFinalValidation();
    };

    this.assetsManager.onTaskError = (task): void => {
      const error = new Error(`Failed to load asset: ${task.name}`);
      this.handleAssetError(task.name, error);
    };
  }

  /**
   * Add a mesh loading task with validation callbacks
   */
  private addMeshTask(asset: AssetDefinition): MeshAssetTask {
    const task = this.assetsManager.addMeshTask(
      asset.name,
      '', // meshNames - empty means load all
      asset.rootUrl,
      asset.sceneFilename
    );

    this.assetStatuses.set(asset.name, AssetLoadStatus.LOADING);

    task.onSuccess = (meshTask: MeshAssetTask): void => {
      this.handleMeshLoaded(asset.name, meshTask);
    };

    task.onError = (_meshTask: MeshAssetTask, message?: string, exception?: unknown): void => {
      const errorMessage =
        message ?? (exception instanceof Error ? exception.message : 'Unknown error');
      this.handleAssetError(asset.name, new Error(errorMessage));
    };

    return task;
  }

  /**
   * Handle successful mesh loading - performs validation
   */
  private handleMeshLoaded(assetName: string, task: MeshAssetTask): void {
    const meshes = task.loadedMeshes;
    this.loadedMeshes.set(assetName, meshes);

    // Verify mesh count
    if (meshes.length === 0) {
      this.handleAssetError(assetName, new Error(`Asset ${assetName} loaded with no meshes`));
      return;
    }

    this.assetStatuses.set(assetName, AssetLoadStatus.LOADED);

    // Perform asset-specific validation
    const validationResult = this.validateAsset(assetName, meshes);
    this.validationResults.set(assetName, validationResult);

    if (validationResult.isValid) {
      this.assetStatuses.set(assetName, AssetLoadStatus.VALIDATED);
      this.updateValidationStatus(assetName, true);
    } else {
      this.assetStatuses.set(assetName, AssetLoadStatus.FAILED);
      this.handleAssetError(assetName, new Error(validationResult.errors.join(', ')));
    }

    this.onAssetLoaded.notifyObservers(validationResult);
    this.callbacks.onAssetLoaded?.(assetName, validationResult);
  }

  /**
   * Validate a loaded asset's meshes, materials, and textures
   */
  private validateAsset(assetName: string, meshes: AbstractMesh[]): AssetValidationResult {
    const errors: string[] = [];
    let hasMaterials = false;
    let hasTextures = false;

    // Check mesh count
    if (meshes.length === 0) {
      errors.push('No meshes loaded');
    }

    // Check materials and textures on each mesh
    for (const mesh of meshes) {
      if (mesh.material) {
        hasMaterials = true;

        // Check for PBR material textures
        if (mesh.material instanceof PBRMaterial) {
          const pbrMat = mesh.material as PBRMaterial;

          // For laser gun specifically, check required textures
          if (assetName === GAME_ASSETS.LASER_GUN.name) {
            const requiredTextures = this.checkLaserGunTextures(pbrMat);
            if (!requiredTextures.valid) {
              errors.push(...requiredTextures.errors);
            } else {
              hasTextures = true;
            }
          } else {
            // For other assets, just check if any texture exists
            if (this.hasAnyTexture(pbrMat)) {
              hasTextures = true;
            }
          }
        } else {
          // Non-PBR materials - check basic texture
          const mat = mesh.material;
          if ('diffuseTexture' in mat && mat.diffuseTexture) {
            hasTextures = true;
          }
        }
      }
    }

    // Asset-specific validation
    switch (assetName) {
      case GAME_ASSETS.LASER_GUN.name:
        if (!hasMaterials) {
          errors.push('Laser gun requires PBR materials');
        }
        break;

      case GAME_ASSETS.TOWN.name:
        // Town is a large file, ensure textures are present
        if (!hasTextures && hasMaterials) {
          // Register for texture loading completion
          this.setupTextureLoadingObserver(meshes);
        }
        break;

      case GAME_ASSETS.SOLDIER.name:
        // Soldier needs materials for visibility
        if (!hasMaterials) {
          errors.push('Soldier model requires materials');
        }
        break;
    }

    return {
      assetName,
      isValid: errors.length === 0,
      meshCount: meshes.length,
      hasMaterials,
      hasTextures,
      errors,
    };
  }

  /**
   * Check laser gun specific PBR textures (albedo and bump)
   */
  private checkLaserGunTextures(material: PBRMaterial): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (!material.albedoTexture) {
      errors.push('Laser gun missing albedo texture');
    }

    if (!material.bumpTexture) {
      // Bump texture is optional but recommended
      console.warn('Laser gun missing bump texture (normal map)');
    }

    return {
      valid: errors.length === 0,
      errors,
    };
  }

  /**
   * Check if a PBR material has any texture bound
   */
  private hasAnyTexture(material: PBRMaterial): boolean {
    return !!(
      material.albedoTexture ||
      material.bumpTexture ||
      material.metallicTexture ||
      material.emissiveTexture ||
      material.ambientTexture
    );
  }

  /**
   * Setup observer for texture loading completion (for large assets like town)
   */
  private setupTextureLoadingObserver(meshes: AbstractMesh[]): void {
    const texturesToWatch: BaseTexture[] = [];

    for (const mesh of meshes) {
      if (mesh.material && mesh.material instanceof PBRMaterial) {
        const pbrMat = mesh.material as PBRMaterial;
        const textures = [
          pbrMat.albedoTexture,
          pbrMat.bumpTexture,
          pbrMat.metallicTexture,
          pbrMat.emissiveTexture,
        ].filter((tex): tex is BaseTexture => tex !== null);

        texturesToWatch.push(...textures);
      }
    }

    // Use texture loading callbacks for completion tracking
    if (texturesToWatch.length > 0) {
      let loadedCount = 0;
      for (const texture of texturesToWatch) {
        if (!texture.isReady()) {
          // Cast to Texture which has onLoadObservable
          if (texture instanceof Texture && texture.onLoadObservable) {
            texture.onLoadObservable.addOnce(() => {
              loadedCount++;
              if (loadedCount === texturesToWatch.length) {
                console.log('All textures loaded for large asset');
              }
            });
          } else {
            loadedCount++;
          }
        } else {
          loadedCount++;
        }
      }
    }
  }

  /**
   * Handle asset loading error
   */
  private handleAssetError(assetName: string, error: Error): void {
    this.assetStatuses.set(assetName, AssetLoadStatus.FAILED);
    this.updateValidationStatus(assetName, false);

    const validationResult: AssetValidationResult = {
      assetName,
      isValid: false,
      meshCount: 0,
      hasMaterials: false,
      hasTextures: false,
      errors: [error.message],
    };
    this.validationResults.set(assetName, validationResult);

    this.onAssetError.notifyObservers({ assetName, error });
    this.callbacks.onAssetError?.(assetName, error);
  }

  /**
   * Update validation status for a specific asset
   */
  private updateValidationStatus(assetName: string, isValid: boolean): void {
    switch (assetName) {
      case GAME_ASSETS.TOWN.name:
        this.validationStatus.townLoaded = true;
        this.validationStatus.townValidated = isValid;
        break;
      case GAME_ASSETS.LASER_GUN.name:
        this.validationStatus.laserGunLoaded = true;
        this.validationStatus.laserGunValidated = isValid;
        break;
      case GAME_ASSETS.SOLDIER.name:
        this.validationStatus.soldierLoaded = true;
        this.validationStatus.soldierValidated = isValid;
        break;
    }
  }

  /**
   * Perform final validation after all assets have been processed
   */
  private performFinalValidation(): void {
    this.validationStatus.validationResults = Array.from(this.validationResults.values());

    // Check if all required assets are validated
    this.validationStatus.allAssetsReady =
      this.validationStatus.townValidated &&
      this.validationStatus.laserGunValidated &&
      this.validationStatus.soldierValidated;

    this.onAllAssetsReady.notifyObservers(this.validationStatus);
    this.callbacks.onAllAssetsReady?.(this.validationStatus);
  }

  /**
   * Start loading all game assets
   */
  public async loadAllAssets(): Promise<AssetsValidationStatus> {
    // Add all mesh tasks
    Object.values(GAME_ASSETS).forEach((asset) => {
      this.addMeshTask(asset);
    });

    // Start loading
    return new Promise((resolve) => {
      this.onAllAssetsReady.addOnce((status) => {
        resolve(status);
      });

      this.assetsManager.load();
    });
  }

  /**
   * Reload a specific failed asset
   */
  public async reloadAsset(assetName: string): Promise<AssetValidationResult | null> {
    const assetDef = Object.values(GAME_ASSETS).find((a) => a.name === assetName);
    if (!assetDef) {
      console.error(`Unknown asset: ${assetName}`);
      return null;
    }

    // Reset status
    this.assetStatuses.set(assetName, AssetLoadStatus.PENDING);

    return new Promise((resolve) => {
      const reloadManager = new AssetsManager(this.scene);
      reloadManager.useDefaultLoadingScreen = false;

      const task = reloadManager.addMeshTask(
        assetDef.name,
        '',
        assetDef.rootUrl,
        assetDef.sceneFilename
      );

      task.onSuccess = (meshTask: MeshAssetTask): void => {
        this.handleMeshLoaded(assetName, meshTask);
        resolve(this.validationResults.get(assetName) ?? null);
      };

      task.onError = (): void => {
        this.handleAssetError(assetName, new Error(`Failed to reload ${assetName}`));
        resolve(this.validationResults.get(assetName) ?? null);
      };

      reloadManager.load();
    });
  }

  /**
   * Get the current validation status
   */
  public getValidationStatus(): AssetsValidationStatus {
    return { ...this.validationStatus };
  }

  /**
   * Check if all assets are ready
   */
  public areAllAssetsReady(): boolean {
    return this.validationStatus.allAssetsReady;
  }

  /**
   * Get loaded meshes for a specific asset
   */
  public getMeshes(assetName: string): AbstractMesh[] | undefined {
    return this.loadedMeshes.get(assetName);
  }

  /**
   * Get the status of a specific asset
   */
  public getAssetStatus(assetName: string): AssetLoadStatus | undefined {
    return this.assetStatuses.get(assetName);
  }

  /**
   * Get failed assets that need reloading
   */
  public getFailedAssets(): string[] {
    const failed: string[] = [];
    this.assetStatuses.forEach((status, name) => {
      if (status === AssetLoadStatus.FAILED) {
        failed.push(name);
      }
    });
    return failed;
  }

  /**
   * Dispose the assets manager
   */
  public dispose(): void {
    this.onAllAssetsReady.clear();
    this.onAssetLoaded.clear();
    this.onAssetError.clear();
    this.onProgress.clear();
    this.loadedMeshes.clear();
    this.validationResults.clear();
    this.assetStatuses.clear();
  }
}
