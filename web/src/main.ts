/**
 * City Shooter - Main Entry Point
 *
 * Babylon.js FPS game with Havok physics engine.
 */

import { App } from './core';

// Main initialization
async function main(): Promise<void> {
  console.log('Starting City Shooter...');

  try {
    // Create the application
    const app = new App('renderCanvas');

    // Initialize (loads Havok WASM, creates scene)
    await app.initialize();

    // Start the render loop
    app.run();

    // Expose app globally for debugging (remove in production)
    (window as unknown as { app: App }).app = app;

    console.log('City Shooter is running!');
    console.log('Click "Enter Game" to start');
  } catch (error) {
    console.error('Failed to start City Shooter:', error);

    // Show error to user
    const loadingElement = document.getElementById('loading');
    if (loadingElement) {
      loadingElement.innerHTML = `
        <div style="color: red; font-size: 18px;">
          <h2>Failed to Load</h2>
          <p>${error instanceof Error ? error.message : 'Unknown error'}</p>
          <p style="font-size: 14px; color: gray;">Check the console for details.</p>
        </div>
      `;
    }
  }
}

// Start the application when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', main);
} else {
  main();
}
