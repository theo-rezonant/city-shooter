import { Color4, FreeCamera, Vector3 } from '@babylonjs/core';
import { AdvancedDynamicTexture, TextBlock, Button, Control, StackPanel } from '@babylonjs/gui';
import { BaseScene } from '../core/IScene';
import { AppState } from '../core/AppState';
import type { App } from '../core/App';

/**
 * Game over scene displayed when the player loses or the game ends.
 * Provides options to restart or return to the main menu.
 */
export class GameOverScene extends BaseScene {
  readonly state = AppState.GAME_OVER;

  /** The GUI texture for the game over screen */
  private guiTexture: AdvancedDynamicTexture | null = null;

  /** Score to display (would be passed from gameplay in a real implementation) */
  private score = 0;

  constructor(app: App) {
    super(app);
  }

  /**
   * Set the score to display.
   * This would be called by the GamePlayScene before transitioning.
   * @param score The player's score
   */
  setScore(score: number): void {
    this.score = score;
  }

  async init(): Promise<void> {
    // Set background color (dark red tint)
    this.scene.clearColor = new Color4(0.1, 0.02, 0.02, 1);

    // Create a basic camera
    const camera = new FreeCamera('gameOverCamera', new Vector3(0, 0, -10), this.scene);
    camera.setTarget(Vector3.Zero());

    // Create the game over UI
    this.createGameOverUI();
  }

  /**
   * Create the game over UI using Babylon GUI.
   */
  private createGameOverUI(): void {
    // Create fullscreen GUI
    this.guiTexture = AdvancedDynamicTexture.CreateFullscreenUI('GameOverUI', true, this.scene);

    // Create container panel
    const panel = new StackPanel('gameOverPanel');
    panel.width = '500px';
    panel.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this.guiTexture.addControl(panel);

    // Create "GAME OVER" text
    const gameOverText = new TextBlock('gameOverTitle', 'GAME OVER');
    gameOverText.color = '#ff4444';
    gameOverText.fontSize = 72;
    gameOverText.fontWeight = 'bold';
    gameOverText.height = '100px';
    gameOverText.paddingBottom = '20px';
    panel.addControl(gameOverText);

    // Create score text
    const scoreText = new TextBlock('scoreText', `Score: ${this.score}`);
    scoreText.color = 'white';
    scoreText.fontSize = 36;
    scoreText.height = '60px';
    scoreText.paddingBottom = '40px';
    panel.addControl(scoreText);

    // Create statistics placeholder
    const statsText = new TextBlock('statsText', 'Enemies Defeated: 0\nTime Survived: 00:00');
    statsText.color = '#888888';
    statsText.fontSize = 18;
    statsText.height = '60px';
    statsText.paddingBottom = '60px';
    panel.addControl(statsText);

    // Create "Play Again" button
    const playAgainButton = this.createButton('Play Again', '#4CAF50', () => {
      this.onPlayAgainClicked();
    });
    playAgainButton.paddingBottom = '20px';
    panel.addControl(playAgainButton);

    // Create "Main Menu" button
    const mainMenuButton = this.createButton('Main Menu', '#2196F3', () => {
      this.onMainMenuClicked();
    });
    panel.addControl(mainMenuButton);
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
   * Handle "Play Again" button click.
   */
  private async onPlayAgainClicked(): Promise<void> {
    try {
      // Unlock audio (in case it was locked)
      this.app.unlockAudio();

      // Request pointer lock
      try {
        await this.app.requestPointerLock();
      } catch (error) {
        console.warn('Failed to acquire pointer lock:', error);
      }

      // Transition directly to gameplay
      await this.app.goToState(AppState.GAME_PLAY);
    } catch (error) {
      console.error('Failed to restart game:', error);
    }
  }

  /**
   * Handle "Main Menu" button click.
   */
  private async onMainMenuClicked(): Promise<void> {
    try {
      await this.app.goToState(AppState.MAIN_MENU);
    } catch (error) {
      console.error('Failed to return to main menu:', error);
    }
  }

  update(_deltaTime: number): void {
    // Could add animations or effects here
  }

  dispose(): void {
    // Dispose GUI
    if (this.guiTexture) {
      this.guiTexture.dispose();
      this.guiTexture = null;
    }

    // Reset score
    this.score = 0;

    // Call parent dispose
    super.dispose();
  }
}
