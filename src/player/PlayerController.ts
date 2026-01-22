import {
  Scene,
  FreeCamera,
  Vector3,
  MeshBuilder,
  Mesh,
  PhysicsAggregate,
  KeyboardInfo,
  KeyboardEventTypes,
  Observer,
  Ray,
} from "@babylonjs/core";
import {
  PhysicsManager,
  CollisionGroups,
  CollisionMasks,
} from "@/physics/PhysicsManager";

/**
 * Player movement configuration
 */
export interface PlayerConfig {
  moveSpeed: number;
  sprintMultiplier: number;
  jumpForce: number;
  height: number;
  radius: number;
  mass: number;
  eyeHeight: number;
  groundCheckDistance: number;
}

const DEFAULT_CONFIG: PlayerConfig = {
  moveSpeed: 5.0,
  sprintMultiplier: 1.8,
  jumpForce: 6.0,
  height: 1.8,
  radius: 0.3,
  mass: 80,
  eyeHeight: 1.6,
  groundCheckDistance: 0.2,
};

/**
 * Input state tracking
 */
interface InputState {
  forward: boolean;
  backward: boolean;
  left: boolean;
  right: boolean;
  jump: boolean;
  sprint: boolean;
}

/**
 * Player controller with physics-based movement
 * Uses Havok physics for collision and movement
 */
export class PlayerController {
  private scene: Scene;
  private camera: FreeCamera;
  private physicsManager: PhysicsManager;
  private config: PlayerConfig;

  private playerMesh: Mesh | null = null;
  private playerAggregate: PhysicsAggregate | null = null;
  private keyboardObserver: Observer<KeyboardInfo> | null = null;

  private inputState: InputState = {
    forward: false,
    backward: false,
    left: false,
    right: false,
    jump: false,
    sprint: false,
  };

  private isGrounded = false;

  constructor(
    scene: Scene,
    camera: FreeCamera,
    physicsManager: PhysicsManager,
    config: Partial<PlayerConfig> = {}
  ) {
    this.scene = scene;
    this.camera = camera;
    this.physicsManager = physicsManager;
    this.config = { ...DEFAULT_CONFIG, ...config };
  }

  /**
   * Initialize the player controller
   */
  async initialize(): Promise<void> {
    console.log("[PlayerController] Initializing...");

    // Create player collision mesh (invisible capsule)
    this.createPlayerMesh();

    // Setup physics aggregate
    this.setupPhysics();

    // Setup keyboard input handlers
    this.setupInputHandlers();

    console.log("[PlayerController] Initialized successfully");
  }

  /**
   * Create the player collision mesh
   */
  private createPlayerMesh(): void {
    // Create a capsule for player collision
    this.playerMesh = MeshBuilder.CreateCapsule(
      "player",
      {
        height: this.config.height,
        radius: this.config.radius,
      },
      this.scene
    );

    // Make it invisible (camera represents the player visually)
    this.playerMesh.isVisible = false;

    // Position at camera location
    this.playerMesh.position = this.camera.position.clone();
    this.playerMesh.position.y -=
      this.config.eyeHeight - this.config.height / 2;

    // Disable picking to avoid interfering with raycasts
    this.playerMesh.isPickable = false;
  }

  /**
   * Setup physics for the player
   */
  private setupPhysics(): void {
    if (!this.playerMesh || !this.physicsManager.isReady()) {
      console.error(
        "[PlayerController] Cannot setup physics - mesh or physics not ready"
      );
      return;
    }

    // Create player physics aggregate with capsule shape
    this.playerAggregate = this.physicsManager.createPlayerCollider(
      this.playerMesh,
      this.config.height,
      this.config.radius,
      {
        mass: this.config.mass,
        friction: 0.8,
        restitution: 0.0,
        collisionGroup: CollisionGroups.PLAYER,
        collisionMask: CollisionMasks.PLAYER,
      }
    );

    if (this.playerAggregate?.body) {
      // Disable angular velocity (no rotation)
      this.playerAggregate.body.setAngularVelocity(Vector3.Zero());
      this.playerAggregate.body.setAngularDamping(1.0);

      // Set linear damping for smoother stops
      this.playerAggregate.body.setLinearDamping(0.1);
    }

    console.log("[PlayerController] Physics setup complete");
  }

  /**
   * Setup keyboard input handlers
   */
  private setupInputHandlers(): void {
    this.keyboardObserver = this.scene.onKeyboardObservable.add((kbInfo) => {
      const pressed = kbInfo.type === KeyboardEventTypes.KEYDOWN;

      switch (kbInfo.event.code) {
        case "KeyW":
        case "ArrowUp":
          this.inputState.forward = pressed;
          break;
        case "KeyS":
        case "ArrowDown":
          this.inputState.backward = pressed;
          break;
        case "KeyA":
        case "ArrowLeft":
          this.inputState.left = pressed;
          break;
        case "KeyD":
        case "ArrowRight":
          this.inputState.right = pressed;
          break;
        case "Space":
          this.inputState.jump = pressed;
          break;
        case "ShiftLeft":
        case "ShiftRight":
          this.inputState.sprint = pressed;
          break;
      }
    });
  }

  /**
   * Update the player each frame
   */
  update(): void {
    if (!this.playerMesh || !this.playerAggregate?.body) return;

    // Check if grounded
    this.checkGrounded();

    // Calculate movement direction based on camera orientation
    const movement = this.calculateMovementDirection();

    // Apply movement forces
    this.applyMovement(movement);

    // Handle jumping
    this.handleJump();

    // Sync camera position with player mesh
    this.syncCameraPosition();
  }

  /**
   * Check if the player is on the ground
   */
  private checkGrounded(): void {
    if (!this.playerMesh) return;

    const rayOrigin = this.playerMesh.position.clone();
    const rayDirection = new Vector3(0, -1, 0);
    const rayLength = this.config.height / 2 + this.config.groundCheckDistance;

    const ray = new Ray(rayOrigin, rayDirection, rayLength);
    const hit = this.scene.pickWithRay(ray, (mesh) => {
      // Ignore the player mesh
      return mesh !== this.playerMesh && mesh.isPickable;
    });

    this.isGrounded = hit?.hit ?? false;
  }

  /**
   * Calculate movement direction based on input and camera orientation
   */
  private calculateMovementDirection(): Vector3 {
    const direction = Vector3.Zero();

    if (this.inputState.forward) direction.z += 1;
    if (this.inputState.backward) direction.z -= 1;
    if (this.inputState.left) direction.x -= 1;
    if (this.inputState.right) direction.x += 1;

    // Normalize to prevent faster diagonal movement
    if (direction.length() > 0) {
      direction.normalize();
    }

    // Transform direction to world space based on camera rotation
    const cameraRotation = this.camera.rotation.y;
    const cos = Math.cos(cameraRotation);
    const sin = Math.sin(cameraRotation);

    const worldDirection = new Vector3(
      direction.x * cos + direction.z * sin,
      0,
      direction.z * cos - direction.x * sin
    );

    return worldDirection;
  }

  /**
   * Apply movement to the physics body
   */
  private applyMovement(direction: Vector3): void {
    if (!this.playerAggregate?.body) return;

    // Calculate speed with sprint modifier
    let speed = this.config.moveSpeed;
    if (this.inputState.sprint) {
      speed *= this.config.sprintMultiplier;
    }

    // Get current velocity
    const currentVelocity = this.playerAggregate.body.getLinearVelocity();

    // Calculate target horizontal velocity
    const targetVelocity = direction.scale(speed);

    // Apply velocity (preserve vertical component)
    this.playerAggregate.body.setLinearVelocity(
      new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z)
    );
  }

  /**
   * Handle jump input
   */
  private handleJump(): void {
    if (!this.playerAggregate?.body) return;

    if (this.inputState.jump && this.isGrounded) {
      const currentVelocity = this.playerAggregate.body.getLinearVelocity();
      this.playerAggregate.body.setLinearVelocity(
        new Vector3(currentVelocity.x, this.config.jumpForce, currentVelocity.z)
      );
    }
  }

  /**
   * Sync the camera position with the player mesh
   */
  private syncCameraPosition(): void {
    if (!this.playerMesh) return;

    // Update camera position to follow player mesh
    this.camera.position.x = this.playerMesh.position.x;
    this.camera.position.y =
      this.playerMesh.position.y +
      this.config.height / 2 -
      (this.config.height - this.config.eyeHeight);
    this.camera.position.z = this.playerMesh.position.z;
  }

  /**
   * Teleport player to a position
   */
  teleport(position: Vector3): void {
    if (!this.playerMesh || !this.playerAggregate?.body) return;

    // Calculate mesh position from camera position
    const meshPosition = position.clone();
    meshPosition.y -= this.config.eyeHeight - this.config.height / 2;

    this.playerMesh.position = meshPosition;
    this.playerAggregate.body.setLinearVelocity(Vector3.Zero());
    this.playerAggregate.body.setAngularVelocity(Vector3.Zero());

    // Sync camera
    this.camera.position = position.clone();
  }

  /**
   * Get the player's current position (camera position)
   */
  getPosition(): Vector3 {
    return this.camera.position.clone();
  }

  /**
   * Get the player mesh
   */
  getMesh(): Mesh | null {
    return this.playerMesh;
  }

  /**
   * Check if player is grounded
   */
  getIsGrounded(): boolean {
    return this.isGrounded;
  }

  /**
   * Get the player's velocity
   */
  getVelocity(): Vector3 {
    return this.playerAggregate?.body?.getLinearVelocity() ?? Vector3.Zero();
  }

  /**
   * Dispose of the player controller
   */
  dispose(): void {
    if (this.keyboardObserver) {
      this.scene.onKeyboardObservable.remove(this.keyboardObserver);
      this.keyboardObserver = null;
    }

    if (this.playerAggregate) {
      this.playerAggregate.dispose();
      this.playerAggregate = null;
    }

    if (this.playerMesh) {
      this.playerMesh.dispose();
      this.playerMesh = null;
    }
  }
}
