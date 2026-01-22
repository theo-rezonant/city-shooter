import { Engine, Scene, FreeCamera, Vector3, HemisphericLight, Color4 } from '@babylonjs/core';

// Import loaders for future use (GLB/GLTF support)
import '@babylonjs/loaders/glTF';

/**
 * Initialize the Babylon.js engine and create a blank scene.
 * This serves as the foundation for the Siege FPS project.
 */
async function initializeBabylon(): Promise<void> {
  // Get the canvas element
  const canvas = document.getElementById('renderCanvas') as HTMLCanvasElement;

  if (!canvas) {
    throw new Error('Canvas element not found');
  }

  // Create the Babylon.js engine
  const engine = new Engine(canvas, true, {
    preserveDrawingBuffer: true,
    stencil: true,
    disableWebGL2Support: false,
  });

  // Create an empty scene
  const scene = new Scene(engine);
  scene.clearColor = new Color4(0.1, 0.1, 0.15, 1); // Dark blue-gray background

  // Create a basic camera for the scene
  const camera = new FreeCamera('mainCamera', new Vector3(0, 5, -10), scene);
  camera.setTarget(Vector3.Zero());
  camera.attachControl(canvas, true);

  // Add a simple hemispheric light
  const light = new HemisphericLight('light', new Vector3(0, 1, 0), scene);
  light.intensity = 0.7;

  // Handle window resize
  window.addEventListener('resize', () => {
    engine.resize();
  });

  // Setup pointer lock on canvas click (for future FPS controls)
  // This needs to be user-initiated per browser security policies
  canvas.addEventListener('click', () => {
    if (!engine.isPointerLock) {
      engine.enterPointerlock();
    }
  });

  // Setup audio unlock on user interaction (for future spatial audio)
  // Browser audio policies require user interaction to enable audio
  const unlockAudio = (): void => {
    if (Engine.audioEngine) {
      Engine.audioEngine.unlock();
      document.removeEventListener('click', unlockAudio);
      document.removeEventListener('keydown', unlockAudio);
    }
  };
  document.addEventListener('click', unlockAudio);
  document.addEventListener('keydown', unlockAudio);

  // Start the render loop
  engine.runRenderLoop(() => {
    scene.render();
  });

  // Log success message
  console.log('Babylon.js engine initialized successfully');
}

// Initialize the application
initializeBabylon().catch((error) => {
  console.error('Failed to initialize Babylon.js:', error);
});
