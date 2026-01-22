import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { PhysicsSystem } from '../physics/PhysicsSystem';

// Mock HavokPhysics since we can't load WASM in unit tests
vi.mock('@babylonjs/havok', () => ({
  default: vi.fn().mockResolvedValue({
    // Mock Havok instance
    HP_World_Create: vi.fn(),
    HP_Body_Create: vi.fn(),
    HP_Shape_CreateCapsule: vi.fn(),
    HP_World_SetGravity: vi.fn(),
  }),
}));

// Mock HavokPlugin
vi.mock('@babylonjs/core/Physics/v2/Plugins/havokPlugin', () => ({
  HavokPlugin: vi.fn().mockImplementation(() => ({
    dispose: vi.fn(),
    setGravity: vi.fn(),
    getGravity: vi.fn().mockReturnValue(new Vector3(0, -9.81, 0)),
  })),
}));

describe('PhysicsSystem', () => {
  beforeEach(() => {
    // Reset singleton before each test
    PhysicsSystem.resetInstance();
  });

  afterEach(() => {
    PhysicsSystem.resetInstance();
  });

  describe('Singleton pattern', () => {
    it('should return the same instance on multiple calls', () => {
      const instance1 = PhysicsSystem.getInstance();
      const instance2 = PhysicsSystem.getInstance();
      expect(instance1).toBe(instance2);
    });

    it('should create a new instance after reset', () => {
      const instance1 = PhysicsSystem.getInstance();
      PhysicsSystem.resetInstance();
      const instance2 = PhysicsSystem.getInstance();
      expect(instance1).not.toBe(instance2);
    });
  });

  describe('Configuration', () => {
    it('should use default gravity when not specified', () => {
      const instance = PhysicsSystem.getInstance();
      const gravity = instance.getGravity();
      expect(gravity.y).toBe(-9.81);
      expect(gravity.x).toBe(0);
      expect(gravity.z).toBe(0);
    });

    it('should use custom gravity when specified', () => {
      const customGravity = new Vector3(0, -15, 0);
      const instance = PhysicsSystem.getInstance({ gravity: customGravity });
      const gravity = instance.getGravity();
      expect(gravity.y).toBe(-15);
    });

    it('should return a clone of gravity to prevent external modification', () => {
      const instance = PhysicsSystem.getInstance();
      const gravity1 = instance.getGravity();
      const gravity2 = instance.getGravity();
      expect(gravity1).not.toBe(gravity2);
      gravity1.y = 100;
      expect(instance.getGravity().y).toBe(-9.81);
    });
  });

  describe('Initialization state', () => {
    it('should not be initialized initially', () => {
      const instance = PhysicsSystem.getInstance();
      expect(instance.isInitialized()).toBe(false);
    });

    it('should throw when getting plugin before initialization', () => {
      const instance = PhysicsSystem.getInstance();
      expect(() => instance.getPlugin()).toThrow('PhysicsSystem not initialized');
    });

    it('should throw when getting Havok instance before initialization', () => {
      const instance = PhysicsSystem.getInstance();
      expect(() => instance.getHavokInstance()).toThrow('PhysicsSystem not initialized');
    });

    it('should return null scene before initialization', () => {
      const instance = PhysicsSystem.getInstance();
      expect(instance.getScene()).toBeNull();
    });
  });

  describe('Gravity modification', () => {
    it('should allow setting gravity before initialization', () => {
      const instance = PhysicsSystem.getInstance();
      const newGravity = new Vector3(0, -20, 0);
      instance.setGravity(newGravity);
      expect(instance.getGravity().y).toBe(-20);
    });

    it('should clone gravity when setting to prevent external modification', () => {
      const instance = PhysicsSystem.getInstance();
      const newGravity = new Vector3(0, -20, 0);
      instance.setGravity(newGravity);
      newGravity.y = 100;
      expect(instance.getGravity().y).toBe(-20);
    });
  });
});

describe('PhysicsSystem constants', () => {
  it('should have standard Earth gravity', () => {
    const instance = PhysicsSystem.getInstance();
    const gravity = instance.getGravity();
    // Standard Earth gravity is approximately 9.81 m/s²
    expect(Math.abs(gravity.y)).toBeCloseTo(9.81, 1);
  });
});
