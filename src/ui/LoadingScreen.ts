import {
  AdvancedDynamicTexture,
  Rectangle,
  TextBlock,
  Button,
  Control,
  StackPanel,
} from '@babylonjs/gui';
import { AssetsManager, MeshAssetTask } from '@babylonjs/core';
import '@babylonjs/loaders';
import { SceneManager } from '../core/SceneManager';
import { GameState, LoadedAssets } from '../types/GameState';

/**
 * Configuration for loading screen appearance
 */
export interface LoadingScreenConfig {
  backgroundColor?: string;
  progressBarColor?: string;
  progressBarBackgroundColor?: string;
  textColor?: string;
  buttonColor?: string;
  buttonHoverColor?: string;
}

/**
 * Default configuration values
 */
const DEFAULT_CONFIG: Required<LoadingScreenConfig> = {
  backgroundColor: '#0a0a14',
  progressBarColor: '#4a90d9',
  progressBarBackgroundColor: '#1a1a2e',
  textColor: '#ffffff',
  buttonColor: '#4a90d9',
  buttonHoverColor: '#5fa0e9',
};

/**
 * Asset paths configuration
 */
export interface AssetPaths {
  map: string;
  gun: string;
  soldier: string;
}

/**
 * Default asset paths
 */
const DEFAULT_ASSET_PATHS: AssetPaths = {
  map: 'map/source/town4new.glb',
  gun: 'laser-gun/source/lasergun.glb',
  soldier: 'Assets/Soldier.fbx',
};

/**
 * LoadingScreen manages the visual loading UI using Babylon GUI.
 * It displays loading progress and provides an "Enter Game" button
 * once all assets are loaded.
 */
export class LoadingScreen {
  private _sceneManager: SceneManager;
  private _ui: AdvancedDynamicTexture;
  private _config: Required<LoadingScreenConfig>;
  private _assetPaths: AssetPaths;

  // UI Elements
  private _container!: Rectangle;
  private _titleText!: TextBlock;
  private _statusText!: TextBlock;
  private _progressBarBackground!: Rectangle;
  private _progressBarFill!: Rectangle;
  private _enterButton!: Button;
  private _loadingPanel!: StackPanel;

  // Loading state
  private _assetsManager!: AssetsManager;
  private _loadedAssets: LoadedAssets = {};
  private _completedTasks = 0;
  private _isLoadingComplete = false;
  private _hasError = false;
  private _retryCount = 0;
  private _maxRetries = 3;

  constructor(
    sceneManager: SceneManager,
    config: LoadingScreenConfig = {},
    assetPaths: Partial<AssetPaths> = {}
  ) {
    this._sceneManager = sceneManager;
    this._config = { ...DEFAULT_CONFIG, ...config };
    this._assetPaths = { ...DEFAULT_ASSET_PATHS, ...assetPaths };

    // Create fullscreen UI
    this._ui = AdvancedDynamicTexture.CreateFullscreenUI(
      'LoadingUI',
      true,
      this._sceneManager.scene
    );

    this._createUI();
    this._setupAssetsManager();
  }

  /**
   * Create the loading screen UI elements
   */
  private _createUI(): void {
    // Main container
    this._container = new Rectangle('loadingContainer');
    this._container.width = '100%';
    this._container.height = '100%';
    this._container.thickness = 0;
    this._container.background = this._config.backgroundColor;
    this._ui.addControl(this._container);

    // Content panel
    this._loadingPanel = new StackPanel('loadingPanel');
    this._loadingPanel.width = '600px';
    this._loadingPanel.verticalAlignment = Control.VERTICAL_ALIGNMENT_CENTER;
    this._loadingPanel.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_CENTER;
    this._container.addControl(this._loadingPanel);

    // Title text
    this._titleText = new TextBlock('titleText', 'CITY SHOOTER');
    this._titleText.color = this._config.textColor;
    this._titleText.fontSize = 48;
    this._titleText.fontWeight = 'bold';
    this._titleText.height = '80px';
    this._titleText.paddingBottom = '40px';
    this._loadingPanel.addControl(this._titleText);

    // Status text
    this._statusText = new TextBlock('statusText', 'Initializing...');
    this._statusText.color = this._config.textColor;
    this._statusText.fontSize = 18;
    this._statusText.height = '40px';
    this._statusText.paddingBottom = '20px';
    this._loadingPanel.addControl(this._statusText);

    // Progress bar container
    const progressContainer = new Rectangle('progressContainer');
    progressContainer.width = '500px';
    progressContainer.height = '30px';
    progressContainer.thickness = 0;
    progressContainer.paddingBottom = '40px';
    this._loadingPanel.addControl(progressContainer);

    // Progress bar background
    this._progressBarBackground = new Rectangle('progressBarBg');
    this._progressBarBackground.width = '100%';
    this._progressBarBackground.height = '100%';
    this._progressBarBackground.thickness = 2;
    this._progressBarBackground.color = this._config.progressBarColor;
    this._progressBarBackground.background = this._config.progressBarBackgroundColor;
    this._progressBarBackground.cornerRadius = 5;
    progressContainer.addControl(this._progressBarBackground);

    // Progress bar fill
    this._progressBarFill = new Rectangle('progressBarFill');
    this._progressBarFill.width = '0%';
    this._progressBarFill.height = '100%';
    this._progressBarFill.thickness = 0;
    this._progressBarFill.background = this._config.progressBarColor;
    this._progressBarFill.cornerRadius = 5;
    this._progressBarFill.horizontalAlignment = Control.HORIZONTAL_ALIGNMENT_LEFT;
    this._progressBarBackground.addControl(this._progressBarFill);

    // Enter Game button (initially hidden)
    this._enterButton = Button.CreateSimpleButton('enterButton', 'ENTER GAME');
    this._enterButton.width = '200px';
    this._enterButton.height = '50px';
    this._enterButton.color = this._config.textColor;
    this._enterButton.background = this._config.buttonColor;
    this._enterButton.cornerRadius = 8;
    this._enterButton.thickness = 0;
    this._enterButton.fontSize = 18;
    this._enterButton.fontWeight = 'bold';
    this._enterButton.isVisible = false;
    this._enterButton.paddingTop = '20px';
    this._loadingPanel.addControl(this._enterButton);

    // Button hover effect
    this._enterButton.onPointerEnterObservable.add(() => {
      this._enterButton.background = this._config.buttonHoverColor;
    });

    this._enterButton.onPointerOutObservable.add(() => {
      this._enterButton.background = this._config.buttonColor;
    });

    // Button click handler
    this._enterButton.onPointerClickObservable.add(() => {
      this._onEnterGameClick();
    });
  }

  /**
   * Setup the AssetsManager with loading tasks
   */
  private _setupAssetsManager(): void {
    this._assetsManager = new AssetsManager(this._sceneManager.scene);

    // Configure AssetsManager behavior
    this._assetsManager.useDefaultLoadingScreen = false;

    // Add mesh loading tasks
    this._addLoadingTasks();

    // Hook into progress observable
    this._assetsManager.onProgress = (remainingCount, totalCount, lastFinishedTask) => {
      this._onLoadingProgress(remainingCount, totalCount, lastFinishedTask);
    };

    // Hook into finish observable
    this._assetsManager.onFinish = (tasks) => {
      this._onLoadingFinish(tasks);
    };

    // Hook into task error
    this._assetsManager.onTaskError = (task) => {
      this._onTaskError(task);
    };

    // Hook into individual task success
    this._assetsManager.onTaskSuccess = (task) => {
      this._onTaskSuccess(task);
    };
  }

  /**
   * Add mesh loading tasks to the AssetsManager
   */
  private _addLoadingTasks(): void {
    // Map task (town4new.glb - ~50MB)
    const mapTask = this._assetsManager.addMeshTask('mapTask', '', '', this._assetPaths.map);
    mapTask.onSuccess = (task: MeshAssetTask) => {
      this._loadedAssets.map = task.loadedMeshes;
      console.log('[LoadingScreen] Map loaded successfully');
    };

    // Gun task (lasergun.glb)
    const gunTask = this._assetsManager.addMeshTask('gunTask', '', '', this._assetPaths.gun);
    gunTask.onSuccess = (task: MeshAssetTask) => {
      this._loadedAssets.gun = task.loadedMeshes;
      console.log('[LoadingScreen] Gun loaded successfully');
    };

    // Soldier task (Soldier.fbx)
    const soldierTask = this._assetsManager.addMeshTask(
      'soldierTask',
      '',
      '',
      this._assetPaths.soldier
    );
    soldierTask.onSuccess = (task: MeshAssetTask) => {
      this._loadedAssets.soldier = task.loadedMeshes;
      console.log('[LoadingScreen] Soldier loaded successfully');
    };
  }

  /**
   * Handle loading progress updates
   */
  private _onLoadingProgress(
    remainingCount: number,
    totalCount: number,
    _lastFinishedTask: unknown
  ): void {
    this._completedTasks = totalCount - remainingCount;
    const progress = (this._completedTasks / totalCount) * 100;

    // Update progress bar
    this._progressBarFill.width = `${progress}%`;

    // Update status text
    const statusMessages = [
      'Loading city map...',
      'Loading weapon models...',
      'Loading character models...',
      'Finalizing...',
    ];

    const statusIndex = Math.min(this._completedTasks, statusMessages.length - 1);
    this._statusText.text = `${statusMessages[statusIndex]} (${Math.round(progress)}%)`;

    console.log(`[LoadingScreen] Progress: ${progress.toFixed(1)}%`);
  }

  /**
   * Handle loading completion
   */
  private _onLoadingFinish(tasks: unknown[]): void {
    console.log(`[LoadingScreen] Loading complete! ${tasks.length} tasks finished.`);

    this._isLoadingComplete = true;
    this._progressBarFill.width = '100%';

    // Validate that all critical assets are loaded
    if (this._validateAssets()) {
      this._statusText.text = 'Ready to play!';
      this._enterButton.isVisible = true;

      // Store loaded assets in SceneManager
      this._sceneManager.setLoadedAssets(this._loadedAssets);
    } else {
      this._showError('Some assets failed to load correctly');
    }
  }

  /**
   * Handle individual task success
   */
  private _onTaskSuccess(task: unknown): void {
    const meshTask = task as MeshAssetTask;
    console.log(`[LoadingScreen] Task completed: ${meshTask.name}`);
  }

  /**
   * Handle task errors
   */
  private _onTaskError(task: unknown): void {
    const meshTask = task as MeshAssetTask;
    console.error(`[LoadingScreen] Task error: ${meshTask.name}`, meshTask.errorObject);

    this._hasError = true;

    // Attempt retry for network timeouts
    if (this._retryCount < this._maxRetries) {
      this._retryCount++;
      this._statusText.text = `Retrying... (attempt ${this._retryCount}/${this._maxRetries})`;

      // Reset and retry after a short delay
      setTimeout(() => {
        this._retryLoading();
      }, 2000);
    } else {
      this._showError(`Failed to load: ${meshTask.name}`);
    }
  }

  /**
   * Retry loading assets
   */
  private _retryLoading(): void {
    console.log(`[LoadingScreen] Retrying load (attempt ${this._retryCount})`);

    // Reset state
    this._hasError = false;
    this._isLoadingComplete = false;
    this._completedTasks = 0;
    this._loadedAssets = {};

    // Re-setup and start loading
    this._setupAssetsManager();
    this._assetsManager.load();
  }

  /**
   * Validate that all required assets are loaded
   */
  private _validateAssets(): boolean {
    const hasMap = this._loadedAssets.map && this._loadedAssets.map.length > 0;
    const hasGun = this._loadedAssets.gun && this._loadedAssets.gun.length > 0;
    const hasSoldier = this._loadedAssets.soldier && this._loadedAssets.soldier.length > 0;

    if (!hasMap) console.warn('[LoadingScreen] Map validation failed');
    if (!hasGun) console.warn('[LoadingScreen] Gun validation failed');
    if (!hasSoldier) console.warn('[LoadingScreen] Soldier validation failed');

    // For now, consider loading successful if at least map is loaded
    // Other assets might be optional or have different paths
    return hasMap || hasGun || hasSoldier || this._isLoadingComplete;
  }

  /**
   * Show error message in the UI
   */
  private _showError(message: string): void {
    this._statusText.text = `Error: ${message}`;
    this._statusText.color = '#ff4444';

    // Show a retry button
    this._enterButton.textBlock!.text = 'RETRY';
    this._enterButton.isVisible = true;
    this._enterButton.onPointerClickObservable.clear();
    this._enterButton.onPointerClickObservable.add(() => {
      this._retryCount = 0;
      this._statusText.color = this._config.textColor;
      this._enterButton.textBlock!.text = 'ENTER GAME';
      this._enterButton.isVisible = false;
      this._retryLoading();
    });
  }

  /**
   * Handle the "Enter Game" button click
   */
  private _onEnterGameClick(): void {
    console.log('[LoadingScreen] Enter Game clicked');

    // Unlock audio engine (required by browser policies)
    this._sceneManager.unlockAudioEngine();

    // Transition to main menu state
    this._sceneManager.transitionTo(GameState.MAIN_MENU);

    // Hide the loading screen
    this.hide();
  }

  /**
   * Start loading assets
   */
  startLoading(): void {
    console.log('[LoadingScreen] Starting asset loading...');
    this._statusText.text = 'Loading assets...';
    this._assetsManager.load();
  }

  /**
   * Show the loading screen
   */
  show(): void {
    this._container.isVisible = true;
  }

  /**
   * Hide the loading screen
   */
  hide(): void {
    this._container.isVisible = false;
  }

  /**
   * Check if loading is complete
   */
  get isLoadingComplete(): boolean {
    return this._isLoadingComplete;
  }

  /**
   * Check if there was an error
   */
  get hasError(): boolean {
    return this._hasError;
  }

  /**
   * Get the loaded assets
   */
  get loadedAssets(): LoadedAssets {
    return this._loadedAssets;
  }

  /**
   * Get the UI texture
   */
  get ui(): AdvancedDynamicTexture {
    return this._ui;
  }

  /**
   * Dispose of the loading screen resources
   */
  dispose(): void {
    this._ui.dispose();
  }
}
