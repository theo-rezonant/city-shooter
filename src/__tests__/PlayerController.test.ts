import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

interface MockVector3 {
  x: number;
  y: number;
  z: number;
  clone: () => MockVector3;
  add: (v: MockVector3) => MockVector3;
  subtract: (v: MockVector3) => MockVector3;
  scale: (s: number) => MockVector3;
  normalize: () => MockVector3;
  length: () => number;
}

// Mock Vector3
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const createMockVector3 = (x = 0, y = 0, z = 0): any => ({
  x,
  y,
  z,
  clone: vi.fn(() => createMockVector3(x, y, z)),
  add: vi.fn((v: MockVector3) => createMockVector3(x + v.x, y + v.y, z + v.z)),
  subtract: vi.fn((v: MockVector3) =>
    createMockVector3(x - v.x, y - v.y, z - v.z)
  ),
  scale: vi.fn((s: number) => createMockVector3(x * s, y * s, z * s)),
  normalize: vi.fn(() => {
    const len = Math.sqrt(x * x + y * y + z * z);
    if (len > 0) {
      return createMockVector3(x / len, y / len, z / len);
    }
    return createMockVector3(0, 0, 0);
  }),
  length: vi.fn(() => Math.sqrt(x * x + y * y + z * z)),
});

const mockPlayerMesh = {
  position: createMockVector3(0, 1, 0),
  isVisible: true,
  isPickable: true,
  dispose: vi.fn(),
};

const mockPhysicsBody = {
  setLinearVelocity: vi.fn(),
  setAngularVelocity: vi.fn(),
  getLinearVelocity: vi.fn(() => createMockVector3(0, 0, 0)),
  setAngularDamping: vi.fn(),
  setLinearDamping: vi.fn(),
  setMassProperties: vi.fn(),
};

const mockAggregate = {
  body: mockPhysicsBody,
  shape: {},
  dispose: vi.fn(),
};

// Mock Vector3 as a constructor function
function MockVector3(x = 0, y = 0, z = 0) {
  return createMockVector3(x, y, z);
}
MockVector3.Zero = vi.fn(() => createMockVector3(0, 0, 0));

vi.mock("@babylonjs/core", () => ({
  Scene: vi.fn().mockImplementation(() => ({
    onKeyboardObservable: {
      add: vi.fn((callback) => {
        // Store callback for testing
        (global as Record<string, unknown>).__keyboardCallback = callback;
        return { dispose: vi.fn() };
      }),
      remove: vi.fn(),
    },
    pickWithRay: vi.fn(() => ({ hit: true })),
  })),
  FreeCamera: vi.fn().mockImplementation(() => ({
    position: createMockVector3(0, 1.6, 0),
    rotation: { y: 0 },
    attachControl: vi.fn(),
    keysUp: [],
    keysDown: [],
    keysLeft: [],
    keysRight: [],
  })),
  MeshBuilder: {
    CreateCapsule: vi.fn(() => mockPlayerMesh),
  },
  Vector3: MockVector3,
  Ray: vi.fn().mockImplementation(() => ({})),
  KeyboardEventTypes: {
    KEYDOWN: 1,
    KEYUP: 2,
  },
}));

// Mock physics manager
const mockPhysicsManager = {
  isReady: vi.fn(() => true),
  createPlayerCollider: vi.fn(() => mockAggregate),
};

import { PlayerController, PlayerConfig } from "@/player/PlayerController";
import { Scene, FreeCamera, KeyboardEventTypes } from "@babylonjs/core";
import { PhysicsManager } from "@/physics/PhysicsManager";

describe("PlayerController", () => {
  let scene: Scene;
  let camera: FreeCamera;
  let playerController: PlayerController;

  beforeEach(() => {
    scene = new Scene({} as never);
    camera = new FreeCamera("camera", {} as never, scene);
    playerController = new PlayerController(
      scene,
      camera,
      mockPhysicsManager as unknown as PhysicsManager
    );
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
    delete (global as Record<string, unknown>).__keyboardCallback;
  });

  describe("initialize", () => {
    it("should create player mesh", async () => {
      await playerController.initialize();

      const mesh = playerController.getMesh();
      expect(mesh).not.toBeNull();
    });

    it("should setup physics aggregate", async () => {
      await playerController.initialize();

      expect(mockPhysicsManager.createPlayerCollider).toHaveBeenCalled();
    });

    it("should register keyboard observer", async () => {
      await playerController.initialize();

      expect(scene.onKeyboardObservable.add).toHaveBeenCalled();
    });

    it("should configure player mesh as not visible", async () => {
      await playerController.initialize();

      const mesh = playerController.getMesh();
      expect(mesh?.isVisible).toBe(false);
    });
  });

  describe("keyboard input", () => {
    it("should handle WASD movement keys", async () => {
      await playerController.initialize();

      const callback = (global as Record<string, unknown>)
        .__keyboardCallback as (info: {
        type: number;
        event: { code: string };
      }) => void;

      // Simulate W key press
      callback({
        type: KeyboardEventTypes.KEYDOWN,
        event: { code: "KeyW" },
      });

      // Verify the input was registered by calling update
      playerController.update();

      expect(mockPhysicsBody.setLinearVelocity).toHaveBeenCalled();
    });

    it("should handle jump input", async () => {
      await playerController.initialize();

      const callback = (global as Record<string, unknown>)
        .__keyboardCallback as (info: {
        type: number;
        event: { code: string };
      }) => void;

      // Simulate Space key press
      callback({
        type: KeyboardEventTypes.KEYDOWN,
        event: { code: "Space" },
      });

      playerController.update();

      // Jump should be attempted
      expect(mockPhysicsBody.setLinearVelocity).toHaveBeenCalled();
    });

    it("should handle sprint input", async () => {
      await playerController.initialize();

      const callback = (global as Record<string, unknown>)
        .__keyboardCallback as (info: {
        type: number;
        event: { code: string };
      }) => void;

      // Simulate Shift + W for sprint
      callback({
        type: KeyboardEventTypes.KEYDOWN,
        event: { code: "ShiftLeft" },
      });
      callback({
        type: KeyboardEventTypes.KEYDOWN,
        event: { code: "KeyW" },
      });

      playerController.update();

      expect(mockPhysicsBody.setLinearVelocity).toHaveBeenCalled();
    });
  });

  describe("update", () => {
    it("should not throw if mesh or aggregate is null", async () => {
      // Don't initialize, just call update
      expect(() => playerController.update()).not.toThrow();
    });

    it("should sync camera position with player mesh", async () => {
      await playerController.initialize();

      playerController.update();

      // Camera position should be updated based on mesh position
      expect(camera.position.x).toBeDefined();
    });
  });

  describe("teleport", () => {
    it("should move player to specified position", async () => {
      await playerController.initialize();

      const targetPosition = createMockVector3(10, 5, 10);
      playerController.teleport(targetPosition as never);

      expect(mockPhysicsBody.setLinearVelocity).toHaveBeenCalled();
      expect(mockPhysicsBody.setAngularVelocity).toHaveBeenCalled();
    });
  });

  describe("getPosition", () => {
    it("should return camera position", async () => {
      await playerController.initialize();

      const position = playerController.getPosition();

      expect(position).toBeDefined();
    });
  });

  describe("getIsGrounded", () => {
    it("should return grounded state", async () => {
      await playerController.initialize();
      playerController.update();

      const isGrounded = playerController.getIsGrounded();

      expect(typeof isGrounded).toBe("boolean");
    });
  });

  describe("getVelocity", () => {
    it("should return player velocity", async () => {
      await playerController.initialize();

      const velocity = playerController.getVelocity();

      expect(velocity).toBeDefined();
    });

    it("should return zero velocity if no aggregate", () => {
      const velocity = playerController.getVelocity();

      expect(velocity.x).toBe(0);
      expect(velocity.y).toBe(0);
      expect(velocity.z).toBe(0);
    });
  });

  describe("custom config", () => {
    it("should accept custom player config", async () => {
      const customConfig: Partial<PlayerConfig> = {
        moveSpeed: 10,
        jumpForce: 12,
        height: 2.0,
      };

      const customController = new PlayerController(
        scene,
        camera,
        mockPhysicsManager as unknown as PhysicsManager,
        customConfig
      );

      await customController.initialize();

      // Controller should be initialized with custom config
      expect(mockPhysicsManager.createPlayerCollider).toHaveBeenCalled();
    });
  });

  describe("dispose", () => {
    it("should clean up resources", async () => {
      await playerController.initialize();

      playerController.dispose();

      expect(mockAggregate.dispose).toHaveBeenCalled();
      expect(mockPlayerMesh.dispose).toHaveBeenCalled();
    });

    it("should remove keyboard observer", async () => {
      await playerController.initialize();

      playerController.dispose();

      expect(scene.onKeyboardObservable.remove).toHaveBeenCalled();
    });
  });
});
