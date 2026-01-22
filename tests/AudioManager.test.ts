import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { AudioManager } from '../src/managers/AudioManager';
import { Engine, Scene, Sound, Vector3, UniversalCamera } from '@babylonjs/core';

// Mock Babylon.js modules
vi.mock('@babylonjs/core', () => {
  const mockAudioContext = {
    state: 'suspended',
    resume: vi.fn().mockResolvedValue(undefined),
    currentTime: 0,
    listener: {
      positionX: { setValueAtTime: vi.fn() },
      positionY: { setValueAtTime: vi.fn() },
      positionZ: { setValueAtTime: vi.fn() },
      forwardX: { setValueAtTime: vi.fn() },
      forwardY: { setValueAtTime: vi.fn() },
      forwardZ: { setValueAtTime: vi.fn() },
      upX: { setValueAtTime: vi.fn() },
      upY: { setValueAtTime: vi.fn() },
      upZ: { setValueAtTime: vi.fn() },
    },
  };

  const mockAudioEngine = {
    unlock: vi.fn().mockResolvedValue(undefined),
    audioContext: mockAudioContext,
    setGlobalVolume: vi.fn(),
  };

  return {
    Engine: {
      audioEngine: mockAudioEngine,
    },
    Scene: vi.fn().mockImplementation(() => ({
      onBeforeRenderObservable: {
        add: vi.fn().mockReturnValue({ remove: vi.fn() }),
        remove: vi.fn(),
      },
    })),
    Sound: vi.fn().mockImplementation((name) => ({
      name,
      play: vi.fn(),
      stop: vi.fn(),
      pause: vi.fn(),
      dispose: vi.fn(),
      setPosition: vi.fn(),
      setVolume: vi.fn(),
      switchPanningModelToHRTF: vi.fn(),
    })),
    Vector3: {
      Forward: vi.fn().mockReturnValue({ x: 0, y: 0, z: 1 }),
    },
    UniversalCamera: vi.fn().mockImplementation(() => ({
      position: { x: 0, y: 0, z: 0 },
      upVector: { x: 0, y: 1, z: 0 },
      getDirection: vi.fn().mockReturnValue({ x: 0, y: 0, z: 1 }),
      attachControl: vi.fn(),
      detachControl: vi.fn(),
    })),
  };
});

describe('AudioManager', () => {
  let audioManager: AudioManager;
  let mockEngine: typeof Engine;
  let mockScene: Scene;
  let mockCamera: UniversalCamera;

  beforeEach(() => {
    mockEngine = Engine;
    mockScene = new Scene(null as any);
    mockCamera = new UniversalCamera('camera', null as any, null as any);

    audioManager = new AudioManager({
      engine: mockEngine as any,
      scene: mockScene,
      masterVolume: 1.0,
    });
  });

  afterEach(() => {
    audioManager.dispose();
    vi.clearAllMocks();
  });

  describe('constructor', () => {
    it('should create an AudioManager instance', () => {
      expect(audioManager).toBeDefined();
    });

    it('should set default master volume to 1.0', () => {
      expect(audioManager.masterVolume).toBe(1.0);
    });

    it('should allow setting custom master volume', () => {
      const customManager = new AudioManager({
        engine: mockEngine as any,
        scene: mockScene,
        masterVolume: 0.5,
      });
      expect(customManager.masterVolume).toBe(0.5);
      customManager.dispose();
    });

    it('should not be unlocked initially', () => {
      expect(audioManager.isUnlocked).toBe(false);
    });
  });

  describe('unlock', () => {
    it('should unlock the audio context', async () => {
      await audioManager.unlock();

      expect(Engine.audioEngine!.unlock).toHaveBeenCalled();
      expect(audioManager.isUnlocked).toBe(true);
    });

    it('should resume suspended audio context', async () => {
      await audioManager.unlock();

      expect(Engine.audioEngine!.audioContext!.resume).toHaveBeenCalled();
    });
  });

  describe('attachToCamera', () => {
    it('should attach listener to camera', () => {
      audioManager.attachToCamera(mockCamera);

      expect(audioManager.camera).toBe(mockCamera);
      expect(mockScene.onBeforeRenderObservable.add).toHaveBeenCalled();
    });

    it('should detach from previous camera when attaching new one', () => {
      const newCamera = new UniversalCamera('newCamera', null as any, null as any);

      audioManager.attachToCamera(mockCamera);
      audioManager.attachToCamera(newCamera);

      expect(audioManager.camera).toBe(newCamera);
    });
  });

  describe('detachFromCamera', () => {
    it('should detach listener from camera', () => {
      audioManager.attachToCamera(mockCamera);
      audioManager.detachFromCamera();

      expect(audioManager.camera).toBeNull();
    });
  });

  describe('createSpatialSound', () => {
    it('should create a spatial sound', () => {
      const sound = audioManager.createSpatialSound('gunshot', 'sounds/gunshot.mp3');

      expect(Sound).toHaveBeenCalledWith(
        'gunshot',
        'sounds/gunshot.mp3',
        mockScene,
        null,
        expect.objectContaining({
          spatialSound: true,
        })
      );
      expect(sound).toBeDefined();
    });

    it('should apply spatial audio options', () => {
      const options = {
        maxDistance: 50,
        refDistance: 2,
        rolloffFactor: 2,
        volume: 0.8,
        loop: true,
      };

      audioManager.createSpatialSound('test', 'test.mp3', options);

      expect(Sound).toHaveBeenCalledWith(
        'test',
        'test.mp3',
        mockScene,
        null,
        expect.objectContaining({
          maxDistance: 50,
          refDistance: 2,
          rolloffFactor: 2,
          volume: 0.8,
          loop: true,
        })
      );
    });

    it('should set position if provided', () => {
      const position = { x: 5, y: 2, z: 10 } as Vector3;
      const sound = audioManager.createSpatialSound('test', 'test.mp3', { position });

      expect(sound.setPosition).toHaveBeenCalledWith(position);
    });

    it('should enable HRTF panning model if specified', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3', {
        panningModel: 'HRTF',
      });

      expect(sound.switchPanningModelToHRTF).toHaveBeenCalled();
    });
  });

  describe('createGlobalSound', () => {
    it('should create a non-spatial sound', () => {
      const sound = audioManager.createGlobalSound('bgMusic', 'music/bg.mp3');

      expect(Sound).toHaveBeenCalledWith(
        'bgMusic',
        'music/bg.mp3',
        mockScene,
        null,
        expect.objectContaining({
          spatialSound: false,
        })
      );
      expect(sound).toBeDefined();
    });
  });

  describe('sound management', () => {
    it('should get a sound by name', () => {
      audioManager.createSpatialSound('test', 'test.mp3');
      const sound = audioManager.getSound('test');

      expect(sound).toBeDefined();
    });

    it('should return undefined for non-existent sound', () => {
      const sound = audioManager.getSound('nonexistent');

      expect(sound).toBeUndefined();
    });

    it('should play a sound by name', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3');
      audioManager.playSound('test');

      expect(sound.play).toHaveBeenCalled();
    });

    it('should stop a sound by name', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3');
      audioManager.stopSound('test');

      expect(sound.stop).toHaveBeenCalled();
    });

    it('should stop all sounds', () => {
      const sound1 = audioManager.createSpatialSound('test1', 'test1.mp3');
      const sound2 = audioManager.createSpatialSound('test2', 'test2.mp3');

      audioManager.stopAllSounds();

      expect(sound1.stop).toHaveBeenCalled();
      expect(sound2.stop).toHaveBeenCalled();
    });

    it('should pause all sounds', () => {
      const sound1 = audioManager.createSpatialSound('test1', 'test1.mp3');
      const sound2 = audioManager.createSpatialSound('test2', 'test2.mp3');

      audioManager.pauseAllSounds();

      expect(sound1.pause).toHaveBeenCalled();
      expect(sound2.pause).toHaveBeenCalled();
    });

    it('should remove and dispose a sound', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3');
      audioManager.removeSound('test');

      expect(sound.dispose).toHaveBeenCalled();
      expect(audioManager.getSound('test')).toBeUndefined();
    });

    it('should set sound position', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3');
      const newPosition = { x: 1, y: 2, z: 3 } as Vector3;

      audioManager.setSoundPosition('test', newPosition);

      expect(sound.setPosition).toHaveBeenCalledWith(newPosition);
    });

    it('should set sound volume', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3');

      audioManager.setSoundVolume('test', 0.5);

      expect(sound.setVolume).toHaveBeenCalledWith(0.5);
    });
  });

  describe('master volume', () => {
    it('should set master volume', () => {
      audioManager.masterVolume = 0.5;

      expect(audioManager.masterVolume).toBe(0.5);
      expect(Engine.audioEngine!.setGlobalVolume).toHaveBeenCalledWith(0.5);
    });

    it('should clamp master volume to 0-1 range', () => {
      audioManager.masterVolume = 2.0;
      expect(audioManager.masterVolume).toBe(1.0);

      audioManager.masterVolume = -0.5;
      expect(audioManager.masterVolume).toBe(0);
    });
  });

  describe('mute/unmute', () => {
    it('should mute audio', () => {
      audioManager.mute();

      expect(Engine.audioEngine!.setGlobalVolume).toHaveBeenCalledWith(0);
    });

    it('should unmute audio to master volume', () => {
      audioManager.masterVolume = 0.8;
      audioManager.unmute();

      expect(Engine.audioEngine!.setGlobalVolume).toHaveBeenCalledWith(0.8);
    });
  });

  describe('dispose', () => {
    it('should dispose all resources', () => {
      const sound = audioManager.createSpatialSound('test', 'test.mp3');
      audioManager.attachToCamera(mockCamera);

      audioManager.dispose();

      expect(sound.dispose).toHaveBeenCalled();
      expect(audioManager.camera).toBeNull();
      expect(audioManager.isUnlocked).toBe(false);
    });
  });
});
