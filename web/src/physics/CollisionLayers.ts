/**
 * Collision Layers for Physics System
 *
 * Using bitmasking approach for collision filtering.
 * Each layer is a power of 2 to allow combining layers with bitwise OR.
 */

/**
 * Collision group identifiers using bitmasks
 */
export const CollisionLayers = {
  /** No collision */
  NONE: 0x0000,
  /** Player character collision group */
  PLAYER: 0x0001,
  /** Static environment (ground, buildings, etc.) */
  ENVIRONMENT: 0x0002,
  /** Enemy characters collision group */
  ENEMIES: 0x0004,
  /** Projectiles collision group */
  PROJECTILES: 0x0008,
  /** Triggers/Sensors (non-physical) */
  TRIGGERS: 0x0010,
  /** All layers combined */
  ALL: 0xffff,
} as const;

/**
 * Type for collision layer values
 */
export type CollisionLayer = (typeof CollisionLayers)[keyof typeof CollisionLayers];

/**
 * Collision masks define which layers an object should collide WITH.
 * These masks determine what each layer can interact with.
 */
export const CollisionMasks = {
  /** Player collides with environment and enemies */
  PLAYER: CollisionLayers.ENVIRONMENT | CollisionLayers.ENEMIES,
  /** Environment collides with player, enemies, and projectiles */
  ENVIRONMENT: CollisionLayers.PLAYER | CollisionLayers.ENEMIES | CollisionLayers.PROJECTILES,
  /** Enemies collide with player, environment, and projectiles */
  ENEMIES: CollisionLayers.PLAYER | CollisionLayers.ENVIRONMENT | CollisionLayers.PROJECTILES,
  /** Projectiles collide with environment and enemies (not player for self-fire prevention) */
  PROJECTILES: CollisionLayers.ENVIRONMENT | CollisionLayers.ENEMIES,
  /** Triggers collide with player only */
  TRIGGERS: CollisionLayers.PLAYER,
} as const;

/**
 * Type for collision mask values
 */
export type CollisionMask = (typeof CollisionMasks)[keyof typeof CollisionMasks];

/**
 * Helper function to check if two layers should collide
 * @param layerA - First layer's collision group
 * @param maskA - First layer's collision mask (what it collides with)
 * @param layerB - Second layer's collision group
 * @param maskB - Second layer's collision mask
 * @returns true if the layers should collide with each other
 */
export function shouldCollide(
  layerA: number,
  maskA: number,
  layerB: number,
  maskB: number
): boolean {
  // Both objects must want to collide with each other
  return (layerA & maskB) !== 0 && (layerB & maskA) !== 0;
}

/**
 * Create a filter configuration for physics aggregates
 */
export interface PhysicsFilterConfig {
  /** The collision group this object belongs to */
  membershipMask: number;
  /** The groups this object should collide with */
  collideWithMask: number;
}

/**
 * Pre-configured filter configurations for common use cases
 */
export const PhysicsFilters: Record<string, PhysicsFilterConfig> = {
  PLAYER: {
    membershipMask: CollisionLayers.PLAYER,
    collideWithMask: CollisionMasks.PLAYER,
  },
  ENVIRONMENT: {
    membershipMask: CollisionLayers.ENVIRONMENT,
    collideWithMask: CollisionMasks.ENVIRONMENT,
  },
  ENEMIES: {
    membershipMask: CollisionLayers.ENEMIES,
    collideWithMask: CollisionMasks.ENEMIES,
  },
  PROJECTILES: {
    membershipMask: CollisionLayers.PROJECTILES,
    collideWithMask: CollisionMasks.PROJECTILES,
  },
  TRIGGERS: {
    membershipMask: CollisionLayers.TRIGGERS,
    collideWithMask: CollisionMasks.TRIGGERS,
  },
};
