import {
  Engine,
  Scene,
  Sound,
  Vector3,
  UniversalCamera,
  Observer,
  Nullable,
} from '@babylonjs/core';
import { SpatialAudioOptions, AudioManagerConfig } from '../core/types';

/**
 * AudioManager handles spatial audio and global sound state for the game.
 *
 * It manages:
 * - Browser AudioContext unlock on user interaction
 * - Spatial audio listener positioning attached to the camera
 * - Sound creation and lifecycle management
 * - Global volume control
 *
 * @example
 * ```typescript
 * const audioManager = new AudioManager({ engine, scene, camera });
 *
 * // Unlock audio (must be called from user interaction)
 * await audioManager.unlock();
 *
 * // Create a spatial sound
 * const gunshot = audioManager.createSpatialSound('gunshot', 'sounds/gunshot.mp3', {
 *   position: new Vector3(0, 1, 5),
 *   maxDistance: 50,
 * });
 *
 * // Play the sound
 * gunshot.play();
 * ```
 */
export class AudioManager {
  private _engine: Engine;
  private _scene: Scene;
  private _camera: UniversalCamera | null = null;
  private _masterVolume: number = 1.0;
  private _sounds: Map<string, Sound> = new Map();
  private _isUnlocked: boolean = false;
  private _renderObserver: Nullable<Observer<Scene>> = null;

  /**
   * Creates a new AudioManager instance.
   *
   * @param config - Configuration options for the AudioManager
   */
  constructor(config: AudioManagerConfig) {
    this._engine = config.engine;
    this._scene = config.scene;
    this._masterVolume = config.masterVolume ?? 1.0;

    if (config.camera) {
      this.attachToCamera(config.camera);
    }
  }

  /**
   * Gets whether the AudioContext has been unlocked.
   */
  public get isUnlocked(): boolean {
    return this._isUnlocked;
  }

  /**
   * Gets the current master volume.
   */
  public get masterVolume(): number {
    return this._masterVolume;
  }

  /**
   * Sets the master volume for all sounds.
   *
   * @param volume - Volume level (0-1)
   */
  public set masterVolume(volume: number) {
    this._masterVolume = Math.max(0, Math.min(1, volume));
    this._updateAllSoundVolumes();
  }

  /**
   * Gets the attached camera.
   */
  public get camera(): UniversalCamera | null {
    return this._camera;
  }

  /**
   * Gets the Babylon.js AudioEngine instance.
   */
  public get audioEngine() {
    return Engine.audioEngine;
  }

  /**
   * Unlocks the AudioContext for playback.
   * Must be called from within a user interaction event handler (click, touch, etc.)
   * to satisfy browser autoplay policies.
   *
   * @returns Promise that resolves when the AudioContext is unlocked
   */
  public async unlock(): Promise<void> {
    const audioEngine = Engine.audioEngine;

    if (!audioEngine) {
      console.warn('AudioManager: No audio engine available');
      return;
    }

    try {
      // Unlock the Babylon.js audio engine
      await audioEngine.unlock();

      // Also ensure the underlying AudioContext is resumed
      const audioContext = audioEngine.audioContext;
      if (audioContext && audioContext.state === 'suspended') {
        await audioContext.resume();
      }

      this._isUnlocked = true;
      console.log('AudioManager: AudioContext unlocked successfully');
    } catch (error) {
      console.error('AudioManager: Failed to unlock AudioContext', error);
      throw error;
    }
  }

  /**
   * Attaches the audio listener to a camera.
   * The listener will follow the camera's position and rotation each frame.
   *
   * @param camera - The UniversalCamera to attach the listener to
   */
  public attachToCamera(camera: UniversalCamera): void {
    // Remove previous observer if it exists
    if (this._renderObserver && this._scene) {
      this._scene.onBeforeRenderObservable.remove(this._renderObserver);
      this._renderObserver = null;
    }

    this._camera = camera;

    // Update the listener position and orientation every frame
    this._renderObserver = this._scene.onBeforeRenderObservable.add(() => {
      this._updateListenerPosition();
    });
  }

  /**
   * Detaches the audio listener from the current camera.
   */
  public detachFromCamera(): void {
    if (this._renderObserver && this._scene) {
      this._scene.onBeforeRenderObservable.remove(this._renderObserver);
      this._renderObserver = null;
    }
    this._camera = null;
  }

  /**
   * Creates a spatial (3D positioned) sound.
   *
   * @param name - Unique identifier for the sound
   * @param url - URL to the audio file
   * @param options - Spatial audio configuration options
   * @returns The created Sound instance
   */
  public createSpatialSound(
    name: string,
    url: string,
    options: SpatialAudioOptions = {}
  ): Sound {
    const sound = new Sound(
      name,
      url,
      this._scene,
      null, // Ready callback
      {
        spatialSound: true,
        distanceModel: 'exponential',
        maxDistance: options.maxDistance ?? 100,
        refDistance: options.refDistance ?? 1,
        rolloffFactor: options.rolloffFactor ?? 1,
        loop: options.loop ?? false,
        autoplay: options.autoplay ?? false,
        volume: (options.volume ?? 1.0) * this._masterVolume,
      }
    );

    // Set panning model if specified
    if (options.panningModel) {
      sound.switchPanningModelToHRTF();
    }

    // Set initial position if provided
    if (options.position) {
      sound.setPosition(options.position);
    }

    // Store the sound for management
    this._sounds.set(name, sound);

    return sound;
  }

  /**
   * Creates a global (non-spatial) sound.
   *
   * @param name - Unique identifier for the sound
   * @param url - URL to the audio file
   * @param options - Audio configuration options
   * @returns The created Sound instance
   */
  public createGlobalSound(
    name: string,
    url: string,
    options: { volume?: number; loop?: boolean; autoplay?: boolean } = {}
  ): Sound {
    const sound = new Sound(
      name,
      url,
      this._scene,
      null, // Ready callback
      {
        spatialSound: false,
        loop: options.loop ?? false,
        autoplay: options.autoplay ?? false,
        volume: (options.volume ?? 1.0) * this._masterVolume,
      }
    );

    // Store the sound for management
    this._sounds.set(name, sound);

    return sound;
  }

  /**
   * Gets a sound by its name.
   *
   * @param name - The name of the sound to retrieve
   * @returns The Sound instance or undefined if not found
   */
  public getSound(name: string): Sound | undefined {
    return this._sounds.get(name);
  }

  /**
   * Plays a sound by its name.
   *
   * @param name - The name of the sound to play
   */
  public playSound(name: string): void {
    const sound = this._sounds.get(name);
    if (sound) {
      sound.play();
    } else {
      console.warn(`AudioManager: Sound "${name}" not found`);
    }
  }

  /**
   * Stops a sound by its name.
   *
   * @param name - The name of the sound to stop
   */
  public stopSound(name: string): void {
    const sound = this._sounds.get(name);
    if (sound) {
      sound.stop();
    }
  }

  /**
   * Stops all currently playing sounds.
   */
  public stopAllSounds(): void {
    this._sounds.forEach((sound) => {
      sound.stop();
    });
  }

  /**
   * Pauses all currently playing sounds.
   */
  public pauseAllSounds(): void {
    this._sounds.forEach((sound) => {
      sound.pause();
    });
  }

  /**
   * Sets the volume of a specific sound.
   *
   * @param name - The name of the sound
   * @param volume - Volume level (0-1)
   */
  public setSoundVolume(name: string, volume: number): void {
    const sound = this._sounds.get(name);
    if (sound) {
      sound.setVolume(Math.max(0, Math.min(1, volume)) * this._masterVolume);
    }
  }

  /**
   * Sets the position of a spatial sound.
   *
   * @param name - The name of the sound
   * @param position - The new 3D position
   */
  public setSoundPosition(name: string, position: Vector3): void {
    const sound = this._sounds.get(name);
    if (sound) {
      sound.setPosition(position);
    }
  }

  /**
   * Removes and disposes a sound.
   *
   * @param name - The name of the sound to remove
   */
  public removeSound(name: string): void {
    const sound = this._sounds.get(name);
    if (sound) {
      sound.dispose();
      this._sounds.delete(name);
    }
  }

  /**
   * Mutes all audio.
   */
  public mute(): void {
    const audioEngine = Engine.audioEngine;
    if (audioEngine) {
      audioEngine.setGlobalVolume(0);
    }
  }

  /**
   * Unmutes all audio to the current master volume.
   */
  public unmute(): void {
    const audioEngine = Engine.audioEngine;
    if (audioEngine) {
      audioEngine.setGlobalVolume(this._masterVolume);
    }
  }

  /**
   * Disposes all resources and cleans up the AudioManager.
   */
  public dispose(): void {
    // Remove render observer
    this.detachFromCamera();

    // Dispose all sounds
    this._sounds.forEach((sound) => {
      sound.dispose();
    });
    this._sounds.clear();

    this._isUnlocked = false;
  }

  /**
   * Updates the audio listener position to match the camera.
   */
  private _updateListenerPosition(): void {
    if (!this._camera) return;

    const audioEngine = Engine.audioEngine;
    if (!audioEngine || !audioEngine.audioContext) return;

    // Get camera position and forward direction
    const position = this._camera.position;
    const forward = this._camera.getDirection(Vector3.Forward());
    const up = this._camera.upVector;

    // Update the listener position on the audio context
    const listener = audioEngine.audioContext.listener;

    if (listener) {
      // Use the modern API if available
      if (listener.positionX) {
        listener.positionX.setValueAtTime(
          position.x,
          audioEngine.audioContext.currentTime
        );
        listener.positionY.setValueAtTime(
          position.y,
          audioEngine.audioContext.currentTime
        );
        listener.positionZ.setValueAtTime(
          position.z,
          audioEngine.audioContext.currentTime
        );
        listener.forwardX.setValueAtTime(forward.x, audioEngine.audioContext.currentTime);
        listener.forwardY.setValueAtTime(forward.y, audioEngine.audioContext.currentTime);
        listener.forwardZ.setValueAtTime(forward.z, audioEngine.audioContext.currentTime);
        listener.upX.setValueAtTime(up.x, audioEngine.audioContext.currentTime);
        listener.upY.setValueAtTime(up.y, audioEngine.audioContext.currentTime);
        listener.upZ.setValueAtTime(up.z, audioEngine.audioContext.currentTime);
      } else {
        // Fall back to deprecated setPosition/setOrientation
        listener.setPosition(position.x, position.y, position.z);
        listener.setOrientation(forward.x, forward.y, forward.z, up.x, up.y, up.z);
      }
    }
  }

  /**
   * Updates all sound volumes based on the master volume.
   */
  private _updateAllSoundVolumes(): void {
    const audioEngine = Engine.audioEngine;
    if (audioEngine) {
      audioEngine.setGlobalVolume(this._masterVolume);
    }
  }
}
