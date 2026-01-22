import { defineConfig } from 'vite';

export default defineConfig({
  // Increase timeout for large asset serving during development
  server: {
    // Increase header timeout for large file requests (like 50MB GLB files)
    headers: {
      'Cross-Origin-Opener-Policy': 'same-origin',
      'Cross-Origin-Embedder-Policy': 'require-corp',
    },
  },
  // Configure optimizations for Babylon.js
  optimizeDeps: {
    // Exclude Havok WASM from optimization - it needs to be loaded as-is
    exclude: ['@babylonjs/havok'],
  },
  // Configure build options
  build: {
    // Increase chunk size warning limit for large game assets
    chunkSizeWarningLimit: 2000,
    // Ensure WASM files are handled correctly
    assetsInlineLimit: 0,
    rollupOptions: {
      output: {
        // Keep asset filenames consistent for caching
        assetFileNames: 'assets/[name]-[hash][extname]',
      },
    },
  },
  // Ensure .wasm files are treated as assets
  assetsInclude: ['**/*.wasm', '**/*.glb', '**/*.gltf', '**/*.fbx'],
});
