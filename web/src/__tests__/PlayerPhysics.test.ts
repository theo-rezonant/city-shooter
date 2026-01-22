import { describe, it, expect } from 'vitest';
import { PLAYER_DIMENSIONS } from '../player/PlayerPhysics';

describe('Player Dimensions', () => {
  describe('PLAYER_DIMENSIONS constants', () => {
    it('should have human-scale height (approximately 1.8m)', () => {
      expect(PLAYER_DIMENSIONS.HEIGHT).toBeCloseTo(1.8, 1);
      expect(PLAYER_DIMENSIONS.HEIGHT).toBeGreaterThanOrEqual(1.6);
      expect(PLAYER_DIMENSIONS.HEIGHT).toBeLessThanOrEqual(2.0);
    });

    it('should have reasonable capsule radius (less than half the height)', () => {
      expect(PLAYER_DIMENSIONS.RADIUS).toBeLessThan(PLAYER_DIMENSIONS.HEIGHT / 2);
      expect(PLAYER_DIMENSIONS.RADIUS).toBeGreaterThan(0.1);
      expect(PLAYER_DIMENSIONS.RADIUS).toBeLessThan(0.5);
    });

    it('should have eye height appropriate for the capsule height', () => {
      // Eye height should be less than total height
      expect(PLAYER_DIMENSIONS.EYE_HEIGHT).toBeLessThan(PLAYER_DIMENSIONS.HEIGHT);
      // Eye height should be realistic (around 90% of total height)
      expect(PLAYER_DIMENSIONS.EYE_HEIGHT).toBeGreaterThan(PLAYER_DIMENSIONS.HEIGHT * 0.8);
      expect(PLAYER_DIMENSIONS.EYE_HEIGHT).toBeCloseTo(1.6, 1);
    });

    it('should have realistic human mass', () => {
      // Average adult human mass is 60-90 kg
      expect(PLAYER_DIMENSIONS.MASS).toBeGreaterThanOrEqual(60);
      expect(PLAYER_DIMENSIONS.MASS).toBeLessThanOrEqual(100);
      expect(PLAYER_DIMENSIONS.MASS).toBe(80); // Default 80kg
    });

    it('should have consistent dimensions relative to each other', () => {
      // Capsule should be taller than twice the radius
      const minCapsuleHeight = 2 * PLAYER_DIMENSIONS.RADIUS;
      expect(PLAYER_DIMENSIONS.HEIGHT).toBeGreaterThan(minCapsuleHeight);

      // Eye height should account for camera being near top of capsule
      const expectedEyeOffset = PLAYER_DIMENSIONS.HEIGHT / 2 - PLAYER_DIMENSIONS.RADIUS;
      expect(PLAYER_DIMENSIONS.EYE_HEIGHT).toBeGreaterThan(expectedEyeOffset);
    });
  });

  describe('Scale relative to game world', () => {
    it('should be appropriately sized for a city environment', () => {
      // A typical door is about 2m tall
      const typicalDoorHeight = 2.0;
      expect(PLAYER_DIMENSIONS.HEIGHT).toBeLessThan(typicalDoorHeight);

      // A typical sidewalk width is about 1.5m
      const typicalSidewalkWidth = 1.5;
      expect(PLAYER_DIMENSIONS.RADIUS * 2).toBeLessThan(typicalSidewalkWidth);
    });

    it('should fit through standard doorways', () => {
      // Standard doorway is 0.8m wide
      const standardDoorWidth = 0.8;
      const playerDiameter = PLAYER_DIMENSIONS.RADIUS * 2;
      expect(playerDiameter).toBeLessThan(standardDoorWidth);
    });
  });
});

describe('Capsule geometry calculations', () => {
  it('should calculate correct capsule height including hemispheres', () => {
    // Total capsule height = cylinder height + 2 * radius (for hemispheres)
    const cylinderHeight = PLAYER_DIMENSIONS.HEIGHT - 2 * PLAYER_DIMENSIONS.RADIUS;
    expect(cylinderHeight).toBeGreaterThan(0);
  });

  it('should calculate correct capsule center offset', () => {
    // Capsule center should be at half the total height
    const capsuleCenter = PLAYER_DIMENSIONS.HEIGHT / 2;
    expect(capsuleCenter).toBeCloseTo(0.9, 1);
  });
});
