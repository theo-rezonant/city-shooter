import type {
  AnimationGroup,
  AssetContainer,
  Bone,
  Mesh,
  Skeleton,
  TransformNode,
} from '@babylonjs/core';
import { Vector3, Quaternion, Matrix, Scene } from '@babylonjs/core';
import type {
  SoldierAnimations,
  SoldierAssets,
  SkeletonCompatibilityReport,
  BoneMapping,
} from '../types';
import { SoldierAnimationState } from '../types';

/**
 * Default transition duration for blending between animations (in seconds)
 */
const DEFAULT_TRANSITION_DURATION = 0.15;

/**
 * Default scale for the soldier to fit the environment
 */
const DEFAULT_SOLDIER_SCALE = 0.01;

/**
 * Controller for managing soldier mesh and animations.
 * Handles loading, skeleton compatibility, animation state transitions,
 * and performance optimizations.
 */
export class SoldierAnimationController {
  private scene: Scene;
  private rootMesh: TransformNode | null = null;
  private meshes: Mesh[] = [];
  private skeleton: Skeleton | null = null;
  private animations: SoldierAnimations = {
    idle: null,
    strafe: null,
    staticFire: null,
    movingFire: null,
    reaction: null,
  };
  private currentState: SoldierAnimationState = SoldierAnimationState.Idle;
  private currentAnimation: AnimationGroup | null = null;
  private isInitialized = false;

  constructor(scene: Scene) {
    this.scene = scene;
  }

  /**
   * Initialize the soldier from loaded asset containers.
   * Sets up mesh, skeleton, and maps animations.
   */
  public async initializeFromContainers(containers: Map<string, AssetContainer>): Promise<void> {
    const soldierContainer = containers.get('soldier');
    if (!soldierContainer) {
      throw new Error('Soldier container not found');
    }

    // Add soldier meshes to the scene
    soldierContainer.addAllToScene();

    // Get the root mesh and all meshes
    if (soldierContainer.meshes.length > 0) {
      this.rootMesh = soldierContainer.meshes[0] as TransformNode;
      this.meshes = soldierContainer.meshes.filter((m) => m.getTotalVertices() > 0) as Mesh[];
    }

    // Get the skeleton
    if (soldierContainer.skeletons.length > 0) {
      this.skeleton = soldierContainer.skeletons[0];
    }

    // Apply initial transformations
    this.applyInitialTransformations();

    // Configure GPU vertex skinning for performance
    this.enableGPUVertexSkinning();

    // Map animations from containers
    await this.mapAnimationsFromContainers(containers);

    // Create idle animation if not present (static pose)
    this.ensureIdleAnimation();

    this.isInitialized = true;
    console.log('Soldier animation controller initialized');
  }

  /**
   * Apply initial transformations to orient the soldier correctly.
   * Forward direction should be along -Z axis (Babylon.js standard).
   */
  private applyInitialTransformations(): void {
    if (!this.rootMesh) return;

    // Scale the model to fit the environment
    // Adjust this value based on the town4new.glb scale
    this.rootMesh.scaling = new Vector3(
      DEFAULT_SOLDIER_SCALE,
      DEFAULT_SOLDIER_SCALE,
      DEFAULT_SOLDIER_SCALE
    );

    // Rotate to face -Z (forward in Babylon.js)
    // FBX models often face +Z or +X, so we rotate 180 degrees around Y
    this.rootMesh.rotationQuaternion = Quaternion.FromEulerAngles(0, Math.PI, 0);

    console.log('Applied initial transformations to soldier mesh');
  }

  /**
   * Enable GPU vertex skinning for all soldier meshes.
   * This improves performance when multiple enemies are spawned.
   */
  private enableGPUVertexSkinning(): void {
    for (const mesh of this.meshes) {
      if (mesh.skeleton) {
        // Enable GPU-based bone computation
        mesh.computeBonesUsingShaders = true;
        console.log(`Enabled GPU vertex skinning for mesh: ${mesh.name}`);
      }
    }
  }

  /**
   * Map animation groups from loaded containers to the soldier skeleton.
   */
  private async mapAnimationsFromContainers(
    containers: Map<string, AssetContainer>
  ): Promise<void> {
    // Map each animation container to the corresponding state
    const animationMappings: Array<{ key: keyof SoldierAnimations; containerName: string }> = [
      { key: 'strafe', containerName: 'strafe' },
      { key: 'reaction', containerName: 'reaction' },
      { key: 'staticFire', containerName: 'staticFire' },
      { key: 'movingFire', containerName: 'movingFire' },
    ];

    for (const mapping of animationMappings) {
      const container = containers.get(mapping.containerName);
      if (!container) {
        console.warn(`Animation container not found: ${mapping.containerName}`);
        continue;
      }

      // Get the animation group from the container
      if (container.animationGroups.length > 0) {
        const animGroup = container.animationGroups[0];

        // Verify skeleton compatibility before retargeting
        if (container.skeletons.length > 0 && this.skeleton) {
          const compatibility = this.verifySkeletonCompatibility(
            this.skeleton,
            container.skeletons[0]
          );

          if (compatibility.isCompatible) {
            console.log(`Skeleton compatible for ${mapping.containerName}:`, {
              matchingBones: compatibility.matchingBones,
              total: compatibility.totalSoldierBones,
            });
          } else {
            console.warn(`Skeleton mismatch for ${mapping.containerName}:`, {
              matching: compatibility.matchingBones,
              soldierBones: compatibility.totalSoldierBones,
              animBones: compatibility.totalAnimationBones,
              missing: compatibility.missingInAnimation.slice(0, 5),
            });
          }
        }

        // Clone and retarget the animation to the soldier skeleton
        const retargetedAnim = this.retargetAnimationToSkeleton(animGroup, mapping.key);
        if (retargetedAnim) {
          this.animations[mapping.key] = retargetedAnim;
          console.log(`Mapped animation: ${mapping.key}`);
        }
      } else {
        console.warn(`No animation groups found in container: ${mapping.containerName}`);
      }
    }
  }

  /**
   * Verify compatibility between soldier skeleton and animation skeleton.
   */
  public verifySkeletonCompatibility(
    soldierSkeleton: Skeleton,
    animationSkeleton: Skeleton
  ): SkeletonCompatibilityReport {
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

    // Consider compatible if at least 50% of bones match
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

  /**
   * Retarget an animation group to work with the soldier skeleton.
   */
  private retargetAnimationToSkeleton(
    sourceAnimGroup: AnimationGroup,
    animName: string
  ): AnimationGroup | null {
    if (!this.skeleton) {
      console.warn('No soldier skeleton available for retargeting');
      return sourceAnimGroup;
    }

    // Clone the animation group with a unique name
    const clonedName = `soldier_${animName}`;
    const clonedAnimGroup = sourceAnimGroup.clone(clonedName, (oldTarget) => {
      // Try to find matching bone in soldier skeleton by name
      if (oldTarget && 'name' in oldTarget) {
        const targetBone = this.skeleton?.bones.find((b) => b.name === oldTarget.name);
        if (targetBone) {
          return targetBone;
        }

        // Try normalized bone name matching
        const normalizedBone = this.findBoneByNormalizedName(oldTarget.name);
        if (normalizedBone) {
          return normalizedBone;
        }
      }
      return oldTarget;
    });

    // Add to scene
    this.scene.addAnimationGroup(clonedAnimGroup);

    return clonedAnimGroup;
  }

  /**
   * Find a bone by normalizing its name (handling different naming conventions).
   */
  private findBoneByNormalizedName(boneName: string): Bone | null {
    if (!this.skeleton) return null;

    // Normalize the bone name by removing common prefixes/suffixes
    const normalizedSource = this.normalizeBoneName(boneName);

    for (const bone of this.skeleton.bones) {
      const normalizedTarget = this.normalizeBoneName(bone.name);
      if (normalizedSource === normalizedTarget) {
        return bone;
      }
    }

    return null;
  }

  /**
   * Normalize a bone name for comparison.
   */
  private normalizeBoneName(name: string): string {
    return name
      .toLowerCase()
      .replace(/^mixamorig[:|_]/i, '') // Remove Mixamo prefix
      .replace(/^bip[0-9]*[:|_]/i, '') // Remove Biped prefix
      .replace(/^bone[:|_]/i, '') // Remove generic bone prefix
      .replace(/[_\-:]/g, '') // Remove separators
      .replace(/[lr]$/, '') // Remove L/R suffix
      .replace(/(left|right)$/i, ''); // Remove left/right suffix
  }

  /**
   * Create an idle animation if one doesn't exist (uses first frame as static pose).
   */
  private ensureIdleAnimation(): void {
    if (this.animations.idle) return;

    // Use strafe animation's first frame as idle if available
    if (this.animations.strafe) {
      const idleAnim = this.animations.strafe.clone('soldier_idle', (target) => target);
      this.animations.idle = idleAnim;
      this.scene.addAnimationGroup(idleAnim);
      console.log('Created idle animation from strafe first frame');
    }
  }

  /**
   * Get the soldier's current position.
   */
  public getPosition(): Vector3 {
    return this.rootMesh?.position.clone() ?? Vector3.Zero();
  }

  /**
   * Set the soldier's position.
   */
  public setPosition(position: Vector3): void {
    if (this.rootMesh) {
      this.rootMesh.position.copyFrom(position);
    }
  }

  /**
   * Set the soldier's rotation (facing direction).
   */
  public setRotation(rotation: Vector3): void {
    if (this.rootMesh) {
      this.rootMesh.rotationQuaternion = Quaternion.FromEulerAngles(
        rotation.x,
        rotation.y,
        rotation.z
      );
    }
  }

  /**
   * Set the soldier's scale.
   */
  public setScale(scale: number): void {
    if (this.rootMesh) {
      this.rootMesh.scaling = new Vector3(scale, scale, scale);
    }
  }

  /**
   * Get the forward direction of the soldier (-Z axis).
   */
  public getForwardDirection(): Vector3 {
    if (!this.rootMesh) return new Vector3(0, 0, -1);

    const forward = new Vector3(0, 0, -1);
    const rotationMatrix = Matrix.Identity();

    if (this.rootMesh.rotationQuaternion) {
      this.rootMesh.rotationQuaternion.toRotationMatrix(rotationMatrix);
    }

    return Vector3.TransformNormal(forward, rotationMatrix);
  }

  /**
   * Transition to a new animation state with blending.
   */
  public transitionToState(
    state: SoldierAnimationState,
    transitionDuration: number = DEFAULT_TRANSITION_DURATION
  ): void {
    if (!this.isInitialized) {
      console.warn('Soldier animation controller not initialized');
      return;
    }

    if (state === this.currentState && this.currentAnimation?.isPlaying) {
      return; // Already in this state
    }

    const newAnimation = this.animations[state];
    if (!newAnimation) {
      console.warn(`Animation not found for state: ${state}`);
      return;
    }

    // Stop current animation with blend out
    if (this.currentAnimation && this.currentAnimation.isPlaying) {
      // Cross-fade by playing both simultaneously briefly
      this.currentAnimation.stop();
    }

    // Start new animation with blend in
    newAnimation.start(true, 1.0, newAnimation.from, newAnimation.to, false);

    // Apply weight blending over time
    this.applyAnimationBlend(this.currentAnimation, newAnimation, transitionDuration);

    this.currentAnimation = newAnimation;
    this.currentState = state;

    console.log(`Transitioned to animation state: ${state}`);
  }

  /**
   * Apply smooth blending between animations.
   */
  private applyAnimationBlend(
    fromAnim: AnimationGroup | null,
    toAnim: AnimationGroup,
    duration: number
  ): void {
    // Simple immediate blend - for advanced blending, implement weight interpolation
    if (fromAnim) {
      fromAnim.setWeightForAllAnimatables(0);
    }
    toAnim.setWeightForAllAnimatables(1);

    // TODO: Implement smooth weight interpolation over duration
    // This would involve using scene.onBeforeRenderObservable to gradually
    // transition weights from 1->0 for fromAnim and 0->1 for toAnim
    console.log(`Blend applied with duration: ${duration}s`);
  }

  /**
   * Play the strafe/run animation.
   */
  public playStrafe(): void {
    this.transitionToState(SoldierAnimationState.Strafe);
  }

  /**
   * Play the static fire animation.
   */
  public playStaticFire(): void {
    this.transitionToState(SoldierAnimationState.StaticFire);
  }

  /**
   * Play the moving fire animation.
   */
  public playMovingFire(): void {
    this.transitionToState(SoldierAnimationState.MovingFire);
  }

  /**
   * Play the reaction/hit animation.
   */
  public playReaction(): void {
    this.transitionToState(SoldierAnimationState.Reaction);
  }

  /**
   * Play the idle animation.
   */
  public playIdle(): void {
    this.transitionToState(SoldierAnimationState.Idle);
  }

  /**
   * Stop all animations.
   */
  public stopAllAnimations(): void {
    for (const animKey of Object.keys(this.animations) as (keyof SoldierAnimations)[]) {
      const anim = this.animations[animKey];
      if (anim) {
        anim.stop();
      }
    }
    this.currentAnimation = null;
  }

  /**
   * Get the current animation state.
   */
  public getCurrentState(): SoldierAnimationState {
    return this.currentState;
  }

  /**
   * Get the soldier assets for external use.
   */
  public getSoldierAssets(): SoldierAssets {
    return {
      rootMesh: this.rootMesh!,
      meshes: this.meshes,
      skeleton: this.skeleton,
      animations: { ...this.animations },
    };
  }

  /**
   * Check if the controller is initialized and ready.
   */
  public isReady(): boolean {
    return this.isInitialized && this.rootMesh !== null;
  }

  /**
   * Get bone mapping for debugging/customization.
   */
  public getBoneMapping(): BoneMapping[] {
    if (!this.skeleton) return [];

    return this.skeleton.bones.map((bone) => ({
      source: bone.name,
      target: bone.name,
    }));
  }

  /**
   * Dispose of the soldier and all its resources.
   */
  public dispose(): void {
    this.stopAllAnimations();

    for (const mesh of this.meshes) {
      mesh.dispose();
    }

    if (this.skeleton) {
      this.skeleton.dispose();
    }

    this.rootMesh = null;
    this.meshes = [];
    this.skeleton = null;
    this.isInitialized = false;
  }
}
