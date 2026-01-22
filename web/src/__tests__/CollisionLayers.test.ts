import { describe, it, expect } from 'vitest';
import {
  CollisionLayers,
  CollisionMasks,
  PhysicsFilters,
  shouldCollide,
} from '../physics/CollisionLayers';

describe('CollisionLayers', () => {
  describe('Layer values', () => {
    it('should have unique bit values for each layer', () => {
      const layers = [
        CollisionLayers.PLAYER,
        CollisionLayers.ENVIRONMENT,
        CollisionLayers.ENEMIES,
        CollisionLayers.PROJECTILES,
        CollisionLayers.TRIGGERS,
      ];

      // Check all layers are powers of 2 (single bit set)
      for (const layer of layers) {
        expect(layer).toBeGreaterThan(0);
        expect((layer & (layer - 1)) === 0).toBe(true); // Power of 2 check
      }

      // Check all layers are unique
      const uniqueLayers = new Set(layers);
      expect(uniqueLayers.size).toBe(layers.length);
    });

    it('should have correct bitmask values', () => {
      expect(CollisionLayers.NONE).toBe(0x0000);
      expect(CollisionLayers.PLAYER).toBe(0x0001);
      expect(CollisionLayers.ENVIRONMENT).toBe(0x0002);
      expect(CollisionLayers.ENEMIES).toBe(0x0004);
      expect(CollisionLayers.PROJECTILES).toBe(0x0008);
      expect(CollisionLayers.TRIGGERS).toBe(0x0010);
      expect(CollisionLayers.ALL).toBe(0xffff);
    });
  });

  describe('CollisionMasks', () => {
    it('should have Player collide with Environment and Enemies', () => {
      expect(CollisionMasks.PLAYER & CollisionLayers.ENVIRONMENT).toBeTruthy();
      expect(CollisionMasks.PLAYER & CollisionLayers.ENEMIES).toBeTruthy();
      // Player should NOT collide with projectiles (friendly fire prevention)
      expect(CollisionMasks.PLAYER & CollisionLayers.PROJECTILES).toBeFalsy();
    });

    it('should have Environment collide with Player, Enemies, and Projectiles', () => {
      expect(CollisionMasks.ENVIRONMENT & CollisionLayers.PLAYER).toBeTruthy();
      expect(CollisionMasks.ENVIRONMENT & CollisionLayers.ENEMIES).toBeTruthy();
      expect(CollisionMasks.ENVIRONMENT & CollisionLayers.PROJECTILES).toBeTruthy();
    });

    it('should have Enemies collide with Player, Environment, and Projectiles', () => {
      expect(CollisionMasks.ENEMIES & CollisionLayers.PLAYER).toBeTruthy();
      expect(CollisionMasks.ENEMIES & CollisionLayers.ENVIRONMENT).toBeTruthy();
      expect(CollisionMasks.ENEMIES & CollisionLayers.PROJECTILES).toBeTruthy();
    });

    it('should have Projectiles collide with Environment and Enemies', () => {
      expect(CollisionMasks.PROJECTILES & CollisionLayers.ENVIRONMENT).toBeTruthy();
      expect(CollisionMasks.PROJECTILES & CollisionLayers.ENEMIES).toBeTruthy();
      // Projectiles should NOT collide with player
      expect(CollisionMasks.PROJECTILES & CollisionLayers.PLAYER).toBeFalsy();
    });

    it('should have Triggers collide with Player only', () => {
      expect(CollisionMasks.TRIGGERS & CollisionLayers.PLAYER).toBeTruthy();
      expect(CollisionMasks.TRIGGERS & CollisionLayers.ENEMIES).toBeFalsy();
      expect(CollisionMasks.TRIGGERS & CollisionLayers.ENVIRONMENT).toBeFalsy();
    });
  });

  describe('shouldCollide', () => {
    it('should return true when player and environment should collide', () => {
      const result = shouldCollide(
        CollisionLayers.PLAYER,
        CollisionMasks.PLAYER,
        CollisionLayers.ENVIRONMENT,
        CollisionMasks.ENVIRONMENT
      );
      expect(result).toBe(true);
    });

    it('should return true when player and enemies should collide', () => {
      const result = shouldCollide(
        CollisionLayers.PLAYER,
        CollisionMasks.PLAYER,
        CollisionLayers.ENEMIES,
        CollisionMasks.ENEMIES
      );
      expect(result).toBe(true);
    });

    it('should return false when player and projectiles should not collide', () => {
      const result = shouldCollide(
        CollisionLayers.PLAYER,
        CollisionMasks.PLAYER,
        CollisionLayers.PROJECTILES,
        CollisionMasks.PROJECTILES
      );
      expect(result).toBe(false);
    });

    it('should return true when enemies and environment should collide', () => {
      const result = shouldCollide(
        CollisionLayers.ENEMIES,
        CollisionMasks.ENEMIES,
        CollisionLayers.ENVIRONMENT,
        CollisionMasks.ENVIRONMENT
      );
      expect(result).toBe(true);
    });

    it('should return true when projectiles and enemies should collide', () => {
      const result = shouldCollide(
        CollisionLayers.PROJECTILES,
        CollisionMasks.PROJECTILES,
        CollisionLayers.ENEMIES,
        CollisionMasks.ENEMIES
      );
      expect(result).toBe(true);
    });

    it('should return false when triggers and environment should not collide', () => {
      const result = shouldCollide(
        CollisionLayers.TRIGGERS,
        CollisionMasks.TRIGGERS,
        CollisionLayers.ENVIRONMENT,
        CollisionMasks.ENVIRONMENT
      );
      expect(result).toBe(false);
    });
  });

  describe('PhysicsFilters', () => {
    it('should have correct PLAYER filter configuration', () => {
      expect(PhysicsFilters.PLAYER.membershipMask).toBe(CollisionLayers.PLAYER);
      expect(PhysicsFilters.PLAYER.collideWithMask).toBe(CollisionMasks.PLAYER);
    });

    it('should have correct ENVIRONMENT filter configuration', () => {
      expect(PhysicsFilters.ENVIRONMENT.membershipMask).toBe(CollisionLayers.ENVIRONMENT);
      expect(PhysicsFilters.ENVIRONMENT.collideWithMask).toBe(CollisionMasks.ENVIRONMENT);
    });

    it('should have correct ENEMIES filter configuration', () => {
      expect(PhysicsFilters.ENEMIES.membershipMask).toBe(CollisionLayers.ENEMIES);
      expect(PhysicsFilters.ENEMIES.collideWithMask).toBe(CollisionMasks.ENEMIES);
    });

    it('should have correct PROJECTILES filter configuration', () => {
      expect(PhysicsFilters.PROJECTILES.membershipMask).toBe(CollisionLayers.PROJECTILES);
      expect(PhysicsFilters.PROJECTILES.collideWithMask).toBe(CollisionMasks.PROJECTILES);
    });

    it('should have correct TRIGGERS filter configuration', () => {
      expect(PhysicsFilters.TRIGGERS.membershipMask).toBe(CollisionLayers.TRIGGERS);
      expect(PhysicsFilters.TRIGGERS.collideWithMask).toBe(CollisionMasks.TRIGGERS);
    });
  });
});

describe('Collision scenarios', () => {
  it('player should be able to stand on environment (floor)', () => {
    // Player falls onto floor - they should collide
    const playerCollidesWithEnv = shouldCollide(
      CollisionLayers.PLAYER,
      CollisionMasks.PLAYER,
      CollisionLayers.ENVIRONMENT,
      CollisionMasks.ENVIRONMENT
    );
    expect(playerCollidesWithEnv).toBe(true);
  });

  it('player should be blocked by environment (walls/buildings)', () => {
    // Same as above - player should collide with static geometry
    const playerCollidesWithWall = shouldCollide(
      CollisionLayers.PLAYER,
      CollisionMasks.PLAYER,
      CollisionLayers.ENVIRONMENT,
      CollisionMasks.ENVIRONMENT
    );
    expect(playerCollidesWithWall).toBe(true);
  });

  it('player should collide with enemies', () => {
    const playerCollidesWithEnemy = shouldCollide(
      CollisionLayers.PLAYER,
      CollisionMasks.PLAYER,
      CollisionLayers.ENEMIES,
      CollisionMasks.ENEMIES
    );
    expect(playerCollidesWithEnemy).toBe(true);
  });

  it('enemies should be blocked by environment', () => {
    const enemyCollidesWithEnv = shouldCollide(
      CollisionLayers.ENEMIES,
      CollisionMasks.ENEMIES,
      CollisionLayers.ENVIRONMENT,
      CollisionMasks.ENVIRONMENT
    );
    expect(enemyCollidesWithEnv).toBe(true);
  });

  it('projectiles should hit enemies', () => {
    const projectileHitsEnemy = shouldCollide(
      CollisionLayers.PROJECTILES,
      CollisionMasks.PROJECTILES,
      CollisionLayers.ENEMIES,
      CollisionMasks.ENEMIES
    );
    expect(projectileHitsEnemy).toBe(true);
  });

  it('projectiles should hit environment (walls)', () => {
    const projectileHitsWall = shouldCollide(
      CollisionLayers.PROJECTILES,
      CollisionMasks.PROJECTILES,
      CollisionLayers.ENVIRONMENT,
      CollisionMasks.ENVIRONMENT
    );
    expect(projectileHitsWall).toBe(true);
  });

  it('player own projectiles should not hit player (no friendly fire)', () => {
    const projectileHitsPlayer = shouldCollide(
      CollisionLayers.PROJECTILES,
      CollisionMasks.PROJECTILES,
      CollisionLayers.PLAYER,
      CollisionMasks.PLAYER
    );
    expect(projectileHitsPlayer).toBe(false);
  });
});
