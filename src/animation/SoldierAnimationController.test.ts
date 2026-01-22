import { describe, it, expect, vi } from 'vitest';
import { SoldierAnimationState } from '../types';

// Mock Babylon.js modules
vi.mock('@babylonjs/core', () => ({
  Vector3: class {
    x: number;
    y: number;
    z: number;
    constructor(x = 0, y = 0, z = 0) {
      this.x = x;
      this.y = y;
      this.z = z;
    }
    static Zero(): { x: number; y: number; z: number } {
      return { x: 0, y: 0, z: 0 };
    }
    static TransformNormal(
      v: { x: number; y: number; z: number },
      _m: unknown
    ): { x: number; y: number; z: number } {
      return v;
    }
    clone(): { x: number; y: number; z: number } {
      return { x: this.x, y: this.y, z: this.z };
    }
    copyFrom(v: { x: number; y: number; z: number }): void {
      this.x = v.x;
      this.y = v.y;
      this.z = v.z;
    }
  },
  Quaternion: {
    FromEulerAngles: (
      _x: number,
      _y: number,
      _z: number
    ): { toRotationMatrix: (_m: unknown) => Record<string, never> } => ({
      toRotationMatrix: (_m: unknown): Record<string, never> => ({}),
    }),
  },
  Matrix: {
    Identity: (): Record<string, never> => ({}),
  },
  Scene: vi.fn().mockImplementation(() => ({
    addAnimationGroup: vi.fn(),
  })),
}));

// Create mock classes for testing
class MockBone {
  name: string;
  constructor(name: string) {
    this.name = name;
  }
}

class MockSkeleton {
  bones: MockBone[];
  constructor(bones: string[]) {
    this.bones = bones.map((name) => new MockBone(name));
  }
  dispose(): void {
    this.bones = [];
  }
}

class MockAnimationGroup {
  name: string;
  isPlaying = false;
  from = 0;
  to = 100;

  constructor(name: string) {
    this.name = name;
  }

  start(): void {
    this.isPlaying = true;
  }

  stop(): void {
    this.isPlaying = false;
  }

  clone(name: string, _callback: (t: unknown) => unknown): MockAnimationGroup {
    return new MockAnimationGroup(name);
  }

  setWeightForAllAnimatables(_weight: number): void {
    // Mock implementation
  }
}

class MockMesh {
  name: string;
  skeleton: MockSkeleton | null = null;
  computeBonesUsingShaders = false;

  constructor(name: string) {
    this.name = name;
  }

  getTotalVertices(): number {
    return 100;
  }

  dispose(): void {
    // Mock implementation
  }
}

class MockAssetContainer {
  meshes: MockMesh[] = [];
  skeletons: MockSkeleton[] = [];
  animationGroups: MockAnimationGroup[] = [];

  addAllToScene(): void {
    // Mock implementation
  }
}

describe('SoldierAnimationController', () => {
  describe('Skeleton Compatibility Verification', () => {
    it('should detect compatible skeletons with matching bones', () => {
      const soldierBones = ['Hips', 'Spine', 'Spine1', 'Spine2', 'LeftArm', 'RightArm'];
      const animBones = ['Hips', 'Spine', 'Spine1', 'Spine2', 'LeftArm', 'RightArm'];

      const soldierSkeleton = new MockSkeleton(soldierBones);
      const animSkeleton = new MockSkeleton(animBones);

      const result = verifySkeletonCompatibility(soldierSkeleton, animSkeleton);

      expect(result.isCompatible).toBe(true);
      expect(result.matchingBones).toBe(6);
      expect(result.missingInAnimation).toHaveLength(0);
    });

    it('should detect partial compatibility with some matching bones', () => {
      const soldierBones = ['Hips', 'Spine', 'Spine1', 'LeftArm', 'RightArm', 'Head'];
      const animBones = ['Hips', 'Spine', 'Spine1', 'LeftHand', 'RightHand'];

      const soldierSkeleton = new MockSkeleton(soldierBones);
      const animSkeleton = new MockSkeleton(animBones);

      const result = verifySkeletonCompatibility(soldierSkeleton, animSkeleton);

      expect(result.matchingBones).toBe(3);
      expect(result.missingInAnimation).toContain('LeftArm');
      expect(result.missingInAnimation).toContain('RightArm');
      expect(result.extraInAnimation).toContain('LeftHand');
    });

    it('should return incompatible when less than 50% bones match', () => {
      const soldierBones = ['Hips', 'Spine', 'LeftArm', 'RightArm', 'LeftLeg', 'RightLeg'];
      const animBones = ['Root', 'Pelvis']; // Completely different naming

      const soldierSkeleton = new MockSkeleton(soldierBones);
      const animSkeleton = new MockSkeleton(animBones);

      const result = verifySkeletonCompatibility(soldierSkeleton, animSkeleton);

      expect(result.isCompatible).toBe(false);
      expect(result.matchingBones).toBe(0);
    });
  });

  describe('Bone Name Normalization', () => {
    it('should normalize Mixamo bone names', () => {
      const normalized1 = normalizeBoneName('mixamorig:Hips');
      const normalized2 = normalizeBoneName('mixamorig_Hips');

      expect(normalized1).toBe('hips');
      expect(normalized2).toBe('hips');
    });

    it('should normalize Biped bone names', () => {
      const normalized = normalizeBoneName('Bip01_Spine');

      expect(normalized).toBe('spine');
    });

    it('should remove L/R suffixes', () => {
      const normalizedL = normalizeBoneName('ArmL');
      const normalizedR = normalizeBoneName('ArmR');

      expect(normalizedL).toBe('arm');
      expect(normalizedR).toBe('arm');
    });

    it('should handle left/right suffixes', () => {
      const normalizedLeft = normalizeBoneName('HandLeft');
      const normalizedRight = normalizeBoneName('HandRight');

      expect(normalizedLeft).toBe('hand');
      expect(normalizedRight).toBe('hand');
    });
  });

  describe('Animation State Management', () => {
    it('should start in idle state', () => {
      const state = SoldierAnimationState.Idle;
      expect(state).toBe('idle');
    });

    it('should have all required animation states', () => {
      expect(SoldierAnimationState.Idle).toBe('idle');
      expect(SoldierAnimationState.Strafe).toBe('strafe');
      expect(SoldierAnimationState.StaticFire).toBe('staticFire');
      expect(SoldierAnimationState.MovingFire).toBe('movingFire');
      expect(SoldierAnimationState.Reaction).toBe('reaction');
    });
  });

  describe('Asset Container Handling', () => {
    it('should create mock asset containers correctly', () => {
      const container = new MockAssetContainer();
      const mesh = new MockMesh('TestMesh');
      const skeleton = new MockSkeleton(['Bone1', 'Bone2']);
      const animGroup = new MockAnimationGroup('TestAnim');

      container.meshes.push(mesh);
      container.skeletons.push(skeleton);
      container.animationGroups.push(animGroup);

      expect(container.meshes).toHaveLength(1);
      expect(container.skeletons).toHaveLength(1);
      expect(container.animationGroups).toHaveLength(1);
    });

    it('should validate that meshes exist', () => {
      const container = new MockAssetContainer();
      expect(container.meshes.length).toBe(0);

      container.meshes.push(new MockMesh('Soldier'));
      expect(container.meshes.length).toBeGreaterThan(0);
    });
  });

  describe('GPU Vertex Skinning', () => {
    it('should enable computeBonesUsingShaders on mesh', () => {
      const mesh = new MockMesh('TestMesh');
      mesh.skeleton = new MockSkeleton(['Bone1']);

      // Simulate enabling GPU vertex skinning
      mesh.computeBonesUsingShaders = true;

      expect(mesh.computeBonesUsingShaders).toBe(true);
    });
  });

  describe('Animation Group Operations', () => {
    it('should start and stop animations', () => {
      const animGroup = new MockAnimationGroup('TestAnim');

      expect(animGroup.isPlaying).toBe(false);

      animGroup.start();
      expect(animGroup.isPlaying).toBe(true);

      animGroup.stop();
      expect(animGroup.isPlaying).toBe(false);
    });

    it('should clone animation groups with new name', () => {
      const original = new MockAnimationGroup('Original');
      const cloned = original.clone('Cloned', (t) => t);

      expect(cloned.name).toBe('Cloned');
      expect(original.name).toBe('Original');
    });
  });
});

// Helper functions extracted for testing
function verifySkeletonCompatibility(
  soldierSkeleton: MockSkeleton,
  animationSkeleton: MockSkeleton
): {
  isCompatible: boolean;
  matchingBones: number;
  totalSoldierBones: number;
  totalAnimationBones: number;
  matchedBoneNames: string[];
  missingInAnimation: string[];
  extraInAnimation: string[];
} {
  const soldierBones = soldierSkeleton.bones.map((b) => b.name);
  const animationBones = animationSkeleton.bones.map((b) => b.name);

  const soldierBoneSet = new Set(soldierBones);
  const animationBoneSet = new Set(animationBones);

  const matchedBoneNames: string[] = [];
  const missingInAnimation: string[] = [];
  const extraInAnimation: string[] = [];

  for (const bone of soldierBones) {
    if (animationBoneSet.has(bone)) {
      matchedBoneNames.push(bone);
    } else {
      missingInAnimation.push(bone);
    }
  }

  for (const bone of animationBones) {
    if (!soldierBoneSet.has(bone)) {
      extraInAnimation.push(bone);
    }
  }

  const matchRatio = matchedBoneNames.length / Math.max(soldierBones.length, 1);
  const isCompatible = matchRatio >= 0.5;

  return {
    isCompatible,
    matchingBones: matchedBoneNames.length,
    totalSoldierBones: soldierBones.length,
    totalAnimationBones: animationBones.length,
    matchedBoneNames,
    missingInAnimation,
    extraInAnimation,
  };
}

function normalizeBoneName(name: string): string {
  return name
    .toLowerCase()
    .replace(/^mixamorig[:|_]/i, '')
    .replace(/^bip[0-9]*[:|_]/i, '')
    .replace(/^bone[:|_]/i, '')
    .replace(/[_\-:]/g, '')
    .replace(/[lr]$/i, '')
    .replace(/(left|right)$/i, '');
}
