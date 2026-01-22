import { AdvancedDynamicTexture } from '@babylonjs/gui/2D/advancedDynamicTexture';
import { Button } from '@babylonjs/gui/2D/controls/button';
import { TextBlock } from '@babylonjs/gui/2D/controls/textBlock';
import { Rectangle } from '@babylonjs/gui/2D/controls/rectangle';
import { Control } from '@babylonjs/gui/2D/controls/control';
import { Engine } from '@babylonjs/core/Engines/engine';

/**
 * Callback type for pointer lock state changes
 */
export type PointerLockCallback = (isLocked: boolean) => void;

/**
 * Game UI Manager
 *
 * Handles the game's UI elements including:
 * - Enter Game button for pointer lock
 * - Pause menu when pointer lock is lost
 * - Loading screens
 */
export class GameUI {
  private engine: Engine;
  private guiTexture: AdvancedDynamicTexture;
  private enterButton: Button | null = null;
  private pauseOverlay: Rectangle | null = null;
  private isPointerLocked: boolean = false;
  private onPointerLockChange: PointerLockCallback | null = null;

  /**
   * Create a new GameUI instance
   * @param engine - The Babylon.js engine
   */
  constructor(engine: Engine) {
    this.engine = engine;

    // Create fullscreen GUI
    this.guiTexture = AdvancedDynamicTexture.CreateFullscreenUI('gameUI');

    // Setup pointer lock listeners
    this.setupPointerLockListeners();
  }

  /**
   * Set up browser pointer lock event listeners
   */
  private setupPointerLockListeners(): void {
    const canvas = this.engine.getRenderingCanvas();
    if (!canvas) return;

    // Listen for pointer lock changes
    document.addEventListener('pointerlockchange', () => {
      this.isPointerLocked = document.pointerLockElement === canvas;
      this.handlePointerLockChange();
    });

    // Listen for pointer lock errors
    document.addEventListener('pointerlockerror', () => {
      console.error('Pointer lock failed');
      this.isPointerLocked = false;
      this.handlePointerLockChange();
    });
  }

  /**
   * Handle pointer lock state changes
   */
  private handlePointerLockChange(): void {
    if (this.isPointerLocked) {
      // Hide enter button and pause overlay
      this.hideEnterButton();
      this.hidePauseOverlay();
    } else {
      // Show pause overlay when lock is lost
      this.showPauseOverlay();
    }

    // Call user callback
    if (this.onPointerLockChange) {
      this.onPointerLockChange(this.isPointerLocked);
    }
  }

  /**
   * Create and show the "Enter Game" button
   * This button triggers pointer lock on user gesture
   */
  public showEnterButton(): void {
    if (this.enterButton) {
      this.enterButton.isVisible = true;
      return;
    }

    // Create the enter button
    this.enterButton = Button.CreateSimpleButton('enterButton', 'Enter Game');
    this.enterButton.width = '200px';
    this.enterButton.height = '60px';
    this.enterButton.color = 'white';
    this.enterButton.background = '#4a90d9';
    this.enterButton.cornerRadius = 10;
    this.enterButton.thickness = 2;
    this.enterButton.fontSize = 24;
    this.enterButton.fontFamily = 'Arial';
    this.enterButton.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.enterButton.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_CENTER;

    // Hover effects
    this.enterButton.onPointerEnterObservable.add(() => {
      if (this.enterButton) {
        this.enterButton.background = '#5aa0e9';
      }
    });

    this.enterButton.onPointerOutObservable.add(() => {
      if (this.enterButton) {
        this.enterButton.background = '#4a90d9';
      }
    });

    // Click handler - request pointer lock
    this.enterButton.onPointerUpObservable.add(() => {
      this.requestPointerLock();
    });

    this.guiTexture.addControl(this.enterButton);
  }

  /**
   * Hide the enter button
   */
  public hideEnterButton(): void {
    if (this.enterButton) {
      this.enterButton.isVisible = false;
    }
  }

  /**
   * Show the pause overlay (when pointer lock is lost)
   */
  public showPauseOverlay(): void {
    if (this.pauseOverlay) {
      this.pauseOverlay.isVisible = true;
      return;
    }

    // Create semi-transparent overlay
    this.pauseOverlay = new Rectangle('pauseOverlay');
    this.pauseOverlay.width = '100%';
    this.pauseOverlay.height = '100%';
    this.pauseOverlay.background = 'rgba(0, 0, 0, 0.7)';
    this.pauseOverlay.thickness = 0;

    // Paused text
    const pausedText = new TextBlock('pausedText', 'PAUSED');
    pausedText.color = 'white';
    pausedText.fontSize = 48;
    pausedText.fontFamily = 'Arial';
    pausedText.fontWeight = 'bold';
    pausedText.top = '-60px';
    pausedText.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.pauseOverlay.addControl(pausedText);

    // Resume button
    const resumeButton = Button.CreateSimpleButton('resumeButton', 'Click to Resume');
    resumeButton.width = '250px';
    resumeButton.height = '50px';
    resumeButton.color = 'white';
    resumeButton.background = '#4a90d9';
    resumeButton.cornerRadius = 8;
    resumeButton.thickness = 2;
    resumeButton.fontSize = 20;
    resumeButton.fontFamily = 'Arial';
    resumeButton.top = '30px';
    resumeButton.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;

    resumeButton.onPointerEnterObservable.add(() => {
      resumeButton.background = '#5aa0e9';
    });

    resumeButton.onPointerOutObservable.add(() => {
      resumeButton.background = '#4a90d9';
    });

    resumeButton.onPointerUpObservable.add(() => {
      this.requestPointerLock();
    });

    this.pauseOverlay.addControl(resumeButton);

    // Instructions text
    const instructionText = new TextBlock('instructionText', 'Press ESC to pause at any time');
    instructionText.color = 'rgba(255, 255, 255, 0.7)';
    instructionText.fontSize = 16;
    instructionText.fontFamily = 'Arial';
    instructionText.top = '100px';
    instructionText.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.pauseOverlay.addControl(instructionText);

    this.guiTexture.addControl(this.pauseOverlay);
  }

  /**
   * Hide the pause overlay
   */
  public hidePauseOverlay(): void {
    if (this.pauseOverlay) {
      this.pauseOverlay.isVisible = false;
    }
  }

  /**
   * Request pointer lock from the browser
   * Must be called from a user gesture (click, etc.)
   */
  public requestPointerLock(): void {
    const canvas = this.engine.getRenderingCanvas();
    if (!canvas) {
      console.error('No canvas available for pointer lock');
      return;
    }

    // Use the engine's built-in pointer lock method if available
    if (this.engine.enterPointerlock) {
      this.engine.enterPointerlock();
    } else {
      // Fallback to direct canvas request
      canvas.requestPointerLock();
    }
  }

  /**
   * Exit pointer lock
   */
  public exitPointerLock(): void {
    if (document.pointerLockElement) {
      document.exitPointerLock();
    }
  }

  /**
   * Check if pointer is currently locked
   */
  public isLocked(): boolean {
    return this.isPointerLocked;
  }

  /**
   * Set callback for pointer lock state changes
   * @param callback - Function called when pointer lock state changes
   */
  public setPointerLockCallback(callback: PointerLockCallback): void {
    this.onPointerLockChange = callback;
  }

  /**
   * Hide the loading screen
   */
  public hideLoadingScreen(): void {
    const loadingElement = document.getElementById('loading');
    if (loadingElement) {
      loadingElement.style.display = 'none';
    }
  }

  /**
   * Show the loading screen
   */
  public showLoadingScreen(): void {
    const loadingElement = document.getElementById('loading');
    if (loadingElement) {
      loadingElement.style.display = 'block';
    }
  }

  /**
   * Get the GUI texture for custom controls
   */
  public getGUITexture(): AdvancedDynamicTexture {
    return this.guiTexture;
  }

  /**
   * Dispose of UI resources
   */
  public dispose(): void {
    this.guiTexture.dispose();
  }
}
