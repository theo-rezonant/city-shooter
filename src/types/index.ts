import type { AnimationGroup, Mesh, Skeleton, TransformNode } from '@babylonjs/core';

/**
 * Represents the loaded soldier assets including mesh, skeleton, and animations
 */
export interface SoldierAssets {
  /** Root mesh containing the soldier model */
  rootMesh: TransformNode;
  /** All meshes that make up the soldier model */
  meshes: Mesh[];
  /** The soldier's skeleton for animation */
  skeleton: Skeleton | null;
  /** Animation groups mapped by state name */
  animations: SoldierAnimations;
}

/**
 * Animation groups for each soldier animation state
 */
export interface SoldierAnimations {
  idle: AnimationGroup | null;
  strafe: AnimationGroup | null;
  staticFire: AnimationGroup | null;
  movingFire: AnimationGroup | null;
  reaction: AnimationGroup | null;
}

/**
 * Animation state names for the soldier
 */
export enum SoldierAnimationState {
  Idle = 'idle',
  Strafe = 'strafe',
  StaticFire = 'staticFire',
  MovingFire = 'movingFire',
  Reaction = 'reaction',
}

/**
 * Configuration for loading soldier assets
 */
export interface SoldierLoadConfig {
  /** Base path for assets */
  basePath: string;
  /** Soldier model filename */
  soldierModel: string;
  /** Animation file mappings */
  animations: {
    strafe: string;
    reaction: string;
    staticFire: string;
    movingFire: string;
  };
}

/**
 * Bone mapping configuration for retargeting animations
 */
export interface BoneMapping {
  /** Source bone name from animation file */
  source: string;
  /** Target bone name on soldier skeleton */
  target: string;
}

/**
 * Skeleton compatibility report
 */
export interface SkeletonCompatibilityReport {
  /** Whether the skeletons are compatible */
  isCompatible: boolean;
  /** Number of matching bones */
  matchingBones: number;
  /** Total bones in soldier skeleton */
  totalSoldierBones: number;
  /** Total bones in animation skeleton */
  totalAnimationBones: number;
  /** List of bones that matched */
  matchedBoneNames: string[];
  /** List of bones in soldier but not in animation */
  missingInAnimation: string[];
  /** List of bones in animation but not in soldier */
  extraInAnimation: string[];
}

/**
 * Asset loading progress callback
 */
export type LoadingProgressCallback = (progress: number, message: string) => void;

/**
 * Game state enum
 */
export enum GameState {
  Loading = 'loading',
  MainMenu = 'mainMenu',
  Playing = 'playing',
  Paused = 'paused',
}
