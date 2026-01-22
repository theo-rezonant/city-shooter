import { GameEngine } from "@/core/GameEngine";
import {
  AdvancedDynamicTexture,
  Button,
  TextBlock,
  StackPanel,
  Control,
} from "@babylonjs/gui";

/**
 * Main application entry point
 */
class App {
  private gameEngine: GameEngine | null = null;
  private loadingScreen: HTMLElement | null = null;
  private loadingProgress: HTMLElement | null = null;
  private loadingText: HTMLElement | null = null;

  constructor() {
    this.loadingScreen = document.getElementById("loadingScreen");
    this.loadingProgress = document.getElementById("loadingProgress");
    this.loadingText = document.getElementById("loadingText");
  }

  /**
   * Initialize and start the application
   */
  async start(): Promise<void> {
    console.log("[App] Starting City Shooter...");

    try {
      // Get canvas element
      const canvas = document.getElementById(
        "renderCanvas"
      ) as HTMLCanvasElement;
      if (!canvas) {
        throw new Error("Canvas element not found");
      }

      // Create game engine
      this.gameEngine = new GameEngine(canvas);

      // Initialize with loading progress
      await this.gameEngine.initialize((progress, message) => {
        this.updateLoadingProgress(progress, message);
      });

      // Hide loading screen and show start menu
      this.hideLoadingScreen();
      this.showStartMenu();

      // Handle window resize
      window.addEventListener("resize", () => {
        this.gameEngine?.resize();
      });

      // Handle keyboard shortcuts
      window.addEventListener("keydown", (e) => {
        if (e.code === "F1") {
          e.preventDefault();
          this.gameEngine?.toggleMetricsDisplay();
        }
        if (e.code === "Escape") {
          // Exit pointer lock
          document.exitPointerLock();
        }
      });

      console.log("[App] City Shooter ready!");
    } catch (error) {
      console.error("[App] Failed to start:", error);
      this.showError(error as Error);
    }
  }

  /**
   * Update loading progress display
   */
  private updateLoadingProgress(progress: number, message: string): void {
    if (this.loadingProgress) {
      this.loadingProgress.style.width = `${progress * 100}%`;
    }
    if (this.loadingText) {
      this.loadingText.textContent = message;
    }
  }

  /**
   * Hide the loading screen
   */
  private hideLoadingScreen(): void {
    if (this.loadingScreen) {
      this.loadingScreen.style.opacity = "0";
      this.loadingScreen.style.transition = "opacity 0.5s ease";
      setTimeout(() => {
        if (this.loadingScreen) {
          this.loadingScreen.style.display = "none";
        }
      }, 500);
    }
  }

  /**
   * Show the start menu using Babylon GUI
   */
  private showStartMenu(): void {
    if (!this.gameEngine) return;

    const scene = this.gameEngine.getScene();
    const advancedTexture = AdvancedDynamicTexture.CreateFullscreenUI(
      "UI",
      true,
      scene
    );

    // Create main panel
    const panel = new StackPanel();
    panel.width = "300px";
    panel.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_CENTER;
    panel.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    advancedTexture.addControl(panel);

    // Title
    const title = new TextBlock();
    title.text = "CITY SHOOTER";
    title.color = "white";
    title.fontSize = 36;
    title.fontWeight = "bold";
    title.height = "80px";
    panel.addControl(title);

    // Subtitle with controls
    const controls = new TextBlock();
    controls.text =
      "WASD - Move | Space - Jump | Shift - Sprint\nF1 - Toggle Metrics | ESC - Release Mouse";
    controls.color = "rgba(255, 255, 255, 0.7)";
    controls.fontSize = 14;
    controls.height = "60px";
    controls.textWrapping = true;
    panel.addControl(controls);

    // Start button
    const startButton = Button.CreateSimpleButton("startButton", "ENTER GAME");
    startButton.width = "200px";
    startButton.height = "50px";
    startButton.color = "white";
    startButton.background = "#e94560";
    startButton.cornerRadius = 10;
    startButton.thickness = 0;
    startButton.fontWeight = "bold";
    startButton.onPointerUpObservable.add(() => {
      // Request pointer lock on user interaction
      this.gameEngine?.requestPointerLock();

      // Unlock audio context
      if (scene.audioEnabled === false) {
        scene.audioEnabled = true;
      }

      // Show metrics panel
      this.gameEngine?.getPerformanceMetrics().showDisplay();

      // Start render loop
      this.gameEngine?.startRenderLoop();

      // Fade out menu
      panel.alpha = 0;
      setTimeout(() => {
        advancedTexture.dispose();
      }, 300);

      // Log performance metrics after start
      setTimeout(() => {
        this.gameEngine?.getPerformanceMetrics().logMetrics("Game started");
      }, 1000);
    });

    // Hover effect
    startButton.onPointerEnterObservable.add(() => {
      startButton.background = "#ff6b6b";
    });
    startButton.onPointerOutObservable.add(() => {
      startButton.background = "#e94560";
    });

    panel.addControl(startButton);
  }

  /**
   * Show error message
   */
  private showError(error: Error): void {
    if (this.loadingText) {
      this.loadingText.textContent = `Error: ${error.message}`;
      this.loadingText.style.color = "#ff6b6b";
    }
  }
}

// Start the application when DOM is ready
window.addEventListener("DOMContentLoaded", () => {
  const app = new App();
  app.start();
});
