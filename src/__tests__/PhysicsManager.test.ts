import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// Mock Havok Physics
vi.mock("@babylonjs/havok", () => ({
  default: vi.fn(() =>
    Promise.resolve({
      // Mock Havok instance
    })
  ),
  HavokPlugin: vi.fn().mockImplementation(() => ({
    dispose: vi.fn(),
  })),
}));

// Mock Babylon.js
const mockBody = {
  setMassProperties: vi.fn(),
  setLinearVelocity: vi.fn(),
  setAngularVelocity: vi.fn(),
  getLinearVelocity: vi.fn(() => ({ x: 0, y: 0, z: 0 })),
};

const mockShape = {
  filterMembershipMask: 0,
  filterCollideMask: 0,
};

const mockAggregate = {
  body: mockBody,
  shape: mockShape,
  dispose: vi.fn(),
};

const mockHavokPlugin = {
  dispose: vi.fn(),
};

vi.mock("@babylonjs/core", () => ({
  Scene: vi.fn().mockImplementation(() => ({
    enablePhysics: vi.fn(),
  })),
  Vector3: vi.fn().mockImplementation((x = 0, y = 0, z = 0) => ({
    x,
    y,
    z,
  })),
  PhysicsAggregate: vi.fn().mockImplementation(() => mockAggregate),
  PhysicsShapeType: {
    BOX: 0,
    SPHERE: 1,
    CAPSULE: 2,
    CYLINDER: 3,
    MESH: 4,
  },
  PhysicsMotionType: {
    STATIC: 0,
    ANIMATED: 1,
    DYNAMIC: 2,
  },
  Mesh: vi.fn(),
  HavokPlugin: vi.fn().mockImplementation(() => mockHavokPlugin),
}));

import {
  PhysicsManager,
  CollisionGroups,
  CollisionMasks,
} from "@/physics/PhysicsManager";
import { Scene, Mesh } from "@babylonjs/core";

describe("PhysicsManager", () => {
  let scene: Scene;
  let physicsManager: PhysicsManager;

  beforeEach(() => {
    scene = new Scene({} as never);
    physicsManager = new PhysicsManager(scene);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe("CollisionGroups", () => {
    it("should define correct collision group values", () => {
      expect(CollisionGroups.NONE).toBe(0x0000);
      expect(CollisionGroups.STATIC_ENVIRONMENT).toBe(0x0001);
      expect(CollisionGroups.PLAYER).toBe(0x0002);
      expect(CollisionGroups.ENEMY).toBe(0x0004);
      expect(CollisionGroups.PROJECTILE).toBe(0x0008);
      expect(CollisionGroups.TRIGGER).toBe(0x0010);
      expect(CollisionGroups.ALL).toBe(0xffff);
    });
  });

  describe("CollisionMasks", () => {
    it("should define correct collision masks for static environment", () => {
      expect(CollisionMasks.STATIC_ENVIRONMENT).toBe(CollisionGroups.ALL);
    });

    it("should define correct collision masks for player", () => {
      const expectedMask =
        CollisionGroups.STATIC_ENVIRONMENT |
        CollisionGroups.ENEMY |
        CollisionGroups.TRIGGER;
      expect(CollisionMasks.PLAYER).toBe(expectedMask);
    });

    it("should define correct collision masks for enemies", () => {
      const expectedMask =
        CollisionGroups.STATIC_ENVIRONMENT |
        CollisionGroups.PLAYER |
        CollisionGroups.ENEMY |
        CollisionGroups.PROJECTILE;
      expect(CollisionMasks.ENEMY).toBe(expectedMask);
    });

    it("should define correct collision masks for projectiles", () => {
      const expectedMask =
        CollisionGroups.STATIC_ENVIRONMENT | CollisionGroups.ENEMY;
      expect(CollisionMasks.PROJECTILE).toBe(expectedMask);
    });
  });

  describe("initialize", () => {
    it("should initialize Havok physics successfully", async () => {
      expect(physicsManager.isReady()).toBe(false);

      await physicsManager.initialize();

      expect(physicsManager.isReady()).toBe(true);
      expect(physicsManager.getPlugin()).not.toBeNull();
    });

    it("should not reinitialize if already initialized", async () => {
      const consoleSpy = vi.spyOn(console, "warn");

      await physicsManager.initialize();
      await physicsManager.initialize();

      expect(consoleSpy).toHaveBeenCalledWith(
        "[PhysicsManager] Already initialized"
      );
    });
  });

  describe("createStaticBoxCollider", () => {
    it("should return null if physics not initialized", () => {
      const mockMesh = {} as Mesh;
      const result = physicsManager.createStaticBoxCollider(mockMesh);

      expect(result).toBeNull();
    });

    it("should create a box collider when physics is ready", async () => {
      await physicsManager.initialize();

      const mockMesh = {} as Mesh;
      const result = physicsManager.createStaticBoxCollider(mockMesh);

      expect(result).not.toBeNull();
    });

    it("should apply default collision filtering", async () => {
      await physicsManager.initialize();

      const mockMesh = {} as Mesh;
      physicsManager.createStaticBoxCollider(mockMesh);

      expect(mockShape.filterMembershipMask).toBe(
        CollisionGroups.STATIC_ENVIRONMENT
      );
      expect(mockShape.filterCollideMask).toBe(
        CollisionMasks.STATIC_ENVIRONMENT
      );
    });
  });

  describe("createStaticMeshCollider", () => {
    it("should return null if physics not initialized", () => {
      const mockMesh = { isMesh: true } as unknown as Mesh;
      const result = physicsManager.createStaticMeshCollider(mockMesh);

      expect(result).toBeNull();
    });
  });

  describe("createPlayerCollider", () => {
    it("should return null if physics not initialized", () => {
      const mockMesh = {} as Mesh;
      const result = physicsManager.createPlayerCollider(mockMesh);

      expect(result).toBeNull();
    });

    it("should create a player collider with correct defaults", async () => {
      await physicsManager.initialize();

      const mockMesh = {} as Mesh;
      const result = physicsManager.createPlayerCollider(mockMesh);

      expect(result).not.toBeNull();
      expect(mockBody.setMassProperties).toHaveBeenCalled();
    });

    it("should apply player collision filtering", async () => {
      await physicsManager.initialize();

      const mockMesh = {} as Mesh;
      physicsManager.createPlayerCollider(mockMesh);

      expect(mockShape.filterMembershipMask).toBe(CollisionGroups.PLAYER);
      expect(mockShape.filterCollideMask).toBe(CollisionMasks.PLAYER);
    });
  });

  describe("setCollisionFiltering", () => {
    it("should set collision masks on aggregate", async () => {
      await physicsManager.initialize();

      const testGroup = CollisionGroups.ENEMY;
      const testMask = CollisionMasks.ENEMY;

      physicsManager.setCollisionFiltering(
        mockAggregate as never,
        testGroup,
        testMask
      );

      expect(mockShape.filterMembershipMask).toBe(testGroup);
      expect(mockShape.filterCollideMask).toBe(testMask);
    });
  });

  describe("clearAllAggregates", () => {
    it("should dispose all tracked aggregates", async () => {
      await physicsManager.initialize();

      const mockMesh = {} as Mesh;
      physicsManager.createStaticBoxCollider(mockMesh);
      physicsManager.createStaticBoxCollider(mockMesh);

      physicsManager.clearAllAggregates();

      // Dispose should have been called on the mock
      expect(mockAggregate.dispose).toHaveBeenCalled();
    });
  });

  describe("dispose", () => {
    it("should clean up all resources", async () => {
      await physicsManager.initialize();

      physicsManager.dispose();

      expect(physicsManager.isReady()).toBe(false);
      expect(physicsManager.getPlugin()).toBeNull();
    });
  });
});
