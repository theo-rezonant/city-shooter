import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  build: {
    target: 'esnext',
    sourcemap: true,
  },
  optimizeDeps: {
    exclude: ['@babylonjs/havok'],
  },
  server: {
    port: 3000,
    open: true,
  },
  // Ensure WASM files are properly served
  assetsInclude: ['**/*.wasm'],
});
