import { Color4, FreeCamera, Vector3 } from '@babylonjs/core';
import { AdvancedDynamicTexture, TextBlock, Button, Control, StackPanel } from '@babylonjs/gui';
import { BaseScene } from '../core/IScene';
import { AppState } from '../core/AppState';
import type { App } from '../core/App';

/**
 * Main menu scene that displays the game title and start button.
 * Handles user interaction to start the game and manages audio unlock
 * and pointer lock requests.
 */
export class MainMenuScene extends BaseScene {
  readonly state = AppState.MAIN_MENU;

  /** The GUI texture for the main menu */
  private guiTexture: AdvancedDynamicTexture | null = null;

  constructor(app: App) {
    super(app);
  }

  async init(): Promise<void> {
    // Set background color
    this.scene.clearColor = new Color4(0.02, 0.02, 0.08, 1);

    // Create a basic camera
    const camera = new FreeCamera('menuCamera', new Vector3(0, 0, -10), this.scene);
    camera.setTarget(Vector3.Zero());

    // Create the menu UI
    this.createMenuUI();
  }

  /**
   * Create the main menu UI using Babylon GUI.
   */
  private createMenuUI(): void {
    // Create fullscreen GUI
    this.guiTexture = AdvancedDynamicTexture.CreateFullscreenUI('MainMenuUI', true, this.scene);

    // Create container panel
    const panel = new StackPanel('menuPanel');
    panel.width = '400px';
    panel.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.guiTexture.addControl(panel);

    // Create title text
    const titleText = new TextBlock('title', 'CITY SHOOTER');
    titleText.color = 'white';
    titleText.fontSize = 56;
    titleText.fontWeight = 'bold';
    titleText.height = '80px';
    titleText.paddingBottom = '40px';
    panel.addControl(titleText);

    // Create subtitle text
    const subtitleText = new TextBlock('subtitle', 'A First-Person Shooter Experience');
    subtitleText.color = '#888888';
    subtitleText.fontSize = 18;
    subtitleText.height = '40px';
    subtitleText.paddingBottom = '60px';
    panel.addControl(subtitleText);

    // Create "Enter Game" button
    const enterGameButton = this.createButton('Enter Game', '#4CAF50', () => {
      this.onEnterGameClicked();
    });
    enterGameButton.paddingBottom = '20px';
    panel.addControl(enterGameButton);

    // Create "Settings" button (placeholder)
    const settingsButton = this.createButton('Settings', '#2196F3', () => {
      console.log('Settings clicked - not yet implemented');
    });
    settingsButton.paddingBottom = '20px';
    panel.addControl(settingsButton);

    // Create "Quit" button (for Electron builds, disabled on web)
    const quitButton = this.createButton('Quit', '#f44336', () => {
      console.log('Quit clicked - only works in Electron builds');
    });
    panel.addControl(quitButton);

    // Add instructions text at the bottom
    const instructionsText = new TextBlock('instructions', 'Click "Enter Game" to start playing');
    instructionsText.color = '#666666';
    instructionsText.fontSize = 14;
    instructionsText.top = '150px';
    instructionsText.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.guiTexture.addControl(instructionsText);

    // Add version text
    const versionText = new TextBlock('version', 'v1.0.0');
    versionText.color = '#444444';
    versionText.fontSize = 12;
    versionText.textHorizontalAlignment = Control.HORIZONTAL_ALIGNMENT_RIGHT;
    versionText.textVerticalAlignment = Control.VERTICAL_ALIGNMENT_BOTTOM;
    versionText.paddingRight = '20px';
    versionText.paddingBottom = '20px';
    this.guiTexture.addControl(versionText);
  }

  /**
   * Create a styled button.
   * @param text Button text
   * @param color Background color
   * @param onClick Click handler
   */
  private createButton(text: string, color: string, onClick: () => void): Button {
    const button = Button.CreateSimpleButton(`btn_${text}`, text);
    button.width = '250px';
    button.height = '50px';
    button.color = 'white';
    button.background = color;
    button.cornerRadius = 10;
    button.fontSize = 20;
    button.thickness = 0;

    // Hover effects
    button.onPointerEnterObservable.add(() => {
      button.background = this.lightenColor(color, 20);
      button.scaleX = 1.05;
      button.scaleY = 1.05;
    });

    button.onPointerOutObservable.add(() => {
      button.background = color;
      button.scaleX = 1;
      button.scaleY = 1;
    });

    button.onPointerClickObservable.add(onClick);

    return button;
  }

  /**
   * Lighten a hex color.
   * @param color The hex color
   * @param percent The percentage to lighten
   */
  private lightenColor(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = ((num >> 8) & 0x00ff) + amt;
    const B = (num & 0x0000ff) + amt;
    return (
      '#' +
      (
        0x1000000 +
        (R < 255 ? (R < 1 ? 0 : R) : 255) * 0x10000 +
        (G < 255 ? (G < 1 ? 0 : G) : 255) * 0x100 +
        (B < 255 ? (B < 1 ? 0 : B) : 255)
      )
        .toString(16)
        .slice(1)
    );
  }

  /**
   * Handle the "Enter Game" button click.
   * This unlocks audio, requests pointer lock, and transitions to gameplay.
   */
  private async onEnterGameClicked(): Promise<void> {
    try {
      // Unlock audio engine (required due to browser autoplay policies)
      this.app.unlockAudio();

      // Request pointer lock for FPS controls
      try {
        await this.app.requestPointerLock();
        console.log('Pointer lock acquired');
      } catch (error) {
        console.warn('Failed to acquire pointer lock:', error);
        // Continue anyway - some browsers may not support pointer lock
        // or user may have denied the request
      }

      // Transition to gameplay
      await this.app.goToState(AppState.GAME_PLAY);
    } catch (error) {
      console.error('Failed to enter game:', error);
    }
  }

  update(_deltaTime: number): void {
    // Could add subtle animations to the menu here
  }

  dispose(): void {
    // Dispose GUI
    if (this.guiTexture) {
      this.guiTexture.dispose();
      this.guiTexture = null;
    }

    // Call parent dispose
    super.dispose();
  }
}
