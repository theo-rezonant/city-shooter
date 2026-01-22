/**
 * Physics Module Exports
 *
 * This module provides the Havok physics integration for the game.
 */

export { PhysicsSystem, initializePhysics } from './PhysicsSystem';
export type { PhysicsSystemConfig } from './PhysicsSystem';

export { CollisionLayers, CollisionMasks, PhysicsFilters, shouldCollide } from './CollisionLayers';
export type { CollisionLayer, CollisionMask, PhysicsFilterConfig } from './CollisionLayers';
