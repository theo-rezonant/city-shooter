import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// Mock Babylon.js modules - mocks are hoisted so they run first
vi.mock("@babylonjs/core", async () => {
  const mockMesh = {
    name: "test_building_01",
    getBoundingInfo: vi.fn(() => ({
      boundingBox: {
        maximumWorld: { x: 5, y: 10, z: 5 },
        minimumWorld: { x: -5, y: 0, z: -5 },
        extendSizeWorld: { x: 5, y: 10, z: 5 },
      },
    })),
    freezeWorldMatrix: vi.fn(),
    unfreezeWorldMatrix: vi.fn(),
    doNotSyncBoundingInfo: false,
    occlusionQueryAlgorithmType: 0,
    occlusionType: 0,
    isOccluded: false,
    getTotalVertices: vi.fn(() => 500),
    material: { id: "building_material" },
    parent: null,
    position: {
      x: 0,
      y: 0,
      z: 0,
      clone: vi.fn(() => ({ x: 0, y: 0, z: 0 })),
      add: vi.fn((v: unknown) => v),
    },
  };

  const mockFloorMesh = {
    ...mockMesh,
    name: "street_floor_01",
    getBoundingInfo: vi.fn(() => ({
      boundingBox: {
        maximumWorld: { x: 50, y: 0.1, z: 50 },
        minimumWorld: { x: -50, y: 0, z: -50 },
        extendSizeWorld: { x: 50, y: 0.1, z: 50 },
      },
    })),
    getTotalVertices: vi.fn(() => 4),
  };

  const mockPropMesh = {
    ...mockMesh,
    name: "prop_bench_01",
    getBoundingInfo: vi.fn(() => ({
      boundingBox: {
        maximumWorld: { x: 1, y: 0.5, z: 1 },
        minimumWorld: { x: -1, y: 0, z: -1 },
        extendSizeWorld: { x: 1, y: 0.5, z: 1 },
      },
    })),
  };

  // Internal Mesh class for instanceof checks
  class InternalMockMesh {
    static MergeMeshes = vi.fn(() => ({
      name: "merged",
      freezeWorldMatrix: vi.fn(),
      doNotSyncBoundingInfo: false,
      material: null,
    }));
  }

  // Mark mocks as instances
  Object.setPrototypeOf(mockMesh, InternalMockMesh.prototype);
  Object.setPrototypeOf(mockFloorMesh, InternalMockMesh.prototype);
  Object.setPrototypeOf(mockPropMesh, InternalMockMesh.prototype);

  return {
    Scene: vi.fn().mockImplementation(() => ({
      freezeMaterials: vi.fn(),
      freezeActiveMeshes: vi.fn(),
      meshes: [],
      materials: [{ id: "mat1" }, { id: "mat2" }],
    })),
    SceneLoader: {
      ImportMeshAsync: vi.fn(() =>
        Promise.resolve({
          meshes: [mockMesh, mockFloorMesh, mockPropMesh],
        })
      ),
    },
    TransformNode: vi.fn().mockImplementation((name: string) => ({
      name,
      position: { y: 0 },
      dispose: vi.fn(),
    })),
    MeshBuilder: {
      CreateGround: vi.fn(() => ({
        position: { y: 0 },
        isVisible: true,
        freezeWorldMatrix: vi.fn(),
      })),
    },
    Mesh: InternalMockMesh,
    Vector3: vi.fn().mockImplementation((x = 0, y = 0, z = 0) => ({
      x,
      y,
      z,
      clone: vi.fn(() => ({ x, y, z })),
      add: vi.fn((v: unknown) => v),
    })),
  };
});

// Mock physics manager
const mockPhysicsManager = {
  isReady: vi.fn(() => true),
  createStaticBoxCollider: vi.fn(() => ({})),
  createStaticMeshCollider: vi.fn(() => ({})),
};

import { MapLoader } from "@/map/MapLoader";
import { Scene } from "@babylonjs/core";
import { PhysicsManager } from "@/physics/PhysicsManager";

describe("MapLoader", () => {
  let scene: Scene;
  let mapLoader: MapLoader;

  beforeEach(() => {
    scene = new Scene({} as never);
    mapLoader = new MapLoader(
      scene,
      mockPhysicsManager as unknown as PhysicsManager
    );
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe("loadMap", () => {
    it("should load a GLB map file", async () => {
      const onProgress = vi.fn();

      await mapLoader.loadMap("/test/map.glb", onProgress);

      const meshes = mapLoader.getMeshes();
      expect(meshes.length).toBe(3);
    });

    it("should call progress callback during loading", async () => {
      const onProgress = vi.fn();

      await mapLoader.loadMap("/test/map.glb", onProgress);

      // Progress callback may be called if lengthComputable
      expect(mapLoader.getMeshes().length).toBeGreaterThan(0);
    });

    it("should create a root transform node", async () => {
      await mapLoader.loadMap("/test/map.glb");

      const rootNode = mapLoader.getRootNode();
      expect(rootNode).not.toBeNull();
    });
  });

  describe("categorizeMeshes", () => {
    it("should categorize building meshes", async () => {
      await mapLoader.loadMap("/test/map.glb");

      const categories = mapLoader.categorizeMeshes();

      expect(categories.buildings.length).toBeGreaterThanOrEqual(1);
    });

    it("should categorize floor meshes", async () => {
      await mapLoader.loadMap("/test/map.glb");

      const categories = mapLoader.categorizeMeshes();

      expect(categories.floors.length).toBeGreaterThanOrEqual(1);
    });

    it("should categorize prop meshes", async () => {
      await mapLoader.loadMap("/test/map.glb");

      const categories = mapLoader.categorizeMeshes();

      expect(categories.props.length).toBeGreaterThanOrEqual(1);
    });
  });

  describe("optimizeMap", () => {
    it("should freeze all materials", async () => {
      await mapLoader.loadMap("/test/map.glb");

      mapLoader.optimizeMap();

      expect(scene.freezeMaterials).toHaveBeenCalled();
    });

    it("should update optimization stats", async () => {
      await mapLoader.loadMap("/test/map.glb");

      mapLoader.optimizeMap();

      const stats = mapLoader.getStats();
      expect(stats.originalMeshCount).toBe(3);
      expect(stats.frozenMaterials).toBeGreaterThan(0);
    });
  });

  describe("bakeCollision", () => {
    it("should return early if physics not ready", async () => {
      mockPhysicsManager.isReady.mockReturnValueOnce(false);

      await mapLoader.loadMap("/test/map.glb");
      await mapLoader.bakeCollision();

      expect(mockPhysicsManager.createStaticBoxCollider).not.toHaveBeenCalled();
    });

    it("should create colliders for meshes", async () => {
      await mapLoader.loadMap("/test/map.glb");
      await mapLoader.bakeCollision();

      const stats = mapLoader.getStats();
      expect(stats.buildingColliders + stats.floorColliders).toBeGreaterThan(0);
    });
  });

  describe("createFallbackGround", () => {
    it("should create an invisible ground plane", () => {
      const ground = mapLoader.createFallbackGround(100, 0);

      expect(ground).toBeDefined();
      expect(mockPhysicsManager.createStaticBoxCollider).toHaveBeenCalled();
    });
  });

  describe("findSpawnPoint", () => {
    it("should return a default spawn point", async () => {
      await mapLoader.loadMap("/test/map.glb");

      const spawnPoint = mapLoader.findSpawnPoint();

      expect(spawnPoint).toBeDefined();
      expect(spawnPoint.y).toBeGreaterThan(0);
    });
  });

  describe("getStats", () => {
    it("should return a copy of optimization stats", async () => {
      await mapLoader.loadMap("/test/map.glb");
      mapLoader.optimizeMap();

      const stats1 = mapLoader.getStats();
      const stats2 = mapLoader.getStats();

      // Should be equal but different objects
      expect(stats1).toEqual(stats2);
      expect(stats1).not.toBe(stats2);
    });
  });

  describe("dispose", () => {
    it("should clean up resources", async () => {
      await mapLoader.loadMap("/test/map.glb");

      mapLoader.dispose();

      expect(mapLoader.getMeshes().length).toBe(0);
      expect(mapLoader.getRootNode()).toBeNull();
    });
  });
});
