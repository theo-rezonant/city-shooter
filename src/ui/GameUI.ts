import { Scene } from '@babylonjs/core/scene';
import { AdvancedDynamicTexture } from '@babylonjs/gui/2D/advancedDynamicTexture';
import { Button } from '@babylonjs/gui/2D/controls/button';
import { TextBlock } from '@babylonjs/gui/2D/controls/textBlock';
import { StackPanel } from '@babylonjs/gui/2D/controls/stackPanel';
import { Rectangle } from '@babylonjs/gui/2D/controls/rectangle';
import { Control } from '@babylonjs/gui/2D/controls/control';
import { Observable } from '@babylonjs/core/Misc/observable';
import {
  GameState,
  PointerLockState,
  AssetsValidationStatus,
  PointerLockError,
} from '../types/GameTypes';

/**
 * GameUI - Babylon GUI based interface for the game
 *
 * Provides:
 * - Loading screen with progress
 * - Main menu with Start button
 * - Pointer lock error messages with retry option
 * - Asset reload functionality
 * - Pause menu
 */
export class GameUI {
  private scene: Scene;
  private guiTexture: AdvancedDynamicTexture;

  // UI Panels
  private loadingPanel: Rectangle | null = null;
  private mainMenuPanel: Rectangle | null = null;
  private errorPanel: Rectangle | null = null;
  private pausePanel: Rectangle | null = null;

  // UI Elements
  private startButton: Button | null = null;
  private retryButton: Button | null = null;
  private reloadAssetsButton: Button | null = null;
  private resumeButton: Button | null = null;
  private loadingText: TextBlock | null = null;
  private progressText: TextBlock | null = null;
  private errorText: TextBlock | null = null;

  // State
  private currentState: GameState = GameState.LOADING;
  private isStartButtonEnabled: boolean = false;

  // Observables for external coordination
  public readonly onStartClicked: Observable<void> = new Observable();
  public readonly onRetryClicked: Observable<void> = new Observable();
  public readonly onReloadAssetsClicked: Observable<void> = new Observable();
  public readonly onResumeClicked: Observable<void> = new Observable();

  constructor(scene: Scene) {
    this.scene = scene;
    this.guiTexture = AdvancedDynamicTexture.CreateFullscreenUI('GameUI', true, this.scene);
    this.createUI();
  }

  /**
   * Create all UI components
   */
  private createUI(): void {
    this.createLoadingPanel();
    this.createMainMenuPanel();
    this.createErrorPanel();
    this.createPausePanel();

    // Initially show loading panel
    this.showLoadingScreen();
  }

  /**
   * Create loading screen panel
   */
  private createLoadingPanel(): void {
    this.loadingPanel = new Rectangle('loadingPanel');
    this.loadingPanel.width = '100%';
    this.loadingPanel.height = '100%';
    this.loadingPanel.background = '#1a1a2e';
    this.loadingPanel.thickness = 0;
    this.guiTexture.addControl(this.loadingPanel);

    const stack = new StackPanel('loadingStack');
    stack.width = '400px';
    stack.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.loadingPanel.addControl(stack);

    // Title
    const title = new TextBlock('loadingTitle', 'CITY SHOOTER');
    title.height = '60px';
    title.color = 'white';
    title.fontSize = 36;
    title.fontWeight = 'bold';
    stack.addControl(title);

    // Loading text
    this.loadingText = new TextBlock('loadingText', 'Loading Assets...');
    this.loadingText.height = '40px';
    this.loadingText.color = '#888888';
    this.loadingText.fontSize = 18;
    stack.addControl(this.loadingText);

    // Progress text
    this.progressText = new TextBlock('progressText', '0 / 3 assets loaded');
    this.progressText.height = '30px';
    this.progressText.color = '#666666';
    this.progressText.fontSize = 14;
    stack.addControl(this.progressText);

    // Progress bar background
    const progressBarBg = new Rectangle('progressBarBg');
    progressBarBg.width = '300px';
    progressBarBg.height = '10px';
    progressBarBg.background = '#333333';
    progressBarBg.thickness = 0;
    progressBarBg.cornerRadius = 5;
    stack.addControl(progressBarBg);

    this.loadingPanel.isVisible = false;
  }

  /**
   * Create main menu panel with Start button
   */
  private createMainMenuPanel(): void {
    this.mainMenuPanel = new Rectangle('mainMenuPanel');
    this.mainMenuPanel.width = '100%';
    this.mainMenuPanel.height = '100%';
    this.mainMenuPanel.background = 'rgba(0, 0, 0, 0.7)';
    this.mainMenuPanel.thickness = 0;
    this.guiTexture.addControl(this.mainMenuPanel);

    const stack = new StackPanel('menuStack');
    stack.width = '400px';
    stack.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.mainMenuPanel.addControl(stack);

    // Title
    const title = new TextBlock('menuTitle', 'CITY SHOOTER');
    title.height = '80px';
    title.color = 'white';
    title.fontSize = 48;
    title.fontWeight = 'bold';
    stack.addControl(title);

    // Subtitle
    const subtitle = new TextBlock('menuSubtitle', 'Click Start to begin');
    subtitle.height = '40px';
    subtitle.color = '#aaaaaa';
    subtitle.fontSize = 16;
    stack.addControl(subtitle);

    // Spacer
    const spacer = new Rectangle('menuSpacer');
    spacer.height = '40px';
    spacer.thickness = 0;
    spacer.background = 'transparent';
    stack.addControl(spacer);

    // Start button
    this.startButton = Button.CreateSimpleButton('startButton', 'START GAME');
    this.startButton.width = '200px';
    this.startButton.height = '50px';
    this.startButton.color = 'white';
    this.startButton.fontSize = 18;
    this.startButton.fontWeight = 'bold';
    this.startButton.background = '#16213e';
    this.startButton.cornerRadius = 10;
    this.startButton.thickness = 2;
    this.startButton.isEnabled = false;
    this.startButton.alpha = 0.5;

    // CRITICAL: Use onPointerClickObservable for user activation requirement
    // The pointer lock request MUST happen within this callback
    this.startButton.onPointerClickObservable.add(() => {
      if (this.isStartButtonEnabled) {
        this.onStartClicked.notifyObservers();
      }
    });

    stack.addControl(this.startButton);

    // Instructions
    const instructions = new TextBlock('instructions', 'Use WASD to move, Mouse to look');
    instructions.height = '60px';
    instructions.color = '#666666';
    instructions.fontSize = 12;
    instructions.paddingTop = '20px';
    stack.addControl(instructions);

    this.mainMenuPanel.isVisible = false;
  }

  /**
   * Create error panel for pointer lock and asset errors
   */
  private createErrorPanel(): void {
    this.errorPanel = new Rectangle('errorPanel');
    this.errorPanel.width = '100%';
    this.errorPanel.height = '100%';
    this.errorPanel.background = 'rgba(0, 0, 0, 0.85)';
    this.errorPanel.thickness = 0;
    this.guiTexture.addControl(this.errorPanel);

    const stack = new StackPanel('errorStack');
    stack.width = '500px';
    stack.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.errorPanel.addControl(stack);

    // Error icon/title
    const errorTitle = new TextBlock('errorTitle', '⚠️ Error');
    errorTitle.height = '60px';
    errorTitle.color = '#ff6b6b';
    errorTitle.fontSize = 32;
    errorTitle.fontWeight = 'bold';
    stack.addControl(errorTitle);

    // Error message
    this.errorText = new TextBlock('errorText', '');
    this.errorText.height = '80px';
    this.errorText.color = '#cccccc';
    this.errorText.fontSize = 16;
    this.errorText.textWrapping = true;
    stack.addControl(this.errorText);

    // Spacer
    const spacer = new Rectangle('errorSpacer');
    spacer.height = '30px';
    spacer.thickness = 0;
    spacer.background = 'transparent';
    stack.addControl(spacer);

    // Button container
    const buttonStack = new StackPanel('buttonStack');
    buttonStack.isVertical = false;
    buttonStack.height = '60px';
    buttonStack.width = '400px';
    stack.addControl(buttonStack);

    // Retry button (for pointer lock errors)
    this.retryButton = Button.CreateSimpleButton('retryButton', 'RETRY');
    this.retryButton.width = '150px';
    this.retryButton.height = '45px';
    this.retryButton.color = 'white';
    this.retryButton.fontSize = 16;
    this.retryButton.fontWeight = 'bold';
    this.retryButton.background = '#4a6fa5';
    this.retryButton.cornerRadius = 8;
    this.retryButton.thickness = 0;
    this.retryButton.paddingRight = '10px';

    this.retryButton.onPointerClickObservable.add(() => {
      this.onRetryClicked.notifyObservers();
    });
    buttonStack.addControl(this.retryButton);

    // Reload Assets button
    this.reloadAssetsButton = Button.CreateSimpleButton('reloadButton', 'RELOAD ASSETS');
    this.reloadAssetsButton.width = '180px';
    this.reloadAssetsButton.height = '45px';
    this.reloadAssetsButton.color = 'white';
    this.reloadAssetsButton.fontSize = 16;
    this.reloadAssetsButton.fontWeight = 'bold';
    this.reloadAssetsButton.background = '#e07b39';
    this.reloadAssetsButton.cornerRadius = 8;
    this.reloadAssetsButton.thickness = 0;
    this.reloadAssetsButton.isVisible = false;

    this.reloadAssetsButton.onPointerClickObservable.add(() => {
      this.onReloadAssetsClicked.notifyObservers();
    });
    buttonStack.addControl(this.reloadAssetsButton);

    this.errorPanel.isVisible = false;
  }

  /**
   * Create pause menu panel
   */
  private createPausePanel(): void {
    this.pausePanel = new Rectangle('pausePanel');
    this.pausePanel.width = '100%';
    this.pausePanel.height = '100%';
    this.pausePanel.background = 'rgba(0, 0, 0, 0.6)';
    this.pausePanel.thickness = 0;
    this.guiTexture.addControl(this.pausePanel);

    const stack = new StackPanel('pauseStack');
    stack.width = '300px';
    stack.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.pausePanel.addControl(stack);

    // Pause title
    const pauseTitle = new TextBlock('pauseTitle', 'PAUSED');
    pauseTitle.height = '60px';
    pauseTitle.color = 'white';
    pauseTitle.fontSize = 36;
    pauseTitle.fontWeight = 'bold';
    stack.addControl(pauseTitle);

    // Spacer
    const spacer = new Rectangle('pauseSpacer');
    spacer.height = '30px';
    spacer.thickness = 0;
    spacer.background = 'transparent';
    stack.addControl(spacer);

    // Resume button
    this.resumeButton = Button.CreateSimpleButton('resumeButton', 'RESUME');
    this.resumeButton.width = '180px';
    this.resumeButton.height = '50px';
    this.resumeButton.color = 'white';
    this.resumeButton.fontSize = 18;
    this.resumeButton.fontWeight = 'bold';
    this.resumeButton.background = '#16213e';
    this.resumeButton.cornerRadius = 10;
    this.resumeButton.thickness = 2;

    // Resume also requires user activation for pointer lock
    this.resumeButton.onPointerClickObservable.add(() => {
      this.onResumeClicked.notifyObservers();
    });
    stack.addControl(this.resumeButton);

    // ESC hint
    const escHint = new TextBlock('escHint', 'Press ESC to pause');
    escHint.height = '40px';
    escHint.color = '#666666';
    escHint.fontSize = 12;
    escHint.paddingTop = '20px';
    stack.addControl(escHint);

    this.pausePanel.isVisible = false;
  }

  /**
   * Show loading screen
   */
  public showLoadingScreen(): void {
    this.hideAllPanels();
    this.loadingPanel!.isVisible = true;
    this.currentState = GameState.LOADING;
  }

  /**
   * Show main menu
   */
  public showMainMenu(): void {
    this.hideAllPanels();
    this.mainMenuPanel!.isVisible = true;
    this.currentState = GameState.MAIN_MENU;
  }

  /**
   * Show error panel with specific message
   */
  public showError(message: string, showReloadButton: boolean = false): void {
    this.hideAllPanels();
    this.errorText!.text = message;
    this.retryButton!.isVisible = !showReloadButton;
    this.reloadAssetsButton!.isVisible = showReloadButton;
    this.errorPanel!.isVisible = true;
    this.currentState = GameState.ERROR;
  }

  /**
   * Show pointer lock specific error
   */
  public showPointerLockError(error: PointerLockError): void {
    this.showError(error.message, false);
  }

  /**
   * Show pause menu
   */
  public showPauseMenu(): void {
    this.hideAllPanels();
    this.pausePanel!.isVisible = true;
    this.currentState = GameState.PAUSED;
  }

  /**
   * Hide all UI panels (for gameplay)
   */
  public hideAllPanels(): void {
    this.loadingPanel!.isVisible = false;
    this.mainMenuPanel!.isVisible = false;
    this.errorPanel!.isVisible = false;
    this.pausePanel!.isVisible = false;
  }

  /**
   * Enter gameplay mode (hide UI)
   */
  public enterGameplay(): void {
    this.hideAllPanels();
    this.currentState = GameState.GAMEPLAY;
  }

  /**
   * Update loading progress
   */
  public updateLoadingProgress(loaded: number, total: number, currentAsset: string): void {
    if (this.loadingText) {
      this.loadingText.text = `Loading: ${currentAsset}`;
    }
    if (this.progressText) {
      this.progressText.text = `${loaded} / ${total} assets loaded`;
    }
  }

  /**
   * Enable/disable start button based on asset validation
   */
  public setStartButtonEnabled(enabled: boolean): void {
    this.isStartButtonEnabled = enabled;
    if (this.startButton) {
      this.startButton.isEnabled = enabled;
      this.startButton.alpha = enabled ? 1.0 : 0.5;
      this.startButton.background = enabled ? '#16213e' : '#333333';
    }
  }

  /**
   * Update start button based on asset validation status
   */
  public updateFromAssetsStatus(status: AssetsValidationStatus): void {
    this.setStartButtonEnabled(status.allAssetsReady);

    if (!status.allAssetsReady && this.startButton) {
      // Show which assets failed
      const failedAssets: string[] = [];
      if (!status.townValidated && status.townLoaded) failedAssets.push('Town');
      if (!status.laserGunValidated && status.laserGunLoaded) failedAssets.push('Laser Gun');
      if (!status.soldierValidated && status.soldierLoaded) failedAssets.push('Soldier');

      if (failedAssets.length > 0) {
        this.showError(
          `Failed to load: ${failedAssets.join(', ')}\nPlease reload the assets.`,
          true
        );
      }
    }
  }

  /**
   * Update UI based on pointer lock state
   */
  public updateFromPointerLockState(state: PointerLockState): void {
    switch (state) {
      case PointerLockState.LOCKED:
        this.enterGameplay();
        break;
      case PointerLockState.UNLOCKED:
        if (this.currentState === GameState.GAMEPLAY) {
          // User exited pointer lock (ESC), show pause menu
          this.showPauseMenu();
        }
        break;
      case PointerLockState.ERROR:
        // Error will be shown via showPointerLockError
        break;
      case PointerLockState.REQUESTING:
        // Could show a brief "Capturing mouse..." indicator
        break;
    }
  }

  /**
   * Get current game state
   */
  public getCurrentState(): GameState {
    return this.currentState;
  }

  /**
   * Dispose UI resources
   */
  public dispose(): void {
    this.onStartClicked.clear();
    this.onRetryClicked.clear();
    this.onReloadAssetsClicked.clear();
    this.onResumeClicked.clear();
    this.guiTexture.dispose();
  }
}
