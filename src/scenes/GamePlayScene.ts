import {
  Color4,
  FreeCamera,
  Vector3,
  HemisphericLight,
  MeshBuilder,
  StandardMaterial,
  Color3,
} from '@babylonjs/core';
import { AdvancedDynamicTexture, TextBlock, Control } from '@babylonjs/gui';
import { BaseScene } from '../core/IScene';
import { AppState } from '../core/AppState';
import type { App } from '../core/App';

/**
 * Main gameplay scene where the actual game takes place.
 * This is a placeholder implementation that will be expanded
 * with proper gameplay mechanics in future tickets.
 */
export class GamePlayScene extends BaseScene {
  readonly state = AppState.GAME_PLAY;

  /** The GUI texture for the HUD */
  private guiTexture: AdvancedDynamicTexture | null = null;

  /** The main camera */
  private camera: FreeCamera | null = null;

  /** Pointer lock change handler */
  private pointerLockChangeHandler: (() => void) | null = null;

  /** Key press handler for escape */
  private keyDownHandler: ((event: KeyboardEvent) => void) | null = null;

  constructor(app: App) {
    super(app);
  }

  async init(): Promise<void> {
    // Set background color (sky)
    this.scene.clearColor = new Color4(0.5, 0.7, 0.9, 1);

    // Enable physics if available
    if (this.app.physics) {
      this.scene.enablePhysics(new Vector3(0, -9.81, 0), this.app.physics);
    }

    // Create camera
    this.createCamera();

    // Create basic lighting
    this.createLighting();

    // Create placeholder environment
    this.createPlaceholderEnvironment();

    // Create HUD
    this.createHUD();

    // Set up input handlers
    this.setupInputHandlers();
  }

  /**
   * Create the FPS camera.
   */
  private createCamera(): void {
    this.camera = new FreeCamera('playerCamera', new Vector3(0, 2, -10), this.scene);
    this.camera.setTarget(new Vector3(0, 2, 0));
    this.camera.attachControl(this.app.canvas, true);

    // Configure camera for FPS controls
    this.camera.speed = 0.5;
    this.camera.angularSensibility = 1000;
    this.camera.inertia = 0.5;

    // Enable WASD keys
    this.camera.keysUp = [87]; // W
    this.camera.keysDown = [83]; // S
    this.camera.keysLeft = [65]; // A
    this.camera.keysRight = [68]; // D
  }

  /**
   * Create basic lighting.
   */
  private createLighting(): void {
    const light = new HemisphericLight('mainLight', new Vector3(0, 1, 0), this.scene);
    light.intensity = 0.8;
    light.groundColor = new Color3(0.2, 0.2, 0.2);
  }

  /**
   * Create a placeholder environment for testing.
   * This will be replaced with the actual city map.
   */
  private createPlaceholderEnvironment(): void {
    // Create ground
    const ground = MeshBuilder.CreateGround('ground', { width: 50, height: 50 }, this.scene);
    const groundMaterial = new StandardMaterial('groundMaterial', this.scene);
    groundMaterial.diffuseColor = new Color3(0.3, 0.5, 0.3);
    ground.material = groundMaterial;

    // Create some placeholder cubes
    for (let i = 0; i < 5; i++) {
      const box = MeshBuilder.CreateBox(`box_${i}`, { size: 2 }, this.scene);
      box.position = new Vector3(Math.random() * 20 - 10, 1, Math.random() * 20 - 10);

      const boxMaterial = new StandardMaterial(`boxMaterial_${i}`, this.scene);
      boxMaterial.diffuseColor = new Color3(Math.random(), Math.random(), Math.random());
      box.material = boxMaterial;
    }
  }

  /**
   * Create the HUD (Heads-Up Display).
   */
  private createHUD(): void {
    this.guiTexture = AdvancedDynamicTexture.CreateFullscreenUI('GamePlayHUD', true, this.scene);

    // Crosshair
    const crosshairVertical = new TextBlock('crosshairV', '|');
    crosshairVertical.color = 'white';
    crosshairVertical.fontSize = 24;
    crosshairVertical.fontWeight = 'bold';
    this.guiTexture.addControl(crosshairVertical);

    const crosshairHorizontal = new TextBlock('crosshairH', '—');
    crosshairHorizontal.color = 'white';
    crosshairHorizontal.fontSize = 24;
    crosshairHorizontal.fontWeight = 'bold';
    this.guiTexture.addControl(crosshairHorizontal);

    // Instructions text
    const instructionsText = new TextBlock(
      'instructions',
      'WASD to move | Mouse to look | ESC to pause'
    );
    instructionsText.color = 'rgba(255, 255, 255, 0.7)';
    instructionsText.fontSize = 16;
    instructionsText.top = '20px';
    instructionsText.verticalAlignment = Control.VERTICAL_ALIGNMENT_TOP;
    this.guiTexture.addControl(instructionsText);

    // Placeholder ammo counter
    const ammoText = new TextBlock('ammo', 'AMMO: 30 / 90');
    ammoText.color = 'white';
    ammoText.fontSize = 18;
    ammoText.textHorizontalAlignment = Control.HORIZONTAL_ALIGNMENT_RIGHT;
    ammoText.textVerticalAlignment = Control.VERTICAL_ALIGNMENT_BOTTOM;
    ammoText.paddingRight = '20px';
    ammoText.paddingBottom = '20px';
    this.guiTexture.addControl(ammoText);

    // Placeholder health bar
    const healthText = new TextBlock('health', 'HEALTH: 100');
    healthText.color = '#4CAF50';
    healthText.fontSize = 18;
    healthText.textHorizontalAlignment = Control.HORIZONTAL_ALIGNMENT_LEFT;
    healthText.textVerticalAlignment = Control.VERTICAL_ALIGNMENT_BOTTOM;
    healthText.paddingLeft = '20px';
    healthText.paddingBottom = '20px';
    this.guiTexture.addControl(healthText);
  }

  /**
   * Set up input event handlers.
   */
  private setupInputHandlers(): void {
    // Handle pointer lock changes
    this.pointerLockChangeHandler = (): void => {
      if (!this.app.isPointerLocked) {
        console.log('Pointer lock lost');
        // Could show a pause menu here
      }
    };
    document.addEventListener('pointerlockchange', this.pointerLockChangeHandler);

    // Handle escape key to go to game over (for testing)
    this.keyDownHandler = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') {
        // Exit pointer lock
        this.app.exitPointerLock();

        // Transition to game over (for testing purposes)
        // In a real game, this would show a pause menu
        this.app.goToState(AppState.GAME_OVER).catch((error) => {
          console.error('Failed to transition to game over:', error);
        });
      }
    };
    this.addEventListener(window, 'keydown', this.keyDownHandler as EventListener);
  }

  update(deltaTime: number): void {
    // Placeholder update logic
    // In a full implementation, this would handle:
    // - Player movement and physics
    // - Enemy AI
    // - Collision detection
    // - Game state updates

    // Example: Could add subtle camera bob here based on deltaTime
    void deltaTime; // Suppress unused warning
  }

  dispose(): void {
    // Remove pointer lock change listener
    if (this.pointerLockChangeHandler) {
      document.removeEventListener('pointerlockchange', this.pointerLockChangeHandler);
      this.pointerLockChangeHandler = null;
    }

    // Exit pointer lock if active
    this.app.exitPointerLock();

    // Detach camera controls
    if (this.camera) {
      this.camera.detachControl();
      this.camera = null;
    }

    // Dispose GUI
    if (this.guiTexture) {
      this.guiTexture.dispose();
      this.guiTexture = null;
    }

    // Call parent dispose
    super.dispose();
  }
}
