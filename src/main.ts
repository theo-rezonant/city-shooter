import { App } from './core/App';

/**
 * Main entry point for the City Shooter game.
 * Initializes the Babylon.js engine and starts the application.
 */
async function main(): Promise<void> {
  // Get the canvas element
  const canvas = document.getElementById('renderCanvas') as HTMLCanvasElement;

  if (!canvas) {
    throw new Error('Could not find canvas element with id "renderCanvas"');
  }

  // Create the app
  const app = new App({
    canvas,
    antialias: true,
    enablePhysics: true,
  });

  // Add state change listener for debugging
  app.onStateChange((newState, oldState) => {
    console.log(`State changed: ${oldState ?? 'null'} -> ${newState}`);
  });

  // Start the application
  // This initializes the engine, transitions to LOADING state,
  // and starts the render loop
  try {
    await app.start();
    console.log('Application started successfully');
  } catch (error) {
    console.error('Failed to start application:', error);
  }

  // Handle cleanup on page unload
  window.addEventListener('beforeunload', () => {
    app.dispose();
  });

  // Expose app globally for debugging (remove in production)
  if (import.meta.env.DEV) {
    (window as unknown as { app: App }).app = app;
  }
}

// Run the main function
main().catch(console.error);
