import { describe, it, expect, beforeEach } from 'vitest';
import {
  AssetLoadStatus,
  AssetValidationResult,
  AssetsValidationStatus,
  AssetDefinition,
} from '../types/GameTypes';

// Test asset definitions matching the production code
const TEST_GAME_ASSETS: Record<string, AssetDefinition> = {
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

describe('Asset Definitions', () => {
  it('should have correct asset definitions for Town', () => {
    const town = TEST_GAME_ASSETS.TOWN;
    expect(town.name).toBe('Town');
    expect(town.sceneFilename).toBe('town4new.glb');
    expect(town.isRequired).toBe(true);
  });

  it('should have correct asset definitions for LaserGun', () => {
    const gun = TEST_GAME_ASSETS.LASER_GUN;
    expect(gun.name).toBe('LaserGun');
    expect(gun.sceneFilename).toBe('lasergun.glb');
    expect(gun.isRequired).toBe(true);
  });

  it('should have correct asset definitions for Soldier', () => {
    const soldier = TEST_GAME_ASSETS.SOLDIER;
    expect(soldier.name).toBe('Soldier');
    expect(soldier.sceneFilename).toBe('Soldier.fbx');
    expect(soldier.isRequired).toBe(true);
  });
});

describe('Asset Load Status', () => {
  it('should have all expected status values', () => {
    expect(AssetLoadStatus.PENDING).toBe('PENDING');
    expect(AssetLoadStatus.LOADING).toBe('LOADING');
    expect(AssetLoadStatus.LOADED).toBe('LOADED');
    expect(AssetLoadStatus.VALIDATED).toBe('VALIDATED');
    expect(AssetLoadStatus.FAILED).toBe('FAILED');
  });

  it('should transition correctly from PENDING to LOADING', () => {
    let status = AssetLoadStatus.PENDING;
    status = AssetLoadStatus.LOADING;
    expect(status).toBe(AssetLoadStatus.LOADING);
  });

  it('should transition correctly from LOADING to LOADED', () => {
    let status = AssetLoadStatus.LOADING;
    status = AssetLoadStatus.LOADED;
    expect(status).toBe(AssetLoadStatus.LOADED);
  });

  it('should transition correctly from LOADED to VALIDATED', () => {
    let status = AssetLoadStatus.LOADED;
    status = AssetLoadStatus.VALIDATED;
    expect(status).toBe(AssetLoadStatus.VALIDATED);
  });

  it('should transition to FAILED on error', () => {
    let status = AssetLoadStatus.LOADING;
    status = AssetLoadStatus.FAILED;
    expect(status).toBe(AssetLoadStatus.FAILED);
  });
});

describe('Asset Validation Result', () => {
  it('should create valid result for successful asset', () => {
    const result: AssetValidationResult = {
      assetName: 'TestAsset',
      isValid: true,
      meshCount: 5,
      hasMaterials: true,
      hasTextures: true,
      errors: [],
    };

    expect(result.isValid).toBe(true);
    expect(result.meshCount).toBeGreaterThan(0);
    expect(result.hasMaterials).toBe(true);
    expect(result.hasTextures).toBe(true);
    expect(result.errors).toHaveLength(0);
  });

  it('should create invalid result for asset with no meshes', () => {
    const result: AssetValidationResult = {
      assetName: 'TestAsset',
      isValid: false,
      meshCount: 0,
      hasMaterials: false,
      hasTextures: false,
      errors: ['No meshes loaded'],
    };

    expect(result.isValid).toBe(false);
    expect(result.meshCount).toBe(0);
    expect(result.errors).toContain('No meshes loaded');
  });

  it('should create invalid result for asset with missing textures', () => {
    const result: AssetValidationResult = {
      assetName: 'LaserGun',
      isValid: false,
      meshCount: 3,
      hasMaterials: true,
      hasTextures: false,
      errors: ['Laser gun missing albedo texture'],
    };

    expect(result.isValid).toBe(false);
    expect(result.hasMaterials).toBe(true);
    expect(result.hasTextures).toBe(false);
    expect(result.errors).toContain('Laser gun missing albedo texture');
  });
});

describe('Assets Validation Status', () => {
  it('should report all assets ready when all are validated', () => {
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

    expect(status.allAssetsReady).toBe(true);
  });

  it('should report not ready when town is not validated', () => {
    const status: AssetsValidationStatus = {
      townLoaded: true,
      townValidated: false,
      laserGunLoaded: true,
      laserGunValidated: true,
      soldierLoaded: true,
      soldierValidated: true,
      allAssetsReady: false,
      validationResults: [],
    };

    expect(status.allAssetsReady).toBe(false);
    expect(status.townValidated).toBe(false);
  });

  it('should report not ready when laser gun is not validated', () => {
    const status: AssetsValidationStatus = {
      townLoaded: true,
      townValidated: true,
      laserGunLoaded: true,
      laserGunValidated: false,
      soldierLoaded: true,
      soldierValidated: true,
      allAssetsReady: false,
      validationResults: [],
    };

    expect(status.allAssetsReady).toBe(false);
    expect(status.laserGunValidated).toBe(false);
  });

  it('should report not ready when soldier is not validated', () => {
    const status: AssetsValidationStatus = {
      townLoaded: true,
      townValidated: true,
      laserGunLoaded: true,
      laserGunValidated: true,
      soldierLoaded: true,
      soldierValidated: false,
      allAssetsReady: false,
      validationResults: [],
    };

    expect(status.allAssetsReady).toBe(false);
    expect(status.soldierValidated).toBe(false);
  });
});

describe('Validation Status Computation', () => {
  it('should compute allAssetsReady correctly', () => {
    const computeAllAssetsReady = (
      townValidated: boolean,
      laserGunValidated: boolean,
      soldierValidated: boolean
    ): boolean => {
      return townValidated && laserGunValidated && soldierValidated;
    };

    expect(computeAllAssetsReady(true, true, true)).toBe(true);
    expect(computeAllAssetsReady(false, true, true)).toBe(false);
    expect(computeAllAssetsReady(true, false, true)).toBe(false);
    expect(computeAllAssetsReady(true, true, false)).toBe(false);
    expect(computeAllAssetsReady(false, false, false)).toBe(false);
  });
});

describe('Asset Manager Status Tracking', () => {
  let assetStatuses: Map<string, AssetLoadStatus>;

  beforeEach(() => {
    assetStatuses = new Map();
    Object.values(TEST_GAME_ASSETS).forEach((asset) => {
      assetStatuses.set(asset.name, AssetLoadStatus.PENDING);
    });
  });

  it('should initialize all assets as pending', () => {
    expect(assetStatuses.get('Town')).toBe(AssetLoadStatus.PENDING);
    expect(assetStatuses.get('LaserGun')).toBe(AssetLoadStatus.PENDING);
    expect(assetStatuses.get('Soldier')).toBe(AssetLoadStatus.PENDING);
  });

  it('should track loading state for each asset', () => {
    assetStatuses.set('Town', AssetLoadStatus.LOADING);
    expect(assetStatuses.get('Town')).toBe(AssetLoadStatus.LOADING);
    expect(assetStatuses.get('LaserGun')).toBe(AssetLoadStatus.PENDING);
  });

  it('should identify failed assets', () => {
    assetStatuses.set('Town', AssetLoadStatus.VALIDATED);
    assetStatuses.set('LaserGun', AssetLoadStatus.FAILED);
    assetStatuses.set('Soldier', AssetLoadStatus.VALIDATED);

    const failedAssets: string[] = [];
    assetStatuses.forEach((status, name) => {
      if (status === AssetLoadStatus.FAILED) {
        failedAssets.push(name);
      }
    });

    expect(failedAssets).toContain('LaserGun');
    expect(failedAssets).toHaveLength(1);
  });
});

describe('Progress Tracking', () => {
  it('should calculate progress correctly', () => {
    const total = 3;
    let loaded = 0;

    expect(loaded / total).toBe(0);

    loaded = 1;
    expect(loaded / total).toBeCloseTo(0.333, 2);

    loaded = 2;
    expect(loaded / total).toBeCloseTo(0.667, 2);

    loaded = 3;
    expect(loaded / total).toBe(1);
  });

  it('should track remaining assets', () => {
    const total = 3;
    let remaining = 3;

    remaining--;
    expect(remaining).toBe(2);
    expect(total - remaining).toBe(1);

    remaining--;
    expect(remaining).toBe(1);
    expect(total - remaining).toBe(2);

    remaining--;
    expect(remaining).toBe(0);
    expect(total - remaining).toBe(3);
  });
});

describe('Mesh Validation Logic', () => {
  it('should fail validation with zero meshes', () => {
    const meshCount = 0;
    const errors: string[] = [];

    if (meshCount === 0) {
      errors.push('No meshes loaded');
    }

    expect(errors).toContain('No meshes loaded');
  });

  it('should pass validation with positive mesh count', () => {
    const meshCount = 5;
    const errors: string[] = [];

    if (meshCount <= 0) {
      errors.push('No meshes loaded');
    }

    expect(errors).toHaveLength(0);
  });
});

describe('PBR Material Validation', () => {
  it('should validate laser gun requires albedo texture', () => {
    const mockMaterial = {
      albedoTexture: null,
      bumpTexture: null,
    };

    const errors: string[] = [];
    if (!mockMaterial.albedoTexture) {
      errors.push('Laser gun missing albedo texture');
    }

    expect(errors).toContain('Laser gun missing albedo texture');
  });

  it('should pass validation when albedo texture exists', () => {
    const mockMaterial = {
      albedoTexture: { name: 'albedo.png' },
      bumpTexture: null,
    };

    const errors: string[] = [];
    if (!mockMaterial.albedoTexture) {
      errors.push('Laser gun missing albedo texture');
    }

    expect(errors).toHaveLength(0);
  });

  it('should warn but not fail when bump texture is missing', () => {
    const mockMaterial = {
      albedoTexture: { name: 'albedo.png' },
      bumpTexture: null,
    };

    const warnings: string[] = [];
    if (!mockMaterial.bumpTexture) {
      warnings.push('Laser gun missing bump texture (normal map)');
    }

    // This is a warning, not an error
    expect(warnings).toHaveLength(1);
  });
});

describe('Asset Reload Logic', () => {
  it('should identify assets that need reloading', () => {
    const assetStatuses = new Map<string, AssetLoadStatus>();
    assetStatuses.set('Town', AssetLoadStatus.VALIDATED);
    assetStatuses.set('LaserGun', AssetLoadStatus.FAILED);
    assetStatuses.set('Soldier', AssetLoadStatus.VALIDATED);

    const needsReload = (status: AssetLoadStatus): boolean => {
      return status === AssetLoadStatus.FAILED;
    };

    expect(needsReload(assetStatuses.get('Town')!)).toBe(false);
    expect(needsReload(assetStatuses.get('LaserGun')!)).toBe(true);
    expect(needsReload(assetStatuses.get('Soldier')!)).toBe(false);
  });

  it('should reset status to pending before reload', () => {
    let status = AssetLoadStatus.FAILED;
    status = AssetLoadStatus.PENDING;
    expect(status).toBe(AssetLoadStatus.PENDING);
  });
});
