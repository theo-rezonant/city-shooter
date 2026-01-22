import { Color4, FreeCamera, Vector3 } from '@babylonjs/core';
import { AssetsManager } from '@babylonjs/core/Misc/assetsManager';
import { AdvancedDynamicTexture, TextBlock, Rectangle, Control } from '@babylonjs/gui';
import { BaseScene } from '../core/IScene';
import { AppState } from '../core/AppState';
import type { App } from '../core/App';

/**
 * Loading scene that displays a loading bar while assets are being loaded.
 * This scene uses the Babylon.js AssetsManager to load game assets
 * before transitioning to the Main Menu.
 */
export class LoadingScene extends BaseScene {
  readonly state = AppState.LOADING;

  /** The GUI texture for the loading screen */
  private guiTexture: AdvancedDynamicTexture | null = null;

  /** The progress bar container */
  private progressBarContainer: Rectangle | null = null;

  /** The progress bar fill */
  private progressBarFill: Rectangle | null = null;

  /** The loading text */
  private loadingText: TextBlock | null = null;

  /** The assets manager instance */
  private assetsManager: AssetsManager | null = null;

  constructor(app: App) {
    super(app);
  }

  async init(): Promise<void> {
    // Set background color
    this.scene.clearColor = new Color4(0.05, 0.05, 0.1, 1);

    // Create a basic camera (required for scene to render)
    const camera = new FreeCamera('loadingCamera', new Vector3(0, 0, -10), this.scene);
    camera.setTarget(Vector3.Zero());

    // Create the GUI
    this.createLoadingUI();

    // Initialize and run the assets manager
    await this.loadAssets();
  }

  /**
   * Create the loading screen UI using Babylon GUI.
   */
  private createLoadingUI(): void {
    // Create fullscreen GUI
    this.guiTexture = AdvancedDynamicTexture.CreateFullscreenUI('LoadingUI', true, this.scene);

    // Create title text
    const titleText = new TextBlock('title', 'CITY SHOOTER');
    titleText.color = 'white';
    titleText.fontSize = 48;
    titleText.fontWeight = 'bold';
    titleText.top = '-100px';
    titleText.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.guiTexture.addControl(titleText);

    // Create loading text
    this.loadingText = new TextBlock('loadingText', 'Loading... 0%');
    this.loadingText.color = 'white';
    this.loadingText.fontSize = 24;
    this.loadingText.top = '20px';
    this.loadingText.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.guiTexture.addControl(this.loadingText);

    // Create progress bar container
    this.progressBarContainer = new Rectangle('progressBarContainer');
    this.progressBarContainer.width = '400px';
    this.progressBarContainer.height = '30px';
    this.progressBarContainer.cornerRadius = 5;
    this.progressBarContainer.color = 'white';
    this.progressBarContainer.thickness = 2;
    this.progressBarContainer.background = 'rgba(255, 255, 255, 0.1)';
    this.progressBarContainer.top = '70px';
    this.progressBarContainer.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.guiTexture.addControl(this.progressBarContainer);

    // Create progress bar fill
    this.progressBarFill = new Rectangle('progressBarFill');
    this.progressBarFill.width = '0%';
    this.progressBarFill.height = '100%';
    this.progressBarFill.cornerRadius = 3;
    this.progressBarFill.background = '#4CAF50';
    this.progressBarFill.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_LEFT;
    this.progressBarContainer.addControl(this.progressBarFill);
  }

  /**
   * Update the loading progress UI.
   * @param progress Progress value from 0 to 1
   */
  private updateProgress(progress: number): void {
    const percentage = Math.round(progress * 100);

    if (this.loadingText) {
      this.loadingText.text = `Loading... ${percentage}%`;
    }

    if (this.progressBarFill) {
      this.progressBarFill.width = `${percentage}%`;
    }
  }

  /**
   * Load game assets using the AssetsManager.
   * This is designed to support loading large assets like the city map and laser gun.
   */
  private async loadAssets(): Promise<void> {
    this.assetsManager = new AssetsManager(this.scene);

    // Configure the assets manager
    this.assetsManager.useDefaultLoadingScreen = false;

    // Register progress callback
    this.assetsManager.onProgress = (remainingCount, totalCount, _lastFinishedTask): void => {
      const progress = 1 - remainingCount / totalCount;
      this.updateProgress(progress);
    };

    // Register completion callback
    this.assetsManager.onFinish = (_tasks): void => {
      console.log('All assets loaded successfully');
      this.updateProgress(1);

      // Small delay to show 100% before transitioning
      setTimeout(() => {
        this.transitionToMainMenu();
      }, 500);
    };

    // Register error callback
    this.assetsManager.onTaskError = (task): void => {
      console.error(`Error loading asset: ${task.name}`, task.errorObject);
    };

    // Note: Actual asset loading tasks would be added here
    // For example:
    // const cityMapTask = this.assetsManager.addMeshTask('cityMap', '', '/map/source/', 'town4new.glb');
    // const laserGunTask = this.assetsManager.addMeshTask('laserGun', '', '/laser-gun/', 'laser-gun.glb');

    // For now, we'll simulate loading with a minimum time
    // to ensure the loading screen is visible
    const minimumLoadTime = this.simulateMinimumLoadTime(1500);

    // Start loading
    await Promise.all([this.assetsManager.loadAsync(), minimumLoadTime]);
  }

  /**
   * Simulate a minimum loading time to ensure the loading screen is visible.
   * This is useful during development when assets load quickly.
   * @param ms Minimum time in milliseconds
   */
  private simulateMinimumLoadTime(ms: number): Promise<void> {
    return new Promise((resolve) => {
      let elapsed = 0;
      const interval = 50;
      const timer = setInterval(() => {
        elapsed += interval;
        const progress = Math.min(elapsed / ms, 1);
        this.updateProgress(progress);

        if (elapsed >= ms) {
          clearInterval(timer);
          resolve();
        }
      }, interval);
    });
  }

  /**
   * Transition to the main menu scene.
   */
  private transitionToMainMenu(): void {
    this.app.goToState(AppState.MAIN_MENU).catch((error) => {
      console.error('Failed to transition to main menu:', error);
    });
  }

  update(_deltaTime: number): void {
    // No update logic needed for loading scene
  }

  dispose(): void {
    // Dispose GUI
    if (this.guiTexture) {
      this.guiTexture.dispose();
      this.guiTexture = null;
    }

    // Clear references
    this.progressBarContainer = null;
    this.progressBarFill = null;
    this.loadingText = null;
    this.assetsManager = null;

    // Call parent dispose to clean up scene and event listeners
    super.dispose();
  }
}
